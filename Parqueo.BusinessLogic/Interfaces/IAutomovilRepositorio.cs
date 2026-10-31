using Parqueo.BusinessLogic.Consultas;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Interfaces;

/// <summary>Repositorio de la tabla de automóviles (prq_automovil).</summary>
public interface IAutomovilRepositorio : IRepositorio<PrqAutomovil>
{
    /// <summary>
    /// Lista de automóviles por color, intervalo de año, nombre del fabricante y
    /// nombre del tipo. Los filtros son opcionales y se combinan entre sí; los de
    /// texto son aproximados. Resultado ordenado por id.
    /// </summary>
    Task<IReadOnlyList<PrqAutomovil>> BuscarAsync(
        FiltroAutomovil filtro,
        Paginacion? paginacion = null,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Cantidad total de automóviles que cumplen el filtro, sin paginación. Permite a los
    /// clientes del API saber cuántas páginas hay (Trabajo 04).
    /// </summary>
    Task<int> ContarAsync(FiltroAutomovil filtro, CancellationToken cancelacion = default);
}
