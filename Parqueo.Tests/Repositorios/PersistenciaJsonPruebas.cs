using Parqueo.BusinessLogic.Repositorios.Json;
using Parqueo.Model.Entidades;
using Parqueo.Tests.Infraestructura;

namespace Parqueo.Tests.Repositorios;

/// <summary>Pruebas propias del almacenamiento en archivos JSON.</summary>
public sealed class PersistenciaJsonPruebas : IDisposable
{
    private readonly CarpetaJsonTemporal _carpeta = new();

    public void Dispose() => _carpeta.Dispose();

    [Theory]
    [InlineData(FuenteDatosJson.ArchivoAutomoviles)]
    [InlineData(FuenteDatosJson.ArchivoParqueos)]
    [InlineData(FuenteDatosJson.ArchivoIngresos)]
    public async Task LeerYGuardarSinCambios_ConservaElArchivoIdentico(string archivo)
    {
        string original = await File.ReadAllTextAsync(_carpeta.RutaArchivo(archivo));

        switch (archivo)
        {
            case FuenteDatosJson.ArchivoAutomoviles:
                await _carpeta.Fuente.GuardarAsync(archivo, await _carpeta.Fuente.LeerAsync<PrqAutomovil>(archivo));
                break;
            case FuenteDatosJson.ArchivoParqueos:
                await _carpeta.Fuente.GuardarAsync(archivo, await _carpeta.Fuente.LeerAsync<PrqParqueo>(archivo));
                break;
            default:
                await _carpeta.Fuente.GuardarAsync(archivo, await _carpeta.Fuente.LeerAsync<PrqIngresoAutomovil>(archivo));
                break;
        }

        Assert.Equal(original, await File.ReadAllTextAsync(_carpeta.RutaArchivo(archivo)));
    }

    [Fact]
    public async Task Guardar_EscribeSoloLasColumnasEnElArchivo()
    {
        var repositorio = new IngresoAutomovilRepositorioJson(_carpeta.Fuente);
        var ingreso = await repositorio.ObtenerPorIdAsync(1); // trae navegaciones y campos calculados
        ingreso!.FechaHoraSalida = new DateTime(2026, 10, 1, 10, 30, 0);

        await repositorio.ActualizarAsync(ingreso);

        string texto = await File.ReadAllTextAsync(_carpeta.RutaArchivo(FuenteDatosJson.ArchivoIngresos));
        Assert.Contains("\"fechaHoraSalida\": \"2026-10-01T10:30:00\"", texto);
        Assert.DoesNotContain("Navigation", texto);
        Assert.DoesNotContain("montoTotalAPagar", texto);
        Assert.DoesNotContain("duracionEstadia", texto);
    }

    [Fact]
    public async Task CambiosSobrevivenAUnaNuevaInstancia()
    {
        var nuevo = await new AutomovilRepositorioJson(_carpeta.Fuente).InsertarAsync(
            new PrqAutomovil { Color = "Morado", Anio = 2024, Fabricante = "BYD", Tipo = "sedán" });

        var otraEjecucion = new AutomovilRepositorioJson(new FuenteDatosJson(_carpeta.Ruta));
        var leido = await otraEjecucion.ObtenerPorIdAsync(nuevo.Id);

        Assert.Equal(7, nuevo.Id);
        Assert.Equal("BYD", leido!.Fabricante);
        Assert.Contains("\"fabricante\": \"BYD\"", await File.ReadAllTextAsync(_carpeta.RutaArchivo(FuenteDatosJson.ArchivoAutomoviles)));
    }

    [Fact]
    public async Task FechaConKindLocal_SeGuardaSinZonaHoraria()
    {
        var repositorio = new IngresoAutomovilRepositorioJson(_carpeta.Fuente);
        var guardado = await repositorio.InsertarAsync(new PrqIngresoAutomovil
        {
            IdParqueo = 2,
            IdAutomovil = 4,
            FechaHoraEntrada = new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Local),
        });

        string texto = await File.ReadAllTextAsync(_carpeta.RutaArchivo(FuenteDatosJson.ArchivoIngresos));
        Assert.Contains("\"fechaHoraEntrada\": \"2026-10-19T09:00:00\",", texto);
        Assert.Equal(new DateTime(2026, 10, 19, 9, 0, 0), (await repositorio.ObtenerPorIdAsync(guardado.Consecutivo))!.FechaHoraEntrada);
    }

    [Fact]
    public async Task ArchivoConJsonInvalido_LanzaInvalidDataException()
    {
        await File.WriteAllTextAsync(_carpeta.RutaArchivo(FuenteDatosJson.ArchivoParqueos), "[ { \"id\": 1, ");

        var error = await Assert.ThrowsAsync<InvalidDataException>(
            () => new ParqueoRepositorioJson(_carpeta.Fuente).ObtenerPorIdAsync(1));
        Assert.Contains(FuenteDatosJson.ArchivoParqueos, error.Message);
    }

    [Fact]
    public void CarpetaInexistente_LanzaDirectoryNotFoundException()
    {
        Assert.Throws<DirectoryNotFoundException>(() => new FuenteDatosJson(Path.Combine(_carpeta.Ruta, "no-existe")));
    }

    [Fact]
    public async Task ArchivoInexistente_SeTrataComoTablaVaciaYSeCreaAlInsertar()
    {
        File.Delete(_carpeta.RutaArchivo(FuenteDatosJson.ArchivoAutomoviles));
        File.Delete(_carpeta.RutaArchivo(FuenteDatosJson.ArchivoIngresos));
        var repositorio = new AutomovilRepositorioJson(_carpeta.Fuente);

        Assert.Empty(await repositorio.BuscarAsync(new()));
        var nuevo = await repositorio.InsertarAsync(new PrqAutomovil { Color = "Negro", Anio = 2020, Fabricante = "Ford", Tipo = "4x4" });

        Assert.Equal(1, nuevo.Id);
        Assert.True(File.Exists(_carpeta.RutaArchivo(FuenteDatosJson.ArchivoAutomoviles)));
    }

    [Fact]
    public async Task IngresoConReferenciaRotaEnElArchivo_SeInformaComoDatosInconsistentes()
    {
        string ruta = _carpeta.RutaArchivo(FuenteDatosJson.ArchivoIngresos);
        await File.WriteAllTextAsync(ruta, (await File.ReadAllTextAsync(ruta)).Replace("\"idAutomovil\": 6,", "\"idAutomovil\": 60,"));

        await Assert.ThrowsAsync<InvalidDataException>(() => new IngresoAutomovilRepositorioJson(_carpeta.Fuente).ObtenerPorIdAsync(7));
    }

    [Fact]
    public async Task InsercionesSimultaneas_NoSePierdenRegistros()
    {
        var repositorio = new AutomovilRepositorioJson(_carpeta.Fuente);

        var tareas = Enumerable.Range(0, 40).Select(n => repositorio.InsertarAsync(
            new PrqAutomovil { Color = "Gris", Anio = 2020, Fabricante = $"Concurrencia {n}", Tipo = "sedán" }));
        var insertados = await Task.WhenAll(tareas);

        Assert.Equal(40, insertados.Select(a => a.Id).Distinct().Count());
        Assert.Equal(46, (await new AutomovilRepositorioJson(new FuenteDatosJson(_carpeta.Ruta)).BuscarAsync(new(), new(1, 100))).Count);
    }

    [Fact]
    public async Task FechaConDesfaseEnElArchivo_SeLeeComoHoraEscrita()
    {
        string ruta = _carpeta.RutaArchivo(FuenteDatosJson.ArchivoIngresos);
        await File.WriteAllTextAsync(ruta, (await File.ReadAllTextAsync(ruta))
            .Replace("\"2026-10-17T09:20:00\"", "\"2026-10-17T09:20:00-06:00\""));

        var ingreso = await new IngresoAutomovilRepositorioJson(_carpeta.Fuente).ObtenerPorIdAsync(17);

        Assert.Equal(new DateTime(2026, 10, 17, 9, 20, 0), ingreso!.FechaHoraEntrada);
        Assert.Equal(DateTimeKind.Unspecified, ingreso.FechaHoraEntrada.Kind);
    }
}
