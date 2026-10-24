using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Repositorios.Json;

/// <summary>Parqueos desde el archivo parqueos.json.</summary>
public sealed class ParqueoRepositorioJson : RepositorioJsonBase<PrqParqueo>, IParqueoRepositorio
{
    public ParqueoRepositorioJson(FuenteDatosJson fuente)
        : base(fuente, FuenteDatosJson.ArchivoParqueos, "prq_parqueo")
    {
    }

    protected override int ObtenerLlave(PrqParqueo entidad) => entidad.Id;

    protected override void AsignarLlave(PrqParqueo entidad, int llave) => entidad.Id = llave;

    protected override void Validar(PrqParqueo entidad) => Validador.Validar(entidad);

    protected override async Task ValidarBorradoAsync(int id, CancellationToken cancelacion)
    {
        var ingresos = await Fuente.LeerAsync<PrqIngresoAutomovil>(FuenteDatosJson.ArchivoIngresos, cancelacion);
        if (ingresos.Any(i => i.IdParqueo == id))
        {
            throw Validador.RegistroConIngresos(NombreTabla, id);
        }
    }

    protected override async Task IncluirPropiedadesNavegacionAsync(PrqParqueo parqueo, CancellationToken cancelacion)
    {
        var ingresos = await Fuente.LeerAsync<PrqIngresoAutomovil>(FuenteDatosJson.ArchivoIngresos, cancelacion);
        var automoviles = (await Fuente.LeerAsync<PrqAutomovil>(FuenteDatosJson.ArchivoAutomoviles, cancelacion))
            .ToDictionary(a => a.Id);

        // Igual que el Include + ThenInclude de EF: cada ingreso con su automóvil.
        parqueo.PrqIngresoAutomovils = ingresos.Where(i => i.IdParqueo == parqueo.Id).ToList();
        foreach (var ingreso in parqueo.PrqIngresoAutomovils)
        {
            ingreso.IdParqueoNavigation = parqueo;
            ingreso.IdAutomovilNavigation = automoviles.GetValueOrDefault(ingreso.IdAutomovil)!;
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
