import { ModuleWithProviders, NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { CheckboxModule } from 'primeng/checkbox';
import { ImageModule } from 'primeng/image';
import { SelectButtonModule } from 'primeng/selectbutton';
import { MultiSelectModule } from 'primeng/multiselect';
import { PaginatorModule } from 'primeng/paginator';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MenuModule } from 'primeng/menu';
import { TextareaModule } from 'primeng/textarea';
import { SkeletonModule } from 'primeng/skeleton';
import { SliderModule } from 'primeng/slider';
import { KeyFilterModule } from 'primeng/keyfilter';
import { RadioButtonModule } from 'primeng/radiobutton';
import { HButtonComponent } from './components/h-button/h-button';
import { ButtonModule } from 'primeng/button';
import { HCheckboxComponent } from './components/h-checkbox/h-checkbox';
import { HConfirmComponent } from './components/h-confirm/h-confirm';
import { HInputTextComponent } from './components/form-inputs/h-input-text/h-input-text';
import { TooltipModule } from 'primeng/tooltip';
import { HIconRequiredComponent } from './components/h-icon-required/h-icon-required';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HInputNumberComponent } from './components/form-inputs/h-input-number/h-input-number';
import { InputNumberModule } from 'primeng/inputnumber';
import { HCalendarComponent } from './components/form-inputs/h-calendar/h-calendar';
import { DatePickerModule } from 'primeng/datepicker';
import { InputMaskModule } from 'primeng/inputmask';
import { HSelectComponent } from './components/form-inputs/h-select/h-select';
import { SelectModule } from 'primeng/select';
import { HLoginFeComponent } from './components/h-login-fe/h-login-fe';
import { PasswordModule } from 'primeng/password';
import { NgxSpinnerModule } from 'ngx-spinner';
import { HLoadingComponent } from './components/h-loading-component/h-loading-component';
import { ProgressBarModule } from 'primeng/progressbar';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { AcceptFilePipe } from './pipes/accept-file.pipe';
import { CurrencyPipe } from './pipes/currency.pipe';
import { DateTimePipe } from './pipes/date-time.pipe';
import { FunctionPipe } from './pipes/function.pipe';
import { ImageResizePipe } from './pipes/image-resize.pipe';
import { LabelDropdownPipe } from './pipes/label-dropdown.pipe';
import { TruncatePipe } from './pipes/truncate.pipe';
import { LibHelperService } from './shared/services/lib-helper.service';
import { HNotFoundComponent } from './components/h-not-found-component/h-not-found-component';
import { PopoverModule } from 'primeng/popover';
import { RouterModule } from '@angular/router';
import { HbreadcrumbComponent } from './components/hbreadcrumb-component/hbreadcrumb-component';
import { CardModule } from 'primeng/card';
import { HTableComponent } from './components/h-table-component/h-table-component';
import { HSettingDisplayColumnTable } from './components/h-table-component/h-setting-display-column-table/h-setting-display-column-table';
import { HTableFilterComponent } from './components/h-table-component/h-table-filter-component/h-table-filter-component';
import { HInputSearch } from './components/filter/h-input-search/h-input-search';
import { HDropdownSearch } from './components/filter/h-dropdown-search/h-dropdown-search';
import { HMultiSelect } from './components/form-inputs/h-multi-select/h-multi-select';
import { ToastModule } from 'primeng/toast';
import { HPasswordComponent } from './components/form-inputs/h-password-component/h-password-component';
import { ChartModule } from 'primeng/chart';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
@NgModule({
    declarations: [
        HButtonComponent,
        HCheckboxComponent,
        HConfirmComponent,
        HInputTextComponent,
        HIconRequiredComponent,
        HInputNumberComponent,
        HCalendarComponent,
        HSelectComponent,
        HLoginFeComponent,
        HLoadingComponent,
        AcceptFilePipe,
        CurrencyPipe,
        DateTimePipe,
        FunctionPipe,
        ImageResizePipe,
        LabelDropdownPipe,
        TruncatePipe,
        HNotFoundComponent,
        HbreadcrumbComponent,
        HTableComponent,
        HSettingDisplayColumnTable,
        HTableFilterComponent,
        HInputSearch,
        HDropdownSearch,
        HMultiSelect,
        HPasswordComponent
    ],
    imports: [
        CommonModule,
        TableModule,
        CheckboxModule,
        ImageModule,
        SelectButtonModule,
        MultiSelectModule,
        PaginatorModule,
        InputTextModule,
        TagModule,
        MenuModule,
        TextareaModule,
        SkeletonModule,
        SliderModule,
        KeyFilterModule,
        RadioButtonModule,
        ButtonModule,
        TooltipModule,
        InputNumberModule,
        FormsModule,
        DatePickerModule,
        InputMaskModule,
        ReactiveFormsModule,
        SelectModule,
        PasswordModule,
        NgxSpinnerModule,
        ProgressBarModule,
        IconFieldModule,
        InputIconModule,
        PopoverModule,
        RouterModule,
        CardModule,
        ToastModule,
        ChartModule,
        ToggleSwitchModule
    ],
    exports: [
        CommonModule,
        TableModule,
        CheckboxModule,
        ImageModule,
        SelectButtonModule,
        MultiSelectModule,
        PaginatorModule,
        InputTextModule,
        TagModule,
        MenuModule,
        TextareaModule,
        SkeletonModule,
        SliderModule,
        KeyFilterModule,
        RadioButtonModule,
        ButtonModule,
        HButtonComponent,
        HCheckboxComponent,
        HConfirmComponent,
        HInputTextComponent,
        TooltipModule,
        HIconRequiredComponent,
        HInputNumberComponent,
        InputNumberModule,
        HCalendarComponent,
        DatePickerModule,
        HSelectComponent,
        HLoginFeComponent,
        PasswordModule,
        HLoadingComponent,
        AcceptFilePipe,
        CurrencyPipe,
        DateTimePipe,
        FunctionPipe,
        ImageResizePipe,
        LabelDropdownPipe,
        TruncatePipe,
        HNotFoundComponent,
        PopoverModule,
        HbreadcrumbComponent,
        CardModule,
        HTableComponent,
        HSettingDisplayColumnTable,
        HTableFilterComponent,
        HInputSearch,
        HDropdownSearch,
        HMultiSelect,
        ToastModule,
        HPasswordComponent,
        ToggleSwitchModule
    ]
})
export class MyLibComponentsModule {
    public static forRoot(): ModuleWithProviders<MyLibComponentsModule> {
        return {
            ngModule: MyLibComponentsModule,
            providers: [
                LibHelperService,
            ],
        };
    }
}
