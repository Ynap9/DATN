using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace sh.be.infrastructure.external.Signing.Base
{
    public static class CmsBuilder
    {
        private const byte TagSetOf = 0x31;
        private const byte TagContext0 = 0xA0;
        private static readonly Asn1Tag Context0 = new(TagClass.ContextSpecific, 0);
        private static readonly Asn1Tag Context1 = new(TagClass.ContextSpecific, 1);

        public static byte[] BuildSigningCertificateV2(X509Certificate2 certificate)
        {
            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.PushSequence();
            writer.PushSequence();
            writer.WriteOctetString(SHA256.HashData(certificate.RawData));
            writer.PopSequence();
            writer.PopSequence();
            writer.PopSequence();
            return writer.Encode();
        }

        public static byte[] BuildSignedAttributes(byte[] contentHash, byte[] signingCertificateV2, DateTime signedAtUtc)
        {
            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSetOf();
            WriteAttribute(writer, PdfSigningConst.OID_CONTENT_TYPE, w => w.WriteObjectIdentifier(PdfSigningConst.OID_DATA));
            WriteAttribute(writer, PdfSigningConst.OID_MESSAGE_DIGEST, w => w.WriteOctetString(contentHash));
            WriteAttribute(writer, PdfSigningConst.OID_SIGNING_TIME, w => w.WriteUtcTime(new DateTimeOffset(signedAtUtc, TimeSpan.Zero)));
            WriteAttribute(writer, PdfSigningConst.OID_SIGNING_CERTIFICATE_V2, w => w.WriteEncodedValue(signingCertificateV2));
            writer.PopSetOf();
            return writer.Encode();
        }

        public static byte[] Assemble(byte[] signedAttributes, byte[] signature, X509Certificate2 certificate, IReadOnlyList<X509Certificate2> chain, byte[] timestampToken)
        {
            if (signedAttributes.Length == 0 || signedAttributes[0] != TagSetOf)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignatureInvalid);
            }

            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteObjectIdentifier(PdfSigningConst.OID_SIGNED_DATA);
            writer.PushSequence(Context0);
            writer.PushSequence();
            writer.WriteInteger(1);
            writer.PushSetOf();
            WriteAlgorithm(writer, PdfSigningConst.OID_SHA256, false);
            writer.PopSetOf();
            writer.PushSequence();
            writer.WriteObjectIdentifier(PdfSigningConst.OID_DATA);
            writer.PopSequence();
            writer.PushSetOf(Context0);
            foreach (var item in chain)
            {
                writer.WriteEncodedValue(item.RawData);
            }
            writer.PopSetOf(Context0);
            writer.PushSetOf();
            WriteSignerInfo(writer, signedAttributes, signature, certificate, timestampToken);
            writer.PopSetOf();
            writer.PopSequence();
            writer.PopSequence(Context0);
            writer.PopSequence();
            return writer.Encode();
        }

        private static void WriteSignerInfo(AsnWriter writer, byte[] signedAttributes, byte[] signature, X509Certificate2 certificate, byte[] timestampToken)
        {
            writer.PushSequence();
            writer.WriteInteger(1);
            writer.PushSequence();
            writer.WriteEncodedValue(certificate.IssuerName.RawData);
            writer.WriteInteger(certificate.SerialNumberBytes.Span);
            writer.PopSequence();
            WriteAlgorithm(writer, PdfSigningConst.OID_SHA256, false);

            var implicitAttributes = signedAttributes.ToArray();
            implicitAttributes[0] = TagContext0;
            writer.WriteEncodedValue(implicitAttributes);

            WriteAlgorithm(writer, PdfSigningConst.OID_RSA_ENCRYPTION, true);
            writer.WriteOctetString(signature);
            writer.PushSetOf(Context1);
            WriteAttribute(writer, PdfSigningConst.OID_SIGNATURE_TIMESTAMP_TOKEN, w => w.WriteEncodedValue(timestampToken));
            writer.PopSetOf(Context1);
            writer.PopSequence();
        }

        private static void WriteAttribute(AsnWriter writer, string oid, Action<AsnWriter> writeValue)
        {
            writer.PushSequence();
            writer.WriteObjectIdentifier(oid);
            writer.PushSetOf();
            writeValue(writer);
            writer.PopSetOf();
            writer.PopSequence();
        }

        private static void WriteAlgorithm(AsnWriter writer, string oid, bool hasNullParameter)
        {
            writer.PushSequence();
            writer.WriteObjectIdentifier(oid);
            if (hasNullParameter)
            {
                writer.WriteNull();
            }
            writer.PopSequence();
        }
    }
}
