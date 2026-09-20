using ECommerce.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs;

public class ImportJobBackgroundService : BackgroundService
{
    private const int MaxRetryAttempts = 3;

    private readonly IImportJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportJobBackgroundService> _logger;

    public ImportJobBackgroundService(
        IImportJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ImportJobBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Import job background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var importJobId =
                    await _queue.DequeueAsync(
                        stoppingToken);

                _logger.LogInformation(
                    "Import job {ImportJobId} received from queue.",
                    importJobId);

                await ProcessImportJobWithRetryAsync(
                    importJobId,
                    stoppingToken);
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
                    "An error occurred while processing an import job.");
            }
        }

        _logger.LogInformation(
            "Import job background service stopped.");
    }

    private async Task ProcessImportJobWithRetryAsync(
        int importJobId,
        CancellationToken cancellationToken)
    {
        for (
            var attempt = 1;
            attempt <= MaxRetryAttempts;
            attempt++)
        {
            try
            {
                await ProcessImportJobAsync(
                    importJobId,
                    cancellationToken);

                return;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Import job {ImportJobId} failed validation. No retry will be attempted.",
                    importJobId);

                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Import job {ImportJobId} failed on attempt {Attempt}/{MaxAttempts}.",
                    importJobId,
                    attempt,
                    MaxRetryAttempts);

                if (attempt == MaxRetryAttempts)
                {
                    _logger.LogError(
                        "Import job {ImportJobId} failed after {MaxAttempts} attempts.",
                        importJobId,
                        MaxRetryAttempts);

                    return;
                }

                var retryCount =
                    attempt;

                await UpdateRetryCountAsync(
                    importJobId,
                    retryCount);

                var delaySeconds =
                    Math.Pow(2, attempt);

                _logger.LogWarning(
                    "Retrying import job {ImportJobId} in {DelaySeconds} seconds.",
                    importJobId,
                    delaySeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken);
            }
        }
    }

    private async Task ProcessImportJobAsync(
        int importJobId,
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var importJobRepository =
            scope.ServiceProvider
                .GetRequiredService<IImportJobRepository>();

        var importService =
            scope.ServiceProvider
                .GetRequiredService<IBulkProductImportService>();

        var importJob =
            await importJobRepository.GetByIdAsync(
                importJobId);

        if (importJob is null)
        {
            _logger.LogWarning(
                "Import job {ImportJobId} was not found.",
                importJobId);

            return;
        }

        _logger.LogInformation(
            "Processing import job {ImportJobId}.",
            importJobId);

        await importService.ProcessAsync(
            importJob,
            cancellationToken);
    }

    private async Task UpdateRetryCountAsync(
        int importJobId,
        int retryCount)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var importJobRepository =
            scope.ServiceProvider
                .GetRequiredService<IImportJobRepository>();

        var importJob =
            await importJobRepository.GetByIdAsync(
                importJobId);

        if (importJob is null)
        {
            _logger.LogWarning(
                "Unable to update retry count. Import job {ImportJobId} was not found.",
                importJobId);

            return;
        }

        importJob.RetryCount =
            retryCount;

        await importJobRepository.UpdateAsync(
            importJob);
    }
}