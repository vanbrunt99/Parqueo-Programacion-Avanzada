using Microsoft.AspNetCore.Mvc.Rendering;

namespace Parqueo.Mvc.Infraestructura;

/// <summary>
/// Tipos de automóvil permitidos por la restricción CHECK de prq_automovil y por el
/// validador del modelo: "sedán", "4x4" y "moto". Los valores se envían al API tal cual;
/// solo la etiqueta visible cambia.
/// </summary>
public static class TiposAutomovil
{
    public static readonly IReadOnlyList<string> Valores = ["sedán", "4x4", "moto"];

    public static bool EsValido(string? tipo) => tipo is not null && Valores.Contains(tipo, StringComparer.Ordinal);

    public static string Etiqueta(string tipo) => tipo switch
    {
        "sedán" => "Sedán",
        "moto" => "Moto",
        _ => tipo,
    };

    public static IReadOnlyList<SelectListItem> Opciones(string? seleccionado = null) =>
        Valores.Select(v => new SelectListItem(Etiqueta(v), v, v == seleccionado)).ToList();
}
