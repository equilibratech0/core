namespace AccountBalance.Core.Infrastructure.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence.Extensions;
using Shared.Infrastructure.Messaging.Extensions;
using AccountBalance.Core.Application.Handlers;
using AccountBalance.Core.Application.Interfaces;
using AccountBalance.Core.Domain.Repositories;
using AccountBalance.Core.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoInfrastructure(configuration);
        services.AddAzureServiceBusInfrastructure(configuration);

        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IAccountBalanceRepository, AccountBalanceRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
        services.AddScoped<IClientAccountMappingRepository, ClientAccountMappingRepository>();

        return services;
    }

    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        services.AddScoped<IProcessTransactionHandler, ProcessTransactionHandler>();
        return services;
    }
}
