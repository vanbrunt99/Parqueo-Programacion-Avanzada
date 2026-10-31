using System.Net;
using System.Text;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Tests;

/// <summary>
/// Pruebas unitarias del cliente HTTP con un manejador simulado: recorrer todas las páginas
/// (más de 100 registros) y armar las URL con los filtros correctamente codificados.
/// </summary>
public sealed class ClienteApiPaginacionPruebas
{
    [Fact]
    public async Task ListarTodos_RecorreLasPaginasHastaElTotalInformado()
    {
        var manejador = new ManejadorSimulado(solicitud =>
        {
            var consulta = System.Web.HttpUtility.ParseQueryString(solicitud.RequestUri!.Query);
            int pagina = int.Parse(consulta["pagina"]!, System.Globalization.CultureInfo.InvariantCulture);
            int inicio = ((pagina - 1) * 100) + 1;
            int cantidad = pagina == 1 ? 100 : pagina == 2 ? 50 : 0;
            string cuerpo = "[" + string.Join(',', Enumerable.Range(inicio, cantidad)
                .Select(id => $"{{\"id\":{id},\"color\":\"Rojo\",\"anio\":2020,\"fabricante\":\"X\",\"tipo\":\"moto\"}}")) + "]";
            var respuesta = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };
            respuesta.Headers.Add("X-Total-Registros", "150");
            respuesta.Headers.Add("X-Origen-Datos", "Json");
            return respuesta;
        });
        var cliente = new ClienteApiParqueo(new HttpClient(manejador) { BaseAddress = new Uri("http://api.prueba/") });

        var resultado = await cliente.ListarTodosLosAutomovilesAsync("moto", default);

        Assert.True(resultado.Exito);
        Assert.Equal(150, resultado.Datos!.Count);
        Assert.Equal(Enumerable.Range(1, 150), resultado.Datos.Select(a => a.Id));
        Assert.Equal(2, manejador.Solicitudes.Count); // no pide una tercera página
        Assert.All(manejador.Solicitudes, s => Assert.Contains("tamanoPagina=100", s));
    }

    [Fact]
    public async Task ListarAutomoviles_CodificaElTipoConTildeYOmiteLosFiltrosVacios()
    {
        var manejador = new ManejadorSimulado(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });
        var cliente = new ClienteApiParqueo(new HttpClient(manejador) { BaseAddress = new Uri("http://api.prueba/") });

        var resultado = await cliente.ListarAutomovilesAsync(2015, null, "sedán", 2, 10, default);

        Assert.Equal("http://api.prueba/api/automoviles?anioDesde=2015&tipo=sed%C3%A1n&pagina=2&tamanoPagina=10", manejador.Solicitudes.Single());
        Assert.Equal(10, resultado.Datos!.Total); // sin encabezado: (página - 1) * tamaño + recibidos
    }

    [Fact]
    public async Task ErrorConCuerpoNoJson_SeInformaConElCodigo()
    {
        var manejador = new ManejadorSimulado(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html>proxy</html>", Encoding.UTF8, "text/html"),
        });
        var cliente = new ClienteApiParqueo(new HttpClient(manejador) { BaseAddress = new Uri("http://api.prueba/") });

        var resultado = await cliente.ObtenerAutomovilAsync(1, default);

        Assert.False(resultado.Exito);
        Assert.Equal(HttpStatusCode.BadGateway, resultado.Estado);
        Assert.Contains("502", resultado.MensajeError);
    }

    private sealed class ManejadorSimulado : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public ManejadorSimulado(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<string> Solicitudes { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Solicitudes.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(_responder(request));
        }
    }
}
