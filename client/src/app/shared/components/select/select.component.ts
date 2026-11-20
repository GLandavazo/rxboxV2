import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';

/**
 * p-select con filtro y limpiar, para listas de objetos.
 *   <app-select [items]="metodos()" descriptionField="nombre" [(value)]="metodo" />
 * Con Reactive Forms usa p-select directo con formControlName.
 */
@Component({
    selector: 'app-select',
    imports: [FormsModule, SelectModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <p-select
            [options]="items()"
            [ngModel]="value()"
            (ngModelChange)="value.set($event)"
            [optionLabel]="descriptionField()"
            [filter]="true"
            [filterBy]="descriptionField()"
            [showClear]="!required()"
            [placeholder]="placeholder()"
            [disabled]="disabled()"
            [invalid]="invalid()"
            appendTo="body"
            fluid
        />
    `
})
export class SelectComponent<T extends object = Record<string, unknown>> {
    readonly items = input.required<T[]>();
    readonly descriptionField = input('nombre');
    readonly required = input(false);
    readonly disabled = input(false);
    readonly invalid = input(false);
    readonly placeholder = input('Selecciona una opción');
    readonly value = model<T | null>(null);
}
