var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/health", (IHostEnvironment env) => TypedResults.Ok(new HealthResponse(
    Status: "ok",
    Environment: env.EnvironmentName,
    TimestampUtc: DateTimeOffset.UtcNow)))
    .WithName("GetHealth");

api.MapFallback(() => TypedResults.NotFound());

app.MapFallbackToFile("index.html");

app.Run();

public sealed record HealthResponse(
    string Status,
    string Environment,
    DateTimeOffset TimestampUtc);
