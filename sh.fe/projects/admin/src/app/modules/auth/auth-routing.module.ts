import { HLoginFeComponent } from './../../../../../lib-components/src/my-lib-components/components/h-login-fe/h-login-fe';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AccountComponent } from './auth.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                      { path: 'login', component: HLoginFeComponent },
                ]
            }
        ])
    ],
    exports: [
        RouterModule
    ]
})
export class AccountRoutingModule { }
