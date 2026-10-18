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
        : base(contexto, nameof(PrqAutomovil.Id), "prq_automovil")
    {
    }

    // Los ingresos traen también su parqueo para que MontoTotalAPagar tenga el precio.
    protected override IQueryable<PrqAutomovil> IncluirPropiedadesNavegacion(IQueryable<PrqAutomovil> consulta) =>
        consulta.Include(a => a.PrqIngresoAutomovils).ThenInclude(i => i.IdParqueoNavigation);

    protected override void Validar(PrqAutomovil entidad) => Validador.Validar(entidad);

    protected override async Task ValidarBorradoAsync(int id, CancellationToken cancelacion)
    {
        if (await Contexto.PrqIngresoAutomovils.AnyAsync(i => i.IdAutomovil == id, cancelacion))
        {
            throw Validador.RegistroConIngresos(NombreTabla, id);
        }
    }

    public async Task<IReadOnlyList<PrqAutomovil>> BuscarAsync(
        FiltroAutomovil filtro, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        return await Reglas.FiltrarAutomoviles(Entidades.AsNoTracking(), filtro)
            .Paginar(paginacion)
            .ToListAsync(cancelacion);
    }
}
