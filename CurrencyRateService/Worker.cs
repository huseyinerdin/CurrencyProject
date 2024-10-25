using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Xml.Linq;

namespace CurrencyRateService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly EmailSettings _emailSettings;
        public Worker(ILogger<Worker> logger, IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now.Hour == 14 && now.Minute == 01)  // Her g�n saat 19:00'da �al���r
                {
                    string usdRate = await GetCurrencyRateAsync();
                    string emailBody = $"Bug�n�n USD Al�� Kuru: {usdRate}";

                    _logger.Log(LogLevel.Information, "mail send");
                    SendEmail(emailBody);

                    // G�n i�erisinde tekrar mail atmas�n� engellemek i�in 24 saat bekle
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                else
                {

                    _logger.Log(LogLevel.Information, "test");
                    // Zaman hen�z gelmediyse 1 dakikada bir kontrol eder
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
        public async Task<string> GetCurrencyRateAsync()
        {
            string url = "https://www.tcmb.gov.tr/kurlar/today.xml";
            using HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(url);

            // XML�i y�kleyip d�viz kurunu �ekiyoruz
            XDocument xmlDoc = XDocument.Parse(response);
            var usdRate = xmlDoc.Descendants("Currency")
                                .FirstOrDefault(c => c.Attribute("Kod")?.Value == "USD")?
                                .Element("ForexBuying")?.Value;

            return usdRate ?? "0";
        }
        public void SendEmail(string body)
        {
            var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
            {
                Port = _emailSettings.Port,
                Credentials = new System.Net.NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail),
                Subject = "G�nl�k Dolar Kuru",
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(_emailSettings.RecipientEmail);

            smtpClient.Send(mailMessage);
        }
    }
}