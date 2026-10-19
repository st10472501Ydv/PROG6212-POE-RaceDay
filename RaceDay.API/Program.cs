using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDay")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Events, categories, enrolments and results. " +
            "POST /api/auth/login first to get the session cookie; public endpoints work without logging in."
    });
    string xml = Path.Combine(AppContext.BaseDirectory, "RaceDay.API.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

var app = builder.Build();

// Swagger UI: the whole API is testable from the browser at /swagger.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

app.UseHttpsRedirection();

// Session must run before authorization so the role filter can read it.
app.UseSession();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class to the test project (WebApplicationFactory).
public partial class Program { }
