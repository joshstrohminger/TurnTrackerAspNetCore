using System;
using System.Linq;
using System.Threading.Tasks;
using Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TurnTrackerAspNetCore.Services
{
    public class AuthMessageSenderOptions
    {
        //public string SendGridUser { get; set; }
        public string SendGridKey { get; set; }
    }

    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string subject, string message, string category, params string[] destinations);
    }

    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }

    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public AuthMessageSender(IOptions<AuthMessageSenderOptions> optionsAccessor, IConfiguration config,
            ILoggerFactory loggerFactory)
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<AuthMessageSender>();
            Options = optionsAccessor.Value;
        }

        public AuthMessageSenderOptions Options { get; } //set only via Secret Manager

        public async Task<bool> SendEmailAsync(string subject, string message, string category, params string[] destinations)
        {
            if (destinations.Length == 0)
            {
                _logger.LogError($"No destinations provided to {nameof(SendEmailAsync)}");
                return false;
            }

            var section = _config.GetSection("Mail").GetSection(category);
            var fromAddress = section["Address"];
            var fromName = section["Name"];
            if (null == fromAddress || null == fromName)
            {
                _logger.LogError($"Couldn't find 'Address' and 'Name' for email category '{category}'");
                return false;
            }

            var myMessage = new SendGrid.SendGridMessage
            {
                From = new System.Net.Mail.MailAddress(fromAddress, fromName),
                Subject = subject,
                Text = message,
                Html = message
            };
            foreach (var to in destinations)
            {
                myMessage.AddTo(to);
            }

            var transportWeb = new SendGrid.Web(Options.SendGridKey);
            try
            {
                await transportWeb.DeliverAsync(myMessage);
                return true;
            }
            catch (InvalidApiRequestException e)
            {
                _logger.LogError(EventIds.EmailError, string.Join(Environment.NewLine, new[] {e.Message}.Union(e.Errors ?? new string[0])));
            }
            catch (Exception e)
            {
                _logger.LogError(EventIds.EmailErrorUnknown, e, $"Category '{category}', Subject '{subject}'");
            }
            return false;
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
