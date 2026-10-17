namespace sh.be.applications.Base.Interfaces
{
    public interface IFileStorage
    {
        Task<string> UploadAsync(Stream content, string objectKey, string contentType);
        Task UploadPrivateAsync(Stream content, string objectKey, string contentType, CancellationToken cancellationToken = default);
        Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);
        string GetTemporaryUrl(string objectKey, string fileName, TimeSpan lifetime);
        Task DeleteAsync(string objectKey);
        Task DeleteByPrefixAsync(string objectKeyPrefix);
    }
}
