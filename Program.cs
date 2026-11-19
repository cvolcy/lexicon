using System.Text.Json.Serialization;
using Lexicon.Infrastructure.Extraction;
using Lexicon.Infrastructure.Ingestion;
using Lexicon.Infrastructure.Persistence;
using Lexicon.Infrastructure.Storage;
using Lexicon.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddLexiconDbContext(builder.Configuration);
builder.Services.AddLexiconDocumentStorage(builder.Configuration);
builder.Services.AddLexiconDocumentExtractors();
builder.Services.AddLexiconIngestionQueue(builder.Configuration);
builder.Services.AddLexiconServices(builder.Configuration);

var app = builder.Build();

await app.MigrateLexiconDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

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
