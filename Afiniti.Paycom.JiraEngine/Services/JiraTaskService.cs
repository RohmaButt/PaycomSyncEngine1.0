using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.JiraEngine.JiraHelpers;
using Afiniti.Paycom.Shared.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Status = Afiniti.Framework.LoggingTracing.Status;

namespace Afiniti.Paycom.JiraEngine.Services
{
    public class JiraTaskService
    {
        public ResponseModel AddNewTaskToJIRA(string token, Jira_Issue jiraIssue)
        {
            var issueStr = JiraHelper.CreateJSONString(jiraIssue);
            using (HttpClient client = JiraHelper.GetJIRAValidContext(token))
            {
                var stringContent = new StringContent(issueStr, Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("issue/", stringContent).Result;
                var model = new ResponseModel { Success = response.IsSuccessStatusCode };
                string result = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    model.Data = JsonConvert.DeserializeObject<Issue>(result);
                    return model;
                }
                else
                {
                    model.Data = JsonConvert.DeserializeObject<ErrorRootObject>(result);
                    return model;
                }
            }
        }
        public ResponseModel UpdateTaskToJIRA(string token, string issueKey, Jira_Issue jiraIssue)
        {
            ApplicationTrace.Log("In Put Task Api - UpdateTask", Status.Started);
            var issueStr = JiraHelper.CreateJSONString(jiraIssue);
            using (HttpClient client = JiraHelper.GetJIRAValidContext(token))
            {
                var stringContent = new StringContent(issueStr, Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PutAsync("issue/" + issueKey, stringContent).Result;
                var model = new ResponseModel
                { Success = response.IsSuccessStatusCode };
                ApplicationTrace.Log("In Put Task Api - Sending call to JIRA", Framework.LoggingTracing.Status.Started);
                string result = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    model.Data = JsonConvert.DeserializeObject<Issue>(result);
                    ApplicationTrace.Log("In Put Task Api - Back from JIRA. SUCCESS. result: " + result, Framework.LoggingTracing.Status.Completed);
                    return model;
                }
                else
                {
                    ApplicationTrace.Log("In Put Task Api - Back from JIRA. ERROR. result: " + result, Framework.LoggingTracing.Status.Completed);
                    model.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorRootObject>(result);
                    return model;
                }
            }
        }

    }
}