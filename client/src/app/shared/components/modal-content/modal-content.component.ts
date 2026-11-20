import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DynamicDialogRef } from 'primeng/dynamicdialog';

/**
 * Cuerpo + pie para componentes abiertos con DialogService (p-dynamicdialog).
 * El título va en la configuración: `dialog.open(MiForm, { header: 'Agregar', modal: true })`.
 *
 *   <app-modal-content>
 *     ...formulario...
 *     <div footer>
 *       <p-button label="Cancelar" [text]="true" (onClick)="modal.close()" />
 *       <p-button label="Guardar" (onClick)="guardar()" />
 *     </div>
 *   </app-modal-content>
 */
@Component({
    selector: 'app-modal-content',
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div class="flex flex-col gap-4">
            <ng-content />
        </div>
        <div class="flex justify-end gap-2 mt-6">
            <ng-content select="[footer]" />
        </div>
    `
})
export class ModalContentComponent {
    readonly dialog = inject(DynamicDialogRef);

    close(): void {
        this.dialog.close();
    }

    /** Cierra avisando éxito: quien abrió hace `ref.onClose.subscribe(ok => ok && recargar())`. */
    closeSuccess(): void {
        this.dialog.close(true);
    }
}
