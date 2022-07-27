using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.JiraEngine.JiraHelpers;
using Afiniti.Paycom.Shared.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Status = Afiniti.Framework.LoggingTracing.Status;

namespace Afiniti.Paycom.JiraEngine.Services
{
    public class JiraComponentService
    {
        internal bool AddComponent(string token, string pKey, string pComponent)
        {
            using (HttpClient client = JiraHelper.GetJIRAValidContext(token))
            {
                ApplicationTrace.Log("In AddComponent - API", Status.Completed);
                StringBuilder sbComponents = new StringBuilder();
                sbComponents.Append("{\"update\" : {\"components\" : [{\"set\" : [").Append("{\"name\" : \"" + pComponent + "\"}").Append("]}]}}");

                #region Model
                var model = new RootObjectForAddingComponent
                {
                    update = new AddCompo
                    {
                        components = new List<AddComponentObj>
                        {
                         new AddComponentObj {
                             set = new List<SetAdd> { new SetAdd { name = pComponent } }
                        }
                      }
                    }
                };
                var userContents = JsonConvert.SerializeObject(model);
                #endregion

                var stringContent = new StringContent(userContents, Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = client.PutAsync("issue/" + pKey, stringContent).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        ApplicationTrace.Log("AddComponent -- Response Received", Status.Completed);
                        return true;
                    }
                    else
                    {
                        ApplicationTrace.Log(response.StatusCode.ToString(), Status.Failed);
                        ApplicationTrace.Log(response.ReasonPhrase, Status.Failed);
                        ApplicationTrace.Log(response.Content.ReadAsStringAsync().Result, Status.Failed);

                        return false;
                    }
                }
                catch (Exception exc)
                {
                    ExceptionHandling.LogException(exc, "AddComponentJIRA");
                    return false;
                }
            }
        }
        internal bool RemoveComponent(string token, string pKey, string existingCompo)
        {
            using (HttpClient client = JiraHelper.GetJIRAValidContext(token))
            {
                #region Model
                var model = new RootObjectForUpdatingComponent
                {
                    update = new Update
                    {
                        components = new List<ComponentObj>
                        {
                         new ComponentObj {
                             remove =new Set {name= existingCompo  },
                        }
                      }
                    }
                };
                #endregion

                try
                {
                    var userContents = JsonConvert.SerializeObject(model);
                    var stringContent = new StringContent(userContents, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PutAsync("issue/" + pKey, stringContent).Result;
                    return response.IsSuccessStatusCode;
                }
                catch (Exception exc)
                {
                    ExceptionHandling.LogException(exc, "RemoveComponentJIRA");
                    return false;
                }
            }
        }

        internal bool AddComponentToJiraIssue(string token, string pKey, string pComponent)
        {
            using (HttpClient client = JiraHelper.GetJIRAValidContext(token))
            {
                ApplicationTrace.Log("In AddComponent - API", Status.Completed);
                StringBuilder sbComponents = new StringBuilder();
                sbComponents.Append("{\"update\" : {\"components\" : [{\"set\" : [").Append("{\"name\" : \"" + pComponent + "\"}").Append("]}]}}");

                #region Model
                var model = new RootObjectForAddingComponent
                {
                    update = new AddCompo
                    {
                        components = new List<AddComponentObj>
                        {
                         new AddComponentObj {
                             set = new List<SetAdd> { new SetAdd { name = pComponent } }
                        }
                      }
                    }
                };
                var userContents = JsonConvert.SerializeObject(model);
                #endregion

                var stringContent = new StringContent(userContents, Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = client.PutAsync("issue/" + pKey, stringContent).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        ApplicationTrace.Log("AddComponent -- Response Received", Framework.LoggingTracing.Status.Completed);
                        return true;
                    }
                    else
                    {
                        ApplicationTrace.Log(response.StatusCode.ToString(), Framework.LoggingTracing.Status.Failed);
                        ApplicationTrace.Log(response.ReasonPhrase, Framework.LoggingTracing.Status.Failed);
                        ApplicationTrace.Log(response.Content.ReadAsStringAsync().Result, Framework.LoggingTracing.Status.Failed);
                        return false;
                    }
                }
                catch (Exception exc)
                {
                    ExceptionHandling.LogException(exc, "AddComponentJIRA");
                    return false;
                }
            }
        }
    }
}