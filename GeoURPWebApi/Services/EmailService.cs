using System.Net;
using System.Net.Mail;

namespace GeoURPWebApi.Services;

public sealed class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendBookLinkAsync(string recipientEmail, string recipientName, string bookTitle, string bookUrl)
    {
        var subject = $"Tu libro solicitado: {bookTitle}";
        var body = GetBookEmailBody(recipientName, bookTitle, bookUrl);
        return await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task<bool> SendPasswordRecoveryAsync(string recipientEmail, string recipientName, string temporaryPassword)
    {
        const string subject = "Recuperacion de contrasena - GEO-URP";
        var body = GetPasswordRecoveryBody(recipientName, temporaryPassword);
        return await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task<bool> SendWelcomeEmailAsync(string recipientEmail, string recipientName)
    {
        var welcomeSubject = "¡Tu registro ha sido aprobado! - GEO-URP";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #4CAF50 0%, #388E3C 100%); color: white; padding: 30px 20px; text-align: center; }}
        .content {{ padding: 30px 20px; }}
        .button-container {{ text-align: center; margin: 30px 0; }}
        .button {{ display: inline-block; padding: 14px 28px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ text-align: center; padding: 20px; background-color: #f9f9f9; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>GEO-URP</h1>
            <p>¡Bienvenido a la comunidad!</p>
        </div>
        <div class='content'>
            <p>Hola {recipientName},</p>
            <p>Nos complace informarte que tu registro ha sido <strong>aprobado</strong> por los directores.</p>
            <p>Ya puedes iniciar sesión en nuestra plataforma utilizando tu correo institucional y la contraseña que registraste.</p>
            
            <div class='button-container'>
                <a href='https://geourp.org/login' class='button'>Iniciar Sesión</a>
            </div>
            
            <p>¡Esperamos que aproveches al máximo nuestra biblioteca digital y todas las herramientas disponibles!</p>
        </div>
        <div class='footer'>
            <p>Este es un correo automático de GEO-URP.</p>
        </div>
    </div>
</body>
</html>";
        return await SendEmailAsync(recipientEmail, welcomeSubject, body);
    }

    private async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpLogin = _configuration["Email:SmtpLogin"];
            var senderEmail = _configuration["Email:SenderEmail"];
            var senderPassword = _configuration["Email:SenderPassword"];
            var senderName = _configuration["Email:SenderName"] ?? "GeoURP";

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                _logger.LogError("Email configuration is missing");
                return false;
            }

            var loginUser = string.IsNullOrEmpty(smtpLogin) ? senderEmail : smtpLogin;

            _logger.LogInformation(
                "Sending email to {Email} using SMTP {Host}:{Port} with login {Login}",
                recipientEmail,
                smtpHost,
                smtpPort,
                loginUser);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(loginUser, senderPassword),
                Timeout = 30000
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(recipientEmail);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", recipientEmail);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP Error: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", recipientEmail);
            return false;
        }
    }

    private static string GetBookEmailBody(string recipientName, string bookTitle, string bookUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); color: white; padding: 30px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 30px 20px; }}
        .content h2 {{ color: #4CAF50; margin-top: 0; }}
        .book-info {{ background-color: #f9f9f9; padding: 20px; border-left: 4px solid #4CAF50; margin: 20px 0; border-radius: 4px; }}
        .button-container {{ text-align: center; margin: 30px 0; }}
        .button {{ display: inline-block; padding: 14px 28px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .link-box {{ background-color: #f0f0f0; padding: 15px; border-radius: 4px; word-break: break-all; font-size: 12px; color: #666; }}
        .footer {{ text-align: center; padding: 20px; background-color: #f9f9f9; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
        .footer a {{ color: #4CAF50; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>GeoURP - Biblioteca Digital</h1>
        </div>
        <div class='content'>
            <h2>Hola {recipientName}</h2>
            <p>Tu solicitud ha sido <strong>aprobada</strong>. Ya puedes acceder al libro que solicitaste.</p>

            <div class='book-info'>
                <strong style='color: #4CAF50;'>Libro:</strong><br>
                <span style='font-size: 16px;'>{bookTitle}</span>
            </div>

            <p>Haz clic en el boton de abajo para descargar o visualizar el libro:</p>

            <div class='button-container'>
                <a href='{bookUrl}' class='button'>Descargar libro</a>
            </div>

            <p style='font-size: 13px; color: #666;'>Si el boton no funciona, copia este enlace en tu navegador:</p>
            <div class='link-box'>{bookUrl}</div>

            <p style='margin-top: 30px;'>Que disfrutes tu lectura.</p>
        </div>
        <div class='footer'>
            <p>Este es un correo automatico, por favor no respondas a este mensaje.</p>
            <p>&copy; 2026 GeoURP - Grupo de Estudios Organizados de la URP</p>
            <p><a href='https://geourp.org'>www.geourp.org</a></p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetPasswordRecoveryBody(string recipientName, string temporaryPassword)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #0f4c81 0%, #1773b0 100%); color: white; padding: 30px 20px; text-align: center; }}
        .content {{ padding: 30px 20px; }}
        .password-box {{ margin: 24px 0; padding: 18px; border-radius: 8px; background: #f3f8fc; border: 1px dashed #1773b0; text-align: center; }}
        .password-label {{ display: block; font-size: 13px; color: #4a6278; margin-bottom: 8px; }}
        .password-value {{ display: inline-block; font-size: 22px; font-weight: bold; letter-spacing: 0.04em; color: #0f4c81; }}
        .footer {{ text-align: center; padding: 20px; background-color: #f9f9f9; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>GEO-URP</h1>
            <p>Recuperacion de contrasena</p>
        </div>
        <div class='content'>
            <p>Hola {recipientName},</p>
            <p>Recibimos una solicitud para recuperar tu acceso. Hemos generado una contrasena temporal para tu cuenta.</p>

            <div class='password-box'>
                <span class='password-label'>Contrasena temporal</span>
                <span class='password-value'>{temporaryPassword}</span>
            </div>

            <p>Ingresa con esta contrasena y luego cambiala desde tu perfil para mantener tu cuenta segura.</p>
            <p>Si no solicitaste esta recuperacion, puedes ignorar este correo.</p>
        </div>
        <div class='footer'>
            <p>Correo automatico de GEO-URP.</p>
        </div>
    </div>
</body>
</html>";
    }
}
