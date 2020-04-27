using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodSpot.Service
{
    public class EmailSender : IEmailSender
    {
        public EmailOptions Options { get; set; }

        public EmailSender(IOptions<EmailOptions> emailOptions)
        {
            Options = emailOptions.Value;
        }
        public Task SendEmailAsync(string email, string subject, string message)
        {
            throw new NotImplementedException();
        }

        //private Task Execute(string sendGridKey, string subject, string message, string email)
        //{
        //    var client = new SendGridClient(sendGridKey);
        //    var msg = new SendGridMessage()
        //    {
        //        From = new EmailAddress("admin@spice.com", "Spice Restaurant"),
        //        Subject = subject,
        //        PlainTextContent = message,
        //        HtmlContent = message
        //    };
        //    msg.AddTo(new EmailAddress(email));
        //    try
        //    {
        //        return client.SendEmailAsync(msg);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return null;
        //}
    }
}
