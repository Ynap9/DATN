import { Confirmation } from "primeng/api";

export enum StatusResonse {
    SUCCESS = 1,
    ERROR = 0,
}

export enum EYesNo {
    YES = "Y",
    NO = "N",
}

export enum EActiveDeactive {
    ACTIVE = 1,
    DEACTIVE = 2,
}

export enum EUnitTime {
    DAY = 'D',
    MONTH = 'M',
    YEAR = 'Y',
    QUARTER = 'Q',
}

export enum EAction {
    ADD = 1,
    UPDATE = 2,
    DELETE = 3,
}

export enum EFormatDate {
    DATE_DMY = 'DD-MM-YYYY',
    DATE_DMY_Hms = 'DD-MM-YYYY HH:mm:ss',
    DATE_DMY_Hm = 'DD-MM-YYYY HH:mm',
    DATE_YMD_Hms = 'YYYY-MM-DDTHH:mm:ss',
}

export enum EFormatDateDisplay {
    DATE_DMY = 'DD/MM/YYYY',
    DATE_DMY_Hms = 'DD/MM/YYYY HH:mm:ss',
    DATE_DMY_Hm = 'DD/MM/YYYY HH:mm',
    DATE_YMD_Hms = 'YYYY/MM/DDTHH:mm:ss',
}

export enum EIconConfirm {
    APPROVE = '/shared/assets/layout/images/icon-dialog-confirm/approve.svg',
    DELETE = '/shared/assets/layout/images/icon-dialog-confirm/delete.svg',
    WARNING = '/shared/assets/layout/images/icon-dialog-confirm/warning.svg',
    QUESTION = '/shared/assets/layout/images/icon-dialog-confirm/question.svg',
}

export enum ContentTypeEView {
    MARKDOWN = 'MARKDOWN',
    HTML = 'HTML',
    IMAGE = "IMAGE",
    FILE = "FILE",
}

export enum EAcceptFile {
    ALL = '',
    IMAGE = 'image',
    VIDEO = 'video',
    MEDIA = 'media',
}

export enum EMediaTypeEnum {
    Image = 1,
    Video = 2,
    File = 3,
}

export enum DefaultImage {
    IMG_ADD = 'shared/assets/layout/images/default-media-image/add-image-bg.png',
    IMG_DEFAUT = 'shared/assets/layout/images/default-media-image/image-bg-default.png',
    IMG_FRONT = 'shared/assets/layout/images/front-image.png',
    IMG_BACK = 'shared/assets/layout/images/back-image.png',
    AVATAR = 'shared/assets/layout/images/avatar/anonymous-avatar.jpg',
    MEDIA = 'shared/assets/layout/images/default-media-image/BACKGROUND_IMAGE_DEFAULT.svg',
    LOGO_EPIC = 'shared/assets/layout/images/logo/logo-epic.svg',
    PAGE_NOT_FOUND = 'shared/assets/layout/images/404NotFound/404NotFound.svg',
    IMG_AVATAR_DEFAULT = 'shared/assets/layout/images/avatar.png',
    AVATAR_DEFAULT = 'shared/assets/layout/images/avatar.png',
    VIDEO_DEFAULT = 'shared/assets/layout/images/default-media-image/videoTemplate.png'
}

export enum ESex {
    MALE = 1,
    FEMALE = 2,
}

export class BaseConsts {
    static readonly imageExtensions = ['jpg', 'jpeg', 'png', 'bmp'];
    static readonly fileExtensions = ['pdf', 'xlsx', 'xls', 'doc', 'docx', 'pptx', 'dwg', 'kmz', 'mp3', 'wma'];
    static readonly videoExtensions = ['mp4', 'avi', 'mkv', 'wmv', 'vob', 'flv', 'wmv9', 'mpeg', '3gp', 'webm', 'hevc', 'mov', 'mpg', '3gpp', 'mpeg1', 'mpeg2', 'mpeg4', 'mpegps'];
    static readonly imageExtensionStrings = '.png, .jpg, .svg, .jpeg, .webp';
    static readonly fileExtensionStrings = '.pdf, .xlsx, .xls, .doc, .docx, .repx';
    static readonly videoExtensionStrings = '.mp4, .avi, .mkv, .wmv, .vob, .flv, .wmv9, .mpeg, .3gp, .webm, .hevc, .mov, .mpg, .3gpp, .mpeg1, .mpeg2, .mpeg4, .mpegps';
    static readonly pageContentId = "page-content";
    static readonly separatorHistory = 'rowSeparator';

    static readonly backLinkParam = 'backLink';
    static readonly methodApiPost = ['POST', 'PUT', 'PATCH'];

    static readonly messageError = "Có lỗi xảy ra. Vui lòng thử lại sau ít phút!";

    static readonly maxSizeUpdate = 10485760;
    static readonly PAGINATOR_MIN_LENGTH_SHOW = 25;
    static readonly DEBOUNCE_TIME = 1200;
    static readonly heightHeaderDefault = 168;

    static readonly messageOnUpload = "Đang upload file ..."
    static readonly emptyMessage = "Không có dữ liệu"

    static pdfType = 'application/pdf';
    static docxType = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
    static docType = 'application/msword';
    static excelType = 'application/vnd.ms-excel';

    static readonly folder = 'core';

    static redicrectHrefOpenDocs = "https://docs.google.com/viewerng/viewer?url=";

    static keyCrypt = 'idCrypt';

    static defaultAvatar = "assets/layout/images/topbar/anonymous-avatar.jpg";

}

export class UserTypes {

}

export class SuccessMessage {
    public static SUCCESS_ADD = "Thêm mới thành công";
    public static SUCCESS_UPDATE = "Chỉnh sửa thành công";
    public static SUCCESS_DELETE = "Xóa thành công";
    public static SUCCESS_ADD_REQUEST = "Yêu cầu thêm mới thành công";
    public static SUCCESS_UPDATE_REQUEST = "Yêu cầu chỉnh sửa thành công";
    public static SUCCESS_DELETE_REQUEST = "Xóa thành công";
    public static SUCCESS_VERIFY_REQUEST = "Yêu cầu xác minh tài khoản thành công";
}

export class SexConst {
    public static list = [
        {
            name: 'Nam',
            code: ESex.MALE,
        },
        {
            name: 'Nữ',
            code: ESex.FEMALE,
        },
    ];

    public static getValue(code) {
        const item = this.list.find(s => s.code === code);
        return item?.name;
    }
}

export class ErrorMessage {
    public static ERR_INVALID_FORM = "Vui lòng kiểm tra các trường thông tin!";
    public static ERR_INVALID_FORM_PERMISSTION = "Vui lòng chọn ít nhất 1 quyền";
    public static ERR_EMOJI_INVALID = "Chứa ký tự emoji không hợp lệ";
    public static ERR_NAME_BUSINESS_INVALID = "Tên doanh nghiệp chứa ký tự không hợp lệ";
    public static ERR_PHONE_INVALID = 'Số điện thoại không hợp lệ';
    public static ERR_EMAIL_INVALID = 'Email không hợp lệ';
    public static ERR_ACCOUNT_INVALID = 'Tài khoản không hợp lệ';
    public static ERR_ARRAY_INVALID = 'Trường bắt buộc nhập';
    public static ERR_JUST_ALPHABET_AND_NUM_INVALID = 'Trường không được nhập dấu và ký tự đặc biệt';
    public static ERR_LONGITUDE_LATITUDE_INVALID = 'Trường không đúng định dạng tọa độ';
}

export class ActiveDeactiveConst {
    public static ACTIVE = 1;
    public static DEACTIVE = 2;

    public static list = [
        {
            name: 'Kích hoạt',
            code: this.ACTIVE,
            class: 'tag-active',
        },
        {
            name: 'Đang khóa',
            code: this.DEACTIVE,
            class: 'tag-lock',
        }
    ];

    public static getInfo(code, atribution = null) {
        const status = this.list.find(type => type.code === code);
        return atribution ? status?.['atribution'] : status;
    }
}

export class ActiveDeactiveTextConst {

    public static ACTIVE = EActiveDeactive.ACTIVE;
    public static DEACTIVE = EActiveDeactive.DEACTIVE;

    public static list = [
        {
            name: 'Kích hoạt',
            code: EActiveDeactive.ACTIVE,
            class: 'tag-active',
        },
        {
            name: 'Đang khóa',
            code: EActiveDeactive.DEACTIVE,
            class: 'tag-lock',
            isDisableCheckbox: true
        },
    ];

    public static getInfo(code, atribution = null) {
        let status = this.list.find(type => type.code == code);
        return status && atribution ? status[atribution] : status;
    }
}

export class StatusDeleteConst {
    public static list = [
        {
            name: 'Đã xóa',
            code: 'Y',
        },
        {
            name: 'Chưa xóa',
            code: 'N',
        },
    ]

    public static DELETE_TRUE = 'Y';
    public static DELETE_FALSE = 'N';
}

export const AtributionConfirmConst: Confirmation = {
    header: 'Thông báo!',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Đồng ý',
    rejectLabel: 'Hủy',
}

export class SortConst {
    public static ASCENDING = 1;
    public static DESCENDING = -1;

    public static listSort = [
        {
            code: this.ASCENDING,
            value: 'asc',
        },
        {
            code: this.DESCENDING,
            value: 'desc',
        },
    ];

    public static getValueSort(code) {
        const sort = this.listSort.find(s => s.code == code);
        return sort ? sort.value : null;
    }
}

export class MarkDownHtmlConst {
    public static HTML = 1;
    public static MARKDOWN = 2;

    public static types = [
        {
            code: 1,
            value: 'HTML'
        },
        {
            code: 2,
            value: 'MARKDOWN'
        }
    ];

    public static getType(code) {
        const type = this.types.find(s => s.code == code);
        return type ? type.value : '';
    };

    public static getValue(value) {
        const type = this.types.find(s => s.value == value);
        return type ? type.code : '';
    }
}
export class CheckboxConsts {
    public static CHECKED = 'checked';
    public static UNCHECKED = 'unChecked';
    public static TYPE_BOOLEAN = 'Boolean';
    public static TYPE_YESNO = 'YesNo';

    public static values = [
        {
            type: this.TYPE_BOOLEAN,
            [this.CHECKED]: true,
            [this.UNCHECKED]: false,
        },
        {
            type: this.TYPE_YESNO,
            [this.CHECKED]: EYesNo.YES,
            [this.UNCHECKED]: EYesNo.NO,
        },
    ];

    public static getCheckedValues() {
        const checkedValues = this.values.map(c => c[this.CHECKED]);
        return checkedValues;
    }
}
export const keyNumbers = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];

export class StatusResponseConst {
    public static list = [
        {
            value: false,
            status: 0,
        },
        {
            value: true,
            status: 1,
        },
    ]

    public static RESPONSE_TRUE = 1;
    public static RESPONSE_FALSE = 0;
    public static HTTP_STATUS_OK = 200;
}


export enum IconConfirm {
    APPROVE = '/shared/assets/layout/images/icon-dialog-confirm/approve.svg',
    DELETE = '/shared/assets/layout/images/icon-dialog-confirm/delete.svg',
    WARNING = '/shared/assets/layout/images/icon-dialog-confirm/warning.svg',
    QUESTION = '/shared/assets/layout/images/icon-dialog-confirm/question.svg',
}
