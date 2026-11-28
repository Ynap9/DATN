using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sh.be.applications.Base.Dtos.Storage;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.infrastructure.external.Storage.Base;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace sh.be.infrastructure.external.Storage.Implements
{
    public class PackageArchiveExternal : IPackageArchiveExternal
    {
        private const string FilesNamespace = "sh.be.infrastructure.external.Storage.Files";
        private const string MetsDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
        private const string EadDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>";
        private const string PremisDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
        private const string MetsNamespace = "http://www.loc.gov/METS/";
        private const string XlinkNamespace = "http://www.w3.org/1999/xlink";
        private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private const string PremisNamespace = "http://www.loc.gov/premis/v3";
        private const string PremisSchemaLocation = "http://www.loc.gov/premis/v3 http://www.loc.gov/standards/premis/premis-3-0-draft.xsd";
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";
        private const string XmlMimeType = "application/xml";
        private const string SchemaMimeType = "application/octet-stream";
        private const string ChecksumType = "SHA-256";
        private const string IdentificationCode = "IDENTIFICATIONCODE";
        private const string SchemasFolder = "schemas";
        private const string RepresentationFolder = "representations/rep1";
        private const string DescriptiveFolder = "metadata/descriptive";
        private const string PreservationFolder = "metadata/preservation";
        private const string DocumentationFolder = "documentation";
        private const string DataFolder = "data";
        private const string MetsFileName = "METS.xml";
        private const string PremisFileName = "PREMIS.xml";
        private const string RepresentationPremisFileName = "PREMIS_rep1.xml";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);
        private static readonly string[] CopiedSchemaFileNames = ["DILCISExtensionMETS.xsd", "DILCISExtensionSIPMETS.xsd", "mets1_12.xsd", "xlink.xsd"];
        private static readonly Lazy<Dictionary<string, byte[]>> CopiedSchemas = new(() => CopiedSchemaFileNames.ToDictionary(fileName => fileName, ReadResource));

        private readonly IFileStorageExternal _fileStorageExternal;
        private readonly PackagingSettings _packagingSettings;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PackageArchiveExternal> _logger;

        public PackageArchiveExternal(IFileStorageExternal fileStorageExternal, IOptions<PackagingSettings> packagingSettings, TimeProvider timeProvider,
            ILogger<PackageArchiveExternal> logger)
        {
            _fileStorageExternal = fileStorageExternal;
            _packagingSettings = packagingSettings.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<long> BuildAsync(PackageArchiveDto dto, string objectKey)
        {
            _logger.LogInformation($"{nameof(BuildAsync)}, objid={dto.Objid}, objectKey={objectKey}");
            var workFolder = Path.Combine(Path.GetTempPath(), $"sh-package-{Guid.NewGuid():N}");
            var packageFolder = Path.Combine(workFolder, dto.Objid.Replace(':', '_'));
            var zipPath = $"{workFolder}{PackageBuildConst.ZIP_EXTENSION}";
            try
            {
                var isPreservation = PackageBuildConst.PRESERVATION_PACKAGE_TYPES.Contains(dto.PackageType);
                var createdDate = FormatNow();
                var checksums = new Dictionary<string, PackageFileChecksum>();

                // Ghi theo thứ tự data → EAD_* → EAD.xml → schemas → PREMIS_rep1 → rep1/METS → PREMIS → METS gốc: METS chứa checksum của file ghi trước nó
                foreach (var file in dto.Files)
                {
                    await DownloadFileAsync(packageFolder, $"{RepresentationFolder}/{DataFolder}/{file.DocKey}{file.Extension}", file, checksums);
                }
                if (dto.DocumentationFile != null)
                {
                    await DownloadFileAsync(packageFolder, $"{DocumentationFolder}/{dto.DocumentationFile.DocKey}{dto.DocumentationFile.Extension}", dto.DocumentationFile, checksums);
                }
                foreach (var file in dto.Files)
                {
                    WriteFile(packageFolder, $"{RepresentationFolder}/{DescriptiveFolder}/{file.MetadataFilePrefix}{file.DocKey}.xml", BuildEad(file.Metadata), checksums);
                }
                WriteFile(packageFolder, $"{DescriptiveFolder}/{dto.MetadataFilePrefix}.xml", BuildEad(dto.Metadata), checksums);
                foreach (var fileName in CopiedSchemaFileNames)
                {
                    WriteFile(packageFolder, $"{SchemasFolder}/{fileName}", CopiedSchemas.Value[fileName], checksums);
                }
                foreach (var schema in dto.Schemas)
                {
                    WriteFile(packageFolder, $"{SchemasFolder}/{schema.XsdFileName}", BuildXsd(schema.ElementNames), checksums);
                }

                var fileIds = dto.Files.ToDictionary(file => file, _ => NewId("ID-"));
                if (isPreservation)
                {
                    var premis = OpenPremis();
                    foreach (var file in dto.Files)
                    {
                        var checksum = checksums[$"{RepresentationFolder}/{DataFolder}/{file.DocKey}{file.Extension}"];
                        premis.Open("object", ("xsi:type", "file"), ("xmlID", NewId()));
                        premis.Open("objectIdentifier");
                        premis.Element("objectIdentifierType", PackageBuildConst.PREMIS_OBJECT_IDENTIFIER_TYPES[file.MetadataType]);
                        premis.Element("objectIdentifierValue", fileIds[file]);
                        premis.Close("objectIdentifier");
                        premis.Open("objectCharacteristics");
                        premis.Element("compositionLevel", "0");
                        premis.Open("fixity");
                        premis.Element("messageDigestAlgorithm", ChecksumType);
                        premis.Element("messageDigest", checksum.Sha256.ToLowerInvariant());
                        premis.Close("fixity");
                        premis.Element("size", checksum.Size.ToString(CultureInfo.InvariantCulture));
                        premis.Open("format");
                        premis.Open("formatDesignation");
                        premis.Element("formatName", file.MimeType);
                        premis.Close("formatDesignation");
                        premis.Close("format");
                        premis.Close("objectCharacteristics");
                        premis.Close("object");
                    }
                    AppendPremisEventAndAgent(premis, dto.PackageType);
                    WriteFile(packageFolder, $"{RepresentationFolder}/{PreservationFolder}/{RepresentationPremisFileName}", premis.ToBytes(), checksums);
                }

                var representationMets = new PackageXmlBuilder(MetsDeclaration);
                var representationAgents = new List<PackageMetsAgent>
                {
                    dto.PackageType == PackageConst.DIP
                        ? new PackageMetsAgent("DISSEMINATOR", "INDIVIDUAL", String.Empty, dto.BuiltBy, dto.PackageCode)
                        : new PackageMetsAgent("CREATOR", "INDIVIDUAL", String.Empty, dto.BuiltBy, dto.FondsCode),
                };
                if (isPreservation && dto.ArchiveCode != null)
                {
                    representationAgents.Add(new PackageMetsAgent("ARCHIVIST", "ORGANIZATION", String.Empty, dto.ArchiveName!, dto.ArchiveCode));
                }
                OpenMets(representationMets, dto.PackageType, "rep1", String.Empty, PackageBuildConst.CSIP_PROFILE, "../../schemas/", createdDate, representationAgents, null, null);
                var dmdSecIds = new Dictionary<PackageArchiveFileDto, string>();
                foreach (var file in dto.Files)
                {
                    var href = $"{DescriptiveFolder}/{file.MetadataFilePrefix}{file.DocKey}.xml";
                    dmdSecIds[file] = AppendDmdSec(representationMets, href, checksums[$"{RepresentationFolder}/{href}"]);
                }
                var representationAmdSecId = NewId();
                var representationPremisMdRefId = AppendAmdSec(representationMets, representationAmdSecId, isPreservation,
                    $"{PreservationFolder}/{RepresentationPremisFileName}", $"{RepresentationFolder}/{PreservationFolder}/{RepresentationPremisFileName}", checksums);
                var dataGroupId = NewId();
                representationMets.Open("fileSec", ("ID", NewId()));
                representationMets.Open("fileGrp", ("ID", dataGroupId), ("USE", "Data"));
                foreach (var file in dto.Files)
                {
                    var href = $"{DataFolder}/{file.DocKey}{file.Extension}";
                    AppendFile(representationMets, fileIds[file], file.MimeType, href, checksums[$"{RepresentationFolder}/{href}"]);
                }
                representationMets.Close("fileGrp");
                representationMets.Close("fileSec");
                representationMets.Open("structMap", ("ID", NewId()), ("TYPE", "PHYSICAL"), ("LABEL", "CSIP"));
                representationMets.Open("div", ("ID", NewId()), ("TYPE", PackageBuildConst.REPRESENTATION_DIV_TYPES[dto.PackageType]), ("LABEL", "rep1"));
                if (isPreservation)
                {
                    representationMets.Open("div", ("ID", NewId()), ("LABEL", "Metadata"));
                    representationMets.Empty("fptr", ("FILEID", representationPremisMdRefId));
                    representationMets.Close("div");
                }
                representationMets.Open("div", ("ID", NewId()), ("LABEL", "MetadataLink"));
                foreach (var file in dto.Files)
                {
                    representationMets.Open("div", ("ID", NewId()), ("ADMID", isPreservation ? representationAmdSecId : null), ("DMDID", dmdSecIds[file]), ("LABEL", "MetadataLink/File"));
                    representationMets.Empty("fptr", ("FILEID", fileIds[file]));
                    representationMets.Close("div");
                }
                representationMets.Close("div");
                representationMets.Open("div", ("ID", NewId()), ("LABEL", "Data"));
                representationMets.Empty("fptr", ("FILEID", dataGroupId));
                representationMets.Close("div");
                representationMets.Close("div");
                representationMets.Close("structMap");
                representationMets.Close("mets");
                WriteFile(packageFolder, $"{RepresentationFolder}/{MetsFileName}", representationMets.ToBytes(), checksums);

                if (isPreservation)
                {
                    var premis = OpenPremis();
                    premis.Open("object", ("xsi:type", "intellectualEntity"), ("xmlID", NewId()));
                    premis.Open("objectIdentifier");
                    premis.Element("objectIdentifierType", "OBJID");
                    premis.Element("objectIdentifierValue", dto.Objid);
                    premis.Close("objectIdentifier");
                    premis.Close("object");
                    AppendPremisEventAndAgent(premis, dto.PackageType);
                    WriteFile(packageFolder, $"{PreservationFolder}/{PremisFileName}", premis.ToBytes(), checksums);
                }

                var mets = new PackageXmlBuilder(MetsDeclaration);
                var agent = new PackageMetsAgent("CREATOR", "OTHER", "SOFTWARE", _packagingSettings.SoftwareName, dto.FondsCode);
                // altRecordID = mã hồ sơ / gói tin chỉ có ở SIP (GM, tr.247); AIP, DIP không phát
                OpenMets(mets, dto.PackageType, dto.Objid, dto.Metadata.FirstOrDefault(e => e.ElementName == PackageBuildConst.TITLE)?.Value ?? String.Empty,
                    PackageBuildConst.METS_PROFILES[dto.PackageType], $"{SchemasFolder}/", createdDate, [agent], _packagingSettings.SoftwareVersion,
                    isPreservation ? null : dto.PackageCode);
                var eadHref = $"{DescriptiveFolder}/{dto.MetadataFilePrefix}.xml";
                var dmdSecId = AppendDmdSec(mets, eadHref, checksums[eadHref]);
                var amdSecId = NewId();
                AppendAmdSec(mets, amdSecId, isPreservation, $"{PreservationFolder}/{PremisFileName}", $"{PreservationFolder}/{PremisFileName}", checksums);
                var schemasGroupId = NewId();
                var documentationGroupId = NewId();
                var representationGroupId = NewId();
                mets.Open("fileSec", ("ID", NewId()));
                mets.Open("fileGrp", ("ID", schemasGroupId), ("USE", "Schemas"));
                foreach (var fileName in CopiedSchemaFileNames.Concat(dto.Schemas.Select(s => s.XsdFileName)))
                {
                    var href = $"{SchemasFolder}/{fileName}";
                    AppendFile(mets, NewId("ID-"), SchemaMimeType, href, checksums[href]);
                }
                mets.Close("fileGrp");
                if (dto.DocumentationFile != null)
                {
                    var href = $"{DocumentationFolder}/{dto.DocumentationFile.DocKey}{dto.DocumentationFile.Extension}";
                    mets.Open("fileGrp", ("ID", documentationGroupId), ("USE", "Documentation"));
                    AppendFile(mets, NewId("ID-"), dto.DocumentationFile.MimeType, href, checksums[href]);
                    mets.Close("fileGrp");
                }
                mets.Open("fileGrp", ("ID", representationGroupId), ("USE", "Representations/rep1"));
                AppendFile(mets, NewId("ID-"), XmlMimeType, $"{RepresentationFolder}/{MetsFileName}", checksums[$"{RepresentationFolder}/{MetsFileName}"]);
                mets.Close("fileGrp");
                mets.Close("fileSec");
                mets.Open("structMap", ("ID", NewId()), ("TYPE", "PHYSICAL"), ("LABEL", "CSIP"));
                mets.Open("div", ("ID", NewId()), ("LABEL", dto.Objid));
                mets.Empty("div", ("ID", NewId()), ("ADMID", isPreservation ? amdSecId : null), ("DMDID", dmdSecId), ("LABEL", "Metadata"));
                mets.Open("div", ("ID", NewId()), ("LABEL", "Schemas"));
                mets.Empty("fptr", ("FILEID", schemasGroupId));
                mets.Close("div");
                mets.Open("div", ("ID", NewId()), ("LABEL", "Representations/rep1"));
                mets.Empty("mptr", ("xlink:type", "simple"), ("xlink:href", $"{RepresentationFolder}/{MetsFileName}"), ("xlink:title", representationGroupId), ("LOCTYPE", "URL"));
                mets.Close("div");
                if (dto.DocumentationFile != null)
                {
                    mets.Open("div", ("ID", NewId()), ("LABEL", "Documentation"));
                    mets.Empty("fptr", ("FILEID", documentationGroupId));
                    mets.Close("div");
                }
                mets.Close("div");
                mets.Close("structMap");
                mets.Close("mets");
                WriteFile(packageFolder, MetsFileName, mets.ToBytes(), checksums);

                // Tự kiểm lại 100% checksum trên đĩa trước khi nén
                foreach (var (relativePath, checksum) in checksums)
                {
                    var actual = ComputeChecksum(Path.Combine(packageFolder, relativePath));
                    if (actual.Size != checksum.Size || actual.Sha256 != checksum.Sha256)
                    {
                        throw new UserFriendlyException(ErrorCodes.BadRequest, $"Checksum file \"{relativePath}\" lệch sau khi ghi gói");
                    }
                }

                ZipFile.CreateFromDirectory(workFolder, zipPath, CompressionLevel.Optimal, false);
                await using (var content = File.OpenRead(zipPath))
                {
                    await _fileStorageExternal.UploadPrivateAsync(content, objectKey, PackageBuildConst.ZIP_MIME_TYPE);
                }
                return new FileInfo(zipPath).Length;
            }
            finally
            {
                if (Directory.Exists(workFolder))
                {
                    Directory.Delete(workFolder, true);
                }
                File.Delete(zipPath);
            }
        }

        private async Task DownloadFileAsync(string packageFolder, string relativePath, PackageArchiveFileDto file, Dictionary<string, PackageFileChecksum> checksums)
        {
            var path = Path.Combine(packageFolder, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using (var destination = File.Create(path))
            {
                await _fileStorageExternal.DownloadToAsync(file.ObjectKey, destination);
            }
            var checksum = ComputeChecksum(path);
            if (!string.Equals(checksum.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"File \"{Path.GetFileName(path)}\" trên kho khác lúc kiểm (SHA-256 lệch)");
            }
            checksums[relativePath] = checksum;
        }

        private void WriteFile(string packageFolder, string relativePath, byte[] content, Dictionary<string, PackageFileChecksum> checksums)
        {
            var path = Path.Combine(packageFolder, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, content);
            checksums[relativePath] = ComputeChecksum(path);
        }

        private PackageFileChecksum ComputeChecksum(string path)
        {
            using var stream = File.OpenRead(path);
            return new PackageFileChecksum(stream.Length, Convert.ToHexString(SHA256.HashData(stream)), FormatNow());
        }

        private string FormatNow()
        {
            return _timeProvider.GetUtcNow().ToOffset(PackageBuildConst.VIETNAM_OFFSET).ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        private void OpenMets(PackageXmlBuilder xml, int packageType, string objid, string label, string profile, string schemaFolder, string createdDate,
            List<PackageMetsAgent> agents, string? softwareVersion, string? altRecordId)
        {
            xml.Open("mets",
                ("xmlns:xsi", XsiNamespace),
                ("xmlns:sip", "https://DILCIS.eu/XML/METS/SIPExtensionMETS"),
                ("xmlns", MetsNamespace),
                ("xmlns:csip", "https://DILCIS.eu/XML/METS/CSIPExtensionMETS"),
                ("xmlns:xlink", XlinkNamespace),
                ("OBJID", objid),
                ("LABEL", label),
                ("TYPE", PackageBuildConst.METS_TYPES[packageType]),
                ("csip:CONTENTINFORMATIONTYPE", "MIXED"),
                ("PROFILE", profile),
                ("xsi:schemaLocation", $"{MetsNamespace} {schemaFolder}mets1_12.xsd {XlinkNamespace} {schemaFolder}xlink.xsd "
                    + $"https://dilcis.eu/XML/METS/CSIPExtensionMETS {schemaFolder}DILCISExtensionMETS.xsd https://dilcis.eu/XML/METS/SIPExtensionMETS {schemaFolder}DILCISExtensionSIPMETS.xsd"));
            xml.Open("metsHdr", ("CREATEDATE", createdDate), ("LASTMODDATE", createdDate), ("RECORDSTATUS", "NEW"),
                ("csip:OAISPACKAGETYPE", PackageBuildConst.OAIS_PACKAGE_TYPES[packageType]));
            foreach (var agent in agents)
            {
                xml.Open("agent", ("ROLE", agent.Role), ("TYPE", agent.Type), ("OTHERTYPE", agent.OtherType));
                xml.Element("name", agent.Name);
                xml.Element("note", agent.IdentificationCode, ("csip:NOTETYPE", IdentificationCode));
                if (softwareVersion != null)
                {
                    xml.Element("note", softwareVersion, ("csip:NOTETYPE", "SOFTWARE VERSION"));
                }
                xml.Close("agent");
            }
            if (altRecordId != null)
            {
                xml.Element("altRecordID", altRecordId, ("TYPE", "SUBMISSIONAGREEMENT"));
            }
            xml.Close("metsHdr");
        }

        private string AppendDmdSec(PackageXmlBuilder xml, string href, PackageFileChecksum checksum)
        {
            var dmdSecId = NewId();
            xml.Open("dmdSec", ("ID", dmdSecId), ("CREATED", checksum.Created), ("STATUS", "CURRENT"));
            AppendMdRef(xml, NewId(), "DC", href, checksum);
            xml.Close("dmdSec");
            return dmdSecId;
        }

        // SIP: amdSec rỗng chỉ có ID (GM); AIP, DIP: digiprovMD trỏ PREMIS, trả ID mdRef để div Metadata rep1 trỏ tới (11 §1, §3)
        private string? AppendAmdSec(PackageXmlBuilder xml, string amdSecId, bool isPreservation, string href, string relativePath,
            Dictionary<string, PackageFileChecksum> checksums)
        {
            if (!isPreservation)
            {
                xml.Empty("amdSec", ("ID", amdSecId));
                return null;
            }

            var mdRefId = NewId();
            xml.Open("amdSec", ("ID", amdSecId));
            xml.Open("digiprovMD", ("ID", NewId()), ("STATUS", "CURRENT"));
            AppendMdRef(xml, mdRefId, "PREMIS", href, checksums[relativePath]);
            xml.Close("digiprovMD");
            xml.Close("amdSec");
            return mdRefId;
        }

        private static void AppendMdRef(PackageXmlBuilder xml, string mdRefId, string mdType, string href, PackageFileChecksum checksum)
        {
            xml.Empty("mdRef",
                ("ID", mdRefId),
                ("LOCTYPE", "URL"),
                ("MDTYPE", mdType),
                ("MDTYPEVERSION", mdType == "DC" ? "1.0.0" : null),
                ("xlink:type", "simple"),
                ("xlink:href", href),
                ("MIMETYPE", XmlMimeType),
                ("SIZE", checksum.Size.ToString(CultureInfo.InvariantCulture)),
                ("CREATED", checksum.Created),
                ("CHECKSUM", checksum.Sha256),
                ("CHECKSUMTYPE", ChecksumType));
        }

        private static void AppendFile(PackageXmlBuilder xml, string fileId, string mimeType, string href, PackageFileChecksum checksum)
        {
            xml.Open("file",
                ("ID", fileId),
                ("MIMETYPE", mimeType),
                ("SIZE", checksum.Size.ToString(CultureInfo.InvariantCulture)),
                ("CREATED", checksum.Created),
                ("CHECKSUM", checksum.Sha256),
                ("CHECKSUMTYPE", ChecksumType));
            xml.Empty("FLocat", ("xlink:type", "simple"), ("xlink:href", href), ("LOCTYPE", "URL"));
            xml.Close("file");
        }

        // Phần tử theo PREMIS v3, khai thêm namespace mặc định để thẻ không tiền tố như ví dụ tr.91 vẫn thuộc namespace PREMIS
        private static PackageXmlBuilder OpenPremis()
        {
            var xml = new PackageXmlBuilder(PremisDeclaration);
            xml.Open("premis",
                ("xmlns", PremisNamespace),
                ("xmlns:premis", PremisNamespace),
                ("xmlns:xlink", XlinkNamespace),
                ("xmlns:xsi", XsiNamespace),
                ("xsi:schemaLocation", PremisSchemaLocation),
                ("version", "3.0"));
            return xml;
        }

        // 1 event (transfer / dissemination) + 1 agent phần mềm; giá trị liên kết bằng đúng agentIdentifierValue (11 §4, không thêm "(task: …)")
        private void AppendPremisEventAndAgent(PackageXmlBuilder xml, int packageType)
        {
            var agentIdentifierValue = $"{_packagingSettings.SoftwareName} {_packagingSettings.SoftwareVersion}";
            xml.Open("event");
            xml.Open("eventIdentifier");
            xml.Element("eventIdentifierType", "UUID");
            xml.Element("eventIdentifierValue", Guid.NewGuid().ToString());
            xml.Close("eventIdentifier");
            xml.Element("eventType", PackageBuildConst.PREMIS_EVENT_TYPES[packageType]);
            xml.Element("eventDateTime", FormatNow());
            xml.Open("linkingAgentIdentifier");
            xml.Element("linkingAgentIdentifierType", "LOCAL");
            xml.Element("linkingAgentIdentifierValue", agentIdentifierValue);
            xml.Element("linkingAgentRole", "executing program");
            xml.Close("linkingAgentIdentifier");
            xml.Close("event");
            xml.Open("agent");
            xml.Open("agentIdentifier");
            xml.Element("agentIdentifierType", "LOCAL");
            xml.Element("agentIdentifierValue", agentIdentifierValue);
            xml.Close("agentIdentifier");
            xml.Element("agentName", _packagingSettings.SoftwareName);
            xml.Element("agentType", "Software");
            xml.Close("agent");
            xml.Close("premis");
        }

        private static byte[] BuildEad(List<PackageArchiveElementDto> metadata)
        {
            var xml = new PackageXmlBuilder(EadDeclaration);
            xml.Open("simpledc");
            foreach (var element in metadata)
            {
                xml.Element(element.ElementName, element.Value);
            }
            xml.Close("simpledc");
            return xml.ToBytes();
        }

        // Khuôn byte đúng 4 file ead*.xsd của gói mẫu: xs:all, xs:string, thụt 4, LF, không xuống dòng cuối file
        private static byte[] BuildXsd(List<string> elementNames)
        {
            var content = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n"
                + "           elementFormDefault=\"qualified\"\n           attributeFormDefault=\"unqualified\">\n\n"
                + "    <xs:complexType name=\"elementContainer\">\n        <xs:all>\n");
            foreach (var elementName in elementNames)
            {
                content.Append($"            <xs:element type=\"xs:string\" name=\"{elementName}\"/>\n");
            }
            content.Append("        </xs:all>\n    </xs:complexType>\n\n    <xs:element name=\"simpledc\" type=\"elementContainer\"/>\n\n</xs:schema>");
            return Utf8WithoutBom.GetBytes(content.ToString());
        }

        private static string NewId(string prefix = "uuid-")
        {
            return $"{prefix}{Guid.NewGuid().ToString().ToUpperInvariant()}";
        }

        // Git trên Windows (autocrlf) có thể checkout .xsd thành CRLF: bỏ "\r" để trả đúng byte gốc LF của gói mẫu
        private static byte[] ReadResource(string fileName)
        {
            var resourceName = $"{FilesNamespace}.{fileName}";
            using var stream = typeof(PackageArchiveExternal).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException(resourceName);
            using var content = new MemoryStream();
            stream.CopyTo(content);
            return content.ToArray().Where(b => b != (byte)'\r').ToArray();
        }
    }

    public record PackageFileChecksum(long Size, string Sha256, string Created);

    public record PackageMetsAgent(string Role, string Type, string OtherType, string Name, string IdentificationCode);
}
