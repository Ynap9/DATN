using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;

namespace sh.be.infrastructure.external.Storage.Implements
{
    public class S3FileStorageExternal : IFileStorageExternal
    {
        private readonly S3Settings _s3Settings;
        private readonly ILogger<S3FileStorageExternal> _logger;
        private readonly Lazy<AmazonS3Client> _client;

        public S3FileStorageExternal(IOptions<S3Settings> s3Settings, ILogger<S3FileStorageExternal> logger)
        {
            _s3Settings = s3Settings.Value;
            _logger = logger;
            _client = new Lazy<AmazonS3Client>(CreateClient);
        }

        public async Task<string> UploadAsync(Stream content, string objectKey, string contentType)
        {
            _logger.LogInformation($"{nameof(UploadAsync)}, objectKey={objectKey}");
            await PutAsync(content, objectKey, contentType, S3CannedACL.PublicRead, CancellationToken.None);
            return $"{_s3Settings.Url.TrimEnd('/')}/{_s3Settings.Bucket}/{objectKey}";
        }

        public Task UploadPrivateAsync(Stream content, string objectKey, string contentType, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(UploadPrivateAsync)}, objectKey={objectKey}");
            return PutAsync(content, objectKey, contentType, S3CannedACL.Private, cancellationToken);
        }

        public async Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _client.Value.GetObjectAsync(_s3Settings.Bucket, objectKey, cancellationToken);
                using var buffer = new MemoryStream(response.ContentLength > 0 ? (int)response.ContentLength : 0);
                await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
                return buffer.ToArray();
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, $"{nameof(DownloadAsync)}, objectKey={objectKey}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorDownloadFileFailed);
            }
        }

        public string GetTemporaryUrl(string objectKey, string fileName, TimeSpan lifetime)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Settings.Bucket,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(lifetime),
                Protocol = _s3Settings.Url.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTPS : Protocol.HTTP,
            };
            request.ResponseHeaderOverrides.ContentDisposition = $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
            return _client.Value.GetPreSignedURL(request);
        }

        private async Task PutAsync(Stream content, string objectKey, string contentType, S3CannedACL cannedAcl, CancellationToken cancellationToken)
        {
            try
            {
                await _client.Value.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _s3Settings.Bucket,
                    Key = objectKey,
                    InputStream = content,
                    ContentType = contentType,
                    CannedACL = cannedAcl,
                    UseChunkEncoding = false,
                    DisableDefaultChecksumValidation = true,
                }, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, $"{nameof(PutAsync)}, objectKey={objectKey}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorUploadFileFailed);
            }
        }

        public async Task DeleteAsync(string objectKey)
        {
            _logger.LogInformation($"{nameof(DeleteAsync)}, objectKey={objectKey}");
            try
            {
                await _client.Value.DeleteObjectAsync(_s3Settings.Bucket, objectKey);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(DeleteAsync)}, objectKey={objectKey}");
            }
        }

        public async Task DeleteByPrefixAsync(string objectKeyPrefix)
        {
            _logger.LogInformation($"{nameof(DeleteByPrefixAsync)}, objectKeyPrefix={objectKeyPrefix}");
            try
            {
                string? continuationToken = null;
                do
                {
                    var listedObjects = await _client.Value.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = _s3Settings.Bucket,
                        Prefix = objectKeyPrefix,
                        ContinuationToken = continuationToken,
                    });
                    foreach (var s3Object in listedObjects.S3Objects ?? [])
                    {
                        await _client.Value.DeleteObjectAsync(_s3Settings.Bucket, s3Object.Key);
                    }
                    continuationToken = listedObjects.IsTruncated == true ? listedObjects.NextContinuationToken : null;
                }
                while (continuationToken != null);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(DeleteByPrefixAsync)}, objectKeyPrefix={objectKeyPrefix}");
            }
        }

        private AmazonS3Client CreateClient()
        {
            if (string.IsNullOrWhiteSpace(_s3Settings.Url) || string.IsNullOrWhiteSpace(_s3Settings.Bucket))
            {
                throw new UserFriendlyException(ErrorCodes.CoreErrorStorageNotConfigured);
            }
            return new AmazonS3Client(_s3Settings.AccessKey, _s3Settings.SecretKey, new AmazonS3Config
            {
                ServiceURL = _s3Settings.Url,
                ForcePathStyle = true,
                AuthenticationRegion = _s3Settings.Region,
            });
        }
    }
}
