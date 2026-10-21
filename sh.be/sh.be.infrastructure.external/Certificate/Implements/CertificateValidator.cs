using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Interfaces;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace sh.be.infrastructure.external.Certificate.Implements
{
    public class CertificateValidator : ICertificateValidator
    {
        private readonly ILogger<CertificateValidator> _logger;
        private readonly X509Certificate2? _rootG1;
        private readonly X509Certificate2? _subCaG1;
        private readonly X509Certificate2? _rootG2;
        private readonly X509Certificate2Collection _intermediateCas = new();

        public CertificateValidator(ILogger<CertificateValidator> logger)
        {
            _logger = logger;
            _rootG1 = LoadPinned(CertificateConst.ROOT_G1_FILE_NAME, CertificateConst.ROOT_G1_SHA256);
            _subCaG1 = LoadPinned(CertificateConst.SUB_CA_G1_FILE_NAME, CertificateConst.SUB_CA_G1_SHA256);
            _rootG2 = LoadPinned(CertificateConst.ROOT_G2_FILE_NAME, CertificateConst.ROOT_G2_SHA256);
            foreach (var (fileName, sha256) in CertificateConst.INTERMEDIATE_CA_PINS)
            {
                var ca = LoadPinned(fileName, sha256);
                if (ca != null)
                {
                    _intermediateCas.Add(ca);
                }
            }
            _logger.LogInformation($"{nameof(CertificateValidator)}, rootG1={_rootG1 != null}, subCaG1={_subCaG1 != null}, rootG2={_rootG2 != null}, intermediateCas={_intermediateCas.Count}");
        }

        public X509Certificate2 LoadTrusted(string base64, DateTime verificationTimeUtc)
        {
            var certificate = Load(base64);
            if (!IsTrusted(certificate, verificationTimeUtc))
            {
                certificate.Dispose();
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorUntrustedCertificate);
            }
            return certificate;
        }

        public bool IsTrusted(X509Certificate2 certificate, DateTime verificationTimeUtc)
        {
            if (_rootG1 != null && _subCaG1 != null && ChainsToRoot(certificate, _rootG1, [_subCaG1], verificationTimeUtc))
            {
                return true;
            }
            return _rootG2 != null && ChainsToRoot(certificate, _rootG2, [], verificationTimeUtc);
        }

        private static X509Certificate2 Load(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorInvalidCertificate);
            }
            try
            {
                return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64.Trim()));
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorInvalidCertificate);
            }
        }

        private bool ChainsToRoot(X509Certificate2 certificate, X509Certificate2 root, X509Certificate2[] extra, DateTime verificationTimeUtc)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(root);
            chain.ChainPolicy.ExtraStore.AddRange(extra);
            chain.ChainPolicy.ExtraStore.AddRange(_intermediateCas);
            chain.ChainPolicy.VerificationTime = verificationTimeUtc;
            if (!chain.Build(certificate))
            {
                return false;
            }
            return string.Equals(chain.ChainElements[^1].Certificate.Thumbprint, root.Thumbprint, StringComparison.OrdinalIgnoreCase);
        }

        private X509Certificate2? LoadPinned(string fileName, string sha256)
        {
            var path = Path.Combine(AppContext.BaseDirectory, CertificateConst.CERTIFICATE_FOLDER, fileName);
            if (!File.Exists(path))
            {
                _logger.LogWarning($"{nameof(LoadPinned)}, missing file {fileName}");
                return null;
            }
            try
            {
                var text = File.ReadAllText(path);
                var certificate = text.Contains("BEGIN CERTIFICATE")
                    ? X509Certificate2.CreateFromPem(text)
                    : X509CertificateLoader.LoadCertificateFromFile(path);
                if (string.Equals(certificate.GetCertHashString(HashAlgorithmName.SHA256), sha256, StringComparison.OrdinalIgnoreCase))
                {
                    return certificate;
                }
                _logger.LogWarning($"{nameof(LoadPinned)}, pin mismatch {fileName}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(LoadPinned)}, cannot load {fileName}");
                return null;
            }
        }
    }
}
