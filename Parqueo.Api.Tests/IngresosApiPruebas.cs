using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Parqueo.Api.Tests.Infraestructura;

namespace Parqueo.Api.Tests;

/// <summary>Pruebas HTTP de /api/ingresos, comunes a JSON y PostgreSQL.</summary>
public abstract class IngresosApiPruebasBase : ApiPruebasBase
{
    private const string Ruta = "/api/ingresos";
    private const string Octubre = "desde=2026-10-01T00:00:00&hasta=2026-10-31T23:59:59";

    protected IngresosApiPruebasBase(OrigenPruebas origen)
        : base(origen)
    {
    }

    [SkippableFact]
    public async Task Get_Listado_IncluyeAutomovilParqueoYCamposCalculados()
    {
        var respuesta = await Cliente.GetAsync($"{Ruta}?tamanoPagina=100");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(Origen.Nombre, respuesta.Headers.GetValues("X-Origen-Datos").Single());
        var cuerpo = await respuesta.LeerJsonAsync();
        Assert.Equal(Enumerable.Range(1, 17), cuerpo.Ids("consecutivo"));
        foreach (var ingreso in cuerpo.EnumerateArray())
        {
            Assert.Equal(ingreso.GetProperty("idAutomovil").GetInt32(), ingreso.GetProperty("automovil").GetProperty("id").GetInt32());
            Assert.Equal(ingreso.GetProperty("idParqueo").GetInt32(), ingreso.GetProperty("parqueo").GetProperty("id").GetInt32());
        }
    }

    [SkippableFact]
    public async Task Get_ListadoPorDefecto_DevuelveLaPrimeraPaginaDeDiez()
    {
        var cuerpo = await (await Cliente.GetAsync(Ruta)).LeerJsonAsync();
        Assert.Equal(Enumerable.Range(1, 10), cuerpo.Ids("consecutivo"));
    }

    [SkippableFact]
    public async Task GetPorId_EstadiaFinalizada_CalculaMinutosHorasYMonto()
    {
        var ingreso = await (await Cliente.GetAsync($"{Ruta}/6")).LeerJsonAsync();

        Assert.Equal(80m, ingreso.DecimalONulo("duracionEstadiaMinutos"));
        Assert.Equal(1.3333m, ingreso.DecimalONulo("duracionEstadiaHoras"));
        Assert.Equal(1666.67m, ingreso.DecimalONulo("montoTotalAPagar"));
        Assert.Equal("Hyundai", ingreso.GetProperty("automovil").GetProperty("fabricante").GetString());
        Assert.Equal(1250.00m, ingreso.GetProperty("parqueo").GetProperty("precioPorHora").GetDecimal());
    }

    [SkippableFact]
    public async Task GetPorId_SinSalida_LosTresCamposSonNull()
    {
        var ingreso = await (await Cliente.GetAsync($"{Ruta}/16")).LeerJsonAsync();

        Assert.Equal(JsonValueKind.Null, ingreso.GetProperty("fechaHoraSalida").ValueKind);
        Assert.Equal(JsonValueKind.Null, ingreso.GetProperty("duracionEstadiaMinutos").ValueKind);
        Assert.Equal(JsonValueKind.Null, ingreso.GetProperty("duracionEstadiaHoras").ValueKind);
        Assert.Equal(JsonValueKind.Null, ingreso.GetProperty("montoTotalAPagar").ValueKind);
    }

    [SkippableFact]
    public async Task PorTipo_4x4EnOctubre_IncluyeEntradaSalidaYMonto()
    {
        var cuerpo = await (await Cliente.GetAsync($"{Ruta}/por-tipo?tipo=4x4&{Octubre}")).LeerJsonAsync();

        Assert.Equal([2, 4, 6, 11, 15, 16], cuerpo.Ids("consecutivo"));
        Assert.All(cuerpo.EnumerateArray(), r => Assert.Equal("4x4", r.GetProperty("automovil").GetProperty("tipo").GetString()));
        Assert.Equal([600.00m, 1857.63m, 1666.67m, 1200.00m, 2812.50m, null],
            cuerpo.EnumerateArray().Select(r => r.DecimalONulo("montoACancelar")));
        Assert.Equal("2026-10-01T09:45:00", cuerpo[0].GetProperty("fechaHoraSalida").GetString());
    }

    [SkippableTheory]
    [InlineData("tipo=sedan&desde=2026-10-04T00:00:00&hasta=2026-10-08T23:59:59", new[] { 5, 8, 9 })]
    [InlineData("tipo=moto&desde=2026-10-02T08:10:00&hasta=2026-10-09T18:15:00", new[] { 3, 7, 10 })] // extremos exactos
    [InlineData("tipo=moto&desde=2026-11-01T00:00:00&hasta=2026-11-30T23:59:59", new int[0])]
    public async Task PorTipo_RespetaElIntervalo(string consulta, int[] esperados)
    {
        var cuerpo = await (await Cliente.GetAsync($"{Ruta}/por-tipo?{consulta}")).LeerJsonAsync();
        Assert.Equal(esperados, cuerpo.Ids("consecutivo"));
    }

    [SkippableFact]
    public async Task PorProvincia_HerediaConEstadiaEnCurso()
    {
        var cuerpo = await (await Cliente.GetAsync($"{Ruta}/por-provincia?provincia=HEREDIA&desde=2026-10-15T00:00:00&hasta=2026-10-17T23:59:59")).LeerJsonAsync();

        Assert.Equal([14, 17], cuerpo.Ids("consecutivo"));
        Assert.Equal([266.67m, null], cuerpo.EnumerateArray().Select(r => r.DecimalONulo("montoACancelar")));
        Assert.All(cuerpo.EnumerateArray(), r => Assert.Equal("Heredia", r.GetProperty("nombreProvincia").GetString()));
    }

    [SkippableTheory]
    [InlineData("provincia=san jose", new[] { 1, 3, 6, 9, 12, 15, 16 })]
    [InlineData("provincia=cartago", new[] { 4, 7, 10, 13 })]
    [InlineData("provincia=Lim%C3%B3n", new int[0])]
    public async Task PorProvincia_EnOctubre(string consulta, int[] esperados)
    {
        var cuerpo = await (await Cliente.GetAsync($"{Ruta}/por-provincia?{consulta}&{Octubre}")).LeerJsonAsync();
        Assert.Equal(esperados, cuerpo.Ids("consecutivo"));
    }

    [SkippableFact]
    public async Task PrecioPorHora_DevuelveLaTarifaO404()
    {
        var precio = await (await Cliente.GetAsync($"{Ruta}/precio-por-hora/3")).LeerJsonAsync();
        Assert.Equal(675.50m, precio.GetProperty("precioPorHora").GetDecimal());
        Assert.Equal(HttpStatusCode.NotFound, (await Cliente.GetAsync($"{Ruta}/precio-por-hora/999")).StatusCode);
    }

    [SkippableFact]
    public async Task PostPutDelete_CicloCompletoConCamposCalculados()
    {
        // POST: estadía en curso
        var creado = await Cliente.PostAsJsonAsync(Ruta, new { idParqueo = 3, idAutomovil = 6, fechaHoraEntrada = "2026-10-18T10:00:00" });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var cuerpo = await creado.LeerJsonAsync();
        int id = cuerpo.GetProperty("consecutivo").GetInt32();
        Assert.True(id > 17);
        Assert.Null(cuerpo.DecimalONulo("montoTotalAPagar"));
        Assert.Equal("Honda", cuerpo.GetProperty("automovil").GetProperty("fabricante").GetString());

        // PUT: registra la salida (90 minutos a 675.50/h)
        var actualizado = await Cliente.PutAsJsonAsync($"{Ruta}/{id}",
            new { idParqueo = 3, idAutomovil = 6, fechaHoraEntrada = "2026-10-18T10:00:00", fechaHoraSalida = "2026-10-18T11:30:00" });
        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);
        var cuerpoActualizado = await actualizado.LeerJsonAsync();
        Assert.Equal(90m, cuerpoActualizado.DecimalONulo("duracionEstadiaMinutos"));
        Assert.Equal(1.5m, cuerpoActualizado.DecimalONulo("duracionEstadiaHoras"));
        Assert.Equal(1013.25m, cuerpoActualizado.DecimalONulo("montoTotalAPagar"));

        // DELETE
        Assert.Equal(HttpStatusCode.NoContent, (await Cliente.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Cliente.GetAsync($"{Ruta}/{id}")).StatusCode);
    }

    [SkippableFact]
    public async Task Post_HoraExacta_CobraUnaHora()
    {
        var creado = await Cliente.PostAsJsonAsync(Ruta,
            new { idParqueo = 1, idAutomovil = 1, fechaHoraEntrada = "2026-10-18T08:00:00", fechaHoraSalida = "2026-10-18T09:00:00" });
        var cuerpo = await creado.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.Equal((60m, 1m, 1250.00m),
            (cuerpo.DecimalONulo("duracionEstadiaMinutos"), cuerpo.DecimalONulo("duracionEstadiaHoras"), cuerpo.DecimalONulo("montoTotalAPagar")));
        Assert.Equal(HttpStatusCode.NoContent, (await Cliente.DeleteAsync($"{Ruta}/{cuerpo.GetProperty("consecutivo").GetInt32()}")).StatusCode);
    }

    [SkippableTheory]
    [InlineData("""{"idParqueo":999,"idAutomovil":1,"fechaHoraEntrada":"2026-10-18T08:00:00"}""", HttpStatusCode.Conflict)]
    [InlineData("""{"idParqueo":1,"idAutomovil":999,"fechaHoraEntrada":"2026-10-18T08:00:00"}""", HttpStatusCode.Conflict)]
    [InlineData("""{"idParqueo":1,"idAutomovil":1,"fechaHoraEntrada":"2026-10-18T10:00:00","fechaHoraSalida":"2026-10-18T10:00:00"}""", HttpStatusCode.BadRequest)]
    [InlineData("""{"idParqueo":1,"idAutomovil":1,"fechaHoraEntrada":"2026-10-18T10:00:00","fechaHoraSalida":"2026-10-18T09:00:00"}""", HttpStatusCode.BadRequest)]
    [InlineData("""{"idParqueo":1,"idAutomovil":1,"fechaHoraEntrada":"2026-10-18T16:00:00Z"}""", HttpStatusCode.BadRequest)]
    [InlineData("""{"idParqueo":1,"idAutomovil":1,"fechaHoraEntrada":"2026-13-40T25:00:00"}""", HttpStatusCode.BadRequest)]
    [InlineData("""{"idParqueo":1,"idAutomovil":1}""", HttpStatusCode.BadRequest)]
    public async Task Post_DatosInvalidosOReferenciasInexistentes_NoGuarda(string json, HttpStatusCode esperado)
    {
        var respuesta = await Cliente.PostAsync(Ruta, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal(17, (await (await Cliente.GetAsync($"{Ruta}?tamanoPagina=100")).LeerJsonAsync()).GetArrayLength());
    }

    [SkippableFact]
    public async Task Put_HaciaParqueoInexistente_Devuelve409YNoModifica()
    {
        var respuesta = await Cliente.PutAsJsonAsync($"{Ruta}/1",
            new { idParqueo = 999, idAutomovil = 1, fechaHoraEntrada = "2026-10-01T07:45:00", fechaHoraSalida = "2026-10-01T10:15:00" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(1, (await (await Cliente.GetAsync($"{Ruta}/1")).LeerJsonAsync()).GetProperty("idParqueo").GetInt32());
    }

    [SkippableTheory]
    [InlineData("/por-tipo?desde=2026-10-01T00:00:00&hasta=2026-10-31T23:59:59")]             // falta el tipo
    [InlineData("/por-tipo?tipo=4x4&desde=2026-10-31T00:00:00&hasta=2026-10-01T00:00:00")]    // intervalo invertido
    [InlineData("/por-provincia?provincia=heredia&desde=ayer&hasta=2026-10-31T00:00:00")]      // fecha inválida
    [InlineData("/por-provincia?provincia=heredia&hasta=2026-10-31T00:00:00")]                 // falta desde
    public async Task Consultas_ParametrosInvalidos_Devuelve400(string consulta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Cliente.GetAsync($"{Ruta}{consulta}")).StatusCode);
    }

    [SkippableTheory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task IdInexistente_Devuelve404(string metodo)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Cliente.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), $"{Ruta}/999"))).StatusCode);
    }
}

public sealed class IngresosApiJsonPruebas : IngresosApiPruebasBase
{
    public IngresosApiJsonPruebas()
        : base(new OrigenJson())
    {
    }
}

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class IngresosApiBaseDatosPruebas : IngresosApiPruebasBase
{
    public IngresosApiBaseDatosPruebas()
        : base(new OrigenBaseDatos())
    {
    }
}
