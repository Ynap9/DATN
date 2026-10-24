using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Dtos.Storage;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.external.Storage.Implements
{
    public class HttpFileDownloadExternal : IFileDownloadExternal
    {
        private const string PdfContentType = "application/pdf";
        private static readonly string[] AcceptedPdfContentTypes = [PdfContentType, "application/octet-stream"];

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpFileDownloadExternal> _logger;

        public HttpFileDownloadExternal(IHttpClientFactory httpClientFactory, ILogger<HttpFileDownloadExternal> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<FileStreamDto> OpenPdfAsync(string fileUrl, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(OpenPdfAsync)}, fileUrl={fileUrl}");
            var fileUri = ToHttpUri(fileUrl);
            var response = await SendAsync(fileUri, cancellationToken);

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (!response.IsSuccessStatusCode || !AcceptedPdfContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError($"{nameof(OpenPdfAsync)}, fileUrl={fileUrl}, statusCode={response.StatusCode}, contentType={contentType}");
                response.Dispose();
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorFileUnavailable);
            }

            return new FileStreamDto
            {
                Content = await response.Content.ReadAsStreamAsync(cancellationToken),
                ContentType = PdfContentType,
                FileName = Path.GetFileName(fileUri.LocalPath),
            };
        }

        private static Uri ToHttpUri(string fileUrl)
        {
            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var fileUri) || (fileUri.Scheme != Uri.UriSchemeHttp && fileUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorFileUnavailable);
            }
            return fileUri;
        }

        private async Task<HttpResponseMessage> SendAsync(Uri fileUri, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                return await httpClient.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                _logger.LogError(ex, $"{nameof(SendAsync)}, fileUri={fileUri}");
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorFileUnavailable);
            }
        }
    }
}
