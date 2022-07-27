using Afiniti.Paycom.DAL;
using System.Collections.Generic;
using System.Linq;

namespace Afiniti.PaycomEngine.Services
{
    public class ConfigurationSettingDTO : ConfigurationSetting
    {
        public List<ConfigurationSettingDetailDTO> Children { get; set; }
    }
    public class ConfigurationSettingDetailDTO : ConfigurationSettingDetail
    {
    }
    public static class EngineAPIConfigService
    {
        public static List<ConfigurationSettingDTO> EngineAPIConfigs { get; set; }
        public static bool Level1Logs { get; set; }
        public static bool Level2Logs { get; set; }
        public static string CrowdTokenURL { get; set; }
        public static string DefaultPath { get; set; }
        public static string WebHookServiceUri { get; set; }
        public static string WebHookServiceCred { get; set; }
        public static string WebHookFromEmail { get; set; }
        public static void SetAPIConfigs()
        {
            PullEngineService pullEngineService = new PullEngineService();
            EngineAPIConfigs = pullEngineService.GetEngineConfigurations();

            Level1Logs = EngineAPIConfigs.FirstOrDefault(x => x.ConfigAppEvent == "Level1Logs").IsActive == true ? true : false;
            Level2Logs = EngineAPIConfigs.FirstOrDefault(x => x.ConfigAppEvent == "Level2Logs").IsActive == true ? true : false;
            CrowdTokenURL = EngineAPIConfigs.FirstOrDefault(x => x.IsActive == true && x.ConfigAppEvent == "CrowdTokenURL").ConfigValue;
            DefaultPath = EngineAPIConfigs.FirstOrDefault(x => x.IsActive == true && x.ConfigAppEvent == "DefaultPath").ConfigValue;
            WebHookFromEmail = EngineAPIConfigs.FirstOrDefault(x => x.IsActive == true && x.ConfigAppEvent == "WebHooks").Email;
            WebHookServiceUri = EngineAPIConfigs.FirstOrDefault(x => x.IsActive == true && x.ConfigAppEvent == "WebHooks").Children.FirstOrDefault(x => x.IsActive == true && x.ParamName == "WebHookServiceUri").ParamValue;
            WebHookServiceCred = EngineAPIConfigs.FirstOrDefault(x => x.IsActive == true && x.ConfigAppEvent == "WebHooks").Children.FirstOrDefault(x => x.IsActive == true && x.ParamName == "WebHookServiceCred").ParamValue;
        }
    }
}