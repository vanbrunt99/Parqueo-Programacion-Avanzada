using Microsoft.EntityFrameworkCore.Storage;
using Parqueo.BusinessLogic.Repositorios.BaseDatos;
using Parqueo.BusinessLogic.Repositorios.Json;
using Parqueo.Model.Contexto;
using Parqueo.Tests.Infraestructura;

namespace Parqueo.Tests.Repositorios;

/// <summary>
/// CRUD contra archivos JSON. Cada prueba usa una copia temporal de los datos y la
/// verificación se hace con una NUEVA <see cref="FuenteDatosJson"/>, es decir,
/// leyendo de nuevo los archivos como lo haría otra ejecución del programa.
/// </summary>
public sealed class CrudJsonPruebas : CrudRepositoriosPruebasBase
{
    private readonly CarpetaJsonTemporal _carpeta = new();

    protected override string? MotivoOmision => null;

    protected override RepositoriosParqueo CrearRepositorios() => Crear(_carpeta.Fuente);

    protected override RepositoriosParqueo CrearRepositoriosDeVerificacion() => Crear(new FuenteDatosJson(_carpeta.Ruta));

    private static RepositoriosParqueo Crear(FuenteDatosJson fuente) => new(
        new AutomovilRepositorioJson(fuente),
        new ParqueoRepositorioJson(fuente),
        new IngresoAutomovilRepositorioJson(fuente));

    public override void Dispose()
    {
        _carpeta.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// CRUD contra PostgreSQL. Todo ocurre dentro de una transacción que se revierte al
/// terminar cada prueba, por lo que la base queda igual que antes.
/// </summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class CrudBaseDatosPruebas : CrudRepositoriosPruebasBase
{
    private ParqueoContext? _contexto;
    private IDbContextTransaction? _transaccion;

    protected override string? MotivoOmision => BaseDatosPruebas.MotivoOmision;

    protected override RepositoriosParqueo CrearRepositorios()
    {
        _contexto = BaseDatosPruebas.CrearContexto();
        _transaccion = _contexto.Database.BeginTransaction();
        return Crear(_contexto);
    }

    protected override RepositoriosParqueo CrearRepositoriosDeVerificacion()
    {
        // Misma conexión y transacción (los cambios aún no confirmados solo son
        // visibles ahí), pero sin entidades en memoria: las consultas van a la base.
        _ = Repos;
        _contexto!.ChangeTracker.Clear();
        return Crear(_contexto);
    }

    private static RepositoriosParqueo Crear(ParqueoContext contexto) => new(
        new AutomovilRepositorioEf(contexto),
        new ParqueoRepositorioEf(contexto),
        new IngresoAutomovilRepositorioEf(contexto));

    public override void Dispose()
    {
        _transaccion?.Rollback();
        _transaccion?.Dispose();
        _contexto?.Dispose();
        base.Dispose();
    }
}
