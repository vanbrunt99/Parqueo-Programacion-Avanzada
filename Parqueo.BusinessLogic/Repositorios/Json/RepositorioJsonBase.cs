using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.BusinessLogic.Validacion;

namespace Parqueo.BusinessLogic.Repositorios.Json;

/// <summary>
/// Base genérica de los repositorios que leen y escriben archivos JSON locales.
/// Equivale a <c>RepositorioEfBase</c>, pero el "contexto" es una carpeta con un
/// archivo por tabla. Cada inserción, actualización o borrado se guarda de
/// inmediato en el archivo, por lo que los cambios se conservan entre ejecuciones.
/// </summary>
public abstract class RepositorioJsonBase<TEntidad> : IRepositorio<TEntidad> where TEntidad : class
{
    protected RepositorioJsonBase(FuenteDatosJson fuente, string archivo, string nombreTabla)
    {
        Fuente = fuente ?? throw new ArgumentNullException(nameof(fuente));
        Archivo = archivo;
        NombreTabla = nombreTabla;
    }

    protected FuenteDatosJson Fuente { get; }

    /// <summary>Archivo JSON de la tabla.</summary>
    protected string Archivo { get; }

    protected string NombreTabla { get; }

    protected ReglasConsulta Reglas { get; } = new();

    protected ValidadorEntidades Validador { get; } = new();

    /// <summary>Devuelve el valor de la llave primaria de la entidad.</summary>
    protected abstract int ObtenerLlave(TEntidad entidad);

    /// <summary>Asigna la llave primaria generada al insertar.</summary>
    protected abstract void AsignarLlave(TEntidad entidad, int llave);

    /// <summary>Aplica las reglas de <see cref="ValidadorEntidades"/> a la entidad.</summary>
    protected abstract void Validar(TEntidad entidad);

    /// <summary>Verifica que existan los registros referenciados (llaves foráneas).</summary>
    protected virtual Task ValidarReferenciasAsync(TEntidad entidad, CancellationToken cancelacion) => Task.CompletedTask;

    /// <summary>Verifica que el registro pueda borrarse (no tenga dependientes).</summary>
    protected virtual Task ValidarBorradoAsync(int id, CancellationToken cancelacion) => Task.CompletedTask;

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

    public virtual async Task<TEntidad> InsertarAsync(TEntidad entidad, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entidad);
        var registros = await LeerTodosAsync(cancelacion);

        int llave = ObtenerLlave(entidad);
        if (llave != 0)
        {
            throw registros.Any(registro => ObtenerLlave(registro) == llave)
                ? Validador.LlaveDuplicada(NombreTabla, llave)
                : Validador.LlaveNoPermitida(NombreTabla, llave);
        }

        Validar(entidad);
        await ValidarReferenciasAsync(entidad, cancelacion);

        // Siguiente consecutivo del archivo (equivalente a la identidad de PostgreSQL).
        int nuevaLlave = registros.Count == 0 ? 1 : registros.Max(ObtenerLlave) + 1;
        AsignarLlave(entidad, nuevaLlave);
        registros.Add(entidad);
        await Fuente.GuardarAsync(Archivo, registros, cancelacion);

        // Se devuelve el registro leído del archivo, con sus propiedades de navegación.
        return await ObtenerPorIdAsync(nuevaLlave, cancelacion)
            ?? throw new InvalidOperationException($"No se pudo leer el registro insertado en {NombreTabla}.");
    }

    public virtual async Task<bool> ActualizarAsync(TEntidad entidad, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entidad);
        var registros = await LeerTodosAsync(cancelacion);

        int posicion = registros.FindIndex(registro => ObtenerLlave(registro) == ObtenerLlave(entidad));
        if (posicion < 0)
        {
            return false;
        }

        Validar(entidad);
        await ValidarReferenciasAsync(entidad, cancelacion);

        registros[posicion] = entidad;
        await Fuente.GuardarAsync(Archivo, registros, cancelacion);
        return true;
    }

    public virtual async Task<bool> BorrarAsync(int id, CancellationToken cancelacion = default)
    {
        var registros = await LeerTodosAsync(cancelacion);

        int posicion = registros.FindIndex(registro => ObtenerLlave(registro) == id);
        if (posicion < 0)
        {
            return false;
        }

        await ValidarBorradoAsync(id, cancelacion);

        registros.RemoveAt(posicion);
        await Fuente.GuardarAsync(Archivo, registros, cancelacion);
        return true;
    }
}
