using Parqueo.Model.Entidades;

namespace Parqueo.Tests.Modelo;

/// <summary>
/// Pruebas de los campos personalizados de <see cref="PrqIngresoAutomovil"/>:
/// duración en minutos, duración en horas y monto total a pagar.
/// </summary>
public class CamposPersonalizadosPruebas
{
    private static PrqIngresoAutomovil CrearIngreso(string entrada, string? salida, decimal? precioPorHora = 1250.00m)
    {
        var ingreso = new PrqIngresoAutomovil
        {
            Consecutivo = 1,
            IdParqueo = 1,
            IdAutomovil = 1,
            FechaHoraEntrada = DateTime.Parse(entrada),
            FechaHoraSalida = salida is null ? null : DateTime.Parse(salida),
        };

        if (precioPorHora is decimal precio)
        {
            ingreso.IdParqueoNavigation = new PrqParqueo
            {
                Id = 1,
                NombreProvincia = "San José",
                Nombre = "Parqueo de prueba",
                PrecioPorHora = precio,
            };
        }

        return ingreso;
    }

    [Fact]
    public void SalidaNula_LosTresCamposSonNulos()
    {
        var ingreso = CrearIngreso("2026-10-17T08:05:00", salida: null);

        Assert.Null(ingreso.DuracionEstadiaMinutos);
        Assert.Null(ingreso.DuracionEstadiaHoras);
        Assert.Null(ingreso.MontoTotalAPagar);
        Assert.Null(ingreso.CalcularMontoTotal(1250.00m));
    }

    [Fact]
    public void SalidaNula_NoDevuelveCero()
    {
        var ingreso = CrearIngreso("2026-10-17T08:05:00", salida: null);

        Assert.NotEqual(0m, ingreso.DuracionEstadiaMinutos);
        Assert.NotEqual(0m, ingreso.DuracionEstadiaHoras);
        Assert.NotEqual(0m, ingreso.MontoTotalAPagar);
    }

    [Fact]
    public void EstadiaDeDosHorasYMedia_CalculaMinutosHorasYMonto()
    {
        var ingreso = CrearIngreso("2026-10-01T07:45:00", "2026-10-01T10:15:00", 1250.00m);

        Assert.Equal(150m, ingreso.DuracionEstadiaMinutos);
        Assert.Equal(2.5m, ingreso.DuracionEstadiaHoras);
        Assert.Equal(3125.00m, ingreso.MontoTotalAPagar);
    }

    [Fact]
    public void FraccionDeHora_NoSePierdePorDivisionEntera()
    {
        // 80 minutos = 1.333... horas; una división entera daría 1 hora.
        var ingreso = CrearIngreso("2026-10-05T12:00:00", "2026-10-05T13:20:00", 1250.00m);

        Assert.Equal(80m, ingreso.DuracionEstadiaMinutos);
        Assert.Equal(1.3333m, Math.Round(ingreso.DuracionEstadiaHoras!.Value, 4));
        Assert.Equal(1666.67m, ingreso.MontoTotalAPagar);
    }

    [Theory]
    [InlineData("2026-10-03T13:20:00", "2026-10-03T16:05:00", "675.50", "1857.63")] // 1857.625 -> 1857.63
    [InlineData("2026-10-09T18:15:00", "2026-10-09T19:00:00", "675.50", "506.63")]  //  506.625 ->  506.63
    [InlineData("2026-10-06T07:00:00", "2026-10-06T07:50:00", "675.50", "562.92")]
    [InlineData("2026-10-15T16:45:00", "2026-10-15T17:05:00", "800.00", "266.67")]
    [InlineData("2026-10-04T10:00:00", "2026-10-04T18:00:00", "800.00", "6400.00")]
    [InlineData("2026-10-06T07:00:00", "2026-10-06T07:50:00", "675.03", "562.53")]  //  562.525 ->  562.53 (sin perder el medio céntimo)
    public void Monto_SeRedondeaADosDecimalesAlejandoseDeCero(string entrada, string salida, string precio, string esperado)
    {
        var ingreso = CrearIngreso(entrada, salida, decimal.Parse(precio, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture), ingreso.MontoTotalAPagar);
    }

    [Fact]
    public void SegundosEnLaEstadia_SeReflejanComoFraccionDeMinuto()
    {
        var ingreso = CrearIngreso("2026-10-01T08:00:00", "2026-10-01T08:10:30", 600.00m);

        Assert.Equal(10.5m, ingreso.DuracionEstadiaMinutos);
        Assert.Equal(0.175m, ingreso.DuracionEstadiaHoras);
        Assert.Equal(105.00m, ingreso.MontoTotalAPagar);
    }

    [Fact]
    public void DiferentesTarifas_ProducenMontosDiferentesParaLaMismaDuracion()
    {
        var sanJose = CrearIngreso("2026-10-01T09:00:00", "2026-10-01T10:30:00", 1250.00m);
        var heredia = CrearIngreso("2026-10-01T09:00:00", "2026-10-01T10:30:00", 800.00m);

        Assert.Equal(1875.00m, sanJose.MontoTotalAPagar);
        Assert.Equal(1200.00m, heredia.MontoTotalAPagar);
    }

    [Fact]
    public void SinNavegacionDeParqueo_MontoEsNuloPeroLaDuracionSeCalcula()
    {
        var ingreso = CrearIngreso("2026-10-01T09:00:00", "2026-10-01T10:00:00", precioPorHora: null);

        Assert.Equal(60m, ingreso.DuracionEstadiaMinutos);
        Assert.Equal(1m, ingreso.DuracionEstadiaHoras);
        Assert.Null(ingreso.MontoTotalAPagar);
        Assert.Equal(800.00m, ingreso.CalcularMontoTotal(800.00m));
    }
}
