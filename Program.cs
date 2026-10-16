using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using RaceDay_api.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using AppUser = RaceDay_api.Models.User;

var builder = WebApplication.CreateBuilder(args);

// Database (SQL Server via EF Core, Code-First) 
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Password hashing (salted PBKDF2 - the original password is never stored) 
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// Session authentication (cookie carries the user's ID and role) 
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Session";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        // This is an API: answer with status codes instead of redirecting to a login page.
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Swagger (descriptions come from the XML comments on the controllers) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for South African road running, walking and cycling events. " +
                      "Register and log in first (/api/auth/register then /api/auth/login) - Swagger UI keeps the session cookie, " +
                      "so protected endpoints work straight away. Organiser and Participant endpoints are role-restricted."
    });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml, includeControllerXmlComments: true);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Needed so the test project can use WebApplicationFactory<Program>.
public partial class Program { }
