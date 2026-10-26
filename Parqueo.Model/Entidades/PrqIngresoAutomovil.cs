using System;
using System.Collections.Generic;

namespace Parqueo.Model.Entidades;

/// <summary>
/// Registro de ingresos y salidas de automóviles en los parqueos.
/// </summary>
public partial class PrqIngresoAutomovil
{
    /// <summary>
    /// Llave primaria consecutiva del ingreso.
    /// </summary>
    public int Consecutivo { get; set; }

    /// <summary>
    /// Parqueo al que ingresó el automóvil.
    /// </summary>
    public int IdParqueo { get; set; }

    /// <summary>
    /// Automóvil que ingresó.
    /// </summary>
    public int IdAutomovil { get; set; }

    /// <summary>
    /// Fecha y hora de entrada.
    /// </summary>
    public DateTime FechaHoraEntrada { get; set; }

    /// <summary>
    /// Fecha y hora de salida; NULL si el automóvil no ha salido.
    /// </summary>
    public DateTime? FechaHoraSalida { get; set; }

    public virtual PrqAutomovil IdAutomovilNavigation { get; set; } = null!;

    public virtual PrqParqueo IdParqueoNavigation { get; set; } = null!;
}
