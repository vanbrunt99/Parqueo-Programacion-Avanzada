using System.Text.RegularExpressions;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Consultas;

/// <summary>
/// Reglas de filtrado y ordenamiento compartidas por los repositorios de base de
/// datos y de JSON. Se escriben una sola vez sobre <see cref="IQueryable{T}"/>:
/// con Entity Framework se traducen a SQL; con JSON se ejecutan en memoria
/// (LINQ to Objects) sobre los registros leídos del archivo.
/// </summary>
public sealed class ReglasConsulta
{
    private readonly BusquedaAproximada _busqueda = new();

    public IQueryable<PrqAutomovil> FiltrarAutomoviles(IQueryable<PrqAutomovil> automoviles, FiltroAutomovil filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        filtro.Validar();

        if (_busqueda.CrearPatron(filtro.Color) is string patronColor)
        {
            automoviles = automoviles.Where(a => Regex.IsMatch(a.Color, patronColor));
        }

        if (filtro.AnioDesde is int anioDesde)
        {
            automoviles = automoviles.Where(a => a.Anio >= anioDesde);
        }

        if (filtro.AnioHasta is int anioHasta)
        {
            automoviles = automoviles.Where(a => a.Anio <= anioHasta);
        }

        if (_busqueda.CrearPatron(filtro.Fabricante) is string patronFabricante)
        {
            automoviles = automoviles.Where(a => Regex.IsMatch(a.Fabricante, patronFabricante));
        }

        if (_busqueda.CrearPatron(filtro.Tipo) is string patronTipo)
        {
            automoviles = automoviles.Where(a => Regex.IsMatch(a.Tipo, patronTipo));
        }

        return automoviles.OrderBy(a => a.Id);
    }

    public IQueryable<PrqParqueo> FiltrarParqueos(IQueryable<PrqParqueo> parqueos, FiltroParqueo filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        filtro.Validar();

        if (_busqueda.CrearPatron(filtro.NombreProvincia) is string patronProvincia)
        {
            parqueos = parqueos.Where(p => Regex.IsMatch(p.NombreProvincia, patronProvincia));
        }

        if (_busqueda.CrearPatron(filtro.Nombre) is string patronNombre)
        {
            parqueos = parqueos.Where(p => Regex.IsMatch(p.Nombre, patronNombre));
        }

        if (filtro.PrecioMinimo is decimal precioMinimo)
        {
            parqueos = parqueos.Where(p => p.PrecioPorHora >= precioMinimo);
        }

        if (filtro.PrecioMaximo is decimal precioMaximo)
        {
            parqueos = parqueos.Where(p => p.PrecioPorHora <= precioMaximo);
        }

        return parqueos.OrderBy(p => p.Id);
    }

    /// <summary>
    /// Ingresos de automóviles cuyo tipo coincide (aproximado) y cuya fecha y hora de
    /// entrada está en [desde, hasta]. Requiere la navegación al automóvil.
    /// </summary>
    public IQueryable<PrqIngresoAutomovil> FiltrarIngresosPorTipo(
        IQueryable<PrqIngresoAutomovil> ingresos, string tipo, DateTime desde, DateTime hasta)
    {
        string patronTipo = PatronObligatorio(tipo, nameof(tipo));
        ValidarIntervalo(desde, hasta);

        return Ordenar(ingresos.Where(i =>
            Regex.IsMatch(i.IdAutomovilNavigation.Tipo, patronTipo)
            && i.FechaHoraEntrada >= desde
            && i.FechaHoraEntrada <= hasta));
    }

    /// <summary>
    /// Ingresos a parqueos de la provincia indicada (aproximado) cuya fecha y hora de
    /// entrada está en [desde, hasta]. Requiere la navegación al parqueo.
    /// </summary>
    public IQueryable<PrqIngresoAutomovil> FiltrarIngresosPorProvincia(
        IQueryable<PrqIngresoAutomovil> ingresos, string provincia, DateTime desde, DateTime hasta)
    {
        string patronProvincia = PatronObligatorio(provincia, nameof(provincia));
        ValidarIntervalo(desde, hasta);

        return Ordenar(ingresos.Where(i =>
            Regex.IsMatch(i.IdParqueoNavigation.NombreProvincia, patronProvincia)
            && i.FechaHoraEntrada >= desde
            && i.FechaHoraEntrada <= hasta));
    }

    private static IQueryable<PrqIngresoAutomovil> Ordenar(IQueryable<PrqIngresoAutomovil> ingresos) =>
        ingresos.OrderBy(i => i.FechaHoraEntrada).ThenBy(i => i.Consecutivo);

    private string PatronObligatorio(string texto, string nombreParametro) =>
        _busqueda.CrearPatron(texto)
        ?? throw new ArgumentException("Debe indicar un texto de búsqueda.", nombreParametro);

    private static void ValidarIntervalo(DateTime desde, DateTime hasta)
    {
        if (desde > hasta)
        {
            throw new ArgumentException(
                $"El intervalo de fechas es inválido: {desde:yyyy-MM-dd HH:mm} es posterior a {hasta:yyyy-MM-dd HH:mm}.");
        }
    }
}
