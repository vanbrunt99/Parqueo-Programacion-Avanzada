using Microsoft.EntityFrameworkCore;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Contexto;

namespace Parqueo.BusinessLogic.Repositorios.BaseDatos;

/// <summary>
/// Base genérica de los repositorios que trabajan con el contexto de Entity
/// Framework (PostgreSQL). Sigue la idea de la clase abstracta genérica vista en la
/// semana 5: recibe el contexto, conoce el nombre de la llave primaria y define qué
/// propiedades de navegación se incluyen (un solo nivel), para no repetir código.
/// </summary>
public abstract class RepositorioEfBase<TEntidad> : IRepositorio<TEntidad> where TEntidad : class
{
    protected RepositorioEfBase(ParqueoContext contexto, string nombreLlavePrimaria)
    {
        Contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        NombreLlavePrimaria = nombreLlavePrimaria;
    }

    protected ParqueoContext Contexto { get; }

    protected string NombreLlavePrimaria { get; }

    protected DbSet<TEntidad> Entidades => Contexto.Set<TEntidad>();

    protected ReglasConsulta Reglas { get; } = new();

    /// <summary>Propiedades de navegación (un nivel) que se cargan al consultar por id.</summary>
    protected virtual IQueryable<TEntidad> IncluirPropiedadesNavegacion(IQueryable<TEntidad> consulta) => consulta;

    public virtual async Task<TEntidad?> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        return await IncluirPropiedadesNavegacion(Entidades.AsNoTracking())
            .FirstOrDefaultAsync(entidad => EF.Property<int>(entidad, NombreLlavePrimaria) == id, cancelacion);
    }
}
