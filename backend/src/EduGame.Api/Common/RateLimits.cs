namespace EduGame.Api.Common;

public static class RateLimits
{
    public const string Auth = "auth";

    /// <summary>Início como convidado: mais folgado, porque uma sala inteira sai pelo mesmo IP da escola.</summary>
    public const string Guest = "guest";
}
