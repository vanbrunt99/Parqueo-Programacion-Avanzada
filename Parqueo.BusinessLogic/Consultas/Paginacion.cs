namespace Parqueo.BusinessLogic.Consultas;

/// <summary>
/// Paginación opcional de las consultas, según el patrón visto en la clase 6:
/// desplazamiento = (página - 1) * tamaño; luego Skip(desplazamiento) y Take(tamaño).
/// Si no se envía paginación, la consulta devuelve todos los registros.
/// </summary>
public sealed record Paginacion
{
    /// <summary>Tamaño máximo permitido por página.</summary>
    public const int TamanoMaximo = 100;

    public Paginacion(int pagina = 1, int tamanoPagina = 10)
    {
        if (pagina < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pagina), pagina, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > TamanoMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanoPagina), tamanoPagina, $"El tamaño de página debe estar entre 1 y {TamanoMaximo}.");
        }

        if ((long)(pagina - 1) * tamanoPagina > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(pagina), pagina, "La página solicitada es demasiado grande.");
        }

        Pagina = pagina;
        TamanoPagina = tamanoPagina;
    }

    public int Pagina { get; }

    public int TamanoPagina { get; }

    public int Desplazamiento => (Pagina - 1) * TamanoPagina;
}

/// <summary>Aplica la paginación a una consulta ya ordenada.</summary>
public static class PaginacionExtensiones
{
    public static IQueryable<T> Paginar<T>(this IQueryable<T> consulta, Paginacion? paginacion) =>
        paginacion is null
            ? consulta
            : consulta.Skip(paginacion.Desplazamiento).Take(paginacion.TamanoPagina);
}
