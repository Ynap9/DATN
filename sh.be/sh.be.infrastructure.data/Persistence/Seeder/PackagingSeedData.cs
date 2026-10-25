using sh.be.shared.Constants.Packaging;
using static sh.be.shared.Constants.Packaging.PackageSchemaFieldConst;

namespace sh.be.infrastructure.data.Persistence.Seeder
{
    public static class PackagingSeedData
    {
        public record CodeValueSeed(string Code, string Name);

        public record CodeGroupSeed(string CodeGroup, IReadOnlyList<CodeValueSeed> Values);

        public record FieldSeed(string ElementName, string DisplayName, int DataType, int? MaxLength, int Requirement,
            string? CodeGroup = null, bool AllowMultipleValues = false, string? ConditionElement = null, string? ConditionValues = null,
            string? Description = null);

        public record SchemaSeed(int PackageType, int MetadataType, string Name, string XsdFileName, string MetadataFilePrefix,
            IReadOnlyList<FieldSeed> Fields);

        public static readonly IReadOnlyList<CodeGroupSeed> CodeGroups =
        [
            new(PackageCodeGroupConst.MAINTENANCE,
            [
                new("01", "Vĩnh viễn"), new("02", "70 năm"), new("03", "50 năm"), new("04", "30 năm"), new("05", "20 năm"),
                new("06", "10 năm"), new("07", "Khác"),
            ]),
            new(PackageCodeGroupConst.MODE, [new("01", "Công khai"), new("02", "Sử dụng có điều kiện"), new("03", "Mật")]),
            new(PackageCodeGroupConst.LANGUAGE,
            [
                new("01", "Tiếng Việt"), new("02", "Tiếng Anh"), new("03", "Tiếng Pháp"), new("04", "Tiếng Nga"), new("05", "Tiếng Trung"),
                new("06", "Việt Anh"), new("07", "Việt Nga"), new("08", "Việt Pháp"), new("09", "Hán Nôm"), new("10", "Việt Trung"),
                new("11", "Khác"),
            ]),
            new(PackageCodeGroupConst.CONFIDENCE_LEVEL, [new("01", "Gốc điện tử"), new("02", "Số hóa"), new("03", "Hỗn hợp")]),
            new(PackageCodeGroupConst.FORMAT, [new("01", "Tốt"), new("02", "Bình thường"), new("03", "Hỏng")]),
            new(PackageCodeGroupConst.RISK_RECOVERY_STATUS, [new("01", "Đã dự phòng"), new("02", "Chưa dự phòng")]),
            new(PackageCodeGroupConst.TYPE_NAME,
            [
                new("01", "Nghị quyết"), new("02", "Quyết định"), new("03", "Chỉ thị"), new("04", "Quy chế"), new("05", "Quy định"),
                new("06", "Thông cáo"), new("07", "Thông báo"), new("08", "Hướng dẫn"), new("09", "Chương trình"), new("10", "Kế hoạch"),
                new("11", "Phương án"), new("12", "Đề án"), new("13", "Dự án"), new("14", "Báo cáo"), new("15", "Tờ trình"),
                new("16", "Giấy ủy quyền"), new("17", "Phiếu gửi"), new("18", "Phiếu chuyển"), new("19", "Phiếu báo"),
                new("20", "Biên bản"), new("21", "Hợp đồng"), new("22", "Công văn"), new("23", "Công điện"), new("24", "Bản ghi nhớ"),
                new("25", "Bản thỏa thuận"), new("26", "Giấy mời"), new("27", "Giấy giới thiệu"), new("28", "Giấy nghỉ phép"),
                new("29", "Thư công"), new("30", "Bản đồ"), new("31", "Bản vẽ kỹ thuật"), new("32", "Khác"),
            ]),
            new(PackageCodeGroupConst.SOURCE, [new("0", "Văn bản đi"), new("1", "Văn bản đến")]),
            new(PackageCodeGroupConst.PURPOSE, [new("01", "Cá nhân"), new("02", "Công vụ"), new("03", "Công vụ đặc biệt")]),
            new(PackageCodeGroupConst.FEE_OBJECT_TYPE,
            [
                new("01", "Học sinh/sinh viên/học viên/nghiên cứu sinh"), new("02", "Thân nhân liệt sĩ"), new("03", "Thương binh, bệnh binh"),
                new("04", "Người hoạt động kháng chiến"), new("05", "Người có công giúp đỡ cách mạng"), new("06", "Người thờ cúng liệt sỹ"),
                new("07", "Người hưởng chế độ hưu trí"), new("08", "Người mất sức lao động, tai nạn lao động"),
                new("09", "Người bị mắc bệnh nghề nghiệp"), new("10", "Khác"),
            ]),
            new(PackageCodeGroupConst.TYPE_PIC, [new("01", "Phim âm bản"), new("02", "Ảnh")]),
            new(PackageCodeGroupConst.COLOUR, [new("01", "Màu"), new("02", "Đen trắng")]),
            new(PackageCodeGroupConst.TYPE_MEDIA, [new("01", "Ghi âm"), new("02", "Ghi hình")]),
        ];

        private static readonly FieldSeed SipHosoFileCode = new("fileCode", "Mã hồ sơ", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã định danh của cơ quan, tổ chức, cá nhân + Năm hình thành hồ sơ + Số và ký hiệu hồ sơ");
        private static readonly FieldSeed SipTailieuFileCode = new("fileCode", "Mã gói tin SIP_tailieu", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã định danh của cơ quan, tổ chức, cá nhân + Năm hình thành tài liệu + Số lần nộp lưu (2 ký tự) + Số thứ tự tài liệu trong lần nộp (7 ký tự)");
        private static readonly FieldSeed AipHosoArcFileCode = new("arcFileCode", "Mã hồ sơ lưu trữ", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã cơ quan lưu trữ + Mã hồ sơ (Mã định danh của cơ quan, tổ chức, cá nhân hoặc Mã phông + Năm hình thành hồ sơ + Số và ký hiệu hồ sơ + Mục lục số nếu có)");
        private static readonly FieldSeed AipTailieuArcFileCode = new("arcFileCode", "Mã gói tin lưu trữ", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã cơ quan lưu trữ + Mã gói tin (Mã định danh của cơ quan, tổ chức, cá nhân + Năm hình thành tài liệu + Số thứ tự lần nộp lưu + Số thứ tự gói tin trong lần nộp)");
        private static readonly FieldSeed FileTitle = new("title", "Tiêu đề hồ sơ", DATA_TYPE_TEXT, 1000, REQUIRED);
        private static readonly FieldSeed PackageTitle = new("title", "Tiêu đề gói tin", DATA_TYPE_TEXT, 1000, REQUIRED,
            Description: "Tóm tắt nội dung và thời gian tài liệu trong gói tin");
        private static readonly FieldSeed Maintenance = new("maintenance", "Thời hạn lưu trữ", DATA_TYPE_TEXT, 100, REQUIRED,
            PackageCodeGroupConst.MAINTENANCE, Description: "Nguồn nộp và sưu tầm chỉ nhận giá trị 01");
        private static readonly FieldSeed PackageMode = new("mode", "Chế độ sử dụng", DATA_TYPE_TEXT, 30, REQUIRED, PackageCodeGroupConst.MODE);
        private static readonly FieldSeed DocumentMode = new("mode", "Chế độ sử dụng", DATA_TYPE_TEXT, 20, REQUIRED, PackageCodeGroupConst.MODE);
        private static readonly FieldSeed Language = new("language", "Ngôn ngữ", DATA_TYPE_TEXT, 100, REQUIRED,
            PackageCodeGroupConst.LANGUAGE, AllowMultipleValues: true, Description: "Được chọn 1 hoặc nhiều giá trị");
        private static readonly FieldSeed StartDate = new("startDate", "Thời gian bắt đầu", DATA_TYPE_DATE, null, REQUIRED,
            Description: "DD/MM/YYYY");
        private static readonly FieldSeed EndDate = new("endDate", "Thời gian kết thúc", DATA_TYPE_DATE, null, REQUIRED,
            Description: "DD/MM/YYYY");
        private static readonly FieldSeed AipStartDate = new("startDate", "Thời gian bắt đầu", DATA_TYPE_DATE, null, REQUIRED,
            Description: "DD/MM/YYYY, MM/YYYY hoặc YYYY");
        private static readonly FieldSeed AipEndDate = new("endDate", "Thời gian kết thúc", DATA_TYPE_DATE, null, REQUIRED,
            Description: "DD/MM/YYYY, MM/YYYY hoặc YYYY");
        private static readonly FieldSeed Keyword = new("keyword", "Từ khóa (nếu có)", DATA_TYPE_TEXT, 100, OPTIONAL);
        private static readonly FieldSeed FileTotalDoc = new("totalDoc", "Tổng số tài liệu trong hồ sơ", DATA_TYPE_NUMBER, 10, REQUIRED,
            Description: "Gồm văn bản, tài liệu kỹ thuật, tài liệu âm bản, ảnh, tài liệu ghi âm, phim điện ảnh");
        private static readonly FieldSeed PackageTotalDoc = new("totalDoc", "Tổng số tài liệu trong gói tin", DATA_TYPE_NUMBER, 10, REQUIRED,
            Description: "Gồm văn bản, tài liệu kỹ thuật, tài liệu âm bản, ảnh, tài liệu ghi âm, phim điện ảnh");
        private static readonly FieldSeed NumberOfPaper = new("numberOfPaper", "Số lượng tờ", DATA_TYPE_NUMBER, 10, CONDITIONAL,
            ConditionElement: "confidenceLevel", ConditionValues: "02",
            Description: "Dành riêng cho tài liệu giấy được số hóa, bắt buộc nhập với hồ sơ số hóa");
        private static readonly FieldSeed PackageNumberOfPage = new("numberOfPage", "Số lượng trang", DATA_TYPE_NUMBER, 10, REQUIRED);
        private static readonly FieldSeed DocumentNumberOfPage = new("numberOfPage", "Số lượng trang", DATA_TYPE_NUMBER, 4, REQUIRED);
        private static readonly FieldSeed Format = new("format", "Tình trạng vật lý (nếu có)", DATA_TYPE_TEXT, 50, OPTIONAL,
            PackageCodeGroupConst.FORMAT);
        private static readonly FieldSeed InforSign = new("inforSign", "Ký hiệu thông tin (nếu có)", DATA_TYPE_TEXT, 30, OPTIONAL);
        private static readonly FieldSeed SipFileConfidenceLevel = new("confidenceLevel", "Mức độ tin cậy (nếu có)", DATA_TYPE_TEXT, 40, OPTIONAL,
            PackageCodeGroupConst.CONFIDENCE_LEVEL);
        private static readonly FieldSeed AipFileConfidenceLevel = new("confidenceLevel", "Mức độ tin cậy", DATA_TYPE_TEXT, 40, REQUIRED,
            PackageCodeGroupConst.CONFIDENCE_LEVEL);
        private static readonly FieldSeed DocumentConfidenceLevel = new("confidenceLevel", "Mức độ tin cậy (nếu có)", DATA_TYPE_TEXT, 30, OPTIONAL,
            PackageCodeGroupConst.CONFIDENCE_LEVEL);
        private static readonly FieldSeed DipDocumentConfidenceLevel = new("confidenceLevel", "Mức độ tin cậy", DATA_TYPE_TEXT, 30, REQUIRED,
            PackageCodeGroupConst.CONFIDENCE_LEVEL);
        private static readonly FieldSeed PaperFileCode = new("paperFileCode", "Mã hồ sơ gốc giấy (nếu có)", DATA_TYPE_TEXT, 100, CONDITIONAL,
            ConditionElement: "confidenceLevel", ConditionValues: "02",
            Description: "[Mã cơ quan lưu trữ].[Số kho/giá/hộp].[Số hồ sơ giấy], bắt buộc nhập với hồ sơ số hóa");
        private static readonly FieldSeed RiskRecovery = new("riskRecovery", "Chế độ dự phòng", DATA_TYPE_BOOLEAN, 1, REQUIRED,
            Description: "1: Có, 0: Không");
        private static readonly FieldSeed RiskRecoveryStatus = new("riskRecoveryStatus", "Tình trạng dự phòng", DATA_TYPE_TEXT, 2, CONDITIONAL,
            PackageCodeGroupConst.RISK_RECOVERY_STATUS, ConditionElement: "riskRecovery", ConditionValues: "1",
            Description: "Bắt buộc nhập khi chế độ dự phòng là có");
        private static readonly FieldSeed PackageDescription = new("description", "Ghi chú", DATA_TYPE_TEXT, 2000, OPTIONAL,
            Description: "Ghi tên người lập hồ sơ và những nội dung cần làm sáng tỏ sự kiện, nội dung tài liệu hoặc những điểm nổi bật khác cần lưu ý");
        private static readonly FieldSeed OptionalPackageDescription = new("description", "Ghi chú (nếu có)", DATA_TYPE_TEXT, 2000, OPTIONAL);
        private static readonly FieldSeed DocumentDescription = new("description", "Ghi chú", DATA_TYPE_TEXT, 500, OPTIONAL);
        private static readonly FieldSeed OptionalDocumentDescription = new("description", "Ghi chú (nếu có)", DATA_TYPE_TEXT, 500, OPTIONAL);
        private static readonly FieldSeed SipSource = new("source", "Nguồn gốc", DATA_TYPE_BOOLEAN, 1, REQUIRED, PackageCodeGroupConst.SOURCE);
        private static readonly FieldSeed AipSource = new("source", "Nguồn gốc", DATA_TYPE_TEXT, 100, REQUIRED, PackageCodeGroupConst.SOURCE);

        private static readonly FieldSeed RequestId = new("requestID", "Mã yêu cầu khai thác", DATA_TYPE_TEXT, 100, REQUIRED);
        private static readonly FieldSeed RequestDate = new("requestDate", "Ngày yêu cầu", DATA_TYPE_DATE, null, REQUIRED);
        private static readonly FieldSeed Purpose = new("purpose", "Mục đích", DATA_TYPE_TEXT, 100, REQUIRED, PackageCodeGroupConst.PURPOSE);
        private static readonly FieldSeed PurposeContent = new("purposeContent", "Nội dung mục đích", DATA_TYPE_TEXT, 500, REQUIRED);
        private static readonly FieldSeed FeeObjectType = new("feeObjectType", "Đối tượng", DATA_TYPE_TEXT, 100, REQUIRED,
            PackageCodeGroupConst.FEE_OBJECT_TYPE);
        private static readonly FieldSeed ResearchTopic = new("researchTopic", "Chủ đề nghiên cứu", DATA_TYPE_TEXT, 250, REQUIRED);

        private static readonly FieldSeed DocId = new("docId", "Mã định danh tài liệu", DATA_TYPE_TEXT, 25, REQUIRED);
        private static readonly FieldSeed DocCode = new("docCode", "Mã lưu trữ của tài liệu", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã hồ sơ + Số thứ tự tài liệu trong hồ sơ (7 ký tự)");
        private static readonly FieldSeed SipTailieuDocCode = new("docCode", "Mã lưu trữ của tài liệu", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã định danh của cơ quan, tổ chức, cá nhân + Năm hình thành tài liệu + Số lần nộp lưu (2 ký tự) + Số thứ tự tài liệu trong lần nộp (7 ký tự)");
        private static readonly FieldSeed ArcDocCode = new("arcDocCode", "Mã lưu trữ tài liệu", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã cơ quan lưu trữ + Mã hồ sơ + Số thứ tự tài liệu trong hồ sơ (7 ký tự)");
        private static readonly FieldSeed AipTailieuArcDocCode = new("arcDocCode", "Mã lưu trữ tài liệu", DATA_TYPE_TEXT, 100, REQUIRED,
            Description: "Mã cơ quan lưu trữ + Mã định danh của cơ quan, tổ chức, cá nhân + Năm hình thành tài liệu + Số lần nộp lưu + Số thứ tự tài liệu trong lần nộp (7 ký tự)");
        private static readonly FieldSeed DocOrdinal = new("docOrdinal", "Số thứ tự tài liệu", DATA_TYPE_NUMBER, 7, REQUIRED,
            Description: "Số thứ tự tài liệu trong hồ sơ");
        private static readonly FieldSeed TypeName = new("typeName", "Tên loại tài liệu", DATA_TYPE_TEXT, 10, REQUIRED, PackageCodeGroupConst.TYPE_NAME);
        private static readonly FieldSeed CodeNumber = new("codeNumber", "Số của tài liệu", DATA_TYPE_TEXT, 11, REQUIRED);
        private static readonly FieldSeed OptionalCodeNumber = new("codeNumber", "Số của tài liệu (nếu có)", DATA_TYPE_TEXT, 11, OPTIONAL);
        private static readonly FieldSeed CodeNotation = new("codeNotation", "Ký hiệu của tài liệu", DATA_TYPE_TEXT, 30, REQUIRED);
        private static readonly FieldSeed OptionalCodeNotation = new("codeNotation", "Ký hiệu của tài liệu (nếu có)", DATA_TYPE_TEXT, 30, OPTIONAL);
        private static readonly FieldSeed IssuedDate = new("issuedDate", "Ngày, tháng, năm tài liệu", DATA_TYPE_DATE, null, REQUIRED,
            Description: "DD/MM/YYYY");
        private static readonly FieldSeed AipIssuedDate = new("issuedDate", "Ngày, tháng, năm tài liệu", DATA_TYPE_DATE, null, REQUIRED,
            Description: "DD/MM/YYYY, MM/YYYY hoặc YYYY");
        private static readonly FieldSeed OrganName = new("organName", "Tên cơ quan, tổ chức, cá nhân ban hành tài liệu", DATA_TYPE_TEXT, 200, REQUIRED);
        private static readonly FieldSeed Subject = new("subject", "Trích yếu nội dung", DATA_TYPE_TEXT, 500, REQUIRED);
        private static readonly FieldSeed Autograph = new("autograph", "Bút tích (nếu có)", DATA_TYPE_TEXT, 2000, OPTIONAL);
        private static readonly FieldSeed Process = new("process", "Quy trình xử lý (nếu có)", DATA_TYPE_BOOLEAN, 1, CONDITIONAL,
            ConditionElement: "confidenceLevel", ConditionValues: "01,03",
            Description: "1: Có quy trình xử lý đi kèm, bắt buộc với tài liệu điện tử xử lý trên Hệ thống");

        private static readonly FieldSeed TypePic = new("typePic", "Phân loại (âm bản/ảnh)", DATA_TYPE_TEXT, 2, REQUIRED, PackageCodeGroupConst.TYPE_PIC);
        private static readonly FieldSeed ArchivesNumber = new("archivesNumber", "Số lưu trữ (đặc thù)", DATA_TYPE_TEXT, 50, REQUIRED);
        private static readonly FieldSeed EventName = new("eventName", "Tên sự kiện", DATA_TYPE_TEXT, 500, REQUIRED);
        private static readonly FieldSeed ImageTitle = new("imageTitle", "Tiêu đề phim/ảnh", DATA_TYPE_TEXT, 500, REQUIRED);
        private static readonly FieldSeed Photographer = new("photographer", "Tác giả", DATA_TYPE_TEXT, 300, REQUIRED,
            Description: "Họ và tên tác giả chụp ảnh");
        private static readonly FieldSeed PhotoPlace = new("photoPlace", "Địa điểm chụp", DATA_TYPE_TEXT, 300, REQUIRED);
        private static readonly FieldSeed PhotoTime = new("photoTime", "Thời gian chụp", DATA_TYPE_DATE, null, REQUIRED, Description: "DD/MM/YYYY");
        private static readonly FieldSeed Colour = new("colour", "Màu sắc", DATA_TYPE_TEXT, 50, REQUIRED, PackageCodeGroupConst.COLOUR);
        private static readonly FieldSeed FilmSize = new("filmSize", "Cỡ phim/ảnh", DATA_TYPE_TEXT, 30, REQUIRED);
        private static readonly FieldSeed DocAttached = new("docAttached", "Tài liệu đi kèm (nếu có)", DATA_TYPE_BOOLEAN, 1, OPTIONAL,
            Description: "1: Có tài liệu đi kèm, 0: Không có tài liệu đi kèm");

        private static readonly FieldSeed TypeMedia = new("typeMedia", "Phân loại (âm thanh/video)", DATA_TYPE_TEXT, 2, REQUIRED,
            PackageCodeGroupConst.TYPE_MEDIA);
        private static readonly FieldSeed MovieTitle = new("movieTitle", "Tiêu đề phim/âm thanh", DATA_TYPE_TEXT, 500, REQUIRED);
        private static readonly FieldSeed Recorder = new("recorder", "Tác giả", DATA_TYPE_TEXT, 300, REQUIRED);
        private static readonly FieldSeed RecordPlace = new("recordPlace", "Địa điểm", DATA_TYPE_TEXT, 300, REQUIRED);
        private static readonly FieldSeed RecordDate = new("recordDate", "Thời gian", DATA_TYPE_DATE, null, REQUIRED, Description: "DD/MM/YYYY");
        private static readonly FieldSeed PlayTime = new("playTime", "Thời lượng", DATA_TYPE_TEXT, 8, REQUIRED);
        private static readonly FieldSeed Quality = new("quality", "Chất lượng", DATA_TYPE_TEXT, 50, REQUIRED,
            Description: "Chất lượng thực tế như: bình thường, mờ, lẫn tạp âm, tiếng lúc to lúc nhỏ");

        public static readonly IReadOnlyList<SchemaSeed> Schemas =
        [
            new(PackageConst.AIP_HOSO, PackageSchemaConst.METADATA_PACKAGE, "Hồ sơ", "ead.xsd", "EAD",
            [
                AipHosoArcFileCode, FileTitle, Maintenance, PackageMode, Language, AipStartDate, AipEndDate, Keyword, FileTotalDoc,
                NumberOfPaper, PackageNumberOfPage, Format, InforSign, AipFileConfidenceLevel, PaperFileCode, PackageDescription,
                RiskRecovery, RiskRecoveryStatus,
            ]),
            new(PackageConst.AIP_HOSO, PackageSchemaConst.METADATA_TEXT, "Văn bản", "ead_doc.xsd", "EAD_DOC_",
            [
                AipHosoArcFileCode, DocumentMode, Language, Keyword, DocumentNumberOfPage, Format, InforSign, DocumentConfidenceLevel,
                DocumentDescription, DocCode, DocOrdinal, TypeName, OptionalCodeNumber, OptionalCodeNotation, AipIssuedDate, OrganName,
                Subject, Autograph, Process,
            ]),
            new(PackageConst.AIP_HOSO, PackageSchemaConst.METADATA_PICTURE, "Phim âm bản, ảnh", "ead_pic.xsd", "EAD_PIC_",
            [
                AipHosoArcFileCode, DocumentMode, Format, InforSign, DocumentDescription, DocOrdinal, TypePic, ArchivesNumber,
                EventName, ImageTitle, Photographer, PhotoPlace, PhotoTime, Colour, FilmSize, DocAttached,
            ]),
            new(PackageConst.AIP_HOSO, PackageSchemaConst.METADATA_MEDIA, "Ghi âm, ghi hình", "ead_media.xsd", "EAD_MEDIA_",
            [
                AipHosoArcFileCode, DocumentMode, Language, Format, InforSign, DocumentDescription, DocOrdinal, ArchivesNumber,
                EventName, DocAttached, TypeMedia, MovieTitle, Recorder, RecordPlace, RecordDate, PlayTime, Quality,
            ]),
            new(PackageConst.AIP_TAILIEU, PackageSchemaConst.METADATA_PACKAGE, "Gói tài liệu", "ead.xsd", "EAD",
            [
                AipTailieuArcFileCode, PackageTitle, AipSource, OptionalPackageDescription,
            ]),
            new(PackageConst.AIP_TAILIEU, PackageSchemaConst.METADATA_TEXT, "Văn bản", "ead_doc.xsd", "EAD_DOC_",
            [
                AipHosoArcFileCode, DocumentMode, Language, Keyword, DocumentNumberOfPage, Format, InforSign, DocumentConfidenceLevel,
                DocumentDescription, DocCode, DocOrdinal, TypeName, OptionalCodeNumber, OptionalCodeNotation, AipIssuedDate, OrganName,
                Subject, Autograph, Process,
            ]),
            new(PackageConst.AIP_TAILIEU, PackageSchemaConst.METADATA_PICTURE, "Phim âm bản, ảnh", "ead_pic.xsd", "EAD_PIC_",
            [
                AipTailieuArcDocCode, Maintenance, TypePic, ArchivesNumber, InforSign, EventName, ImageTitle, Photographer, PhotoPlace,
                PhotoTime, Colour, FilmSize, DocAttached, DocumentMode, Format, RiskRecovery, RiskRecoveryStatus, DocumentDescription,
            ]),
            new(PackageConst.AIP_TAILIEU, PackageSchemaConst.METADATA_MEDIA, "Ghi âm, ghi hình", "ead_media.xsd", "EAD_MEDIA_",
            [
                AipTailieuArcDocCode, Maintenance, TypeMedia, ArchivesNumber, InforSign, EventName, MovieTitle, Recorder, RecordPlace,
                RecordDate, Language, PlayTime, DocAttached, DocumentMode, Quality, Format, RiskRecovery, RiskRecoveryStatus,
                DocumentDescription,
            ]),
            new(PackageConst.SIP_HOSO, PackageSchemaConst.METADATA_PACKAGE, "Hồ sơ", "ead.xsd", "EAD",
            [
                SipHosoFileCode, FileTitle, Maintenance, PackageMode, Language, StartDate, EndDate, Keyword, FileTotalDoc, NumberOfPaper,
                PackageNumberOfPage, Format, InforSign, SipFileConfidenceLevel, PaperFileCode, RiskRecovery, RiskRecoveryStatus,
                PackageDescription,
            ]),
            new(PackageConst.SIP_HOSO, PackageSchemaConst.METADATA_TEXT, "Văn bản", "ead_doc.xsd", "EAD_DOC_",
            [
                DocId, DocCode, Maintenance, TypeName, CodeNumber, CodeNotation, IssuedDate, OrganName, Subject, Language,
                DocumentNumberOfPage, InforSign, Keyword, DocumentMode, DocumentConfidenceLevel, Autograph, Format, Process, RiskRecovery,
                RiskRecoveryStatus, DocumentDescription,
            ]),
            new(PackageConst.SIP_HOSO, PackageSchemaConst.METADATA_PICTURE, "Phim âm bản, ảnh", "ead_pic.xsd", "EAD_PIC_",
            [
                DocCode, Maintenance, TypePic, ArchivesNumber, InforSign, EventName, ImageTitle, Photographer, PhotoPlace, PhotoTime,
                Colour, FilmSize, DocAttached, DocumentMode, Format, RiskRecovery, RiskRecoveryStatus, DocumentDescription,
            ]),
            new(PackageConst.SIP_HOSO, PackageSchemaConst.METADATA_MEDIA, "Ghi âm, ghi hình", "ead_media.xsd", "EAD_MEDIA_",
            [
                DocCode, Maintenance, TypeMedia, ArchivesNumber, InforSign, EventName, MovieTitle, Recorder, RecordPlace, RecordDate,
                Language, PlayTime, DocAttached, DocumentMode, Quality, Format, RiskRecovery, RiskRecoveryStatus, DocumentDescription,
            ]),
            new(PackageConst.SIP_TAILIEU, PackageSchemaConst.METADATA_PACKAGE, "Gói tài liệu", "ead.xsd", "EAD",
            [
                SipTailieuFileCode, PackageTitle, SipSource, PackageTotalDoc, PackageDescription,
            ]),
            new(PackageConst.SIP_TAILIEU, PackageSchemaConst.METADATA_TEXT, "Văn bản", "ead_doc.xsd", "EAD_DOC_",
            [
                DocId, SipTailieuDocCode, Maintenance, TypeName, OptionalCodeNumber, OptionalCodeNotation, IssuedDate, OrganName, Subject,
                Language, DocumentNumberOfPage, InforSign, Keyword, DocumentMode, DocumentConfidenceLevel, Autograph, Format, Process,
                RiskRecovery, RiskRecoveryStatus, DocumentDescription,
            ]),
            new(PackageConst.SIP_TAILIEU, PackageSchemaConst.METADATA_PICTURE, "Phim âm bản, ảnh", "ead_pic.xsd", "EAD_PIC_",
            [
                SipTailieuDocCode, Maintenance, TypePic, ArchivesNumber, InforSign, EventName, ImageTitle, Photographer, PhotoPlace,
                PhotoTime, Colour, FilmSize, DocAttached, DocumentMode, Format, RiskRecovery, RiskRecoveryStatus, DocumentDescription,
            ]),
            new(PackageConst.SIP_TAILIEU, PackageSchemaConst.METADATA_MEDIA, "Ghi âm, ghi hình", "ead_media.xsd", "EAD_MEDIA_",
            [
                SipTailieuDocCode, Maintenance, TypeMedia, ArchivesNumber, InforSign, EventName, MovieTitle, Recorder, RecordPlace,
                RecordDate, Language, PlayTime, DocAttached, DocumentMode, Quality, Format, RiskRecovery, RiskRecoveryStatus,
                DocumentDescription,
            ]),
            new(PackageConst.DIP, PackageSchemaConst.METADATA_PACKAGE, "Yêu cầu khai thác", "ead.xsd", "EAD",
            [
                RequestId, RequestDate, Purpose, PurposeContent, FeeObjectType, ResearchTopic, OptionalPackageDescription,
            ]),
            new(PackageConst.DIP, PackageSchemaConst.METADATA_TEXT, "Văn bản", "ead_doc.xsd", "EAD_DOC_",
            [
                DocId, ArcDocCode, Maintenance, TypeName, CodeNumber, OptionalCodeNotation, IssuedDate, Subject, Language,
                DocumentNumberOfPage, InforSign, Keyword, DocumentMode, DipDocumentConfidenceLevel, Autograph, Format, Process,
                RiskRecovery, RiskRecoveryStatus, OptionalDocumentDescription,
            ]),
            new(PackageConst.DIP, PackageSchemaConst.METADATA_PICTURE, "Phim âm bản, ảnh", "ead_pic.xsd", "EAD_PIC_",
            [
                ArcDocCode, Maintenance, TypePic, ArchivesNumber, InforSign, EventName, ImageTitle, Photographer, PhotoPlace, PhotoTime,
                Colour, FilmSize, DocAttached, DocumentMode, Format, RiskRecovery, RiskRecoveryStatus, DocumentDescription,
            ]),
            new(PackageConst.DIP, PackageSchemaConst.METADATA_MEDIA, "Ghi âm, ghi hình", "ead_media.xsd", "EAD_MEDIA_",
            [
                ArcDocCode, Maintenance, TypeMedia, ArchivesNumber, InforSign, EventName, MovieTitle, Recorder, RecordPlace, RecordDate,
                Language, PlayTime, DocAttached, DocumentMode, Quality, Format, RiskRecovery, RiskRecoveryStatus, DocumentDescription,
            ]),
        ];
    }
}
