var builder = WebApplication.CreateBuilder(args);

// YARP konfigürasyonunu appsettings.json dosyasındaki "ReverseProxy" bölümünden alıyoruz.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS Policy for Next.js Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

app.MapGet("/", () => "Vitrin API Gateway is running! (YARP)");

// Gelen istekleri ilgili mikroservislere yönlendirecek olan YARP Middleware'i
app.MapReverseProxy();

app.Run();
