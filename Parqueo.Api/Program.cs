using Microsoft.AspNetCore.Mvc;
using Parqueo.Api.Infraestructura;
using Parqueo.BusinessLogic;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión vive en User Secrets (ConnectionStrings:ParqueoDb), compartidos
// con Parqueo.AppConsole. Se cargan también fuera del ambiente Development.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Origen de datos de los repositorios: "Json" (predeterminado) o "BaseDatos".
// Ejemplo: dotnet run --project Parqueo.Api -- --OrigenDatos=BaseDatos
string textoOrigen = builder.Configuration["OrigenDatos"] ?? nameof(OrigenDatos.Json);
if (!Enum.TryParse(textoOrigen, ignoreCase: true, out OrigenDatos origen))
{
    throw new InvalidOperationException($"OrigenDatos inválido: '{textoOrigen}'. Use Json o BaseDatos.");
}

string? cadenaConexion = builder.Configuration.GetConnectionString("ParqueoDb");
string carpetaJson = Path.GetFullPath(builder.Configuration["RutaDatosJson"] ?? "Datos", AppContext.BaseDirectory);

builder.Services.AgregarRepositoriosParqueo(origen, cadenaConexion, carpetaJson);
builder.Services
    .AddControllers(opciones =>
    {
        // Mensajes de enlace de datos en español (valores no numéricos, cuerpo ausente...).
        var mensajes = opciones.ModelBindingMessageProvider;
        mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
        mensajes.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
        mensajes.SetMissingRequestBodyRequiredValueAccessor(() => "Falta el cuerpo JSON de la solicitud.");
        mensajes.SetValueMustNotBeNullAccessor(valor => $"El valor '{valor}' no es válido.");
    })
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // Errores de validación de DTO como ProblemDetails con título en español.
        opciones.InvalidModelStateResponseFactory = contexto =>
            new BadRequestObjectResult(new ValidationProblemDetails(contexto.ModelState)
            {
                Title = "Datos inválidos",
                Status = StatusCodes.Status400BadRequest,
            })
            { ContentTypes = { "application/problem+json" } };
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Cada respuesta, incluidas las de error, indica qué origen de datos la produjo
// (evidencia en Postman). Se agrega al enviar los encabezados para que el manejador de
// excepciones no la borre.
app.Use(async (contexto, siguiente) =>
{
    contexto.Response.OnStarting(() =>
    {
        contexto.Response.Headers["X-Origen-Datos"] = origen.ToString();
        return Task.CompletedTask;
    });
    await siguiente(contexto);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // documento OpenAPI en /openapi/v1.json
}

app.MapControllers();

app.Logger.LogInformation(
    "Origen de datos: {Origen}{Detalle}",
    origen,
    origen == OrigenDatos.Json ? $" (carpeta {carpetaJson})" : string.Empty);

app.Run();

/// <summary>Visible para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
