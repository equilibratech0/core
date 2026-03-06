namespace AccountBalance.Core.Infrastructure.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence.Abstractions;
using Shared.Infrastructure.Persistence.Mongo;
using Shared.Infrastructure.Messaging.AzureServiceBus;
using AccountBalance.Core.Application.Handlers;
using AccountBalance.Core.Application.Interfaces;
using AccountBalance.Core.Domain.Repositories;
using AccountBalance.Core.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.AddScoped<IMongoDbContext, MongoDbContext>();

        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IAccountBalanceRepository, AccountBalanceRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
        services.AddScoped<IClientAccountMappingRepository, ClientAccountMappingRepository>();

        services.Configure<AzureServiceBusOptions>(configuration.GetSection(AzureServiceBusOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        services.AddScoped<IProcessTransactionHandler, ProcessTransactionHandler>();
        return services;
    }
}
