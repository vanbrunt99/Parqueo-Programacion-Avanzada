using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Consultas;

/// <summary>
/// Convierte ingresos en <see cref="IngresoAutomovilResumen"/> calculando el monto a
/// cancelar con la función <c>ObtenerPrecioPorHoraPorParqueo</c> del repositorio,
/// como lo exige el enunciado. Lo usan las implementaciones EF y JSON.
/// </summary>
public sealed class GeneradorResumenIngresos
{
    public async Task<IReadOnlyList<IngresoAutomovilResumen>> CrearAsync(
        IReadOnlyList<PrqIngresoAutomovil> ingresos,
        IIngresoAutomovilRepositorio repositorio,
        CancellationToken cancelacion = default)
    {
        // Se llama a la función una vez por parqueo distinto (no una vez por fila)
        // para no repetir consultas iguales.
        var preciosPorParqueo = new Dictionary<int, decimal?>();
        foreach (int idParqueo in ingresos.Select(i => i.IdParqueo).Distinct())
        {
            preciosPorParqueo[idParqueo] = await repositorio.ObtenerPrecioPorHoraPorParqueo(idParqueo, cancelacion);
        }

        return ingresos
            .Select(ingreso =>
            {
                decimal? precioPorHora = preciosPorParqueo[ingreso.IdParqueo];
                return new IngresoAutomovilResumen(
                    ingreso.Consecutivo,
                    ingreso.IdAutomovil,
                    ingreso.IdAutomovilNavigation.Color,
                    ingreso.IdAutomovilNavigation.Anio,
                    ingreso.IdAutomovilNavigation.Fabricante,
                    ingreso.IdAutomovilNavigation.Tipo,
                    ingreso.IdParqueo,
                    ingreso.IdParqueoNavigation.Nombre,
                    ingreso.IdParqueoNavigation.NombreProvincia,
                    ingreso.FechaHoraEntrada,
                    ingreso.FechaHoraSalida,
                    ingreso.DuracionEstadiaMinutos,
                    ingreso.DuracionEstadiaHoras,
                    precioPorHora,
                    ingreso.CalcularMontoTotal(precioPorHora));
            })
            .ToList();
    }
}
