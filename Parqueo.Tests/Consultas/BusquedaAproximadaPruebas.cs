using System.Text.RegularExpressions;
using Parqueo.BusinessLogic.Consultas;

namespace Parqueo.Tests.Consultas;

public class BusquedaAproximadaPruebas
{
    private readonly BusquedaAproximada _busqueda = new();

    [Theory]
    [InlineData("sedan", "sedán")]
    [InlineData("SEDÁN", "sedán")]
    [InlineData("san jose", "San José")]
    [InlineData("san   josé", "San José")]
    [InlineData("TOYO", "Toyota")]
    [InlineData("limon", "Limón")]
    [InlineData("4x4", "4x4")]
    [InlineData("ruinas", "Parqueo Las Ruinas")]
    public void Coincide_SinDistinguirMayusculasNiTildes(string texto, string valor)
    {
        string patron = _busqueda.CrearPatron(texto)!;
        Assert.Matches(new Regex(patron), valor);
    }

    [Theory]
    [InlineData("moto", "sedán")]
    [InlineData("4.4", "4x4")]       // el punto es literal, no comodín
    [InlineData("a+", "aaa")]        // el + es literal
    [InlineData("san jose", "sanjose")]
    public void NoCoincide_CuandoElTextoNoEstaPresente(string texto, string valor)
    {
        string patron = _busqueda.CrearPatron(texto)!;
        Assert.DoesNotMatch(new Regex(patron), valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TextoVacio_NoGeneraPatron(string? texto)
    {
        Assert.Null(_busqueda.CrearPatron(texto));
    }

    [Fact]
    public void Patron_UsaSoloClasesDeCaracteresYEscapes()
    {
        Assert.Equal(@"[sS][eéèëêEÉÈËÊ][dD][aáàäâAÁÀÄÂ][nN]", _busqueda.CrearPatron("sedán"));
        Assert.Equal(@"4[xX]4\.", _busqueda.CrearPatron("4x4."));
    }
}
