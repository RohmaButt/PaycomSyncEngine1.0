using Afiniti.Paycom.DAL;
using System.Collections.Generic;
using System.Linq;

namespace Afiniti.Paycom.JiraEngine.Services
{
    public static class JiraEngineConfigService
    {
        public static string JiraBaseUrl { get; set; }
        public static string JIRAProjectKey { get; set; }
        public static string EnableEncryption { get; set; }
        public static string Org { get; set; }
        public static string CrowdTokenURL { get; set; }
        public static string WebHookServiceUri { get; set; }
        public static string WebHookServiceCred { get; set; }
        public static void SetJiraEngineAPIConfigs()
        {
            JiraDBService jiraDBService = new JiraDBService();
            List<ConfigurationSettingDetail> data_ForJira = jiraDBService.GetJiraConfigs();
            JiraBaseUrl = data_ForJira.FirstOrDefault(x => x.ParamName == "JiraBaseUrl").ParamValue;
            JIRAProjectKey = data_ForJira.FirstOrDefault(x => x.ParamName == "JIRAProjectKey").ParamValue;
            EnableEncryption = data_ForJira.FirstOrDefault(x => x.ParamName == "EnableEncryption").ParamValue;
            Org = data_ForJira.FirstOrDefault(x => x.ParamName == "Org").ParamValue;
            CrowdTokenURL = jiraDBService.GetCrowdFromDB();
            WebHookServiceUri = data_ForJira.FirstOrDefault(x => x.ParamName == "WebHookServiceUri").ParamValue;
            WebHookServiceCred = data_ForJira.FirstOrDefault(x => x.ParamName == "WebHookServiceCred").ParamValue;
        }
    }
}