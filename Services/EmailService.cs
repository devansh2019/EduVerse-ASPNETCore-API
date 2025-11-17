using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
namespace Exam.Service
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService (IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task Execute(string email, string confirmationLink)
        {
            var apiKey = _configuration["sendGridApiKey"];
            var client = new SendGridClient(apiKey);

            // Get verified sender email from configuration
            var fromEmail = _configuration["SendGrid:FromEmail"] ?? "minamagdybushra@gmail.com";
            var fromName = _configuration["SendGrid:FromName"] ?? "Exam Platform";
            
            var from = new EmailAddress(fromEmail, fromName);
            var subject = "Exam - Account Confirmation";
            var to = new EmailAddress(email);

            var plainTextContent = $"Welcome to Exam Platform! Please confirm your email using the following link: {confirmationLink}";

            var htmlContent = $@"
        <div style='font-family:Arial; font-size:14px; color:#333'>
            <h2>Welcome to <span style='color:#0052cc'>Exam Platform</span> 🎓</h2>
            <p>Thank you for registering on our Exam system.</p>
            <p>Please click the button below to confirm your account:</p>
            <a href='{confirmationLink}' 
               style='display:inline-block; padding:10px 20px; background-color:#0052cc; color:white; text-decoration:none; border-radius:5px; margin-top:10px;'>
               Confirm My Account
            </a>
            <p>If the button doesn't work, copy and paste this link into your browser:</p>
            <p>{confirmationLink}</p>
            <br/>
            <p>Best Regards,<br/>Exam Platform Team</p>
        </div>
    ";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted && 
                response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new Exception($"Email failed to send. Status: {response.StatusCode}, Body: {body}");
            }
        }

    }
}
