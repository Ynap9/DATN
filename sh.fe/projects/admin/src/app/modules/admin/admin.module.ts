import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MyLibComponentsModule } from '../../../../../lib-components/src/public-api';
import { AdminRoutingModule } from './admin-routing.module';

@NgModule({
    declarations: [],
    imports: [
        CommonModule,
        AdminRoutingModule,
        ReactiveFormsModule,
        FormsModule,
        MyLibComponentsModule,
    ]
})
export class AdminModule { }
