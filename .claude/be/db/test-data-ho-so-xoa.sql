SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @subs TABLE (Id int);
INSERT INTO @subs SELECT Id FROM construction.Submission WHERE CreatedBy = 'TESTDATA';

DELETE FROM core.AuditLog WHERE UserId IN ('testdata-tester', 'testdata-viewer')
    OR Content LIKE N'%HS[_]0[0-9][0-9].pdf%' OR Content LIKE N'%HS[_]KHAC[_]LOAI.pdf%';
DELETE FROM construction.SubmissionLog WHERE SubmissionId IN (SELECT Id FROM @subs);
DELETE FROM construction.SubmissionData WHERE SubmissionId IN (SELECT Id FROM @subs);
DELETE FROM construction.Submission WHERE Id IN (SELECT Id FROM @subs);
DELETE FROM construction.ProjectDocumentTypeDocumentField WHERE CreatedBy = 'TESTDATA';
DELETE FROM construction.DocumentField WHERE CreatedBy = 'TESTDATA';
DELETE FROM auth.RolePermission WHERE CreatedBy = 'TESTDATA';
DELETE FROM auth.UserRole WHERE UserId IN ('testdata-tester', 'testdata-viewer');
DELETE FROM dbo.AspNetUsers WHERE Id IN ('testdata-tester', 'testdata-viewer');
UPDATE construction.Project SET Status = 1 WHERE Id = 3 AND Status = 4;

COMMIT;

SELECT (SELECT COUNT(*) FROM construction.Submission WHERE CreatedBy = 'TESTDATA') Submissions,
       (SELECT COUNT(*) FROM construction.DocumentField WHERE CreatedBy = 'TESTDATA') Fields,
       (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE Id LIKE 'testdata-%') Users;
