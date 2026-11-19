using System.ComponentModel;

namespace RxBox.Core.Enums
{
    public enum TipoCatalogo
    {
        [Description("Categoría de membresía")] CATEGORIA_MEMBRESIA,
        [Description("Método de pago")] METODO_PAGO
    }

    public enum TipoCobertura
    {
        ILIMITADA,
        PAQUETE,
        VISITA
    }

    public enum ExpiraDesde
    {
        COMPRA,
        PRIMERA_VISITA
    }

    public enum EstadoClase
    {
        ACTIVA,
        CANCELADA
    }

    public enum EstadoInscripcion
    {
        INSCRITO,
        PRESENTE,
        AUSENTE,
        CANCELADO
    }

    public enum CanalReserva
    {
        COACH,
        PORTAL
    }

    public enum CanalCheckin
    {
        COACH,
        PORTAL,
        [Description("QR")] QR
    }

    public enum AccionBitacora
    {
        CREAR,
        EDITAR,
        ELIMINAR,
        LOGIN,
        [Description("Login fallido")] LOGIN_FALLIDO,
        [Description("Cambio de box")] CAMBIO_BOX
    }

    public enum TipoEventoMembresia
    {
        ALTA,
        [Description("Renovación")] RENOVACION,
        PAGO,
        [Description("Pago anulado")] PAGO_ANULADO,
        [Description("Extensión")] EXTENSION,
        [Description("Cancelación")] CANCELACION
    }

}
