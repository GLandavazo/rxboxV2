import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Páginas sin menú: login, selección de box, landing, errores, check-in del kiosco. */
@Component({
    selector: 'app-public-layout',
    imports: [RouterOutlet],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `<router-outlet />`
})
export class PublicLayout {}
