using System;
using System.Collections.Generic;

namespace Parqueo.Model.Entidades;

/// <summary>
/// Parqueos (estacionamientos) disponibles.
/// </summary>
public partial class PrqParqueo
{
    /// <summary>
    /// Llave primaria del parqueo.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Provincia de Costa Rica donde se ubica el parqueo.
    /// </summary>
    public string NombreProvincia { get; set; } = null!;

    /// <summary>
    /// Nombre del parqueo.
    /// </summary>
    public string Nombre { get; set; } = null!;

    /// <summary>
    /// Tarifa por hora en colones.
    /// </summary>
    public decimal PrecioPorHora { get; set; }

    public virtual ICollection<PrqIngresoAutomovil> PrqIngresoAutomovils { get; set; } = new List<PrqIngresoAutomovil>();
}
