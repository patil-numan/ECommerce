namespace ECommerce.Application.Interfaces;

public interface IImportJobQueue
{
    ValueTask QueueAsync(int importJobId);

    ValueTask<int> DequeueAsync(
        CancellationToken cancellationToken);
}