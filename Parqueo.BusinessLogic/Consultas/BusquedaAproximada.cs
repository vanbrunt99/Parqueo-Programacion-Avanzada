using System.Globalization;
using System.Text;

namespace Parqueo.BusinessLogic.Consultas;

/// <summary>
/// Construye el patrón de las búsquedas aproximadas por texto.
/// </summary>
/// <remarks>
/// Regla común para JSON y base de datos: el texto buscado puede aparecer en
/// cualquier parte del valor (subcadena), sin distinguir mayúsculas/minúsculas ni
/// tildes. Ejemplos: "sedan" encuentra "sedán"; "san jose" encuentra "San José";
/// "TOYO" encuentra "Toyota".
/// <para>
/// Se genera una expresión regular que solo usa clases de caracteres explícitas
/// (por ejemplo, <c>[sS][eéEÉ]...</c>) y escapes de símbolos. Esa sintaxis es igual
/// en .NET (<see cref="System.Text.RegularExpressions.Regex"/>) y en PostgreSQL
/// (operador <c>~</c>, al que Npgsql traduce <c>Regex.IsMatch</c>), por lo que ambos
/// orígenes de datos aplican exactamente la misma regla, sin depender de la
/// configuración regional del servidor ni de la extensión unaccent.
/// </para>
/// </remarks>
public sealed class BusquedaAproximada
{
    private static readonly Dictionary<char, string> Vocales = new()
    {
        ['a'] = "[aáàäâAÁÀÄÂ]",
        ['e'] = "[eéèëêEÉÈËÊ]",
        ['i'] = "[iíìïîIÍÌÏÎ]",
        ['o'] = "[oóòöôOÓÒÖÔ]",
        ['u'] = "[uúùüûUÚÙÜÛ]",
    };

    /// <summary>
    /// Devuelve el patrón para el texto indicado, o <c>null</c> si el texto está
    /// vacío (en ese caso no se debe filtrar).
    /// </summary>
    public string? CrearPatron(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var palabras = texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var patron = new StringBuilder();

        for (int indice = 0; indice < palabras.Length; indice++)
        {
            if (indice > 0)
            {
                patron.Append(@"\s+");
            }

            foreach (char caracter in palabras[indice])
            {
                patron.Append(PatronDeCaracter(caracter));
            }
        }

        return patron.ToString();
    }

    private static string PatronDeCaracter(char caracter)
    {
        char minuscula = char.ToLowerInvariant(caracter);

        if (minuscula == 'ñ')
        {
            return "[ñÑ]";
        }

        char basico = QuitarTilde(minuscula);
        if (Vocales.TryGetValue(basico, out var claseVocal))
        {
            return claseVocal;
        }

        if (char.IsLetter(basico))
        {
            return $"[{basico}{char.ToUpperInvariant(basico)}]";
        }

        if (char.IsDigit(caracter))
        {
            return caracter.ToString();
        }

        // Cualquier otro símbolo se toma literalmente: "\" + símbolo no alfanumérico
        // significa "este carácter" tanto en .NET como en PostgreSQL.
        return "\\" + caracter;
    }

    private static char QuitarTilde(char caracter)
    {
        string descompuesto = caracter.ToString().Normalize(NormalizationForm.FormD);
        foreach (char parte in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(parte) != UnicodeCategory.NonSpacingMark)
            {
                return parte;
            }
        }

        return caracter;
    }
}
