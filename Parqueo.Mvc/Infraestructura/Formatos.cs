using System.Globalization;

namespace Parqueo.Mvc.Infraestructura;

/// <summary>
/// Formato de presentación en español de Costa Rica. Solo da formato a los valores que
/// calcula el API: no recalcula duraciones ni montos. Un valor null (estadía sin salida) se
/// muestra como texto, nunca como 0.
/// </summary>
public static class Formatos
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    /// <summary>"05/10/2026 12:00"; los segundos se muestran solo si el registro los tiene.</summary>
    public static string FechaHora(DateTime fecha) =>
        fecha.ToString(TieneSegundos(fecha) ? "dd/MM/yyyy HH:mm:ss" : "dd/MM/yyyy HH:mm", Cultura);

    /// <summary>
    /// Formato del valor de un input datetime-local: con segundos solo si el registro los tiene,
    /// para que guardar un formulario sin cambios no recorte los segundos (ni altere el monto).
    /// </summary>
    public static string FormatoEntradaFecha(DateTime? fecha) =>
        fecha is DateTime valor && TieneSegundos(valor) ? "{0:yyyy-MM-ddTHH:mm:ss}" : "{0:yyyy-MM-ddTHH:mm}";

    private static bool TieneSegundos(DateTime fecha) => fecha.Second != 0 || fecha.Millisecond != 0;

    public static string Salida(DateTime? fecha) => fecha is DateTime valor ? FechaHora(valor) : "Sin salida (en curso)";

    public static string Minutos(decimal? minutos) => minutos?.ToString("#,##0.##", Cultura) ?? "En curso";

    public static string Horas(decimal? horas) => horas?.ToString("#,##0.####", Cultura) ?? "En curso";

    public static string Colones(decimal? monto) => monto is decimal valor ? "₡" + valor.ToString("#,##0.00", Cultura) : "Pendiente (sin salida)";

    public static string Precio(decimal precio) => "₡" + precio.ToString("#,##0.00", Cultura);
}
