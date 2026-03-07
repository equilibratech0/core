namespace AccountBalance.Core.Application.DTOs;

using Shared.Domain.Enums;

public class MovementPayload
{
    public AmountPayload Amount { get; set; } = null!;
    public string TransactionId { get; set; } = null!;
    public AccountPayload? Account { get; set; }
    public string? Country { get; set; }
    public PaymentMethodPayload? PaymentMethod { get; set; }
    public MerchantPayload? Merchant { get; set; }
    public string? Description { get; set; }
}

public class AmountPayload
{
    public decimal TotalAmount { get; set; }
    public decimal? GrossAmount { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal? PaymentFee { get; set; }
    public decimal? PlatformFee { get; set; }
}

public class AccountPayload
{
    public string? AccountId { get; set; }
    public string? AccountName { get; set; }
    public Currency? Currency { get; set; }
}

public class PaymentMethodPayload
{
    public string? PaymentMethodId { get; set; }
    public string? ProviderName { get; set; }
    public PaymentMethodType? Type { get; set; }
}

public class MerchantPayload
{
    public string? MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public ShopPayload? Shop { get; set; }
}

public class ShopPayload
{
    public string? ShopId { get; set; }
    public string? ShopName { get; set; }
}
