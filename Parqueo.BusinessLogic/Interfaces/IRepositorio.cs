namespace Parqueo.BusinessLogic.Interfaces;

/// <summary>
/// Operaciones comunes a todos los repositorios, sin importar el origen de los
/// datos (contexto de Entity Framework o archivos JSON).
/// </summary>
/// <typeparam name="TEntidad">Entidad generada por el scaffold.</typeparam>
public interface IRepositorio<TEntidad> where TEntidad : class
{
    /// <summary>
    /// Consulta por llave primaria. Incluye las propiedades de navegación de un
    /// solo nivel. Devuelve <c>null</c> si el registro no existe.
    /// </summary>
    Task<TEntidad?> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    /// <summary>
    /// Inserta el registro. La llave primaria debe venir en 0: la asigna el origen de
    /// datos (identidad en PostgreSQL, siguiente consecutivo en JSON) y también se
    /// asigna en el objeto recibido. Devuelve el registro guardado, leído de nuevo con
    /// sus propiedades de navegación.
    /// </summary>
    /// <exception cref="ArgumentException">Datos inválidos o llave enviada por el llamador.</exception>
    /// <exception cref="Validacion.ReglaNegocioException">Llave duplicada o referencia inexistente.</exception>
    Task<TEntidad> InsertarAsync(TEntidad entidad, CancellationToken cancelacion = default);

    /// <summary>
    /// Actualiza el registro con la misma llave primaria. Devuelve <c>false</c> si no
    /// existe (no se modifica nada).
    /// </summary>
    /// <exception cref="ArgumentException">Datos inválidos.</exception>
    /// <exception cref="Validacion.ReglaNegocioException">Referencia inexistente.</exception>
    Task<bool> ActualizarAsync(TEntidad entidad, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra el registro por llave primaria. Devuelve <c>false</c> si no existe.
    /// </summary>
    /// <exception cref="Validacion.ReglaNegocioException">El registro tiene ingresos que lo referencian.</exception>
    Task<bool> BorrarAsync(int id, CancellationToken cancelacion = default);
}
