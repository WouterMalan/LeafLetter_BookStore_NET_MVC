using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using SendGrid.Helpers.Mail;

namespace Bulky.Utility
{
    public class EmailSender : IEmailSender
    {
        public string SendGridSecret { get; set; }

        public EmailSender(IConfiguration configuration)
        {
            SendGridSecret = configuration["SendGrid:SecretKey"];
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SendGrid.SendGridClient(SendGridSecret);

            //TODO: Change the email address to a real one
            var from = new EmailAddress("testingSendGrid@gmail.com", "Book Store");
            var to = new EmailAddress(email);

            var message = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);

            return client.SendEmailAsync(message);
        }
    }
}