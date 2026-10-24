using Parqueo.Api.Tests.Infraestructura;

namespace Parqueo.Api.Tests;

/// <summary>Origen de datos con el que se levanta el API en una clase de prueba.</summary>
public abstract class OrigenPruebas : IDisposable
{
    /// <summary>Texto para omitir la prueba, o null si el origen está disponible.</summary>
    public abstract string? MotivoOmision { get; }

    /// <summary>Valor esperado del encabezado X-Origen-Datos.</summary>
    public abstract string Nombre { get; }

    /// <summary>Carpeta de los JSON (solo para el origen JSON).</summary>
    public virtual string? CarpetaJson => null;

    public abstract FabricaApi CrearFabrica();

    public virtual void Dispose() => GC.SuppressFinalize(this);
}

/// <summary>Archivos JSON sobre una copia temporal (Datos/ no se modifica).</summary>
public sealed class OrigenJson : OrigenPruebas
{
    private readonly CarpetaJsonTemporal _carpeta = new();

    public override string? MotivoOmision => null;

    public override string Nombre => "Json";

    public override string? CarpetaJson => _carpeta.Ruta;

    public override FabricaApi CrearFabrica() => FabricaApi.ConJson(_carpeta.Ruta);

    public override void Dispose()
    {
        _carpeta.Dispose();
        base.Dispose();
    }
}

/// <summary>PostgreSQL con la cadena de PARQUEO_PRUEBAS_BD.</summary>
public sealed class OrigenBaseDatos : OrigenPruebas
{
    public override string? MotivoOmision => BaseDatosPruebas.MotivoOmision;

    public override string Nombre => "BaseDatos";

    public override FabricaApi CrearFabrica() => FabricaApi.ConBaseDatos(BaseDatosPruebas.CadenaConexion!);
}

/// <summary>
/// Base de las pruebas del API. Las pruebas de cada recurso se escriben una vez en una
/// clase abstracta y se heredan dos veces (JSON y PostgreSQL): ambos orígenes deben
/// responder igual por HTTP.
/// </summary>
public abstract class ApiPruebasBase : IDisposable
{
    private FabricaApi? _fabrica;
    private HttpClient? _cliente;

    protected ApiPruebasBase(OrigenPruebas origen)
    {
        Origen = origen;
    }

    protected OrigenPruebas Origen { get; }

    protected HttpClient Cliente
    {
        get
        {
            Skip.If(Origen.MotivoOmision is not null, Origen.MotivoOmision);
            _fabrica ??= Origen.CrearFabrica();
            return _cliente ??= _fabrica.CreateClient();
        }
    }

    public void Dispose()
    {
        _cliente?.Dispose();
        _fabrica?.Dispose();
        Origen.Dispose();
        GC.SuppressFinalize(this);
    }
}
