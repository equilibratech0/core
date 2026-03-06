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
        var mappingRepo = new ClientAccountMappingRepository(dbContext, NullLogger<ClientAccountMappingRepository>.Instance);

        var handler = new ProcessTransactionHandler(
            movementRepo, balanceRepo, processedEventRepo,
            mappingRepo, dbContext, NullLogger<ProcessTransactionHandler>.Instance);

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
                Currency = currency,
                GrossAmount = totalAmount,
                NetAmount = totalAmount - 2m,
                PaymentFee = 1m,
                PlatformFee = 1m
            },
            TransactionId = transactionId,
            AccountId = accountId,
            Country = "US",
            Description = "Integration test",
            PaymentMethod = paymentMethod,
            Merchant = merchant
        });

    private static ProcessTransactionCommand CreatePayInCommand(
        Guid clientId,
        decimal amount,
        Currency currency = Currency.USD,
        string? idempotencyKey = null,
        string? accountId = "acc-001") =>
        new(
            TransactionId: Guid.NewGuid(),
            ClientId: clientId,
            ClientName: "IntegrationClient",
            UserIds: new List<string> { "user-1" },
            IdempotencyKey: idempotencyKey ?? Guid.NewGuid().ToString(),
            EventType: MovementEventType.TransactionCreated,
            RawPayload: BuildPayload(amount, currency, Guid.NewGuid().ToString(), accountId));

    private static ProcessTransactionCommand CreatePayOutCommand(
        Guid clientId,
        decimal amount,
        Currency currency = Currency.USD,
        string? idempotencyKey = null,
        string? accountId = "acc-001") =>
        new(
            TransactionId: Guid.NewGuid(),
            ClientId: clientId,
            ClientName: "IntegrationClient",
            UserIds: new List<string> { "user-1" },
            IdempotencyKey: idempotencyKey ?? Guid.NewGuid().ToString(),
            EventType: MovementEventType.PayoutCreated,
            RawPayload: BuildPayload(amount, currency, Guid.NewGuid().ToString(), accountId));

    #region Full PayIn Flow

    [Fact]
    public async Task PayIn_ShouldCreateBalanceMovementMappingAndIdempotencyRecord()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var clientId = Guid.NewGuid();

        var command = CreatePayInCommand(clientId, 500m);
        await handler.HandleAsync(command);

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.ClientId == clientId).SingleAsync();
        balance.AvailableBalance.Should().Be(500m);
        balance.TotalPayins.Should().Be(500m);
        balance.TotalPayouts.Should().Be(0m);
        balance.Currency.Should().Be(Currency.USD);
        balance.AccountId.Should().Be("acc-001");

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(1);

        var mappings = dbContext.GetCollection<ClientAccountMapping>("client_account");
        var mapping = await mappings.Find(m => m.ClientId == clientId).SingleAsync();
        mapping.ClientName.Should().Be("IntegrationClient");
        mapping.AccountId.Should().Be("acc-001");

        var events = dbContext.GetCollection<ProcessedEvent>("processed_events");
        var processedEvent = await events.Find(e => e.IdempotencyKey == command.IdempotencyKey).SingleAsync();
        processedEvent.SourceTransactionId.Should().Be(command.TransactionId);
    }

    #endregion

    #region PayIn then PayOut

    [Fact]
    public async Task PayIn_ThenPayOut_ShouldUpdateBalanceCorrectly()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var clientId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(clientId, 1000m));
        await handler.HandleAsync(CreatePayOutCommand(clientId, 350m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.ClientId == clientId).SingleAsync();
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
        var clientId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(clientId, 200m));
        await handler.HandleAsync(CreatePayInCommand(clientId, 300m));
        await handler.HandleAsync(CreatePayInCommand(clientId, 150m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.ClientId == clientId).SingleAsync();
        balance.AvailableBalance.Should().Be(650m);
        balance.TotalPayins.Should().Be(650m);

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(3);
    }

    #endregion

    #region Idempotency

    [Fact]
    public async Task DuplicateIdempotencyKey_ShouldNotDuplicateData()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var clientId = Guid.NewGuid();
        var idempotencyKey = "idem-duplicate-test";

        var command = CreatePayInCommand(clientId, 500m, idempotencyKey: idempotencyKey);
        await handler.HandleAsync(command);
        await handler.HandleAsync(command);

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.ClientId == clientId).SingleAsync();
        balance.AvailableBalance.Should().Be(500m, "second call with same key should be ignored");

        var events = dbContext.GetCollection<ProcessedEvent>("processed_events");
        var eventCount = await events.CountDocumentsAsync(
            Builders<ProcessedEvent>.Filter.Eq(e => e.IdempotencyKey, idempotencyKey));
        eventCount.Should().Be(1);

        var movements = dbContext.GetCollection<Movement>("movements");
        var movementCount = await movements.CountDocumentsAsync(FilterDefinition<Movement>.Empty);
        movementCount.Should().Be(1);
    }

    #endregion

    #region Client Mapping Update

    [Fact]
    public async Task SecondTransaction_ShouldUpdateExistingClientMapping()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var clientId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(clientId, 100m, accountId: "acc-001"));
        await handler.HandleAsync(CreatePayInCommand(clientId, 200m, accountId: "acc-002"));

        var mappings = dbContext.GetCollection<ClientAccountMapping>("client_account");
        var mappingCount = await mappings.CountDocumentsAsync(
            Builders<ClientAccountMapping>.Filter.Eq(m => m.ClientId, clientId));
        mappingCount.Should().Be(1, "should update, not duplicate");

        var mapping = await mappings.Find(m => m.ClientId == clientId).SingleAsync();
        mapping.AccountId.Should().Be("acc-002", "should reflect the latest account");
    }

    #endregion

    #region Negative Balance Allowed

    [Fact]
    public async Task PayOut_WithoutSufficientFunds_ShouldAllowNegativeBalance()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var (handler, dbContext) = CreateHandler(dbName);
        var clientId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(clientId, 100m));
        await handler.HandleAsync(CreatePayOutCommand(clientId, 250m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.ClientId == clientId).SingleAsync();
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
        var clientId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(clientId, 500m, Currency.USD));
        await handler.HandleAsync(CreatePayInCommand(clientId, 300m, Currency.EUR));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var allBalances = await balances.Find(b => b.ClientId == clientId).ToListAsync();
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
        var clientId = Guid.NewGuid();

        await handler.HandleAsync(CreatePayInCommand(clientId, 1000m));
        await handler.HandleAsync(CreatePayOutCommand(clientId, 200m));
        await handler.HandleAsync(CreatePayInCommand(clientId, 500m));
        await handler.HandleAsync(CreatePayOutCommand(clientId, 150m));
        await handler.HandleAsync(CreatePayOutCommand(clientId, 100m));

        var balances = dbContext.GetCollection<AccountBalanceEntry>("account_balances");
        var balance = await balances.Find(b => b.ClientId == clientId).SingleAsync();

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
        var clientId = Guid.NewGuid();
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
            ClientId: clientId,
            ClientName: "MerchantClient",
            UserIds: new List<string> { "user-1", "user-2" },
            IdempotencyKey: Guid.NewGuid().ToString(),
            EventType: MovementEventType.TransactionCreated,
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
        movement.Amount.PlatformFee.Should().Be(1m);

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
        var balance = await balances.Find(b => b.ClientId == clientId).SingleAsync();
        balance.AvailableBalance.Should().Be(750m);
        balance.TotalPayins.Should().Be(750m);
    }

    #endregion
}
