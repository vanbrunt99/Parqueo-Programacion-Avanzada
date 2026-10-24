using Microsoft.EntityFrameworkCore;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Contexto;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Repositorios.BaseDatos;

/// <summary>Ingresos de automóviles desde PostgreSQL mediante el contexto de EF Core.</summary>
public sealed class IngresoAutomovilRepositorioEf : RepositorioEfBase<PrqIngresoAutomovil>, IIngresoAutomovilRepositorio
{
    private readonly GeneradorResumenIngresos _generadorResumen = new();

    public IngresoAutomovilRepositorioEf(ParqueoContext contexto)
        : base(contexto, nameof(PrqIngresoAutomovil.Consecutivo), "prq_ingreso_automovil")
    {
    }

    protected override IQueryable<PrqIngresoAutomovil> IncluirPropiedadesNavegacion(IQueryable<PrqIngresoAutomovil> consulta) =>
        consulta
            .Include(i => i.IdAutomovilNavigation)
            .Include(i => i.IdParqueoNavigation);

    protected override void Validar(PrqIngresoAutomovil entidad) => Validador.Validar(entidad);

    protected override async Task ValidarReferenciasAsync(PrqIngresoAutomovil ingreso, CancellationToken cancelacion)
    {
        if (!await Contexto.PrqParqueos.AnyAsync(p => p.Id == ingreso.IdParqueo, cancelacion))
        {
            throw Validador.ReferenciaInexistente("prq_parqueo", ingreso.IdParqueo);
        }

        if (!await Contexto.PrqAutomovils.AnyAsync(a => a.Id == ingreso.IdAutomovil, cancelacion))
        {
            throw Validador.ReferenciaInexistente("prq_automovil", ingreso.IdAutomovil);
        }
    }

    public async Task<decimal?> ObtenerPrecioPorHoraPorParqueo(int idParqueo, CancellationToken cancelacion = default)
    {
        return await Contexto.PrqParqueos
            .AsNoTracking()
            .Where(p => p.Id == idParqueo)
            .Select(p => (decimal?)p.PrecioPorHora)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task<IReadOnlyList<PrqIngresoAutomovil>> ListarAsync(
        Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        return await IngresosConNavegacion()
            .OrderBy(i => i.Consecutivo)
            .Paginar(paginacion)
            .ToListAsync(cancelacion);
    }

    public async Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorTipoAsync(
        string tipo, DateTime desde, DateTime hasta, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        var ingresos = await Reglas.FiltrarIngresosPorTipo(IngresosConNavegacion(), tipo, desde, hasta)
            .Paginar(paginacion)
            .ToListAsync(cancelacion);

        return await _generadorResumen.CrearAsync(ingresos, this, cancelacion);
    }

    public async Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorProvinciaAsync(
        string provincia, DateTime desde, DateTime hasta, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        var ingresos = await Reglas.FiltrarIngresosPorProvincia(IngresosConNavegacion(), provincia, desde, hasta)
            .Paginar(paginacion)
            .ToListAsync(cancelacion);

        return await _generadorResumen.CrearAsync(ingresos, this, cancelacion);
    }

    private IQueryable<PrqIngresoAutomovil> IngresosConNavegacion() =>
        IncluirPropiedadesNavegacion(Entidades.AsNoTracking());
}
