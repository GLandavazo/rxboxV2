import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { AppFloatingConfigurator } from '@/app/layouts/main-layout/components/app.floatingconfigurator';
import { AuthService } from '@/app/core/services';
import { mensajeDeError } from '@/app/shared/functions';

/** Elegir box tras un login con acceso a varios. La maqueta final la genera Stitch. */
@Component({
    selector: 'app-seleccionar-box',
    standalone: true,
    imports: [ButtonModule, MessageModule, AppFloatingConfigurator],
    template: `
        <app-floating-configurator />
        <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-screen overflow-hidden">
            <div class="w-full max-w-md bg-surface-0 dark:bg-surface-900 rounded-3xl py-12 px-8 sm:px-12 shadow-sm">
                <div class="text-center mb-8">
                    <div class="text-surface-900 dark:text-surface-0 text-2xl font-medium mb-2">¿A qué box entras?</div>
                    <span class="text-muted-color">Tienes acceso a varios.</span>
                </div>
                @if (error()) {
                    <p-message severity="error" styleClass="mb-4">{{ error() }}</p-message>
                }
                <div class="flex flex-col gap-3">
                    @for (box of boxes; track box.publicId) {
                        <p-button [label]="box.nombre" [badge]="box.rol === 'ADMIN' ? 'Admin' : 'Coach'" severity="secondary" [outlined]="true" styleClass="w-full justify-between" [loading]="eligiendo() === box.publicId" [disabled]="!!eligiendo()" (onClick)="elegir(box.publicId)" />
                    }
                </div>
            </div>
        </div>
    `
})
export class SeleccionarBox {
    private readonly auth = inject(AuthService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    readonly boxes = this.auth.seleccionPendiente()?.boxes ?? [];
    readonly eligiendo = signal<string | null>(null);
    readonly error = signal<string | null>(null);

    async elegir(boxPublicId: string): Promise<void> {
        this.eligiendo.set(boxPublicId);
        this.error.set(null);
        try {
            await this.auth.seleccionarBox(boxPublicId);
            await this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('volver') ?? '/');
        } catch (e) {
            // El selectionToken dura 5 minutos: si venció, hay que volver a entrar.
            if (e instanceof HttpErrorResponse && e.status === 401) {
                await this.router.navigate(['/auth/login'], { queryParams: { expirada: 1 } });
                return;
            }
            this.error.set(mensajeDeError(e));
        } finally {
            this.eligiendo.set(null);
        }
    }
}
