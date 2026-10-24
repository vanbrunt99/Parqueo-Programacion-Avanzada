using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;

namespace Parqueo.Tests.Repositorios;

/// <summary>Conjunto de repositorios de un mismo origen de datos.</summary>
public sealed record RepositoriosParqueo(
    IAutomovilRepositorio Automoviles,
    IParqueoRepositorio Parqueos,
    IIngresoAutomovilRepositorio Ingresos);

/// <summary>
/// Pruebas de consulta escritas UNA sola vez contra las interfaces. Las clases
/// derivadas solo indican el origen (JSON o PostgreSQL); como ambas deben pasar las
/// mismas aserciones con los mismos datos, se comprueba que los dos orígenes
/// devuelven resultados equivalentes.
/// </summary>
public abstract class ConsultasRepositoriosPruebasBase : IDisposable
{
    private static readonly DateTime InicioOctubre = new(2026, 10, 1, 0, 0, 0);
    private static readonly DateTime FinOctubre = new(2026, 10, 31, 23, 59, 59);

    private RepositoriosParqueo? _repositorios;

    /// <summary>Texto para omitir la prueba, o null si el origen está disponible.</summary>
    protected abstract string? MotivoOmision { get; }

    protected abstract RepositoriosParqueo CrearRepositorios();

    protected RepositoriosParqueo Repos
    {
        get
        {
            Skip.If(MotivoOmision is not null, MotivoOmision);
            return _repositorios ??= CrearRepositorios();
        }
    }

    public virtual void Dispose() => GC.SuppressFinalize(this);

    // ------------------------------------------------------------------ Automóviles

    [SkippableFact]
    public async Task Automovil_ObtenerPorId_DevuelveElRegistroConSusIngresos()
    {
        var automovil = await Repos.Automoviles.ObtenerPorIdAsync(1);

        Assert.NotNull(automovil);
        Assert.Equal(("Rojo", 2019, "Toyota", "sedán"), (automovil.Color, automovil.Anio, automovil.Fabricante, automovil.Tipo));
        Assert.Equal([1, 8, 13], automovil.PrqIngresoAutomovils.Select(i => i.Consecutivo).Order());

        // Los ingresos del automóvil traen su parqueo, así que el monto se puede calcular.
        var primero = automovil.PrqIngresoAutomovils.Single(i => i.Consecutivo == 1);
        Assert.Equal("San José", primero.IdParqueoNavigation.NombreProvincia);
        Assert.Equal(3125.00m, primero.MontoTotalAPagar);
    }

    [SkippableFact]
    public async Task Automovil_ObtenerPorIdInexistente_DevuelveNull()
    {
        Assert.Null(await Repos.Automoviles.ObtenerPorIdAsync(999));
    }

    [SkippableTheory]
    [InlineData("rojo", new[] { 1 })]
    [InlineData("RO", new[] { 1, 3 })]      // Rojo, Negro
    [InlineData("zul", new[] { 4 })]
    [InlineData("morado", new int[0])]
    public async Task Automovil_BuscarPorColorAproximado(string color, int[] esperados)
    {
        var resultado = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(Color: color));
        Assert.Equal(esperados, resultado.Select(a => a.Id));
    }

    [SkippableTheory]
    [InlineData("toyo", new[] { 1 })]
    [InlineData("MITSU", new[] { 5 })]
    [InlineData("da", new[] { 2, 6 })]      // Hyundai, Honda
    public async Task Automovil_BuscarPorFabricanteAproximado(string fabricante, int[] esperados)
    {
        var resultado = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(Fabricante: fabricante));
        Assert.Equal(esperados, resultado.Select(a => a.Id));
    }

    [SkippableTheory]
    [InlineData("sedan", new[] { 1, 3 })]   // sin tilde encuentra "sedán"
    [InlineData("SEDÁN", new[] { 1, 3 })]
    [InlineData("4x4", new[] { 2, 5 })]
    [InlineData("4X4", new[] { 2, 5 })]
    [InlineData("mot", new[] { 4, 6 })]
    public async Task Automovil_BuscarPorTipoAproximado(string tipo, int[] esperados)
    {
        var resultado = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(Tipo: tipo));
        Assert.Equal(esperados, resultado.Select(a => a.Id));
    }

    [SkippableTheory]
    [InlineData(2015, 2019, new[] { 1, 3, 5 })] // extremos incluidos
    [InlineData(2020, null, new[] { 2, 4 })]
    [InlineData(null, 2012, new[] { 6 })]
    [InlineData(2016, 2017, new int[0])]
    public async Task Automovil_BuscarPorIntervaloDeAnios(int? desde, int? hasta, int[] esperados)
    {
        var resultado = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(AnioDesde: desde, AnioHasta: hasta));
        Assert.Equal(esperados, resultado.Select(a => a.Id));
    }

    [SkippableFact]
    public async Task Automovil_BuscarConFiltrosCombinados()
    {
        var porTipoYAnio = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(AnioDesde: 2019, Tipo: "4x4"));
        var porColorYTipo = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(Color: "gris", Tipo: "4x4"));
        var cincoFiltros = await Repos.Automoviles.BuscarAsync(
            new FiltroAutomovil(Color: "a", AnioDesde: 2010, AnioHasta: 2025, Fabricante: "a", Tipo: "o"));
        var ninguno = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(Color: "rojo", Tipo: "moto"));

        Assert.Equal([2], porTipoYAnio.Select(a => a.Id));
        Assert.Equal([5], porColorYTipo.Select(a => a.Id));
        Assert.Equal([4], cincoFiltros.Select(a => a.Id)); // Azul, 2023, Yamaha, moto
        Assert.Empty(ninguno);
    }

    [SkippableFact]
    public async Task Automovil_BuscarSinFiltros_DevuelveTodosOrdenadosPorId()
    {
        var resultado = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil());
        Assert.Equal([1, 2, 3, 4, 5, 6], resultado.Select(a => a.Id));
    }

    [SkippableFact]
    public async Task Automovil_BuscarPaginado_DevuelveLaPaginaSolicitada()
    {
        var pagina2 = await Repos.Automoviles.BuscarAsync(new FiltroAutomovil(), new Paginacion(2, 4));
        Assert.Equal([5, 6], pagina2.Select(a => a.Id));
    }

    [SkippableFact]
    public async Task Automovil_IntervaloDeAniosInvertido_LanzaArgumentException()
    {
        _ = Repos;
        await Assert.ThrowsAsync<ArgumentException>(
            () => Repos.Automoviles.BuscarAsync(new FiltroAutomovil(AnioDesde: 2020, AnioHasta: 2010)));
    }

    // ------------------------------------------------------------------ Parqueos

    [SkippableFact]
    public async Task Parqueo_ObtenerPorId_DevuelveElRegistroConSusIngresos()
    {
        var parqueo = await Repos.Parqueos.ObtenerPorIdAsync(3);

        Assert.NotNull(parqueo);
        Assert.Equal(("Cartago", "Parqueo Las Ruinas", 675.50m), (parqueo.NombreProvincia, parqueo.Nombre, parqueo.PrecioPorHora));
        Assert.Equal([4, 7, 10, 13], parqueo.PrqIngresoAutomovils.Select(i => i.Consecutivo).Order());
        var ingreso4 = parqueo.PrqIngresoAutomovils.Single(i => i.Consecutivo == 4);
        Assert.Equal(1857.63m, ingreso4.MontoTotalAPagar);
        Assert.Equal("Mitsubishi", ingreso4.IdAutomovilNavigation.Fabricante); // ThenInclude del automóvil
    }

    [SkippableFact]
    public async Task Parqueo_ObtenerPorIdInexistente_DevuelveNull()
    {
        Assert.Null(await Repos.Parqueos.ObtenerPorIdAsync(999));
    }

    [SkippableTheory]
    [InlineData("san jose", new[] { 1 })]   // sin tilde encuentra "San José"
    [InlineData("HEREDIA", new[] { 2 })]
    [InlineData("tag", new[] { 3 })]
    [InlineData("Limón", new int[0])]
    public async Task Parqueo_BuscarPorProvinciaAproximada(string provincia, int[] esperados)
    {
        var resultado = await Repos.Parqueos.BuscarAsync(new FiltroParqueo(NombreProvincia: provincia));
        Assert.Equal(esperados, resultado.Select(p => p.Id));
    }

    [SkippableTheory]
    [InlineData("mercado", new[] { 2 })]
    [InlineData("parqueo", new[] { 1, 2, 3 })]
    [InlineData("avenida segunda", new[] { 1 })]
    [InlineData("RUINAS", new[] { 3 })]
    [InlineData("parqueo_central", new int[0])]   // símbolos literales: no falla en ningún origen
    [InlineData("(central)", new int[0])]
    public async Task Parqueo_BuscarPorNombreAproximado(string nombre, int[] esperados)
    {
        var resultado = await Repos.Parqueos.BuscarAsync(new FiltroParqueo(Nombre: nombre));
        Assert.Equal(esperados, resultado.Select(p => p.Id));
    }

    [SkippableTheory]
    [InlineData("700", "1250", new[] { 1, 2 })]     // extremos incluidos
    [InlineData("800", null, new[] { 1, 2 })]
    [InlineData(null, "675.50", new[] { 3 })]
    [InlineData("675.51", null, new[] { 1, 2 })]    // comparación decimal exacta
    [InlineData("1250.01", null, new int[0])]
    public async Task Parqueo_BuscarPorRangoDePrecioPorHora(string? minimo, string? maximo, int[] esperados)
    {
        var filtro = new FiltroParqueo(PrecioMinimo: ADecimal(minimo), PrecioMaximo: ADecimal(maximo));
        var resultado = await Repos.Parqueos.BuscarAsync(filtro);
        Assert.Equal(esperados, resultado.Select(p => p.Id));
    }

    [SkippableFact]
    public async Task Parqueo_BuscarConFiltrosCombinados()
    {
        var resultado = await Repos.Parqueos.BuscarAsync(new FiltroParqueo(Nombre: "parqueo", PrecioMaximo: 1000m));
        Assert.Equal([2, 3], resultado.Select(p => p.Id));
    }

    [SkippableFact]
    public async Task Parqueo_RangoDePreciosInvalido_LanzaArgumentException()
    {
        _ = Repos;
        await Assert.ThrowsAsync<ArgumentException>(
            () => Repos.Parqueos.BuscarAsync(new FiltroParqueo(PrecioMinimo: 1000m, PrecioMaximo: 500m)));
        await Assert.ThrowsAsync<ArgumentException>(
            () => Repos.Parqueos.BuscarAsync(new FiltroParqueo(PrecioMinimo: -1m)));
    }

    // ------------------------------------------------------------------ Ingresos

    [SkippableTheory]
    [InlineData(1, "1250.00")]
    [InlineData(2, "800.00")]
    [InlineData(3, "675.50")]
    public async Task ObtenerPrecioPorHoraPorParqueo_DevuelveLaTarifaDelParqueo(int idParqueo, string esperado)
    {
        Assert.Equal(ADecimal(esperado), await Repos.Ingresos.ObtenerPrecioPorHoraPorParqueo(idParqueo));
    }

    [SkippableFact]
    public async Task ObtenerPrecioPorHoraPorParqueo_ParqueoInexistente_DevuelveNull()
    {
        Assert.Null(await Repos.Ingresos.ObtenerPrecioPorHoraPorParqueo(999));
    }

    [SkippableFact]
    public async Task Ingreso_ObtenerPorId_IncluyeAutomovilParqueoYCamposCalculados()
    {
        var ingreso = await Repos.Ingresos.ObtenerPorIdAsync(6);

        Assert.NotNull(ingreso);
        Assert.Equal(2, ingreso.IdAutomovilNavigation.Id);
        Assert.Equal("Hyundai", ingreso.IdAutomovilNavigation.Fabricante);
        Assert.Equal("San José", ingreso.IdParqueoNavigation.NombreProvincia);
        Assert.Equal(80m, ingreso.DuracionEstadiaMinutos);
        Assert.Equal(1666.67m, ingreso.MontoTotalAPagar);
    }

    [SkippableFact]
    public async Task Ingreso_EnCurso_TieneSalidaYCamposCalculadosNulos()
    {
        var ingreso = await Repos.Ingresos.ObtenerPorIdAsync(16);

        Assert.NotNull(ingreso);
        Assert.Null(ingreso.FechaHoraSalida);
        Assert.Null(ingreso.DuracionEstadiaMinutos);
        Assert.Null(ingreso.DuracionEstadiaHoras);
        Assert.Null(ingreso.MontoTotalAPagar);
    }

    [SkippableFact]
    public async Task Ingreso_Listar_DevuelveTodosConAutomovilYParqueo()
    {
        var ingresos = await Repos.Ingresos.ListarAsync();

        Assert.Equal(Enumerable.Range(1, 17), ingresos.Select(i => i.Consecutivo));
        Assert.All(ingresos, i =>
        {
            Assert.Equal(i.IdAutomovil, i.IdAutomovilNavigation.Id);
            Assert.Equal(i.IdParqueo, i.IdParqueoNavigation.Id);
        });
        Assert.Equal(1666.67m, ingresos.Single(i => i.Consecutivo == 6).MontoTotalAPagar);
        Assert.Null(ingresos.Single(i => i.Consecutivo == 17).MontoTotalAPagar);
    }

    [SkippableFact]
    public async Task Ingreso_ListarPaginado()
    {
        var pagina2 = await Repos.Ingresos.ListarAsync(new Paginacion(2, 5));
        Assert.Equal([6, 7, 8, 9, 10], pagina2.Select(i => i.Consecutivo));
    }

    [SkippableFact]
    public async Task Ingreso_ObtenerPorIdInexistente_DevuelveNull()
    {
        Assert.Null(await Repos.Ingresos.ObtenerPorIdAsync(999));
    }

    [SkippableFact]
    public async Task ListarPorTipo_4x4EnOctubre_IncluyeEntradaSalidaYMonto()
    {
        var resultado = await Repos.Ingresos.ListarPorTipoAsync("4x4", InicioOctubre, FinOctubre);

        Assert.Equal([2, 4, 6, 11, 15, 16], resultado.Select(r => r.Consecutivo));
        Assert.All(resultado, r => Assert.Equal("4x4", r.Tipo));
        Assert.Equal([600.00m, 1857.63m, 1666.67m, 1200.00m, 2812.50m, null], resultado.Select(r => r.MontoACancelar));

        var enCurso = resultado.Single(r => r.Consecutivo == 16);
        Assert.Equal(new DateTime(2026, 10, 17, 8, 5, 0), enCurso.FechaHoraEntrada);
        Assert.Null(enCurso.FechaHoraSalida);
        Assert.Null(enCurso.MontoACancelar);
        Assert.Equal(1250.00m, enCurso.PrecioPorHora);
    }

    [SkippableFact]
    public async Task ListarPorTipo_SedanSinTildeEnIntervalo()
    {
        var resultado = await Repos.Ingresos.ListarPorTipoAsync(
            "sedan", new DateTime(2026, 10, 4), new DateTime(2026, 10, 8, 23, 59, 59));

        Assert.Equal([5, 8, 9], resultado.Select(r => r.Consecutivo));
        Assert.All(resultado, r => Assert.Equal("sedán", r.Tipo));
        Assert.Equal([6400.00m, 1200.00m, 5625.00m], resultado.Select(r => r.MontoACancelar));
    }

    [SkippableFact]
    public async Task ListarPorTipo_IncluyeAmbosExtremosDelIntervalo()
    {
        var resultado = await Repos.Ingresos.ListarPorTipoAsync(
            "moto", new DateTime(2026, 10, 2, 8, 10, 0), new DateTime(2026, 10, 9, 18, 15, 0));

        Assert.Equal([3, 7, 10], resultado.Select(r => r.Consecutivo));
    }

    [SkippableFact]
    public async Task ListarPorTipo_RelacionesCorrectas()
    {
        var resultado = await Repos.Ingresos.ListarPorTipoAsync("moto", InicioOctubre, FinOctubre);

        Assert.Equal([3, 7, 10, 12, 14], resultado.Select(r => r.Consecutivo));
        var registro = resultado.Single(r => r.Consecutivo == 7);
        Assert.Equal((6, "Verde", 2012, "Honda"), (registro.IdAutomovil, registro.Color, registro.Anio, registro.Fabricante));
        Assert.Equal((3, "Parqueo Las Ruinas", "Cartago", 675.50m),
            (registro.IdParqueo, registro.NombreParqueo, registro.NombreProvincia, registro.PrecioPorHora));
        Assert.Equal(new DateTime(2026, 10, 6, 7, 50, 0), registro.FechaHoraSalida);
        Assert.Equal(562.92m, registro.MontoACancelar);
    }

    [SkippableFact]
    public async Task ListarPorProvincia_SanJoseEnOctubre()
    {
        var resultado = await Repos.Ingresos.ListarPorProvinciaAsync("san jose", InicioOctubre, FinOctubre);

        Assert.Equal([1, 3, 6, 9, 12, 15, 16], resultado.Select(r => r.Consecutivo));
        Assert.All(resultado, r => Assert.Equal("San José", r.NombreProvincia));
        Assert.All(resultado, r => Assert.Equal(1250.00m, r.PrecioPorHora));
        Assert.Null(resultado.Last().MontoACancelar);
        Assert.Equal(14479.17m, resultado.Sum(r => r.MontoACancelar ?? 0m));
    }

    [SkippableFact]
    public async Task ListarPorProvincia_HerediaConEstadiaEnCurso()
    {
        var resultado = await Repos.Ingresos.ListarPorProvinciaAsync(
            "HEREDIA", new DateTime(2026, 10, 15), new DateTime(2026, 10, 17, 23, 59, 59));

        Assert.Equal([14, 17], resultado.Select(r => r.Consecutivo));
        Assert.Equal([266.67m, null], resultado.Select(r => r.MontoACancelar));
        Assert.Equal([20m, null], resultado.Select(r => r.DuracionEstadiaMinutos));
    }

    [SkippableFact]
    public async Task ListarPorProvincia_DiferentesTarifasPorParqueo()
    {
        var cartago = await Repos.Ingresos.ListarPorProvinciaAsync("cartago", InicioOctubre, FinOctubre);
        var heredia = await Repos.Ingresos.ListarPorProvinciaAsync("heredia", InicioOctubre, FinOctubre);

        // 45 minutos en cada provincia: el monto cambia por la tarifa del parqueo.
        Assert.Equal(506.63m, cartago.Single(r => r.Consecutivo == 10).MontoACancelar);
        Assert.Equal(600.00m, heredia.Single(r => r.Consecutivo == 2).MontoACancelar);
    }

    [SkippableFact]
    public async Task ListarPorProvincia_SinCoincidencias_DevuelveListaVacia()
    {
        Assert.Empty(await Repos.Ingresos.ListarPorProvinciaAsync("Limón", InicioOctubre, FinOctubre));
        Assert.Empty(await Repos.Ingresos.ListarPorProvinciaAsync("san jose", new DateTime(2026, 11, 1), new DateTime(2026, 11, 30)));
    }

    [SkippableFact]
    public async Task ListarPorProvincia_Paginado()
    {
        var pagina2 = await Repos.Ingresos.ListarPorProvinciaAsync("san jose", InicioOctubre, FinOctubre, new Paginacion(2, 3));
        Assert.Equal([9, 12, 15], pagina2.Select(r => r.Consecutivo));
    }

    [SkippableFact]
    public async Task ListadosDeIngresos_ParametrosInvalidos_LanzanArgumentException()
    {
        _ = Repos;
        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Ingresos.ListarPorTipoAsync("4x4", FinOctubre, InicioOctubre));
        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Ingresos.ListarPorTipoAsync("  ", InicioOctubre, FinOctubre));
        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Ingresos.ListarPorProvinciaAsync("", InicioOctubre, FinOctubre));
    }

    private static decimal? ADecimal(string? valor) =>
        valor is null ? null : decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
}
