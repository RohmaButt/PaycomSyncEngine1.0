using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.Shared.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Status = Afiniti.Framework.LoggingTracing.Status;

namespace PaycomScheduler
{
    class Program
    {
        static readonly string WebApiBaseURL = ConfigurationManager.AppSettings["PaycomApiBaseURL"];
        private ConfigActivityResponseModel TriggerPayComEngineEventAsync(string itemName)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string baseUri = $"{WebApiBaseURL}/{itemName}";
                    var stringContent = new StringContent("", UnicodeEncoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var responseMessage = client.PostAsync(baseUri, stringContent).Result;
                    var isSuccess = responseMessage.IsSuccessStatusCode;
                    if (isSuccess)
                    {
                        var response = responseMessage.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<ConfigActivityResponseModel>(response.Result);
                    }
                    else
                    {
                        throw new Exception(string.Concat(responseMessage.StatusCode.ToString(), " -- ", WebApiBaseURL));
                    }
                }
                catch (Exception exc)
                {
                    ExceptionHandling.LogException(exc, "CreateCrowdTokenByUserName");
                }
            }
            return null;
        }

        private static List<string> ValidationsListToStopProcess = new List<string>() {
            "Another process is already in progress. Please wait",
            "Completed with source file does not exist",
            "Error occured while file transfer to Processing folder",//Targetted Paycom File is opened somewhere
            "Error in finding Processing folder",
            "Error in parsing source file. Please check Paycom file"// Any mapped column is missing from Excel
            ,"Error Validation failure for mandatory data of downstreams"// validations for downstreams generations
            ,"Error "
        };
        static void Main(string[] args)
        {
            ApplicationTrace.Log("PaycomEngineScheduler", Status.Started);
            try
            {
                Program program = new Program();
                Console.WriteLine("STARTED - uploading Paycom data");
                ApplicationTrace.Log("uploading for Paycom Data", Status.Started);
                var gTask = Task.Factory.StartNew(() => program.TriggerPayComEngineEventAsync("PullAndPushDataInDB"))
                         .ContinueWith(m =>
                         {
                             if (m.Status == TaskStatus.Running)
                             {
                                 //do nothing
                             }
                             else if (m.Status == TaskStatus.Faulted)
                             {
                                 if (m.Exception != null)
                                 {
                                     Console.WriteLine("ENDED - Process");
                                     ApplicationTrace.Log("PaycomEngineScheduler Faulted", Status.Failed);
                                     ExceptionHandling.LogException(m.Exception.GetBaseException(), "PaycomEngineScheduler");
                                 }
                                 Environment.Exit(1);
                             }
                             else if (m.Status == TaskStatus.RanToCompletion)
                             {
                                 if (!ValidationsListToStopProcess.Contains(m.Result.ResponseDescription) && !m.Result.ResponseDescription.Contains("Completed with file has been transfered. Warning: File has not been processed. Delta"))
                                 {
                                     ApplicationTrace.Log("Uploading for Paycom Data", m.Result.ToString(), Status.Completed);
                                     Console.WriteLine("ENDED - uploading Paycom data");

                                     //validations addition
                                     Console.WriteLine("STARTED - validation for downstreams generation");
                                     ApplicationTrace.Log("STARTED - validation for downstreams", Status.Started);
                                     var response = program.TriggerPayComEngineEventAsync("ValidationsForDS");

                                     if (!ValidationsListToStopProcess.Contains(m.Result.ResponseDescription))
                                     {
                                         ApplicationTrace.Log("ENDED - validation for downstreams", response.ResponseDescription.ToString(), Status.Completed);
                                         Console.WriteLine("ENDED - validation for downstreams generation");

                                         Console.WriteLine("STARTED - downstreaming for Exchange");
                                         ApplicationTrace.Log("STARTED - Downstreaming for Exchange", Status.Started);
                                         response = program.TriggerPayComEngineEventAsync("PushDownStreamForExchange");
                                         ApplicationTrace.Log("ENDED - Downstreaming for Exchange", response.ResponseDescription.ToString(), Status.Completed);
                                         Console.WriteLine("ENDED - downstreaming for Exchange");

                                         Console.WriteLine("STARTED - downstreaming for Certify");
                                         ApplicationTrace.Log("STARTED - Downstreaming for Certify", Status.Started);
                                         response = program.TriggerPayComEngineEventAsync("PushDownStreamForCertify");
                                         ApplicationTrace.Log("ENDED - Downstreaming for Certify", response.ResponseDescription.ToString(), Status.Completed);
                                         Console.WriteLine("ENDED - downstreaming for Certify");

                                         //Everbridge
                                         Console.WriteLine("STARTED - downstreaming for Everbridge");
                                         ApplicationTrace.Log("STARTED - Downstreaming for Everbridge", Status.Started);
                                         response = program.TriggerPayComEngineEventAsync("PushDownStreamForEverbridge");
                                         ApplicationTrace.Log("ENDED - Downstreaming for Everbridge", response.ResponseDescription.ToString(), Status.Completed);
                                         Console.WriteLine("ENDED - downstreaming for Everbridge");

                                         //HeadCountRpt
                                         Console.WriteLine("STARTED - generating GSD Head Count Report");
                                         ApplicationTrace.Log("STARTED - generating GSD Head Count Report", Status.Started);
                                         response = program.TriggerPayComEngineEventAsync("PushHeadCountReport");
                                         ApplicationTrace.Log("ENDED - generating GSD Head Count Report", response.ResponseDescription.ToString(), Status.Completed);
                                         Console.WriteLine("ENDED - generating GSD Head Count Report");

                                         ApplicationTrace.Log("PaycomEngineScheduler Completed", Status.Completed);
                                     }
                                     else
                                     {
                                         ApplicationTrace.Log("ENDED- validation for downstreams", response.ResponseDescription.ToString(), Status.Failed);
                                         Console.WriteLine("ENDED - validation for downstreams generation" + m.Result.ResponseDescription);
                                         ApplicationTrace.Log("PaycomEngineScheduler Faulted", m.Result.ResponseDescription, Status.Failed);
                                     }
                                 }
                                 else
                                 {
                                     ApplicationTrace.Log("ENDED - uploading Paycom data", m.Result.ToString(), Status.Completed);
                                     Console.WriteLine("ENDED - Process " + m.Result.ResponseDescription);
                                     ApplicationTrace.Log("PaycomEngineScheduler Faulted", m.Result.ResponseDescription, Status.Failed);
                                 }
                                 Environment.Exit(1);
                             }
                             else if (m.Status == TaskStatus.Canceled)
                             {
                                 ApplicationTrace.Log("PaycomEngineScheduler Canceled", Status.Failed);
                                 Environment.Exit(1);
                             }
                         }, TaskContinuationOptions.None);
                gTask.Wait();
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PaycomEngineScheduler", Status.Failed);
                ExceptionHandling.LogException(ex, "PaycomEngineScheduler_" + string.Join(" ", args));
            }
            ApplicationTrace.Log("PaycomEngineScheduler", Status.Completed);

        }
    }
}
