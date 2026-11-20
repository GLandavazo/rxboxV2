import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AppState } from '@/app/shared/states';
import { Breadcrumb } from './breadcrumb';

/**
 * Marco de cada página: migas, título (que también se muestra en el topbar) y acciones.
 *   <app-page-content pageTitle="Personas" subtitle="Atletas y coaches del box">
 *     <ng-container acciones><p-button label="Agregar" /></ng-container>
 *     ...contenido...
 *   </app-page-content>
 */
@Component({
    selector: 'app-page-content',
    imports: [Breadcrumb],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <section class="flex flex-col gap-4">
            <app-breadcrumb />
            @if (pageTitle()) {
                <header class="flex flex-wrap items-end gap-3">
                    <div>
                        <h1 class="text-2xl font-semibold m-0">{{ pageTitle() }}</h1>
                        @if (subtitle()) {
                            <p class="text-muted-color m-0 mt-1">{{ subtitle() }}</p>
                        }
                    </div>
                    <div class="ml-auto flex flex-wrap gap-2">
                        <ng-content select="[acciones]" />
                    </div>
                </header>
            }
            <ng-content />
        </section>
    `
})
export class PageContent {
    readonly pageTitle = input('');
    readonly subtitle = input('');

    constructor() {
        const app = inject(AppState);
        const titulo = inject(Title);
        effect(() => {
            app.set('pageTitle', this.pageTitle());
            titulo.setTitle(this.pageTitle() ? `${this.pageTitle()} · RxBox` : 'RxBox');
        });
    }
}
