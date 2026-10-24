using Microsoft.EntityFrameworkCore;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.BusinessLogic.Validacion;
using Parqueo.Model.Contexto;

namespace Parqueo.BusinessLogic.Repositorios.BaseDatos;

/// <summary>
/// Base genérica de los repositorios que trabajan con el contexto de Entity
/// Framework (PostgreSQL). Sigue la idea de la clase abstracta genérica vista en la
/// semana 5: recibe el contexto, conoce el nombre de la llave primaria, define qué
/// propiedades de navegación se incluyen (un solo nivel) e implementa una sola vez
/// insertar, actualizar, borrar y consultar por llave primaria.
/// </summary>
public abstract class RepositorioEfBase<TEntidad> : IRepositorio<TEntidad> where TEntidad : class, new()
{
    protected RepositorioEfBase(ParqueoContext contexto, string nombreLlavePrimaria, string nombreTabla)
    {
        Contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        NombreLlavePrimaria = nombreLlavePrimaria;
        NombreTabla = nombreTabla;
    }

    protected ParqueoContext Contexto { get; }

    protected string NombreLlavePrimaria { get; }

    protected string NombreTabla { get; }

    protected DbSet<TEntidad> Entidades => Contexto.Set<TEntidad>();

    protected ReglasConsulta Reglas { get; } = new();

    protected ValidadorEntidades Validador { get; } = new();

    /// <summary>Propiedades de navegación (un nivel) que se cargan al consultar por id.</summary>
    protected virtual IQueryable<TEntidad> IncluirPropiedadesNavegacion(IQueryable<TEntidad> consulta) => consulta;

    /// <summary>Aplica las reglas de <see cref="ValidadorEntidades"/> a la entidad.</summary>
    protected abstract void Validar(TEntidad entidad);

    /// <summary>Verifica que existan los registros referenciados (llaves foráneas).</summary>
    protected virtual Task ValidarReferenciasAsync(TEntidad entidad, CancellationToken cancelacion) => Task.CompletedTask;

    /// <summary>Verifica que el registro pueda borrarse (no tenga dependientes).</summary>
    protected virtual Task ValidarBorradoAsync(int id, CancellationToken cancelacion) => Task.CompletedTask;

    public virtual async Task<TEntidad?> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        return await IncluirPropiedadesNavegacion(Entidades.AsNoTracking())
            .FirstOrDefaultAsync(entidad => EF.Property<int>(entidad, NombreLlavePrimaria) == id, cancelacion);
    }

    public virtual async Task<TEntidad> InsertarAsync(TEntidad entidad, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entidad);

        int llave = LeerLlave(entidad);
        if (llave != 0)
        {
            throw await ExisteAsync(llave, cancelacion)
                ? Validador.LlaveDuplicada(NombreTabla, llave)
                : Validador.LlaveNoPermitida(NombreTabla, llave);
        }

        Validar(entidad);
        await ValidarReferenciasAsync(entidad, cancelacion);

        // Se inserta una copia con solo las columnas: así EF no intenta insertar
        // también las entidades enlazadas en las propiedades de navegación.
        var nuevo = new TEntidad();
        var entrada = Contexto.Entry(nuevo);
        entrada.CurrentValues.SetValues(entidad);
        entrada.State = EntityState.Added;

        try
        {
            await GuardarCambiosAsync(cancelacion);

            // La identidad generada por PostgreSQL se devuelve también en el objeto recibido.
            typeof(TEntidad).GetProperty(NombreLlavePrimaria)!.SetValue(entidad, LeerLlave(nuevo));
        }
        finally
        {
            // Si el guardado falla, la copia no queda pendiente en el contexto.
            entrada.State = EntityState.Detached;
        }

        // Se devuelve el registro leído de la base, con sus propiedades de navegación.
        return await ObtenerPorIdAsync(LeerLlave(nuevo), cancelacion)
            ?? throw new InvalidOperationException($"No se pudo leer el registro insertado en {NombreTabla}.");
    }

    public virtual async Task<bool> ActualizarAsync(TEntidad entidad, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entidad);

        var existente = await Entidades.FindAsync([LeerLlave(entidad)], cancelacion);
        if (existente is null)
        {
            return false;
        }

        try
        {
            Validar(entidad);
            await ValidarReferenciasAsync(entidad, cancelacion);

            // Copia solo los valores de las columnas; el estado pasa a Modified.
            Contexto.Entry(existente).CurrentValues.SetValues(entidad);
            await GuardarCambiosAsync(cancelacion);
            return true;
        }
        finally
        {
            Contexto.Entry(existente).State = EntityState.Detached;
        }
    }

    public virtual async Task<bool> BorrarAsync(int id, CancellationToken cancelacion = default)
    {
        var existente = await Entidades.FindAsync([id], cancelacion);
        if (existente is null)
        {
            return false;
        }

        try
        {
            await ValidarBorradoAsync(id, cancelacion);
            Entidades.Remove(existente);
            return await GuardarCambiosAsync(cancelacion) > 0;
        }
        finally
        {
            Contexto.Entry(existente).State = EntityState.Detached;
        }
    }

    protected int LeerLlave(TEntidad entidad) =>
        (int)typeof(TEntidad).GetProperty(NombreLlavePrimaria)!.GetValue(entidad)!;

    private Task<bool> ExisteAsync(int id, CancellationToken cancelacion) =>
        Entidades.AnyAsync(entidad => EF.Property<int>(entidad, NombreLlavePrimaria) == id, cancelacion);

    /// <summary>
    /// Guarda los cambios y traduce los errores de PostgreSQL (restricciones, llaves
    /// foráneas, conexión) a una excepción con un mensaje comprensible.
    /// </summary>
    private async Task<int> GuardarCambiosAsync(CancellationToken cancelacion)
    {
        try
        {
            return await Contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException error)
        {
            string detalle = error.InnerException?.Message ?? error.Message;
            throw new ReglaNegocioException($"PostgreSQL rechazó el cambio en {NombreTabla}: {detalle}", error);
        }
    }
}
