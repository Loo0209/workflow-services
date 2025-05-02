using NLog;
using wfDocServices.BL;

namespace wfDocServices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private System.Threading.Timer emailInvoiceTimer;
        private System.Threading.Timer emailVerificationCodeTimer;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private bool emailInvoiceRunning = false;
        private bool emailVerificationCodeRunning = false;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            emailInvoiceTimer = new System.Threading.Timer(emailInvoice, null, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(2));
            emailVerificationCodeTimer = new System.Threading.Timer(emailVerificationCode, null, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(2));
            return Task.CompletedTask;
        }
        private void emailInvoice(object state)
        {
            try
            {
                if (!emailInvoiceRunning)
                {
                    emailInvoiceRunning = true;

                    DateTime startTime = DateTime.Now;
                    logger.Info($"Getting response at {startTime}. ");
                    // Perform service 3 task
                    MailBL maillogic = new MailBL();
                    maillogic.ActionNotificationAutoEmail();
                    maillogic.Dispose();

                    DateTime endTime = DateTime.Now;
                    logger.Info($"Getting response ended at {endTime}");
                    TimeSpan elapsedTime = endTime - startTime;
                    logger.Info($"Elapsed time: {elapsedTime}");

                    emailInvoiceRunning = false;
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                emailInvoiceRunning = false;
            }
        }

        private void emailVerificationCode(object state)
        {
            try
            {
                if (!emailVerificationCodeRunning)
                {
                    emailVerificationCodeRunning = true;

                    DateTime startTime = DateTime.Now;
                    logger.Info($"Getting response at {startTime}. ");
                    // Perform service 3 task
                    MailBL maillogic = new MailBL();
                    maillogic.VerificationCodeEmail();
                    maillogic.Dispose();

                    DateTime endTime = DateTime.Now;
                    logger.Info($"Getting response ended at {endTime}");
                    TimeSpan elapsedTime = endTime - startTime;
                    logger.Info($"Elapsed time: {elapsedTime}");

                    emailVerificationCodeRunning = false;
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                emailInvoiceRunning = false;
            }
        }
    }
}
