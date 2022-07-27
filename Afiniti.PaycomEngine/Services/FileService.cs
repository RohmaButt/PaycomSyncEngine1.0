using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.DAL;
using Afiniti.Paycom.Shared.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using static Afiniti.Paycom.Shared.Enums;
using Status = Afiniti.Framework.LoggingTracing.Status;

namespace Afiniti.PaycomEngine.Services
{
    public class FileService
    {
        public string ValidateFileProcessedStatus()
        {
            // If FIRST file is processing for today
            // Check if Yestherday any file has been processed or not
            // If any file has not been processed yesterday ==> Send notification; “File was not received on November 3, 2020”.  
            // else do nothing
            // Process today's file as normal
            // Stop notifications for all day if file has not been processed.
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            string fileStatus = string.Empty;
            using (var context = new PaycomEngineContext())
            {
                var TodayProcessedFilesCount = context.ConfigurationActivity.Where(x => x.Description == RunningPaycomActivity.PushInDB.ToString() && x.IsActive == false && DbFunctions.TruncateTime(x.StartDate) == today).ToList()?.Count;
                if (TodayProcessedFilesCount == 0)// First file of today
                {
                    var YesterdayProcessedFilesCount = context.ConfigurationActivity.Where(x => x.Description == RunningPaycomActivity.PushInDB.ToString() && DbFunctions.TruncateTime(x.StartDate) == yesterday && x.ActivityDetail.Contains("Completed with file has been transfered. New=")).ToList()?.Count;
                    if (YesterdayProcessedFilesCount == 0)
                    {
                        fileStatus = "File was not received";
                        return fileStatus;
                    }
                }
                else // Not first file of today
                {
                    //do nothing
                }
            }
            return fileStatus;
        }
        public string GetFilePath(string paycomActivity)
        {
            Enum.TryParse(paycomActivity, out RunningPaycomActivity CurrentpaycomActivity);
            string filePath = string.Empty;
            filePath = EngineAPIConfigService.EngineAPIConfigs.SingleOrDefault(x => x.ConfigAppEvent == paycomActivity && x.IsActive == true)?.ConfigValue;
            if (filePath == null || filePath == string.Empty)
            {
                filePath = EngineAPIConfigService.DefaultPath;
            }
            switch (paycomActivity)
            {
                case "Processing":
                case "PushInDB"://here checks file standardized name   TODO
                    string[] files = Directory.GetFiles(filePath, "*.xls");
                    if (files.Length > 0)
                    {
                        filePath = files?[0];
                    }
                    else
                    {
                        files = Directory.GetFiles(filePath, "*.xlsx");
                        if (files.Length > 0)
                            filePath = files?[0];
                    }
                    break;
                case "SD":
                case "AD":
                case "Certify":
                case "Exchange":
                    if (!Directory.Exists(filePath))
                    {
                        filePath = "C:\\";
                    }
                    break;
                default:
                    break;

            }
            return filePath;
        }

        public bool TransferFile(String sourceFile, string DestinationFolder)
        {
            try
            {
                ApplicationTrace.Log("TransferFile", Status.Started);
                string destinationFile = string.Empty;
                destinationFile = EngineAPIConfigService.EngineAPIConfigs.FirstOrDefault(x => x.IsActive == true && x.ConfigAppEvent == DestinationFolder)?.ConfigValue;
                if (sourceFile != string.Empty && destinationFile != string.Empty && destinationFile != null && sourceFile != null)
                {
                    string filename = Path.GetFileName(sourceFile);
                    if (DestinationFolder == "Processed")
                    {
                        File.Move(sourceFile, string.Concat(destinationFile, "\\", filename));
                    }
                    else if (DestinationFolder == "Processing")//File shouldnt remove if already exists. 
                    {
                        File.Move(sourceFile, string.Concat(destinationFile, "\\", DateTime.Now.ToString("yyyyMMddhhmm"), "_", filename));
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("TransferFile", Status.Failed);
                ExceptionHandling.LogException(ex, "TransferFile" + ex.StackTrace);
                return false;
            }
            ApplicationTrace.Log("TransferFile", Status.Completed);
            return true;
        }

    }
}