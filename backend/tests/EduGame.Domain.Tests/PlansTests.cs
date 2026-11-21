using EduGame.Domain.Billing;

namespace EduGame.Domain.Tests;

public class PlansTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private static Subscription Sub(string plan, int startDays, int? endDays) =>
        new() { Plan = plan, StartsAt = Now.AddDays(startDays), EndsAt = endDays is null ? null : Now.AddDays(endDays.Value) };

    [Fact]
    public void Without_billing_everyone_gets_teacher_and_family_features()
    {
        var f = Plans.Features([], Now, enforced: false);
        Assert.True(f.CanExport && f.Worksheets && f.WeeklyReport && f.Reinforcement);
        Assert.Equal(Plans.Teacher.Features.MaxClassrooms, f.MaxClassrooms);
        Assert.Equal(Plans.Family.Features.MaxProfiles, f.MaxProfiles);
        Assert.Equal([Plans.Free], Plans.Active([], Now));
    }

    [Fact]
    public void With_billing_each_plan_unlocks_only_its_features()
    {
        var free = Plans.Features([], Now, enforced: true);
        Assert.False(free.CanExport || free.Worksheets || free.WeeklyReport || free.Reinforcement);

        var teacher = Plans.Features([Sub(Plans.TeacherSlug, -1, null)], Now, enforced: true);
        Assert.True(teacher.CanExport && teacher.Worksheets);
        Assert.False(teacher.WeeklyReport || teacher.Reinforcement);

        var family = Plans.Features([Sub(Plans.FamilySlug, -1, 30)], Now, enforced: true);
        Assert.True(family.WeeklyReport && family.Reinforcement && family.Worksheets);
        Assert.False(family.CanExport);
        Assert.Equal(8, family.MaxProfiles);
        Assert.Equal(1, family.MaxClassrooms);
    }

    [Fact]
    public void Two_plans_add_up()
    {
        var both = Plans.Features([Sub(Plans.TeacherSlug, -1, null), Sub(Plans.FamilySlug, -1, null)], Now, enforced: true);
        Assert.True(both.CanExport && both.WeeklyReport && both.Reinforcement);
        Assert.Equal(20, both.MaxClassrooms);
        Assert.Equal(8, both.MaxProfiles);
    }

    [Fact]
    public void Expired_future_and_unknown_plans_do_not_count()
    {
        Assert.Equal([Plans.Free], Plans.Active([Sub(Plans.TeacherSlug, -40, -10)], Now));
        Assert.Equal([Plans.Free], Plans.Active([Sub(Plans.FamilySlug, 5, 40)], Now));
        Assert.Equal([Plans.Free], Plans.Active([Sub("premium-inexistente", -5, null)], Now));
    }

    [Fact]
    public void Only_free_teacher_and_family_are_self_service()
    {
        Assert.Equal([Plans.FreeSlug, Plans.TeacherSlug, Plans.FamilySlug], Plans.All.Where(p => p.SelfService).Select(p => p.Slug));
        Assert.Equal(3, Plans.Free.Features.MaxProfiles);
    }
}
