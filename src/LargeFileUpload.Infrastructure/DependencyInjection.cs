using LargeFileUpload.Application.Common.Interfaces;
using LargeFileUpload.Infrastructure.Persistence;
using LargeFileUpload.Infrastructure.Services;
using LargeFileUpload.Infrastructure.Storage;
using LargeFileUpload.Infrastructure.Storage.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFileUpload.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Persistence (EF Core & SQL Server)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<UploadDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(UploadDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<UploadDbContext>());

        // 2. Azure Storage & SAS Generation
        services.Configure<AzureStorageOptions>(configuration.GetSection(AzureStorageOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<BlobServiceClientFactory>();
        services.AddScoped<IUserDelegationSasService, UserDelegationSasService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        // 3. Infrastructure Common Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHostedService<ExpiredUploadCleanupBackgroundService>();

        return services;
    }
}
