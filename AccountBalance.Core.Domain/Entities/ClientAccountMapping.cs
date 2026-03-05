namespace AccountBalance.Core.Domain.Entities;

using System;
using System.Collections.Generic;

public class ClientAccountMapping
{
    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string ClientName { get; private set; } = null!;
    public string AccountId { get; private set; } = null!;
    public IReadOnlyList<string> UserIds { get; private set; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ClientAccountMapping() { }

    public ClientAccountMapping(Guid clientId, string clientName, string accountId, IReadOnlyList<string> userIds)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        ClientName = clientName ?? throw new ArgumentNullException(nameof(clientName));
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        UserIds = userIds ?? Array.Empty<string>();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string accountId, IReadOnlyList<string> userIds)
    {
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        UserIds = userIds ?? Array.Empty<string>();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
