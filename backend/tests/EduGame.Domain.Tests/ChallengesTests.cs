using EduGame.Domain.Progression;

namespace EduGame.Domain.Tests;

public class GuardianTests
{
    [Theory]
    [InlineData(9, 12, true)]
    [InlineData(12, 12, true)]
    [InlineData(8, 12, false)]
    [InlineData(0, 0, false)]
    public void Pass_requires_75_percent(int correct, int total, bool passed)
    {
        Assert.Equal(passed, Guardian.Passed(correct, total));
    }

    [Fact]
    public void Target_difficulty_is_near_mastery_of_the_level()
    {
        var rng = new Random(1);
        var targets = Enumerable.Range(0, 200).Select(_ => Guardian.TargetDifficulty(LevelDefinitions.Iniciante, rng)).ToList();
        Assert.InRange(targets.Min(), 700, 900);
        Assert.InRange(targets.Max(), 800, 1000);
    }
}

public class PlacementTests
{
    [Fact]
    public void Strong_player_climbs_and_weak_player_stays_at_iniciante()
    {
        var strong = Placement.StartRating(5);
        var weak = Placement.StartRating(5);
        for (var i = 0; i < Placement.ProblemCount; i++)
        {
            strong = Placement.Next(strong, true, i);
            weak = Placement.Next(weak, false, i);
        }

        Assert.Equal("avancado", Placement.LevelFor(strong, maxLevelOrder: 3).Slug);
        Assert.Equal("iniciante", Placement.LevelFor(weak, maxLevelOrder: 3).Slug);
    }

    [Fact]
    public void Placement_never_goes_beyond_levels_with_content()
    {
        Assert.Equal("medio", Placement.LevelFor(2600, maxLevelOrder: 2).Slug);
    }

    [Fact]
    public void Steps_shrink_but_never_below_minimum()
    {
        Assert.True(Placement.Step(0) > Placement.Step(5));
        Assert.Equal(80, Placement.Step(50));
    }

    [Fact]
    public void Level_to_ask_follows_the_estimate()
    {
        Assert.Equal("iniciante", Placement.LevelToAsk(700, 3).Slug);
        Assert.Equal("medio", Placement.LevelToAsk(1100, 3).Slug);
        Assert.Equal("avancado", Placement.LevelToAsk(2500, 3).Slug);
    }
}

public class StreakTests
{
    private static readonly DateOnly Today = new(2026, 11, 19);

    private static DateOnly[] Days(params int[] daysAgo) => [.. daysAgo.Select(d => Today.AddDays(-d))];

    [Fact]
    public void Counts_consecutive_days()
    {
        Assert.Equal(3, Streaks.Current(Days(0, 1, 2), Today));
    }

    [Fact]
    public void One_day_off_does_not_break_the_streak()
    {
        Assert.Equal(3, Streaks.Current(Days(0, 2, 3), Today));
        Assert.Equal(2, Streaks.Current(Days(2, 3), Today)); // ontem faltou, hoje ainda dá
    }

    [Fact]
    public void Two_days_off_break_the_streak()
    {
        Assert.Equal(1, Streaks.Current(Days(0, 3, 4), Today));
        Assert.Equal(0, Streaks.Current(Days(3, 4), Today));
    }

    [Fact]
    public void Best_streak_looks_at_whole_history()
    {
        Assert.Equal(4, Streaks.Best(Days(0, 10, 11, 12, 13, 20)));
    }
}

public class CalibrationTests
{
    private static List<Calibration.Sample> Samples(int count, double outcome) =>
        [.. Enumerable.Range(0, count).Select(_ => new Calibration.Sample(800, 800, outcome))];

    [Fact]
    public void Too_few_samples_keep_offset()
    {
        Assert.Equal(40, Calibration.NewOffset(40, Samples(10, 1.0)));
    }

    [Fact]
    public void Easier_than_expected_lowers_difficulty_and_harder_raises_it()
    {
        Assert.True(Calibration.NewOffset(0, Samples(100, 1.0)) < 0);
        Assert.True(Calibration.NewOffset(0, Samples(100, 0.0)) > 0);
        Assert.Equal(0, Calibration.NewOffset(0, Samples(100, 0.5)));
    }

    [Fact]
    public void Steps_and_total_offset_are_bounded()
    {
        Assert.Equal(-Calibration.MaxStep, Calibration.NewOffset(0, Samples(100, 1.0)));
        Assert.Equal(-Calibration.MaxOffset, Calibration.NewOffset(-Calibration.MaxOffset, Samples(100, 1.0)));
    }
}

public class AchievementTests
{
    private static readonly PlayerStats Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 1);

    [Fact]
    public void Nothing_earned_at_start()
    {
        Assert.Empty(Achievements.EarnedBy(Empty));
    }

    [Fact]
    public void First_problem_and_level_up_earn_medals()
    {
        var earned = Achievements.EarnedBy(Empty with { ProblemsSolved = 1, HighestLevelOrder = 2 }).Select(a => a.Slug).ToList();
        Assert.Contains("primeiros-passos", earned);
        Assert.Contains("nivel-medio", earned);
        Assert.DoesNotContain("nivel-avancado", earned);
    }

    [Fact]
    public void Slugs_are_unique()
    {
        Assert.Equal(Achievements.All.Count, Achievements.All.Select(a => a.Slug).Distinct().Count());
    }
}

public class EngagementTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Daily_seed_is_stable_and_changes_by_day_and_level()
    {
        var day = new DateOnly(2026, 11, 19);
        Assert.Equal(DailyChallenge.Seed(day, "matematica", "medio"), DailyChallenge.Seed(day, "matematica", "medio"));
        Assert.NotEqual(DailyChallenge.Seed(day, "matematica", "medio"), DailyChallenge.Seed(day.AddDays(1), "matematica", "medio"));
        Assert.NotEqual(DailyChallenge.Seed(day, "matematica", "medio"), DailyChallenge.Seed(day, "matematica", "avancado"));
        Assert.True(DailyChallenge.Seed(day, "logica", "expert") >= 0);
    }

    [Fact]
    public void Daily_target_is_inside_the_level_band()
    {
        foreach (var level in LevelDefinitions.All)
            Assert.InRange(DailyChallenge.TargetDifficulty(level), Rating.InitialFor(level), LevelProgress.TargetRating(level));
    }

    [Fact]
    public void Review_prefers_skills_needing_review_then_weakest()
    {
        var level = LevelDefinitions.Iniciante;
        var picked = Review.PickSkills(
        [
            new(1, level, 900, 30, Now.AddDays(-1)),   // forte e recente
            new(2, level, 400, 10, Now.AddDays(-1)),   // fraca
            new(3, level, 1100, 40, Now.AddDays(-40)), // dominada, esquecida → revisar
            new(4, level, 300, 0, null),               // nunca praticada: fora
        ], Now);

        Assert.Equal([3, 2, 1], picked);
    }

    [Fact]
    public void Avatar_items_unlock_with_xp()
    {
        Assert.True(AvatarItems.IsUnlocked(AvatarItems.Accessories, "nenhum", 0));
        Assert.False(AvatarItems.IsUnlocked(AvatarItems.Accessories, "coroa", 100));
        Assert.True(AvatarItems.IsUnlocked(AvatarItems.Accessories, "coroa", 5000));
        Assert.False(AvatarItems.IsUnlocked(AvatarItems.Colors, "inexistente", 99999));
    }
}
