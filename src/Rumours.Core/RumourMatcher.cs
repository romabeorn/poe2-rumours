namespace Rumours.Core;

public sealed class RumourMatcher(IReadOnlyList<Island> islands)
{
    private const double MinSimilarity = 0.6, MinLead = 0.1;
    private const double MinSimilarityWithClearLead = 0.45, ClearLead = 0.25;

    private readonly (Island Island, string Key)[] _keys =
        islands.SelectMany(i => i.Texts.Select(t => (i, Fold(t.Value.Rumour)))).ToArray();

    public Island? Match(string line)
    {
        var key = Fold(line);
        if (key.Length < 4) return null;

        Island? best = null;
        double bestScore = 0, runnerUp = 0;
        foreach (var (island, rumourKey) in _keys)
        {
            var score = Similarity(key, rumourKey);
            if (score > bestScore)
            {
                if (island != best) runnerUp = bestScore;
                (best, bestScore) = (island, score);
            }
            else if (island != best && score > runnerUp)
            {
                runnerUp = score;
            }
        }

        var lead = bestScore - runnerUp;
        var accepted = (bestScore >= MinSimilarity && lead >= MinLead)
            || (bestScore >= MinSimilarityWithClearLead && lead >= ClearLead);
        return accepted ? best : null;
    }

    internal static string Fold(string text)
    {
        var letters = Normalize(text).Replace("ru", "m").Replace("rn", "m");
        return new(letters.Select(c => "wmuvhbk".Contains(c) ? 'n' : c).ToArray());
    }

    internal static string Normalize(string text) =>
        new(text.Where(char.IsLetter).Select(char.ToLowerInvariant).ToArray());

    internal static double Similarity(string a, string b)
    {
        var longest = Math.Max(a.Length, b.Length);
        return longest == 0 ? 0 : 1.0 - (double)Levenshtein(a, b) / longest;
    }

    private static int Levenshtein(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitution, Math.Min(previous[j], current[j - 1]) + 1);
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
}
