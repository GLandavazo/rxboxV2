namespace RxBox.Core.Enums
{
    public enum TipoCatalogo
    {
        CATEGORIA_MEMBRESIA,
        METODO_PAGO
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
        QR
    }
}
