using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Parqueo.Mvc.Servicios;

/// <summary>
/// Cliente HTTP tipado del API del Trabajo 03 (registrado con IHttpClientFactory en Program.cs;
/// la URL base sale de Api:UrlBase). Es el único lugar del MVC que conoce las rutas del API:
/// los controladores y las vistas solo llaman a estos métodos.
/// </summary>
/// <remarks>
/// El MVC no decide si los datos vienen de JSON o de PostgreSQL: eso lo configura el API con
/// OrigenDatos. Cada respuesta trae el encabezado X-Origen-Datos y este cliente lo conserva en
/// <see cref="RespuestaApi{T}.OrigenDatos"/> para mostrarlo como evidencia.
/// </remarks>
public sealed class ClienteApiParqueo
{
    /// <summary>Tamaño máximo de página que acepta el API (ParametrosPaginacion).</summary>
    public const int TamanoMaximoPagina = 100;

    public const string EncabezadoOrigen = "X-Origen-Datos";

    public const string EncabezadoTotal = "X-Total-Registros";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ClienteApiParqueo(HttpClient http)
    {
        _http = http;
    }

    /// <summary>URL base del API configurada (se muestra en las vistas).</summary>
    public Uri? UrlBase => _http.BaseAddress;

    // ------------------------------------------------------------------ Automóviles

    /// <summary>GET /api/automoviles con filtro de años y tipo, una página.</summary>
    public Task<RespuestaApi<PaginaApi<AutomovilApi>>> ListarAutomovilesAsync(
        int? anioDesde, int? anioHasta, string? tipo, int pagina, int tamanoPagina, CancellationToken cancelacion)
    {
        string ruta = Ruta("api/automoviles",
            ("anioDesde", anioDesde?.ToString(CultureInfo.InvariantCulture)),
            ("anioHasta", anioHasta?.ToString(CultureInfo.InvariantCulture)),
            ("tipo", tipo),
            ("pagina", pagina.ToString(CultureInfo.InvariantCulture)),
            ("tamanoPagina", tamanoPagina.ToString(CultureInfo.InvariantCulture)));
        return ObtenerPaginaAsync<AutomovilApi>(ruta, pagina, tamanoPagina, cancelacion);
    }

    /// <summary>
    /// Todos los automóviles que cumplen el filtro: recorre las páginas del API (100 por página)
    /// hasta completar el total que informa X-Total-Registros.
    /// </summary>
    public Task<RespuestaApi<IReadOnlyList<AutomovilApi>>> ListarTodosLosAutomovilesAsync(string? tipo, CancellationToken cancelacion) =>
        ListarTodoAsync(
            (pagina, c) => ListarAutomovilesAsync(null, null, tipo, pagina, TamanoMaximoPagina, c),
            cancelacion);

    /// <summary>GET /api/automoviles/{id}: el automóvil con sus ingresos y el parqueo de cada uno.</summary>
    public Task<RespuestaApi<AutomovilApi>> ObtenerAutomovilAsync(int id, CancellationToken cancelacion) =>
        EnviarAsync<AutomovilApi>(new HttpRequestMessage(HttpMethod.Get, $"api/automoviles/{id}"), cancelacion);

    /// <summary>POST /api/automoviles (201 con el registro creado).</summary>
    public Task<RespuestaApi<AutomovilApi>> CrearAutomovilAsync(AutomovilSolicitudApi solicitud, CancellationToken cancelacion) =>
        EnviarAsync<AutomovilApi>(ConCuerpo(HttpMethod.Post, "api/automoviles", solicitud), cancelacion);

    /// <summary>PUT /api/automoviles/{id} (200 con el registro actualizado).</summary>
    public Task<RespuestaApi<AutomovilApi>> ActualizarAutomovilAsync(int id, AutomovilSolicitudApi solicitud, CancellationToken cancelacion) =>
        EnviarAsync<AutomovilApi>(ConCuerpo(HttpMethod.Put, $"api/automoviles/{id}", solicitud), cancelacion);

    /// <summary>DELETE /api/automoviles/{id} (204; 409 si tiene ingresos).</summary>
    public Task<RespuestaApi<bool>> BorrarAutomovilAsync(int id, CancellationToken cancelacion) =>
        BorrarAsync($"api/automoviles/{id}", cancelacion);

    // ------------------------------------------------------------------ Parqueos

    /// <summary>Todos los parqueos (para el combo del formulario de ingresos), página por página.</summary>
    public Task<RespuestaApi<IReadOnlyList<ParqueoApi>>> ListarTodosLosParqueosAsync(CancellationToken cancelacion) =>
        ListarTodoAsync(
            (pagina, c) => ObtenerPaginaAsync<ParqueoApi>(
                Ruta("api/parqueos", ("pagina", pagina.ToString(CultureInfo.InvariantCulture)), ("tamanoPagina", TamanoMaximoPagina.ToString(CultureInfo.InvariantCulture))),
                pagina, TamanoMaximoPagina, c),
            cancelacion);

    // ------------------------------------------------------------------ Ingresos

    /// <summary>GET /api/ingresos, una página, ordenada por consecutivo.</summary>
    public Task<RespuestaApi<PaginaApi<IngresoApi>>> ListarIngresosAsync(int pagina, int tamanoPagina, CancellationToken cancelacion) =>
        ObtenerPaginaAsync<IngresoApi>(
            Ruta("api/ingresos", ("pagina", pagina.ToString(CultureInfo.InvariantCulture)), ("tamanoPagina", tamanoPagina.ToString(CultureInfo.InvariantCulture))),
            pagina, tamanoPagina, cancelacion);

    public Task<RespuestaApi<IngresoApi>> ObtenerIngresoAsync(int consecutivo, CancellationToken cancelacion) =>
        EnviarAsync<IngresoApi>(new HttpRequestMessage(HttpMethod.Get, $"api/ingresos/{consecutivo}"), cancelacion);

    public Task<RespuestaApi<IngresoApi>> CrearIngresoAsync(IngresoSolicitudApi solicitud, CancellationToken cancelacion) =>
        EnviarAsync<IngresoApi>(ConCuerpo(HttpMethod.Post, "api/ingresos", solicitud), cancelacion);

    public Task<RespuestaApi<IngresoApi>> ActualizarIngresoAsync(int consecutivo, IngresoSolicitudApi solicitud, CancellationToken cancelacion) =>
        EnviarAsync<IngresoApi>(ConCuerpo(HttpMethod.Put, $"api/ingresos/{consecutivo}", solicitud), cancelacion);

    public Task<RespuestaApi<bool>> BorrarIngresoAsync(int consecutivo, CancellationToken cancelacion) =>
        BorrarAsync($"api/ingresos/{consecutivo}", cancelacion);

    // ------------------------------------------------------------------ Auxiliares

    private async Task<RespuestaApi<PaginaApi<T>>> ObtenerPaginaAsync<T>(string ruta, int pagina, int tamanoPagina, CancellationToken cancelacion)
    {
        using var respuesta = await _http.GetAsync(ruta, cancelacion);
        string? origen = Origen(respuesta);
        if (!respuesta.IsSuccessStatusCode)
        {
            return RespuestaApi<PaginaApi<T>>.Fallida(respuesta.StatusCode, await LeerProblemaAsync(respuesta, cancelacion), origen);
        }

        var registros = await respuesta.Content.ReadFromJsonAsync<List<T>>(Json, cancelacion) ?? [];
        // Si el API no informara el total (versión anterior), se toma lo recibido como mínimo.
        int total = respuesta.Headers.TryGetValues(EncabezadoTotal, out var valores)
                    && int.TryParse(valores.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out int valor)
            ? valor
            : ((pagina - 1) * tamanoPagina) + registros.Count;
        return RespuestaApi<PaginaApi<T>>.Correcta(respuesta.StatusCode, new PaginaApi<T>(registros, pagina, tamanoPagina, total), origen);
    }

    private static async Task<RespuestaApi<IReadOnlyList<T>>> ListarTodoAsync<T>(
        Func<int, CancellationToken, Task<RespuestaApi<PaginaApi<T>>>> obtenerPagina, CancellationToken cancelacion)
    {
        var todos = new List<T>();
        string? origen = null;
        for (int pagina = 1; ; pagina++)
        {
            var respuesta = await obtenerPagina(pagina, cancelacion);
            if (!respuesta.Exito)
            {
                return respuesta.ComoFallida<IReadOnlyList<T>>();
            }

            origen = respuesta.OrigenDatos;
            var datos = respuesta.Datos!;
            todos.AddRange(datos.Registros);
            if (datos.Registros.Count == 0 || todos.Count >= datos.Total)
            {
                return RespuestaApi<IReadOnlyList<T>>.Correcta(respuesta.Estado, todos, origen);
            }
        }
    }

    private async Task<RespuestaApi<T>> EnviarAsync<T>(HttpRequestMessage solicitud, CancellationToken cancelacion)
    {
        using (solicitud)
        using (var respuesta = await _http.SendAsync(solicitud, cancelacion))
        {
            string? origen = Origen(respuesta);
            if (!respuesta.IsSuccessStatusCode)
            {
                return RespuestaApi<T>.Fallida(respuesta.StatusCode, await LeerProblemaAsync(respuesta, cancelacion), origen);
            }

            var datos = await respuesta.Content.ReadFromJsonAsync<T>(Json, cancelacion)
                ?? throw new InvalidOperationException($"El API devolvió una respuesta vacía para {solicitud.RequestUri}.");
            return RespuestaApi<T>.Correcta(respuesta.StatusCode, datos, origen);
        }
    }

    private async Task<RespuestaApi<bool>> BorrarAsync(string ruta, CancellationToken cancelacion)
    {
        using var respuesta = await _http.DeleteAsync(ruta, cancelacion);
        string? origen = Origen(respuesta);
        return respuesta.IsSuccessStatusCode
            ? RespuestaApi<bool>.Correcta(respuesta.StatusCode, true, origen)
            : RespuestaApi<bool>.Fallida(respuesta.StatusCode, await LeerProblemaAsync(respuesta, cancelacion), origen);
    }

    private static HttpRequestMessage ConCuerpo<T>(HttpMethod metodo, string ruta, T cuerpo) =>
        new(metodo, ruta) { Content = JsonContent.Create(cuerpo, options: Json) };

    private static async Task<ProblemaApi> LeerProblemaAsync(HttpResponseMessage respuesta, CancellationToken cancelacion)
    {
        try
        {
            var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaApi>(Json, cancelacion);
            if (problema is not null)
            {
                return problema;
            }
        }
        catch (JsonException)
        {
            // El cuerpo no era ProblemDetails: se informa solo el código.
        }
        catch (NotSupportedException)
        {
            // Tipo de contenido no JSON.
        }

        return new ProblemaApi($"El API respondió {(int)respuesta.StatusCode} {respuesta.ReasonPhrase}.", null, (int)respuesta.StatusCode);
    }

    private static string? Origen(HttpResponseMessage respuesta) =>
        respuesta.Headers.TryGetValues(EncabezadoOrigen, out var valores) ? valores.FirstOrDefault() : null;

    private static string Ruta(string ruta, params (string Nombre, string? Valor)[] parametros)
    {
        var partes = parametros
            .Where(p => !string.IsNullOrWhiteSpace(p.Valor))
            .Select(p => $"{p.Nombre}={Uri.EscapeDataString(p.Valor!)}")
            .ToList();
        return partes.Count == 0 ? ruta : $"{ruta}?{string.Join('&', partes)}";
    }
}
