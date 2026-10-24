using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Validacion;
using Parqueo.Model.Entidades;

namespace Parqueo.Tests.Repositorios;

/// <summary>
/// Pruebas de insertar, actualizar y borrar escritas una sola vez contra las
/// interfaces; se ejecutan contra JSON y contra PostgreSQL. Después de cada cambio
/// se vuelve a leer con repositorios "de verificación" (nuevas instancias que leen
/// del archivo o de la base) para comprobar que el cambio quedó guardado.
/// </summary>
public abstract class CrudRepositoriosPruebasBase : IDisposable
{
    private RepositoriosParqueo? _repositorios;

    protected abstract string? MotivoOmision { get; }

    protected abstract RepositoriosParqueo CrearRepositorios();

    /// <summary>Repositorios nuevos que leen otra vez desde el almacenamiento.</summary>
    protected abstract RepositoriosParqueo CrearRepositoriosDeVerificacion();

    protected RepositoriosParqueo Repos
    {
        get
        {
            Skip.If(MotivoOmision is not null, MotivoOmision);
            return _repositorios ??= CrearRepositorios();
        }
    }

    protected RepositoriosParqueo Verificacion
    {
        get
        {
            Skip.If(MotivoOmision is not null, MotivoOmision);
            return CrearRepositoriosDeVerificacion();
        }
    }

    public virtual void Dispose() => GC.SuppressFinalize(this);

    // ------------------------------------------------------------------ Automóviles

    [SkippableFact]
    public async Task Automovil_Insertar_AsignaLlaveYPersiste()
    {
        var nuevo = new PrqAutomovil { Color = "Café", Anio = 2022, Fabricante = "Kia", Tipo = "sedán" };

        var guardado = await Repos.Automoviles.InsertarAsync(nuevo);

        Assert.True(guardado.Id > 6);
        Assert.Equal(guardado.Id, nuevo.Id);
        var leido = await Verificacion.Automoviles.ObtenerPorIdAsync(guardado.Id);
        Assert.NotNull(leido);
        Assert.Equal(("Café", 2022, "Kia", "sedán"), (leido.Color, leido.Anio, leido.Fabricante, leido.Tipo));
        Assert.Equal(7, (await Verificacion.Automoviles.BuscarAsync(new FiltroAutomovil())).Count);
    }

    [SkippableFact]
    public async Task Automovil_InsertarAnioPermitidoPorLaBase_SeAcepta()
    {
        // El validador usa el mismo rango que la restricción CHECK (1886-2100).
        var guardado = await Repos.Automoviles.InsertarAsync(
            new PrqAutomovil { Color = "Gris", Anio = 2030, Fabricante = "Tesla", Tipo = "sedán" });

        Assert.Equal(2030, (await Verificacion.Automoviles.ObtenerPorIdAsync(guardado.Id))!.Anio);
    }

    [SkippableFact]
    public async Task Automovil_InsertarConLlaveExistente_LanzaReglaNegocioException()
    {
        var duplicado = new PrqAutomovil { Id = 1, Color = "Café", Anio = 2022, Fabricante = "Kia", Tipo = "sedán" };

        await Assert.ThrowsAsync<ReglaNegocioException>(() => Repos.Automoviles.InsertarAsync(duplicado));
        Assert.Equal("Rojo", (await Verificacion.Automoviles.ObtenerPorIdAsync(1))!.Color);
    }

    [SkippableFact]
    public async Task Automovil_InsertarConLlaveNoAsignadaPorElOrigen_LanzaArgumentException()
    {
        var conLlave = new PrqAutomovil { Id = 500, Color = "Café", Anio = 2022, Fabricante = "Kia", Tipo = "sedán" };

        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Automoviles.InsertarAsync(conLlave));
        Assert.Null(await Verificacion.Automoviles.ObtenerPorIdAsync(500));
    }

    [SkippableTheory]
    [InlineData("Café", 2022, "Kia", "camión")]
    [InlineData("Café", 2022, "Kia", "Sedan")]
    [InlineData("", 2022, "Kia", "sedán")]
    [InlineData("Café", 2022, "   ", "moto")]
    [InlineData("Café", 1800, "Kia", "4x4")]
    [InlineData("Café", 2999, "Kia", "4x4")]
    public async Task Automovil_InsertarDatosInvalidos_LanzaArgumentExceptionYNoGuarda(string color, int anio, string fabricante, string tipo)
    {
        var invalido = new PrqAutomovil { Color = color, Anio = anio, Fabricante = fabricante, Tipo = tipo };

        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Automoviles.InsertarAsync(invalido));
        Assert.Equal(6, (await Verificacion.Automoviles.BuscarAsync(new FiltroAutomovil())).Count);
    }

    [SkippableFact]
    public async Task Automovil_Actualizar_ModificaYPersiste()
    {
        var automovil = await Repos.Automoviles.ObtenerPorIdAsync(3);
        automovil!.Color = "Plateado";
        automovil.Anio = 2016;

        Assert.True(await Repos.Automoviles.ActualizarAsync(automovil));

        var leido = await Verificacion.Automoviles.ObtenerPorIdAsync(3);
        Assert.Equal(("Plateado", 2016, "Nissan", "sedán"), (leido!.Color, leido.Anio, leido.Fabricante, leido.Tipo));
        Assert.Equal(3, leido.PrqIngresoAutomovils.Count); // los ingresos no se ven afectados
    }

    [SkippableFact]
    public async Task Automovil_ActualizarInexistente_DevuelveFalse()
    {
        var inexistente = new PrqAutomovil { Id = 999, Color = "Café", Anio = 2022, Fabricante = "Kia", Tipo = "sedán" };

        Assert.False(await Repos.Automoviles.ActualizarAsync(inexistente));
        Assert.Null(await Verificacion.Automoviles.ObtenerPorIdAsync(999));
    }

    [SkippableFact]
    public async Task Automovil_ActualizarConDatosInvalidos_NoModifica()
    {
        var automovil = await Repos.Automoviles.ObtenerPorIdAsync(3);
        automovil!.Tipo = "bus";

        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Automoviles.ActualizarAsync(automovil));
        Assert.Equal("sedán", (await Verificacion.Automoviles.ObtenerPorIdAsync(3))!.Tipo);
    }

    [SkippableFact]
    public async Task Automovil_BorrarSinIngresos_EliminaYPersiste()
    {
        var nuevo = await Repos.Automoviles.InsertarAsync(
            new PrqAutomovil { Color = "Celeste", Anio = 2020, Fabricante = "Suzuki", Tipo = "moto" });

        Assert.True(await Repos.Automoviles.BorrarAsync(nuevo.Id));

        Assert.Null(await Verificacion.Automoviles.ObtenerPorIdAsync(nuevo.Id));
        Assert.Equal(6, (await Verificacion.Automoviles.BuscarAsync(new FiltroAutomovil())).Count);
    }

    [SkippableFact]
    public async Task Automovil_BorrarInexistente_DevuelveFalse()
    {
        Assert.False(await Repos.Automoviles.BorrarAsync(999));
    }

    [SkippableFact]
    public async Task Automovil_BorrarConIngresos_LanzaReglaNegocioExceptionYNoBorra()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Repos.Automoviles.BorrarAsync(1));
        Assert.NotNull(await Verificacion.Automoviles.ObtenerPorIdAsync(1));
    }

    // ------------------------------------------------------------------ Parqueos

    [SkippableFact]
    public async Task Parqueo_Insertar_AsignaLlaveYPersiste()
    {
        var guardado = await Repos.Parqueos.InsertarAsync(
            new PrqParqueo { NombreProvincia = "Limón", Nombre = "Parqueo del Puerto", PrecioPorHora = 950.25m });

        Assert.True(guardado.Id > 3);
        var leido = await Verificacion.Parqueos.ObtenerPorIdAsync(guardado.Id);
        Assert.Equal(("Limón", "Parqueo del Puerto", 950.25m), (leido!.NombreProvincia, leido.Nombre, leido.PrecioPorHora));
        Assert.Equal(950.25m, await Verificacion.Ingresos.ObtenerPrecioPorHoraPorParqueo(guardado.Id));
    }

    [SkippableTheory]
    [InlineData("Panamá", "Parqueo X", "500")]
    [InlineData("Limón", "", "500")]
    [InlineData("Limón", "Parqueo X", "0")]
    [InlineData("Limón", "Parqueo X", "-5")]
    [InlineData("Limón", "Parqueo X", "100.125")]
    public async Task Parqueo_InsertarDatosInvalidos_LanzaArgumentExceptionYNoGuarda(string provincia, string nombre, string precio)
    {
        var invalido = new PrqParqueo
        {
            NombreProvincia = provincia,
            Nombre = nombre,
            PrecioPorHora = decimal.Parse(precio, System.Globalization.CultureInfo.InvariantCulture),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Parqueos.InsertarAsync(invalido));
        Assert.Equal(3, (await Verificacion.Parqueos.BuscarAsync(new FiltroParqueo())).Count);
    }

    [SkippableFact]
    public async Task Parqueo_ActualizarPrecio_CambiaElMontoDeLosListados()
    {
        var parqueo = await Repos.Parqueos.ObtenerPorIdAsync(2);
        parqueo!.PrecioPorHora = 1000.00m;

        Assert.True(await Repos.Parqueos.ActualizarAsync(parqueo));

        Assert.Equal(1000.00m, await Verificacion.Ingresos.ObtenerPrecioPorHoraPorParqueo(2));
        var listado = await Verificacion.Ingresos.ListarPorProvinciaAsync(
            "heredia", new DateTime(2026, 10, 1), new DateTime(2026, 10, 1, 23, 59, 59));
        Assert.Equal(750.00m, listado.Single(r => r.Consecutivo == 2).MontoACancelar); // 45 min a 1000/h
    }

    [SkippableFact]
    public async Task Parqueo_ActualizarYBorrarInexistente_DevuelvenFalse()
    {
        var inexistente = new PrqParqueo { Id = 999, NombreProvincia = "Limón", Nombre = "X", PrecioPorHora = 100m };

        Assert.False(await Repos.Parqueos.ActualizarAsync(inexistente));
        Assert.False(await Repos.Parqueos.BorrarAsync(999));
    }

    [SkippableFact]
    public async Task Parqueo_Borrar_RespetaLaIntegridadReferencial()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Repos.Parqueos.BorrarAsync(1));
        Assert.NotNull(await Verificacion.Parqueos.ObtenerPorIdAsync(1));

        var nuevo = await Repos.Parqueos.InsertarAsync(
            new PrqParqueo { NombreProvincia = "Guanacaste", Nombre = "Parqueo Liberia", PrecioPorHora = 700m });
        Assert.True(await Repos.Parqueos.BorrarAsync(nuevo.Id));
        Assert.Null(await Verificacion.Parqueos.ObtenerPorIdAsync(nuevo.Id));
    }

    // ------------------------------------------------------------------ Ingresos

    [SkippableFact]
    public async Task Ingreso_InsertarEnCurso_PersisteConCamposCalculadosNulos()
    {
        var guardado = await Repos.Ingresos.InsertarAsync(new PrqIngresoAutomovil
        {
            IdParqueo = 3,
            IdAutomovil = 6,
            FechaHoraEntrada = new DateTime(2026, 10, 17, 10, 0, 0),
        });

        Assert.True(guardado.Consecutivo > 17);
        var leido = await Verificacion.Ingresos.ObtenerPorIdAsync(guardado.Consecutivo);
        Assert.NotNull(leido);
        Assert.Null(leido.FechaHoraSalida);
        Assert.Null(leido.MontoTotalAPagar);
        Assert.Equal(("Honda", "Cartago"), (leido.IdAutomovilNavigation.Fabricante, leido.IdParqueoNavigation.NombreProvincia));
    }

    [SkippableFact]
    public async Task Ingreso_InsertarFinalizado_DevuelveElMontoCalculado()
    {
        var guardado = await Repos.Ingresos.InsertarAsync(new PrqIngresoAutomovil
        {
            IdParqueo = 1,
            IdAutomovil = 1,
            FechaHoraEntrada = new DateTime(2026, 10, 18, 8, 0, 0),
            FechaHoraSalida = new DateTime(2026, 10, 18, 9, 0, 0),
        });

        // El objeto devuelto trae su parqueo, por lo que el monto no queda en null.
        Assert.Equal((60m, 1m, 1250.00m), (guardado.DuracionEstadiaMinutos, guardado.DuracionEstadiaHoras, guardado.MontoTotalAPagar));
        Assert.Equal("Toyota", guardado.IdAutomovilNavigation.Fabricante);
    }

    [SkippableFact]
    public async Task Ingreso_RegistrarSalida_CalculaDuracionYMonto()
    {
        var ingreso = await Repos.Ingresos.ObtenerPorIdAsync(16); // San José, entrada 08:05, en curso
        ingreso!.FechaHoraSalida = new DateTime(2026, 10, 17, 10, 5, 0);

        Assert.True(await Repos.Ingresos.ActualizarAsync(ingreso));

        var leido = await Verificacion.Ingresos.ObtenerPorIdAsync(16);
        Assert.Equal((120m, 2m, 2500.00m), (leido!.DuracionEstadiaMinutos, leido.DuracionEstadiaHoras, leido.MontoTotalAPagar));
        var listado = await Verificacion.Ingresos.ListarPorTipoAsync("4x4", new DateTime(2026, 10, 17), new DateTime(2026, 10, 17, 23, 59, 59));
        Assert.Equal(2500.00m, listado.Single(r => r.Consecutivo == 16).MontoACancelar);
    }

    [SkippableFact]
    public async Task Ingreso_InsertarConReferenciasInexistentes_LanzaReglaNegocioException()
    {
        var sinParqueo = new PrqIngresoAutomovil { IdParqueo = 99, IdAutomovil = 1, FechaHoraEntrada = new DateTime(2026, 10, 18, 8, 0, 0) };
        var sinAutomovil = new PrqIngresoAutomovil { IdParqueo = 1, IdAutomovil = 99, FechaHoraEntrada = new DateTime(2026, 10, 18, 8, 0, 0) };

        await Assert.ThrowsAsync<ReglaNegocioException>(() => Repos.Ingresos.InsertarAsync(sinParqueo));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Repos.Ingresos.InsertarAsync(sinAutomovil));
        var todos = await Verificacion.Ingresos.ListarPorProvinciaAsync("a", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        Assert.Equal(17, todos.Count);
    }

    [SkippableTheory]
    [InlineData("2026-10-18T10:00:00", "2026-10-18T10:00:00")]
    [InlineData("2026-10-18T10:00:00", "2026-10-18T09:00:00")]
    public async Task Ingreso_SalidaNoPosteriorALaEntrada_LanzaArgumentException(string entrada, string salida)
    {
        var invalido = new PrqIngresoAutomovil
        {
            IdParqueo = 1,
            IdAutomovil = 1,
            FechaHoraEntrada = DateTime.Parse(entrada),
            FechaHoraSalida = DateTime.Parse(salida),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Ingresos.InsertarAsync(invalido));
    }

    [SkippableFact]
    public async Task Ingreso_FechaEnUtc_LanzaArgumentException()
    {
        var enUtc = new PrqIngresoAutomovil
        {
            IdParqueo = 1,
            IdAutomovil = 1,
            FechaHoraEntrada = new DateTime(2026, 10, 18, 16, 0, 0, DateTimeKind.Utc),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => Repos.Ingresos.InsertarAsync(enUtc));
    }

    [SkippableFact]
    public async Task Ingreso_ActualizarHaciaParqueoInexistente_NoModifica()
    {
        var ingreso = await Repos.Ingresos.ObtenerPorIdAsync(1);
        ingreso!.IdParqueo = 99;

        await Assert.ThrowsAsync<ReglaNegocioException>(() => Repos.Ingresos.ActualizarAsync(ingreso));
        Assert.Equal(1, (await Verificacion.Ingresos.ObtenerPorIdAsync(1))!.IdParqueo);
    }

    [SkippableFact]
    public async Task Ingreso_BorrarIngresosPermiteLuegoBorrarElAutomovil()
    {
        // El automóvil 6 tiene los ingresos 7 y 12.
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Repos.Automoviles.BorrarAsync(6));

        Assert.True(await Repos.Ingresos.BorrarAsync(7));
        Assert.True(await Repos.Ingresos.BorrarAsync(12));
        Assert.True(await Repos.Automoviles.BorrarAsync(6));

        Assert.Null(await Verificacion.Ingresos.ObtenerPorIdAsync(7));
        Assert.Null(await Verificacion.Automoviles.ObtenerPorIdAsync(6));

        // "a" aparece en San José, Heredia y Cartago: el listado abarca todos los ingresos restantes.
        var restantes = await Verificacion.Ingresos.ListarPorProvinciaAsync("a", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        Assert.Equal(15, restantes.Count);
        Assert.DoesNotContain(restantes, r => r.IdAutomovil == 6);
    }

    [SkippableFact]
    public async Task Ingreso_ActualizarYBorrarInexistente_DevuelvenFalse()
    {
        var inexistente = new PrqIngresoAutomovil
        {
            Consecutivo = 999,
            IdParqueo = 1,
            IdAutomovil = 1,
            FechaHoraEntrada = new DateTime(2026, 10, 18, 8, 0, 0),
        };

        Assert.False(await Repos.Ingresos.ActualizarAsync(inexistente));
        Assert.False(await Repos.Ingresos.BorrarAsync(999));
    }
}
