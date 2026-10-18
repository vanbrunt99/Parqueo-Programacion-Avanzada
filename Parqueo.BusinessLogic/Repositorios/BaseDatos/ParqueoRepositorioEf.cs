using Microsoft.EntityFrameworkCore;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Contexto;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Repositorios.BaseDatos;

/// <summary>Parqueos desde PostgreSQL mediante el contexto de EF Core.</summary>
public sealed class ParqueoRepositorioEf : RepositorioEfBase<PrqParqueo>, IParqueoRepositorio
{
    public ParqueoRepositorioEf(ParqueoContext contexto)
        : base(contexto, nameof(PrqParqueo.Id))
    {
    }

    protected override IQueryable<PrqParqueo> IncluirPropiedadesNavegacion(IQueryable<PrqParqueo> consulta) =>
        consulta.Include(p => p.PrqIngresoAutomovils);

    public async Task<IReadOnlyList<PrqParqueo>> BuscarAsync(
        FiltroParqueo filtro, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        return await Reglas.FiltrarParqueos(Entidades.AsNoTracking(), filtro)
            .Paginar(paginacion)
            .ToListAsync(cancelacion);
    }
}
