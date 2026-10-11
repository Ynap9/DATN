import { IErrorCode } from "../interfaces/base.interface";

export class ErrorCode {
    public static list: IErrorCode = {
        400: 'BestRequest',
        401: 'Unauthorized',
        403: 'Forbidden',
        404: 'Not Found',
        405: 'Method Not Allowed',
        408: 'Request Time-out',
        415: 'Unsupported Media Type',
        500: 'Internal Server Error',
        502: 'Bad Gateway',
        504: 'Gateway Time-out',
    }
}

export enum EErrorCode {
    Unauthorized = 401,
    BadRequest = 400,
    Forbidden = 403,
    NotFound = 404,
    MethodNotAllowed = 405,
    RequestTimeout = 408,
    UnsupportedMediaType = 415,
    InternalServerError = 500,
    BadGateway = 502,
    GatewayTimeout = 504,
}

export class ErrorMessage {
    public static ERR_INVALID_FORM = "Vui lòng kiểm tra các trường thông tin";
    public static ERR_INVALID_FORM_PERMISSTION = "Vui lòng chọn ít nhất 1 quyền";
    public static ERR_EMOJI_INVALID = "Chứa ký tự emoji không hợp lệ";
    public static ERR_NAME_BUSINESS_INVALID = "Tên doanh nghiệp chứa ký tự không hợp lệ";
    public static ERR_PHONE_INVALID = 'Số điện thoại không hợp lệ';
    public static ERR_EMAIL_INVALID = 'Email không hợp lệ';
    public static ERR_ACCOUNT_INVALID = 'Tài khoản không hợp lệ';
    public static ERR_ARRAY_INVALID = 'Trường bắt buộc nhập';
    public static ERR_JUST_ALPHABET_AND_NUM_INVALID = 'Trường không được nhập dấu và ký tự đặc biệt';
    public static ERR_LONGITUDE_LATITUDE_INVALID = 'Trường không đúng định dạng tọa độ';
    public static ERR_EXPORT_INVALID = 'Xuất thông tin sinh viên thất bại';
    public static ERR_DELETE_FILE_INVALID = 'Xóa file thất bại';
}

