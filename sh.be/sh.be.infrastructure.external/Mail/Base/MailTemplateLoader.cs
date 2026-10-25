namespace sh.be.infrastructure.external.Mail.Base
{
    public static class MailTemplateLoader
    {
        public static string Load(Type templateType, string fileName)
        {
            var resourceName = $"{templateType.Namespace}.{fileName}";
            using var stream = templateType.Assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException(resourceName);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
