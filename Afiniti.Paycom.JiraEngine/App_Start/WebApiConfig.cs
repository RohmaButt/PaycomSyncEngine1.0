using Afiniti.Paycom.JiraEngine.Services;
using System.Web.Http;

namespace Afiniti.Paycom.JiraEngine
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            JiraEngineConfigService.SetJiraEngineAPIConfigs();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
