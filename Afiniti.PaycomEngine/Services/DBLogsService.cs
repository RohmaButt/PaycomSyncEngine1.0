using System;
using System.Collections.Generic;
using static Afiniti.Paycom.Shared.Enums;
using System.IO;
using Afiniti.Paycom.DAL;

namespace Afiniti.PaycomEngine.Services
{
    public class DBLogsService
    {
        public void LogMessagesForLevel1InDB(string LogMessage, string FilePath, LogType logType, LogStage logStage)
        {
            if (EngineAPIConfigService.Level1Logs)
            {
                PaycomData_Logs_Level1 log = new PaycomData_Logs_Level1()
                {
                    LogDetail = LogMessage,
                    LogDate = DateTime.Now,
                    FileName = Path.GetFileName(FilePath),
                    LogType = logType.ToString(),
                    LogStage = logStage.ToString()
                };
                using (PaycomEngineContext context = new PaycomEngineContext())
                {
                    context.PaycomData_Logs_Level1.Add(log);
                    context.SaveChanges();
                }
            }
        }
        public void LogMessagesForLevel1InDB(List<PaycomData_Logs_Level1> level1_Logs)
        {
            if (EngineAPIConfigService.Level1Logs)
            {
                using (PaycomEngineContext context = new PaycomEngineContext())
                {
                    context.PaycomData_Logs_Level1.AddRange(level1_Logs);
                    context.SaveChanges();
                }
            }
        }
        public void LogMessagesForLevel2InDB(string logData, string LogMessage, string fileName, LogType logType, LogStage logStage, string columnName)
        {
            if (EngineAPIConfigService.Level2Logs)
            {
                List<PaycomData_Logs_Level2> PaycomData_Logs_Level2 = new List<PaycomData_Logs_Level2>();
                foreach (var item in logData.Split(','))
                {
                    PaycomData_Logs_Level2.Add(
                        new PaycomData_Logs_Level2()
                        {
                            ColumnName = columnName,
                            FileName = fileName,
                            LogDate = DateTime.Now,
                            LogDetail = string.Concat(LogMessage, item),
                            LogStage = logStage.ToString(),
                            LogType = logType.ToString()
                        });
                }
                using (PaycomEngineContext context = new PaycomEngineContext())
                {
                    context.PaycomData_Logs_Level2.AddRange(PaycomData_Logs_Level2);
                    context.SaveChanges();
                }
            }
        }

        //public static void LogMessagesForLevel1And2InDB(string logData, string LogMessage, string fileName, LogType logType, LogStage logStage, string columnName)
        //{
        //    PaycomData_Logs_Level1 level1Log = new PaycomData_Logs_Level1()
        //    {
        //        LogDetail = string.Concat(logData, LogMessage),
        //        LogDate = DateTime.Now,
        //        FileName = fileName,
        //        LogType = logType.ToString(),
        //        LogStage = logStage.ToString()
        //    };
        //    List<PaycomData_Logs_Level2> PaycomData_Logs_Level2 = new List<PaycomData_Logs_Level2>();
        //    string msg = LogMessage.Split('.')?[0];
        //    foreach (var item in logData.Split(','))
        //    {
        //        PaycomData_Logs_Level2.Add(
        //            new PaycomData_Logs_Level2()
        //            {
        //                ColumnName = columnName,
        //                FileName = fileName,
        //                LogDate = DateTime.Now,
        //                LogDetail = string.Concat(msg, item),
        //                LogStage = LogStage.Scrubbing.ToString(),
        //                LogType = LogType.Warning.ToString()
        //            });
        //    }
        //    using (PaycomEngineContext context = new PaycomEngineContext())
        //    {
        //        context.PaycomData_Logs_Level1.Add(level1Log);
        //        context.PaycomData_Logs_Level2.AddRange(PaycomData_Logs_Level2);
        //        context.SaveChanges();
        //    }
        //}

        //public void LogColumnsData(IWorkbook workbook, string FilePath)//not in use 
        //{
        //    //col list which are different for Excel and DB so we have to exclude them explicity.
        //    List<string> excludes = new List<string>() { "Firstname", "Lastname", "Department", "ClockSeq_#", "Full-Time_to_Part-Time_Date", "Part-Time_to_Full-Time_Date", "Supervisor_Primary_Code", "City" };
        //    List<string> Excel_ColsList = new List<string>();
        //    List<string> PaycomDTO_AttributesList = new List<string>();
        //    foreach (PropertyInfo p in typeof(PaycomRequestDTO).GetProperties())
        //    {
        //        if (!excludes.Contains(p.Name))
        //        {
        //            PaycomDTO_AttributesList.Add(p.Name);
        //        }
        //    }
        //    if (workbook.GetSheetAt(0).GetRow(0).Cells.Count > 0)
        //    {
        //        for (int i = 0; i < workbook.GetSheetAt(0).GetRow(0).Cells.Count; i++)
        //        {
        //            if (!excludes.Contains(workbook.GetSheetAt(0).GetRow(0).Cells[i].StringCellValue))
        //            {
        //                Excel_ColsList.Add(workbook.GetSheetAt(0).GetRow(0).Cells[i].StringCellValue);
        //            }
        //        }
        //    }
        //    var UnCommonList = Excel_ColsList.Except(PaycomDTO_AttributesList).ToList();
        //    if (UnCommonList != null && UnCommonList.Any())
        //    {
        //        PaycomData_Process_Logs scrub_Log = new PaycomData_Process_Logs()
        //        {
        //            LogDetail = string.Concat(string.Join(" , ", UnCommonList), " column(s) are available in ", Path.GetFileName(FilePath), " Paycom excel file but not in DB schema so its data will not be dumped."),
        //            LogDate = DateTime.Now,
        //            FileName = Path.GetFileName(FilePath),
        //            LogType = nameof(Log_Type.Warning),
        //            LogStage = nameof(Log_Stage.Scrubbing)
        //        };
        //        using (PaycomEngineContext context = new PaycomEngineContext())
        //        {
        //            context.PaycomData_Process_Logs.Add(scrub_Log);
        //            context.SaveChanges();
        //        }
        //    }
        //}

    }

}