import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountComponent } from './auth.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AccountRoutingModule } from './auth-routing.module';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ToastModule } from 'primeng/toast';
import { MyLibComponentsModule } from '../../../../../lib-components/src/my-lib-components/my-lib-components.module';
@NgModule({
    declarations: [AccountComponent],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        AccountRoutingModule,
        ButtonModule,
        InputTextModule,
        PasswordModule,
        ToastModule,
        MyLibComponentsModule
    ],
    exports: [
        AccountComponent
    ]
})
export class AuthModule { }
