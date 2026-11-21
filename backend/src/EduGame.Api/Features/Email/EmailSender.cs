using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EduGame.Api.Features.Email;

/// <summary>Um e-mail pronto para enviar (HTML e texto puro, para clientes que não mostram HTML).</summary>
public sealed record EmailMessage(string To, string Subject, string Html, string Text, string Kind);

/// <summary>
/// Configuração do envio (seção "Email"; no Docker: SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, EMAIL_FROM).
/// Funciona com qualquer provedor que fale SMTP: Resend, Postmark, Brevo, Amazon SES, Mailgun, Gmail…
/// </summary>
public sealed class EmailSettings(IConfiguration config)
{
    public string? Host => Value("Email:SmtpHost");
    public int Port => int.TryParse(Value("Email:SmtpPort"), out var p) && p is > 0 and < 65536 ? p : 587;
    public string? Username => Value("Email:SmtpUser");
    public string? Password => Value("Email:SmtpPassword");

    /// <summary>Remetente, ex.: "Educa Game &lt;nao-responda@seu-dominio&gt;".</summary>
    public string? From => Value("Email:From");

    /// <summary>Para onde vão as respostas (opcional), ex.: o e-mail de suporte.</summary>
    public string? ReplyTo => Value("Email:ReplyTo");

    /// <summary>"auto" (padrão: 465 = SSL, outras portas = STARTTLS se disponível), "ssl", "starttls" ou "none".</summary>
    public string Security => Value("Email:Security")?.ToLowerInvariant() ?? "auto";

    public bool Enabled => Host is not null && From is not null;

    private string? Value(string key) => config[key] is { Length: > 0 } v ? v.Trim() : null;
}

/// <summary>Quem de fato entrega o e-mail (SMTP em produção; log quando não há SMTP; falso nos testes).</summary>
public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed class SmtpEmailTransport(EmailSettings settings) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(settings.From!));
        mime.To.Add(MailboxAddress.Parse(message.To));
        if (settings.ReplyTo is { } replyTo) mime.ReplyTo.Add(MailboxAddress.Parse(replyTo));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.Html, TextBody = message.Text }.ToMessageBody();

        using var smtp = new SmtpClient { Timeout = 20_000 };
        var security = settings.Security switch
        {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "starttls" => SecureSocketOptions.StartTls,
            "none" => SecureSocketOptions.None,
            _ => SecureSocketOptions.Auto,
        };
        await smtp.ConnectAsync(settings.Host!, settings.Port, security, ct);
        if (settings.Username is not null) await smtp.AuthenticateAsync(settings.Username, settings.Password ?? "", ct);
        await smtp.SendAsync(new MailKit.SendRequest(mime), ct);
        await smtp.DisconnectAsync(true, ct);
    }
}

/// <summary>
/// Sem SMTP configurado: nada é enviado. Em desenvolvimento o e-mail inteiro vai para o log
/// (dá para clicar no link de nova senha); fora dele, só um aviso, porque o texto tem links secretos.
/// </summary>
public sealed class LogEmailTransport(IHostEnvironment env, ILogger<LogEmailTransport> logger) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (env.IsDevelopment())
            logger.LogInformation("E-mail (não enviado, SMTP não configurado) para {To}: {Subject}\n{Text}", message.To, message.Subject, message.Text);
        else
            logger.LogWarning("E-mail \"{Kind}\" não enviado: configure SMTP_HOST e EMAIL_FROM.", message.Kind);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Fila de e-mails: a requisição só enfileira e responde na hora (o tempo de resposta não revela
/// se o e-mail existe, e um SMTP lento não trava o site). <see cref="EmailBackgroundSender"/> envia.
/// </summary>
public sealed class EmailQueue(ILogger<EmailQueue> logger)
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public void Enqueue(EmailMessage message)
    {
        if (!_channel.Writer.TryWrite(message))
            logger.LogError("Fila de e-mails cheia: \"{Kind}\" descartado.", message.Kind);
    }

    public ChannelReader<EmailMessage> Reader => _channel.Reader;
}

/// <summary>Envia os e-mails da fila, tentando de novo quando o provedor falha.</summary>
public sealed class EmailBackgroundSender(EmailQueue queue, IEmailTransport transport, ILogger<EmailBackgroundSender> logger) : BackgroundService
{
    private static readonly TimeSpan[] Retries = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await transport.SendAsync(message, stoppingToken);
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt >= Retries.Length)
                    {
                        logger.LogError(ex, "Não foi possível enviar o e-mail \"{Kind}\" depois de {Attempts} tentativas.", message.Kind, attempt + 1);
                        break;
                    }
                    logger.LogWarning(ex, "Falha ao enviar o e-mail \"{Kind}\"; tentando de novo.", message.Kind);
                    await Task.Delay(Retries[attempt], stoppingToken);
                }
            }
        }
    }
}

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services)
    {
        services.AddSingleton<EmailSettings>();
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailTransport>(sp => sp.GetRequiredService<EmailSettings>().Enabled
            ? ActivatorUtilities.CreateInstance<SmtpEmailTransport>(sp)
            : ActivatorUtilities.CreateInstance<LogEmailTransport>(sp));
        services.AddHostedService<EmailBackgroundSender>();
        return services;
    }
}
