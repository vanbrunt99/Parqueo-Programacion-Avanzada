using Microsoft.AspNetCore.Mvc;
using Parqueo.Mvc.Infraestructura;
using Parqueo.Mvc.Modelos;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Controladores;

/// <summary>
/// MVC completo de automóviles (primera etapa del Trabajo 04): listado filtrado por intervalo
/// de años con paginación, detalles, crear, editar y borrar. Cada acción llama al método
/// correspondiente del API: GET /api/automoviles, GET /api/automoviles/{id},
/// POST /api/automoviles, PUT /api/automoviles/{id} y DELETE /api/automoviles/{id}.
/// </summary>
public sealed class AutomovilesController : ControladorBase
{
    public const int TamanoPaginaPredeterminado = 10;

    private static readonly string[] Campos = ["Color", "Anio", "Fabricante", "Tipo"];

    public AutomovilesController(ClienteApiParqueo api)
        : base(api)
    {
    }

    /// <summary>Listado por intervalo de años (año inferior y superior, ambos incluidos).</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int? anioDesde, int? anioHasta, int pagina = 1, int tamanoPagina = TamanoPaginaPredeterminado, CancellationToken cancelacion = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, ClienteApiParqueo.TamanoMaximoPagina);
        ValidarFiltro(anioDesde, anioHasta);

        var modelo = new AutomovilesIndexViewModel { AnioDesde = anioDesde, AnioHasta = anioHasta, TamanoPagina = tamanoPagina };
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var respuesta = await Api.ListarAutomovilesAsync(anioDesde, anioHasta, null, pagina, tamanoPagina, cancelacion);
        RegistrarOrigen(respuesta);
        if (!respuesta.Exito)
        {
            ModelState.AddModelError(string.Empty, $"El API rechazó la consulta: {respuesta.MensajeError}");
            return View(modelo);
        }

        // Una página posterior a la última (por ejemplo, después de borrar) lleva a la última.
        var resultado = respuesta.Datos!;
        if (resultado.Registros.Count == 0 && resultado.TotalPaginas > 0 && pagina > resultado.TotalPaginas)
        {
            return RedirectToAction(nameof(Index), new { anioDesde, anioHasta, pagina = resultado.TotalPaginas, tamanoPagina });
        }

        return View(new AutomovilesIndexViewModel
        {
            AnioDesde = anioDesde,
            AnioHasta = anioHasta,
            TamanoPagina = tamanoPagina,
            Resultado = resultado,
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int id, string? volver, CancellationToken cancelacion)
    {
        var respuesta = await Api.ObtenerAutomovilAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Automóvil no encontrado", $"El API no tiene un automóvil con id {id}.");
        }

        if (!respuesta.Exito)
        {
            return ErrorApi(respuesta);
        }

        ViewData["Volver"] = UrlVolver(volver);
        return View(respuesta.Datos);
    }

    [HttpGet]
    public IActionResult Crear(string? volver)
    {
        ViewData["Volver"] = UrlVolver(volver);
        return View(new AutomovilFormularioViewModel { Tipos = TiposAutomovil.Opciones() });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(AutomovilFormularioViewModel formulario, string? volver, CancellationToken cancelacion)
    {
        ViewData["Volver"] = UrlVolver(volver);
        ValidarTipo(formulario);
        if (ModelState.IsValid)
        {
            var respuesta = await Api.CrearAutomovilAsync(formulario.ASolicitud(), cancelacion);
            RegistrarOrigen(respuesta);
            if (respuesta.Exito)
            {
                Aviso($"Se creó el automóvil #{respuesta.Datos!.Id} (API respondió {(int)respuesta.Estado}, origen {respuesta.OrigenDatos}).");
                return RedirectToAction(nameof(Detalles), new { id = respuesta.Datos.Id, volver });
            }

            AgregarErroresApi(respuesta, Campos);
        }

        formulario.Tipos = TiposAutomovil.Opciones(formulario.Tipo);
        return View(formulario);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, string? volver, CancellationToken cancelacion)
    {
        var respuesta = await Api.ObtenerAutomovilAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Automóvil no encontrado", $"El API no tiene un automóvil con id {id}.");
        }

        if (!respuesta.Exito)
        {
            return ErrorApi(respuesta);
        }

        var formulario = AutomovilFormularioViewModel.Desde(respuesta.Datos!);
        formulario.Tipos = TiposAutomovil.Opciones(formulario.Tipo);
        ViewData["Volver"] = UrlVolver(volver);
        return View(formulario);
    }

    /// <summary>Guarda la edición con PUT sobre el id de la URL (no crea un registro nuevo).</summary>
    [HttpPost]
    public async Task<IActionResult> Editar(int id, AutomovilFormularioViewModel formulario, string? volver, CancellationToken cancelacion)
    {
        formulario.Id = id;
        ViewData["Volver"] = UrlVolver(volver);
        ValidarTipo(formulario);
        if (ModelState.IsValid)
        {
            var respuesta = await Api.ActualizarAutomovilAsync(id, formulario.ASolicitud(), cancelacion);
            RegistrarOrigen(respuesta);
            if (respuesta.NoEncontrado)
            {
                return NoEncontrado("Automóvil no encontrado", $"El automóvil {id} ya no existe en el API.");
            }

            if (respuesta.Exito)
            {
                Aviso($"Se actualizaron los datos del automóvil #{id} (API respondió {(int)respuesta.Estado}, origen {respuesta.OrigenDatos}).");
                return RedirectToAction(nameof(Detalles), new { id, volver });
            }

            AgregarErroresApi(respuesta, Campos);
        }

        formulario.Tipos = TiposAutomovil.Opciones(formulario.Tipo);
        return View(formulario);
    }

    /// <summary>Confirmación de borrado (GET no borra nada).</summary>
    [HttpGet]
    public async Task<IActionResult> Borrar(int id, string? volver, CancellationToken cancelacion)
    {
        var respuesta = await Api.ObtenerAutomovilAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Automóvil no encontrado", $"El API no tiene un automóvil con id {id}.");
        }

        if (!respuesta.Exito)
        {
            return ErrorApi(respuesta);
        }

        ViewData["Volver"] = UrlVolver(volver);
        return View(new BorrarAutomovilViewModel(respuesta.Datos!));
    }

    /// <summary>
    /// Borra con DELETE. Si el automóvil tiene ingresos, el API responde 409 y se muestra el
    /// conflicto: no se borran ingresos a escondidas.
    /// </summary>
    [HttpPost]
    [ActionName("Borrar")]
    public async Task<IActionResult> BorrarConfirmado(int id, string? volver, CancellationToken cancelacion)
    {
        ViewData["Volver"] = UrlVolver(volver);
        var respuesta = await Api.BorrarAutomovilAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.Exito)
        {
            Aviso($"Se borró el automóvil #{id} (API respondió {(int)respuesta.Estado}, origen {respuesta.OrigenDatos}).");
            return Redirect(UrlVolver(volver));
        }

        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Automóvil no encontrado", $"El automóvil {id} ya no existe en el API.");
        }

        var automovil = await Api.ObtenerAutomovilAsync(id, cancelacion);
        if (!automovil.Exito)
        {
            return ErrorApi(respuesta);
        }

        string motivo = respuesta.Conflicto
            ? $"No se puede borrar: {respuesta.MensajeError} Primero deben borrarse sus ingresos desde la pantalla de ingresos."
            : $"El API no pudo borrar el automóvil: {respuesta.MensajeError}";
        return View(new BorrarAutomovilViewModel(automovil.Datos!, motivo));
    }

    private void ValidarFiltro(int? anioDesde, int? anioHasta)
    {
        foreach (var (anio, campo, etiqueta) in new[] { (anioDesde, "anioDesde", "año inferior"), (anioHasta, "anioHasta", "año superior") })
        {
            // Un texto que no es número (por ejemplo "abc") llega como error del enlace de datos.
            if (ModelState.TryGetValue(campo, out var estado) && estado.Errors.Count > 0)
            {
                string? recibido = estado.AttemptedValue;
                estado.Errors.Clear();
                ModelState.AddModelError(campo, $"El {etiqueta} debe ser un número entero; se recibió '{recibido}'.");
                continue;
            }

            if (anio is int valor && (valor < AutomovilesIndexViewModel.AnioMinimo || valor > AutomovilesIndexViewModel.AnioMaximo))
            {
                ModelState.AddModelError(campo, $"El {etiqueta} debe estar entre {AutomovilesIndexViewModel.AnioMinimo} y {AutomovilesIndexViewModel.AnioMaximo}.");
            }
        }

        if (anioDesde is int desde && anioHasta is int hasta && desde > hasta)
        {
            ModelState.AddModelError(string.Empty, $"El intervalo es inválido: el año inferior ({desde}) es mayor que el año superior ({hasta}).");
        }
    }

    private void ValidarTipo(AutomovilFormularioViewModel formulario)
    {
        if (formulario.Tipo is not null && !TiposAutomovil.EsValido(formulario.Tipo))
        {
            ModelState.AddModelError(nameof(formulario.Tipo), "Seleccione un tipo válido: sedán, 4x4 o moto.");
        }
    }
}
