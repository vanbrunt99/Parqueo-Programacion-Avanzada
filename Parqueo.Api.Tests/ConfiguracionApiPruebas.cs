using System.Net;
using Parqueo.Api.Tests.Infraestructura;

namespace Parqueo.Api.Tests;

/// <summary>Pruebas de configuración del API que no necesitan una base de datos real.</summary>
public sealed class ConfiguracionApiPruebas
{
    /// <summary>
    /// Con una cadena de conexión a un puerto cerrado, el API debe responder 503. Esto
    /// demuestra además que usa la cadena configurada para la prueba (y no los User
    /// Secrets del desarrollador) y que el error no expone detalles de la conexión.
    /// </summary>
    [Fact]
    public async Task BaseDeDatosNoDisponible_Devuelve503SinDetallesInternos()
    {
        using var fabrica = FabricaApi.ConBaseDatos("Host=127.0.0.1;Port=1;Database=inexistente;Username=nadie;Password=secreto-de-prueba;Timeout=3");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/automoviles");
        string cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("BaseDatos", respuesta.Headers.GetValues("X-Origen-Datos").Single());
        Assert.Contains("Base de datos no disponible", cuerpo);
        Assert.DoesNotContain("secreto-de-prueba", cuerpo);
    }

    /// <summary>
    /// Una cadena de conexión mal formada es un error de configuración del servidor (500), no
    /// datos inválidos del cliente (400), y la respuesta no debe repetir la cadena.
    /// </summary>
    [Fact]
    public async Task CadenaDeConexionMalFormada_Devuelve500SinExponerla()
    {
        using var fabrica = FabricaApi.ConBaseDatos("Host=127.0.0.1;Password=\"secreto\"roto\";Port=1");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/automoviles");
        string cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains("Error interno", cuerpo);
        Assert.DoesNotContain("secreto", cuerpo);
    }
}
