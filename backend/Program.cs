using Microsoft.EntityFrameworkCore;
using backend.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DevMatchDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevMatchPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddHttpClient();

builder.Services.AddScoped<backend.Services.IGitHubService, backend.Services.GitHubService>();
builder.Services.AddScoped<backend.Services.IAiService, backend.Services.AiService>();
builder.Services.AddScoped<backend.Services.IJobService, backend.Services.JobService>();
builder.Services.AddScoped<backend.Services.IJoobleService, backend.Services.JoobleService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("DevMatchPolicy");

app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
.WithName("HealthCheck");

app.Run();
