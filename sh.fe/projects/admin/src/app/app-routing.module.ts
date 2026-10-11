import { AuthGuard } from './../../../shared/guard/auth.guard';
import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AppLayout } from './core/layout/app-layout/app-layout';
import { HNotFoundComponent } from '../../../lib-components/src/my-lib-components/components/h-not-found-component/h-not-found-component';

export const routes: Routes = [
    { path: '', redirectTo: '/admin', pathMatch: 'full' },
    {
        canActivate: [AuthGuard],
        path: 'auth',
        loadChildren: () => import('./modules/auth/auth.module').then((m) => m.AuthModule)
    },
    {
        path: 'admin',
        component: AppLayout,
        canActivate: [AuthGuard],
        children: [
            {
                path: '',
                loadChildren: () => import('./modules/admin/admin.module').then((m) => m.AdminModule)
            }
        ]
    },
    { path: '**', component: HNotFoundComponent }
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
})
export class AppRoutingModule { }
