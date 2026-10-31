using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public sealed class StorageDeletionOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StorageDeletionOutboxWorker> _logger;

    public StorageDeletionOutboxWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<StorageDeletionOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessPendingAsync(stoppingToken);

                if (!processedAny)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while processing storage deletion outbox.");

                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessPendingAsync(
        CancellationToken cancellationToken)
    {
        var processedAny = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var repository = scope.ServiceProvider
                .GetRequiredService<IStorageDeletionOutboxRepository>();

            var storage = scope.ServiceProvider
                .GetRequiredService<IObjectStorageService>();

            var now = DateTime.UtcNow;

            var message = await repository.TryClaimNextAsync(
                now,
                now.AddMinutes(2),
                cancellationToken);

            if (message is null)
                break;

            processedAny = true;

            try
            {
                await storage.DeleteAsync(
                    message.StorageKey,
                    cancellationToken);

                await repository.MarkAsProcessedAsync(
                    message.Id,
                    DateTime.UtcNow,
                    cancellationToken);

                _logger.LogInformation(
                    "Storage object {StorageKey} was deleted.",
                    message.StorageKey);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var retryDelay = TimeSpan.FromMinutes(
                    Math.Min(Math.Pow(2, message.Attempts - 1), 60));

                await repository.MarkAsFailedAsync(
                    message.Id,
                    DateTime.UtcNow.Add(retryDelay),
                    exception.Message,
                    cancellationToken);

                _logger.LogError(
                    exception,
                    "Failed to delete storage object {StorageKey}. Attempt: {Attempt}.",
                    message.StorageKey,
                    message.Attempts);
            }
        }

        return processedAny;
    }
}