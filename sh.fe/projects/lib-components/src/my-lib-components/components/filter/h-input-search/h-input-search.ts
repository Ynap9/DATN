import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { debounceTime, Subject } from 'rxjs';

@Component({
    selector: 'h-input-search',
    standalone: false,
    templateUrl: './h-input-search.html',
    styleUrl: './h-input-search.scss',
})
export class HInputSearch implements OnInit {

    constructor(
        private ref: ChangeDetectorRef,
    ) { }
    @Input() placeholder: string;
    @Input() width: number;
    @Input() class: string;
    @Input() isShowLabel: boolean = true;
    @Input() label: string = '';
    @Input() maxLength: number = 100;
    @Input() blockEmoji: boolean = true;
    @Input() keyword: string;
    @Input() showClear: boolean = true;
    @Output() keywordChange = new EventEmitter<string>();
    @Output() _onChange = new EventEmitter<string>();
    @Output() _keyup = new EventEmitter<string>();
    isShow: boolean = true;
    keywordOld: string;
    id: string;
    changeKeywordSubject = new Subject<any>();
    @Input() disabled: boolean = false;
    @Output() _invalidKeyword = new EventEmitter<boolean>();
    isInvalid: boolean;
    modelChange: Subject<any> = new Subject<any>();
    debounceTime = 1000;

    ngOnInit(): void {
        this.id = new Date().getTime().toString();
        this.modelChange.pipe(debounceTime(this.debounceTime)).subscribe((res: { value: string, issetEmoji: boolean }) => {
            this.changeKeyword(res.value, res.issetEmoji);
        })
    }

    inputChange(value?: string) {
        this._keyup.emit(value);
        const emoij: RegExp = /<a?:.+?:\d{18}>|\p{Extended_Pictographic}/gu;
        const issetEmoji = !!(this.blockEmoji && emoij.test(value));
        if (value && issetEmoji) value = value.replace(emoij, '');
        let valueTrim = value ? value.trim() : null;
        if (issetEmoji) {
            setTimeout(() => this.keyword = valueTrim);
        }
        this.modelChange.next({ value: valueTrim, issetEmoji: issetEmoji });
    }

    changeKeyword(value: string, issetEmoji: boolean) {
        setTimeout(() => {
            if ((!issetEmoji && (value || (this.keywordOld && !value))) || (issetEmoji && value !== this.keywordOld) || !this.keywordOld) {
                this.keywordChange.emit(value);
                this._onChange.emit(value);
                this.keywordOld = value;
                this.keyword = value;
                if (this.isInvalid) this.checkValid();
            }
        }, 0);
    }

    clearData() {
        this.keyword = '';
    }

    checkValid() {
        if (this.keyword) {
            this.isInvalid = false;
            this._invalidKeyword.emit(this.isInvalid);
        } else {
            this.isInvalid = true;
            this._invalidKeyword.emit(this.isInvalid);
        }
    }
}
