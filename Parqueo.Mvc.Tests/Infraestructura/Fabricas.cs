using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Tests.Infraestructura;

/// <summary>
/// API real del Trabajo 03 levantado en memoria con el origen indicado. Se usa un tipo del
/// ensamblado del API como marcador (el MVC también tiene una clase Program).
/// </summary>
public sealed class ApiEnMemoria : WebApplicationFactory<Parqueo.Api.Controladores.AutomovilesController>
{
    private readonly Dictionary<string, string?> _configuracion;

    static ApiEnMemoria()
    {
        // Evita agotar los observadores de archivos (inotify) con muchos hosts de prueba.
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
    }

    public ApiEnMemoria(Dictionary<string, string?> configuracion)
    {
        _configuracion = configuracion;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ambiente de pruebas: el API no carga User Secrets; usa solo esta configuración.
        builder.UseEnvironment("Pruebas");
        foreach (var (clave, valor) in _configuracion)
        {
            builder.UseSetting(clave, valor);
        }
    }
}

/// <summary>
/// MVC en memoria. Su cliente tipado <see cref="ClienteApiParqueo"/> se conecta al API en
/// memoria (o a la URL indicada) en lugar de abrir un puerto: el recorrido
/// vista → controlador MVC → HTTP → API → repositorios es el mismo que en ejecución real.
/// </summary>
public sealed class MvcEnMemoria : WebApplicationFactory<Parqueo.Mvc.Controladores.InicioController>
{
    private readonly ApiEnMemoria? _api;
    private readonly string _urlApi;

    public MvcEnMemoria(ApiEnMemoria api)
    {
        _api = api;
        _urlApi = "http://localhost/";
    }

    /// <summary>MVC apuntando a una URL real (por ejemplo, un puerto cerrado para simular el API caído).</summary>
    public MvcEnMemoria(string urlApi)
    {
        _urlApi = urlApi;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Pruebas");
        builder.UseSetting("Api:UrlBase", _urlApi);
        if (_api is not null)
        {
            var manejador = _api.Server.CreateHandler();
            builder.ConfigureTestServices(servicios =>
                servicios.AddHttpClient<ClienteApiParqueo>().ConfigurePrimaryHttpMessageHandler(() => manejador));
        }
    }

    /// <summary>Cliente del navegador simulado: conserva cookies y no sigue redirecciones.</summary>
    public HttpClient CrearNavegador() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
}

/// <summary>Origen de datos con el que se levanta el API en una clase de pruebas.</summary>
public abstract class OrigenPruebas : IDisposable
{
    public abstract string Nombre { get; }

    public virtual string? MotivoOmision => null;

    public abstract ApiEnMemoria CrearApi();

    public virtual void Dispose() => GC.SuppressFinalize(this);
}

/// <summary>Copia temporal de Datos/*.json: los archivos entregados nunca se modifican.</summary>
public sealed class OrigenJson : OrigenPruebas
{
    public OrigenJson()
    {
        Carpeta = Path.Combine(Path.GetTempPath(), "parqueo-mvc-pruebas", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Carpeta);
        foreach (string archivo in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "DatosOriginales"), "*.json"))
        {
            File.Copy(archivo, Path.Combine(Carpeta, Path.GetFileName(archivo)));
        }
    }

    public string Carpeta { get; }

    public override string Nombre => "Json";

    public override ApiEnMemoria CrearApi() => new(new()
    {
        ["OrigenDatos"] = "Json",
        ["RutaDatosJson"] = Carpeta,
    });

    public override void Dispose()
    {
        try
        {
            Directory.Delete(Carpeta, recursive: true);
        }
        catch (IOException)
        {
            // Una carpeta temporal que no se pudo borrar no invalida la prueba.
        }

        base.Dispose();
    }
}

/// <summary>PostgreSQL indicado en PARQUEO_PRUEBAS_BD (base local de pruebas, no la del profesor).</summary>
public sealed class OrigenBaseDatos : OrigenPruebas
{
    public const string VariableEntorno = "PARQUEO_PRUEBAS_BD";

    private static string? Cadena => Environment.GetEnvironmentVariable(VariableEntorno);

    public override string Nombre => "BaseDatos";

    public override string? MotivoOmision => string.IsNullOrWhiteSpace(Cadena)
        ? $"Defina la variable de entorno {VariableEntorno} para probar el MVC con el API sobre PostgreSQL."
        : null;

    public override ApiEnMemoria CrearApi() => new(new()
    {
        ["OrigenDatos"] = "BaseDatos",
        ["ConnectionStrings:ParqueoDb"] = Cadena,
    });
}

/// <summary>Las pruebas contra PostgreSQL escriben datos reales: se ejecutan en serie.</summary>
[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionBaseDatos : ICollectionFixture<BloqueoBaseDatos>
{
    public const string Nombre = "MvcBaseDatos";
}

/// <summary>
/// Candado consultivo de PostgreSQL con la misma llave que Parqueo.Tests y Parqueo.Api.Tests,
/// para que las pruebas de base de datos de los tres proyectos no se ejecuten a la vez.
/// </summary>
public sealed class BloqueoBaseDatos : IDisposable
{
    public const long Llave = 20261031;

    private readonly Npgsql.NpgsqlConnection? _conexion;

    public BloqueoBaseDatos()
    {
        string? cadena = Environment.GetEnvironmentVariable(OrigenBaseDatos.VariableEntorno);
        if (string.IsNullOrWhiteSpace(cadena))
        {
            return;
        }

        _conexion = new Npgsql.NpgsqlConnection(cadena);
        _conexion.Open();
        using var comando = new Npgsql.NpgsqlCommand($"SELECT pg_advisory_lock({Llave})", _conexion);
        comando.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_conexion is null)
        {
            return;
        }

        using (var comando = new Npgsql.NpgsqlCommand($"SELECT pg_advisory_unlock({Llave})", _conexion))
        {
            comando.ExecuteNonQuery();
        }

        _conexion.Dispose();
    }
}

/// <summary>
/// Base de las pruebas del MVC: levanta el API con el origen indicado y el MVC conectado a él.
/// Las clases derivadas solo eligen el origen (Json o BaseDatos), así las mismas aserciones
/// comprueban que el MVC funciona igual con ambos.
/// </summary>
public abstract class MvcPruebasBase : IDisposable
{
    private ApiEnMemoria? _api;
    private MvcEnMemoria? _mvc;
    private HttpClient? _navegador;
    private ClienteApiParqueo? _clienteApi;

    protected MvcPruebasBase(OrigenPruebas origen)
    {
        Origen = origen;
    }

    protected OrigenPruebas Origen { get; }

    private ApiEnMemoria Api
    {
        get
        {
            Skip.If(Origen.MotivoOmision is not null, Origen.MotivoOmision);
            return _api ??= Origen.CrearApi();
        }
    }

    /// <summary>Navegador simulado contra el MVC.</summary>
    protected HttpClient Navegador => _navegador ??= (_mvc ??= new MvcEnMemoria(Api)).CrearNavegador();

    /// <summary>Cliente tipado del MVC apuntando al API en memoria (para preparar y limpiar datos).</summary>
    protected ClienteApiParqueo ClienteApi
    {
        get
        {
            if (_clienteApi is null)
            {
                var http = Api.CreateClient();
                _clienteApi = new ClienteApiParqueo(http);
            }

            return _clienteApi;
        }
    }

    protected async Task<string> HtmlAsync(string url)
    {
        var respuesta = await Navegador.GetAsync(url);
        Assert.True(respuesta.IsSuccessStatusCode, $"GET {url} devolvió {(int)respuesta.StatusCode}");
        return await respuesta.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Envía un formulario como lo haría el navegador: primero lo carga para obtener el token
    /// antiforgery (y su cookie) y luego hace el POST.
    /// </summary>
    protected async Task<HttpResponseMessage> EnviarFormularioAsync(string urlFormulario, string urlDestino, IDictionary<string, string> campos)
    {
        string token = await ObtenerTokenAsync(urlFormulario);
        var datos = new Dictionary<string, string>(campos) { ["__RequestVerificationToken"] = token };
        return await Navegador.PostAsync(urlDestino, new FormUrlEncodedContent(datos));
    }

    protected async Task<string> ObtenerTokenAsync(string urlFormulario)
    {
        string html = await HtmlAsync(urlFormulario);
        var coincidencia = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(coincidencia.Success, $"El formulario {urlFormulario} no tiene token antiforgery.");
        return coincidencia.Groups[1].Value;
    }

    protected static int IdDeRedireccion(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var coincidencia = Regex.Match(respuesta.Headers.Location!.ToString(), @"/Detalles/(\d+)");
        Assert.True(coincidencia.Success, $"Redirección inesperada: {respuesta.Headers.Location}");
        return int.Parse(coincidencia.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Ids de las filas (atributo data-*) en el HTML.</summary>
    protected static int[] IdsDeFilas(string html, string atributo = "data-id") =>
        Regex.Matches(html, $"<tr {atributo}=\"(\\d+)\"")
            .Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

    public virtual void Dispose()
    {
        _navegador?.Dispose();
        _mvc?.Dispose();
        _api?.Dispose();
        Origen.Dispose();
        GC.SuppressFinalize(this);
    }
}
