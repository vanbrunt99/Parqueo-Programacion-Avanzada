using System.Net;
using Parqueo.Mvc.Servicios;
using Parqueo.Mvc.Tests.Infraestructura;

namespace Parqueo.Mvc.Tests;

/// <summary>
/// Pantallas MVC de automóviles de punta a punta: navegador simulado → controlador MVC →
/// cliente HTTP → API → repositorio (JSON o PostgreSQL).
/// </summary>
public abstract class AutomovilesMvcPruebasBase : MvcPruebasBase
{
    protected AutomovilesMvcPruebasBase(OrigenPruebas origen)
        : base(origen)
    {
    }

    [SkippableTheory]
    [InlineData("", new[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData("?anioDesde=2015&anioHasta=2019", new[] { 1, 3, 5 })]       // extremos incluidos (2015 y 2019)
    [InlineData("?anioDesde=2019&anioHasta=2019", new[] { 1 })]
    [InlineData("?anioDesde=2021", new[] { 2, 4 })]                          // solo año inferior
    [InlineData("?anioHasta=2015", new[] { 3, 6 })]                          // solo año superior
    public async Task Index_FiltraPorIntervaloDeAnios(string consulta, int[] esperados)
    {
        string html = await HtmlAsync($"/Automoviles{consulta}");

        Assert.Equal(esperados, IdsDeFilas(html));
        Assert.Contains($"data-origen=\"{Origen.Nombre}\"", html); // el origen viene del encabezado X-Origen-Datos
    }

    [SkippableFact]
    public async Task Index_SinCoincidencias_MuestraMensaje()
    {
        string html = await HtmlAsync("/Automoviles?anioDesde=2030&anioHasta=2040");

        Assert.Empty(IdsDeFilas(html));
        Assert.Contains("No hay automóviles en el intervalo indicado", html);
    }

    [SkippableTheory]
    [InlineData("?anioDesde=2020&anioHasta=2010", "el año inferior (2020) es mayor que el año superior (2010)")]
    [InlineData("?anioDesde=abc", "El año inferior debe ser un número entero; se recibió &#x27;abc&#x27;.")]
    [InlineData("?anioDesde=1500", "El año inferior debe estar entre 1886 y 2100")]
    public async Task Index_FiltroInvalido_MuestraElErrorSinListar(string consulta, string mensaje)
    {
        string html = await HtmlAsync($"/Automoviles{consulta}");

        Assert.Contains(mensaje, html);
        Assert.Empty(IdsDeFilas(html));
    }

    [SkippableFact]
    public async Task Index_PaginaEnorme_LlevaALaUltimaPaginaSinErrorDelApi()
    {
        var respuesta = await Navegador.GetAsync("/Automoviles?anioDesde=2012&pagina=2147483647");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("pagina=1", respuesta.Headers.Location!.ToString());
        Assert.Contains("anioDesde=2012", respuesta.Headers.Location!.ToString());
    }

    [SkippableFact]
    public async Task Index_PrimeraPagina_AnteriorNoEsEnlace()
    {
        string html = await HtmlAsync("/Automoviles?tamanoPagina=4");

        Assert.Contains("<span class=\"page-link\" aria-disabled=\"true\">Anterior</span>", html);
        Assert.DoesNotContain("pagina=0", html);
        Assert.Contains("aria-label=\"Página siguiente\"", html);
    }

    [SkippableFact]
    public async Task Index_Paginado_ConservaElFiltroYMuestraElTotal()
    {
        string pagina1 = await HtmlAsync("/Automoviles?anioDesde=2012&tamanoPagina=4");
        string pagina2 = await HtmlAsync("/Automoviles?anioDesde=2012&pagina=2&tamanoPagina=4");

        Assert.Equal([1, 2, 3, 4], IdsDeFilas(pagina1));
        Assert.Contains("Mostrando 1–4 de 6 automóviles · página 1 de 2", pagina1);
        Assert.Contains("href=\"/Automoviles?anioDesde=2012&amp;pagina=2&amp;tamanoPagina=4\"", pagina1);
        Assert.Equal([5, 6], IdsDeFilas(pagina2));
        Assert.Contains("Mostrando 5–6 de 6 automóviles · página 2 de 2", pagina2);
    }

    [SkippableFact]
    public async Task Detalles_MuestraLosIngresosConCamposCalculadosDelApi()
    {
        string html = await HtmlAsync("/Automoviles/Detalles/2");

        Assert.Equal([2, 6, 16], IdsDeFilas(html, "data-consecutivo"));
        Assert.Contains("₡1&#xA0;666,67", html); // 80 min a ₡1 250/h, calculado por el API (separador de miles: espacio duro)
        Assert.Contains("Sin salida (en curso)", html);   // ingreso 16: salida null
    }

    [SkippableFact]
    public async Task Detalles_IdInexistente_Devuelve404ConMensaje()
    {
        var respuesta = await Navegador.GetAsync("/Automoviles/Detalles/999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("no tiene un automóvil con id 999", await respuesta.Content.ReadAsStringAsync());
    }

    [SkippableFact]
    public async Task CrearEditarYBorrar_RecorreElMantenimientoCompleto()
    {
        // Crear (POST /api/automoviles)
        var creado = await EnviarFormularioAsync("/Automoviles/Crear", "/Automoviles/Crear", new Dictionary<string, string>
        {
            ["Color"] = "Turquesa",
            ["Anio"] = "2022",
            ["Fabricante"] = "Kia",
            ["Tipo"] = "sedán",
        });
        int id = IdDeRedireccion(creado);
        try
        {
            string detalles = await HtmlAsync($"/Automoviles/Detalles/{id}");
            Assert.Contains("Turquesa", detalles);
            Assert.Contains("Este automóvil no tiene ingresos registrados.", detalles);

            // Editar (PUT /api/automoviles/{id}): no crea otro registro.
            var total = (await ClienteApi.ListarAutomovilesAsync(null, null, null, 1, 1, default)).Datos!.Total;
            var editado = await EnviarFormularioAsync($"/Automoviles/Editar/{id}", $"/Automoviles/Editar/{id}", new Dictionary<string, string>
            {
                ["Color"] = "Turquesa oscuro",
                ["Anio"] = "2023",
                ["Fabricante"] = "Kia",
                ["Tipo"] = "4x4",
            });
            Assert.Equal(id, IdDeRedireccion(editado));
            var api = (await ClienteApi.ObtenerAutomovilAsync(id, default)).Datos!;
            Assert.Equal(("Turquesa oscuro", 2023, "4x4"), (api.Color, api.Anio, api.Tipo));
            Assert.Equal(total, (await ClienteApi.ListarAutomovilesAsync(null, null, null, 1, 1, default)).Datos!.Total);

            // Borrar: GET solo confirma; el POST borra (DELETE /api/automoviles/{id}).
            string confirmacion = await HtmlAsync($"/Automoviles/Borrar/{id}");
            Assert.Contains("¿Desea borrar este automóvil?", confirmacion);
            Assert.True((await ClienteApi.ObtenerAutomovilAsync(id, default)).Exito);

            var borrado = await EnviarFormularioAsync($"/Automoviles/Borrar/{id}", $"/Automoviles/Borrar/{id}", new Dictionary<string, string>
            {
                ["volver"] = "/Automoviles?anioDesde=2020",
            });
            Assert.Equal(HttpStatusCode.Redirect, borrado.StatusCode);
            Assert.Equal("/Automoviles?anioDesde=2020", borrado.Headers.Location!.ToString());
            Assert.True((await ClienteApi.ObtenerAutomovilAsync(id, default)).NoEncontrado);
        }
        finally
        {
            await ClienteApi.BorrarAutomovilAsync(id, default);
        }
    }

    [SkippableFact]
    public async Task Crear_DatosInvalidos_ConservaLoEscritoYNoLlamaAlApi()
    {
        int antes = (await ClienteApi.ListarAutomovilesAsync(null, null, null, 1, 1, default)).Datos!.Total;

        var respuesta = await EnviarFormularioAsync("/Automoviles/Crear", "/Automoviles/Crear", new Dictionary<string, string>
        {
            ["Color"] = "",
            ["Anio"] = "1800",
            ["Fabricante"] = "Fabricante escrito",
            ["Tipo"] = "camión",
        });
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("El color es obligatorio.", html);
        Assert.Contains("El año debe estar entre 1886 y 2100.", html);
        Assert.Contains("Seleccione un tipo válido", html);
        Assert.Contains("value=\"Fabricante escrito\"", html);
        Assert.Equal(antes, (await ClienteApi.ListarAutomovilesAsync(null, null, null, 1, 1, default)).Datos!.Total);
    }

    [SkippableFact]
    public async Task Borrar_AutomovilConIngresos_MuestraElConflictoDelApiYNoBorra()
    {
        var respuesta = await EnviarFormularioAsync("/Automoviles/Borrar/1", "/Automoviles/Borrar/1", new Dictionary<string, string>());
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("No se puede borrar", html);
        Assert.True((await ClienteApi.ObtenerAutomovilAsync(1, default)).Exito);
        Assert.Equal(3, (await ClienteApi.ObtenerAutomovilAsync(1, default)).Datos!.Ingresos!.Count);
    }

    [SkippableFact]
    public async Task Post_SinTokenAntiforgery_SeRechaza()
    {
        var respuesta = await Navegador.PostAsync("/Automoviles/Borrar/6", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True((await ClienteApi.ObtenerAutomovilAsync(6, default)).Exito);
    }

    [SkippableFact]
    public async Task Editar_IdInexistente_Devuelve404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Navegador.GetAsync("/Automoviles/Editar/999")).StatusCode);
    }
}

public sealed class AutomovilesMvcJsonPruebas : AutomovilesMvcPruebasBase
{
    public AutomovilesMvcJsonPruebas()
        : base(new OrigenJson())
    {
    }

    [Fact]
    public async Task Crear_EscribeEnLaCopiaDeTrabajoNoEnLosJsonEntregados()
    {
        string entregado = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "DatosOriginales", "automoviles.json"));
        var creado = await EnviarFormularioAsync("/Automoviles/Crear", "/Automoviles/Crear", new Dictionary<string, string>
        {
            ["Color"] = "Lila",
            ["Anio"] = "2024",
            ["Fabricante"] = "BYD",
            ["Tipo"] = "sedán",
        });
        int id = IdDeRedireccion(creado);

        string copia = await File.ReadAllTextAsync(Path.Combine(((OrigenJson)Origen).Carpeta, "automoviles.json"));
        await ClienteApi.BorrarAutomovilAsync(id, default);
        Assert.Contains("\"color\": \"Lila\"", copia);
        Assert.DoesNotContain("Lila", entregado);
    }
}

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class AutomovilesMvcBaseDatosPruebas : AutomovilesMvcPruebasBase
{
    public AutomovilesMvcBaseDatosPruebas()
        : base(new OrigenBaseDatos())
    {
    }
}
