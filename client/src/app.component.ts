import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule, ToastModule, ConfirmDialogModule],
    // p-toast y p-confirmdialog una sola vez aquí: los usa AvisosService.
    template: `
        <router-outlet />
        <p-toast position="top-right" />
        <p-confirmdialog />
    `
})
export class AppComponent {}
