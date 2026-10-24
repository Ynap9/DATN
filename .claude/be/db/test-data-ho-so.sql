SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM construction.Submission WHERE CreatedBy = 'TESTDATA')
BEGIN
    PRINT N'Đã có dữ liệu TESTDATA, chạy test-data-ho-so-xoa.sql trước nếu muốn tạo lại.';
    RETURN;
END

BEGIN TRAN;

INSERT INTO construction.DocumentField (DocumentTypeId, Name, Field, IsShow, OrderTableView, OrderListView, IsShowTableView, IsShowListView, IsChargeable, IsFieldOcr, CreatedBy)
VALUES (3, N'Số hiệu', 'SoHieu', 1, 1, 1, 1, 1, 0, 0, 'TESTDATA'),
       (3, N'Ngày ký', 'NgayKy', 1, 2, 2, 1, 1, 0, 0, 'TESTDATA'),
       (3, N'Trích yếu', 'TrichYeu', 1, 3, 3, 1, 1, 0, 0, 'TESTDATA'),
       (3, N'Người ký', 'NguoiKy', 1, 4, 4, 0, 1, 0, 0, 'TESTDATA'),
       (3, N'Ghi chú', 'GhiChu', 1, 5, 5, 1, 1, 0, 0, 'TESTDATA');

DECLARE @soHieu int = (SELECT Id FROM construction.DocumentField WHERE CreatedBy = 'TESTDATA' AND Field = 'SoHieu');
DECLARE @ngayKy int = (SELECT Id FROM construction.DocumentField WHERE CreatedBy = 'TESTDATA' AND Field = 'NgayKy');
DECLARE @trichYeu int = (SELECT Id FROM construction.DocumentField WHERE CreatedBy = 'TESTDATA' AND Field = 'TrichYeu');
DECLARE @nguoiKy int = (SELECT Id FROM construction.DocumentField WHERE CreatedBy = 'TESTDATA' AND Field = 'NguoiKy');

INSERT INTO construction.ProjectDocumentTypeDocumentField (ProjectId, DocumentTypeId, DocumentFieldId, FieldImport, OrderFieldImport, CreatedBy)
VALUES (3, 3, @soHieu, N'Số hiệu', 1, 'TESTDATA'),
       (3, 3, @ngayKy, N'Ngày ký', 2, 'TESTDATA'),
       (3, 3, @trichYeu, N'Trích yếu', 3, 'TESTDATA'),
       (3, 3, @nguoiKy, N'Người ký', 4, 'TESTDATA');

DECLARE @rows TABLE (FileName nvarchar(255), ProfileRange nvarchar(100), Box nvarchar(100), StatusCheck int,
    SoHieu nvarchar(100), NgayKy nvarchar(100), TrichYeu nvarchar(100), NguoiKy nvarchar(100));
INSERT INTO @rows VALUES
    (N'HS_001.pdf', N'HS01', N'HOP01', 0, N'01/CV', N'02/01/2024', N'Công văn',   N'Nguyễn Văn A'),
    (N'HS_002.pdf', N'HS01', N'HOP01', 0, N'02/QĐ', N'05/01/2024', N'Quyết định', N'Trần Thị B'),
    (N'HS_003.pdf', N'HS01', N'HOP01', 0, N'03/TTr', N'10/01/2024', N'Tờ trình',  N'Nguyễn Văn A'),
    (N'HS_004.pdf', N'HS01', N'HOP01', 1, N'04/CV', N'12/01/2024', N'Công văn',   N'Trần Thị B'),
    (N'HS_005.pdf', N'HS01', N'HOP02', 0, N'05/QĐ', N'15/01/2024', NULL,          N'Nguyễn Văn A'),
    (N'HS_006.pdf', N'HS02', N'HOP02', 0, N'06/CV', N'20/01/2024', N'Công văn',   N'Nguyễn Văn A'),
    (N'HS_007.pdf', N'HS02', N'HOP02', 0, N'07/TTr', N'22/01/2024', N'Tờ trình',  N'Trần Thị B'),
    (N'HS_008.pdf', N'HS02', N'HOP02', 1, N'08/QĐ', N'25/01/2024', N'Quyết định', N'Nguyễn Văn A'),
    (N'HS_009.pdf', N'HS02', N'HOP03', 0, N'09/CV', N'01/02/2024', N'Công văn',   N'Trần Thị B'),
    (N'HS_010.pdf', N'HS02', N'HOP03', 0, N'10/QĐ', N'03/02/2024', N'Quyết định', NULL),
    (N'HS_011.pdf', N'HS03', N'HOP03', 0, N'11/CV', N'07/02/2024', N'Công văn',   N'Nguyễn Văn A'),
    (N'HS_012.pdf', N'HS03', N'HOP03', 1, N'12/TTr', N'10/02/2024', N'Tờ trình',  N'Trần Thị B'),
    (N'HS_013.pdf', N'HS03', N'HOP04', 0, N'13/CV', N'14/02/2024', N'Công văn',   N'Trần Thị B'),
    (N'HS_014.pdf', N'HS03', N'HOP04', 0, N'14/QĐ', N'18/02/2024', N'Quyết định', N'Nguyễn Văn A'),
    (N'HS_015.pdf', N'HS03', N'HOP04', 1, N'15/CV', N'20/02/2024', N'Công văn',   N'Nguyễn Văn A');

INSERT INTO construction.Submission (ProjectId, DocumentTypeId, Name, FileName, FileUrlGoogleDrive, FileUrlGstore, FileUrlS3Object, ProfileRange, Box, StatusCheck, CreatedBy)
SELECT 3, 3, '', FileName, '', '', '', ProfileRange, Box, StatusCheck, 'TESTDATA' FROM @rows ORDER BY FileName;

INSERT INTO construction.Submission (ProjectId, DocumentTypeId, Name, FileName, FileUrlGoogleDrive, FileUrlGstore, FileUrlS3Object, ProfileRange, Box, StatusCheck, CreatedBy)
VALUES (3, 4, '', N'HS_KHAC_LOAI.pdf', '', '', '', N'HS09', N'HOP09', 0, 'TESTDATA');

INSERT INTO construction.SubmissionData (SubmissionId, ProjectId, DocumentTypeId, DocumentFieldId, Data, IndexRow, CreatedBy)
SELECT s.Id, 3, 3, v.FieldId, v.Data, v.IndexRow, 'TESTDATA'
FROM @rows r
JOIN construction.Submission s ON s.CreatedBy = 'TESTDATA' AND s.FileName = r.FileName
CROSS APPLY (VALUES (@soHieu, r.SoHieu, 1), (@ngayKy, r.NgayKy, 2), (@trichYeu, r.TrichYeu, 3), (@nguoiKy, r.NguoiKy, 4)) v(FieldId, Data, IndexRow)
WHERE v.Data IS NOT NULL;

INSERT INTO dbo.AspNetUsers (Id, FullName, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
SELECT v.Id, v.FullName, v.UserName, UPPER(v.UserName), v.UserName + '@test.local', UPPER(v.UserName + '@test.local'), 1, a.PasswordHash, NEWID(), NEWID(), 0, 0, 0, 0
FROM (VALUES ('testdata-tester', N'Người kiểm tra (test)', 'tester'), ('testdata-viewer', N'Người không có quyền (test)', 'viewer')) v(Id, FullName, UserName)
CROSS JOIN (SELECT PasswordHash FROM dbo.AspNetUsers WHERE UserName = 'admin') a;

INSERT INTO auth.UserRole (UserId, RoleId, CreatedBy)
VALUES ('testdata-tester', (SELECT Id FROM auth.Role WHERE Name = 'QA'), 'TESTDATA'),
       ('testdata-viewer', (SELECT Id FROM auth.Role WHERE Name = 'User'), 'TESTDATA');

INSERT INTO auth.RolePermission (RoleId, PermissionKey, CreatedBy)
SELECT (SELECT Id FROM auth.Role WHERE Name = 'QA'), k.PermissionKey, 'TESTDATA'
FROM (VALUES ('Function.Construction.Submission.View'), ('Function.Construction.Submission.Check'), ('Function.Construction.Submission.Qa')) k(PermissionKey)
WHERE NOT EXISTS (SELECT 1 FROM auth.RolePermission rp WHERE rp.RoleId = (SELECT Id FROM auth.Role WHERE Name = 'QA') AND rp.PermissionKey = k.PermissionKey);

COMMIT;

SELECT s.Id, s.FileName, s.ProfileRange, s.Box, s.StatusCheck, s.DocumentTypeId FROM construction.Submission s WHERE s.CreatedBy = 'TESTDATA' ORDER BY s.Id;
SELECT Id, Field, Name FROM construction.DocumentField WHERE CreatedBy = 'TESTDATA' ORDER BY Id;
