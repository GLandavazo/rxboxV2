/** null, undefined, '' / '   ' o arreglo vacío. */
export function IsNoE(value: unknown): boolean {
    return value == null || (typeof value === 'string' && value.trim() === '') || (Array.isArray(value) && value.length === 0);
}

/** Quita propiedades vacías (IsNoE). Útil antes de armar filtros o query params. */
export function removeNullProperties<T extends object>(obj: T): Partial<T> {
    return Object.fromEntries(Object.entries(obj).filter(([, value]) => !IsNoE(value))) as Partial<T>;
}

/** Copia solo las llaves indicadas. Ej.: mandar al PUT solo los campos editables de un formulario. */
export function filterObjectFields<T>(original: Partial<T>, keys: (keyof T)[]): Partial<T> {
    const filtered: Partial<T> = {};
    keys.forEach((key) => {
        if (original[key] !== undefined) filtered[key] = original[key];
    });
    return filtered;
}

/** Elementos presentes en ambos arreglos. */
export function matchArrays<T>(array1: T[] | null | undefined, array2: T[] | null | undefined): T[] {
    const otros = array2 ?? [];
    return (array1 ?? []).filter((element) => otros.includes(element));
}

/** true si el texto es un objeto o arreglo JSON válido. */
export function hasJsonStructure(str: unknown): boolean {
    if (typeof str !== 'string') return false;
    try {
        const tipo = Object.prototype.toString.call(JSON.parse(str));
        return tipo === '[object Object]' || tipo === '[object Array]';
    } catch {
        return false;
    }
}
