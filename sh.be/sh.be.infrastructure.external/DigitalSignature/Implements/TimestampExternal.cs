using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Interfaces.External.DigitalSignature;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

namespace sh.be.infrastructure.external.DigitalSignature.Implements
{
    public class TimestampExternal : ITimestampExternal
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TimestampExternal> _logger;

        public TimestampExternal(IHttpClientFactory httpClientFactory, ILogger<TimestampExternal> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<byte[]> RequestTokenAsync(byte[] signature, CancellationToken cancellationToken)
        {
            var hash = SHA256.HashData(signature);
            var delay = TimeSpan.FromSeconds(TimestampConst.RETRY_DELAY_SECONDS);
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await RequestOnceAsync(hash, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, $"{nameof(RequestTokenAsync)}, attempt={attempt}");
                    if (attempt >= TimestampConst.MAX_ATTEMPTS)
                    {
                        throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTimestampFailed);
                    }
                }
                await Task.Delay(delay, cancellationToken);
                delay += delay;
            }
        }

        public DateTime? ReadTimeUtc(byte[] token)
        {
            return Rfc3161TimestampToken.TryDecode(token, out var decoded, out _) ? decoded!.TokenInfo.Timestamp.UtcDateTime : null;
        }

        private async Task<byte[]> RequestOnceAsync(byte[] hash, CancellationToken cancellationToken)
        {
            var request = Rfc3161TimestampRequest.CreateFromHash(hash, HashAlgorithmName.SHA256, nonce: RandomNumberGenerator.GetBytes(8), requestSignerCertificates: true);
            using var content = new ByteArrayContent(request.Encode());
            content.Headers.ContentType = new MediaTypeHeaderValue(TimestampConst.CONTENT_TYPE);

            var client = _httpClientFactory.CreateClient(TimestampConst.HTTP_CLIENT_NAME);
            using var response = await client.PostAsync(TimestampConst.URL, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var reply = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return request.ProcessResponse(reply, out _).AsSignedCms().Encode();
        }
    }
}
