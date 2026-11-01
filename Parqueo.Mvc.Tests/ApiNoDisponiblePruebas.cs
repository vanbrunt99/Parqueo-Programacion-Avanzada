using System.Net;
using Parqueo.Mvc.Tests.Infraestructura;

namespace Parqueo.Mvc.Tests;

/// <summary>
/// Si el API no está levantado, el MVC no muestra una excepción: informa que no pudo
/// comunicarse con la URL configurada (pantallas) o devuelve ProblemDetails 503 (JavaScript).
/// </summary>
public sealed class ApiNoDisponiblePruebas : IDisposable
{
    // Puerto 1: no hay ningún servicio escuchando.
    private readonly MvcEnMemoria _mvc = new("http://127.0.0.1:1/");

    [Fact]
    public async Task Pantalla_ApiCaido_Muestra503ConLaUrlConfigurada()
    {
        var respuesta = await _mvc.CrearNavegador().GetAsync("/Automoviles");
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("No se pudo comunicar con el API del parqueo en http://127.0.0.1:1/", html);
    }

    [Fact]
    public async Task ConsultaJson_ApiCaido_DevuelveProblemDetails503()
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get, "/Ingresos");
        solicitud.Headers.Accept.ParseAdd("application/json");

        var respuesta = await _mvc.CrearNavegador().SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("API no disponible", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Inicio_ApiCaido_InformaElEstadoSinFallar()
    {
        var respuesta = await _mvc.CrearNavegador().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no disponible", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Formulario_ApiCaidoAlGuardar_ConservaLoEscritoCon503()
    {
        // El GET de Crear automóvil no consulta el API, así que se obtiene el token aunque el API esté caído.
        var navegador = _mvc.CrearNavegador();
        string formulario = await (await navegador.GetAsync("/Automoviles/Crear")).Content.ReadAsStringAsync();
        string token = System.Text.RegularExpressions.Regex.Match(formulario, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var respuesta = await navegador.PostAsync("/Automoviles/Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Color"] = "Turquesa",
            ["Anio"] = "2022",
            ["Fabricante"] = "Kia",
            ["Tipo"] = "sedán",
            ["__RequestVerificationToken"] = token,
        }));
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("No se pudo comunicar con el API del parqueo en http://127.0.0.1:1/", html);
        Assert.Contains("Los datos escritos se conservan", html);
        Assert.Contains("value=\"Turquesa\"", html);
        Assert.Contains("value=\"Kia\"", html);
        Assert.Contains("<form", html); // es el formulario, no la página genérica
    }

    [Fact]
    public async Task Listado_BaseDeDatosCaida_ConservaEl503DelApi()
    {
        // API real con origen BaseDatos y una cadena hacia un puerto cerrado (no requiere PostgreSQL).
        using var api = new ApiEnMemoria(new()
        {
            ["OrigenDatos"] = "BaseDatos",
            ["ConnectionStrings:ParqueoDb"] = "Host=127.0.0.1;Port=1;Database=nada;Username=nadie;Password=nada;Timeout=2",
        });
        using var mvc = new MvcEnMemoria(api);

        var respuesta = await mvc.CrearNavegador().GetAsync("/Automoviles");
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("Base de datos no disponible", html);
        Assert.DoesNotContain("El API rechazó la consulta", html);
    }

    public void Dispose() => _mvc.Dispose();
}
