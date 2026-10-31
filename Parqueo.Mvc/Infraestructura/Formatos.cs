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

    public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public static string Salida(DateTime? fecha) => fecha is DateTime valor ? FechaHora(valor) : "Sin salida (en curso)";

    public static string Minutos(decimal? minutos) => minutos?.ToString("#,##0.##", Cultura) ?? "En curso";

    public static string Horas(decimal? horas) => horas?.ToString("#,##0.####", Cultura) ?? "En curso";

    public static string Colones(decimal? monto) => monto is decimal valor ? "₡" + valor.ToString("#,##0.00", Cultura) : "Pendiente (sin salida)";

    public static string Precio(decimal precio) => "₡" + precio.ToString("#,##0.00", Cultura);
}
