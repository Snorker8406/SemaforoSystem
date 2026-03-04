using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.Auth;
using SemaforoSystem.Server.Models;
using SemaforoSystem.Server.Services;
//Scaffold-DbContext "Host=localhost;Database=semaforo;Username=IOTek_Admin;Password=1234" Npgsql.EntityFrameworkCore.PostgreSQL -OutputDir Models -Context ApplicationDbContext -DataAnnotations -Force

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:2488")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// DbContext para Identity
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>().AddEntityFrameworkStores<AuthDbContext>();

// Domain services
builder.Services.AddScoped<InventoryService>();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/Auth").MapIdentityApi<ApplicationUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SemaforoSystem API v1");
    });
}

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
