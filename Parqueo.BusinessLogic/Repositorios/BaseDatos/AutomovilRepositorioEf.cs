using Microsoft.EntityFrameworkCore;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Contexto;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Repositorios.BaseDatos;

/// <summary>Automóviles desde PostgreSQL mediante el contexto de EF Core.</summary>
public sealed class AutomovilRepositorioEf : RepositorioEfBase<PrqAutomovil>, IAutomovilRepositorio
{
    public AutomovilRepositorioEf(ParqueoContext contexto)
        : base(contexto, nameof(PrqAutomovil.Id))
    {
    }

    protected override IQueryable<PrqAutomovil> IncluirPropiedadesNavegacion(IQueryable<PrqAutomovil> consulta) =>
        consulta.Include(a => a.PrqIngresoAutomovils);

    public async Task<IReadOnlyList<PrqAutomovil>> BuscarAsync(
        FiltroAutomovil filtro, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        return await Reglas.FiltrarAutomoviles(Entidades.AsNoTracking(), filtro)
            .Paginar(paginacion)
            .ToListAsync(cancelacion);
    }
}
