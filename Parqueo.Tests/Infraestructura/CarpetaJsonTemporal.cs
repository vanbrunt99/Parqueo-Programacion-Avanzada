using Parqueo.BusinessLogic.Repositorios.Json;

namespace Parqueo.Tests.Infraestructura;

/// <summary>
/// Copia los JSON entregados (carpeta DatosOriginales de la salida de pruebas) a una
/// carpeta temporal única. Así cada prueba puede insertar, actualizar o borrar sin
/// modificar los archivos de la carpeta Datos/ del repositorio.
/// </summary>
public sealed class CarpetaJsonTemporal : IDisposable
{
    public CarpetaJsonTemporal()
    {
        Ruta = Path.Combine(Path.GetTempPath(), "parqueo-pruebas", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Ruta);

        string origen = Path.Combine(AppContext.BaseDirectory, "DatosOriginales");
        foreach (string archivo in Directory.GetFiles(origen, "*.json"))
        {
            File.Copy(archivo, Path.Combine(Ruta, Path.GetFileName(archivo)));
        }

        Fuente = new FuenteDatosJson(Ruta);
    }

    public string Ruta { get; }

    public FuenteDatosJson Fuente { get; }

    public string RutaArchivo(string archivo) => Path.Combine(Ruta, archivo);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Ruta, recursive: true);
        }
        catch (IOException)
        {
            // Una carpeta temporal que no se pudo borrar no invalida la prueba.
        }
    }
}
