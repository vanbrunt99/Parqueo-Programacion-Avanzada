using System.Net;

namespace Parqueo.Mvc.Servicios;

/// <summary>
/// Resultado de una llamada al API: los datos si fue exitosa o el problema que devolvió el
/// API (400, 404, 409, 500, 503), más el origen de datos que la atendió según el encabezado
/// X-Origen-Datos ("Json" o "BaseDatos"). Los controladores MVC deciden qué mostrar.
/// </summary>
public sealed class RespuestaApi<T>
{
    private RespuestaApi(HttpStatusCode estado, T? datos, ProblemaApi? problema, string? origenDatos)
    {
        Estado = estado;
        Datos = datos;
        Problema = problema;
        OrigenDatos = origenDatos;
    }

    public HttpStatusCode Estado { get; }

    public T? Datos { get; }

    public ProblemaApi? Problema { get; }

    /// <summary>Valor del encabezado X-Origen-Datos de la respuesta (null si no vino).</summary>
    public string? OrigenDatos { get; }

    public bool Exito => Problema is null;

    public bool NoEncontrado => Estado == HttpStatusCode.NotFound;

    public bool Conflicto => Estado == HttpStatusCode.Conflict;

    /// <summary>Mensaje legible del error: el detalle del API, o su título si no hay detalle.</summary>
    public string? MensajeError => Problema is null ? null : Problema.Detail ?? Problema.Title ?? $"El API respondió {(int)Estado}.";

    public static RespuestaApi<T> Correcta(HttpStatusCode estado, T datos, string? origenDatos) =>
        new(estado, datos, null, origenDatos);

    public static RespuestaApi<T> Fallida(HttpStatusCode estado, ProblemaApi problema, string? origenDatos) =>
        new(estado, default, problema, origenDatos);

    /// <summary>Convierte un error a otro tipo de datos conservando estado, problema y origen.</summary>
    public RespuestaApi<TOtro> ComoFallida<TOtro>() =>
        RespuestaApi<TOtro>.Fallida(Estado, Problema ?? new ProblemaApi("Error del API", null, (int)Estado), OrigenDatos);
}
