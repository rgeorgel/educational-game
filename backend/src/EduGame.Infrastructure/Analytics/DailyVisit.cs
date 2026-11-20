namespace EduGame.Infrastructure.Analytics;

/// <summary>
/// Quantas vezes cada conta abriu o jogo em cada dia (fuso do jogo).
/// Só contagem agregada: não guarda IP, aparelho nem horário exato. Usado pelo painel de administração.
/// </summary>
public class DailyVisit
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public int Visits { get; set; }
}
