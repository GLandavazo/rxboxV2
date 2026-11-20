import { Routes } from '@angular/router';
import { AppLayout } from './app/layouts/main-layout/app.layout';
import { PublicLayout } from './app/layouts/public-layout/public-layout';
import { Notfound } from './app/pages/notfound/notfound';
import { authGuard, roleGuard } from './app/core/guards';

export const appRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        // roleGuard lee data.roles de cada hija: { path: 'catalogos', data: { roles: ['ADMIN'] }, ... }
        canActivateChild: [roleGuard],
        children: [
            // Inicio temporal: el dashboard de Sakai hasta que llegue la pantalla de RxBox.
            { path: '', loadComponent: () => import('./app/pages/dashboard/dashboard').then((m) => m.Dashboard) },
            // Demos de Sakai
            { path: 'uikit', loadChildren: () => import('./app/pages/uikit/uikit.routes') },
            { path: 'documentation', loadComponent: () => import('./app/pages/documentation/documentation').then((m) => m.Documentation) },
            { path: 'pages', loadChildren: () => import('./app/pages/pages.routes') }
        ]
    },
    {
        path: '',
        component: PublicLayout,
        children: [
            { path: 'landing', loadComponent: () => import('./app/pages/landing/landing').then((m) => m.Landing) },
            { path: 'notfound', component: Notfound },
            { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') }
        ]
    },
    { path: '**', redirectTo: '/notfound' }
];
