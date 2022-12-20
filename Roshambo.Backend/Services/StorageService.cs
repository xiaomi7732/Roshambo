using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace Roshambo.Services;

internal interface IStorageService
{
    Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken);
    Stream OpenWrite(string blobName, CancellationToken cancellationToken);
}

internal class StorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;
    public StorageService(IOptions<StorageOptions> storageOptions, ILogger<StorageService> logger)
    {
        StorageOptions option = storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));
        string connectionString = option.ConnectionString;
        BlobServiceClient client = new BlobServiceClient(connectionString);

        logger.LogInformation("Ensure container exist: {0}", option.ContainerName);
        _containerClient = client.GetBlobContainerClient(option.ContainerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(blobName))
        {
            throw new ArgumentException($"'{nameof(blobName)}' cannot be null or empty.", nameof(blobName));
        }

        BlobClient blobClient = _containerClient.GetBlobClient(blobName);
        if(!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }
        return blobClient.OpenRead(null, cancellationToken);
    }

    public Stream OpenWrite(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(blobName))
        {
            throw new ArgumentException($"'{nameof(blobName)}' cannot be null or empty.", nameof(blobName));
        }

        BlobClient blobClient = _containerClient.GetBlobClient(blobName);
        return blobClient.OpenWrite(overwrite: true, options: null, cancellationToken);
    }
}

internal class StorageOptions
{
    public string ConnectionString { get; set; } = default!;
    public string ContainerName { get; set; } = default!;
}