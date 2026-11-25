using System.Net;
using static EduGame.Problems.Lang;

namespace EduGame.Api.Features.Email;

/// <summary>
/// Textos dos e-mails. HTML simples, com estilos na própria tag (clientes de e-mail ignoram CSS externo),
/// sempre com uma versão em texto puro. Valores dinâmicos passam por HtmlEncode.
/// </summary>
public static class EmailTemplates
{
    public const int ResetLinkHours = 2;

    public static EmailMessage PasswordReset(string to, string link) => Build(
        to,
        Tr("Redefinir sua senha do Educa Game", "Reset your Educa Game password", "Restablece tu contraseña de Educa Game"),
        "senha-redefinir",
        title: Tr("Vamos criar uma senha nova", "Let's create a new password", "Vamos a crear una contraseña nueva"),
        paragraphs:
        [
            Tr("Recebemos um pedido para redefinir a senha da sua conta no Educa Game.",
               "We received a request to reset the password of your Educa Game account.",
               "Recibimos una solicitud para restablecer la contraseña de tu cuenta de Educa Game."),
            Tr($"Clique no botão abaixo para escolher uma senha nova. O link vale por {ResetLinkHours} horas e só pode ser usado uma vez.",
               $"Click the button below to choose a new password. The link is valid for {ResetLinkHours} hours and can only be used once.",
               $"Haz clic en el botón de abajo para elegir una contraseña nueva. El enlace vale por {ResetLinkHours} horas y solo se puede usar una vez."),
        ],
        button: (Tr("Escolher nova senha", "Choose a new password", "Elegir nueva contraseña"), link),
        footer: Tr("Não foi você? É só ignorar este e-mail: sua senha continua a mesma.",
                   "Wasn't you? Just ignore this email: your password stays the same.",
                   "¿No fuiste tú? Ignora este correo: tu contraseña sigue siendo la misma."));

    public static EmailMessage PasswordChanged(string to, string baseUrl) => Build(
        to,
        Tr("Sua senha do Educa Game foi alterada", "Your Educa Game password was changed", "Tu contraseña de Educa Game fue cambiada"),
        "senha-alterada",
        title: Tr("Senha alterada", "Password changed", "Contraseña cambiada"),
        paragraphs:
        [
            Tr("A senha da sua conta no Educa Game acabou de ser alterada.",
               "The password of your Educa Game account was just changed.",
               "La contraseña de tu cuenta de Educa Game se acaba de cambiar."),
            Tr("Por segurança, os outros aparelhos conectados vão pedir para entrar de novo.",
               "For security, other signed-in devices will ask you to sign in again.",
               "Por seguridad, los demás dispositivos conectados pedirán que vuelvas a entrar."),
        ],
        button: (Tr("Abrir o Educa Game", "Open Educa Game", "Abrir Educa Game"), $"{baseUrl}/jogar#/conta"),
        footer: Tr("Não foi você? Use \"Esqueci minha senha\" na tela de entrar para criar outra senha agora mesmo e responda este e-mail para nos avisar.",
                   "Wasn't you? Use \"Forgot my password\" on the sign-in screen to create another password right now, and reply to this email to let us know.",
                   "¿No fuiste tú? Usa \"Olvidé mi contraseña\" en la pantalla de entrar para crear otra contraseña ahora mismo y responde a este correo para avisarnos."));

    public static EmailMessage Welcome(string to, string baseUrl) => Build(
        to,
        Tr("Bem-vindo ao Educa Game", "Welcome to Educa Game", "Bienvenido a Educa Game"),
        "boas-vindas",
        title: Tr("Conta criada! 🧭", "Account created! 🧭", "¡Cuenta creada! 🧭"),
        paragraphs:
        [
            Tr("Agora o progresso das crianças fica salvo na sua conta e dá para jogar em qualquer aparelho.",
               "Now the children's progress is saved in your account and they can play on any device.",
               "Ahora el progreso de los niños queda guardado en tu cuenta y se puede jugar en cualquier dispositivo."),
            Tr("Próximos passos: crie um perfil para cada criança (só o apelido, nada de nome completo) e, se você é professor, crie uma turma e compartilhe o código.",
               "Next steps: create a profile for each child (just a nickname, no full names) and, if you're a teacher, create a class and share the code.",
               "Próximos pasos: crea un perfil para cada niño (solo el apodo, nada de nombre completo) y, si eres profesor, crea una clase y comparte el código."),
        ],
        button: (Tr("Começar", "Get started", "Empezar"), $"{baseUrl}/jogar#/perfis"),
        footer: Tr($"Este e-mail foi enviado porque esta conta foi criada no Educa Game. Termos de uso: {baseUrl}/termos · Privacidade: {baseUrl}/privacidade",
                   $"This email was sent because this account was created on Educa Game. Terms of use: {baseUrl}/en/terms · Privacy: {baseUrl}/en/privacy",
                   $"Este correo se envió porque esta cuenta se creó en Educa Game. Términos de uso: {baseUrl}/es/terminos · Privacidad: {baseUrl}/es/privacidad"));

    private static EmailMessage Build(string to, string subject, string kind, string title, string[] paragraphs, (string Label, string Url) button, string footer)
    {
        static string E(string s) => WebUtility.HtmlEncode(s);
        var html = $$"""
            <!doctype html>
            <html lang="{{HtmlTag}}">
            <body style="margin:0;padding:24px;background:#f4f6fb;font-family:Arial,Helvetica,sans-serif;color:#1d2433;">
              <div style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;padding:28px;">
                <p style="margin:0 0 16px;font-weight:bold;font-size:16px;">🧭 Educa Game</p>
                <h1 style="margin:0 0 16px;font-size:22px;">{{E(title)}}</h1>
                {{string.Concat(paragraphs.Select(p => $"<p style=\"margin:0 0 14px;font-size:15px;line-height:1.5;\">{E(p)}</p>"))}}
                <p style="margin:24px 0;">
                  <a href="{{E(button.Url)}}" style="display:inline-block;background:#2f6fde;color:#ffffff;text-decoration:none;font-weight:bold;padding:12px 20px;border-radius:10px;">{{E(button.Label)}}</a>
                </p>
                <p style="margin:0 0 8px;font-size:13px;color:#5b6478;line-height:1.5;">{{E(Tr("Se o botão não funcionar, copie este endereço no navegador:", "If the button doesn't work, copy this address into your browser:", "Si el botón no funciona, copia esta dirección en el navegador:"))}}<br><span style="word-break:break-all;">{{E(button.Url)}}</span></p>
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
