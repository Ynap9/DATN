import { AfterViewInit, ChangeDetectionStrategy, Component, ContentChild, EventEmitter, forwardRef, Input, OnInit, Output, TemplateRef, ViewChild } from '@angular/core';
import { H_NG_VALUE_ACCESSOR, HBaseControl } from '../h-base';
import { Password } from 'primeng/password';

@Component({
    selector: 'h-password-component',
    standalone: false,
    templateUrl: './h-password-component.html',
    styleUrl: './h-password-component.scss',
    providers: [
        {
            provide: H_NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => HPasswordComponent),
            multi: true,
        }
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HPasswordComponent extends HBaseControl implements OnInit, AfterViewInit {
    @Input() toggleMask: boolean = true;
    @Input() feedback: boolean = false;
    @Input() promptLabel: string;
    @Input() mediumRegex: string = '^(((?=.*[a-z])(?=.*[A-Z]))|((?=.*[a-z])(?=.*[0-9]))|((?=.*[A-Z])(?=.*[0-9])))(?=.{6,})';
    @Input() strongRegex: string = '^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{8,})';
    @Input() weakLabel: string = null;
    @Input() mediumLabel: string = null;
    @Input() maxLength: number = null;
    @Input() strongLabel: string = null;
    @Input() appendTo: string = 'body';
    @Input() inputStyleClass: string = null;
    @Input() styleClass: string = null;
    @Input() style: Object = null;
    @Input() inputStyle: Object = null;
    @Input() showClear: boolean = false;

    @Output() onFocus = new EventEmitter<any>(null);
    @Output() onBlur = new EventEmitter<any>(null);
    @Output() onClear = new EventEmitter<any>(null);

    @ContentChild('headerTemplate') headerTemplate: TemplateRef<any>;
    @ContentChild('contentTemplate') contentTemplate: TemplateRef<any>;
    @ContentChild('footerTemplate') footerTemplate: TemplateRef<any>;

    @ViewChild('password') password: Password;

    override ngOnInit(): void {
        this.onInit();
    }

    ngAfterViewInit(): void {
        if (this.headerTemplate || this.contentTemplate || this.footerTemplate) {
            this.feedback = true;
        }
    }
}
