using Afiniti.Paycom.JiraEngine.JiraHelpers;
using Afiniti.Paycom.Shared.Models;
using Newtonsoft.Json;
using System.Net.Http;

namespace Afiniti.Paycom.JiraEngine.Services
{
    public class JiraPullService
    {
        public Issue GetIssueByKeyFromJIRA(string token, string issueKey)
        {
            using (HttpClient client = JiraHelper.GetJIRAValidContext(token))
            {
                var messge = client.GetAsync($"issue/{issueKey}").Result;
                string result = messge.Content.ReadAsStringAsync().Result;

                if (messge.IsSuccessStatusCode)
                {
                    var data = messge.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<Issue>(data);
                }
                return null;
            }
        }
    }
}