using EduGame.Domain.Progression;

namespace EduGame.Domain.Tests;

public class LevelDefinitionsTests
{
    [Fact]
    public void Levels_are_ordered_with_increasing_rating_thresholds()
    {
        var levels = LevelDefinitions.All;

        Assert.Equal(["iniciante", "medio", "avancado", "expert"], levels.Select(l => l.Slug));
        Assert.True(levels.Zip(levels.Skip(1)).All(p => p.First.MinRating < p.Second.MinRating));
    }
}
