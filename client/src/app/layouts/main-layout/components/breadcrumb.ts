import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { MENU } from '@/app/core/data';
import { AppMenuItem } from '@/app/core/models';

interface Miga {
    label: string;
    ruta?: string;
}

/**
 * Migas a partir de MENU. Si la URL es más profunda que la entrada del menú
 * (ej. /personas/abc), agrega como última miga el `data.breadcrumb` de la ruta:
 *   { path: 'personas/:publicId', data: { breadcrumb: 'Detalle' }, loadComponent: ... }
 */
@Component({
    selector: 'app-breadcrumb',
    imports: [RouterLink],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <nav aria-label="Ruta" class="text-sm text-muted-color">
            <ol class="flex flex-wrap items-center gap-2">
                @for (miga of migas(); track $index; let ultima = $last) {
                    <li class="flex items-center gap-2" [class.text-color]="ultima" [class.font-medium]="ultima">
                        @if (miga.ruta && !ultima) {
                            <a [routerLink]="miga.ruta" class="hover:text-primary">{{ miga.label }}</a>
                        } @else {
                            <span>{{ miga.label }}</span>
                        }
                        @if (!ultima) {
                            <i class="pi pi-angle-right text-xs"></i>
                        }
                    </li>
                }
            </ol>
        </nav>
    `
})
export class Breadcrumb {
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    protected readonly migas = toSignal(
        this.router.events.pipe(
            filter((e) => e instanceof NavigationEnd),
            startWith(null),
            map(() => this.calcular())
        ),
        { initialValue: [] as Miga[] }
    );

    private calcular(): Miga[] {
        const url = this.router.url.split('?')[0];
        const migas = buscar(MENU, url);

        const ultima = migas.at(-1);
        if (ultima?.ruta !== url) {
            const hoja = this.breadcrumbDeRuta();
            if (hoja) migas.push({ label: hoja });
        }
        return migas;
    }

    private breadcrumbDeRuta(): string | null {
        let r = this.route;
        while (r.firstChild) r = r.firstChild;
        return r.snapshot.data?.['breadcrumb'] ?? null;
    }
}

/** La entrada de menú cuya ruta es el prefijo más largo de la URL, con sus grupos padre. */
function buscar(items: AppMenuItem[], url: string): Miga[] {
    let mejor: Miga[] = [];
    let mejorLargo = -1;

    for (const item of items) {
        const ruta = rutaDe(item);
        if (ruta !== null) {
            const coincide = ruta === url || (ruta !== '/' && url.startsWith(ruta + '/'));
            if (coincide && ruta.length > mejorLargo) {
                mejorLargo = ruta.length;
                mejor = [{ label: item.label ?? '', ruta }];
            }
        } else if (item.items?.length) {
            const sub = buscar(item.items, url);
            const largo = sub.at(-1)?.ruta?.length ?? -1;
            if (sub.length && largo > mejorLargo) {
                mejorLargo = largo;
                // El grupo solo se muestra si no se llama igual que su hijo (ej. "Inicio > Inicio").
                mejor = item.label && item.label !== sub[0].label ? [{ label: item.label }, ...sub] : sub;
            }
        }
    }
    return mejor;
}

function rutaDe(item: AppMenuItem): string | null {
    const link = item.routerLink;
    if (!link) return null;
    return Array.isArray(link) ? link.join('/').replace(/^\/\//, '/') : String(link);
}
