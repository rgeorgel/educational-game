using EduGame.Domain.Billing;

namespace EduGame.Domain.Tests;

public class PlansTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private static Subscription Sub(string plan, int startDays, int? endDays) =>
        new() { Plan = plan, StartsAt = Now.AddDays(startDays), EndsAt = endDays is null ? null : Now.AddDays(endDays.Value) };

    [Fact]
    public void Without_billing_everyone_gets_teacher_features()
    {
        Assert.Equal(Plans.Teacher, Plans.Effective([], Now, enforced: false));
        Assert.Equal(Plans.School, Plans.Effective([Sub(Plans.SchoolSlug, -1, null)], Now, enforced: false));
    }

    [Fact]
    public void With_billing_the_best_active_subscription_wins()
    {
        Assert.Equal(Plans.Free, Plans.Effective([], Now, enforced: true));
        Assert.Equal(Plans.Teacher, Plans.Effective([Sub(Plans.TeacherSlug, -10, 10)], Now, enforced: true));
        Assert.Equal(Plans.School, Plans.Effective([Sub(Plans.TeacherSlug, -10, 10), Sub(Plans.SchoolSlug, -1, 30)], Now, enforced: true));
    }

    [Fact]
    public void Expired_future_and_unknown_plans_do_not_count()
    {
        Assert.Equal(Plans.Free, Plans.Effective([Sub(Plans.TeacherSlug, -40, -10)], Now, enforced: true));
        Assert.Equal(Plans.Free, Plans.Effective([Sub(Plans.TeacherSlug, 5, 40)], Now, enforced: true));
        Assert.Equal(Plans.Free, Plans.Effective([Sub("premium-inexistente", -5, null)], Now, enforced: true));
    }

    [Fact]
    public void Free_plan_is_the_most_limited()
    {
        Assert.Equal(1, Plans.Free.MaxClassrooms);
        Assert.False(Plans.Free.CanExport);
        Assert.True(Plans.Teacher.CanExport);
        Assert.True(Plans.School.MaxClassrooms > Plans.Teacher.MaxClassrooms);
    }
}
