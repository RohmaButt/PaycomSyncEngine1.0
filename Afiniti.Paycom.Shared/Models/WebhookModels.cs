using System.Collections.Generic;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.Paycom.Shared.Models
{
    public class WebHookObject<T>
    {
        public string CallerApp { get; set; }
        public string Action { get; set; }
        public string UTC_Time { get; set; }
        public T Data { get; set; }
    }
    public class GeneralNotification
    {
        public string emailTemplate { get; set; }
        public string emailUsers { get; set; }
        public string emailSubject { get; set; }
        public string emailCc { get; set; }
        public string emailBcc { get; set; }
        public string emailFrom { get; set; }
        public List<string> emailTemplateValues { get; set; }
        public string notificationMessage { get; set; }
        public List<string> notifyUsers { get; set; }
    }
    public class PaycomEngineStatusUpdateTemplateModel
    {
        public string ToEmail { get; set; }
        public string CcEmail { get; set; }
        public string EmailAdditionalMessage { get; set; }
        public RunningPaycomActivity PaycomActivity { get; set; }
    }
}
