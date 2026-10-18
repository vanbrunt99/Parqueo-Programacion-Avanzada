using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Repositorios.Json;

/// <summary>Automóviles desde el archivo automoviles.json.</summary>
public sealed class AutomovilRepositorioJson : RepositorioJsonBase<PrqAutomovil>, IAutomovilRepositorio
{
    public AutomovilRepositorioJson(FuenteDatosJson fuente)
        : base(fuente, FuenteDatosJson.ArchivoAutomoviles)
    {
    }

    protected override int ObtenerLlave(PrqAutomovil entidad) => entidad.Id;

    protected override async Task IncluirPropiedadesNavegacionAsync(PrqAutomovil automovil, CancellationToken cancelacion)
    {
        var ingresos = await Fuente.LeerAsync<PrqIngresoAutomovil>(FuenteDatosJson.ArchivoIngresos, cancelacion);
        automovil.PrqIngresoAutomovils = ingresos.Where(i => i.IdAutomovil == automovil.Id).ToList();
        foreach (var ingreso in automovil.PrqIngresoAutomovils)
        {
            ingreso.IdAutomovilNavigation = automovil;
        }
    }

    public async Task<IReadOnlyList<PrqAutomovil>> BuscarAsync(
        FiltroAutomovil filtro, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        var automoviles = await LeerTodosAsync(cancelacion);
        return Reglas.FiltrarAutomoviles(automoviles.AsQueryable(), filtro)
            .Paginar(paginacion)
            .ToList();
    }
}
