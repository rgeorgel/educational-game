namespace EduGame.Api.Common;

/// <summary>Data "de hoje" no fuso do jogo (para sequência de dias e histórico).</summary>
public sealed class GameClock(TimeProvider time, IConfiguration config)
{
    private readonly TimeZoneInfo _zone = FindZone(config["Game:TimeZone"] ?? "America/Sao_Paulo");

    public DateTimeOffset Now => time.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), _zone).DateTime);

    private static TimeZoneInfo FindZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            // Imagens sem tzdata: usa o horário de Brasília fixo.
            return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT");
        }
    }
}
