using Microsoft.EntityFrameworkCore;
using Parqueo.BusinessLogic.Repositorios.BaseDatos;
using Parqueo.BusinessLogic.Repositorios.Json;
using Parqueo.Model.Contexto;
using Parqueo.Model.Entidades;
using Parqueo.Tests.Infraestructura;

namespace Parqueo.Tests.Repositorios;

/// <summary>Consultas contra los archivos JSON (siempre disponibles).</summary>
public sealed class ConsultasJsonPruebas : ConsultasRepositoriosPruebasBase
{
    private readonly CarpetaJsonTemporal _carpeta = new();

    protected override string? MotivoOmision => null;

    protected override RepositoriosParqueo CrearRepositorios() => new(
        new AutomovilRepositorioJson(_carpeta.Fuente),
        new ParqueoRepositorioJson(_carpeta.Fuente),
        new IngresoAutomovilRepositorioJson(_carpeta.Fuente));

    public override void Dispose()
    {
        _carpeta.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Las mismas consultas contra PostgreSQL mediante el contexto de EF Core. Además
/// comprueba que la base y los archivos JSON contienen exactamente los mismos datos.
/// </summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ConsultasBaseDatosPruebas : ConsultasRepositoriosPruebasBase
{
    private ParqueoContext? _contexto;

    protected override string? MotivoOmision => BaseDatosPruebas.MotivoOmision;

    protected override RepositoriosParqueo CrearRepositorios()
    {
        _contexto = BaseDatosPruebas.CrearContexto();
        return new(
            new AutomovilRepositorioEf(_contexto),
            new ParqueoRepositorioEf(_contexto),
            new IngresoAutomovilRepositorioEf(_contexto));
    }

    [SkippableFact]
    public async Task DatosDeLaBaseYDeLosJson_SonIguales()
    {
        _ = Repos;
        using var carpeta = new CarpetaJsonTemporal();

        var automovilesJson = await carpeta.Fuente.LeerAsync<PrqAutomovil>(FuenteDatosJson.ArchivoAutomoviles);
        var parqueosJson = await carpeta.Fuente.LeerAsync<PrqParqueo>(FuenteDatosJson.ArchivoParqueos);
        var ingresosJson = await carpeta.Fuente.LeerAsync<PrqIngresoAutomovil>(FuenteDatosJson.ArchivoIngresos);

        var automovilesBd = await _contexto!.PrqAutomovils.AsNoTracking().OrderBy(a => a.Id).ToListAsync();
        var parqueosBd = await _contexto.PrqParqueos.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        var ingresosBd = await _contexto.PrqIngresoAutomovils.AsNoTracking().OrderBy(i => i.Consecutivo).ToListAsync();

        Assert.Equal(
            automovilesJson.Select(a => (a.Id, a.Color, a.Anio, a.Fabricante, a.Tipo)),
            automovilesBd.Select(a => (a.Id, a.Color, a.Anio, a.Fabricante, a.Tipo)));
        Assert.Equal(
            parqueosJson.Select(p => (p.Id, p.NombreProvincia, p.Nombre, p.PrecioPorHora)),
            parqueosBd.Select(p => (p.Id, p.NombreProvincia, p.Nombre, p.PrecioPorHora)));
        Assert.Equal(
            ingresosJson.Select(i => (i.Consecutivo, i.IdParqueo, i.IdAutomovil, i.FechaHoraEntrada, i.FechaHoraSalida)),
            ingresosBd.Select(i => (i.Consecutivo, i.IdParqueo, i.IdAutomovil, i.FechaHoraEntrada, i.FechaHoraSalida)));
    }

    public override void Dispose()
    {
        _contexto?.Dispose();
        base.Dispose();
    }
}
