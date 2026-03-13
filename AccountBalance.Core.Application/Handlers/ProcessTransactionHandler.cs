namespace AccountBalance.Core.Application.Handlers;

using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly ICompanyAccountMappingRepository _companyAccountMappingRepository;
    private readonly IUserAccountAssignmentRepository _userAccountAssignmentRepository;
    private readonly IAccountProvisioningRepository _accountProvisioningRepository;
    private readonly IMongoDbContext _dbContext;
    private readonly ILogger<ProcessTransactionHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ProcessTransactionHandler(
        IMovementRepository movementRepository,
        IAccountBalanceRepository balanceRepository,
        IProcessedEventRepository processedEventRepository,
        ICompanyAccountMappingRepository companyAccountMappingRepository,
        IUserAccountAssignmentRepository userAccountAssignmentRepository,
        IAccountProvisioningRepository accountProvisioningRepository,
        IMongoDbContext dbContext,
        ILogger<ProcessTransactionHandler> logger)
    {
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _balanceRepository = balanceRepository ?? throw new ArgumentNullException(nameof(balanceRepository));
        _processedEventRepository = processedEventRepository ?? throw new ArgumentNullException(nameof(processedEventRepository));
        _companyAccountMappingRepository = companyAccountMappingRepository ?? throw new ArgumentNullException(nameof(companyAccountMappingRepository));
        _userAccountAssignmentRepository = userAccountAssignmentRepository ?? throw new ArgumentNullException(nameof(userAccountAssignmentRepository));
        _accountProvisioningRepository = accountProvisioningRepository ?? throw new ArgumentNullException(nameof(accountProvisioningRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ProcessTransactionCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing transaction {TransactionId}, CompanyId={CompanyId}, EventType={EventType}",
            command.TransactionId, command.CompanyId, command.EventType);

        if (await _processedEventRepository.ExistsAsync(command.TransactionId, command.EventType, cancellationToken))
        {
            _logger.LogWarning("Duplicate event skipped. TransactionId: {TransactionId}, EventType: {EventType}",
                command.TransactionId, command.EventType);
            return;
        }

        var payload = DeserializePayload(command.RawPayload);

        var currency = payload.Account?.Currency
            ?? throw new InvalidOperationException("Account currency is required.");

        var accountReference = payload.Account?.AccountId
            ?? throw new InvalidOperationException("Account ID is required.");

        var amount = new Amount(
            payload.Amount.TotalAmount,
            currency,
            payload.Amount.GrossAmount,
            payload.Amount.NetAmount,
            payload.Amount.PaymentFee);

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
            accountReference,
            payload.Country,
            paymentMethod,
            merchant,
            payload.Description);

        var account = await _accountProvisioningRepository.GetByReferenceAsync(
            command.CompanyId, accountReference, cancellationToken);

        if (account is null)
        {
            account = new Account(command.CompanyId, accountReference, accountReference);
            await _accountProvisioningRepository.AddAsync(account, cancellationToken);
        }

        var direction = MovementClassifier.Classify(command.EventType);

        var balance = await _balanceRepository.GetByAccountAsync(
            command.CompanyId, account.Id, amount.Currency, cancellationToken)
            ?? AccountBalanceEntry.Create(command.CompanyId, account.Id, amount.Currency);

        if (direction == MovementDirection.PayIn)
            balance.AddBalance(amount.TotalAmount);
        else
            balance.SubtractBalance(amount.TotalAmount);

        await _dbContext.BeginTransactionAsync(cancellationToken);
        try
        {
            await _movementRepository.AddAsync(movement, cancellationToken);

            await _balanceRepository.UpsertAsync(balance, cancellationToken);

            await _processedEventRepository.AddAsync(
                new ProcessedEvent(command.TransactionId, command.EventType), cancellationToken);

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

        await _companyAccountMappingRepository.UpsertAsync(
            new CompanyAccountMapping(command.CompanyId, account.Id), cancellationToken);

        await _userAccountAssignmentRepository.AssignAccountToAdminUsersAsync(
            command.CompanyId, account.Id, cancellationToken);
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
