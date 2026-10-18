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
}
