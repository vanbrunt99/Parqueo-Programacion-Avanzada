using Parqueo.BusinessLogic.Consultas;

namespace Parqueo.Api.Dtos;

/// <summary>
/// Parámetros de paginación de los GET de colección (?pagina=1&amp;tamanoPagina=10).
/// Los valores por defecto son los de la clase 6: página 1 y 10 registros. Un valor fuera
/// de rango (página &lt; 1, tamaño fuera de 1..100) produce 400.
/// </summary>
public sealed class ParametrosPaginacion
{
    public int Pagina { get; init; } = 1;

    public int TamanoPagina { get; init; } = 10;

    public Paginacion ACriterio() => new(Pagina, TamanoPagina);
}
