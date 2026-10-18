using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;

namespace Parqueo.BusinessLogic.Repositorios.Json;

/// <summary>
/// Acceso a los archivos JSON de una carpeta: un archivo por tabla.
/// Lee y escribe listas de entidades con <see cref="JsonSerializer"/>.
/// </summary>
/// <remarks>
/// Convenciones del formato: propiedades en camelCase (igual que los archivos de
/// la carpeta Datos/), fechas ISO 8601 sin zona horaria, decimales como números y
/// salida nula como <c>null</c>. Las propiedades de navegación y los campos
/// calculados no se escriben, para que cada archivo conserve solo las columnas de
/// su tabla.
/// </remarks>
public sealed class FuenteDatosJson
{
    public const string ArchivoAutomoviles = "automoviles.json";
    public const string ArchivoParqueos = "parqueos.json";
    public const string ArchivoIngresos = "ingresos-automoviles.json";

    private readonly JsonSerializerOptions _opciones;

    public FuenteDatosJson(string directorio)
    {
        if (string.IsNullOrWhiteSpace(directorio))
        {
            throw new ArgumentException("Debe indicar la carpeta de los archivos JSON.", nameof(directorio));
        }

        Directorio = Path.GetFullPath(directorio);
        if (!Directory.Exists(Directorio))
        {
            throw new DirectoryNotFoundException($"No existe la carpeta de datos JSON '{Directorio}'.");
        }

        _opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ConservarSoloColumnas } },
        };
    }

    /// <summary>Carpeta absoluta donde se leen y escriben los archivos.</summary>
    public string Directorio { get; }

    /// <summary>
    /// Lee todos los registros del archivo. Si el archivo no existe, devuelve una
    /// lista vacía (se creará en la primera escritura).
    /// </summary>
    /// <exception cref="InvalidDataException">El archivo no contiene JSON válido.</exception>
    public async Task<List<T>> LeerAsync<T>(string archivo, CancellationToken cancelacion = default)
    {
        string ruta = RutaDe(archivo);
        if (!File.Exists(ruta))
        {
            return new List<T>();
        }

        try
        {
            await using var flujo = File.OpenRead(ruta);
            return await JsonSerializer.DeserializeAsync<List<T>>(flujo, _opciones, cancelacion) ?? new List<T>();
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"El archivo JSON '{archivo}' no tiene un formato válido: {error.Message}", error);
        }
    }

    /// <summary>
    /// Reemplaza el contenido del archivo con los registros indicados. Escribe primero
    /// un archivo temporal y luego lo mueve, para no dejar el archivo a medio escribir.
    /// </summary>
    public async Task GuardarAsync<T>(string archivo, IEnumerable<T> registros, CancellationToken cancelacion = default)
    {
        string ruta = RutaDe(archivo);
        string temporal = ruta + ".tmp";

        await using (var flujo = File.Create(temporal))
        {
            await JsonSerializer.SerializeAsync(flujo, registros, _opciones, cancelacion);
            await flujo.WriteAsync("\n"u8.ToArray(), cancelacion);
        }

        File.Move(temporal, ruta, overwrite: true);
    }

    private string RutaDe(string archivo) => Path.Combine(Directorio, archivo);

    /// <summary>
    /// Solo se serializan propiedades de tipos simples (números, texto y fechas):
    /// se omiten las navegaciones (otras entidades y colecciones).
    /// </summary>
    private static void ConservarSoloColumnas(JsonTypeInfo informacion)
    {
        if (informacion.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        for (int indice = informacion.Properties.Count - 1; indice >= 0; indice--)
        {
            Type tipo = informacion.Properties[indice].PropertyType;
            tipo = Nullable.GetUnderlyingType(tipo) ?? tipo;

            bool esColumna = tipo.IsPrimitive || tipo == typeof(string) || tipo == typeof(decimal) || tipo == typeof(DateTime);
            if (!esColumna)
            {
                informacion.Properties.RemoveAt(indice);
            }
        }
    }
}
