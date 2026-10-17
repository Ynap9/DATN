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

        public const int ConstructionErrorDocumentTypeNotFound = 301;
        public const int ConstructionErrorDocumentFieldNotFound = 302;
        public const int ConstructionErrorProjectNotFound = 303;
        public const int ConstructionErrorProjectConfigNotFound = 304;

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

    }
}
