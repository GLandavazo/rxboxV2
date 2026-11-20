import { Injectable, inject } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';

type Severidad = 'success' | 'info' | 'warn' | 'error';

interface OpcionesConfirmar {
    titulo?: string;
    aceptar?: string;
    cancelar?: string;
    /** Botón rojo para acciones destructivas (desactivar, eliminar). */
    peligro?: boolean;
}

/**
 * Reemplaza a cAlert / cConfirm / toast de sicogu (SweetAlert + toastr) con PrimeNG.
 * Necesita <p-toast /> y <p-confirmdialog /> en AppComponent y los dos servicios en providers.
 *
 *   avisos.toast('Persona guardada');
 *   if (await avisos.confirmar('¿Desactivar a Ana?', { peligro: true })) { ... }
 */
@Injectable({ providedIn: 'root' })
export class AvisosService {
    private readonly messages = inject(MessageService);
    private readonly confirmation = inject(ConfirmationService);

    toast(texto: string, severidad: Severidad = 'success', titulo?: string): void {
        this.messages.add({ severity: severidad, summary: titulo ?? TITULOS[severidad], detail: texto, life: 5000 });
    }

    confirmar(mensaje: string, opciones: OpcionesConfirmar = {}): Promise<boolean> {
        return new Promise((resolve) =>
            this.confirmation.confirm({
                header: opciones.titulo ?? 'Confirmar',
                message: mensaje,
                icon: 'pi pi-exclamation-triangle',
                acceptButtonProps: { label: opciones.aceptar ?? 'Aceptar', severity: opciones.peligro ? 'danger' : undefined },
                rejectButtonProps: { label: opciones.cancelar ?? 'Cancelar', text: true, severity: 'secondary' },
                accept: () => resolve(true),
                reject: () => resolve(false)
            })
        );
    }
}

const TITULOS: Record<Severidad, string> = {
    success: 'Listo',
    info: 'Aviso',
    warn: 'Atención',
    error: 'Error'
};
