using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.Shared.Models;
using Afiniti.PaycomEngine.Polymorphics;
using Afiniti.PaycomEngine.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using static Afiniti.Paycom.Shared.Enums;
using Status = Afiniti.Framework.LoggingTracing.Status;

namespace Afiniti.PaycomEngine.Controllers
{
    [RoutePrefix("ProcessPaycom")]
    public class ProcessPaycomController : ApiController
    {
        private readonly PushEngineService PushService = new PushEngineService();
        private readonly PullEngineService PullService = new PullEngineService();
        private readonly ConfigurationActivityService configService = new ConfigurationActivityService();
        private readonly FileService fileService = new FileService();
        private ConfigActivityResponseModel configResponse = new ConfigActivityResponseModel { };

        [Route("PullDataFromExcel")]//for Pull data only
        [HttpGet]
        private async Task<string> ReadExcelFileFromPathAsync(/*ReadFileRequestModel requestModel*/)
        {
            dynamic response = "Yo bro.";
            //try
            //{
            //    ApplicationTrace.Log("ReadPayComData", Status.Started);
            //    Data_ScruberAndValidator PullService = new Data_ScruberAndValidator();
            //    string path = "D:\\test.xlsx";
            //    if (File.Exists(path))
            //    {
            //        response = PullService.PrepareDataAfterScrubbingValidations(path);
            //        ApplicationTrace.Log("ReadPayComData", Status.Completed);
            //    }
            //    else
            //    {
            //        ApplicationTrace.Log("ReadPayComData", Status.Failed);
            //        return "error";
            //    }
            //}
            //catch (Exception ex)
            //{
            //    ApplicationTrace.Log("ReadPayComData", Status.Failed);
            //}
            return JsonConvert.SerializeObject(response);
        }

        /// <summary>
        /// Pull and Push data in DB
        /// </summary>
        /// <remarks>
        /// Pull data from Excel file and Push  Data In Database
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PullAndPushDataInDB")]//Pull and Push in DB
        [HttpPost]
        public async Task<IHttpActionResult> PullAndPushDataInDBAsync()
        {
            try
            {
                ApplicationTrace.Log("PushPayComData", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        if (fileService.ValidateFileProcessedStatus() == "File was not received")
                            await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), $" The file was not received on {DateTime.Today.AddDays(-1):MMM dd yyyy}"));
                        string path = fileService.GetFilePath(RunningPaycomActivity.PushInDB.ToString());
                        if (File.Exists(path) && path != string.Empty)
                        {
                            if (fileService.TransferFile(path, "Processing"))
                            {
                                path = fileService.GetFilePath(RunningPaycomActivity.Processing.ToString());
                                if (File.Exists(path) && path != string.Empty)
                                {
                                    var DataToDump = PullService.PullPaycomDataInDTO(path);
                                    if (DataToDump == true)
                                    {
                                        var response = await PushService.DumpPaycomDataInDB(path);
                                        if (!string.IsNullOrEmpty(response))
                                        {
                                            if (fileService.TransferFile(path, "Processed"))
                                            {
                                                ApplicationTrace.Log("PushPayComData", Status.Completed);
                                                if (response.Contains("New="))
                                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), $"Paycom file has been transfered. {response.Substring(0, response.IndexOf("New"))}"));
                                                else
                                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), $"Paycom file has been transfered. {response}"));
                                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), $"Completed with file has been transfered. {response}");
                                            }
                                            else
                                            {
                                                ApplicationTrace.Log("PushPayComData", Status.Completed);
                                                await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), $"Paycom file has not been transfered, either the file is opened or in Edit mode. {response}"));
                                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), $"Completed with issue in file transfer only. {response}");
                                            }
                                        }
                                        else
                                        {
                                            ApplicationTrace.Log("PushPayComData", Status.Failed);
                                            await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), "Error: Paycom data upload process is failed. Please contact Connect"));
                                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), $"Error in dumping. Contact Connect team{response}");
                                        }
                                    }
                                    else
                                    {
                                        fileService.TransferFile(path, "Processed");
                                        ApplicationTrace.Log("PushPayComData", Status.Failed);
                                        await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), "Error: Paycom data upload process is failed. Required Excel column is missing in Paycom File. Please contact Connect"));
                                        configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), "Error in parsing source file. Please check Paycom file");
                                    }
                                }
                                else
                                {
                                    ApplicationTrace.Log("PushPayComData", Status.Failed);
                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), "Error: Paycom data upload process is failed. Processing folder is not available. Please contact Connect"));
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), "Error in finding Processing folder");
                                }
                            }
                            else
                            {
                                ApplicationTrace.Log("PushPayComData", Status.Failed);
                                await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.PushInDB.ToString(), "Error: Paycom data upload process is failed. Paycom File is in Open/Edit mode. Please contact Connect"));
                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), "Error occured while file transfer to Processing folder");
                            }
                        }
                        else
                        {
                            ApplicationTrace.Log("PushPayComData", Status.Completed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), "Completed with source file does not exist");
                        }
                    }
                }
                else
                {
                    ApplicationTrace.Log("PushPayComData", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), "Error -" + configResponse.ResponseDescription);
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushPayComData", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.PushInDB.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }

            return Ok(configResponse);
        }

        /// <summary>
        /// Validations For downStreams generation
        /// </summary>
        /// <remarks>
        /// Validations For downStreams generation
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("ValidationsForDS")]
        [HttpPost]
        public async Task<IHttpActionResult> ValidationsForDSsGenerationAsync()
        {
            try
            {
                ApplicationTrace.Log("ValidationsForDSsGeneration", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.DownstreamsValidation.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        var data = PullService.ValidateMandatoryDataForDSsGeneration();
                        if (data == 0)
                        {
                            ApplicationTrace.Log("ValidationsForDSsGeneration", Status.Completed);
                            await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.DownstreamsValidation.ToString(), "Downstream validations are passed"));
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.DownstreamsValidation.ToString(), "Completed");
                        }
                        else
                        {
                            ApplicationTrace.Log("ValidationsForDSsGeneration", Status.Completed);
                            await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.DownstreamsValidation.ToString(), "Error Validation failure for mandatory data of downstreams"));
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.DownstreamsValidation.ToString(), "Error Validation failure for mandatory data of downstreams");
                        }
                    }
                }
                else
                {
                    ApplicationTrace.Log("ValidationsForDSsGeneration", Status.Failed);
                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.DownstreamsValidation.ToString(), "Error Validation failure for mandatory data of downstreams"));
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.DownstreamsValidation.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("ValidationsForDSsGeneration", Status.Failed);
                await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.DownstreamsValidation.ToString(), "Error Validation failure for mandatory data of downstreams"));
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.DownstreamsValidation.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Push downStream for Exchange
        /// </summary>
        /// <remarks>
        /// Create a downStream file for Exchange
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDownStreamForExchange")]
        [HttpPost]
        public async Task<IHttpActionResult> PushDownStream_ExchangeAsync()
        {
            try
            {
                ApplicationTrace.Log("PushDownStream_Exchange", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Exchange.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        string path = fileService.GetFilePath(RunningPaycomActivity.Exchange.ToString());
                        if (Directory.Exists(path) && path != string.Empty)
                        {
                            var data = PullService.PullDownStreamData(RunningPaycomActivity.Exchange.ToString());
                            if (data != null && data.Count > 0)
                            {
                                if (PushService.PushDownstreams(RunningPaycomActivity.Exchange.ToString(), data, path))
                                {
                                    ApplicationTrace.Log("PushDownStream_Exchange", Status.Completed);
                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.Exchange.ToString()));
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Exchange.ToString(), "Completed");
                                }
                                else
                                {
                                    ApplicationTrace.Log("PushDownStream_Exchange", Status.Failed);
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Exchange.ToString(), "Error in Push service");
                                }
                            }
                            else
                            {
                                ApplicationTrace.Log("PushDownStream_Exchange", Status.Completed);
                            //    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.Exchange.ToString(), "( No data found to put in downtream)"));
                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Exchange.ToString(), "Completed with no data found in Pull service");
                            }
                        }
                        else
                        {
                            ApplicationTrace.Log("PushDownStream_Exchange", Status.Failed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Exchange.ToString(), "Error Downstream path does not exist");
                        }

                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDownStream_Exchange", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Exchange.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDownStream_Exchange", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Exchange.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Push downStream for Certify
        /// </summary>
        /// <remarks>
        /// Create a downStream file for Certify
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDownStreamForCertify")]
        [HttpPost]
        public async Task<IHttpActionResult> PushDownStream_CertifyAsync()
        {
            try
            {
                ApplicationTrace.Log("PushDownStream_Certify", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Certify.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        string path = fileService.GetFilePath(RunningPaycomActivity.Certify.ToString());
                        if (Directory.Exists(path) && path != string.Empty)
                        {
                            var data = PullService.PullDownStreamData(RunningPaycomActivity.Certify.ToString());
                            if (data != null && data.Count > 0)
                            {
                                if (PushService.PushDownstreams(RunningPaycomActivity.Certify.ToString(), data, path))
                                {
                                    ApplicationTrace.Log("PushDownStream_Certify", Status.Completed);
                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.Certify.ToString()));
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Certify.ToString(), "Completed");
                                }
                                else
                                {
                                    ApplicationTrace.Log("PushDownStream_Certify", Status.Failed);
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Certify.ToString(), "Error in Push service");
                                }
                            }
                            else
                            {
                                ApplicationTrace.Log("PushDownStream_Certify", Status.Completed);
                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Certify.ToString(), "Completed with no data found in Pull service");
                            }
                        }
                        else
                        {
                            ApplicationTrace.Log("PushDownStream_Certify", Status.Failed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Certify.ToString(), "Error Downstream path does not exist");
                        }

                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDownStream_Certify", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Certify.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDownStream_Certify", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Certify.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Pull data for TimeKeeping
        /// </summary>
        /// <remarks>
        ///  Pull data for TimeKeeping
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PullDataForTK")]
        [HttpGet]
        public async Task<IHttpActionResult> PullDataForTK()
        {
            List<TK_DTO> data = new List<TK_DTO>();
            try
            {
                ApplicationTrace.Log("PullDownStreamForTK", Status.Started);
                data = PullService.PullDownStreamData(RunningPaycomActivity.TK_Pull.ToString());
                if (data != null)
                {
                    ApplicationTrace.Log("PullDownStreamForTK", Status.Completed);
                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.TK_Pull.ToString(), " Data has been pulled for TimeKeeping system. "));
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Pull.ToString(), "Completed");
                }
                else
                {
                    ApplicationTrace.Log("PullDownStreamForTK", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Pull.ToString(), "Error in Pull service or no data available");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PullDownStreamForTK", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Pull.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Json(data);
        }

        /// <summary>
        /// Pull head count downStream report 
        /// </summary>
        /// <remarks>
        /// Create a head count downStream report 
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushHeadCountReport")]
        [HttpPost]
        public async Task<IHttpActionResult> PushHeadCountReport()
        {
            /*    try
                {
                    string filePath = "D:\\Certify_202010020103.xlsx";// "//afiniti.com/lahore/paycom/downstream/certify/Certify_202010020124.xlsx";
                    if (File.Exists(filePath))
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex) {
                   dynamic ff= ex;
                }
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //if (Directory.Exists(this.TempTestFolderPath))
                //{
                //    Directory.Delete(this.TempTestFolderPath, true);
                //}

                //foreach (System.Diagnostics.Process myProc in System.Diagnostics.Process.GetProcesses())
                //{
                //    if (myProc.ProcessName == "EXCEL")
                //    {
                //       myProc.Kill();
                //        break;
                //    }
                //}
                */
            try
            {
                ApplicationTrace.Log("PushHeadCountReport", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.HeadCountRpt.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        string path = fileService.GetFilePath(RunningPaycomActivity.HeadCountRpt.ToString());
                        if (Directory.Exists(path) && path != string.Empty)
                        {
                            var data = PullService.PullDownStreamData(RunningPaycomActivity.HeadCountRpt.ToString());
                            if (data != null && data.Count > 0)
                            {
                                if (PushService.PushDownstreams(RunningPaycomActivity.HeadCountRpt.ToString(), data, path))
                                {
                                    ApplicationTrace.Log("PushHeadCountReport", Status.Completed);
                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.HeadCountRpt.ToString()));
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.HeadCountRpt.ToString(), "Completed");
                                }
                                else
                                {
                                    ApplicationTrace.Log("PushHeadCountReport", Status.Failed);
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.HeadCountRpt.ToString(), "Error in Push service");
                                }
                            }
                            else
                            {
                                ApplicationTrace.Log("PushHeadCountReport", Status.Completed);
                                await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.HeadCountRpt.ToString(), "No data found to push in downtream report"));
                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.HeadCountRpt.ToString(), "Completed with no data found in Pull service");
                            }
                        }
                        else
                        {
                            ApplicationTrace.Log("PushHeadCountReport", Status.Failed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.HeadCountRpt.ToString(), "Error Downstream path does not exist");
                        }

                    }
                }
                else
                {
                    ApplicationTrace.Log("PushHeadCountReport", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.HeadCountRpt.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushHeadCountReport", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.HeadCountRpt.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }


        /// <summary>
        /// Push downStream for Everbridge
        /// </summary>
        /// <remarks>
        /// Create a downStream file for Everbridge
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDownStreamForEverbridge")]
        [HttpPost]
        public async Task<IHttpActionResult> PushDownStream_EverbridgeAsync()
        {
            try
            {
                ApplicationTrace.Log("PushDownStream_Everbridge", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.EverBridge.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        string path = fileService.GetFilePath(RunningPaycomActivity.EverBridge.ToString());
                        if (Directory.Exists(path) && path != string.Empty)
                        {
                            var data = PullService.PullDownStreamData(RunningPaycomActivity.EverBridge.ToString());
                            if (data != null && data.Count > 0)
                            {
                                if (PushService.PushDownstreams(RunningPaycomActivity.EverBridge.ToString(), data, path))
                                {
                                    ApplicationTrace.Log("PushDownStream_Everbridge", Status.Completed);
                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.EverBridge.ToString()));
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.EverBridge.ToString(), "Completed");
                                }
                                else
                                {
                                    ApplicationTrace.Log("PushDownStream_Everbridge", Status.Failed);
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.EverBridge.ToString(), "Error in Push service");
                                }
                            }
                            else
                            {
                                ApplicationTrace.Log("PushDownStream_Everbridge", Status.Completed);
                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.EverBridge.ToString(), "Completed with no data found in Pull service");
                            }
                        }
                        else
                        {
                            ApplicationTrace.Log("PushDownStream_Everbridge", Status.Failed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.EverBridge.ToString(), "Error Downstream path does not exist");
                        }

                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDownStream_Everbridge", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.EverBridge.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDownStream_Everbridge", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.EverBridge.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Push downStream for SD
        /// </summary>
        /// <remarks>
        /// Create a downStream file for SD
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDownStreamForSD")]
        [HttpPost]
        public IHttpActionResult PushDownStream_SD()
        {
            try
            {
                ApplicationTrace.Log("PushDownStream_SD", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.SD.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        string path = fileService.GetFilePath(RunningPaycomActivity.SD.ToString());
                        if (Directory.Exists(path) && path != string.Empty)
                        {
                            var data = PullService.PullDownStreamData(RunningPaycomActivity.SD.ToString());
                            if (data != null && data.Count > 0)
                            {
                                if (PushService.PushDownstreams(RunningPaycomActivity.SD.ToString(), data, path))
                                {
                                    ApplicationTrace.Log("PushDownStream_SD", Status.Completed);
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.SD.ToString(), "Completed");
                                }
                                else
                                {
                                    ApplicationTrace.Log("PushDownStream_SD", Status.Failed);
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.SD.ToString(), "Error in Push service");
                                }
                            }
                            else
                            {
                                ApplicationTrace.Log("PushDownStream_SD", Status.Completed);
                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.SD.ToString(), "Completed with no data found in Pull service");
                            }
                        }
                        else
                        {
                            ApplicationTrace.Log("PushDownStream_SD", Status.Failed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.SD.ToString(), "Error Downstream path does not exist");
                        }

                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDownStream_SD", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.SD.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDownStream_SD", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.SD.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Push downStream for AD
        /// </summary>
        /// <remarks>
        /// Create a downStream file for Active Directory
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDownStreamForAD")]
        [HttpPost]
        public async Task<IHttpActionResult> PushDownStream_ADAsync()
        {
            try
            {
                ApplicationTrace.Log("PushDownStream_AD", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.AD.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        string path = fileService.GetFilePath(RunningPaycomActivity.AD.ToString());
                        if (Directory.Exists(path) && path != string.Empty)
                        {
                            var data = PullService.PullDownStreamData(RunningPaycomActivity.AD.ToString());
                            if (data != null && data.Count > 0)
                            {
                                if (PushService.PushDownstreams(RunningPaycomActivity.AD.ToString(), data, path))
                                {
                                    ApplicationTrace.Log("PushDownStream_AD", Status.Completed);
                                    await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.AD.ToString()));
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.AD.ToString(), "Completed");
                                }
                                else
                                {
                                    ApplicationTrace.Log("PushDownStream_AD", Status.Failed);
                                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.AD.ToString(), "Error in Push service");
                                }
                            }
                            else
                            {
                                ApplicationTrace.Log("PushDownStream_AD", Status.Completed);
                                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.AD.ToString(), "Completed with no data found in Pull service");
                            }
                        }
                        else
                        {
                            ApplicationTrace.Log("PushDownStream_AD", Status.Failed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.AD.ToString(), "Error Downstream path does not exist");
                        }

                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDownStream_AD", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.AD.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDownStream_AD", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.AD.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Push data to TimeKeeping 
        /// </summary>
        /// <remarks>
        /// Push data to TimeKeeping
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDataToTK")]
        [HttpPost]
        public async Task<IHttpActionResult> PushDataToTKAsync()
        {
            try
            {
                ApplicationTrace.Log("PushDataTo_TK", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Push.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)
                    {
                        var data = PushService.DumpEmployeesToTKDatabase();
                        if (data == "OK")
                        {
                            ApplicationTrace.Log("PushDataTo_TK", Status.Completed);
                            await Task.Factory.StartNew(() => WebHookService.CreateAndSendCustomizedWebHook(RunningPaycomActivity.TK_Push.ToString(), " Data has been pushed to TimeKeeping system. "));
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Push.ToString(), "Completed - Pushed data to TK system");
                        }
                        else
                        {
                            ApplicationTrace.Log("PushDataTo_TK", Status.Failed);
                            configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Push.ToString(), "Error in Push service for TK push");
                        }
                    }
                    else
                    {
                        ApplicationTrace.Log("PushDataTo_TK", Status.Failed);
                        configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Push.ToString(), "Error . Another dump is allready running for TK push");
                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDataTo_TK", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Push.ToString(), "Error for TK push");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDataTo_TK", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.TK_Push.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Push downStream for Deceibel
        /// </summary>
        /// <remarks>
        /// Create a downStream file for Decibel
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDownStreamForDecibel")]
        [HttpPost]
        public IHttpActionResult PushDownStream_Decibel(/*ReadFileRequestModel requestModel*/)
        {
            try
            {
                //if (requestModel == null || string.IsNullOrEmpty(requestModel.FileName))
                //{
                //    return Ok("Invalid input data");
                //}
                ApplicationTrace.Log("PushDownStream_Decibel", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Decibel.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)//dump is not progress
                    {
                        //add here
                        ApplicationTrace.Log("PushDownStream_Decibel", Status.Completed);
                        configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Decibel.ToString(), "Completed");
                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDownStream_Decibel", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Decibel.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDownStream_Decibel", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Decibel.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }

        /// <summary>
        /// Push downStream for Cornerstone
        /// </summary>
        /// <remarks>
        /// Create a downStream file for Cornerstone
        /// </remarks>
        /// <returns>
        /// returns custom response
        /// </returns>
        ///<response code="200"></response>
        [ResponseType(typeof(IEnumerable<IHttpActionResult>))]
        [Route("PushDownStreamForCornerstone")]
        [HttpPost]
        public IHttpActionResult PushDownStream_Cornerstone(/*ReadFileRequestModel requestModel*/)
        {
            try
            {
                //if (requestModel == null || string.IsNullOrEmpty(requestModel.FileName))
                //{
                //    return Ok("Invalid input data");
                //}
                ApplicationTrace.Log("PushDownStream_Cornerstone", Status.Started);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Cornerstone.ToString(), "Started");
                if (configResponse != null)
                {
                    if (configResponse.ResponseStatus != 1)//dump is not progress
                    {
                        //add here
                        ApplicationTrace.Log("PushDownStream_Cornerstone", Status.Completed);
                        configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Cornerstone.ToString(), "Completed");
                    }
                }
                else
                {
                    ApplicationTrace.Log("PushDownStream_Cornerstone", Status.Failed);
                    configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Cornerstone.ToString(), "Error");
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushDownStream_Cornerstone", Status.Failed);
                configResponse = configService.RegisterConfigurationActivity(RunningPaycomActivity.Cornerstone.ToString(), "Error - " + ex.Message + " - " + ex.StackTrace);
            }
            return Ok(configResponse);
        }
    }
}
