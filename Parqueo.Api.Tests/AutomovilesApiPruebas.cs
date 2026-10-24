using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Parqueo.Api.Tests.Infraestructura;

namespace Parqueo.Api.Tests;

/// <summary>Pruebas HTTP de /api/automoviles, comunes a JSON y PostgreSQL.</summary>
public abstract class AutomovilesApiPruebasBase : ApiPruebasBase
{
    private const string Ruta = "/api/automoviles";

    protected AutomovilesApiPruebasBase(OrigenPruebas origen)
        : base(origen)
    {
    }

    [SkippableFact]
    public async Task Get_SinFiltros_DevuelveTodosYElOrigenEnElEncabezado()
    {
        var respuesta = await Cliente.GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(Origen.Nombre, respuesta.Headers.GetValues("X-Origen-Datos").Single());
        var cuerpo = await respuesta.LeerJsonAsync();
        Assert.Equal([1, 2, 3, 4, 5, 6], cuerpo.Ids());
        Assert.False(cuerpo[0].TryGetProperty("ingresos", out _)); // los listados no anidan ingresos
    }

    [SkippableFact]
    public async Task Get_Paginado_DevuelveLaPaginaPedida()
    {
        var cuerpo = await (await Cliente.GetAsync($"{Ruta}?pagina=2&tamanoPagina=4")).LeerJsonAsync();
        Assert.Equal([5, 6], cuerpo.Ids());
    }

    [SkippableTheory]
    [InlineData("color=ro", new[] { 1, 3 })]
    [InlineData("anioDesde=2015&anioHasta=2019", new[] { 1, 3, 5 })]
    [InlineData("fabricante=DA", new[] { 2, 6 })]
    [InlineData("tipo=sedan", new[] { 1, 3 })]
    [InlineData("tipo=4x4&anioDesde=2019", new[] { 2 })]
    [InlineData("color=rojo&tipo=moto", new int[0])]
    public async Task Get_ConFiltros_AplicaBusquedaAproximadaEIntervalos(string consulta, int[] esperados)
    {
        var respuesta = await Cliente.GetAsync($"{Ruta}?{consulta}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(esperados, (await respuesta.LeerJsonAsync()).Ids());
    }

    [SkippableFact]
    public async Task GetPorId_IncluyeIngresosConParqueoYMonto()
    {
        var cuerpo = await (await Cliente.GetAsync($"{Ruta}/1")).LeerJsonAsync();

        Assert.Equal("Toyota", cuerpo.GetProperty("fabricante").GetString());
        var ingresos = cuerpo.GetProperty("ingresos");
        Assert.Equal([1, 8, 13], ingresos.Ids("consecutivo"));
        var primero = ingresos[0];
        Assert.Equal("San José", primero.GetProperty("parqueo").GetProperty("nombreProvincia").GetString());
        Assert.Equal(150m, primero.DecimalONulo("duracionEstadiaMinutos"));
        Assert.Equal(3125.00m, primero.DecimalONulo("montoTotalAPagar"));
        Assert.False(primero.TryGetProperty("automovil", out _)); // sin referencia circular
    }

    [SkippableTheory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task IdInexistente_Devuelve404ConProblemDetails(string metodo)
    {
        var solicitud = new HttpRequestMessage(new HttpMethod(metodo), $"{Ruta}/999");
        if (metodo == "PUT")
        {
            solicitud.Content = JsonContent.Create(new { color = "Gris", anio = 2020, fabricante = "Kia", tipo = "sedán" });
        }

        var respuesta = await Cliente.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("Registro no encontrado", (await respuesta.LeerJsonAsync()).GetProperty("title").GetString());
    }

    [SkippableFact]
    public async Task PostPutDelete_CicloCompleto()
    {
        // POST
        var creado = await Cliente.PostAsJsonAsync(Ruta, new { color = "Café", anio = 2022, fabricante = "Kia", tipo = "sedán" });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var cuerpoCreado = await creado.LeerJsonAsync();
        int id = cuerpoCreado.GetProperty("id").GetInt32();
        try
        {
        Assert.True(id > 6);
        Assert.EndsWith($"{Ruta}/{id}", creado.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, cuerpoCreado.GetProperty("ingresos").GetArrayLength());

        // PUT
        var actualizado = await Cliente.PutAsJsonAsync($"{Ruta}/{id}", new { color = "Café oscuro", anio = 2023, fabricante = "Kia", tipo = "4x4" });
        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);
        var cuerpoActualizado = await actualizado.LeerJsonAsync();
        Assert.Equal(("Café oscuro", 2023, "4x4"),
            (cuerpoActualizado.GetProperty("color").GetString(), cuerpoActualizado.GetProperty("anio").GetInt32(), cuerpoActualizado.GetProperty("tipo").GetString()));

        // GET confirma el cambio
        var leido = await (await Cliente.GetAsync($"{Ruta}/{id}")).LeerJsonAsync();
        Assert.Equal("Café oscuro", leido.GetProperty("color").GetString());

        // DELETE
        Assert.Equal(HttpStatusCode.NoContent, (await Cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Cliente.GetAsync($"{Ruta}/{id}")).StatusCode);
        }
        finally
        {
            await Cliente.DeleteAsync($"{Ruta}/{id}"); // limpieza aunque falle una aserción
        }
    }

    [SkippableFact]
    public async Task Get_FiltroConCaracterDeControl_SeIgnoraIgualEnAmbosOrigenes()
    {
        var respuesta = await Cliente.GetAsync($"{Ruta}?color=%00");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ids = (await respuesta.LeerJsonAsync()).Ids();
        Assert.Equal([1, 2, 3, 4, 5, 6], ids);
    }

    [SkippableTheory]
    [InlineData("""{"color":"Café","anio":2022,"fabricante":"Kia","tipo":"camión"}""", "tipo")]
    [InlineData("""{"color":"Café","anio":1800,"fabricante":"Kia","tipo":"moto"}""", "año")]
    [InlineData("""{"color":"   ","anio":2022,"fabricante":"Kia","tipo":"moto"}""", "color")]
    [InlineData("""{"anio":2022}""", "obligatorio")]
    [InlineData("""{"color":"Café","anio":"dos mil","fabricante":"Kia","tipo":"moto"}""", "")]
    [InlineData("""{"color":"Ca\u0000fé","anio":2022,"fabricante":"Kia","tipo":"moto"}""", "control")]
    public async Task Post_DatosInvalidos_Devuelve400SinGuardar(string json, string textoEsperado)
    {
        var respuesta = await Cliente.PostAsync(Ruta, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(textoEsperado, await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(6, (await (await Cliente.GetAsync(Ruta)).LeerJsonAsync()).GetArrayLength());
    }

    [SkippableFact]
    public async Task Delete_AutomovilConIngresos_Devuelve409YNoBorra()
    {
        var respuesta = await Cliente.DeleteAsync($"{Ruta}/1");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(Origen.Nombre, respuesta.Headers.GetValues("X-Origen-Datos").Single());
        Assert.Equal(HttpStatusCode.OK, (await Cliente.GetAsync($"{Ruta}/1")).StatusCode);
    }

    [SkippableTheory]
    [InlineData("anioDesde=2020&anioHasta=2010")]
    [InlineData("tamanoPagina=0")]
    [InlineData("pagina=0")]
    [InlineData("anioDesde=abc")]
    [InlineData("pagina=21474838&tamanoPagina=100")]   // el desplazamiento no cabe en un int
    public async Task Get_ParametrosInvalidos_Devuelve400(string consulta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Cliente.GetAsync($"{Ruta}?{consulta}")).StatusCode);
    }
}

public sealed class AutomovilesApiJsonPruebas : AutomovilesApiPruebasBase
{
    public AutomovilesApiJsonPruebas()
        : base(new OrigenJson())
    {
    }

    [Fact]
    public async Task Post_GuardaElRegistroEnElArchivoJson()
    {
        var creado = await Cliente.PostAsJsonAsync("/api/automoviles", new { color = "Morado", anio = 2024, fabricante = "BYD", tipo = "sedán" });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        int id = (await creado.LeerJsonAsync()).GetProperty("id").GetInt32();

        string archivo = await File.ReadAllTextAsync(Path.Combine(Origen.CarpetaJson!, "automoviles.json"));
        await Cliente.DeleteAsync($"/api/automoviles/{id}");
        Assert.Contains("\"fabricante\": \"BYD\"", archivo);
        Assert.Equal(7, id);
    }
}

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class AutomovilesApiBaseDatosPruebas : AutomovilesApiPruebasBase
{
    public AutomovilesApiBaseDatosPruebas()
        : base(new OrigenBaseDatos())
    {
    }
}
