namespace Rumours.Core;

public sealed class MapNameMatcher(IReadOnlyList<Island> islands)
{
    private const double MinSimilarity = 0.85;

    private const double MinLead = 0.1;

    private readonly (Island Island, GameLanguage Language, string Key)[] _keys = islands
        .SelectMany(i => i.Texts.Select(t => (i, t.Key, RumourMatcher.Normalize(t.Value.Map))))
        .ToArray();

    public (Island Island, GameLanguage Language)? Match(IEnumerable<TextLine> lines)
    {
        (Island Island, GameLanguage Language)? best = null;
        double bestScore = 0, runnerUp = 0;
        foreach (var line in lines)
        {
            var key = RumourMatcher.Normalize(line.Text);
            foreach (var (island, language, mapKey) in _keys)
            {
                var score = RumourMatcher.Similarity(key, mapKey);
                if (score > bestScore)
                {
                    if (island != best?.Island) runnerUp = bestScore;
                    (best, bestScore) = ((island, language), score);
                }
                else if (island != best?.Island && score > runnerUp)
                {
                    runnerUp = score;
                }
            }
        }
        return bestScore >= MinSimilarity && bestScore - runnerUp >= MinLead ? best : null;
    }
}
