using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Parqueo.Mvc.Servicios;
using Parqueo.Mvc.Tests.Infraestructura;

namespace Parqueo.Mvc.Tests;

/// <summary>
/// Consultas que usa el JavaScript del maestro/detalle (/MaestroDetalle/Automoviles y
/// /MaestroDetalle/Ingresos/{id}): el MVC las responde en JSON con datos que obtiene del API.
/// </summary>
public abstract class MaestroDetalleMvcPruebasBase : MvcPruebasBase
{
    protected MaestroDetalleMvcPruebasBase(OrigenPruebas origen)
        : base(origen)
    {
    }

    [SkippableFact]
    public async Task Index_MuestraElComboConLosTiposPermitidos()
    {
        string html = await HtmlAsync("/MaestroDetalle");

        Assert.Contains("<option value=\"sedán\">Sedán</option>", html);
        Assert.Contains("<option value=\"4x4\">4x4</option>", html);
        Assert.Contains("<option value=\"moto\">Moto</option>", html);
        Assert.Contains("id=\"boton-consultar\"", html);
        Assert.Contains("data-url-automoviles=\"/MaestroDetalle/Automoviles\"", html);
        Assert.Contains("/js/maestro-detalle.js", html);
    }

    [SkippableTheory]
    [InlineData("sedán", new[] { 1, 3 })]
    [InlineData("4x4", new[] { 2, 5 })]
    [InlineData("moto", new[] { 4, 6 })]
    public async Task Automoviles_DevuelveLosDelTipoConElOrigen(string tipo, int[] esperados)
    {
        var json = await JsonAsync($"/MaestroDetalle/Automoviles?tipo={Uri.EscapeDataString(tipo)}");

        Assert.Equal(esperados, json.GetProperty("automoviles").EnumerateArray().Select(a => a.GetProperty("id").GetInt32()));
        Assert.Equal(esperados.Length, json.GetProperty("total").GetInt32());
        Assert.Equal(Origen.Nombre, json.GetProperty("origenDatos").GetString());
        Assert.All(json.GetProperty("automoviles").EnumerateArray(), a => Assert.Equal(tipo, a.GetProperty("tipo").GetString()));
    }

    [SkippableTheory]
    [InlineData("")]
    [InlineData("?tipo=camión")]
    [InlineData("?tipo=sedan")]   // el combo envía el valor exacto "sedán"
    public async Task Automoviles_TipoInvalido_Devuelve400(string consulta)
    {
        var respuesta = await Navegador.GetAsync($"/MaestroDetalle/Automoviles{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Seleccione un tipo de automóvil válido", await respuesta.Content.ReadAsStringAsync());
    }

    [SkippableFact]
    public async Task Automoviles_NoSeLimitaALaPrimeraPaginaDelApi()
    {
        var creados = new List<int>();
        try
        {
            for (int i = 0; i < 11; i++)
            {
                creados.Add((await ClienteApi.CrearAutomovilAsync(new AutomovilSolicitudApi($"Prueba {i}", 2020, "Vespa", "moto"), default)).Datos!.Id);
            }

            var json = await JsonAsync("/MaestroDetalle/Automoviles?tipo=moto");

            Assert.Equal(13, json.GetProperty("total").GetInt32()); // 2 originales + 11 temporales (> 10 por página)
            Assert.Equal(13, json.GetProperty("automoviles").GetArrayLength());
        }
        finally
        {
            foreach (int id in creados)
            {
                await ClienteApi.BorrarAutomovilAsync(id, default);
            }
        }
    }

    [SkippableFact]
    public async Task Ingresos_DevuelveSoloLosDelAutomovilConIdDeParqueoYCamposDelApi()
    {
        var json = await JsonAsync("/MaestroDetalle/Ingresos/2");

        Assert.Equal(2, json.GetProperty("automovil").GetProperty("id").GetInt32());
        var ingresos = json.GetProperty("ingresos").EnumerateArray().ToList();
        Assert.Equal([2, 6, 16], ingresos.Select(i => i.GetProperty("consecutivo").GetInt32())); // por fecha de entrada
        var ingreso6 = ingresos.Single(i => i.GetProperty("consecutivo").GetInt32() == 6);
        Assert.Equal(1, ingreso6.GetProperty("idParqueo").GetInt32());
        Assert.Equal("2026-10-05T12:00:00", ingreso6.GetProperty("fechaHoraEntrada").GetString());
        Assert.Equal("2026-10-05T13:20:00", ingreso6.GetProperty("fechaHoraSalida").GetString());
        Assert.Equal(80m, ingreso6.GetProperty("duracionEstadiaMinutos").GetDecimal());
        Assert.Equal(1.3333m, ingreso6.GetProperty("duracionEstadiaHoras").GetDecimal());
        Assert.Equal(1666.67m, ingreso6.GetProperty("montoACancelar").GetDecimal());

        var enCurso = ingresos.Single(i => i.GetProperty("consecutivo").GetInt32() == 16);
        foreach (string campo in new[] { "fechaHoraSalida", "duracionEstadiaMinutos", "duracionEstadiaHoras", "montoACancelar" })
        {
            Assert.Equal(JsonValueKind.Null, enCurso.GetProperty(campo).ValueKind); // null, no 0
        }
    }

    [SkippableFact]
    public async Task Ingresos_AutomovilSinIngresos_DevuelveListaVacia()
    {
        int id = (await ClienteApi.CrearAutomovilAsync(new AutomovilSolicitudApi("Sin ingresos", 2024, "BYD", "sedán"), default)).Datos!.Id;
        try
        {
            var json = await JsonAsync($"/MaestroDetalle/Ingresos/{id}");

            Assert.Equal(id, json.GetProperty("automovil").GetProperty("id").GetInt32());
            Assert.Equal(0, json.GetProperty("ingresos").GetArrayLength());
        }
        finally
        {
            await ClienteApi.BorrarAutomovilAsync(id, default);
        }
    }

    [SkippableFact]
    public async Task Ingresos_AutomovilInexistente_Devuelve404ConProblemDetails()
    {
        var respuesta = await Navegador.GetAsync("/MaestroDetalle/Ingresos/999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("no tiene un automóvil con id 999", await respuesta.Content.ReadAsStringAsync());
    }

    private async Task<JsonElement> JsonAsync(string url)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get, url);
        solicitud.Headers.Accept.ParseAdd("application/json");
        var respuesta = await Navegador.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }
}

public sealed class MaestroDetalleMvcJsonPruebas : MaestroDetalleMvcPruebasBase
{
    public MaestroDetalleMvcJsonPruebas()
        : base(new OrigenJson())
    {
    }
}

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class MaestroDetalleMvcBaseDatosPruebas : MaestroDetalleMvcPruebasBase
{
    public MaestroDetalleMvcBaseDatosPruebas()
        : base(new OrigenBaseDatos())
    {
    }
}
