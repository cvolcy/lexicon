namespace Lexicon.Services;

internal static class TokenEstimator
{
    public static int Estimate(string text)
        => string.IsNullOrWhiteSpace(text) ? 0 : Estimate(text.Length);

    public static int Estimate(int characterCount)
        => characterCount <= 0 ? 0 : (int)Math.Ceiling(characterCount / 4.0);
}
