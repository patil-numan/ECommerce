var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Health Checks
builder.Services.AddHealthChecks();

// YARP Reverse Proxy
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(
        builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Health Check
app.MapHealthChecks("/health");

// Reverse Proxy
app.MapReverseProxy();

app.Run();