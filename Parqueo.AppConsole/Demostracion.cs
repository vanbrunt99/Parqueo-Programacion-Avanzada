using System.Globalization;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.AppConsole;

/// <summary>
/// Ejecuta las consultas del enunciado contra las interfaces de repositorio.
/// La clase no sabe si los datos vienen de JSON o de PostgreSQL: lo decide la
/// inyección de dependencias en Program.cs.
/// </summary>
public sealed class Demostracion
{
    private static readonly CultureInfo Formato = CultureInfo.InvariantCulture;
    private static readonly DateTime Desde = new(2026, 10, 1, 0, 0, 0);
    private static readonly DateTime Hasta = new(2026, 10, 31, 23, 59, 59);

    private readonly IAutomovilRepositorio _automoviles;
    private readonly IParqueoRepositorio _parqueos;
    private readonly IIngresoAutomovilRepositorio _ingresos;

    public Demostracion(IAutomovilRepositorio automoviles, IParqueoRepositorio parqueos, IIngresoAutomovilRepositorio ingresos)
    {
        _automoviles = automoviles;
        _parqueos = parqueos;
        _ingresos = ingresos;
    }

    public async Task EjecutarConsultasAsync()
    {
        Titulo("Consulta por llave primaria");
        var automovil = await _automoviles.ObtenerPorIdAsync(1);
        Console.WriteLine(automovil is null ? "Automóvil 1: no existe" : $"Automóvil 1: {Describir(automovil)}; ingresos: {automovil.PrqIngresoAutomovils.Count}");
        var parqueo = await _parqueos.ObtenerPorIdAsync(3);
        Console.WriteLine(parqueo is null ? "Parqueo 3: no existe" : $"Parqueo 3: {Describir(parqueo)}; ingresos: {parqueo.PrqIngresoAutomovils.Count}");
        var ingreso = await _ingresos.ObtenerPorIdAsync(6);
        Console.WriteLine(ingreso is null ? "Ingreso 6: no existe" :
            $"Ingreso 6: {ingreso.FechaHoraEntrada:yyyy-MM-dd HH:mm} → {Fecha(ingreso.FechaHoraSalida)}; " +
            $"minutos {Numero(ingreso.DuracionEstadiaMinutos)}, horas {Numero(ingreso.DuracionEstadiaHoras)}, monto {Monto(ingreso.MontoTotalAPagar)}");
        Console.WriteLine($"Automóvil 999: {(await _automoviles.ObtenerPorIdAsync(999) is null ? "no existe (null)" : "existe")}");

        Titulo("Automóviles por color, intervalo de año, fabricante y tipo (aproximados)");
        await MostrarAutomovilesAsync("Color ~ 'ro'", new FiltroAutomovil(Color: "ro"));
        await MostrarAutomovilesAsync("Años 2015–2019", new FiltroAutomovil(AnioDesde: 2015, AnioHasta: 2019));
        await MostrarAutomovilesAsync("Fabricante ~ 'da'", new FiltroAutomovil(Fabricante: "da"));
        await MostrarAutomovilesAsync("Tipo ~ 'sedan'", new FiltroAutomovil(Tipo: "sedan"));
        await MostrarAutomovilesAsync("Tipo ~ '4x4' y año ≥ 2019", new FiltroAutomovil(AnioDesde: 2019, Tipo: "4x4"));

        Titulo("Parqueos por provincia, nombre y rango de precio por hora");
        await MostrarParqueosAsync("Provincia ~ 'san jose'", new FiltroParqueo(NombreProvincia: "san jose"));
        await MostrarParqueosAsync("Nombre ~ 'mercado'", new FiltroParqueo(Nombre: "mercado"));
        await MostrarParqueosAsync("Precio entre 700 y 1250", new FiltroParqueo(PrecioMinimo: 700m, PrecioMaximo: 1250m));

        Titulo("ObtenerPrecioPorHoraPorParqueo");
        foreach (int idParqueo in new[] { 1, 2, 3, 99 })
        {
            decimal? precio = await _ingresos.ObtenerPrecioPorHoraPorParqueo(idParqueo);
            Console.WriteLine($"Parqueo {idParqueo}: {(precio is null ? "null (no existe)" : Monto(precio))}");
        }

        Titulo($"Automóviles tipo '4x4' que ingresaron entre {Desde:yyyy-MM-dd HH:mm} y {Hasta:yyyy-MM-dd HH:mm}");
        MostrarIngresos(await _ingresos.ListarPorTipoAsync("4x4", Desde, Hasta));

        Titulo($"Automóviles en parqueos de 'Heredia' entre {Desde:yyyy-MM-dd HH:mm} y {Hasta:yyyy-MM-dd HH:mm}");
        MostrarIngresos(await _ingresos.ListarPorProvinciaAsync("heredia", Desde, Hasta));
    }

    /// <summary>
    /// Inserta, actualiza y borra un registro en cada tabla. Al final borra lo que
    /// insertó, de modo que los datos quedan como estaban (salvo el avance de las
    /// secuencias de identidad en PostgreSQL).
    /// </summary>
    public async Task EjecutarCrudAsync()
    {
        Titulo("CRUD: insertar");
        var automovil = await _automoviles.InsertarAsync(
            new PrqAutomovil { Color = "Celeste", Anio = 2024, Fabricante = "Suzuki", Tipo = "moto" });
        Console.WriteLine($"Automóvil insertado: {Describir(automovil)}");

        var parqueo = await _parqueos.InsertarAsync(
            new PrqParqueo { NombreProvincia = "Limón", Nombre = "Parqueo del Puerto", PrecioPorHora = 950.25m });
        Console.WriteLine($"Parqueo insertado:   {Describir(parqueo)}");

        var ingreso = await _ingresos.InsertarAsync(new PrqIngresoAutomovil
        {
            IdParqueo = parqueo.Id,
            IdAutomovil = automovil.Id,
            FechaHoraEntrada = new DateTime(2026, 10, 18, 9, 0, 0),
        });
        var leido = await _ingresos.ObtenerPorIdAsync(ingreso.Consecutivo);
        Console.WriteLine($"Ingreso insertado:   #{leido!.Consecutivo}, entrada {leido.FechaHoraEntrada:yyyy-MM-dd HH:mm}, salida {Fecha(leido.FechaHoraSalida)}, monto {Monto(leido.MontoTotalAPagar)}");

        Titulo("CRUD: actualizar");
        automovil.Color = "Celeste metálico";
        Console.WriteLine($"Automóvil actualizado: {await _automoviles.ActualizarAsync(automovil)} → {Describir((await _automoviles.ObtenerPorIdAsync(automovil.Id))!)}");
        leido.FechaHoraSalida = new DateTime(2026, 10, 18, 10, 40, 0);
        Console.WriteLine($"Ingreso actualizado (registro de salida): {await _ingresos.ActualizarAsync(leido)}");
        leido = await _ingresos.ObtenerPorIdAsync(ingreso.Consecutivo);
        Console.WriteLine($"   minutos {Numero(leido!.DuracionEstadiaMinutos)}, horas {Numero(leido.DuracionEstadiaHoras)}, monto {Monto(leido.MontoTotalAPagar)}");
        Console.WriteLine($"Actualizar automóvil inexistente (999): {await _automoviles.ActualizarAsync(new PrqAutomovil { Id = 999, Color = "X", Anio = 2020, Fabricante = "X", Tipo = "moto" })}");

        Titulo("CRUD: validaciones e integridad referencial");
        await MostrarErrorAsync("Insertar automóvil tipo 'camión'",
            () => _automoviles.InsertarAsync(new PrqAutomovil { Color = "Rojo", Anio = 2020, Fabricante = "Isuzu", Tipo = "camión" }));
        await MostrarErrorAsync("Insertar ingreso con parqueo 999",
            () => _ingresos.InsertarAsync(new PrqIngresoAutomovil { IdParqueo = 999, IdAutomovil = automovil.Id, FechaHoraEntrada = new DateTime(2026, 10, 18, 9, 0, 0) }));
        await MostrarErrorAsync("Borrar automóvil con ingresos",
            () => _automoviles.BorrarAsync(automovil.Id));

        Titulo("CRUD: borrar");
        Console.WriteLine($"Ingreso #{ingreso.Consecutivo} borrado: {await _ingresos.BorrarAsync(ingreso.Consecutivo)}");
        Console.WriteLine($"Automóvil #{automovil.Id} borrado: {await _automoviles.BorrarAsync(automovil.Id)}");
        Console.WriteLine($"Parqueo #{parqueo.Id} borrado: {await _parqueos.BorrarAsync(parqueo.Id)}");
        Console.WriteLine($"Borrar parqueo inexistente (999): {await _parqueos.BorrarAsync(999)}");
        Console.WriteLine($"Consulta posterior del automóvil #{automovil.Id}: {(await _automoviles.ObtenerPorIdAsync(automovil.Id) is null ? "null (ya no existe)" : "existe")}");
    }

    private static async Task MostrarErrorAsync(string descripcion, Func<Task> operacion)
    {
        try
        {
            await operacion();
            Console.WriteLine($"{descripcion}: se aceptó (inesperado)");
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"{descripcion}: rechazado ({error.GetType().Name}) — {error.Message}");
        }
    }

    private async Task MostrarAutomovilesAsync(string descripcion, FiltroAutomovil filtro)
    {
        var resultado = await _automoviles.BuscarAsync(filtro);
        Console.WriteLine($"{descripcion}: {resultado.Count} resultado(s)");
        foreach (var automovil in resultado)
        {
            Console.WriteLine($"   {Describir(automovil)}");
        }
    }

    private async Task MostrarParqueosAsync(string descripcion, FiltroParqueo filtro)
    {
        var resultado = await _parqueos.BuscarAsync(filtro);
        Console.WriteLine($"{descripcion}: {resultado.Count} resultado(s)");
        foreach (var parqueo in resultado)
        {
            Console.WriteLine($"   {Describir(parqueo)}");
        }
    }

    private static void MostrarIngresos(IReadOnlyList<IngresoAutomovilResumen> registros)
    {
        Console.WriteLine("Consec. | Automóvil                          | Provincia | Entrada          | Salida           | Precio/h | Monto");
        foreach (var r in registros)
        {
            string auto = $"#{r.IdAutomovil} {r.Fabricante} {r.Color} {r.Anio} ({r.Tipo})";
            Console.WriteLine(
                $"{r.Consecutivo,7} | {auto,-34} | {r.NombreProvincia,-9} | {r.FechaHoraEntrada:yyyy-MM-dd HH:mm} | " +
                $"{Fecha(r.FechaHoraSalida),-16} | {Monto(r.PrecioPorHora),8} | {Monto(r.MontoACancelar)}");
        }

        Console.WriteLine($"Total de registros: {registros.Count}; total facturado: {Monto(registros.Sum(r => r.MontoACancelar ?? 0m))}");
    }

    private static string Describir(PrqAutomovil a) => $"#{a.Id} {a.Color}, {a.Anio}, {a.Fabricante}, {a.Tipo}";

    private static string Describir(PrqParqueo p) => $"#{p.Id} {p.Nombre} ({p.NombreProvincia}) {Monto(p.PrecioPorHora)}/h";

    private static string Fecha(DateTime? fecha) => fecha?.ToString("yyyy-MM-dd HH:mm", Formato) ?? "(en curso)";

    private static string Monto(decimal? valor) => valor?.ToString("0.00", Formato) ?? "null";

    private static string Numero(decimal? valor) => valor?.ToString("0.####", Formato) ?? "null";

    private static void Titulo(string texto)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {texto} ===");
    }
}
