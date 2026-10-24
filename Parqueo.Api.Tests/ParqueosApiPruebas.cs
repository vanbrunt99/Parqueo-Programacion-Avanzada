using System.Net;
using System.Net.Http.Json;
using Parqueo.Api.Tests.Infraestructura;

namespace Parqueo.Api.Tests;

/// <summary>Pruebas HTTP de /api/parqueos, comunes a JSON y PostgreSQL.</summary>
public abstract class ParqueosApiPruebasBase : ApiPruebasBase
{
    private const string Ruta = "/api/parqueos";

    protected ParqueosApiPruebasBase(OrigenPruebas origen)
        : base(origen)
    {
    }

    [SkippableFact]
    public async Task Get_SinFiltros_DevuelveTodos()
    {
        var respuesta = await Cliente.GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(Origen.Nombre, respuesta.Headers.GetValues("X-Origen-Datos").Single());
        var cuerpo = await respuesta.LeerJsonAsync();
        Assert.Equal([1, 2, 3], cuerpo.Ids());
        Assert.Equal(675.50m, cuerpo[2].GetProperty("precioPorHora").GetDecimal());
    }

    [SkippableTheory]
    [InlineData("provincia=san jose", new[] { 1 })]
    [InlineData("provincia=HEREDIA", new[] { 2 })]
    [InlineData("nombre=mercado", new[] { 2 })]
    [InlineData("nombre=parqueo", new[] { 1, 2, 3 })]
    [InlineData("precioMinimo=700&precioMaximo=1250", new[] { 1, 2 })]
    [InlineData("precioMaximo=675.50", new[] { 3 })]
    [InlineData("precioMinimo=675.51", new[] { 1, 2 })]
    [InlineData("nombre=parqueo&precioMaximo=1000", new[] { 2, 3 })]
    [InlineData("provincia=Lim%C3%B3n", new int[0])]
    public async Task Get_ConFiltros_AplicaBusquedaAproximadaYRangoDePrecio(string consulta, int[] esperados)
    {
        var respuesta = await Cliente.GetAsync($"{Ruta}?{consulta}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(esperados, (await respuesta.LeerJsonAsync()).Ids());
    }

    [SkippableFact]
    public async Task GetPorId_IncluyeIngresosConAutomovilYMonto()
    {
        var cuerpo = await (await Cliente.GetAsync($"{Ruta}/3")).LeerJsonAsync();

        Assert.Equal("Cartago", cuerpo.GetProperty("nombreProvincia").GetString());
        var ingresos = cuerpo.GetProperty("ingresos");
        Assert.Equal([4, 7, 10, 13], ingresos.Ids("consecutivo"));
        var ingreso4 = ingresos[0];
        Assert.Equal("Mitsubishi", ingreso4.GetProperty("automovil").GetProperty("fabricante").GetString());
        Assert.Equal(1857.63m, ingreso4.DecimalONulo("montoTotalAPagar"));
        Assert.False(ingreso4.TryGetProperty("parqueo", out _)); // sin referencia circular
    }

    [SkippableTheory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task IdInexistente_Devuelve404(string metodo)
    {
        var solicitud = new HttpRequestMessage(new HttpMethod(metodo), $"{Ruta}/999");
        if (metodo == "PUT")
        {
            solicitud.Content = JsonContent.Create(new { nombreProvincia = "Limón", nombre = "X", precioPorHora = 100 });
        }

        Assert.Equal(HttpStatusCode.NotFound, (await Cliente.SendAsync(solicitud)).StatusCode);
    }

    [SkippableFact]
    public async Task PostPutDelete_CicloCompleto()
    {
        var creado = await Cliente.PostAsJsonAsync(Ruta, new { nombreProvincia = "Limón", nombre = "Parqueo del Puerto", precioPorHora = 950.25m });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var cuerpoCreado = await creado.LeerJsonAsync();
        int id = cuerpoCreado.GetProperty("id").GetInt32();
        Assert.True(id > 3);
        Assert.Equal(950.25m, cuerpoCreado.GetProperty("precioPorHora").GetDecimal());

        var actualizado = await Cliente.PutAsJsonAsync($"{Ruta}/{id}", new { nombreProvincia = "Limón", nombre = "Parqueo del Puerto Moín", precioPorHora = 1000m });
        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);
        var cuerpoActualizado = await actualizado.LeerJsonAsync();
        Assert.Equal("Parqueo del Puerto Moín", cuerpoActualizado.GetProperty("nombre").GetString());
        Assert.Equal(1000m, cuerpoActualizado.GetProperty("precioPorHora").GetDecimal());

        var filtrado = await (await Cliente.GetAsync($"{Ruta}?provincia=limon")).LeerJsonAsync();
        Assert.Equal([id], filtrado.Ids());

        Assert.Equal(HttpStatusCode.NoContent, (await Cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Cliente.GetAsync($"{Ruta}/{id}")).StatusCode);
    }

    [SkippableTheory]
    [InlineData("""{"nombreProvincia":"Panamá","nombre":"X","precioPorHora":500}""", "provincia")]
    [InlineData("""{"nombreProvincia":"Limón","nombre":"X","precioPorHora":0}""", "precio")]
    [InlineData("""{"nombreProvincia":"Limón","nombre":"X","precioPorHora":-10}""", "precio")]
    [InlineData("""{"nombreProvincia":"Limón","nombre":"X","precioPorHora":100.125}""", "decimales")]
    [InlineData("""{"nombreProvincia":"Limón"}""", "obligatorio")]
    public async Task Post_DatosInvalidos_Devuelve400SinGuardar(string json, string textoEsperado)
    {
        var respuesta = await Cliente.PostAsync(Ruta, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(textoEsperado, await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, (await (await Cliente.GetAsync(Ruta)).LeerJsonAsync()).GetArrayLength());
    }

    [SkippableFact]
    public async Task Delete_ParqueoConIngresos_Devuelve409()
    {
        Assert.Equal(HttpStatusCode.Conflict, (await Cliente.DeleteAsync($"{Ruta}/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Cliente.GetAsync($"{Ruta}/1")).StatusCode);
    }

    [SkippableTheory]
    [InlineData("precioMinimo=1000&precioMaximo=500")]
    [InlineData("precioMinimo=-1")]
    [InlineData("precioMaximo=mucho")]
    public async Task Get_RangoDePrecioInvalido_Devuelve400(string consulta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Cliente.GetAsync($"{Ruta}?{consulta}")).StatusCode);
    }
}

public sealed class ParqueosApiJsonPruebas : ParqueosApiPruebasBase
{
    public ParqueosApiJsonPruebas()
        : base(new OrigenJson())
    {
    }
}

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ParqueosApiBaseDatosPruebas : ParqueosApiPruebasBase
{
    public ParqueosApiBaseDatosPruebas()
        : base(new OrigenBaseDatos())
    {
    }
}
