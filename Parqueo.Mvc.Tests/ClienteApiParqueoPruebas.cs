using System.Net;
using Parqueo.Mvc.Servicios;
using Parqueo.Mvc.Tests.Infraestructura;

namespace Parqueo.Mvc.Tests;

/// <summary>
/// Pruebas del cliente HTTP tipado contra el API real en memoria: comprueban que el contrato
/// JSON que el MVC espera es el que el API devuelve (nombres, navegación, fechas, nulos,
/// total de registros y origen de datos).
/// </summary>
public abstract class ClienteApiParqueoPruebasBase : MvcPruebasBase
{
    protected ClienteApiParqueoPruebasBase(OrigenPruebas origen)
        : base(origen)
    {
    }

    [SkippableFact]
    public async Task ListarAutomoviles_FiltroPorAnios_DevuelveLaPaginaElTotalYElOrigen()
    {
        var respuesta = await ClienteApi.ListarAutomovilesAsync(2015, 2019, null, 1, 10, default);

        Assert.True(respuesta.Exito);
        Assert.Equal(Origen.Nombre, respuesta.OrigenDatos);
        Assert.Equal([1, 3, 5], respuesta.Datos!.Registros.Select(a => a.Id));
        Assert.Equal(3, respuesta.Datos.Total);
    }

    [SkippableFact]
    public async Task ListarAutomoviles_Paginado_CalculaLasPaginas()
    {
        var respuesta = await ClienteApi.ListarAutomovilesAsync(null, null, null, 2, 4, default);

        Assert.Equal([5, 6], respuesta.Datos!.Registros.Select(a => a.Id));
        Assert.Equal((6, 2, 5, 6), (respuesta.Datos.Total, respuesta.Datos.TotalPaginas, respuesta.Datos.Desde, respuesta.Datos.Hasta));
    }

    [SkippableTheory]
    [InlineData("sedán", new[] { 1, 3 })]
    [InlineData("4x4", new[] { 2, 5 })]
    [InlineData("moto", new[] { 4, 6 })]
    public async Task ListarTodosLosAutomoviles_PorTipo(string tipo, int[] esperados)
    {
        var respuesta = await ClienteApi.ListarTodosLosAutomovilesAsync(tipo, default);

        Assert.Equal(esperados, respuesta.Datos!.Select(a => a.Id));
        Assert.All(respuesta.Datos!, a => Assert.Equal(tipo, a.Tipo));
    }

    [SkippableFact]
    public async Task ObtenerAutomovil_TraeIngresosConElParqueoAnidadoYCamposCalculados()
    {
        var respuesta = await ClienteApi.ObtenerAutomovilAsync(2, default);

        var ingresos = respuesta.Datos!.Ingresos!;
        Assert.Equal([2, 6, 16], ingresos.Select(i => i.Consecutivo).Order());
        var ingreso6 = ingresos.Single(i => i.Consecutivo == 6);
        Assert.Equal(1, ingreso6.Parqueo.Id);
        Assert.Equal(new DateTime(2026, 10, 5, 12, 0, 0), ingreso6.FechaHoraEntrada);
        Assert.Equal(DateTimeKind.Unspecified, ingreso6.FechaHoraEntrada.Kind);
        Assert.Equal((80m, 1.3333m, 1666.67m), (ingreso6.DuracionEstadiaMinutos, ingreso6.DuracionEstadiaHoras, ingreso6.MontoTotalAPagar));
        var enCurso = ingresos.Single(i => i.Consecutivo == 16);
        Assert.Null(enCurso.FechaHoraSalida);
        Assert.Null(enCurso.DuracionEstadiaMinutos);
        Assert.Null(enCurso.DuracionEstadiaHoras);
        Assert.Null(enCurso.MontoTotalAPagar);
    }

    [SkippableFact]
    public async Task ObtenerAutomovilInexistente_Devuelve404SinExcepcion()
    {
        var respuesta = await ClienteApi.ObtenerAutomovilAsync(999, default);

        Assert.True(respuesta.NoEncontrado);
        Assert.Equal("Registro no encontrado", respuesta.Problema!.Title);
    }

    [SkippableFact]
    public async Task CrearActualizarYBorrarAutomovil_UsaPostPutYDelete()
    {
        var creado = await ClienteApi.CrearAutomovilAsync(new AutomovilSolicitudApi("Celeste", 2020, "Suzuki", "moto"), default);
        Assert.Equal(HttpStatusCode.Created, creado.Estado);
        int id = creado.Datos!.Id;
        try
        {
            var actualizado = await ClienteApi.ActualizarAutomovilAsync(id, new AutomovilSolicitudApi("Celeste claro", 2021, "Suzuki", "moto"), default);
            Assert.Equal(HttpStatusCode.OK, actualizado.Estado);
            Assert.Equal(("Celeste claro", 2021, id), (actualizado.Datos!.Color, actualizado.Datos.Anio, actualizado.Datos.Id));

            var borrado = await ClienteApi.BorrarAutomovilAsync(id, default);
            Assert.Equal(HttpStatusCode.NoContent, borrado.Estado);
            Assert.True((await ClienteApi.ObtenerAutomovilAsync(id, default)).NoEncontrado);
        }
        finally
        {
            await ClienteApi.BorrarAutomovilAsync(id, default);
        }
    }

    [SkippableFact]
    public async Task BorrarAutomovilConIngresos_DevuelveElConflictoDelApi()
    {
        var respuesta = await ClienteApi.BorrarAutomovilAsync(1, default);

        Assert.True(respuesta.Conflicto);
        Assert.Contains("ingresos", respuesta.MensajeError, StringComparison.OrdinalIgnoreCase);
        Assert.True((await ClienteApi.ObtenerAutomovilAsync(1, default)).Exito);
    }

    [SkippableFact]
    public async Task CrearAutomovilInvalido_DevuelveElDetalleDelApi()
    {
        var respuesta = await ClienteApi.CrearAutomovilAsync(new AutomovilSolicitudApi("Café", 2022, "Kia", "camión"), default);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.Estado);
        Assert.Contains("sedán, 4x4, moto", respuesta.MensajeError);
    }

    [SkippableFact]
    public async Task Ingresos_CrearSinSalidaActualizarYBorrar()
    {
        var entrada = new DateTime(2026, 10, 31, 8, 0, 0);
        var creado = await ClienteApi.CrearIngresoAsync(new IngresoSolicitudApi(1, 4, entrada, null), default);
        Assert.Equal(HttpStatusCode.Created, creado.Estado);
        int consecutivo = creado.Datos!.Consecutivo;
        try
        {
            Assert.Equal(entrada, creado.Datos.FechaHoraEntrada); // sin desfase horario
            Assert.Null(creado.Datos.FechaHoraSalida);
            Assert.Null(creado.Datos.MontoTotalAPagar);

            var conSalida = await ClienteApi.ActualizarIngresoAsync(consecutivo, new IngresoSolicitudApi(1, 4, entrada, entrada.AddMinutes(80)), default);
            Assert.Equal((80m, 1666.67m), (conSalida.Datos!.DuracionEstadiaMinutos, conSalida.Datos.MontoTotalAPagar));

            var salidaAnterior = await ClienteApi.ActualizarIngresoAsync(consecutivo, new IngresoSolicitudApi(1, 4, entrada, entrada.AddMinutes(-5)), default);
            Assert.Equal(HttpStatusCode.BadRequest, salidaAnterior.Estado);
            Assert.Contains("posterior", salidaAnterior.MensajeError);
        }
        finally
        {
            Assert.True((await ClienteApi.BorrarIngresoAsync(consecutivo, default)).Exito);
        }
    }

    [SkippableFact]
    public async Task ListarIngresosYParqueos_RespetaLaPaginacion()
    {
        var pagina2 = await ClienteApi.ListarIngresosAsync(2, 10, default);
        var parqueos = await ClienteApi.ListarTodosLosParqueosAsync(default);

        Assert.Equal(Enumerable.Range(11, 7), pagina2.Datos!.Registros.Select(i => i.Consecutivo));
        Assert.Equal(17, pagina2.Datos.Total);
        Assert.Equal([1, 2, 3], parqueos.Datos!.Select(p => p.Id));
    }
}

public sealed class ClienteApiParqueoJsonPruebas : ClienteApiParqueoPruebasBase
{
    public ClienteApiParqueoJsonPruebas()
        : base(new OrigenJson())
    {
    }
}

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ClienteApiParqueoBaseDatosPruebas : ClienteApiParqueoPruebasBase
{
    public ClienteApiParqueoBaseDatosPruebas()
        : base(new OrigenBaseDatos())
    {
    }
}
