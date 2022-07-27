using System.Web.Http;

namespace Afiniti.Paycom.JiraEngine.Controllers
{
    public class EchoController : ApiController
    {
        public string Get()
        {
            return "Yo bro:) I am in with Jira Sync Engine";
        }
    }
}
