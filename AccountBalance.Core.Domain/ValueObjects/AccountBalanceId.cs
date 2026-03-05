namespace AccountBalance.Core.Domain.ValueObjects;

using Shared.Domain.Types;

public record AccountBalanceId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static AccountBalanceId New() => new(Guid.NewGuid());
}
