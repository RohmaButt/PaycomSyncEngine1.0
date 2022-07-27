using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Afiniti.Paycom.JiraEngine.Services
{
    public class JiraWebHookService
    {
    }

    public static class Notifications
    {
        public static void SendCallReceptionNotification(JiraIssueRequestModel issueModel, string additionalText = "")
        {
            var fCaller = issueModel.Jira_Issue.Issue_Key_And_Value.Where(m => m.Column_Name == "summary").Select(m => m.Column_Value).FirstOrDefault();
            var fTime = issueModel.Jira_Issue.Issue_Key_And_Value.Where(m => m.Column_Name == "customfield_15000").Select(m => m.Column_Value).FirstOrDefault();
            var fLocation = issueModel.Jira_Issue.Issue_Key_And_Value.Where(m => m.Column_Name == "customfield_15001").Select(m => m.Column_Value).FirstOrDefault();
            var fPurpose = issueModel.Jira_Issue.Issue_Key_And_Value.Where(m => m.Column_Name == "description").Select(m => m.Column_Value).FirstOrDefault();

            if (!string.IsNullOrEmpty(fCaller))
            {
                List<string> lMessage = new List<string>();
                lMessage.Add("Afiniti Global Reception - Call from " + fCaller + " " + additionalText);
                lMessage.Add(fTime);
                lMessage.Add(fCaller);
                lMessage.Add(fLocation);
                lMessage.Add(fPurpose);
                lMessage.Add(issueModel.CallerNumber);
                lMessage.Add(DateTime.Now.Year.ToString());

                SendNotificationEmail(issueModel.EmailAssignee, "Afiniti Global Reception - Automated Message", lMessage);
            }
        }

        internal static void SendNotificationEmail(string pEmail, string pTitle, List<string> pMessage)
        {
            Task.Factory.StartNew(() => NotificationEmail(pEmail, pTitle, pMessage));
        }

        private static void NotificationEmail(string pEmail, string pTitle, List<string> pMessage)
        {
            try
            {
                ApplicationTrace.Log("Send email service request", Framework.LoggingTracing.Status.Started);
                ApplicationTrace.Log(string.Format("Email address filtered -- {0}", pEmail), Framework.LoggingTracing.Status.Started);

                WebHookObject<GeneralNotification> nObject = new WebHookObject<GeneralNotification>();
                nObject.CallerApp = "GRA";
                nObject.Action = "GRACallNotification";
                nObject.UTC_Time = string.Concat(DateTime.Now.ToString("yyyy-MM-dd"), "T", DateTime.Now.Hour.ToString("00"), ":", DateTime.Now.Minute.ToString("00"), ":", DateTime.Now.Second.ToString("00"), ".", DateTime.Now.Millisecond.ToString(),
                    TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours.ToString("00"), "00");

                nObject.Data = new GeneralNotification()
                {
                    emailUsers = pEmail,
                    emailTemplate = "GRACallNotification",
                    emailSubject = pTitle,
                    emailFrom = "",
                    notificationMessage = pTitle,
                    emailBcc = ""
                };

                nObject.Data.emailTemplateValues = pMessage;
                SendWebHooks(nObject);

                ApplicationTrace.Log("Send email service request", Framework.LoggingTracing.Status.Completed);
            }
            catch (Exception ex)
            {
                ExceptionHandling.LogException(ex, "DraftClasses_NotificationEmail");
            }
        }

        internal static async void SendWebHooks<T>(WebHookObject<T> pObject) where T : class
        {
            try
            {
                //TODO
                var uri = new Uri(JiraEngineConfigService.WebHookServiceUri);

                MemoryStream objectStream = new MemoryStream();
                DataContractJsonSerializer objectSerialize = new DataContractJsonSerializer(typeof(WebHookObject<T>));
                objectSerialize.WriteObject(objectStream, pObject);

                objectStream.Position = 0;
                StreamReader objectRead = new StreamReader(objectStream);
                string objectJSON = objectRead.ReadToEnd();

                var stringContent = new StringContent(objectJSON, Encoding.UTF8, "application/json");
                using (var client = new HttpClient())
                {
                    string pCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(JiraEngineConfigService.WebHookServiceCred));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", pCredentials);
                    var messReturn = await client.PostAsync(uri, stringContent);
                    if (!messReturn.IsSuccessStatusCode)
                    {
                        throw new Exception(messReturn.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandling.LogException(ex, "DraftClasses_SendWebHooks");
            }
        }
    }

}