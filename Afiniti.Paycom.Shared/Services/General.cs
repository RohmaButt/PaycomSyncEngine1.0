using Afiniti.Paycom.Shared.Models;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.Paycom.Shared.Services
{
    public static class General
    {
        public static CrowdUserObj GetCrowdTokenAsync(string CrowdTokenURL)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var responseMessage = client.GetAsync(CrowdTokenURL).Result;
                    var isSuccess = responseMessage.IsSuccessStatusCode;
                    if (isSuccess)
                    {
                        var response = responseMessage.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<CrowdUserObj>(response.Result);
                    }
                    else
                    {
                        throw new Exception(string.Concat(responseMessage.StatusCode.ToString(), " -- ", CrowdTokenURL));
                    }
                }
                catch (Exception exc)
                {
                    ExceptionHandling.LogException(exc, "CreateCrowdTokenByUserName");
                }
            }
            return null;
        }
        public static string GetEnumDescription(string inputStr)
        {
            RunningPaycomActivity activity;
            Enum.TryParse(inputStr, true, out activity);
            System.Reflection.MemberInfo[] memInfo = activity.GetType().GetMember(activity.ToString());
            DescriptionAttribute attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);
            return attribute.Description;
        }
    }
}
