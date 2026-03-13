namespace AccountBalance.Core.Tests.Integration;

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using AccountBalance.Core.Application.Commands;
using AccountBalance.Core.Application.DTOs;
using AccountBalance.Core.Application.Handlers;
using AccountBalance.Core.Domain.Aggregates;
using AccountBalance.Core.Domain.Entities;
using AccountBalance.Core.Domain.Repositories;
using AccountBalance.Core.Infrastructure.Persistence;
using AccountBalance.Core.Tests.Integration.Fixtures;
using global::Shared.Domain.Enums;
using global::Shared.Domain.Entities;
using global::Shared.Infrastructure.Persistence.Mongo;

[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
public class ProcessTransactionIntegrationTests
{
    private readonly MongoDbFixture _fixture;

    public ProcessTransactionIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    private (ProcessTransactionHandler handler, MongoDbContext dbContext) CreateHandler(string databaseName)
    {
        var options = Options.Create(new MongoDbOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = databaseName
        });

        var dbContext = new MongoDbContext(options, NullLogger<MongoDbContext>.Instance);

        var movementRepo = new MovementRepository(dbContext, NullLogger<MovementRepository>.Instance);
        var balanceRepo = new AccountBalanceRepository(dbContext, NullLogger<AccountBalanceRepository>.Instance);
        var processedEventRepo = new ProcessedEventRepository(dbContext, NullLogger<ProcessedEventRepository>.Instance);
        var configOptions = Options.Create(new MongoDbConfigOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = databaseName
        });
        var configContext = new MongoDbConfigContext(configOptions, NullLogger<MongoDbConfigContext>.Instance);
        var mappingRepo = new CompanyAccountMappingRepository(configContext, NullLogger<CompanyAccountMappingRepository>.Instance);
        var userAccountAssignmentRepo = new UserAccountAssignmentRepository(configContext, NullLogger<UserAccountAssignmentRepository>.Instance);
        var accountProvisioningRepo = new AccountProvisioningRepository(configContext, NullLogger<AccountProvisioningRepository>.Instance);

        var handler = new ProcessTransactionHandler(
            movementRepo, balanceRepo, processedEventRepo,
            mappingRepo, userAccountAssignmentRepo, accountProvisioningRepo, dbContext, NullLogger<ProcessTransactionHandler>.Instance);

        return (handler, dbContext);
    }

    private static string BuildPayload(
        decimal totalAmount,
        Currency currency,
        string transactionId,
        string? accountId = "acc-001",
        PaymentMethodPayload? paymentMethod = null,
        MerchantPayload? merchant = null) =>
        JsonSerializer.Serialize(new MovementPayload
        {
            Amount = new AmountPayload
            {
                TotalAmount = totalAmount,
                GrossAmount = totalAmount,
                NetAmount = totalAmount - 2m,
                PaymentFee = 1m
            },
            TransactionId = transactionId,
            Account = new AccountPayload { AccountId = accountId, Currency = currency },
            Country = "US",
            Description = "Integration test",
            PaymentMethod = paymentMethod,
            Merchant = merchant
        });

    private static ProcessTransactionCommand CreatePayInCommand(
        Guid companyId,
        decimal amount,
        Currency currency = Currency.USD,
        string? accountId = "acc-001") =>
        new(
            TransactionId: Guid.NewGuid(),
            CompanyId: companyId,
            EventType: MovementEventType.TransactionApproved,
            RawPayload: BuildPayload(amount, currency, Guid.NewGuid().ToString(), accountId));

    private static ProcessTransactionCommand CreatePayOutCommand(
        Guid companyId,
        decimal amount,
        Currency currency = Currency.USD,
        string? accountId = "acc-001") =>
        new(
            TransactionId: Guid.NewGuid(),
            CompanyId: companyId,
            EventType: MovementEventType.PayoutFinished,
            RawPayload: BuildPayload(amount, currency, Guid.NewGuid().ToString(), accountId));

    #region Full PayIn Flow

    [Fact]
    public async Task PayIn_ShouldCreateBalanceMovementMappingAndIdempotencyRecord()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        var command = CreatePayInCommand(companyId, 500m);
        await handler.HandleAsync(command);

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.CompanyId == companyId).SingleAsync();
        balance.AvailableBalance.Should().Be(500m);
        balance.TotalPayins.Should().Be(500m);
        balance.TotalPayouts.Should().Be(0m);
        balance.Currency.Should().Be(Currency.USD);
        balance.AccountId.Should().NotBe(Guid.Empty);

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(1);

        var mappings = dbContext.GetCollection<CompanyAccountMapping>("company_account");
        var mapping = await mappings.Find(m => m.CompanyId == companyId).SingleAsync();
        mapping.AccountId.Should().NotBe(Guid.Empty);

        var events = dbContext.GetCollection<ProcessedEvent>("processed_events");
        var processedEvent = await events.Find(e => e.TransactionId == command.TransactionId).SingleAsync();
        processedEvent.TransactionId.Should().Be(command.TransactionId);
    }

    #endregion

    #region PayIn then PayOut

    [Fact]
    public async Task PayIn_ThenPayOut_ShouldUpdateBalanceCorrectly()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(companyId, 1000m));
        await handler.HandleAsync(CreatePayOutCommand(companyId, 350m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.CompanyId == companyId).SingleAsync();
        balance.AvailableBalance.Should().Be(650m);
        balance.TotalPayins.Should().Be(1000m);
        balance.TotalPayouts.Should().Be(350m);

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(2);
    }

    #endregion

    #region Multiple PayIns Accumulate

    [Fact]
    public async Task MultiplePayIns_ShouldAccumulateBalance()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(companyId, 200m));
        await handler.HandleAsync(CreatePayInCommand(companyId, 300m));
        await handler.HandleAsync(CreatePayInCommand(companyId, 150m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.CompanyId == companyId).SingleAsync();
        balance.AvailableBalance.Should().Be(650m);
        balance.TotalPayins.Should().Be(650m);

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(3);
    }

    #endregion

    #region Idempotency

    [Fact]
    public async Task DuplicateTransactionIdAndEventType_ShouldNotDuplicateData()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        var command = CreatePayInCommand(companyId, 500m);
        await handler.HandleAsync(command);
        await handler.HandleAsync(command);

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.CompanyId == companyId).SingleAsync();
        balance.AvailableBalance.Should().Be(500m, "second call with same TransactionId+EventType should be ignored");

        var events = dbContext.GetCollection<ProcessedEvent>("processed_events");
        var eventCount = await events.CountDocumentsAsync(
            Builders<ProcessedEvent>.Filter.Eq(e => e.TransactionId, command.TransactionId));
        eventCount.Should().Be(1);

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(1);
    }

    [Fact]
    public async Task SameTransactionId_DifferentEventType_ShouldProcessBoth()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        var payInCommand = new ProcessTransactionCommand(
            TransactionId: transactionId,
            CompanyId: companyId,
            EventType: MovementEventType.TransactionApproved,
            RawPayload: BuildPayload(500m, Currency.USD, Guid.NewGuid().ToString()));

        var chargebackCommand = new ProcessTransactionCommand(
            TransactionId: transactionId,
            CompanyId: companyId,
            EventType: MovementEventType.ChargebackClose,
            RawPayload: BuildPayload(500m, Currency.USD, Guid.NewGuid().ToString()));

        await handler.HandleAsync(payInCommand);
        await handler.HandleAsync(chargebackCommand);

        var events = dbContext.GetCollection<ProcessedEvent>("processed_events");
        var eventCount = await events.CountDocumentsAsync(
            Builders<ProcessedEvent>.Filter.Eq(e => e.TransactionId, transactionId));
        eventCount.Should().Be(2, "same TransactionId with different EventType should both be processed");

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(2);
    }

    #endregion

    #region Company Mapping Update

    [Fact]
    public async Task SecondTransaction_ShouldUpdateExistingCompanyMapping()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(companyId, 100m, accountId: "acc-001"));
        await handler.HandleAsync(CreatePayInCommand(companyId, 200m, accountId: "acc-002"));

        var mappings = dbContext.GetCollection<CompanyAccountMapping>("company_account");
        var mappingCount = await mappings.CountDocumentsAsync(
            Builders<CompanyAccountMapping>.Filter.Eq(m => m.CompanyId, companyId));
        mappingCount.Should().Be(1, "should update, not duplicate");

        var mapping = await mappings.Find(m => m.CompanyId == companyId).SingleAsync();
        mapping.AccountId.Should().NotBe(Guid.Empty, "should reflect the latest account");
    }

    #endregion

    #region Negative Balance Allowed

    [Fact]
    public async Task PayOut_WithoutSufficientFunds_ShouldAllowNegativeBalance()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(companyId, 100m));
        await handler.HandleAsync(CreatePayOutCommand(companyId, 250m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.CompanyId == companyId).SingleAsync();
        balance.AvailableBalance.Should().Be(-150m);
        balance.TotalPayins.Should().Be(100m);
        balance.TotalPayouts.Should().Be(250m);
    }

    #endregion

    #region Multi-Currency Isolation

    [Fact]
    public async Task DifferentCurrencies_ShouldCreateSeparateBalances()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(companyId, 500m, Currency.USD));
        await handler.HandleAsync(CreatePayInCommand(companyId, 300m, Currency.EUR));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var allBalances = await balances.Find(b => b.CompanyId == companyId).ToListAsync();
        allBalances.Should().HaveCount(2);

        var usd = allBalances.Single(b => b.Currency == Currency.USD);
        usd.AvailableBalance.Should().Be(500m);

        var eur = allBalances.Single(b => b.Currency == Currency.EUR);
        eur.AvailableBalance.Should().Be(300m);
    }

    #endregion

    #region Full Complex Scenario

    [Fact]
    public async Task ComplexScenario_MultipleOperations_ShouldMaintainConsistency()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(companyId, 1000m));
        await handler.HandleAsync(CreatePayOutCommand(companyId, 200m));
        await handler.HandleAsync(CreatePayInCommand(companyId, 500m));
        await handler.HandleAsync(CreatePayOutCommand(companyId, 150m));
        await handler.HandleAsync(CreatePayOutCommand(companyId, 100m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.CompanyId == companyId).SingleAsync();

        balance.AvailableBalance.Should().Be(1050m);
        balance.TotalPayins.Should().Be(1500m);
        balance.TotalPayouts.Should().Be(450m);
        balance.LastMovementAt.Should().NotBeNull();

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(5);

        var events = dbContext.GetCollection<ProcessedEvent>("processed_events");
        var eventCount = await events.CountDocumentsAsync(FilterDefinition<ProcessedEvent>.Empty);
        eventCount.Should().Be(5);
    }

    #endregion

    #region Full Payload With PaymentMethod And Merchant

    [Fact]
    public async Task PayIn_WithPaymentMethodAndMerchant_ShouldPersistFullMovementDetails()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var companyId = Guid.NewGuid();
        var transactionId = Guid.NewGuid().ToString();

        var paymentMethod = new PaymentMethodPayload
        {
            PaymentMethodId = "pm-12345",
            ProviderName = "Stripe",
            Type = PaymentMethodType.CreditCard
        };

        var merchant = new MerchantPayload
        {
            MerchantId = "merchant-001",
            MerchantName = "Acme Corp",
            Shop = new ShopPayload
            {
                ShopId = "shop-042",
                ShopName = "Acme Online Store"
            }
        };

        var rawPayload = BuildPayload(750m, Currency.USD, transactionId,
            paymentMethod: paymentMethod, merchant: merchant);

        var command = new ProcessTransactionCommand(
            TransactionId: Guid.NewGuid(),
            CompanyId: companyId,
            EventType: MovementEventType.TransactionApproved,
            RawPayload: rawPayload);

        await handler.HandleAsync(command);

        var movements = dbContext.GetCollection<Movement>("movements");
        var movement = await movements.Find(FilterDefinition<Movement>.Empty).SingleAsync();

        movement.TransactionId.Should().Be(transactionId);
        movement.Amount.TotalAmount.Should().Be(750m);
        movement.Amount.Currency.Should().Be(Currency.USD);
        movement.Amount.GrossAmount.Should().Be(750m);
        movement.Amount.NetAmount.Should().Be(748m);
        movement.Amount.PaymentFee.Should().Be(1m);

        movement.PaymentMethod.Should().NotBeNull();
        movement.PaymentMethod!.PaymentMethodId.Should().Be("pm-12345");
        movement.PaymentMethod.ProviderName.Should().Be("Stripe");
        movement.PaymentMethod.Type.Should().Be(PaymentMethodType.CreditCard);

        movement.Merchant.Should().NotBeNull();
        movement.Merchant!.MerchantId.Should().Be("merchant-001");
        movement.Merchant.MerchantName.Should().Be("Acme Corp");
        movement.Merchant.Shop.Should().NotBeNull();
        movement.Merchant.Shop!.ShopId.Should().Be("shop-042");
        movement.Merchant.Shop.ShopName.Should().Be("Acme Online Store");

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.CompanyId == companyId).SingleAsync();
        balance.AvailableBalance.Should().Be(750m);
        balance.TotalPayins.Should().Be(750m);
    }

    #endregion
}
