using Lexicon.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddLexiconDbContext(builder.Configuration);

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

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapFallback("/api/{**slug}", () => TypedResults.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
