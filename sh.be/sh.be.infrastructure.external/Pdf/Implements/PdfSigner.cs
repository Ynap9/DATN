using sh.be.applications.Base.Dtos.Signing;
using sh.be.applications.Base.Interfaces;
using sh.be.infrastructure.external.Pdf.Base;
using sh.be.infrastructure.external.Signing.Base;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace sh.be.infrastructure.external.Pdf.Implements
{
    public class PdfSigner : IPdfSigner
    {
        private readonly PdfPreparer _pdfPreparer = new(new PdfAppearanceBuilder(new AppearanceFontResolver()));

        public PdfSigningContextDto CreateContext(X509Certificate2 certificate, PdfSigningOptionsDto options)
        {
            return new PdfSigningContextDto
            {
                Certificate = certificate,
                Chain = BuildChain(certificate),
                SignerName = certificate.GetNameInfo(X509NameType.SimpleName, false),
                SigningCertificateV2 = CmsBuilder.BuildSigningCertificateV2(certificate),
                Options = options,
            };
        }

        public bool HasSignature(byte[] pdf)
        {
            return PdfSignatureReader.HasSignature(pdf);
        }

        public PdfPreparedSignatureDto Prepare(byte[] pdf, PdfSigningContextDto context, DateTime signedAt, DateTime signedAtUtc)
        {
            var prepared = _pdfPreparer.Prepare(pdf, context, signedAt);
            return new PdfPreparedSignatureDto
            {
                Bytes = prepared.Bytes,
                ContentsHexStart = prepared.ContentsHexStart,
                ContentsHexLength = prepared.ContentsHexLength,
                SignedAttributes = CmsBuilder.BuildSignedAttributes(SHA256.HashData(prepared.SignedContent), context.SigningCertificateV2, signedAtUtc),
                SignedAt = signedAt,
            };
        }

        public byte[] Complete(PdfPreparedSignatureDto prepared, byte[] signature, byte[] timestampToken, PdfSigningContextDto context)
        {
            var cms = CmsBuilder.Assemble(prepared.SignedAttributes, signature, context.Certificate, context.Chain, timestampToken);
            var hex = Convert.ToHexString(cms);
            if (hex.Length > prepared.ContentsHexLength)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignatureTooLarge);
            }

            var output = prepared.Bytes.ToArray();
            Encoding.Latin1.GetBytes(hex).CopyTo(output, prepared.ContentsHexStart);
            EnsureValid(output);
            return output;
        }

        private static void EnsureValid(byte[] signedPdf)
        {
            var signature = PdfSignatureReader.ReadLast(signedPdf);
            if (signature == null || !signature.CoversWholeFile)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignatureInvalid);
            }

            try
            {
                var cms = new SignedCms(new ContentInfo(signature.SignedContent), true);
                cms.Decode(signature.Cms);
                cms.CheckSignature(true);
                var token = cms.SignerInfos[0].UnsignedAttributes
                    .Cast<CryptographicAttributeObject>()
                    .FirstOrDefault(a => a.Oid.Value == PdfSigningConst.OID_SIGNATURE_TIMESTAMP_TOKEN)
                    ?.Values.Cast<AsnEncodedData>().FirstOrDefault()?.RawData;
                if (token == null || !Rfc3161TimestampToken.TryDecode(token, out _, out _))
                {
                    throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignatureInvalid);
                }
            }
            catch (CryptographicException)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignatureInvalid);
            }
        }

        private static List<X509Certificate2> BuildChain(X509Certificate2 certificate)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
            return chain.Build(certificate) ? chain.ChainElements.Select(e => e.Certificate).ToList() : [certificate];
        }
    }
}
