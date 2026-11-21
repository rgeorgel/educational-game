using System.Net;

namespace EduGame.Api.Features.Email;

/// <summary>
/// Textos dos e-mails. HTML simples, com estilos na própria tag (clientes de e-mail ignoram CSS externo),
/// sempre com uma versão em texto puro. Valores dinâmicos passam por HtmlEncode.
/// </summary>
public static class EmailTemplates
{
    public const int ResetLinkHours = 2;

    public static EmailMessage PasswordReset(string to, string link) => Build(
        to, "Redefinir sua senha do Educa Game", "senha-redefinir",
        title: "Vamos criar uma senha nova",
        paragraphs:
        [
            "Recebemos um pedido para redefinir a senha da sua conta no Educa Game.",
            $"Clique no botão abaixo para escolher uma senha nova. O link vale por {ResetLinkHours} horas e só pode ser usado uma vez.",
        ],
        button: ("Escolher nova senha", link),
        footer: "Não foi você? É só ignorar este e-mail: sua senha continua a mesma.");

    public static EmailMessage PasswordChanged(string to, string baseUrl) => Build(
        to, "Sua senha do Educa Game foi alterada", "senha-alterada",
        title: "Senha alterada",
        paragraphs:
        [
            "A senha da sua conta no Educa Game acabou de ser alterada.",
            "Por segurança, os outros aparelhos conectados vão pedir para entrar de novo.",
        ],
        button: ("Abrir o Educa Game", $"{baseUrl}/jogar#/conta"),
        footer: "Não foi você? Use \"Esqueci minha senha\" na tela de entrar para criar outra senha agora mesmo e responda este e-mail para nos avisar.");

    public static EmailMessage Welcome(string to, string baseUrl) => Build(
        to, "Bem-vindo ao Educa Game", "boas-vindas",
        title: "Conta criada! 🧭",
        paragraphs:
        [
            "Agora o progresso das crianças fica salvo na sua conta e dá para jogar em qualquer aparelho.",
            "Próximos passos: crie um perfil para cada criança (só o apelido, nada de nome completo) e, se você é professor, crie uma turma e compartilhe o código.",
        ],
        button: ("Começar", $"{baseUrl}/jogar#/perfis"),
        footer: $"Este e-mail foi enviado porque esta conta foi criada no Educa Game. Termos de uso: {baseUrl}/termos · Privacidade: {baseUrl}/privacidade");

    private static EmailMessage Build(string to, string subject, string kind, string title, string[] paragraphs, (string Label, string Url) button, string footer)
    {
        static string E(string s) => WebUtility.HtmlEncode(s);
        var html = $$"""
            <!doctype html>
            <html lang="pt-BR">
            <body style="margin:0;padding:24px;background:#f4f6fb;font-family:Arial,Helvetica,sans-serif;color:#1d2433;">
              <div style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;padding:28px;">
                <p style="margin:0 0 16px;font-weight:bold;font-size:16px;">🧭 Educa Game</p>
                <h1 style="margin:0 0 16px;font-size:22px;">{{E(title)}}</h1>
                {{string.Concat(paragraphs.Select(p => $"<p style=\"margin:0 0 14px;font-size:15px;line-height:1.5;\">{E(p)}</p>"))}}
                <p style="margin:24px 0;">
                  <a href="{{E(button.Url)}}" style="display:inline-block;background:#2f6fde;color:#ffffff;text-decoration:none;font-weight:bold;padding:12px 20px;border-radius:10px;">{{E(button.Label)}}</a>
                </p>
                <p style="margin:0 0 8px;font-size:13px;color:#5b6478;line-height:1.5;">Se o botão não funcionar, copie este endereço no navegador:<br><span style="word-break:break-all;">{{E(button.Url)}}</span></p>
                <hr style="border:0;border-top:1px solid #e3e7ef;margin:20px 0;">
                <p style="margin:0;font-size:12px;color:#5b6478;line-height:1.5;">{{E(footer)}}</p>
              </div>
            </body>
            </html>
            """;
        var text = $"""
            Educa Game

            {title}

            {string.Join("\n\n", paragraphs)}

            {button.Label}: {button.Url}

            --
            {footer}
            """;
        return new EmailMessage(to, subject, html, text, kind);
    }
}
