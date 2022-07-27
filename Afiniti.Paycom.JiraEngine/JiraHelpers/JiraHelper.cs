using Afiniti.Paycom.JiraEngine.Services;
using Afiniti.Paycom.Shared.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.Paycom.JiraEngine.JiraHelpers
{
    public static class JiraHelper
    {
        public static string CreateJSONString(Jira_Issue jIn)
        {
            StringBuilder sbJSON = new StringBuilder();
            sbJSON.Append("{").Append("\"fields\":{");
            sbJSON.Append("\"project\":").Append("{\"key\":\"").Append(jIn.Project_Key).Append("\"},");

            foreach (var gVal in jIn.Issue_Key_And_Value)
            {
                switch (gVal.Value_Type)
                {
                    case Column_Type.Cascading:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":{")
                            .Append("\"value\":").Append("\"").Append(gVal.Column_Value).Append("\",\"child\": {")
                            .Append("\"value\":").Append("\"").Append(gVal.Child_Value).Append("\"}").Append("}");
                        break;
                    case Column_Type.DatePicker:
                        DateTime pDate = new DateTime();
                        DateTime.TryParse(gVal.Column_Value, out pDate);

                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("\"").Append(pDate.ToString("yyyy-MM-dd")).Append("\"");
                        break;
                    case Column_Type.DateAndTime:
                        DateTime tDate = new DateTime();
                        DateTime.TryParse(gVal.Column_Value, out tDate);

                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("\"")
                            .Append(string.Concat(tDate.ToString("yyyy-MM-dd"), "T00:00:00.0", TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").GetUtcOffset(tDate).Hours.ToString("00"), "00")).Append("\"");

                        // .Append(string.Concat(tDate.ToString("yyyy-MM-dd"), "T00:00:00.0", tDate.AddHours(TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").GetUtcOffset(tDate).Hours).ToString("hh:mm:ss.s"), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").GetUtcOffset(tDate).Hours.ToString("00"), "00")).Append("\"");

                        break;
                    case Column_Type.FreeText:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("\"").Append(gVal.Column_Value).Append("\"");
                        break;
                    case Column_Type.TextField:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("\"").Append(gVal.Column_Value).Append("\"");
                        break;
                    case Column_Type.URLField:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("\"").Append(gVal.Column_Value).Append("\"");
                        break;
                    case Column_Type.GroupPicker:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("{\"name\":\"").Append(gVal.Column_Value).Append("\"}");
                        break;
                    case Column_Type.Labels:
                        string[] spLabels = gVal.Column_Value.Split('|');
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":[");

                        for (int i = 0; i < spLabels.Length; i++)
                        {
                            sbJSON.Append("\"").Append(spLabels[i]).Append("\"");
                            if (i < spLabels.Length - 1)
                            {
                                sbJSON.Append(",");
                            }
                        }

                        sbJSON.Append("]");
                        break;
                    case Column_Type.MultiGroupPicker:
                        string[] spGroup = gVal.Column_Value.Split('|');
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":[");

                        for (int i = 0; i < spGroup.Length; i++)
                        {
                            sbJSON.Append("{\"name\":\"").Append(spGroup[i]).Append("\"}");
                            if (i < spGroup.Length - 1)
                            {
                                sbJSON.Append(",");
                            }
                        }

                        sbJSON.Append("]");
                        break;
                    case Column_Type.MultiSelect:
                        string[] spSelect = gVal.Column_Value.Split('|');
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":[");

                        for (int i = 0; i < spSelect.Length; i++)
                        {
                            sbJSON.Append("{\"value\":\"").Append(spSelect[i]).Append("\"}");
                            if (i < spSelect.Length - 1)
                            {
                                sbJSON.Append(",");
                            }
                        }

                        sbJSON.Append("]");
                        break;
                    case Column_Type.MultiUserPicker:
                        string[] spUsers = gVal.Column_Value.Split('|');
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":[");

                        for (int i = 0; i < spUsers.Length; i++)
                        {
                            sbJSON.Append("{\"name\":\"").Append(spUsers[i]).Append("\"}");
                            if (i < spUsers.Length - 1)
                            {
                                sbJSON.Append(",");
                            }
                        }

                        sbJSON.Append("]");
                        break;
                    case Column_Type.NumberField:
                        decimal dValue = 0;
                        decimal.TryParse(gVal.Column_Value, out dValue);

                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append(gVal.Column_Value);
                        break;
                    case Column_Type.Priority:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("{\"id\":\"").Append(gVal.Column_Value).Append("\"}");
                        break;
                    case Column_Type.ProjectPicker:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("{\"key\":\"").Append(gVal.Column_Value).Append("\"}");
                        break;
                    case Column_Type.RadioButton:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("{\"value\":\"").Append(gVal.Column_Value).Append("\"}");
                        break;
                    case Column_Type.SelectList:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("{\"value\":\"").Append(gVal.Column_Value).Append("\"}");
                        break;
                    case Column_Type.SingleVersionPicker:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("{\"name\":\"").Append(gVal.Column_Value).Append("\"}");
                        break;
                    case Column_Type.UserPicker:
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":").Append("{\"name\":\"").Append(gVal.Column_Value).Append("\"}");
                        break;
                    case Column_Type.Link:
                        string[] spEPIC = gVal.Column_Value.Split('|');
                        for (int i = 0; i < spEPIC.Length; i++)
                        {
                            sbJSON.Append("\"").Append(gVal.Column_Value).Append("\"");
                            if (i < spEPIC.Length - 1)
                            {
                                sbJSON.Append(",");
                            }
                        }
                        break;
                    case Column_Type.VersionPicker:
                        string[] spVersions = gVal.Column_Value.Split('|');
                        sbJSON.Append("\"").Append(gVal.Column_Name).Append("\":[");

                        for (int i = 0; i < spVersions.Length; i++)
                        {
                            sbJSON.Append("{\"name\":\"").Append(spVersions[i]).Append("\"}");
                            if (i < spVersions.Length - 1)
                            {
                                sbJSON.Append(",");
                            }
                        }

                        sbJSON.Append("]");
                        break;
                }

                if (gVal.Value_Type != Column_Type.Components)
                    sbJSON.Append(",");
            }

            sbJSON.Append("\"issuetype\":{").Append("\"name\":\"").Append(jIn.Project_Issue_Type).Append("\"}");
            sbJSON.Append("}}");

            return sbJSON.ToString();
        }
        public static HttpClient GetJIRAValidContext(string token)
        {
            string _JiraSiteAddress = JiraEngineConfigService.JiraBaseUrl;
            _JiraSiteAddress = string.Concat(_JiraSiteAddress, "rest/api/2/");
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            handler.CookieContainer.Add(new Uri(_JiraSiteAddress), new Cookie("crowd.token_key", token));
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = new Uri(_JiraSiteAddress);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}