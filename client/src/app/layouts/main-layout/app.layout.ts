import { Component, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppTopbar } from './components/app.topbar';
import { AppSidebar } from './components/app.sidebar';
import { AppFooter } from './components/app.footer';
import { LayoutService } from '@/app/layouts/main-layout/service/layout.service';
import { LoadingComponent } from '@/app/shared/components';

@Component({
    selector: 'app-layout',
    standalone: true,
    imports: [CommonModule, AppTopbar, AppSidebar, RouterModule, AppFooter, LoadingComponent],
    template: `<div class="layout-wrapper" [ngClass]="containerClass()">
            <app-topbar></app-topbar>
            <app-sidebar></app-sidebar>
            <div class="layout-main-container">
                <div class="layout-main">
                    <router-outlet></router-outlet>
                </div>
                <app-footer></app-footer>
            </div>
            <div class="layout-mask"></div>
        </div>
        <app-loading [listener]="true" />`
})
export class AppLayout {
    layoutService = inject(LayoutService);

    constructor() {
        effect(() => {
            const state = this.layoutService.layoutState();
            if (state.mobileMenuActive) {
                document.body.classList.add('blocked-scroll');
            } else {
                document.body.classList.remove('blocked-scroll');
            }
        });
    }

    containerClass = computed(() => {
        const config = this.layoutService.layoutConfig();
        const state = this.layoutService.layoutState();
        return {
            'layout-overlay': config.menuMode === 'overlay',
            'layout-static': config.menuMode === 'static',
            'layout-static-inactive': state.staticMenuDesktopInactive && config.menuMode === 'static',
            'layout-overlay-active': state.overlayMenuActive,
            'layout-mobile-active': state.mobileMenuActive
        };
    });
}
