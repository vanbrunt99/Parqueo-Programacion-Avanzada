using System.Net;
using Parqueo.Mvc.Servicios;
using Parqueo.Mvc.Tests.Infraestructura;

namespace Parqueo.Mvc.Tests;

/// <summary>Mantenimiento de ingresos en el MVC, de punta a punta contra el API.</summary>
public abstract class IngresosMvcPruebasBase : MvcPruebasBase
{
    protected IngresosMvcPruebasBase(OrigenPruebas origen)
        : base(origen)
    {
    }

    [SkippableFact]
    public async Task Index_PaginaConElTotalDelApi()
    {
        string pagina1 = await HtmlAsync("/Ingresos");
        string pagina2 = await HtmlAsync("/Ingresos?pagina=2");

        Assert.Equal(Enumerable.Range(1, 10), IdsDeFilas(pagina1, "data-consecutivo"));
        Assert.Contains("Mostrando 1–10 de 17 ingresos · página 1 de 2", pagina1);
        Assert.Equal(Enumerable.Range(11, 7), IdsDeFilas(pagina2, "data-consecutivo"));
    }

    [SkippableFact]
    public async Task Crear_FormularioCargaTodosLosAutomovilesYParqueosDelApi()
    {
        string html = await HtmlAsync("/Ingresos/Crear");

        foreach (int id in Enumerable.Range(1, 6))
        {
            Assert.Contains($"<option value=\"{id}\">#{id} · ", html);
        }

        Assert.Contains("<option value=\"3\">#3 · Parqueo Las Ruinas (Cartago)", html);
        Assert.Contains("type=\"datetime-local\"", html);
    }

    [SkippableFact]
    public async Task CrearEditarYBorrar_SalidaVaciaEsNullYLosCamposSeCalculanEnElApi()
    {
        var creado = await EnviarFormularioAsync("/Ingresos/Crear", "/Ingresos/Crear", new Dictionary<string, string>
        {
            ["IdAutomovil"] = "4",
            ["IdParqueo"] = "1",
            ["FechaHoraEntrada"] = "2026-10-31T08:00",
            ["FechaHoraSalida"] = "",
        });
        int consecutivo = IdDeRedireccion(creado);
        try
        {
            var api = (await ClienteApi.ObtenerIngresoAsync(consecutivo, default)).Datos!;
            Assert.Equal(new DateTime(2026, 10, 31, 8, 0, 0), api.FechaHoraEntrada); // sin conversión a UTC
            Assert.Null(api.FechaHoraSalida);
            Assert.Null(api.MontoTotalAPagar);
            Assert.Contains("Sin salida (en curso)", await HtmlAsync($"/Ingresos/Detalles/{consecutivo}"));

            // Registrar la salida: 80 minutos a ₡1 250/h = ₡1 666,67 (lo calcula el API).
            var editado = await EnviarFormularioAsync($"/Ingresos/Editar/{consecutivo}", $"/Ingresos/Editar/{consecutivo}", new Dictionary<string, string>
            {
                ["IdAutomovil"] = "4",
                ["IdParqueo"] = "1",
                ["FechaHoraEntrada"] = "2026-10-31T08:00",
                ["FechaHoraSalida"] = "2026-10-31T09:20",
            });
            Assert.Equal(consecutivo, IdDeRedireccion(editado));
            string detalles = await HtmlAsync($"/Ingresos/Detalles/{consecutivo}");
            Assert.Contains("₡1&#xA0;666,67", detalles);
            Assert.Contains("1,3333", detalles);

            var borrado = await EnviarFormularioAsync($"/Ingresos/Borrar/{consecutivo}", $"/Ingresos/Borrar/{consecutivo}", new Dictionary<string, string>());
            Assert.Equal(HttpStatusCode.Redirect, borrado.StatusCode);
            Assert.True((await ClienteApi.ObtenerIngresoAsync(consecutivo, default)).NoEncontrado);
        }
        finally
        {
            await ClienteApi.BorrarIngresoAsync(consecutivo, default);
        }
    }

    [SkippableFact]
    public async Task Editar_SalidaAnteriorALaEntrada_MuestraElErrorDelApiYConservaLoEscrito()
    {
        var respuesta = await EnviarFormularioAsync("/Ingresos/Editar/17", "/Ingresos/Editar/17", new Dictionary<string, string>
        {
            ["IdAutomovil"] = "3",
            ["IdParqueo"] = "2",
            ["FechaHoraEntrada"] = "2026-10-17T09:20",
            ["FechaHoraSalida"] = "2026-10-17T08:00",
        });
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("El API rechazó la operación", html);
        Assert.Contains("posterior", html);
        Assert.Contains("value=\"2026-10-17T08:00\"", html);
        Assert.Null((await ClienteApi.ObtenerIngresoAsync(17, default)).Datos!.FechaHoraSalida);
    }

    [SkippableFact]
    public async Task Crear_SinCamposObligatorios_NoLlamaAlApi()
    {
        var respuesta = await EnviarFormularioAsync("/Ingresos/Crear", "/Ingresos/Crear", new Dictionary<string, string>
        {
            ["IdAutomovil"] = "",
            ["IdParqueo"] = "",
            ["FechaHoraEntrada"] = "",
        });
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("Seleccione el automóvil.", html);
        Assert.Contains("La fecha y hora de entrada es obligatoria.", html);
        Assert.Equal(17, (await ClienteApi.ListarIngresosAsync(1, 1, default)).Datos!.Total);
    }

    [SkippableFact]
    public async Task Editar_SinCambios_ConservaLosSegundosYElMontoDelApi()
    {
        // Un registro creado por el API o por Postman puede tener segundos (columna timestamp).
        var entrada = new DateTime(2026, 10, 20, 10, 0, 45);
        var salida = new DateTime(2026, 10, 20, 11, 20, 15); // 79,5 minutos
        int consecutivo = (await ClienteApi.CrearIngresoAsync(new IngresoSolicitudApi(1, 1, entrada, salida), default)).Datos!.Consecutivo;
        try
        {
            string formulario = await HtmlAsync($"/Ingresos/Editar/{consecutivo}");
            Assert.Contains("value=\"2026-10-20T10:00:45\"", formulario);
            Assert.Contains("value=\"2026-10-20T11:20:15\"", formulario);

            // Guardar sin cambios, como lo haría el navegador con los valores del formulario.
            var respuesta = await EnviarFormularioAsync($"/Ingresos/Editar/{consecutivo}", $"/Ingresos/Editar/{consecutivo}", new Dictionary<string, string>
            {
                ["IdAutomovil"] = "1",
                ["IdParqueo"] = "1",
                ["FechaHoraEntrada"] = "2026-10-20T10:00:45",
                ["FechaHoraSalida"] = "2026-10-20T11:20:15",
            });
            Assert.Equal(consecutivo, IdDeRedireccion(respuesta));

            var guardado = (await ClienteApi.ObtenerIngresoAsync(consecutivo, default)).Datos!;
            Assert.Equal(entrada, guardado.FechaHoraEntrada);
            Assert.Equal(salida, guardado.FechaHoraSalida);
            Assert.Equal(79.5m, guardado.DuracionEstadiaMinutos);
            Assert.Equal(1656.25m, guardado.MontoTotalAPagar);
            Assert.Contains("20/10/2026 10:00:45", await HtmlAsync($"/Ingresos/Detalles/{consecutivo}"));
        }
        finally
        {
            await ClienteApi.BorrarIngresoAsync(consecutivo, default);
        }
    }

    [SkippableTheory]
    [InlineData("2026-10-17T09:20+02:00")]
    [InlineData("2026-10-17T09:20Z")]
    public async Task Editar_FechaConZonaHoraria_SeRechazaSinConvertir(string entradaConZona)
    {
        var antes = (await ClienteApi.ObtenerIngresoAsync(17, default)).Datos!;

        var respuesta = await EnviarFormularioAsync("/Ingresos/Editar/17", "/Ingresos/Editar/17", new Dictionary<string, string>
        {
            ["IdAutomovil"] = antes.IdAutomovil.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["IdParqueo"] = antes.IdParqueo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["FechaHoraEntrada"] = entradaConZona,
        });
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("debe escribirse sin zona horaria", html);
        Assert.Contains($"value=\"{entradaConZona.Replace("+", "&#x2B;")}\"", html); // se conserva lo escrito (Razor codifica el "+")
        Assert.Equal(antes.FechaHoraEntrada, (await ClienteApi.ObtenerIngresoAsync(17, default)).Datos!.FechaHoraEntrada);
    }

    [SkippableTheory]
    [InlineData("999", "1")]   // parqueo inexistente
    [InlineData("1", "999")]   // automóvil inexistente
    public async Task Crear_LlaveForaneaInexistente_MuestraElRechazoDelApiYNoCrea(string idParqueo, string idAutomovil)
    {
        var respuesta = await EnviarFormularioAsync("/Ingresos/Crear", "/Ingresos/Crear", new Dictionary<string, string>
        {
            ["IdAutomovil"] = idAutomovil,
            ["IdParqueo"] = idParqueo,
            ["FechaHoraEntrada"] = "2026-10-20T08:00",
        });
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); // se vuelve a mostrar el formulario
        Assert.Contains("El API rechazó la operación", html);
        Assert.Contains("999", html);
        Assert.Contains("value=\"2026-10-20T08:00\"", html);
        Assert.Equal(17, (await ClienteApi.ListarIngresosAsync(1, 1, default)).Datos!.Total);
    }

    [SkippableFact]
    public async Task Crear_FechaMalEscrita_NoLlamaAlApi()
    {
        var respuesta = await EnviarFormularioAsync("/Ingresos/Crear", "/Ingresos/Crear", new Dictionary<string, string>
        {
            ["IdAutomovil"] = "1",
            ["IdParqueo"] = "1",
            ["FechaHoraEntrada"] = "2026-13-45T25:00",
        });
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("2026-13-45T25:00", html);
        Assert.Contains("no es válido", html);
        Assert.Equal(17, (await ClienteApi.ListarIngresosAsync(1, 1, default)).Datos!.Total);
    }

    [SkippableTheory]
    [InlineData("/Ingresos/Detalles/999")]
    [InlineData("/Ingresos/Editar/999")]
    [InlineData("/Ingresos/Borrar/999")]
    public async Task ConsecutivoInexistente_Devuelve404(string url)
    {
        var respuesta = await Navegador.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("consecutivo 999", await respuesta.Content.ReadAsStringAsync());
    }
}

public sealed class IngresosMvcJsonPruebas : IngresosMvcPruebasBase
{
    public IngresosMvcJsonPruebas()
        : base(new OrigenJson())
    {
    }
}

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class IngresosMvcBaseDatosPruebas : IngresosMvcPruebasBase
{
    public IngresosMvcBaseDatosPruebas()
        : base(new OrigenBaseDatos())
    {
    }
}
