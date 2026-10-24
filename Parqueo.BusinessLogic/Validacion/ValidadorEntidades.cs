using Parqueo.Model.Entidades;

namespace Parqueo.BusinessLogic.Validacion;

/// <summary>
/// Reglas de validación de las tres tablas, compartidas por los repositorios de EF
/// y de JSON (una sola fuente de verdad). Reflejan las restricciones de
/// diseño-bd.sql para que los archivos JSON acepten exactamente los mismos datos
/// que PostgreSQL. Los datos inválidos producen <see cref="ArgumentException"/>.
/// </summary>
public sealed class ValidadorEntidades
{
    /// <summary>Tipos permitidos, iguales a la restricción ck_prq_automovil_tipo.</summary>
    public static readonly IReadOnlyList<string> TiposAutomovil = ["sedán", "4x4", "moto"];

    /// <summary>Provincias permitidas, iguales a la restricción ck_prq_parqueo_provincia.</summary>
    public static readonly IReadOnlyList<string> Provincias =
        ["San José", "Alajuela", "Cartago", "Heredia", "Guanacaste", "Puntarenas", "Limón"];

    /// <summary>Rango de años igual a la restricción ck_prq_automovil_anio.</summary>
    public const int AnioMinimo = 1886;

    public const int AnioMaximo = 2100;

    private const decimal PrecioMaximo = 99_999_999.99m; // numeric(10,2)

    /// <summary>
    /// Rango razonable de fechas para un ingreso. Evita valores extremos (por ejemplo, el
    /// año 9999) que desbordan los cálculos de duración y monto o que no se pueden
    /// leer de nuevo desde JSON.
    /// </summary>
    public static readonly DateTime FechaMinima = new(2000, 1, 1);

    public static readonly DateTime FechaMaxima = new(2100, 12, 31, 23, 59, 59);

    public void Validar(PrqAutomovil automovil)
    {
        ArgumentNullException.ThrowIfNull(automovil);
        ValidarTexto(automovil.Color, "color", 30);
        ValidarTexto(automovil.Fabricante, "fabricante", 50);

        if (automovil.Anio < AnioMinimo || automovil.Anio > AnioMaximo)
        {
            throw new ArgumentException($"El año debe estar entre {AnioMinimo} y {AnioMaximo}; se recibió {automovil.Anio}.");
        }

        if (!TiposAutomovil.Contains(automovil.Tipo))
        {
            throw new ArgumentException(
                $"El tipo '{automovil.Tipo}' no es válido. Valores permitidos: {string.Join(", ", TiposAutomovil)}.");
        }
    }

    public void Validar(PrqParqueo parqueo)
    {
        ArgumentNullException.ThrowIfNull(parqueo);
        ValidarTexto(parqueo.Nombre, "nombre del parqueo", 100);

        if (!Provincias.Contains(parqueo.NombreProvincia))
        {
            throw new ArgumentException(
                $"La provincia '{parqueo.NombreProvincia}' no es válida. Valores permitidos: {string.Join(", ", Provincias)}.");
        }

        if (parqueo.PrecioPorHora <= 0 || parqueo.PrecioPorHora > PrecioMaximo)
        {
            throw new ArgumentException($"El precio por hora debe ser mayor que 0 y menor o igual a {PrecioMaximo}.");
        }

        if (decimal.Round(parqueo.PrecioPorHora, 2) != parqueo.PrecioPorHora)
        {
            throw new ArgumentException("El precio por hora admite como máximo 2 decimales.");
        }
    }

    public void Validar(PrqIngresoAutomovil ingreso)
    {
        ArgumentNullException.ThrowIfNull(ingreso);

        if (ingreso.IdParqueo <= 0 || ingreso.IdAutomovil <= 0)
        {
            throw new ArgumentException("El ingreso debe indicar un id de parqueo y un id de automóvil válidos.");
        }

        if (ingreso.FechaHoraEntrada == default)
        {
            throw new ArgumentException("Debe indicar la fecha y hora de entrada.");
        }

        if (ingreso.FechaHoraEntrada.Kind == DateTimeKind.Utc || ingreso.FechaHoraSalida?.Kind == DateTimeKind.Utc)
        {
            throw new ArgumentException("Las fechas se registran en hora local de Costa Rica, no en UTC.");
        }

        foreach (var (fecha, campo) in new[] { ((DateTime?)ingreso.FechaHoraEntrada, "entrada"), (ingreso.FechaHoraSalida, "salida") })
        {
            if (fecha is DateTime valor && (valor < FechaMinima || valor > FechaMaxima))
            {
                throw new ArgumentException(
                    $"La fecha y hora de {campo} ({valor:yyyy-MM-dd HH:mm}) debe estar entre {FechaMinima:yyyy-MM-dd} y {FechaMaxima:yyyy-MM-dd}.");
            }
        }

        if (ingreso.FechaHoraSalida is DateTime salida && salida <= ingreso.FechaHoraEntrada)
        {
            throw new ArgumentException("La fecha y hora de salida debe ser posterior a la de entrada.");
        }
    }

    /// <summary>Error de llave foránea: el registro referenciado no existe.</summary>
    public ReglaNegocioException ReferenciaInexistente(string tabla, int id) =>
        new($"No existe el registro {id} en {tabla}; no se puede guardar el ingreso.");

    /// <summary>Error de borrado: el registro tiene ingresos que lo referencian.</summary>
    public ReglaNegocioException RegistroConIngresos(string tabla, int id) =>
        new($"No se puede borrar el registro {id} de {tabla} porque tiene ingresos registrados.");

    /// <summary>Error de inserción con llave primaria repetida.</summary>
    public ReglaNegocioException LlaveDuplicada(string tabla, int id) =>
        new($"Ya existe un registro con la llave {id} en {tabla}.");

    /// <summary>La llave la asigna el origen de datos (identidad o consecutivo).</summary>
    public ArgumentException LlaveNoPermitida(string tabla, int id) =>
        new($"La llave de {tabla} la asigna el origen de datos; envíe 0 en lugar de {id}.");

    private static void ValidarTexto(string? valor, string campo, int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException($"El campo {campo} es obligatorio.");
        }

        if (valor.Length > longitudMaxima)
        {
            throw new ArgumentException($"El campo {campo} admite como máximo {longitudMaxima} caracteres.");
        }

        // PostgreSQL no admite el carácter nulo en texto; JSON sí. Se rechazan los
        // caracteres de control para que ambos orígenes acepten los mismos datos.
        if (valor.Any(char.IsControl))
        {
            throw new ArgumentException($"El campo {campo} contiene caracteres de control no permitidos.");
        }
    }
}
