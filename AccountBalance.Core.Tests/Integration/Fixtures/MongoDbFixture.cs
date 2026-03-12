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
        var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AccountBalance.Core.Worker"));

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        ConnectionString = config["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException(
                "MongoDB connection string not configured. " +
                "Set it in appsettings.Development.json or via the environment variable MongoDb__ConnectionString.");
    }
}
