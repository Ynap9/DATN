using Microsoft.Extensions.Logging;
using PdfSharp.Pdf.IO;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.infrastructure.external.DigitalSignature.Base;
using sh.be.shared.Constants.Packaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.DataReader.Implements
{
    public class PackageFileInspectExternal : IPackageFileInspectExternal
    {
        private readonly ILogger<PackageFileInspectExternal> _logger;

        public PackageFileInspectExternal(ILogger<PackageFileInspectExternal> logger)
        {
            _logger = logger;
        }

        public async Task<PackageFileInspectDto> InspectAsync(Stream content, bool isPdf)
        {
            if (!isPdf)
            {
                return new PackageFileInspectDto { Sha256 = Convert.ToHexString(await SHA256.HashDataAsync(content)) };
            }

            using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var text = Encoding.Latin1.GetString(bytes);
            var part = Regex.Match(text, PackageSessionConst.PDFA_PART_PATTERN);
            var conformance = Regex.Match(text, PackageSessionConst.PDFA_CONFORMANCE_PATTERN);

            int? pageCount = null;
            try
            {
                memoryStream.Position = 0;
                using var document = PdfReader.Open(memoryStream, PdfDocumentOpenMode.Import);
                pageCount = document.PageCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(InspectAsync)}, size={bytes.Length}");
            }

            return new PackageFileInspectDto
            {
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                IsSigned = PdfSignatureReader.HasSignature(bytes),
                PdfAPart = part.Success
                    ? $"{part.Groups[1].Value}{part.Groups[2].Value}{conformance.Groups[1].Value}{conformance.Groups[2].Value}".ToUpperInvariant()
                    : null,
                PageCount = pageCount,
            };
        }
    }
}
