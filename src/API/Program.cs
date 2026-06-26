using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<Orchi.Api.Data.AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("DesktopDev", policy =>
        policy.SetIsOriginAllowed(origin =>
                string.IsNullOrEmpty(origin) ||
                origin == "null" ||
                (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                 uri.Host is "localhost" or "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("DesktopDev");
app.UseAuthorization();
app.MapControllers();

app.Run();
