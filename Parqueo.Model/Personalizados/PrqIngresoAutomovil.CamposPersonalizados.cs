using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parqueo.Model.Entidades;

/// <summary>
/// Campos personalizados del ingreso de automóviles.
/// </summary>
/// <remarks>
/// Esta clase parcial vive fuera de la carpeta <c>Entidades</c> para que un nuevo
/// scaffold (<c>dotnet ef dbcontext scaffold ... --force</c>) no la sobrescriba.
/// Los tres campos dependen de que la fecha y hora de salida no sea nula; si el
/// automóvil aún no ha salido, el resultado es <c>null</c> (nunca cero).
/// Las propiedades no se mapean a columnas (<see cref="NotMappedAttribute"/>) ni se
/// escriben en los archivos JSON (<see cref="JsonIgnoreAttribute"/>).
/// </remarks>
public partial class PrqIngresoAutomovil
{
    /// <summary>
    /// Duración de la estadía en minutos (incluye fracciones si hay segundos);
    /// <c>null</c> si el automóvil no ha salido.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public decimal? DuracionEstadiaMinutos =>
        TicksDeEstadia() is long ticks ? (decimal)ticks / TimeSpan.TicksPerMinute : null;

    /// <summary>
    /// Duración de la estadía en horas, con fracción exacta (80 minutos = 1.333... horas);
    /// <c>null</c> si el automóvil no ha salido.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public decimal? DuracionEstadiaHoras =>
        TicksDeEstadia() is long ticks ? (decimal)ticks / TimeSpan.TicksPerHour : null;

    /// <summary>
    /// Monto total a pagar según el precio por hora del parqueo asociado
    /// (<see cref="IdParqueoNavigation"/>). Es <c>null</c> si el automóvil no ha salido.
    /// Requiere que la navegación del parqueo esté cargada: los repositorios la cargan
    /// en la consulta por id, en los ingresos de un automóvil o parqueo y en lo que
    /// devuelven al insertar. Sin esa navegación (por ejemplo, un objeto creado a mano)
    /// use <see cref="CalcularMontoTotal"/> con el precio.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public decimal? MontoTotalAPagar => CalcularMontoTotal(IdParqueoNavigation?.PrecioPorHora);

    /// <summary>
    /// Calcula el monto a pagar con el precio por hora indicado.
    /// </summary>
    /// <param name="precioPorHora">Tarifa por hora del parqueo.</param>
    /// <returns>
    /// El monto redondeado a 2 decimales, o <c>null</c> si no hay salida registrada
    /// o no se conoce el precio.
    /// </returns>
    /// <remarks>
    /// Política de cobro PROVISIONAL (el enunciado no la define): cobro proporcional
    /// al tiempo exacto de la estadía, monto = horas × precio por hora, redondeado a
    /// 2 decimales con <see cref="MidpointRounding.AwayFromZero"/> (igual que
    /// <c>round(numeric, 2)</c> de PostgreSQL). Si el profesor indica cobrar horas
    /// completas, basta con cambiar este método, por ejemplo usando
    /// <c>Math.Ceiling</c> sobre las horas.
    /// </remarks>
    public decimal? CalcularMontoTotal(decimal? precioPorHora)
    {
        if (TicksDeEstadia() is not long ticks || precioPorHora is not decimal precio)
        {
            return null;
        }

        // Se multiplica antes de dividir: (ticks × precio) es exacto y la división por
        // las ticks de una hora no arrastra el redondeo de una fracción periódica
        // (50 min a 675.03 = 562.525 exacto -> 562.53).
        return Math.Round(ticks * precio / TimeSpan.TicksPerHour, 2, MidpointRounding.AwayFromZero);
    }

    private long? TicksDeEstadia() =>
        FechaHoraSalida is DateTime salida ? (salida - FechaHoraEntrada).Ticks : null;
}
