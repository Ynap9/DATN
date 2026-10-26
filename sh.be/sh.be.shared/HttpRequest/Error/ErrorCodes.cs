using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.shared.HttpRequest.Error
{
    public static class ErrorCodes
    {
        //Các mã lỗi căn bản
        public const int System = 1;
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int NotFound = 404;
        public const int Found = 409;
        public const int InternalServerError = 500;

        //Authentication
        public const int AuthInvalidPassword = 101;
        public const int AuthErrorCreateUser = 102;
        public const int AuthErrorUserNotFound = 103;
        public const int AuthErrorCreateRole = 104;
        public const int AuthErrorRoleNotFound = 105;
        public const int AuthErrorUpdateUser = 106;
        public const int AuthErrorDeleteUser = 107;
        public const int AuthErrorChangePassword = 108;
        public const int AuthErrorConflictingRoles = 109;

        //Core
        public const int CoreErrorAuditLogNotFound = 201;
        public const int CoreErrorInvalidDataSource = 202;
        public const int CoreErrorReadDataSourceFailed = 203;
        public const int CoreErrorSheetNotFound = 204;
        public const int CoreErrorCreateDataSourceFailed = 205;
        public const int CoreErrorUpdateDataSourceFailed = 206;
        public const int CoreErrorAuthorizeDataSourceFailed = 207;
        public const int CoreErrorStorageNotConfigured = 208;
        public const int CoreErrorUploadFileFailed = 209;
        public const int CoreErrorDownloadFileFailed = 210;
        public const int CoreErrorBackgroundJobUnavailable = 211;

        public const int ConstructionErrorDocumentTypeNotFound = 301;
        public const int ConstructionErrorDocumentFieldNotFound = 302;
        public const int ConstructionErrorProjectNotFound = 303;
        public const int ConstructionErrorProjectConfigNotFound = 304;
        public const int ConstructionErrorSubmissionNotFound = 305;
        public const int ConstructionErrorImportSheetEmpty = 306;
        public const int ConstructionErrorImportColumnNotFound = 307;
        public const int ConstructionErrorImportDuplicateSubmissionKey = 308;
        public const int ConstructionErrorImportNotEnoughFieldMapping = 309;
        public const int ConstructionErrorImportInProgress = 310;
        public const int ConstructionErrorDocumentFieldNotInProject = 311;
        public const int ConstructionErrorDocumentTypeNotInProject = 312;
        public const int ConstructionErrorProjectDone = 313;
        public const int ConstructionErrorImportFileUrlMissing = 314;
        public const int ConstructionErrorFileUnavailable = 315;
        public const int ConstructionErrorProjectDoneNotEditable = 316;
        public const int ConstructionErrorSubmissionPositionOutOfRange = 317;
        public const int ConstructionErrorSubmissionDataDuplicateField = 318;
        public const int ConstructionErrorSubmissionNotInDocumentType = 319;
        public const int ConstructionErrorSubmissionNotCheckedForQa = 320;
        public const int ConstructionErrorQaReasonRequired = 321;
        public const int ConstructionErrorExportTooManyRows = 322;
        public const int ConstructionErrorProjectExpired = 323;
        public const int ConstructionErrorQaWrongFieldRequired = 324;
        public const int ConstructionErrorSubmissionCheckedNotDeletable = 325;
        public const int ConstructionErrorExportJobNotFound = 326;
        public const int ConstructionErrorQaTicketNotFound = 327;
        public const int ConstructionErrorQaNoSubmissionToAssign = 328;
        public const int ConstructionErrorQaTicketTooLarge = 329;
        public const int ConstructionErrorQaAssignConflict = 330;
        public const int ConstructionErrorQaSameAsEntry = 331;
        public const int ConstructionErrorQaSubmissionLocked = 332;
        public const int ConstructionErrorQaSubmissionNotSample = 333;
        public const int ConstructionErrorQaNotAssignee = 334;
        public const int ConstructionErrorQaTicketStatusInvalid = 335;

        public const int DigitalSignatureErrorTemplateNotFound = 601;
        public const int DigitalSignatureErrorInvalidTemplatePositionKind = 602;
        public const int DigitalSignatureErrorTemplatePositionOutOfPage = 603;
        public const int DigitalSignatureErrorSigningBatchNotFound = 604;
        public const int DigitalSignatureErrorSignedFolderNameRequired = 605;
        public const int DigitalSignatureErrorProjectBatchRejectsUpload = 606;
        public const int DigitalSignatureErrorSigningBatchStarted = 607;
        public const int DigitalSignatureErrorSigningBatchRunning = 608;
        public const int DigitalSignatureErrorCertificateRequired = 609;
        public const int DigitalSignatureErrorSigningBatchFileNotFound = 610;
        public const int DigitalSignatureErrorSigningBatchFileNotDeletable = 611;
        public const int DigitalSignatureErrorInvalidTemplateImage = 612;
        public const int DigitalSignatureErrorPluginSetupMissing = 613;
        public const int DigitalSignatureErrorInvalidCertificate = 614;
        public const int DigitalSignatureErrorUntrustedCertificate = 615;
        public const int DigitalSignatureErrorSigningBatchNotOwned = 616;
        public const int DigitalSignatureErrorTooManyUploadFiles = 617;
        public const int DigitalSignatureErrorSigningBatchNotSignable = 618;
        public const int DigitalSignatureErrorSigningSessionNotOpen = 619;
        public const int DigitalSignatureErrorSigningCertificateMismatch = 620;
        public const int DigitalSignatureErrorSigningBatchEmpty = 621;
        public const int DigitalSignatureErrorSigningBatchNotRunning = 622;
        public const int DigitalSignatureErrorSigningBatchFinished = 623;
        public const int DigitalSignatureErrorSignedFileNotReady = 624;
        public const int DigitalSignatureErrorPdfUnsupported = 625;
        public const int DigitalSignatureErrorPdfEncrypted = 626;
        public const int DigitalSignatureErrorPdfLocked = 627;
        public const int DigitalSignatureErrorPdfAlreadySigned = 628;
        public const int DigitalSignatureErrorSignatureTooLarge = 629;
        public const int DigitalSignatureErrorTimestampFailed = 630;
        public const int DigitalSignatureErrorSignatureInvalid = 631;
        public const int DigitalSignatureErrorSignatureRejected = 632;
        public const int DigitalSignatureErrorTemplateImageUnreadable = 633;
        public const int DigitalSignatureErrorSigningRequestTimeout = 634;
        public const int DigitalSignatureErrorAppearanceBuildFailed = 635;

        public const int PackagingErrorPackageTypeInvalid = 701;
        public const int PackagingErrorMetadataTypeInvalid = 702;
        public const int PackagingErrorSchemaFieldNotFound = 703;

        public const int OperationErrorStaffGroupNotFound = 801;
        public const int OperationErrorStaffGroupNameExists = 802;
        public const int OperationErrorStaffGroupForbidden = 803;
        public const int OperationErrorUserInvalidRole = 804;
        public const int OperationErrorNotProjectLeader = 805;
        public const int OperationErrorMemberNotFound = 806;
        public const int OperationErrorMemberHasActiveTicket = 807;
        public const int OperationErrorPriceInvalid = 808;
        public const int OperationErrorCompanyPriceForbidden = 809;
        public const int OperationErrorEffectiveFromInvalid = 810;
        public const int OperationErrorAssigneeNotMember = 811;
        public const int OperationErrorAssignmentConflict = 812;
        public const int OperationErrorAssignmentNodeEmpty = 813;
        public const int OperationErrorTicketNotFound = 815;
        public const int OperationErrorTicketNotActive = 816;
        public const int OperationErrorNothingToRevoke = 817;
        public const int OperationErrorProjectClosed = 818;
        public const int OperationErrorDateRangeInvalid = 819;
        public const int OperationErrorSubmissionNotAssigned = 820;
        public const int OperationErrorLeaderForbiddenTarget = 821;
        public const int OperationErrorPriceDefaultMissing = 822;
        public const int OperationErrorLeaderNotMember = 823;

    }
}
