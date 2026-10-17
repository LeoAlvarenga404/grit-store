using System.Data.Common;
using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Grit.Modules.Catalog.Domain;
using Microsoft.Extensions.Configuration;

namespace Grit.Modules.Catalog.Infrastructure;

// Plain S3 (ADR 0003): today the AppHost's SeaweedFS, which serves buckets as paths and checks AWS signatures (SigV4).
internal sealed class ProductImageStorage(IConfiguration configuration)
{
    public const string ConnectionName = "storage";

    public const string Bucket = "product-images";

    public static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(5);

    // Read URLs are signed on every render, so an hour covers a page view and a leaked URL goes stale.
    public static readonly TimeSpan DownloadLifetime = TimeSpan.FromHours(1);

    // Created on first use: hosts without the storage (most tests, the build-time OpenAPI run) never need it.
    private readonly Lazy<AmazonS3Client> _client = new(() => CreateClient(configuration.GetConnectionString(ConnectionName)));

    public bool IsConfigured => !string.IsNullOrEmpty(configuration.GetConnectionString(ConnectionName));

    // The SDK signs https URLs unless told otherwise; the local storage speaks http.
    private Protocol Scheme => _client.Value.Config.ServiceURL.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTPS : Protocol.HTTP;

    public Task<string> UploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt) =>
        _client.Value.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            // Signed: the upload must declare exactly the type the API allowed.
            ContentType = contentType,
            Expires = expiresAt.UtcDateTime,
            Protocol = Scheme,
        });

    public async Task<IReadOnlyList<string>> DownloadUrlsAsync(IReadOnlyList<string> keys)
    {
        var urls = new List<string>(keys.Count);
        foreach (var key in keys)
        {
            urls.Add(await _client.Value.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = Bucket,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow + DownloadLifetime,
                Protocol = Scheme,
            }));
        }

        return urls;
    }

    // Uploads land here; confirmed images are copied out, so no upload URL can ever overwrite an attached image.
    public static string UploadKeyPrefix(ProductId id) => $"uploads/{id.Value}/";

    public async Task<(long SizeInBytes, string ContentType, string ETag)?> FindAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _client.Value.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = Bucket, Key = key }, cancellationToken);
            return (metadata.ContentLength, metadata.Headers.ContentType, metadata.ETag);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // The ETag condition closes the gap between the check and the copy: an upload replaced meanwhile is not copied.
    public async Task<bool> CopyIfUnchangedAsync(string fromKey, string toKey, string eTag, CancellationToken cancellationToken)
    {
        try
        {
            await _client.Value.CopyObjectAsync(
                new CopyObjectRequest { SourceBucket = Bucket, SourceKey = fromKey, DestinationBucket = Bucket, DestinationKey = toKey, ETagToMatch = eTag },
                cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        _client.Value.DeleteObjectAsync(Bucket, key, cancellationToken);

    public async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.Value.PutBucketAsync(new PutBucketRequest { BucketName = Bucket }, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "BucketAlreadyOwnedByYou")
        {
            // Created by an earlier start: provisioning is idempotent.
        }
    }

    private static AmazonS3Client CreateClient(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"ConnectionStrings:{ConnectionName} is missing: product images need the S3 storage (the AppHost's '{ConnectionName}' resource).");
        }

        // The Aspire SeaweedFS reference: "Endpoint=http://host:port;AccessKey=...;SecretKey=...".
        var settings = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var config = new AmazonS3Config
        {
            ServiceURL = (string)settings["Endpoint"],
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
            // AWS SDK 4 adds checksums by default, which S3-compatible servers do not all accept.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        return new AmazonS3Client(new BasicAWSCredentials((string)settings["AccessKey"], (string)settings["SecretKey"]), config);
    }
}
