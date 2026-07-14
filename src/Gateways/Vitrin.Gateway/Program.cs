using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication configuration
var secret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret en az 32 bayt uzunluğunda yapılandırılmalıdır.");
}
var issuer = builder.Configuration["Jwt:Issuer"] ?? "Vitrin";
var audience = builder.Configuration["Jwt:Audience"] ?? "Vitrin";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// YARP konfigürasyonunu appsettings.json dosyasındaki "ReverseProxy" bölümünden alıyoruz.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS Policy for Next.js Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",    // local dev
                "http://vitrin-web:3000"    // docker iç ağ
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => "Vitrin API Gateway is running! (YARP)");

// Gelen istekleri ilgili mikroservislere yönlendirecek olan YARP Middleware'i
app.MapReverseProxy();

app.Run();
