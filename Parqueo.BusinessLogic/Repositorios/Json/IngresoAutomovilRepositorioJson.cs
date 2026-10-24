using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Repositorios.Json;

/// <summary>Ingresos de automóviles desde el archivo ingresos-automoviles.json.</summary>
public sealed class IngresoAutomovilRepositorioJson : RepositorioJsonBase<PrqIngresoAutomovil>, IIngresoAutomovilRepositorio
{
    private readonly GeneradorResumenIngresos _generadorResumen = new();

    public IngresoAutomovilRepositorioJson(FuenteDatosJson fuente)
        : base(fuente, FuenteDatosJson.ArchivoIngresos, "prq_ingreso_automovil")
    {
    }

    protected override int ObtenerLlave(PrqIngresoAutomovil entidad) => entidad.Consecutivo;

    protected override void AsignarLlave(PrqIngresoAutomovil entidad, int llave) => entidad.Consecutivo = llave;

    protected override void Validar(PrqIngresoAutomovil entidad) => Validador.Validar(entidad);

    protected override async Task ValidarReferenciasAsync(PrqIngresoAutomovil ingreso, CancellationToken cancelacion)
    {
        var parqueos = await Fuente.LeerAsync<PrqParqueo>(FuenteDatosJson.ArchivoParqueos, cancelacion);
        if (!parqueos.Any(p => p.Id == ingreso.IdParqueo))
        {
            throw Validador.ReferenciaInexistente("prq_parqueo", ingreso.IdParqueo);
        }

        var automoviles = await Fuente.LeerAsync<PrqAutomovil>(FuenteDatosJson.ArchivoAutomoviles, cancelacion);
        if (!automoviles.Any(a => a.Id == ingreso.IdAutomovil))
        {
            throw Validador.ReferenciaInexistente("prq_automovil", ingreso.IdAutomovil);
        }
    }

    protected override async Task IncluirPropiedadesNavegacionAsync(PrqIngresoAutomovil ingreso, CancellationToken cancelacion)
    {
        await EnlazarConAutomovilYParqueoAsync([ingreso], cancelacion);
    }

    public async Task<decimal?> ObtenerPrecioPorHoraPorParqueo(int idParqueo, CancellationToken cancelacion = default)
    {
        var parqueos = await Fuente.LeerAsync<PrqParqueo>(FuenteDatosJson.ArchivoParqueos, cancelacion);
        return parqueos.FirstOrDefault(p => p.Id == idParqueo)?.PrecioPorHora;
    }

    public async Task<IReadOnlyList<PrqIngresoAutomovil>> ListarAsync(
        Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        var ingresos = await LeerIngresosConNavegacionAsync(cancelacion);
        return ingresos.AsQueryable()
            .OrderBy(i => i.Consecutivo)
            .Paginar(paginacion)
            .ToList();
    }

    public async Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorTipoAsync(
        string tipo, DateTime desde, DateTime hasta, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        var ingresos = await LeerIngresosConNavegacionAsync(cancelacion);
        var seleccionados = Reglas.FiltrarIngresosPorTipo(ingresos.AsQueryable(), tipo, desde, hasta)
            .Paginar(paginacion)
            .ToList();

        return await _generadorResumen.CrearAsync(seleccionados, this, cancelacion);
    }

    public async Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorProvinciaAsync(
        string provincia, DateTime desde, DateTime hasta, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        var ingresos = await LeerIngresosConNavegacionAsync(cancelacion);
        var seleccionados = Reglas.FiltrarIngresosPorProvincia(ingresos.AsQueryable(), provincia, desde, hasta)
            .Paginar(paginacion)
            .ToList();

        return await _generadorResumen.CrearAsync(seleccionados, this, cancelacion);
    }

    private async Task<List<PrqIngresoAutomovil>> LeerIngresosConNavegacionAsync(CancellationToken cancelacion)
    {
        var ingresos = await LeerTodosAsync(cancelacion);
        await EnlazarConAutomovilYParqueoAsync(ingresos, cancelacion);
        return ingresos;
    }

    /// <summary>
    /// Equivalente JSON del Include de EF: asigna a cada ingreso su automóvil y su
    /// parqueo. Si un ingreso apunta a un id inexistente, los archivos son
    /// inconsistentes y se informa con una excepción clara.
    /// </summary>
    private async Task EnlazarConAutomovilYParqueoAsync(IReadOnlyList<PrqIngresoAutomovil> ingresos, CancellationToken cancelacion)
    {
        var automoviles = (await Fuente.LeerAsync<PrqAutomovil>(FuenteDatosJson.ArchivoAutomoviles, cancelacion))
            .ToDictionary(a => a.Id);
        var parqueos = (await Fuente.LeerAsync<PrqParqueo>(FuenteDatosJson.ArchivoParqueos, cancelacion))
            .ToDictionary(p => p.Id);

        foreach (var ingreso in ingresos)
        {
            ingreso.IdAutomovilNavigation = automoviles.GetValueOrDefault(ingreso.IdAutomovil)
                ?? throw new InvalidDataException(
                    $"El ingreso {ingreso.Consecutivo} referencia al automóvil {ingreso.IdAutomovil}, que no existe en {FuenteDatosJson.ArchivoAutomoviles}.");
            ingreso.IdParqueoNavigation = parqueos.GetValueOrDefault(ingreso.IdParqueo)
                ?? throw new InvalidDataException(
                    $"El ingreso {ingreso.Consecutivo} referencia al parqueo {ingreso.IdParqueo}, que no existe en {FuenteDatosJson.ArchivoParqueos}.");
        }
    }
}
