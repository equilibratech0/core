namespace AccountBalance.Core.Application.Handlers;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using AccountBalance.Core.Application.Commands;
using AccountBalance.Core.Application.DTOs;
using AccountBalance.Core.Application.Interfaces;
using AccountBalance.Core.Domain.Aggregates;
using AccountBalance.Core.Domain.Entities;
using AccountBalance.Core.Domain.Enums;
using AccountBalance.Core.Domain.Repositories;
using AccountBalance.Core.Domain.Services;
using Shared.Domain.Entities;
using Shared.Infrastructure.Persistence.Abstractions;

public class ProcessTransactionHandler : IProcessTransactionHandler
{
    private readonly IMovementRepository _movementRepository;
    private readonly IAccountBalanceRepository _balanceRepository;
    private readonly IProcessedEventRepository _processedEventRepository;
    private readonly IClientAccountMappingRepository _clientAccountMappingRepository;
    private readonly IMongoDbContext _dbContext;
    private readonly ILogger<ProcessTransactionHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProcessTransactionHandler(
        IMovementRepository movementRepository,
        IAccountBalanceRepository balanceRepository,
        IProcessedEventRepository processedEventRepository,
        IClientAccountMappingRepository clientAccountMappingRepository,
        IMongoDbContext dbContext,
        ILogger<ProcessTransactionHandler> logger)
    {
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _balanceRepository = balanceRepository ?? throw new ArgumentNullException(nameof(balanceRepository));
        _processedEventRepository = processedEventRepository ?? throw new ArgumentNullException(nameof(processedEventRepository));
        _clientAccountMappingRepository = clientAccountMappingRepository ?? throw new ArgumentNullException(nameof(clientAccountMappingRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ProcessTransactionCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing transaction {TransactionId}, EventType={EventType}, IdempotencyKey={Key}",
            command.TransactionId, command.EventType, command.IdempotencyKey);

        if (await _processedEventRepository.ExistsAsync(command.IdempotencyKey, cancellationToken))
        {
            _logger.LogWarning("Duplicate event skipped. IdempotencyKey: {Key}", command.IdempotencyKey);
            return;
        }

        var payload = DeserializePayload(command.RawPayload);

        var amount = new Amount(
            payload.Amount.TotalAmount,
            payload.Amount.Currency,
            payload.Amount.GrossAmount,
            payload.Amount.NetAmount,
            payload.Amount.PaymentFee,
            payload.Amount.PlatformFee);

        PaymentMethodDetails? paymentMethod = payload.PaymentMethod is not null
            ? new PaymentMethodDetails(
                payload.PaymentMethod.PaymentMethodId,
                payload.PaymentMethod.ProviderName,
                payload.PaymentMethod.Type)
            : null;

        MerchantDetails? merchant = payload.Merchant is not null
            ? new MerchantDetails(
                payload.Merchant.MerchantId,
                payload.Merchant.MerchantName,
                payload.Merchant.Shop is not null
                    ? new ShopDetails(payload.Merchant.Shop.ShopId, payload.Merchant.Shop.ShopName)
                    : null)
            : null;

        var movement = Movement.Create(
            command.EventType,
            amount,
            payload.TransactionId,
            payload.AccountId,
            payload.Country,
            paymentMethod,
            merchant,
            payload.Description);

        var direction = MovementClassifier.Classify(command.EventType);

        var accountId = payload.AccountId ?? command.ClientId.ToString();

        var balance = await _balanceRepository.GetByAccountAndCurrencyAsync(
            command.ClientId, accountId, amount.Currency, cancellationToken);

        bool isNewBalance = balance is null;
        balance ??= AccountBalanceEntry.Create(command.ClientId, accountId, amount.Currency);

        if (direction == MovementDirection.PayIn)
            balance.ApplyCredit(amount.TotalAmount);
        else
            balance.ApplyDebit(amount.TotalAmount);

        var existingMapping = await _clientAccountMappingRepository
            .GetByClientIdAsync(command.ClientId, cancellationToken);

        await _dbContext.BeginTransactionAsync(cancellationToken);
        try
        {
            await _movementRepository.AddAsync(movement, cancellationToken);

            if (isNewBalance)
                await _balanceRepository.AddAsync(balance, cancellationToken);
            else
                await _balanceRepository.UpdateAsync(balance, cancellationToken);

            if (existingMapping is null)
            {
                var mapping = new ClientAccountMapping(command.ClientId, command.ClientName, accountId, command.UserIds);
                await _clientAccountMappingRepository.AddAsync(mapping, cancellationToken);
            }
            else
            {
                existingMapping.Update(accountId, command.UserIds);
                await _clientAccountMappingRepository.UpdateAsync(existingMapping, cancellationToken);
            }

            await _processedEventRepository.AddAsync(
                new ProcessedEvent(command.IdempotencyKey, command.TransactionId), cancellationToken);

            await _dbContext.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Transaction {TransactionId} processed. Direction={Direction}, Amount={Amount}, Balance={Balance}",
                command.TransactionId, direction, amount, balance.AvailableBalance);
        }
        catch
        {
            await _dbContext.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static MovementPayload DeserializePayload(string rawPayload)
    {
        var payload = JsonSerializer.Deserialize<MovementPayload>(rawPayload, JsonOptions);

        if (payload?.Amount is null)
            throw new InvalidOperationException("Movement payload deserialization failed or Amount is missing.");

        if (string.IsNullOrWhiteSpace(payload.TransactionId))
            throw new InvalidOperationException("Movement payload is missing TransactionId.");

        return payload;
    }
}
