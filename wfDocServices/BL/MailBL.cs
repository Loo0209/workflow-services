
using NLog;
using RabbitMQ.Client;
using System.Text;
using System.Data;
using System.Net.Mail;
using System.Net;
using RabbitMQ.Client.Events;
using wfDocServices.Constants;
using wfDocServices.Entity;
using Newtonsoft.Json;
using wfDocServices.DL;
using wfDocServices.Models;
using wfDocServices.EnumDefine.ApprovalRules;
namespace wfDocServices.BL
{
    public class MailBL : IDisposable
    {
        private static UtilityBL utility = new UtilityBL();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public async Task ActionNotificationAutoEmail()
        {
            try
            {
                // 1. Get the singleton instance
                var rabbitqueueBL = RabbitqueueBL.Instance;

                // 2. Get a channel (it will auto-initialize or reconnect if needed)
                var channel = await rabbitqueueBL.RabbitqueueChannel();
                UtilityBL utilitylogic = new UtilityBL();
                // 3. Declare queue
                await channel.QueueDeclareAsync(
                    queue: RabbitqueueStringType.ActionNotify + utilitylogic.ReadConfigFile(ConfigConstants.env),
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // 4. Create a consumer
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    MailTo(message); // your mail sending logic

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                  //  await channel.BasicConsumeAsync(queue: RabbitqueueStringType.ActionNotify, autoAck: false, consumer: consumer);
                };

                // 5. Start consuming

                await channel.BasicConsumeAsync(queue: RabbitqueueStringType.ActionNotify + utilitylogic.ReadConfigFile(ConfigConstants.env), autoAck: false, consumer: consumer);

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

        }
        public async Task VerificationCodeEmail()
        {
            try
            {
                // 1. Get the singleton instance
                var rabbitqueueBL = RabbitqueueBL.Instance;

                // 2. Get a channel (it will auto-initialize or reconnect if needed)
                var channel = await rabbitqueueBL.RabbitqueueChannel();
                UtilityBL utilitylogic = new UtilityBL();
                // 3. Declare queue
                await channel.QueueDeclareAsync(
                    queue: RabbitqueueStringType.VerificationCode+ utilitylogic.ReadConfigFile(ConfigConstants.env),
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // 4. Create a consumer
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    VerificationCodeMailTo(message); // your mail sending logic

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                // 5. Start consuming

                await channel.BasicConsumeAsync(queue: RabbitqueueStringType.VerificationCode+ utilitylogic.ReadConfigFile(ConfigConstants.env), autoAck: false, consumer: consumer);

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

        }
        private async void VerificationCodeMailTo(string message)
        {
            RabbitqueueAutoEmailVCode actionNotificationjson = JsonConvert.DeserializeObject<RabbitqueueAutoEmailVCode>(message);
            bool IsSuccess = false;

            if(actionNotificationjson.type== RabbitqueueStringType.ActivateAcc)
            {
                var result = await MailVerificationCodeTo(actionNotificationjson.code, actionNotificationjson.email);
                IsSuccess = result.IsSuccess;
            }
            else if (actionNotificationjson.type == RabbitqueueStringType.ForgetPass)
            {
                var result = await MailResetPwdVerifyCodeTo(actionNotificationjson.code, actionNotificationjson.email);
                IsSuccess = result.IsSuccess;
            }
            else
            {

            }
            if (!IsSuccess)
            {
                logger.Error($"EMAIL FAIL {message}");
            }

        }
        private async void MailTo(string message)
        {
            ActionNotificationjson actionNotificationjson= JsonConvert.DeserializeObject<ActionNotificationjson>(message);
            var dapperRepo = new DapperRepository<Approval_Workflow_Notification_Info>();
            List<FilterOption> searchcriteriaRequestlist = new List<FilterOption>();
            FilterOption filterRequest = new FilterOption();
            filterRequest.Key = "request_id";
            filterRequest.Operator = "=";
            filterRequest.Value = actionNotificationjson.request_id.ToString();
            searchcriteriaRequestlist.Add(filterRequest);

            var request = dapperRepo.GetAll(searchcriteriaRequestlist).ToList();

            var dapperRepoUser = new DapperRepository<Users>();
            foreach (var r in request)
            {
                if(r.approval_type == ApprovalTypeEnum.Individual.ToString())
                {
                    
                    var userentity = dapperRepoUser.GetByKey("name", r.user_group_name);
                    if (userentity == null)
                    {
                        logger.Error($"User {r.user_group_name} not found");
                        continue;
                    }
                    var IsSuccessResult = await Notification(r.tenant_code, r.company_code, r.doc_code, r.docnum, r.user_group_name, userentity.email);
                    if (!IsSuccessResult.IsSuccess)
                    {
                        logger.Error($"Email fail to send {r.user_group_name}");
                    }
                }

                if(r.approval_type == ApprovalTypeEnum.Group.ToString())
                {
                    var dapperRepoGroup = new DapperRepository<Group_User_Info>();
                    List<FilterOption> searchcriteriaGrouplist = new List<FilterOption>();


                    FilterOption groupfilter = new FilterOption();
                    groupfilter.Key = "group_name";
                    groupfilter.Operator = "=";
                    groupfilter.Value = r.user_group_name.ToString();

                    FilterOption groupfilter2 = new FilterOption();
                    groupfilter2.Key = "company_code";
                    groupfilter2.Operator = "=";
                    groupfilter2.Value = r.company_code.ToString();

                    FilterOption groupfilter3 = new FilterOption();
                    groupfilter3.Key = "tenant_id";
                    groupfilter3.Operator = "=";
                    groupfilter3.Value = r.tenant_id.ToString();

                    searchcriteriaGrouplist.Add(groupfilter);
                    searchcriteriaGrouplist.Add(groupfilter2);
                    searchcriteriaGrouplist.Add(groupfilter3);

                    var user_in_group_List = dapperRepoGroup.GetAll(searchcriteriaGrouplist);
                    foreach(var g in user_in_group_List)
                    {
                        var userentity = dapperRepoUser.GetByKey("name", g.username);
                        if (userentity == null)
                        {
                            logger.Error($"User {r.user_group_name} not found");
                            continue;
                        }
                        var IsSuccessResult = await Notification(r.tenant_code, r.company_code, r.doc_code, r.docnum, r.user_group_name, userentity.email);
                        if (!IsSuccessResult.IsSuccess)
                        {
                            logger.Error($"Email fail to send {r.user_group_name}");
                        }
                    }

                }
            }


        }
        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> MailVerificationCodeTo(string code,string emailTo)
        {
            string mailbody = "";
            mailbody += "Test verification code " + code;
            return await autoemail(emailTo, "TEST Activate Account Code", mailbody);
        }
        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> MailResetPwdVerifyCodeTo(string code, string emailTo)
        {
            string mailbody = "";
            mailbody += "Test verification code " + code;
            return await autoemail(emailTo, "TEST reset password", mailbody);
        }
        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> Notification(string tenant_code,string company_code,string doc_code,string docnum,string username,string emailTo)
        {
            string subject = $"Approval Request: Document {doc_code} - {docnum} from {tenant_code} : {company_code}";
            string mailbody = "";

            mailbody += $"Dear {username},<br><br>";
            mailbody += $"You have a new approval request for the following document:<br>";
            mailbody += $"<strong>Document:</strong> {doc_code} - {docnum}<br>";
            mailbody += $"<strong>Tenant:</strong> {tenant_code}<br>";
            mailbody += $"<strong>Company:</strong> {company_code}<br><br>";
            mailbody += $"Please log in to <a href='https://www.xxxxx.com'>www.xxxxx.com</a> to review and take the necessary action.<br><br>";
            mailbody += $"Best regards,<br>";
            mailbody += $"SD Auto Email Bot<br><br>";
            mailbody += $"<em>This is an automated email. Please do not reply.</em>";

            return await autoemail(emailTo, subject, mailbody);
        }


        private async Task<(bool IsSuccess, IEnumerable<string> Errors)> autoemail(string emailTo, string subject,string mailbody)
        {
            bool IsSuccess = false;
            List<string> ErrorList = new List<string>();
            IEnumerable<string> Errors;
            try
            {
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "smtp.gmail.com";
                smtpClient.Port = 587;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(utility.ReadConfigFile(ConfigConstants.autoemail), utility.ReadConfigFile(ConfigConstants.autoemailpass));
                smtpClient.EnableSsl = true;

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(utility.ReadConfigFile(ConfigConstants.autoemail));
                mailMessage.To.Add(emailTo);

                mailMessage.Subject = subject;
                mailMessage.Body = mailbody;
                mailMessage.IsBodyHtml = true;

                smtpClient.Send(mailMessage);

                //report.Dispose();
                smtpClient.Dispose();
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                ErrorList.Add(ex.Message);
                IsSuccess = false;
            }
            Errors = ErrorList;
            return (IsSuccess,Errors);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //connection.Close();
                if (utility != null)
                {
                    utility.Dispose();
                }
            }
        }
    }
    
}
