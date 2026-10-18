using Parqueo.BusinessLogic.Consultas;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Interfaces;

/// <summary>Repositorio de la tabla de parqueos (prq_parqueo).</summary>
public interface IParqueoRepositorio : IRepositorio<PrqParqueo>
{
    /// <summary>
    /// Parqueos por nombre de provincia, nombre del parqueo y rango de precios por
    /// hora. Los filtros son opcionales y se combinan; los de texto son aproximados.
    /// Resultado ordenado por id.
    /// </summary>
    Task<IReadOnlyList<PrqParqueo>> BuscarAsync(
        FiltroParqueo filtro,
        Paginacion? paginacion = null,
        CancellationToken cancelacion = default);
}
