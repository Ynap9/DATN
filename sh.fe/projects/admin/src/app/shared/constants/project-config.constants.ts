// Khớp ProjectConfigConst của BE: tên dòng là khoá BE tra cứu
export const PROJECT_CONFIG_NAME = {
    GOOGLE_CLIENT_ID: 'GoogleClientId',
    GOOGLE_CLIENT_SECRET: 'GoogleClientSecret',
    GOOGLE_REDIRECT_URI: 'GoogleRedirectUri',
    GOOGLE_FOLDER_ID: 'GoogleFolderId',
    GOOGLE_REFRESH_TOKEN: 'GoogleRefreshToken',
    DEADLINE_WARNING_DAYS: 'DeadlineWarningDays',
    DEADLINE_MAIL_ENABLED: 'DeadlineMailEnabled',
} as const;

// BE trả giá trị này cho secret đang có giá trị
export const PROJECT_CONFIG_MASKED = '******';

export const GOOGLE_CALLBACK_PATH = '/api/v1/construction/project-config/google-callback';

// Query ?google= BE gắn khi google-callback chuyển về màn Cấu hình (Task 50A)
export const GOOGLE_CALLBACK_RESULT = {
    SUCCESS: 'success',
    ERROR: 'error',
} as const;

// Mã lỗi BE (ErrorCodes) màn Cấu hình cần xử lý riêng
export const PROJECT_CONFIG_ERROR_CODE = {
    NAME_EXISTS: 409,
} as const;

// BE dùng khi DeadlineWarningDays trống / sai
export const DEADLINE_WARNING_DAYS_DEFAULT = 3;
