using Amazon.Runtime;
using Amazon.S3;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Infrastructure.BackgroundServices;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Connection string 'PostgreSQL' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        var storageSettings = configuration
            .GetSection(ObjectStorageSettings.SectionName)
            .Get<ObjectStorageSettings>();

        if (storageSettings is not null &&
            !string.IsNullOrWhiteSpace(storageSettings.ServiceUrl) &&
            !string.IsNullOrWhiteSpace(storageSettings.BucketName) &&
            !string.IsNullOrWhiteSpace(storageSettings.AccessKey) &&
            !string.IsNullOrWhiteSpace(storageSettings.SecretKey))
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = storageSettings.ServiceUrl,
                    ForcePathStyle = true,
                    AuthenticationRegion = storageSettings.Region
                };

                var credentials = new BasicAWSCredentials(
                    storageSettings.AccessKey,
                    storageSettings.SecretKey);

                return new AmazonS3Client(credentials, config);
            });

            services.AddSingleton<IObjectStorageService>(provider =>
                new S3ObjectStorageService(
                    provider.GetRequiredService<IAmazonS3>(),
                    storageSettings.BucketName));
        }

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<ITokenService, JwtTokenService>();
        services.Configure<LiveKitSettings>(configuration.GetSection(LiveKitSettings.SectionName));
        services.AddScoped<ILiveKitTokenService, LiveKitTokenService>();
        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IStorageDeletionOutboxRepository, StorageDeletionOutboxRepository>();

        services.AddHostedService<StorageDeletionOutboxWorker>();

        return services;
    }

}
