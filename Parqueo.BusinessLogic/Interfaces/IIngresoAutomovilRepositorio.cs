using Parqueo.BusinessLogic.Consultas;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Interfaces;

/// <summary>Repositorio de la tabla de ingresos de automóviles (prq_ingreso_automovil).</summary>
public interface IIngresoAutomovilRepositorio : IRepositorio<PrqIngresoAutomovil>
{
    /// <summary>
    /// Función solicitada en el enunciado: recibe un id de parqueo y devuelve el
    /// monto por hora que se paga en ese parqueo. Devuelve <c>null</c> si el
    /// parqueo no existe.
    /// </summary>
    /// <remarks>
    /// El nombre se conserva exactamente como lo pide el enunciado (sin el sufijo
    /// "Async" que llevan los demás métodos asincrónicos).
    /// </remarks>
    Task<decimal?> ObtenerPrecioPorHoraPorParqueo(int idParqueo, CancellationToken cancelacion = default);

    /// <summary>
    /// Todos los ingresos ordenados por consecutivo, cada uno con su automóvil y su
    /// parqueo (propiedades de navegación), de modo que los campos calculados quedan
    /// disponibles. Admite paginación.
    /// </summary>
    Task<IReadOnlyList<PrqIngresoAutomovil>> ListarAsync(
        Paginacion? paginacion = null,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Automóviles de un tipo (búsqueda aproximada) que ingresaron dentro del
    /// intervalo [desde, hasta] (ambos extremos incluidos, según la fecha y hora de
    /// entrada). Incluye entrada, salida y monto a cancelar, calculado con
    /// <see cref="ObtenerPrecioPorHoraPorParqueo"/>. <paramref name="hasta"/> es un
    /// instante exacto: para incluir un día completo use 23:59:59.
    /// </summary>
    Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorTipoAsync(
        string tipo,
        DateTime desde,
        DateTime hasta,
        Paginacion? paginacion = null,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Automóviles que ingresaron dentro del intervalo [desde, hasta] a parqueos
    /// ubicados en la provincia indicada (búsqueda aproximada). Incluye entrada,
    /// salida y monto a cancelar, calculado con <see cref="ObtenerPrecioPorHoraPorParqueo"/>.
    /// </summary>
    Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorProvinciaAsync(
        string provincia,
        DateTime desde,
        DateTime hasta,
        Paginacion? paginacion = null,
        CancellationToken cancelacion = default);
}
