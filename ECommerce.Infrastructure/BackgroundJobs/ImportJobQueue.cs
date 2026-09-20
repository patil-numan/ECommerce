using System.Threading.Channels;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.BackgroundJobs;

public class ImportJobQueue : IImportJobQueue
{
    private readonly Channel<int> _queue;

    public ImportJobQueue(
        IConfiguration configuration)
    {
        var queueCapacity =
            configuration.GetValue<int>(
                "BulkImport:QueueCapacity");

        if (queueCapacity <= 0)
        {
            queueCapacity = 100;
        }

        var options =
            new BoundedChannelOptions(queueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

        _queue =
            Channel.CreateBounded<int>(options);
    }

    public async ValueTask QueueAsync(
        int importJobId)
    {
        await _queue.Writer.WriteAsync(
            importJobId);
    }

    public async ValueTask<int> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(
            cancellationToken);
    }
}