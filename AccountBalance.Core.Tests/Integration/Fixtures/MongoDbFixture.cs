namespace AccountBalance.Core.Tests.Integration.Fixtures;

using Microsoft.Extensions.Configuration;

[CollectionDefinition(Name)]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
{
    public const string Name = "MongoDb";
}

public class MongoDbFixture
{
    public string ConnectionString { get; }

    public MongoDbFixture()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Integration.json", optional: true)
            .AddJsonFile("appsettings.Integration.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        ConnectionString = config["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException(
                "MongoDB connection string not configured. " +
                "Set it in appsettings.Integration.local.json or via the environment variable MongoDb__ConnectionString.");
    }
}
