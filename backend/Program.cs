using Microsoft.EntityFrameworkCore;
using backend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from .env file (will be read by appsettings.json configuration)
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DevMatchDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configure CORS
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

// Add HTTP client for external APIs
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<backend.Services.IGitHubService, backend.Services.GitHubService>();
builder.Services.AddScoped<backend.Services.IAiService, backend.Services.AiService>();
builder.Services.AddScoped<backend.Services.IJobService, backend.Services.JobService>();
builder.Services.AddScoped<backend.Services.IJoobleService, backend.Services.JoobleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("DevMatchPolicy");

app.UseAuthorization();

app.MapControllers();

// Test endpoint
app.MapGet("/api/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
.WithName("HealthCheck");

app.Run();
