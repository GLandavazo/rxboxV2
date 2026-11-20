/**
 * Fechas para el backend (DateOnly de .NET viaja como "yyyy-MM-dd").
 * Usar en vez de `toISOString()`, que convierte a UTC y en Hermosillo (UTC-7)
 * puede cambiar el día después de las 17:00.
 */

/** Date → "yyyy-MM-dd" con valores locales. Al MANDAR una fecha al API. */
export function toDateOnly(fecha: Date | null | undefined): string | null {
    if (!fecha) return null;
    const mm = String(fecha.getMonth() + 1).padStart(2, '0');
    const dd = String(fecha.getDate()).padStart(2, '0');
    return `${fecha.getFullYear()}-${mm}-${dd}`;
}

/** "yyyy-MM-dd" → Date local a medianoche. Al RECIBIR un DateOnly para un p-datepicker. */
export function fromDateOnly(valor: string | null | undefined): Date | null {
    if (!valor) return null;
    const [y, m, d] = valor.split('-').map(Number);
    return new Date(y, m - 1, d);
}
