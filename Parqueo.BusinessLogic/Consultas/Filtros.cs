namespace Parqueo.BusinessLogic.Consultas;

/// <summary>
/// Filtros de la lista de automóviles. Todos son opcionales: un valor nulo o vacío
/// no filtra. Los filtros de texto son aproximados (ver <see cref="BusquedaAproximada"/>)
/// y el intervalo de años incluye ambos extremos.
/// </summary>
public sealed record FiltroAutomovil(
    string? Color = null,
    int? AnioDesde = null,
    int? AnioHasta = null,
    string? Fabricante = null,
    string? Tipo = null)
{
    /// <summary>Lanza <see cref="ArgumentException"/> si el intervalo de años está invertido.</summary>
    public void Validar()
    {
        if (AnioDesde is int desde && AnioHasta is int hasta && desde > hasta)
        {
            throw new ArgumentException(
                $"El intervalo de años es inválido: el año inicial ({desde}) es mayor que el final ({hasta}).");
        }
    }
}

/// <summary>
/// Filtros de parqueos. Todos son opcionales. Provincia y nombre son aproximados;
/// el rango de precio por hora incluye ambos extremos y se compara en decimal.
/// </summary>
public sealed record FiltroParqueo(
    string? NombreProvincia = null,
    string? Nombre = null,
    decimal? PrecioMinimo = null,
    decimal? PrecioMaximo = null)
{
    /// <summary>Lanza <see cref="ArgumentException"/> si el rango de precios es inválido.</summary>
    public void Validar()
    {
        if (PrecioMinimo is < 0 || PrecioMaximo is < 0)
        {
            throw new ArgumentException("Los precios del rango no pueden ser negativos.");
        }

        if (PrecioMinimo is decimal minimo && PrecioMaximo is decimal maximo && minimo > maximo)
        {
            throw new ArgumentException(
                $"El rango de precios es inválido: el mínimo ({minimo}) es mayor que el máximo ({maximo}).");
        }
    }
}
