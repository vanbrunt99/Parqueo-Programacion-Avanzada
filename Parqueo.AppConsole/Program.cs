using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Parqueo.Model.Contexto;

// Host: carga appsettings, variables de entorno, argumentos y User Secrets.
// Las herramientas de EF Core también lo usan para resolver
// "Name=ConnectionStrings:ParqueoDb" durante el scaffold.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory, // appsettings.json se copia junto al ejecutable
});
builder.Configuration.AddUserSecrets<Program>(optional: true);

string? cadenaConexion = builder.Configuration.GetConnectionString("ParqueoDb");
builder.Services.AddDbContext<ParqueoContext>(opciones => opciones.UseNpgsql(cadenaConexion));

using var host = builder.Build();

if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    Console.WriteLine("No se encontró el secreto 'ConnectionStrings:ParqueoDb'. Configúrelo con dotnet user-secrets.");
    return;
}

// Verificación de la conexión: consulta las tablas PRQ mediante el contexto de EF Core.
using var alcance = host.Services.CreateScope();
var contexto = alcance.ServiceProvider.GetRequiredService<ParqueoContext>();

Console.WriteLine($"Automóviles: {await contexto.PrqAutomovils.CountAsync()}");
Console.WriteLine($"Parqueos:    {await contexto.PrqParqueos.CountAsync()}");
Console.WriteLine($"Ingresos:    {await contexto.PrqIngresoAutomovils.CountAsync()}");
Console.WriteLine();

var ingresos = await contexto.PrqIngresoAutomovils
    .AsNoTracking()
    .Include(i => i.IdParqueoNavigation)
    .OrderBy(i => i.Consecutivo)
    .ToListAsync();

Console.WriteLine("Consec. | Entrada          | Salida           | Minutos | Horas  | Precio/h | Monto");
foreach (var ingreso in ingresos)
{
    Console.WriteLine(
        $"{ingreso.Consecutivo,7} | {ingreso.FechaHoraEntrada:yyyy-MM-dd HH:mm} | " +
        $"{(ingreso.FechaHoraSalida?.ToString("yyyy-MM-dd HH:mm") ?? "(en curso)"),-16} | " +
        $"{ingreso.DuracionEstadiaMinutos?.ToString("0.##") ?? "null",7} | " +
        $"{ingreso.DuracionEstadiaHoras?.ToString("0.####") ?? "null",6} | " +
        $"{ingreso.IdParqueoNavigation.PrecioPorHora,8:0.00} | " +
        $"{ingreso.MontoTotalAPagar?.ToString("0.00") ?? "null"}");
}
