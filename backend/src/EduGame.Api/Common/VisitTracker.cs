using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Common;

/// <summary>Conta um acesso (abrir o jogo) da conta no dia de hoje. Upsert atômico, sem leitura prévia.</summary>
public static class VisitTracker
{
    public static Task RecordVisitAsync(this AppDbContext db, Guid userId, DateOnly today, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO daily_visits (user_id, date, visits) VALUES ({userId}, {today}, 1)
            ON CONFLICT (user_id, date) DO UPDATE SET visits = daily_visits.visits + 1
            """, ct);
}
