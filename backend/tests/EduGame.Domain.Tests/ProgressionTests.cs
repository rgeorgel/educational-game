using EduGame.Domain.Progression;

namespace EduGame.Domain.Tests;

public class RatingTests
{
    [Fact]
    public void Expected_is_half_when_rating_equals_difficulty()
    {
        Assert.Equal(0.5, Rating.Expected(500, 500), 3);
    }

    [Fact]
    public void Correct_answer_raises_rating_and_wrong_lowers_it()
    {
        Assert.True(Rating.Update(500, 500, 1.0, 0) > 500);
        Assert.True(Rating.Update(500, 500, 0.0, 0) < 500);
    }

    [Fact]
    public void Solving_harder_problem_gains_more()
    {
        var easy = Rating.Update(500, 300, 1.0, 0) - 500;
        var hard = Rating.Update(500, 700, 1.0, 0) - 500;
        Assert.True(hard > easy);
    }

    [Fact]
    public void Hints_reduce_the_outcome()
    {
        Assert.True(Rating.Outcome(true, 0) > Rating.Outcome(true, 1));
        Assert.True(Rating.Outcome(true, 1) > Rating.Outcome(true, 2));
        Assert.True(Rating.Outcome(true, 3) > Rating.Outcome(false, 0));
    }

    [Fact]
    public void Rating_is_clamped()
    {
        Assert.Equal(Rating.Min, Rating.Update(0, 2000, 0.0, 0));
    }

    [Fact]
    public void Consistent_success_reaches_mastery_in_reasonable_number_of_problems()
    {
        // Simula um jogador que acerta ~80% de problemas sempre perto do seu rating.
        var rng = new Random(1);
        var rating = Rating.Initial;
        var attempts = 0;
        while (rating < 1000 && attempts < 500)
        {
            var difficulty = Math.Min(1050, rating + 50);
            var correct = rng.NextDouble() < 0.8;
            rating = Rating.Update(rating, difficulty, Rating.Outcome(correct, 0), attempts++);
        }
        Assert.InRange(attempts, 15, 150);
    }
}

public class RewardsTests
{
    [Theory]
    [InlineData(10, 10, 3)]
    [InlineData(9, 10, 2)]
    [InlineData(8, 10, 2)]
    [InlineData(6, 10, 1)]
    [InlineData(5, 10, 0)]
    [InlineData(0, 0, 0)]
    public void Stars_follow_thresholds(int correct, int total, int stars)
    {
        Assert.Equal(stars, Rewards.Stars(correct, total));
    }

    [Fact]
    public void Wrong_answer_still_earns_effort_xp()
    {
        Assert.Equal(Rewards.EffortXp, Rewards.Xp(false, 800, 0));
    }

    [Fact]
    public void Harder_problems_and_fewer_hints_give_more_xp()
    {
        Assert.True(Rewards.Xp(true, 900, 0) > Rewards.Xp(true, 300, 0));
        Assert.True(Rewards.Xp(true, 900, 0) > Rewards.Xp(true, 900, 2));
        Assert.True(Rewards.Xp(true, 300, 3) > Rewards.EffortXp);
    }
}

public class LevelProgressTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void No_practice_means_zero_progress()
    {
        var result = LevelProgress.Compute(LevelDefinitions.Iniciante, [null, null, null]);
        Assert.Equal(0, result.Percent);
        Assert.False(result.ReadyForNextLevel);
        Assert.Equal(LevelDefinitions.Medio, result.Next);
    }

    [Fact]
    public void Progress_grows_with_ratings_but_only_hits_100_when_ready()
    {
        var partial = LevelProgress.Compute(LevelDefinitions.Iniciante, [650, 650, 650, 650, 650]);
        Assert.InRange(partial.Percent, 30, 99);
        Assert.False(partial.ReadyForNextLevel);

        var ready = LevelProgress.Compute(LevelDefinitions.Iniciante, [1000, 1050, 1100, 1000, 400]);
        Assert.Equal(100, ready.Percent);
        Assert.True(ready.ReadyForNextLevel);
        Assert.Equal(4, ready.MasteredSkills);
    }

    [Fact]
    public void Below_80_percent_mastery_is_not_ready()
    {
        var result = LevelProgress.Compute(LevelDefinitions.Iniciante, [1000, 1000, 1000, 400, 400]);
        Assert.False(result.ReadyForNextLevel);
        Assert.True(result.Percent < 100);
    }

    [Fact]
    public void Skill_status_reflects_rating_and_recency()
    {
        var level = LevelDefinitions.Iniciante;
        Assert.Equal(SkillStatus.NotStarted, LevelProgress.StatusOf(null, 0, null, level, Now));
        Assert.Equal(SkillStatus.Practicing, LevelProgress.StatusOf(600, 5, Now, level, Now));
        Assert.Equal(SkillStatus.Mastered, LevelProgress.StatusOf(1000, 40, Now.AddDays(-2), level, Now));
        Assert.Equal(SkillStatus.NeedsReview, LevelProgress.StatusOf(1000, 40, Now.AddDays(-30), level, Now));
    }
}
