namespace Parqueo.BusinessLogic.Validacion;

/// <summary>
/// El cambio solicitado choca con los datos existentes: llave duplicada, referencia
/// a un registro inexistente o borrado de un registro que tiene ingresos.
/// Hereda de <see cref="InvalidOperationException"/>; el API la responde con
/// HTTP 409 (Conflict), a diferencia de otros errores internos.
/// </summary>
public sealed class ReglaNegocioException : InvalidOperationException
{
    public ReglaNegocioException(string mensaje)
        : base(mensaje)
    {
    }

    public ReglaNegocioException(string mensaje, Exception causa)
        : base(mensaje, causa)
    {
    }
}
