import { Routes } from '@angular/router';
import { ShNotFoundComponent } from '@sh/ui';
import { authGuard } from '@/core/auth/guard/auth.guard';
import { AppLayout } from '@/core/layout/app-layout/app-layout';
import { HomeComponent } from './features/home/home.component';

export const routes: Routes = [
    { path: '', redirectTo: 'admin', pathMatch: 'full' },
    {
        path: 'auth',
        loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_PUBLIC_ROUTES),
    },
    {
        path: 'admin',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', component: HomeComponent },
            { path: 'auth', loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES) },
            { path: 'core', loadChildren: () => import('./features/core/core.routes').then((m) => m.CORE_ROUTES) },
            { path: 'construction', loadChildren: () => import('./features/construction/construction.routes').then((m) => m.CONSTRUCTION_ROUTES) },
            { path: 'digital-signature', loadChildren: () => import('./features/digital-signature/digital-signature.routes').then((m) => m.DIGITAL_SIGNATURE_ROUTES) },
            { path: 'packaging', loadChildren: () => import('./features/packaging/packaging.routes').then((m) => m.PACKAGING_ROUTES) },
            { path: 'operation', loadChildren: () => import('./features/operation/operation.routes').then((m) => m.OPERATION_ROUTES) },
            { path: 'ai', loadChildren: () => import('./features/ai/ai.routes').then((m) => m.AI_ROUTES) },
        ],
    },
    { path: '**', component: ShNotFoundComponent },
];
