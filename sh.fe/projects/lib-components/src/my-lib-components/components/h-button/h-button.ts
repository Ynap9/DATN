import { booleanAttribute, ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';

@Component({
    selector: 'h-button',
    standalone: false,
    templateUrl: './h-button.html',
    styleUrl: './h-button.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HButtonComponent implements OnChanges {

    constructor(
        private cd: ChangeDetectorRef
    ) {

    }
    @Input() label: string;
    @Input() isPrimary: boolean = true;
    @Input() imgIcon: string;
    @Input() type: string | 'button' | 'reset' | 'submit' = 'button';
    @Input() icon: string;
    @Input() iconType: 'create' | 'create-s' | 'edit' | 'save' | 'saveIcon' | 'cancel' | 'cancelIcon' | 'upload' | 'info' | 'active' | 'remove' | 'download' | 'request' | 'approve' | 'approveOrCancel' | 'back' | 'open' | 'close';
    @Input() iconPos: 'left' | 'right' = 'left';
    @Input() badge: string;
    @Input() disabled: boolean = false;
    @Input() loading: boolean = false;
    @Input() severity: 'success' | 'info' | 'warning' | 'danger' | 'help' | 'primary' | 'secondary' | 'contrast' | null | undefined = 'primary';
    @Input({ transform: booleanAttribute }) link: boolean = false;
    @Input() styleClass: string;
    @Input() style: Object;
    @Input() badgeClass: string;
    @Input() raised: boolean = false;
    @Input() rounded: boolean = false;
    @Input() text: boolean = false;
    @Input() outlined: boolean = false;
    @Input() size: 'small' | 'large' | undefined = null;
    @Input() fullWidth: boolean = false;
    @Input() routerLink: boolean = false;

    @Output() onClick = new EventEmitter<any>(null);
    @Output() onFocus = new EventEmitter<any>(null);
    @Output() onBlur = new EventEmitter<any>(null);


    severityClass: string = '';
    raisedClass: string = '';
    textClass: string = '';
    outlinedClass: string = '';
    roundedClass: string = '';
    sizeClass: string = 'p-button-normal';
    cancelClass: string = '';
    fullWidthClass: string = '';
    systemStyleClass: string = 'system-style';

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['severity']?.currentValue) {
            this.severityClass = 'p-button-' + this.severity;
        }
        if (changes['raised']?.currentValue) {
            this.raisedClass = 'p-button-raised';
        }
        if (changes['rounded']?.currentValue) {
            this.roundedClass = 'p-button-rounded';
        }
        if (changes['text']?.currentValue) {
            this.textClass = 'p-button-text';
        }
        if (changes['outlined']?.currentValue) {
            this.outlinedClass = 'p-button-outlined';
        }
        if (changes['size']?.currentValue) {
            this.sizeClass = 'p-button-' + this.size;
        }
        if (changes['fullWidth']?.currentValue) {
            this.fullWidthClass = 'w-full';
        }
        if (changes['iconType']?.currentValue) {
            switch (this.iconType) {
                case 'create':
                    this.icon = 'pi pi-plus';
                    this.label = this.label || 'Thêm mới';
                    break;
                case 'create-s':
                    this.icon = 'pi pi-plus';
                    this.label = this.label || 'Thêm mới';
                    this.styleClass = 'btn-secondary ' + this.styleClass;
                    break;
                case 'edit':
                    this.icon = 'pi pi-pencil';
                    this.label = this.label || 'Chỉnh sửa';
                    break;
                case 'save':
                    break;
                case 'saveIcon':
                    this.icon = 'pi pi-save';
                    break;
                case 'cancel':
                    this.cancelClass = 'p-button-cancel';
                    break;
                case 'cancelIcon':
                    this.icon = 'pi pi-times';
                    this.label = this.label || 'Hủy kích hoạt'
                    break;
                case 'upload':
                    this.icon = 'pi pi-upload';
                    this.styleClass = 'btn-upload btn-secondary ' + this.styleClass
                    break;
                case 'info':
                    this.icon = 'pi pi-info-circle';
                    break;
                case 'active':
                    this.icon = 'pi pi-check-circle';
                    this.label = this.label || 'Kích hoạt'
                    break;
                case 'remove':
                    this.icon = 'pi pi-trash';
                    break;
                case 'download':
                    this.icon = 'pi pi-download';
                    break;
                case 'request':
                    this.icon = 'pi pi-arrow-up';
                    this.label = this.label || 'Trình duyệt'
                    break;
                case 'approve':
                    this.icon = 'pi pi-verified';
                    this.label = this.label || 'Phê duyệt'
                    break;
                case 'approveOrCancel':
                    this.icon = 'pi pi-file-edit';
                    this.label = this.label || 'Xử lý yêu cầu'
                    break;
                case 'open':
                    this.icon = 'pi pi-lock-open';
                    break;
                case 'close':
                    this.icon = 'pi pi-lock';
                    break;
                case 'back':
                    this.icon = 'pi pi-chevron-left';
                    this.label = this.label || 'Quay lại'
                    break;
                default:
            }
        }

        if (changes['isPrimary']?.currentValue === false) {
            this.styleClass = 'btn-secondary ' + this.styleClass;
        }

        if (changes['icon']?.currentValue) {
            this.icon = changes['icon']?.currentValue;
        }
    }
}
