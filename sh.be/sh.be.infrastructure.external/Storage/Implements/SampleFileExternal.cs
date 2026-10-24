using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.Storage;

namespace sh.be.infrastructure.external.Storage.Implements
{
    public class SampleFileExternal : ISampleFileExternal
    {
        private const string SigningSampleFileName = "signing-sample.pdf";
        private const string PdfContentType = "application/pdf";
        private const string FilesNamespace = "sh.be.infrastructure.external.Storage.Files";

        private static readonly Lazy<byte[]> SigningSampleContent = new(() => ReadResource(SigningSampleFileName));

        public ExportFileDto ReadSigningSampleFile()
        {
            return new ExportFileDto
            {
                FileName = SigningSampleFileName,
                ContentType = PdfContentType,
                Content = SigningSampleContent.Value,
            };
        }

        private static byte[] ReadResource(string fileName)
        {
            var resourceName = $"{FilesNamespace}.{fileName}";
            using var stream = typeof(SampleFileExternal).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException(resourceName);
            using var content = new MemoryStream();
            stream.CopyTo(content);
            return content.ToArray();
        }
    }
}
