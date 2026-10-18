using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Repositorios.Json;

/// <summary>Parqueos desde el archivo parqueos.json.</summary>
public sealed class ParqueoRepositorioJson : RepositorioJsonBase<PrqParqueo>, IParqueoRepositorio
{
    public ParqueoRepositorioJson(FuenteDatosJson fuente)
        : base(fuente, FuenteDatosJson.ArchivoParqueos)
    {
    }

    protected override int ObtenerLlave(PrqParqueo entidad) => entidad.Id;

    protected override async Task IncluirPropiedadesNavegacionAsync(PrqParqueo parqueo, CancellationToken cancelacion)
    {
        var ingresos = await Fuente.LeerAsync<PrqIngresoAutomovil>(FuenteDatosJson.ArchivoIngresos, cancelacion);
        parqueo.PrqIngresoAutomovils = ingresos.Where(i => i.IdParqueo == parqueo.Id).ToList();
        foreach (var ingreso in parqueo.PrqIngresoAutomovils)
        {
            ingreso.IdParqueoNavigation = parqueo;
        }
    }

    public async Task<IReadOnlyList<PrqParqueo>> BuscarAsync(
        FiltroParqueo filtro, Paginacion? paginacion = null, CancellationToken cancelacion = default)
    {
        var parqueos = await LeerTodosAsync(cancelacion);
        return Reglas.FiltrarParqueos(parqueos.AsQueryable(), filtro)
            .Paginar(paginacion)
            .ToList();
    }
}
