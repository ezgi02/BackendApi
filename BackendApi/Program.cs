using BackendApi.Data;
using BackendApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//bunlar benim eklediðim
var cs = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=app.db";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(cs));
//builder.Services.AddScoped<AiClient>(); // Mock AI client’i DI’a ekle
builder.Services.AddHttpClient<AiClient>();
//bunlar zaten vardý
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DB'yi oluþtur ve bekleyen migration'larý uygula
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // app.db oluþturur, Messages tablosunu yaratýr
}

// Saðlýk ucu (Render health check için)
app.MapGet("/health", () => "ok");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
