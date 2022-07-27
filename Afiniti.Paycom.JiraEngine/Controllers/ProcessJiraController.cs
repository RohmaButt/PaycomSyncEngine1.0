using Afiniti.Paycom.DAL;
using Afiniti.Paycom.JiraEngine.JiraHelpers;
using Afiniti.Paycom.JiraEngine.Services;
using Afiniti.Paycom.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace Afiniti.Paycom.JiraEngine.Controllers
{
    [RoutePrefix("ProcessJira")]
    public class ProcessJiraController : ApiController
    {
        private JiraPushService jiraService = new JiraPushService();
        private JiraDBService jiraDBService = new JiraDBService();
        ///// <summary>
        ///// Create JIRA Ticket
        ///// </summary>
        ///// <remarks>
        ///// Create JIRA Ticket for missing NTLogin/Email of employees
        ///// </remarks>
        ///// <returns>
        ///// returns custom response
        ///// </returns>
        /////<response code="200"></response>
        //[ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        //[Route("CreateJIRATicket")]
        //[HttpPost]
        //public async Task<IHttpActionResult> CreateJIRATicket()
        //{
        //    try
        //    {
        //        ApplicationTrace.Log("CreateJIRATicket", Status.Started);
        //        configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.JIRA.ToString(), "Started");
        //        if (configResponse != null)
        //        {
        //            if (configResponse.ResponseStatus != 1)
        //            {
        //                if (jiraService.CreateJIRATicket())
        //                {
        //                    ApplicationTrace.Log("CreateJIRATicket", Status.Completed);
        //                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.JIRA.ToString(), "Completed");
        //                }
        //                else
        //                {
        //                    ApplicationTrace.Log("CreateJIRATicket", Status.Failed);
        //                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.JIRA.ToString(), "Error in creating JIRA ticket. Contact Connect team");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            ApplicationTrace.Log("CreateJIRATicket", Status.Failed);
        //            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.JIRA.ToString(), "Error -" + configResponse.ResponseDescription);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ApplicationTrace.Log("CreateJIRATicket", Status.Failed);
        //        configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.JIRA.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
        //    }
        //    return Ok(configResponse);
        //}

        /// <summary>
        /// Post JIRA Tickets
        /// </summary>
        /// <remarks>
        /// Post JIRA Tickets for blank NTLogin and WorkEmail
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PostJIRATickets")]
        [HttpPost]
        public async Task<IHttpActionResult> PostJIRATickets()
        {
            ResponseModel response = new ResponseModel();
            try
            {
                List<PaycomData_MissingInfo> data_Scrubbed = jiraDBService.GetEmployeesWith_BlankNTLoginOrWorkEmail();
                response = jiraService.ProcessJIRATickets(data_Scrubbed);
                return Ok(response);
            }
            catch (Exception excep)
            {
                ExceptionHandling.LogException(excep, "POST-Task-API");
                //var response = new ResponseModel
                //{
                //    Data = JiraIssueModel,
                //    Success = false,
                //    Messages = new List<string> { excep.Message }
                //};
                return BadRequest(Newtonsoft.Json.JsonConvert.SerializeObject(response));
            }
        }
    }
}
