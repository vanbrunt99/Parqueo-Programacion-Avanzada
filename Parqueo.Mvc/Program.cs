using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Parqueo.Mvc.Infraestructura;
using Parqueo.Mvc.Servicios;

var builder = WebApplication.CreateBuilder(args);

// URL base del API del Trabajo 03. Es la ÚNICA configuración de datos del MVC: el MVC no se
// conecta a PostgreSQL ni lee los archivos JSON. Qué origen usan los repositorios lo decide
// el API con su propio OrigenDatos (Parqueo.Api/appsettings.json).
// Se puede cambiar sin editar el archivo: --Api:UrlBase=http://localhost:5247/ o la variable
// de entorno Api__UrlBase.
string? textoUrlApi = builder.Configuration["Api:UrlBase"];
if (!Uri.TryCreate(textoUrlApi, UriKind.Absolute, out Uri? urlApi))
{
    throw new InvalidOperationException($"Api:UrlBase debe ser una URL absoluta (valor actual: '{textoUrlApi}').");
}

// Cliente HTTP tipado (IHttpClientFactory): administra las conexiones y registra en el log
// cada llamada al API (System.Net.Http.HttpClient.ClienteApiParqueo).
builder.Services.AddHttpClient<ClienteApiParqueo>(cliente =>
{
    cliente.BaseAddress = urlApi;
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

// Las vistas escriben tildes y símbolos (₡, ñ) tal cual, sin convertirlos en entidades HTML.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(opciones =>
    opciones.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddScoped<FiltroApiNoDisponible>();
builder.Services.AddControllersWithViews(opciones =>
{
    // Todas las acciones POST de formularios exigen el token antiforgery.
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    opciones.Filters.AddService<FiltroApiNoDisponible>();

    // Mensajes del enlace de datos en español (por ejemplo, un año no numérico en el filtro).
    var mensajes = opciones.ModelBindingMessageProvider;
    mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    mensajes.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
    mensajes.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    mensajes.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor de {campo}.");
    mensajes.SetValueMustNotBeNullAccessor(campo => $"El campo {campo} es obligatorio.");
    mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    mensajes.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
    mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor indicado no es válido para {campo}.");
    mensajes.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor indicado no es válido.");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
}

// Fechas, números y moneda en formato de Costa Rica.
var cultura = CultureInfo.GetCultureInfo("es-CR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura],
});

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Inicio}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Logger.LogInformation("Parqueo.Mvc consumirá el API en {UrlApi}", urlApi);

app.Run();
