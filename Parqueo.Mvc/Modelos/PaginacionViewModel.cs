namespace Parqueo.Mvc.Modelos;

/// <summary>
/// Datos de la barra de paginación. <see cref="Filtro"/> son los parámetros que se conservan al
/// cambiar de página (por ejemplo, el intervalo de años).
/// </summary>
public sealed record PaginacionViewModel(
    string Controlador,
    int Pagina,
    int TotalPaginas,
    int Desde,
    int Hasta,
    int Total,
    int TamanoPagina,
    string Unidad,
    IReadOnlyDictionary<string, string> Filtro)
{
    public Dictionary<string, string> RutaDe(int pagina)
    {
        var ruta = new Dictionary<string, string>(Filtro)
        {
            ["pagina"] = pagina.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["tamanoPagina"] = TamanoPagina.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        return ruta;
    }
}
