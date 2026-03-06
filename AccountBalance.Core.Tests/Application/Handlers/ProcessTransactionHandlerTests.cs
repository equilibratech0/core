namespace AccountBalance.Core.Tests.Application.Handlers;

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using AccountBalance.Core.Application.Commands;
using AccountBalance.Core.Application.DTOs;
using AccountBalance.Core.Application.Handlers;
using AccountBalance.Core.Domain.Aggregates;
using AccountBalance.Core.Domain.Entities;
using AccountBalance.Core.Domain.Repositories;
using global::Shared.Domain.Enums;
using global::Shared.Infrastructure.Persistence.Abstractions;

public class ProcessTransactionHandlerTests
{
    private readonly Mock<IMovementRepository> _movementRepo = new();
    private readonly Mock<IAccountBalanceRepository> _balanceRepo = new();
    private readonly Mock<IProcessedEventRepository> _processedEventRepo = new();
    private readonly Mock<IClientAccountMappingRepository> _mappingRepo = new();
    private readonly Mock<IMongoDbContext> _dbContext = new();
    private readonly Mock<ILogger<ProcessTransactionHandler>> _logger = new();

    private ProcessTransactionHandler CreateHandler() => new(
        _movementRepo.Object,
        _balanceRepo.Object,
        _processedEventRepo.Object,
        _mappingRepo.Object,
        _dbContext.Object,
        _logger.Object);

    private static string BuildRawPayload(
        decimal totalAmount = 100m,
        Currency currency = Currency.USD,
        string transactionId = "txn-001",
        string? accountId = "acc-001",
        string? country = "US",
        string? description = "Test payment") =>
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
            Country = country,
            Description = description
        });

    private static ProcessTransactionCommand CreateCommand(
        MovementEventType eventType = MovementEventType.TransactionCreated,
        string? rawPayload = null,
        string idempotencyKey = "idem-key-001") =>
        new(
            TransactionId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            ClientName: "TestClient",
            UserIds: new List<string> { "user-1" },
            IdempotencyKey: idempotencyKey,
            EventType: eventType,
            RawPayload: rawPayload ?? BuildRawPayload());

    #region Idempotency

    [Fact]
    public async Task HandleAsync_DuplicateEvent_ShouldSkipProcessing()
    {
        var command = CreateCommand();
        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        _movementRepo.Verify(r => r.AddAsync(It.IsAny<global::Shared.Domain.Entities.Movement>(), It.IsAny<CancellationToken>()), Times.Never);
        _balanceRepo.Verify(r => r.AddAsync(It.IsAny<AccountBalanceEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _dbContext.Verify(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region PayIn - New Balance

    [Fact]
    public async Task HandleAsync_PayInWithNewBalance_ShouldCreateBalanceWithCredit()
    {
        var command = CreateCommand(MovementEventType.TransactionCreated);

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, "acc-001", Currency.USD, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountBalanceEntry?)null);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientAccountMapping?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        _balanceRepo.Verify(r => r.AddAsync(
            It.Is<AccountBalanceEntry>(b =>
                b.AvailableBalance == 100m &&
                b.TotalCredits == 100m &&
                b.Currency == Currency.USD),
            It.IsAny<CancellationToken>()), Times.Once);

        _movementRepo.Verify(r => r.AddAsync(It.IsAny<global::Shared.Domain.Entities.Movement>(), It.IsAny<CancellationToken>()), Times.Once);
        _processedEventRepo.Verify(r => r.AddAsync(It.IsAny<ProcessedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _dbContext.Verify(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region PayOut - Existing Balance

    [Fact]
    public async Task HandleAsync_PayOutWithExistingBalance_ShouldUpdateBalanceWithDebit()
    {
        var command = CreateCommand(MovementEventType.PayoutCreated);
        var existingBalance = AccountBalanceEntry.Create(command.ClientId, "acc-001", Currency.USD);
        existingBalance.ApplyCredit(500m);

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, "acc-001", Currency.USD, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientAccountMapping(command.ClientId, "TestClient", "acc-001", new List<string>()));

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        existingBalance.AvailableBalance.Should().Be(400m);
        existingBalance.TotalDebits.Should().Be(100m);

        _balanceRepo.Verify(r => r.UpdateAsync(existingBalance, It.IsAny<CancellationToken>()), Times.Once);
        _balanceRepo.Verify(r => r.AddAsync(It.IsAny<AccountBalanceEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Client Account Mapping

    [Fact]
    public async Task HandleAsync_NewClient_ShouldCreateMapping()
    {
        var command = CreateCommand();

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, "acc-001", Currency.USD, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountBalanceEntry?)null);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientAccountMapping?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        _mappingRepo.Verify(r => r.AddAsync(
            It.Is<ClientAccountMapping>(m =>
                m.ClientId == command.ClientId &&
                m.ClientName == "TestClient"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingClient_ShouldUpdateMapping()
    {
        var command = CreateCommand();
        var existingMapping = new ClientAccountMapping(command.ClientId, "TestClient", "old-acc", new List<string>());

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, "acc-001", Currency.USD, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountBalanceEntry?)null);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMapping);

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        _mappingRepo.Verify(r => r.UpdateAsync(existingMapping, It.IsAny<CancellationToken>()), Times.Once);
        _mappingRepo.Verify(r => r.AddAsync(It.IsAny<ClientAccountMapping>(), It.IsAny<CancellationToken>()), Times.Never);
        existingMapping.AccountId.Should().Be("acc-001");
    }

    #endregion

    #region AccountId fallback

    [Fact]
    public async Task HandleAsync_WhenAccountIdIsNull_ShouldFallbackToClientId()
    {
        var rawPayload = BuildRawPayload(accountId: null);
        var command = CreateCommand(rawPayload: rawPayload);

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, command.ClientId.ToString(), Currency.USD, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountBalanceEntry?)null);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientAccountMapping?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        _balanceRepo.Verify(r => r.GetByAccountAndCurrencyAsync(
            command.ClientId, command.ClientId.ToString(), Currency.USD, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Payload Deserialization Errors

    [Fact]
    public async Task HandleAsync_InvalidJsonPayload_ShouldThrowInvalidOperationException()
    {
        var command = CreateCommand(rawPayload: "not-valid-json");

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task HandleAsync_PayloadMissingAmount_ShouldThrowInvalidOperationException()
    {
        var payload = JsonSerializer.Serialize(new { TransactionId = "txn-001" });
        var command = CreateCommand(rawPayload: payload);

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Amount*");
    }

    [Fact]
    public async Task HandleAsync_PayloadMissingTransactionId_ShouldThrowInvalidOperationException()
    {
        var payload = JsonSerializer.Serialize(new
        {
            Amount = new { TotalAmount = 100m, Currency = 0 }
        });
        var command = CreateCommand(rawPayload: payload);

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TransactionId*");
    }

    #endregion

    #region Transaction Rollback

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ShouldAbortTransactionAndRethrow()
    {
        var command = CreateCommand();

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, "acc-001", Currency.USD, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountBalanceEntry?)null);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientAccountMapping?)null);
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<global::Shared.Domain.Entities.Movement>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        var handler = CreateHandler();

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB failure");
        _dbContext.Verify(c => c.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbContext.Verify(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Full PayIn Flow with PaymentMethod and Merchant

    [Fact]
    public async Task HandleAsync_PayloadWithPaymentMethodAndMerchant_ShouldProcessSuccessfully()
    {
        var rawPayload = JsonSerializer.Serialize(new MovementPayload
        {
            Amount = new AmountPayload
            {
                TotalAmount = 250m,
                Currency = Currency.EUR,
                GrossAmount = 260m,
                NetAmount = 245m,
                PaymentFee = 10m,
                PlatformFee = 5m
            },
            TransactionId = "txn-full",
            AccountId = "acc-full",
            Country = "ES",
            Description = "Full payment",
            PaymentMethod = new PaymentMethodPayload
            {
                PaymentMethodId = "pm-001",
                ProviderName = "Stripe",
                Type = PaymentMethodType.CreditCard
            },
            Merchant = new MerchantPayload
            {
                MerchantId = "merch-001",
                MerchantName = "TestMerchant",
                Shop = new ShopPayload
                {
                    ShopId = "shop-001",
                    ShopName = "TestShop"
                }
            }
        });

        var command = CreateCommand(MovementEventType.TransactionApproved, rawPayload);

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, "acc-full", Currency.EUR, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountBalanceEntry?)null);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientAccountMapping?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        _balanceRepo.Verify(r => r.AddAsync(
            It.Is<AccountBalanceEntry>(b =>
                b.AvailableBalance == 250m &&
                b.Currency == Currency.EUR),
            It.IsAny<CancellationToken>()), Times.Once);

        _dbContext.Verify(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Constructor Null Guards

    [Fact]
    public void Constructor_NullMovementRepository_ShouldThrow()
    {
        var act = () => new ProcessTransactionHandler(
            null!, _balanceRepo.Object, _processedEventRepo.Object,
            _mappingRepo.Object, _dbContext.Object, _logger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("movementRepository");
    }

    [Fact]
    public void Constructor_NullBalanceRepository_ShouldThrow()
    {
        var act = () => new ProcessTransactionHandler(
            _movementRepo.Object, null!, _processedEventRepo.Object,
            _mappingRepo.Object, _dbContext.Object, _logger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("balanceRepository");
    }

    [Fact]
    public void Constructor_NullProcessedEventRepository_ShouldThrow()
    {
        var act = () => new ProcessTransactionHandler(
            _movementRepo.Object, _balanceRepo.Object, null!,
            _mappingRepo.Object, _dbContext.Object, _logger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("processedEventRepository");
    }

    [Fact]
    public void Constructor_NullClientAccountMappingRepository_ShouldThrow()
    {
        var act = () => new ProcessTransactionHandler(
            _movementRepo.Object, _balanceRepo.Object, _processedEventRepo.Object,
            null!, _dbContext.Object, _logger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("clientAccountMappingRepository");
    }

    [Fact]
    public void Constructor_NullDbContext_ShouldThrow()
    {
        var act = () => new ProcessTransactionHandler(
            _movementRepo.Object, _balanceRepo.Object, _processedEventRepo.Object,
            _mappingRepo.Object, null!, _logger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new ProcessTransactionHandler(
            _movementRepo.Object, _balanceRepo.Object, _processedEventRepo.Object,
            _mappingRepo.Object, _dbContext.Object, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Transaction Lifecycle

    [Fact]
    public async Task HandleAsync_SuccessfulProcessing_ShouldFollowBeginCommitLifecycle()
    {
        var callOrder = new List<string>();
        var command = CreateCommand();

        _processedEventRepo.Setup(r => r.ExistsAsync(command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _balanceRepo.Setup(r => r.GetByAccountAndCurrencyAsync(
                command.ClientId, "acc-001", Currency.USD, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountBalanceEntry?)null);
        _mappingRepo.Setup(r => r.GetByClientIdAsync(command.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientAccountMapping?)null);

        _dbContext.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("BeginTransaction"));
        _dbContext.Setup(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("CommitTransaction"));

        var handler = CreateHandler();
        await handler.HandleAsync(command);

        callOrder.Should().ContainInOrder("BeginTransaction", "CommitTransaction");
        _dbContext.Verify(c => c.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
