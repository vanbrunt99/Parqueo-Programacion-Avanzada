using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.Tests.Consultas;

/// <summary>
/// Comprueba que el monto de los listados sale de ObtenerPrecioPorHoraPorParqueo y
/// no del precio guardado en la navegación del parqueo.
/// </summary>
public class GeneradorResumenIngresosPruebas
{
    [Fact]
    public async Task UsaObtenerPrecioPorHoraPorParqueo_UnaVezPorParqueo()
    {
        var parqueo = new PrqParqueo { Id = 1, NombreProvincia = "San José", Nombre = "Central", PrecioPorHora = 9999m };
        var automovil = new PrqAutomovil { Id = 1, Color = "Rojo", Anio = 2019, Fabricante = "Toyota", Tipo = "sedán" };
        var ingresos = new List<PrqIngresoAutomovil>
        {
            Ingreso(1, parqueo, automovil, "2026-10-05T12:00:00", "2026-10-05T13:20:00"),
            Ingreso(2, parqueo, automovil, "2026-10-06T12:00:00", null),
        };
        var repositorio = new RepositorioDePrecioFijo(precio: 1000m);

        var resumen = await new GeneradorResumenIngresos().CrearAsync(ingresos, repositorio);

        Assert.Equal([1], repositorio.ParqueosConsultados);
        Assert.Equal([1000m, 1000m], resumen.Select(r => r.PrecioPorHora));
        Assert.Equal(1333.33m, resumen[0].MontoACancelar); // 80 min a 1000/h, no a 9999/h
        Assert.Null(resumen[1].MontoACancelar);
        Assert.Equal(("Rojo", "sedán", "San José"), (resumen[0].Color, resumen[0].Tipo, resumen[0].NombreProvincia));
    }

    private static PrqIngresoAutomovil Ingreso(int consecutivo, PrqParqueo parqueo, PrqAutomovil automovil, string entrada, string? salida) => new()
    {
        Consecutivo = consecutivo,
        IdParqueo = parqueo.Id,
        IdParqueoNavigation = parqueo,
        IdAutomovil = automovil.Id,
        IdAutomovilNavigation = automovil,
        FechaHoraEntrada = DateTime.Parse(entrada),
        FechaHoraSalida = salida is null ? null : DateTime.Parse(salida),
    };

    /// <summary>Doble de prueba: solo implementa la función de precio y registra sus llamadas.</summary>
    private sealed class RepositorioDePrecioFijo(decimal precio) : IIngresoAutomovilRepositorio
    {
        public List<int> ParqueosConsultados { get; } = [];

        public Task<decimal?> ObtenerPrecioPorHoraPorParqueo(int idParqueo, CancellationToken cancelacion = default)
        {
            ParqueosConsultados.Add(idParqueo);
            return Task.FromResult<decimal?>(precio);
        }

        public Task<PrqIngresoAutomovil?> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<PrqIngresoAutomovil> InsertarAsync(PrqIngresoAutomovil entidad, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<bool> ActualizarAsync(PrqIngresoAutomovil entidad, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<bool> BorrarAsync(int id, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorTipoAsync(string tipo, DateTime desde, DateTime hasta, Paginacion? paginacion = null, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<IngresoAutomovilResumen>> ListarPorProvinciaAsync(string provincia, DateTime desde, DateTime hasta, Paginacion? paginacion = null, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();
    }
}
