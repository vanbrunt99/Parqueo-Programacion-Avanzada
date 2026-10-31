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

    public void Dispose() => _mvc.Dispose();
}
