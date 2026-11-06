namespace RxBox.Core.Exceptions
{
    /// <summary>
    /// Toda excepción de negocio hereda de aquí y está mapeada en
    /// <c>GlobalExceptionHandler</c>. Nunca lances InvalidOperationException /
    /// KeyNotFoundException sueltas para reglas de negocio: el handler no las
    /// reconoce como tuyas y responden 500.
    /// </summary>
    public abstract class BusinessException : Exception
    {
        protected BusinessException(string message) : base(message) { }
    }

    /// <summary>409 — el estado actual del recurso no permite la operación.</summary>
    public class ConflictException : BusinessException
    {
        public ConflictException(string message) : base(message) { }
    }

    /// <summary>404 — el recurso solicitado no existe.</summary>
    public class NotFoundException : BusinessException
    {
        public NotFoundException(string resourceName, object id)
            : base($"{resourceName} con id '{id}' no encontrado.") { }
    }

    /// <summary>401 — credenciales o token inválidos.</summary>
    public class UnauthenticatedException : BusinessException
    {
        public UnauthenticatedException(string message) : base(message) { }
    }

    /// <summary>409 — no se puede continuar por una dependencia con otro recurso.</summary>
    public class DependencyException : BusinessException
    {
        public DependencyException(string message) : base(message) { }
    }
}
