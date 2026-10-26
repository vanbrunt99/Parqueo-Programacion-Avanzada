using System.Data.Common;
using System.Globalization;
using Npgsql;

namespace Parqueo.AppConsole;

/// <summary>
/// Prepara y revisa la base PostgreSQL del curso sin necesidad de instalar psql.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>--EstadoBd=true</c>: muestra base, esquema, usuario y versión, y si existen las
/// tablas PRQ con su cantidad de registros. Código de salida: 0 si existen las tres, 2 si no
/// existe ninguna, 3 si solo existen algunas, 1 si hubo un error.</item>
/// <item><c>--EjecutarScripts="ruta1;ruta2"</c>: ejecuta los scripts en orden (rutas relativas a
/// la carpeta actual) y muestra el resultado de sus consultas. Se detiene en el primer error;
/// como los scripts del repositorio trabajan dentro de una transacción, un error no deja
/// cambios parciales.</item>
/// </list>
/// Los scripts no deben usar comandos propios de psql (por ejemplo <c>\echo</c>); por eso
/// <c>verificar-restricciones.sql</c> se sigue ejecutando con psql.
/// </remarks>
public sealed class HerramientasBaseDatos
{
    private static readonly string[] Tablas = ["prq_automovil", "prq_parqueo", "prq_ingreso_automovil"];

    private readonly string _cadenaConexion;

    public HerramientasBaseDatos(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    public async Task<int> MostrarEstadoAsync()
    {
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync();

        await using (var consulta = new NpgsqlCommand(
            "SELECT current_database(), current_schema(), current_user, split_part(version(), ' on ', 1)", conexion))
        await using (var lector = await consulta.ExecuteReaderAsync())
        {
            await lector.ReadAsync();
            Console.WriteLine($"Base: {lector.GetString(0)} · Esquema: {lector.GetString(1)} · Usuario: {lector.GetString(2)} · {lector.GetString(3)}");
        }

        int existentes = 0;
        foreach (string tabla in Tablas)
        {
            await using var existe = new NpgsqlCommand("SELECT to_regclass(@tabla) IS NOT NULL", conexion);
            existe.Parameters.AddWithValue("tabla", tabla);
            if ((bool)(await existe.ExecuteScalarAsync())!)
            {
                existentes++;
                // El nombre de la tabla viene de la lista fija de arriba, no del usuario.
                await using var conteo = new NpgsqlCommand($"SELECT count(*) FROM {tabla}", conexion);
                Console.WriteLine($"  {tabla,-24} existe  · {await conteo.ExecuteScalarAsync()} registros");
            }
            else
            {
                Console.WriteLine($"  {tabla,-24} no existe");
            }
        }

        return existentes == Tablas.Length ? 0 : existentes == 0 ? 2 : 3;
    }

    public async Task<int> EjecutarScriptsAsync(IReadOnlyList<string> rutas)
    {
        foreach (string ruta in rutas)
        {
            if (!File.Exists(ruta))
            {
                Console.WriteLine($"No se encontró el script {Path.GetFullPath(ruta)}.");
                return 1;
            }
        }

        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync();

        foreach (string ruta in rutas)
        {
            Console.WriteLine();
            Console.WriteLine($"== Ejecutando {ruta}");
            string texto = await File.ReadAllTextAsync(ruta);
            await using var comando = new NpgsqlCommand(texto, conexion) { CommandTimeout = 120 };
            try
            {
                await using var lector = await comando.ExecuteReaderAsync();
                do
                {
                    if (lector.FieldCount > 0)
                    {
                        await MostrarResultadoAsync(lector);
                    }
                }
                while (await lector.NextResultAsync());
            }
            catch (PostgresException error)
            {
                Console.WriteLine($"PostgreSQL rechazó el script ({error.SqlState}): {error.MessageText}");
                Console.WriteLine("No se ejecutaron los scripts restantes.");
                return 1;
            }

            Console.WriteLine($"== {ruta}: ejecutado sin errores.");
        }

        return 0;
    }

    private static async Task MostrarResultadoAsync(DbDataReader lector)
    {
        var columnas = Enumerable.Range(0, lector.FieldCount).Select(lector.GetName).ToArray();
        var filas = new List<string[]>();
        while (await lector.ReadAsync())
        {
            filas.Add(Enumerable.Range(0, lector.FieldCount).Select(i => Texto(lector.GetValue(i))).ToArray());
        }

        var anchos = columnas.Select((c, i) => Math.Max(c.Length, filas.Count == 0 ? 0 : filas.Max(f => f[i].Length))).ToArray();
        Console.WriteLine();
        Console.WriteLine(string.Join(" | ", columnas.Select((c, i) => c.PadRight(anchos[i]))));
        Console.WriteLine(string.Join("-+-", anchos.Select(a => new string('-', a))));
        foreach (var fila in filas)
        {
            Console.WriteLine(string.Join(" | ", fila.Select((v, i) => v.PadRight(anchos[i]))));
        }

        Console.WriteLine($"({filas.Count} filas)");
    }

    private static string Texto(object valor) => valor switch
    {
        DBNull => "null",
        DateTime fecha => fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable numero => numero.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString() ?? "",
    };
}
