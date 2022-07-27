using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.DAL;
using Afiniti.Paycom.Shared;
using Afiniti.Paycom.Shared.Models;
using Afiniti.Paycom.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using static Afiniti.Paycom.Shared.Enums;
using Status = Afiniti.Framework.LoggingTracing.Status;

namespace Afiniti.Paycom.JiraEngine.Services
{
    public class JiraPushService
    {
        private JiraDBService jiraDBService = new JiraDBService();
        private JiraComponentService jiraComponentService = new JiraComponentService();
        private JiraTaskService jiraTaskService = new JiraTaskService();
        public ResponseModel ProcessJIRATickets(List<PaycomData_MissingInfo> MissingInfoOfEmployees)
        {
            string CrowdURL = string.Empty;
            using (var context = new PaycomEngineContext())
            {
                CrowdURL = JiraEngineConfigService.CrowdTokenURL;
            }
            if (CrowdURL == string.Empty)
            {
                throw new Exception("Null Parameter(s)");
            }
            CrowdUserObj crowdUserObj = General.GetCrowdTokenAsync(CrowdURL);
            var callModel = CreateNewCallModel();
            var JiraIssueModel = CreateNewJiraIssueModel(callModel, crowdUserObj);
            if (JiraIssueModel == null || string.IsNullOrEmpty(JiraIssueModel.SSOToken))
            {
                throw new Exception("Null Parameter(s)");
            }
            #region EDIT 
            if (!string.IsNullOrEmpty(JiraIssueModel.IssueKey))
            {
                ApplicationTrace.Log("In AddNewCall - going to EditIssueFromPanel", Status.Started);
                var updateResponse = jiraTaskService.UpdateTaskToJIRA(crowdUserObj.CrowdSSOToken, JiraIssueModel.IssueKey, JiraIssueModel.Jira_Issue);
                ApplicationTrace.Log("In Put Task Api - Back from UpdateTask. RESPONSE : " + updateResponse.Success, Status.Started);

                if (updateResponse.Success && JiraIssueModel.Notify)
                {
                    ApplicationTrace.Log("In Put Task Api - Going to Send email ", Status.Started);
                    Notifications.SendCallReceptionNotification(JiraIssueModel);
                    ApplicationTrace.Log("In Put Task Api - Back from Send email ", Status.Started);
                }
                // Update To DB
                jiraDBService.UpdateTaskToDB(callModel);
                ApplicationTrace.Log("In AddNewCall - back from UpdateTaskToDb", Status.Started);
                var ssoToken = JiraIssueModel.SSOToken;
                var issueKey = JiraIssueModel.Jira_Issue.Issue_Key;
                var fComponent = JiraIssueModel.Jira_Issue.Issue_Key_And_Value.Where(m => m.Value_Type == Column_Type.Components).Select(m => m.Column_Value).FirstOrDefault();
                if (!string.IsNullOrEmpty(fComponent))
                {
                    var isRemoved = jiraComponentService.RemoveComponent(ssoToken, issueKey, JiraIssueModel.ExistingComponent);
                    var isAdded = jiraComponentService.AddComponent(ssoToken, issueKey, fComponent);
                    ApplicationTrace.Log("In AddNewCall - existing from AddNewCall  ", Status.Completed);
                    if (isAdded)
                    {
                        return new ResponseModel
                        {
                            Data = "SUCCESS",
                            Success = true,
                            Messages = new List<string> { "OK" }
                        };
                    }
                    else
                    {
                        return new ResponseModel
                        {
                            Data = "FAIL",
                            Success = true,
                            Messages = new List<string> { string.Concat("Error occurred while adding component information to issue. -- ", issueKey) }
                        };
                    }
                }
                #endregion
                return updateResponse;
            }
            Issue result = new Issue();
            var response = jiraTaskService.AddNewTaskToJIRA(JiraIssueModel.SSOToken, JiraIssueModel.Jira_Issue);
            if (response.Success && response.Data != null)
            {
                JiraPullService jiraPullService = new JiraPullService();
                result = jiraPullService.GetIssueByKeyFromJIRA(JiraIssueModel.SSOToken, ((Issue)response.Data).key);
                response.Data = result;
                Notifications.SendCallReceptionNotification(JiraIssueModel);
            }
            if (result != null)
            {
                callModel.Key = result.key;
                JiraIssueModel.Jira_Issue.Issue_Key = result.key;
                callModel.URL = string.Concat(JiraEngineConfigService.JiraBaseUrl, "browse/", result.key);
                jiraDBService.AddNewTaskToDB(callModel);

                ApplicationTrace.Log("In AddNewCall - going to AddComponentToIssue", Status.Started);
                var fComponent = JiraIssueModel.Jira_Issue.Issue_Key_And_Value.Where(m => m.Value_Type == Column_Type.Components).Select(m => m.Column_Value).FirstOrDefault();
                var responseBack = jiraComponentService.AddComponentToJiraIssue(JiraIssueModel.SSOToken, JiraIssueModel.Jira_Issue.Issue_Key, fComponent);
                ApplicationTrace.Log("In AddNewCall - back from AddComponentToIssue. response :" + responseBack, Status.Started);
            }
            return response;
        }
        private JiraIssueRequestModel CreateNewJiraIssueModel(CallModel callModel, CrowdUserObj crowdUserObj)
        {
            string issueKey = null;//"TESGRA-231";//
            string desc = "Designation: Principal Software Engineer\\nDepartment: Connect\\nPOC(Reports to): Khan, Muhammad Emal\\nCity: Karachi\\nEmail Distros to be added in / Mirroring ID: All General distros\\nMirror ID: umar.khalid";
            //CallModel callModel = new CallModel()
            //{
            //    IssueKey = "TESGRA-231",// null,
            //    Notify = false,
            //    AssigneeName = "rohma.butt@afiniti.com",
            //    CallType = "Call from Vendor",
            //    ExistingComponent = null,
            //    CallbackNumber = "+923225658915",
            //    CallPurpose = desc,
            //    CallerLocation = "Lahore",
            //    CallerName = "baqer",
            //    DateTime = DateTime.Now,
            //    Key = null,
            //    URL = null,
            //};
            JiraIssueRequestModel JiraIssueModel = new JiraIssueRequestModel
            {
                SSOToken = crowdUserObj.CrowdSSOToken,
                EmailAssignee = "rohma.butt@afiniti.com",
                CallerNumber = "+923225658915",
                ExistingComponent = null,
                Jira_Issue = new Jira_Issue
                {
                    Issue_Key = issueKey,
                    Project_Key = JiraEngineConfigService.JIRAProjectKey,
                    Project_Issue_Type = "Task",//"Service Request",
                    Issue_Key_And_Value = jiraDBService.GetTaskColumns(callModel)
                },
                Notify = false,
                IssueKey = issueKey,
            };
            return JiraIssueModel;
        }
        private CallModel CreateNewCallModel()
        {
            string issueKey = null;//"TESGRA-231";//
            string desc = "Designation: Principal Software Engineer\\nDepartment: Connect\\nPOC(Reports to): Khan, Muhammad Emal\\nCity: Karachi\\nEmail Distros to be added in / Mirroring ID: All General distros\\nMirror ID: umar.khalid";
            CallModel callModel = new CallModel()
            {
                IssueKey = issueKey,
                Notify = false,
                AssigneeName = "rohma.butt@afiniti.com",
                CallType = "Call from Vendor",
                ExistingComponent = null,
                CallbackNumber = "+923225658915",
                CallPurpose = desc,
                CallerLocation = "Lahore",
                CallerName = "baqer",
                DateTime = DateTime.Now,
                Key = null,
                URL = null,
            };
            return callModel;
        }
    }
}