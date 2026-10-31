using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Modelos;

/// <summary>
/// Formulario de crear y editar un ingreso. Solo son editables el automóvil, el parqueo, la
/// entrada y la salida; el consecutivo es la llave (viene de la URL) y la duración y el monto
/// los calcula el API. Las fechas se manejan sin zona horaria (input datetime-local), igual que
/// el contrato del API.
/// </summary>
public sealed class IngresoFormularioViewModel
{
    public int Consecutivo { get; set; }

    [Required(ErrorMessage = "Seleccione el automóvil.")]
    [Display(Name = "Automóvil")]
    public int? IdAutomovil { get; set; }

    [Required(ErrorMessage = "Seleccione el parqueo.")]
    [Display(Name = "Parqueo")]
    public int? IdParqueo { get; set; }

    [Required(ErrorMessage = "La fecha y hora de entrada es obligatoria.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fecha y hora de entrada")]
    public DateTime? FechaHoraEntrada { get; set; }

    /// <summary>Vacía mientras el automóvil no haya salido: se envía como null.</summary>
    [DataType(DataType.DateTime)]
    [Display(Name = "Fecha y hora de salida (opcional)")]
    public DateTime? FechaHoraSalida { get; set; }

    public IReadOnlyList<SelectListItem> Automoviles { get; set; } = [];

    public IReadOnlyList<SelectListItem> Parqueos { get; set; } = [];

    /// <summary>
    /// Convierte al cuerpo del API. Las fechas conservan DateTimeKind.Unspecified (sin "Z" ni
    /// conversión a UTC), como exige el API.
    /// </summary>
    public IngresoSolicitudApi ASolicitud() => new(
        IdParqueo!.Value,
        IdAutomovil!.Value,
        DateTime.SpecifyKind(FechaHoraEntrada!.Value, DateTimeKind.Unspecified),
        FechaHoraSalida is DateTime salida ? DateTime.SpecifyKind(salida, DateTimeKind.Unspecified) : null);

    public static IngresoFormularioViewModel Desde(IngresoApi ingreso) => new()
    {
        Consecutivo = ingreso.Consecutivo,
        IdAutomovil = ingreso.IdAutomovil,
        IdParqueo = ingreso.IdParqueo,
        FechaHoraEntrada = ingreso.FechaHoraEntrada,
        FechaHoraSalida = ingreso.FechaHoraSalida,
    };
}

/// <summary>Confirmación de borrado de un ingreso.</summary>
public sealed record BorrarIngresoViewModel(IngresoApi Ingreso, string? Error = null);
