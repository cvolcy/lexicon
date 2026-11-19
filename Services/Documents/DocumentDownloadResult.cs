namespace Lexicon.Services.Documents;

public sealed record DocumentDownloadResult(
    Stream Content,
    string ContentType,
    string FileName);
