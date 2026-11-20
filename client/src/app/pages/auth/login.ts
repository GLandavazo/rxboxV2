import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AppFloatingConfigurator } from '@/app/layouts/main-layout/components/app.floatingconfigurator';
import { AuthService } from '@/app/core/services';
import { mensajeDeError } from '@/app/shared/functions';

/**
 * Login. La maqueta final la genera Stitch; lo que debe conservarse es la lógica:
 * formulario reactivo, error dentro del formulario (SIN_ALERTA en AuthService) y la redirección.
 */
@Component({
    selector: 'app-login',
    standalone: true,
    imports: [ButtonModule, InputTextModule, PasswordModule, MessageModule, ReactiveFormsModule, AppFloatingConfigurator],
    template: `
        <app-floating-configurator />
        <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-screen overflow-hidden">
            <div class="w-full max-w-md bg-surface-0 dark:bg-surface-900 rounded-3xl py-12 px-8 sm:px-12 shadow-sm">
                <div class="text-center mb-8">
                    <div class="text-surface-900 dark:text-surface-0 text-3xl font-medium mb-2">RxBox</div>
                    <span class="text-muted-color font-medium">Inicia sesión para continuar</span>
                </div>

                @if (expirada) {
                    <p-message severity="info" styleClass="mb-4">Tu sesión expiró. Vuelve a entrar.</p-message>
                }
                @if (error()) {
                    <p-message severity="error" styleClass="mb-4">{{ error() }}</p-message>
                }

                <form [formGroup]="form" (ngSubmit)="entrar()" class="flex flex-col gap-2">
                    <label for="email" class="font-medium">Correo</label>
                    <input pInputText id="email" type="email" autocomplete="username" formControlName="email" class="w-full" />
                    @if (form.controls.email.touched && form.controls.email.invalid) {
                        <small class="text-red-500">Escribe un correo válido.</small>
                    }

                    <label for="password" class="font-medium mt-4">Contraseña</label>
                    <p-password id="password" formControlName="password" [feedback]="false" [toggleMask]="true" autocomplete="current-password" styleClass="w-full" inputStyleClass="w-full" [fluid]="true" />
                    @if (form.controls.password.touched && form.controls.password.invalid) {
                        <small class="text-red-500">Escribe tu contraseña.</small>
                    }

                    <p-button type="submit" label="Entrar" styleClass="w-full mt-6" [loading]="enviando()" />
                </form>
            </div>
        </div>
    `
})
export class Login {
    private readonly auth = inject(AuthService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    readonly form = inject(FormBuilder).nonNullable.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', Validators.required]
    });

    readonly enviando = signal(false);
    readonly error = signal<string | null>(null);
    readonly expirada = this.route.snapshot.queryParamMap.has('expirada');

    async entrar(): Promise<void> {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        this.enviando.set(true);
        this.error.set(null);
        try {
            const { email, password } = this.form.getRawValue();
            const resultado = await this.auth.login(email, password);
            const volver = this.route.snapshot.queryParamMap.get('volver');
            if (resultado === 'elegir-box') await this.router.navigate(['/auth/seleccionar-box'], { queryParams: volver ? { volver } : {} });
            else await this.router.navigateByUrl(volver ?? '/');
        } catch (e) {
            this.error.set(e instanceof HttpErrorResponse && e.status === 401 ? 'Correo o contraseña incorrectos.' : mensajeDeError(e));
        } finally {
            this.enviando.set(false);
        }
    }
}
