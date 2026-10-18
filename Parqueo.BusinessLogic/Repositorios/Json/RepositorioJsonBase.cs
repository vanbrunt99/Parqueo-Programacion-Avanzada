using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;

namespace Parqueo.BusinessLogic.Repositorios.Json;

/// <summary>
/// Base genérica de los repositorios que leen y escriben archivos JSON locales.
/// Equivale a <c>RepositorioEfBase</c>, pero el "contexto" es una carpeta con un
/// archivo por tabla.
/// </summary>
public abstract class RepositorioJsonBase<TEntidad> : IRepositorio<TEntidad> where TEntidad : class
{
    protected RepositorioJsonBase(FuenteDatosJson fuente, string archivo)
    {
        Fuente = fuente ?? throw new ArgumentNullException(nameof(fuente));
        Archivo = archivo;
    }

    protected FuenteDatosJson Fuente { get; }

    /// <summary>Archivo JSON de la tabla.</summary>
    protected string Archivo { get; }

    protected ReglasConsulta Reglas { get; } = new();

    /// <summary>Devuelve el valor de la llave primaria de la entidad.</summary>
    protected abstract int ObtenerLlave(TEntidad entidad);

    /// <summary>
    /// Carga las propiedades de navegación de un nivel, igual que el Include de EF.
    /// </summary>
    protected virtual Task IncluirPropiedadesNavegacionAsync(TEntidad entidad, CancellationToken cancelacion) =>
        Task.CompletedTask;

    protected Task<List<TEntidad>> LeerTodosAsync(CancellationToken cancelacion) =>
        Fuente.LeerAsync<TEntidad>(Archivo, cancelacion);

    public virtual async Task<TEntidad?> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        var registros = await LeerTodosAsync(cancelacion);
        var entidad = registros.FirstOrDefault(registro => ObtenerLlave(registro) == id);

        if (entidad is not null)
        {
            await IncluirPropiedadesNavegacionAsync(entidad, cancelacion);
        }

        return entidad;
    }
}
