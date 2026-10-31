using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Modelos;

/// <summary>Pantalla de automóviles: filtro por intervalo de años, página actual y resultados.</summary>
public sealed class AutomovilesIndexViewModel
{
    public const int AnioMinimo = 1886;
    public const int AnioMaximo = 2100;

    [Display(Name = "Año inferior")]
    public int? AnioDesde { get; init; }

    [Display(Name = "Año superior")]
    public int? AnioHasta { get; init; }

    public int TamanoPagina { get; init; }

    /// <summary>Página que devolvió el API; null si el filtro no era válido y no se consultó.</summary>
    public PaginaApi<AutomovilApi>? Resultado { get; init; }

    public bool HayFiltro => AnioDesde is not null || AnioHasta is not null;
}

/// <summary>
/// Formulario de crear y editar automóvil. No es la entidad del modelo ni el DTO del API:
/// solo acepta los cuatro campos editables (el id viene de la URL).
/// </summary>
public sealed class AutomovilFormularioViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El color es obligatorio.")]
    [StringLength(30, ErrorMessage = "El color admite como máximo 30 caracteres.")]
    [Display(Name = "Color")]
    public string? Color { get; set; }

    [Required(ErrorMessage = "El año es obligatorio.")]
    [Range(AutomovilesIndexViewModel.AnioMinimo, AutomovilesIndexViewModel.AnioMaximo, ErrorMessage = "El año debe estar entre {1} y {2}.")]
    [Display(Name = "Año")]
    public int? Anio { get; set; }

    [Required(ErrorMessage = "El fabricante es obligatorio.")]
    [StringLength(50, ErrorMessage = "El fabricante admite como máximo 50 caracteres.")]
    [Display(Name = "Fabricante")]
    public string? Fabricante { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de automóvil.")]
    [Display(Name = "Tipo")]
    public string? Tipo { get; set; }

    public IReadOnlyList<SelectListItem> Tipos { get; set; } = [];

    public AutomovilSolicitudApi ASolicitud() => new(Color!.Trim(), Anio!.Value, Fabricante!.Trim(), Tipo!);

    public static AutomovilFormularioViewModel Desde(AutomovilApi automovil) => new()
    {
        Id = automovil.Id,
        Color = automovil.Color,
        Anio = automovil.Anio,
        Fabricante = automovil.Fabricante,
        Tipo = automovil.Tipo,
    };
}

/// <summary>Confirmación de borrado; <see cref="Error"/> trae el conflicto del API si lo hubo.</summary>
public sealed record BorrarAutomovilViewModel(AutomovilApi Automovil, string? Error = null);

/// <summary>Mensaje simple (registro no encontrado, API no disponible).</summary>
public sealed record MensajeViewModel(string Titulo, string Detalle);
