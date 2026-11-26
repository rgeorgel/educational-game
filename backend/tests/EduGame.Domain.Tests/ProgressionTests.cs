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
    public void Percent_is_the_honest_average_of_skill_progress()
    {
        // Iniciante: 300 → 1000. 650 é metade do caminho.
        var half = LevelProgress.Compute(LevelDefinitions.Iniciante, [650, 650, 650, 650]);
        Assert.Equal(50, half.Percent);
        Assert.False(half.ReadyForNextLevel);

        // Habilidades acima do domínio não compensam as que faltam: cada uma vale no máximo 100%.
        var capped = LevelProgress.Compute(LevelDefinitions.Iniciante, [2000, 300]);
        Assert.Equal(50, capped.Percent);

        Assert.Equal(100, LevelProgress.Compute(LevelDefinitions.Iniciante, [1000, 1200]).Percent);
    }

    [Fact]
    public void Guardian_opens_at_85_percent_even_without_every_skill_mastered()
    {
        // O caso que motivou a regra: muito acerto, mas nenhuma habilidade em 1000 ainda.
        var almost = LevelProgress.Compute(LevelDefinitions.Iniciante, [918, 981, 912, 799]);
        Assert.Equal(0, almost.MasteredSkills);
        Assert.Equal(86, almost.Percent);
        Assert.True(almost.ReadyForNextLevel);

        var below = LevelProgress.Compute(LevelDefinitions.Iniciante, [880, 880, 880, 880]);
        Assert.Equal(85, LevelProgress.GuardianThreshold);
        Assert.True(below.Percent < LevelProgress.GuardianThreshold);
        Assert.False(below.ReadyForNextLevel);
    }

    [Fact]
    public void Top_level_is_never_ready_for_a_next_level()
    {
        Assert.False(LevelProgress.Compute(LevelDefinitions.Expert, [3000, 3000]).ReadyForNextLevel);
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

public class TooHardTests
{
    [Theory]
    [InlineData("medio", 15, 5, true)]       // 33% de acerto
    [InlineData("medio", 15, 6, false)]      // 40%: ainda dá
    [InlineData("medio", 14, 0, false)]      // poucas respostas para julgar
    [InlineData("iniciante", 30, 0, false)]  // não há nível mais fácil
    public void Suggests_an_easier_level_only_with_enough_answers_and_low_accuracy(string level, int attempts, int correct, bool expected) =>
        Assert.Equal(expected, LevelProgress.TooHard(LevelProgress.BySlug(level), attempts, correct));
}
