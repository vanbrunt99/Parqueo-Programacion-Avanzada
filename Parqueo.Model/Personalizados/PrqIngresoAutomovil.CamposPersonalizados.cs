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
    /// (<see cref="IdParqueoNavigation"/>). Es <c>null</c> si el automóvil no ha salido
    /// o si la propiedad de navegación del parqueo no fue cargada.
    /// Los repositorios del proyecto siempre cargan esa navegación.
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
    /// <c>Math.Ceiling(horas)</c>.
    /// </remarks>
    public decimal? CalcularMontoTotal(decimal? precioPorHora)
    {
        if (DuracionEstadiaHoras is not decimal horas || precioPorHora is not decimal precio)
        {
            return null;
        }

        return Math.Round(horas * precio, 2, MidpointRounding.AwayFromZero);
    }

    private long? TicksDeEstadia() =>
        FechaHoraSalida is DateTime salida ? (salida - FechaHoraEntrada).Ticks : null;
}
