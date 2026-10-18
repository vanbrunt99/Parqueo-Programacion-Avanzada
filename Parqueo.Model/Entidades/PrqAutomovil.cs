using System;
using System.Collections.Generic;

namespace Parqueo.Model.Entidades;

/// <summary>
/// Automóviles que utilizan los parqueos.
/// </summary>
public partial class PrqAutomovil
{
    /// <summary>
    /// Llave primaria del automóvil.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Color del automóvil.
    /// </summary>
    public string Color { get; set; } = null!;

    /// <summary>
    /// Año (modelo) del automóvil.
    /// </summary>
    public int Anio { get; set; }

    /// <summary>
    /// Fabricante o marca del automóvil.
    /// </summary>
    public string Fabricante { get; set; } = null!;

    /// <summary>
    /// Tipo de automóvil: sedán, 4x4 o moto.
    /// </summary>
    public string Tipo { get; set; } = null!;

    public virtual ICollection<PrqIngresoAutomovil> PrqIngresoAutomovils { get; set; } = new List<PrqIngresoAutomovil>();
}
