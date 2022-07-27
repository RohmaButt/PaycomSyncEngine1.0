using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.DAL;
using Afiniti.Paycom.Shared.Models;
using Afiniti.Paycom.Shared.Services;
using Afiniti.PaycomEngine.Helpers;
using Afiniti.PaycomEngine.Polymorphics;
using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static Afiniti.Paycom.Shared.Enums;
using PaycomData = Afiniti.Paycom.DAL.PaycomData;
using Status = Afiniti.Framework.LoggingTracing.Status;

//https://joshclose.github.io/CsvHelper/
//https://dotnet.libhunt.com/csvhelper-alternatives
//independent-software.com/introduction-to-npoi.html
namespace Afiniti.PaycomEngine.Services
{
    public class PushEngineService
    {
        #region DB Dumping
        List<PaycomData> PaycomDataToInsert = new List<PaycomData>();
        List<PaycomData> PaycomDataToUpdate = new List<PaycomData>();
        List<PaycomData> PaycomDataToHistory = new List<PaycomData>();

        int NewRowsCount = 0, UpdatedRowsCount = 0, HistoryRowsCount = 0;
        public async Task<string> DumpPaycomDataInDB(string FilePath)// string return type to log processed UpdatedRows and NewRows
        {
            string DumpStatus = string.Empty;
            ApplicationTrace.Log("DumpPaycomDataInDB: Main", Status.Started);
            List<PaycomData_Scrubbed> scrubbedData = new List<PaycomData_Scrubbed>();
            List<PaycomData> OldPaycomData = new List<PaycomData>();
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                OldPaycomData = context.PaycomData.ToList();
                scrubbedData = context.PaycomData_Scrubbed.Where(x => x.ValidationFail_Flag != true).ToList();
            }
            if (scrubbedData == null)
            {
                return "Error with reading/parsing Excel";
            }
            if (scrubbedData.Count > 0)
            {
                List<Task> tasksList = new List<Task>();
                if (scrubbedData.Count < 6000)
                {
                    tasksList.Add(Task.Factory.StartNew(() => ValidatePaycomScrubbedData(scrubbedData, OldPaycomData))
                        .ContinueWith(m =>
                        {
                            if (m.Status == TaskStatus.Faulted)
                            {
                                if (m.Exception != null)
                                {
                                    ExceptionHandling.LogException(m.Exception.GetBaseException(), "DumpPaycomDataInDB");
                                }
                            }
                            else if (m.Status == TaskStatus.RanToCompletion)
                            {
                                ApplicationTrace.Log("DumpPaycomDataInDB:Main", Status.Completed);
                            }
                        }, TaskContinuationOptions.None));
                }
                else
                {
                    int iChunk = 1000;
                    List<int> lLoop = new List<int>();
                    var loop = scrubbedData.Count / iChunk;
                    for (int i = 0; i < loop; i++)
                    {
                        lLoop.Add(iChunk);
                    }
                    var spreadRest = scrubbedData.Count % iChunk;
                    if (lLoop.Count == 1)
                    {
                        var gElement = lLoop[0];
                        lLoop[0] = gElement + spreadRest;
                    }
                    else
                    {
                        for (int i = 1; i <= spreadRest; i++)
                        {
                            var gIn = i % lLoop.Count;
                            var gElement = lLoop[gIn];
                            lLoop[gIn] = gElement + 1;
                        }
                    }
                    int iSkip = 0;
                    for (int iCount = 0; iCount < lLoop.Count; iCount++)
                    {
                        var gFirst = scrubbedData.OrderBy(m => m.ClockSeq).Skip(iSkip).Take(lLoop[iCount]).ToList();
                        tasksList.Add(Task.Factory.StartNew(() => ValidatePaycomScrubbedData(gFirst, OldPaycomData))
                            .ContinueWith(m =>
                            {
                                if (m.Status == TaskStatus.Faulted)
                                {
                                    if (m.Exception != null)
                                    {
                                        ExceptionHandling.LogException(m.Exception.GetBaseException(), "DumpPaycomDataInDB");
                                    }
                                }
                                else if (m.Status == TaskStatus.RanToCompletion)
                                {
                                    ApplicationTrace.Log("DumpPaycomDataInDB:Main", Status.Completed);
                                }
                            }, TaskContinuationOptions.None));

                        iSkip += lLoop[iCount];
                    }
                }
                await Task.WhenAll(tasksList);

                ApplicationTrace.Log("DumpPaycomDataInDB:PercentageCheck", Status.Started);
                decimal ResultPercent = 0;
                //PercentageFlag and Value: If enable then check % age.If flag is ON and under configured % age then maintain history and Insert Update rows else not
                if (CheckDeltaPercentage(PaycomDataToUpdate.Count() + PaycomDataToInsert.Count(), OldPaycomData.Count(), ref ResultPercent))
                {
                    if (PaycomDataToInsert.Any())
                    {
                        DumpPaycomBaseDataInDB();
                    }

                    if (PaycomDataToHistory.Any())
                    {
                        DumpEmployeeHistoryInDB(PaycomDataToHistory);
                    }
                    HistoryRowsCount = PaycomDataToHistory?.Count() ?? 0;

                    if (PaycomDataToUpdate.Any())
                    {
                        List<PaycomData> paycoms = new List<PaycomData>();
                        paycoms = PaycomDataToUpdate.ToList();
                        foreach (var item in paycoms)
                        {
                            PaycomData OldDataOfEmployee = new PaycomData();
                            using (PaycomEngineContext context = new PaycomEngineContext())
                            {
                                //DB-ClockSeq!= DB-Emp_Code and Scrubber-ClockSeq is updated for DB-Emp_Code(old employee), so instead of new, update ClockSeq for already created employee
                                if (context.PaycomData.Any(x => x.Employee_Code != x.ClockSeq && x.Employee_Code == item.Employee_Code && x.ClockSeq != item.ClockSeq && x.Employee_Status != "Discard"))
                                    OldDataOfEmployee = context.PaycomData.FirstOrDefault(x => x.Employee_Code != x.ClockSeq && x.Employee_Code == item.Employee_Code && x.ClockSeq != item.ClockSeq && x.Employee_Status != "Discard");
                                //DB-ClockSeq== DB-Emp_Code and Scrubber-ClockSeq is updated for DB-Emp_Code(old employee), so instead of new, update ClockSeq for already created employee
                                else if (context.PaycomData.Any(x => x.Employee_Code == x.ClockSeq && x.Employee_Code == item.Employee_Code && x.ClockSeq != item.ClockSeq && x.Employee_Status != "Discard"))
                                    OldDataOfEmployee = context.PaycomData.FirstOrDefault(x => x.Employee_Code == x.ClockSeq && x.Employee_Code == item.Employee_Code && x.ClockSeq != item.ClockSeq && x.Employee_Status != "Discard");
                                else
                                    OldDataOfEmployee = context.PaycomData.FirstOrDefault(x => x.ClockSeq == item.ClockSeq && x.Employee_Status != "Discard");
                                System.Data.Entity.Infrastructure.DbEntityEntry<PaycomData> ee = context.Entry(OldDataOfEmployee);
                                ee.CurrentValues.SetValues(item);
                                context.SaveChanges();
                            }
                        }
                    }
                    UpdatedRowsCount = PaycomDataToUpdate?.Count() ?? 0;
                }
                else
                {
                    DumpStatus = $"Warning: File has not been processed. Delta is {ResultPercent:0.###} which is more than configured. Please contact Connect team.";
                }
                ApplicationTrace.Log("DumpPaycomDataInDB:PercentageCheck", Status.Completed);
                DumpStatus += $"New={NewRowsCount} :: Updated={UpdatedRowsCount} :: History= {HistoryRowsCount}";

                InsertDataForJiraTickets(Path.GetFileName(FilePath));

                return DumpStatus;
            }
            else
            {
                DumpStatus = $"New={NewRowsCount} :: Updated={UpdatedRowsCount} :: History= {HistoryRowsCount}";
                return DumpStatus;
            }
        }
        private void InsertDataForJiraTickets(string fileName)//Insert employees with blank NTLogin/Emails to create Jira tickets
        {
            ApplicationTrace.Log("DumpPaycomDataInDB:InsertDataForJiraTickets", Status.Started);
            List<PaycomData_MissingInfo> missingEmployeeInfoFromDB = new List<PaycomData_MissingInfo>();
            List<PaycomData_MissingInfo> missingEmployeeItemList = new List<PaycomData_MissingInfo>();
            string sConnection = string.Empty;
            List<PaycomData_Scrubbed> scrubbedData = new List<PaycomData_Scrubbed>();
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                sConnection = context.Database.Connection.ConnectionString;
                scrubbedData = context.PaycomData_Scrubbed.AsNoTracking().Where(x => x.Employee_Status == "Active" && x.ValidationFail_Flag == true && (string.IsNullOrEmpty(x.NTLogin) || string.IsNullOrEmpty(x.Work_Email))).ToList();
                missingEmployeeInfoFromDB = context.PaycomData_MissingInfo.AsNoTracking().ToList();
            }
            if (scrubbedData.Count > 0)
            {
                foreach (var item in scrubbedData)
                {
                    if (!missingEmployeeInfoFromDB.Any(x => x.ClockSeq == item.ClockSeq))
                    {
                        PaycomData_MissingInfo missingEmployeeItem = new PaycomData_MissingInfo()
                        {
                            ClockSeq = item.ClockSeq,
                            Employee_Code = item.Employee_Code,
                            InsertionDate = DateTime.Now,
                            InsertionFileName = Path.GetFileName(fileName),
                            UpdateCount = 0,
                            Is_Processed = PaycomData_MissingInfoStatus.NotProcessed.ToString(),
                            IsActive = "1",
                            CurrentJiraStatus = "Initiated",
                            InitiatedBy = "PaycomEngineUser",
                        };
                        missingEmployeeItemList.Add(missingEmployeeItem);
                    }
                }
                if (missingEmployeeItemList.Count > 0)
                {
                    DataTable itemsForJira_DataTable = Common.ToDataTable(missingEmployeeItemList);
                    using (SqlBulkCopy sqlbc = new SqlBulkCopy(sConnection))
                    {
                        sqlbc.DestinationTableName = "PaycomData_MissingInfo";
                        sqlbc.ColumnMappings.Add("ClockSeq", "ClockSeq");
                        sqlbc.ColumnMappings.Add("Employee_Code", "Employee_Code");
                        sqlbc.ColumnMappings.Add("InsertionDate", "InsertionDate");
                        sqlbc.ColumnMappings.Add("InsertionFileName", "InsertionFileName");
                        sqlbc.ColumnMappings.Add("UpdateCount", "UpdateCount");
                        sqlbc.ColumnMappings.Add("Is_Processed", "Is_Processed");
                        sqlbc.ColumnMappings.Add("IsActive", "IsActive");
                        sqlbc.ColumnMappings.Add("CurrentJiraStatus", "CurrentJiraStatus");
                        sqlbc.ColumnMappings.Add("InitiatedBy", "InitiatedBy");
                        sqlbc.WriteToServer(itemsForJira_DataTable);
                    }
                }
            }
            ApplicationTrace.Log("DumpPaycomDataInDB:InsertDataForJiraTickets", Status.Completed);
        }
        private bool ValidatePaycomScrubbedData(dynamic scrubbingData, List<PaycomData> OldPaycomData)
        {
            ApplicationTrace.Log("ProcessPaycomBaseData", Status.Started);

            foreach (var item in scrubbingData)
            {
                if (item != null)
                {
                    string ScrubbingClockSeq = item.ClockSeq;
                    string ScrubbingEmployeeCde = item.Employee_Code;

                    PaycomData PaycomScrubbedItem = new PaycomData()
                    {
                        City1 = item.City1,
                        City2 = item.City2,
                        ClockSeq = item.ClockSeq,
                        Country = item.Country,
                        Currency = string.IsNullOrWhiteSpace(item.Currency) || string.IsNullOrEmpty(item.Currency) ? "USD" : item.Currency,
                        Department_Desc = item.Department_Desc,
                        DumpDate = DateTime.Now,
                        Employee_Code = item.Employee_Code,
                        Employee_FirstName = item.Employee_FirstName,
                        Employee_LastName = item.Employee_LastName,
                        Employee_MiddleName = item.Employee_MiddleName,
                        Employee_Name = item.Employee_Name,
                        EntityName = item.EntityName,
                        Employee_Status = item.Employee_Status,
                        FullTime_to_PartTime_Date = item.FullTime_to_PartTime_Date == null || item.FullTime_to_PartTime_Date.ToString() == "00/00/0000" || item.FullTime_to_PartTime_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.FullTime_to_PartTime_Date).ToString("d"),
                        Hire_Date = item.Hire_Date == null || item.Hire_Date.ToString() == "00/00/0000" || item.Hire_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Hire_Date).ToString("d"),
                        InitiatedBy = "PaycomEngineUser",
                        Last_Position_Change_Date = item.Last_Position_Change_Date == null || item.Last_Position_Change_Date.ToString() == "00/00/0000" || item.Last_Position_Change_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Last_Position_Change_Date).ToString("d"),
                        LineMgrEmailID = item.LineMgrEmailID,
                        ManagerEmpID = item.ManagerEmpID,
                        Manager_NT_Login = item.Manager_NT_Login,
                        NTLogin = item.NTLogin,
                        PartTime_to_FullTime_Date = item.PartTime_to_FullTime_Date == null || item.PartTime_to_FullTime_Date.ToString() == "00/00/0000" || item.PartTime_to_FullTime_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.PartTime_to_FullTime_Date).ToString("d"),
                        PaycomDataKey = Guid.NewGuid(),
                        Position = item.Position,
                        Rehire_Date = item.Rehire_Date == null || item.Rehire_Date.ToString() == "00/00/0000" || item.Rehire_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Rehire_Date).ToString("d"),
                        Supervisor_Primary = item.Supervisor_Primary,
                        Termination_Date = item.Termination_Date == null || item.Termination_Date.ToString() == "00/00/0000" || item.Termination_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Termination_Date).ToString("d"),
                        Tier_2_Desc = item.Tier_2_Desc,
                        Tier_3_Desc = item.Tier_3_Desc,
                        Work_Email = item.Work_Email,
                        InsertionDate = item.InsertionDate,
                        WorkLocation = item.WorkLocation
                    };

                    PaycomData OldDataOfEmployee = new PaycomData();
                    //DB-ClockSeq!=DB-Emp_Code and Scrubber-ClockSeq is new for DB-Emp_Code(old employee), so instead of new, update ClockSeq for already created employee
                    if (OldPaycomData.Any(x => x.Employee_Code != x.ClockSeq && x.Employee_Code == ScrubbingEmployeeCde && x.ClockSeq != ScrubbingClockSeq && x.Employee_Status != "Discard"))
                        OldDataOfEmployee = OldPaycomData.FirstOrDefault(x => x.Employee_Code != x.ClockSeq && x.Employee_Code == ScrubbingEmployeeCde && x.ClockSeq != ScrubbingClockSeq && x.Employee_Status != "Discard");
                    //DB-ClockSeq==DB-Emp_Code and Scrubber-ClockSeq is new for DB-Emp_Code(old employee), so instead of new, update ClockSeq for already created employee
                    else if (OldPaycomData.Any(x => x.Employee_Code == x.ClockSeq && x.Employee_Code == ScrubbingEmployeeCde && x.ClockSeq != ScrubbingClockSeq && x.Employee_Status != "Discard"))
                        OldDataOfEmployee = OldPaycomData.FirstOrDefault(x => x.Employee_Code == x.ClockSeq && x.Employee_Code == ScrubbingEmployeeCde && x.ClockSeq != ScrubbingClockSeq && x.Employee_Status != "Discard");
                    else
                        OldDataOfEmployee = OldPaycomData.FirstOrDefault(x => x.ClockSeq == ScrubbingClockSeq && x.Employee_Status != "Discard");

                    if (OldDataOfEmployee != null && PaycomScrubbedItem != null)
                    {
                        if (!PaycomScrubbedItem.Equals(OldDataOfEmployee))// if TRUE then NO change in any property otherwise we have to check
                        {
                            if (!PaycomDataToHistory.Any(x => x.ClockSeq == ScrubbingClockSeq && x.ClockSeq != null && !string.IsNullOrEmpty(x.ClockSeq)))
                            {
                                PaycomDataToHistory.Add(OldDataOfEmployee.Clone());
                            }
                            OldDataOfEmployee.City1 = item.City1;
                            OldDataOfEmployee.City2 = item.City2;
                            OldDataOfEmployee.ClockSeq = item.ClockSeq;
                            OldDataOfEmployee.Country = item.Country;
                            OldDataOfEmployee.Currency = string.IsNullOrWhiteSpace(item.Currency) || string.IsNullOrEmpty(item.Currency) ? "USD" : item.Currency;
                            OldDataOfEmployee.Department_Desc = item.Department_Desc;
                            OldDataOfEmployee.Employee_Code = item.Employee_Code;
                            OldDataOfEmployee.Employee_FirstName = item.Employee_FirstName;
                            OldDataOfEmployee.Employee_LastName = item.Employee_LastName;
                            OldDataOfEmployee.Employee_MiddleName = item.Employee_MiddleName;
                            OldDataOfEmployee.Employee_Name = item.Employee_Name;
                            OldDataOfEmployee.EntityName = item.EntityName;
                            OldDataOfEmployee.Employee_Status = item.Employee_Status;
                            OldDataOfEmployee.FullTime_to_PartTime_Date = item.FullTime_to_PartTime_Date == null || item.FullTime_to_PartTime_Date.ToString() == "00/00/0000" || item.FullTime_to_PartTime_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.FullTime_to_PartTime_Date).ToString("d");
                            OldDataOfEmployee.Hire_Date = item.Hire_Date == null || item.Hire_Date.ToString() == "00/00/0000" || item.Hire_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Hire_Date).ToString("d");
                            OldDataOfEmployee.Last_Position_Change_Date = item.Last_Position_Change_Date == null || item.Last_Position_Change_Date.ToString() == "00/00/0000" || item.Last_Position_Change_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Last_Position_Change_Date).ToString("d");
                            OldDataOfEmployee.LineMgrEmailID = item.LineMgrEmailID;
                            OldDataOfEmployee.ManagerEmpID = item.ManagerEmpID;
                            OldDataOfEmployee.Manager_NT_Login = item.Manager_NT_Login;
                            OldDataOfEmployee.NTLogin = item.NTLogin;
                            OldDataOfEmployee.PartTime_to_FullTime_Date = item.PartTime_to_FullTime_Date == null || item.PartTime_to_FullTime_Date.ToString() == "00/00/0000" || item.PartTime_to_FullTime_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.PartTime_to_FullTime_Date).ToString("d");
                            OldDataOfEmployee.Position = item.Position;
                            OldDataOfEmployee.Rehire_Date = item.Rehire_Date == null || item.Rehire_Date.ToString() == "00/00/0000" || item.Rehire_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Rehire_Date).ToString("d");
                            OldDataOfEmployee.Supervisor_Primary = item.Supervisor_Primary;
                            OldDataOfEmployee.Termination_Date = item.Termination_Date == null || item.Termination_Date.ToString() == "00/00/0000" || item.Termination_Date.ToString() == "" ? "00/00/0000" : Convert.ToDateTime(item.Termination_Date).ToString("d");
                            OldDataOfEmployee.Tier_2_Desc = item.Tier_2_Desc;
                            OldDataOfEmployee.Tier_3_Desc = item.Tier_3_Desc;
                            OldDataOfEmployee.Work_Email = item.Work_Email;
                            OldDataOfEmployee.ValidityDate = DateTime.Now;
                            OldDataOfEmployee.Certify_DSFlag = "1";
                            OldDataOfEmployee.Exchange_DSFlag = "1";
                            OldDataOfEmployee.WorkLocation = item.WorkLocation;
                            PaycomDataToUpdate.Add(OldDataOfEmployee);
                        }
                    }
                    else//new clockseq = new employee
                    {
                        var clock = PaycomScrubbedItem.ClockSeq;
                        if (!PaycomDataToInsert.Any(x => x.ClockSeq == clock))
                        {
                            PaycomScrubbedItem.InsertionDate = DateTime.Now;
                            PaycomScrubbedItem.ValidityDate = null;
                            PaycomScrubbedItem.Certify_DSFlag = "1";
                            PaycomScrubbedItem.Exchange_DSFlag = "1";
                            PaycomDataToInsert.Add(PaycomScrubbedItem);
                        }
                    }
                }
            }
            ApplicationTrace.Log("ProcessPaycomBaseData", Status.Completed);
            return true;
        }
        public bool CheckDeltaPercentage(decimal deltaRowsCount, decimal DBRowsCount, ref decimal RunningPercentage)//return FALSE if update %age is less than configred %age.
        {
            decimal deltaPercent = 0;
            bool deltaFlag = false;
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                var predicateSettings = (from det in context.ConfigurationSettingDetail
                                         join p in context.ConfigurationSetting on det.ConfigAppKey equals p.ConfigValueKey
                                         where p.IsActive == true && det.IsActive == true && p.ConfigAppEvent == "PushInDB"
                                         select det).ToList();
                string deltaFlagStr = predicateSettings.Where(x => x.ParamName == "Delta_PercentageFlag").FirstOrDefault()?.ParamValue == null ? "false" : predicateSettings.Where(x => x.ParamName == "Delta_PercentageFlag").FirstOrDefault()?.ParamValue;
                deltaFlag = Convert.ToBoolean(deltaFlagStr);
                deltaPercent = Convert.ToDecimal(predicateSettings.Where(x => x.ParamName == "Delta_PercentageValue").FirstOrDefault()?.ParamValue);
            }
            if (deltaFlag == true && deltaPercent > 0 && DBRowsCount > 0 && deltaRowsCount > 0)
            {
                RunningPercentage = deltaRowsCount / DBRowsCount * 100;
                if (RunningPercentage <= deltaPercent)
                    return true;
                else
                    return false;
            }
            else
                return true;
        }
        private bool DumpPaycomBaseDataInDB()
        {
            ApplicationTrace.Log("ProcessPaycomBaseData:Dump", Status.Started);
            if (PaycomDataToInsert.Any())
            {
                string sConnection = string.Empty;
                using (PaycomEngineContext context = new PaycomEngineContext())
                {
                    sConnection = context.Database.Connection.ConnectionString;
                }
                DataTable paycomInsert_DataTable = Common.ToDataTable(PaycomDataToInsert);
                using (SqlBulkCopy sqlbc = new SqlBulkCopy(sConnection))
                {
                    sqlbc.DestinationTableName = "PaycomData";
                    sqlbc.ColumnMappings.Add("Employee_Code", "Employee_Code");
                    sqlbc.ColumnMappings.Add("Employee_FirstName", "Employee_FirstName");
                    sqlbc.ColumnMappings.Add("Employee_MiddleName", "Employee_MiddleName");
                    sqlbc.ColumnMappings.Add("Employee_LastName", "Employee_LastName");
                    sqlbc.ColumnMappings.Add("Employee_Name", "Employee_Name");
                    sqlbc.ColumnMappings.Add("Department_Desc", "Department_Desc");
                    sqlbc.ColumnMappings.Add("Tier_2_Desc", "Tier_2_Desc");
                    sqlbc.ColumnMappings.Add("Tier_3_Desc", "Tier_3_Desc");
                    sqlbc.ColumnMappings.Add("Employee_Status", "Employee_Status");
                    sqlbc.ColumnMappings.Add("ClockSeq", "ClockSeq");
                    sqlbc.ColumnMappings.Add("Hire_Date", "Hire_Date");
                    sqlbc.ColumnMappings.Add("Termination_Date", "Termination_Date");
                    sqlbc.ColumnMappings.Add("Rehire_Date", "Rehire_Date");
                    sqlbc.ColumnMappings.Add("FullTime_to_PartTime_Date", "FullTime_to_PartTime_Date");
                    sqlbc.ColumnMappings.Add("PartTime_to_FullTime_Date", "PartTime_to_FullTime_Date");
                    sqlbc.ColumnMappings.Add("Supervisor_Primary", "Supervisor_Primary");
                    sqlbc.ColumnMappings.Add("Last_Position_Change_Date", "Last_Position_Change_Date");
                    sqlbc.ColumnMappings.Add("City1", "City1");
                    sqlbc.ColumnMappings.Add("Country", "Country");
                    sqlbc.ColumnMappings.Add("Currency", "Currency");
                    sqlbc.ColumnMappings.Add("City2", "City2");
                    sqlbc.ColumnMappings.Add("NTLogin", "NTLogin");
                    sqlbc.ColumnMappings.Add("Work_Email", "Work_Email");
                    sqlbc.ColumnMappings.Add("Position", "Position");
                    sqlbc.ColumnMappings.Add("ManagerEmpID", "ManagerEmpID");
                    sqlbc.ColumnMappings.Add("EntityName", "EntityName");
                    sqlbc.ColumnMappings.Add("LineMgrEmailID", "LineMgrEmailID");
                    sqlbc.ColumnMappings.Add("Manager_NT_Login", "Manager_NT_Login");
                    sqlbc.ColumnMappings.Add("InitiatedBy", "InitiatedBy");
                    sqlbc.ColumnMappings.Add("InsertionDate", "InsertionDate");
                    sqlbc.ColumnMappings.Add("ValidityDate", "ValidityDate");
                    sqlbc.ColumnMappings.Add("Certify_DSFlag", "Certify_DSFlag");
                    sqlbc.ColumnMappings.Add("Exchange_DSFlag", "Exchange_DSFlag");
                    sqlbc.ColumnMappings.Add("WorkLocation", "WorkLocation");
                    sqlbc.WriteToServer(paycomInsert_DataTable);
                }
            }
            NewRowsCount += PaycomDataToInsert?.Count() ?? 0;
            ApplicationTrace.Log("ProcessPaycomBaseData:Dump", Status.Completed);
            return true;
        }
        private bool DumpEmployeeHistoryInDB(List<PaycomData> pEmployees)
        {
            ApplicationTrace.Log("DumpPaycomDataInDB: DumpEmployeeRowInHistory", Status.Started);
            List<PaycomData_History> historyObj = new List<PaycomData_History>();
            foreach (var employee in pEmployees)
            {
                historyObj.Add(new PaycomData_History()
                {
                    City1 = employee.City1,
                    City2 = employee.City2,
                    ClockSeq = employee.ClockSeq,
                    Country = employee.Country,
                    Currency = employee.Currency,
                    Department_Desc = employee.Department_Desc,
                    DumpDate = employee.DumpDate,
                    Employee_Code = employee.Employee_Code,
                    Employee_FirstName = employee.Employee_FirstName,
                    Employee_LastName = employee.Employee_LastName,
                    Employee_MiddleName = employee.Employee_MiddleName,
                    Employee_Name = employee.Employee_Name,
                    EntityName = employee.EntityName,
                    Employee_Status = employee.Employee_Status,
                    FullTime_to_PartTime_Date = employee.FullTime_to_PartTime_Date,
                    Hire_Date = employee.Hire_Date,
                    InitiatedBy = employee.InitiatedBy,
                    Last_Position_Change_Date = employee.Last_Position_Change_Date,
                    LineMgrEmailID = employee.LineMgrEmailID,
                    ManagerEmpID = employee.ManagerEmpID,
                    Manager_NT_Login = employee.Manager_NT_Login,
                    NTLogin = employee.NTLogin,
                    PartTime_to_FullTime_Date = employee.PartTime_to_FullTime_Date,
                    PaycomDataKey = employee.PaycomDataKey,
                    Position = employee.Position,
                    Rehire_Date = employee.Rehire_Date,
                    Supervisor_Primary = employee.Supervisor_Primary,
                    Termination_Date = employee.Termination_Date,
                    Tier_2_Desc = employee.Tier_2_Desc,
                    Tier_3_Desc = employee.Tier_3_Desc,
                    Work_Email = employee.Work_Email,
                    PaycomDataId = employee.PaycomDataId,
                    InsertionDate = employee.InsertionDate,
                    PaycomHistoryDataKey = Guid.NewGuid(),
                    HistoryDumpDate = DateTime.Now,
                    ValidityDate = DateTime.Now,
                    Certify_DSFlag = employee.Certify_DSFlag,
                    Exchange_DSFlag = employee.Exchange_DSFlag,
                    WorkLocation = employee.WorkLocation
                });
            }
            string sConnection = string.Empty;
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                sConnection = context.Database.Connection.ConnectionString;
            }
            DataTable paycomInsertHistory_DataTable = Common.ToDataTable(historyObj);
            using (SqlBulkCopy sqlbc = new SqlBulkCopy(sConnection))
            {
                sqlbc.DestinationTableName = "PaycomData_History";
                sqlbc.ColumnMappings.Add("PaycomDataId", "PaycomDataId");
                sqlbc.ColumnMappings.Add("PaycomDataKey", "PaycomDataKey");
                sqlbc.ColumnMappings.Add("Employee_Code", "Employee_Code");
                sqlbc.ColumnMappings.Add("Employee_FirstName", "Employee_FirstName");
                sqlbc.ColumnMappings.Add("Employee_MiddleName", "Employee_MiddleName");
                sqlbc.ColumnMappings.Add("Employee_LastName", "Employee_LastName");
                sqlbc.ColumnMappings.Add("Employee_Name", "Employee_Name");
                sqlbc.ColumnMappings.Add("Department_Desc", "Department_Desc");
                sqlbc.ColumnMappings.Add("Tier_2_Desc", "Tier_2_Desc");
                sqlbc.ColumnMappings.Add("Tier_3_Desc", "Tier_3_Desc");
                sqlbc.ColumnMappings.Add("Employee_Status", "Employee_Status");
                sqlbc.ColumnMappings.Add("ClockSeq", "ClockSeq");
                sqlbc.ColumnMappings.Add("Hire_Date", "Hire_Date");
                sqlbc.ColumnMappings.Add("Termination_Date", "Termination_Date");
                sqlbc.ColumnMappings.Add("Rehire_Date", "Rehire_Date");
                sqlbc.ColumnMappings.Add("FullTime_to_PartTime_Date", "FullTime_to_PartTime_Date");
                sqlbc.ColumnMappings.Add("PartTime_to_FullTime_Date", "PartTime_to_FullTime_Date");
                sqlbc.ColumnMappings.Add("Supervisor_Primary", "Supervisor_Primary");
                sqlbc.ColumnMappings.Add("Last_Position_Change_Date", "Last_Position_Change_Date");
                sqlbc.ColumnMappings.Add("City1", "City1");
                sqlbc.ColumnMappings.Add("Country", "Country");
                sqlbc.ColumnMappings.Add("Currency", "Currency");
                sqlbc.ColumnMappings.Add("City2", "City2");
                sqlbc.ColumnMappings.Add("NTLogin", "NTLogin");
                sqlbc.ColumnMappings.Add("Work_Email", "Work_Email");
                sqlbc.ColumnMappings.Add("Position", "Position");
                sqlbc.ColumnMappings.Add("ManagerEmpID", "ManagerEmpID");
                sqlbc.ColumnMappings.Add("EntityName", "EntityName");
                sqlbc.ColumnMappings.Add("LineMgrEmailID", "LineMgrEmailID");
                sqlbc.ColumnMappings.Add("Manager_NT_Login", "Manager_NT_Login");
                sqlbc.ColumnMappings.Add("ValidityDate", "ValidityDate");
                sqlbc.ColumnMappings.Add("Certify_DSFlag", "Certify_DSFlag");
                sqlbc.ColumnMappings.Add("Exchange_DSFlag", "Exchange_DSFlag");
                sqlbc.ColumnMappings.Add("InsertionDate", "InsertionDate");
                sqlbc.ColumnMappings.Add("InitiatedBy", "InitiatedBy");
                sqlbc.ColumnMappings.Add("DumpDate", "DumpDate");
                sqlbc.ColumnMappings.Add("WorkLocation", "WorkLocation");
                sqlbc.WriteToServer(paycomInsertHistory_DataTable);
            }
            ApplicationTrace.Log("DumpPaycomDataInDB: DumpEmployeeRowInHistory", Status.Completed);
            return true;
        }

        #endregion DB Dumping

        #region AD

        public bool CreateAD_CSV(dynamic data, string FilePath)
        {
            try
            {
                ApplicationTrace.Log("PushEngineService:CreateAD_CSV", Status.Started);
                List<ActiveDirectoryDTO> records = new List<ActiveDirectoryDTO>();
                foreach (var item in data)
                {
                    records.Add(new ActiveDirectoryDTO()
                    {
                        Employee_Code = item?.Employee_Code,
                        FirstName = item?.Employee_FirstName,
                        MiddleName = item?.Employee_MiddleName,
                        LastName = item?.Employee_LastName,
                        Department_Desc = item?.Department_Desc,
                        ClockSeq = item?.ClockSeq,
                        City = item?.City2,
                        NTLogin = item?.NTLogin,
                        Work_Email = item?.Work_Email,
                        Position = item?.Position,
                        ManagerEmpID = item?.ManagerEmpID,
                        EntityName = item?.EntityName,
                        LineMgrEmailID = item?.LineMgrEmailID,
                        Manager_NT_Login = item?.Manager_NT_Login,
                    });
                }
                using (var writer = new StreamWriter(string.Concat(FilePath, "\\ActiveDirectory_", DateTime.Now.ToString("yyyyMMddhhmm"), ".", FileType.csv)))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(records);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushEngineService:CreateAD_CSV", Status.Failed);
                ExceptionHandling.LogException(ex, "PushEngineService:CreateAdminCSV");
                return false;
            }
            ApplicationTrace.Log("PushEngineService:CreateAD_CSV", Status.Completed);
            return true;
        }

        #endregion AD

        #region SD

        public bool CreateServiceDeskCSV(dynamic data, string FilePath)
        {
            try
            {
                ApplicationTrace.Log("PushEngineService:CreateServiceDeskCSV", Status.Started);
                List<ServiceDeskDTO> records = new List<ServiceDeskDTO>();
                foreach (var item in data)
                {
                    records.Add(new ServiceDeskDTO()
                    {
                        Employee_Code = item?.Employee_Code,
                        FirstName = item?.Employee_FirstName,
                        MiddleName = item?.Employee_MiddleName,
                        LastName = item?.Employee_LastName,
                        Department_Desc = item?.Department_Desc,
                        ClockSeq = item?.ClockSeq,
                        City = item?.City2,
                        NTLogin = item?.NTLogin,
                        Work_Email = item?.Work_Email,
                        Position = item?.Position,
                        ManagerEmpID = item?.ManagerEmpID,
                        EntityName = item?.EntityName,
                        LineMgrEmailID = item?.LineMgrEmailID,
                        Manager_NT_Login = item?.Manager_NT_Login,
                    });
                }
                using (var writer = new StreamWriter(string.Concat(FilePath, "\\NewEmployee_", DateTime.Now.ToString("yyyyMMddhhmm"), ".", FileType.csv)))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(records);
                    writer.Flush();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                ApplicationTrace.Log("PushEngineService:CreateServiceDeskCSV", Status.Failed);
                ExceptionHandling.LogException(ex, "PushEngineService" + ex.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushEngineService:CreateServiceDeskCSV", Status.Failed);
                ExceptionHandling.LogException(ex, "PushEngineService:CreateServiceDeskCSV");
                return false;
            }
            ApplicationTrace.Log("PushEngineService:CreateServiceDeskCSV", Status.Completed);
            return true;
        }

        #endregion SD

        #region Exchange

        public bool CreateExchangeCSV(dynamic data, string FilePath)
        {
            try
            {
                ApplicationTrace.Log("PushEngineService:CreateExchangeCSV", Status.Started);
                List<ExchangeDTO> records = new List<ExchangeDTO>();
                foreach (var item in data)
                {
                    records.Add(new ExchangeDTO()
                    {
                        Employee_Code = item?.Employee_Code,
                        FirstName = item?.Employee_FirstName,
                        MiddleName = item?.Employee_MiddleName,
                        LastName = item?.Employee_LastName,
                        Department_Desc = item?.Tier_3_Desc,
                        ClockSeq = item?.ClockSeq,
                        City = item?.City2,
                        NTLogin = item?.NTLogin,
                        Work_Email = item?.Work_Email,
                        Position = item?.Position,
                        ManagerEmpID = item?.ManagerEmpID,
                        EntityName = item?.EntityName,
                        LineMgrEmailID = item?.LineMgrEmailID,
                        Manager_NT_Login = item?.Manager_NT_Login,
                    });
                }
                using (var writer = new StreamWriter(string.Concat(FilePath, "\\Exchange_", DateTime.Now.ToString("yyyyMMddhhmm"), ".", FileType.csv)))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(records);
                    writer.Flush();
                }
                UpdateDeltaFlagForDownStream(RunningPaycomActivity.Exchange);
            }
            catch (UnauthorizedAccessException ex)
            {
                ApplicationTrace.Log("PushEngineService:CreateExchangeCSV", Status.Failed);
                ExceptionHandling.LogException(ex, "PushEngineService" + ex.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushEngineService:CreateExchangeCSV", Status.Failed);
                ExceptionHandling.LogException(ex, "PushEngineService:CreateExchangeCSV");
                return false;
            }
            ApplicationTrace.Log("PushEngineService:CreateExchangeCSV", Status.Completed);
            return true;
        }

        #endregion Exchange

        #region Certify

        public bool CreateCertifyExcelFile(dynamic data, string FilePath)
        {
            try
            {
                ApplicationTrace.Log("CreateCertifyExcelFile", Status.Started);
                IWorkbook workbook = new XSSFWorkbook();
                string[] sheetNames = { "FullData", "DeltaOnly" };
                List<CertifyDTO> items = new List<CertifyDTO>();
                PullEngineService PullService = new PullEngineService();
                var columns = new[] { "Employee_Code", "FirstName", "MiddleName", "LastName", "Department_Desc", "Tier_2_Desc", "Tier_3_Desc", "ClockSeq", "Hire_Date", "Termination_Date", "Supervisor_Primary", "City", "Country", "Currency", "City2", "Work_Email", "Position", "ManagerEmpID", "EntityName", "LineMgrEmailID" };
                var headers = new[] { "Employee Code", "First Name", "Middle Name", "Last Name", "Department", "Sub Department", "Team", "ClockSeq#", "Hire Date", "Termination Date", "Primary Supervisor", "City", "Country", "Currency", "International City", "Work Email", "Position", "Manager Employee ID", "Entity Name", "Manager Email ID" };
                List<PaycomData> workingData = new List<PaycomData>();
                for (int k = 0; k < sheetNames.Count(); k++)
                {
                    if (k == 0)
                        workingData = PullService.GetDownStreamData(RunningPaycomActivity.Certify, true);
                    else
                    {
                        workingData.Clear();
                        workingData = data;
                        items.Clear();
                    }
                    foreach (var item in workingData)
                    {
                        items.Add(new CertifyDTO()
                        {
                            Employee_Code = item?.Employee_Code,
                            FirstName = item?.Employee_FirstName,
                            MiddleName = item?.Employee_MiddleName,
                            LastName = item?.Employee_LastName,
                            Department_Desc = item?.Department_Desc,
                            Tier_2_Desc = item?.Tier_2_Desc,
                            Tier_3_Desc = item?.Tier_3_Desc,
                            ClockSeq = item?.ClockSeq,
                            Hire_Date = item?.Hire_Date,
                            Termination_Date = item?.Termination_Date,
                            Supervisor_Primary = item?.Supervisor_Primary,
                            City = item?.City1,
                            Country = item?.Country,
                            Currency = item?.Currency,
                            City2 = item?.City2,
                            Work_Email = item?.Work_Email,
                            Position = item?.Position,
                            ManagerEmpID = item?.ManagerEmpID,
                            EntityName = item?.EntityName,
                            LineMgrEmailID = item?.LineMgrEmailID,
                        });
                    }
                    ISheet sheet = workbook.CreateSheet(sheetNames[k]);
                    XSSFRow headerRow = (XSSFRow)sheet.CreateRow(0);
                    for (int i = 0; i < columns.Length; i++)
                    {
                        ICell cell = headerRow.CreateCell(i);
                        cell.SetCellValue(headers[i]);
                    }
                    for (int i = 0; i < items.Count; i++)
                    {
                        var rowIndex = i + 1;
                        var row = sheet.CreateRow(rowIndex);
                        for (int j = 0; j < columns.Length; j++)
                        {
                            ICell cell = row.CreateCell(j);
                            var o = items[i];
                            if (o != null)
                            {
                                if (o?.GetType()?.GetProperty(columns[j]) != null)
                                    cell.SetCellValue(o?.GetType()?.GetProperty(columns[j])?.GetValue(o, null)?.ToString());
                                else
                                    cell.SetCellValue("");
                            }
                            else
                                cell.SetCellValue("");
                        }
                    }
                }
                using (FileStream file = new FileStream(string.Concat(FilePath, "\\Certify_", DateTime.Now.ToString("yyyyMMddhhmm"), ".", FileType.xlsx), FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(file);
                    file.Close();
                }
                UpdateDeltaFlagForDownStream(RunningPaycomActivity.Certify);
            }
            catch (UnauthorizedAccessException ex)
            {
                ApplicationTrace.Log("CreateCertifyExcelFile", Status.Failed);
                ExceptionHandling.LogException(ex, "CreateCertifyExcelFiles" + ex.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("CreateCertifyExcelFile", Status.Failed);
                ExceptionHandling.LogException(ex, "CreateCertifyExcelFiles");
                return false;
            }
            ApplicationTrace.Log("CreateCertifyExcelFile", Status.Completed);
            return true;
        }

        #endregion Certify

        #region TK

        static readonly string TimeKeepingURL = ConfigurationManager.AppSettings["TimeKeepingURL"];
        public string DumpEmployeesToTKDatabase()
        {
            string CrowdURL = string.Empty;
            using (var context = new PaycomEngineContext())
            {
                CrowdURL = EngineAPIConfigService.CrowdTokenURL;
            }
            JObject objData = new JObject
            {
                {
                    "CrowdSSOToken", General.GetCrowdTokenAsync(CrowdURL).CrowdSSOToken
                }
            };
            var json = JsonConvert.SerializeObject(objData);
            var stringContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
            HttpResponseMessage Result;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(TimeKeepingURL);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Result = client.PostAsync(TimeKeepingURL, stringContent).Result;
            }
            if (!Result.IsSuccessStatusCode)
            {
                throw new Exception(Result.ReasonPhrase);
            }
            return Result.StatusCode.ToString();
        }

        #endregion TK

        #region HeadCountRpt
        public bool CreateHeadCountRpt(dynamic data, string FilePath)
        {
            try
            {
                ApplicationTrace.Log("CreateHeadCountRpt", Status.Started);
                List<HeadCountRptDTO> rows = new List<HeadCountRptDTO>();
                List<HeadCountRptDTO> DBItems = new List<HeadCountRptDTO>();
                foreach (var item in data)
                {
                    List<string> eName = new List<string>();
                    eName.Add(item.Employee_FirstName);
                    if (!string.IsNullOrEmpty(item.Employee_MiddleName))
                    {
                        eName.Add(item.Employee_MiddleName);
                    }
                    eName.Add(item.Employee_LastName);
                    DBItems.Add(new HeadCountRptDTO()
                    {
                        EmployeeID = item?.ClockSeq,
                        EmployeeName = string.Join(" ", eName),
                        EmailID = item?.Work_Email,
                        Designation = item?.Position,
                        Team = item?.Tier_3_Desc,
                        LineManager = item?.Supervisor_Primary,
                        DateOfJoining = (item?.Hire_Date) != "00/00/0000" && (item?.Hire_Date) != null ? DateTime.Parse(item?.Hire_Date) : default(DateTime),
                        Country = item?.Country,
                        Location = item?.City2,
                        LastWorkingDate = item?.Termination_Date != "00/00/0000" && item?.Termination_Date != null ? DateTime.Parse(item?.Termination_Date) : default(DateTime),
                        Reason = "",
                        Status = item?.Employee_Status
                    });
                }
                IWorkbook workbook = new XSSFWorkbook();
                IFont fontBold = workbook.CreateFont();
                fontBold.IsBold = true;
                fontBold.FontName = "Josefin Sans Regular";
                fontBold.FontHeightInPoints = 12;
                ICellStyle boldStyle = workbook.CreateCellStyle();
                boldStyle.SetFont(fontBold);
                boldStyle.BorderLeft = BorderStyle.Thin;
                boldStyle.BorderBottom = BorderStyle.Thin;
                boldStyle.BorderRight = BorderStyle.Thin;
                boldStyle.BorderTop = BorderStyle.Thin;
                boldStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
                boldStyle.FillPattern = FillPattern.SolidForeground;

                IFont fontNormal = workbook.CreateFont();
                fontNormal.FontName = "Proxima Nova Regular";
                fontNormal.FontHeightInPoints = 10;
                ICellStyle normalStyle = workbook.CreateCellStyle();
                normalStyle.SetFont(fontNormal);
                normalStyle.BorderLeft = BorderStyle.Thin;
                normalStyle.BorderBottom = BorderStyle.Thin;
                normalStyle.BorderRight = BorderStyle.Thin;
                normalStyle.BorderTop = BorderStyle.Thin;

                ICellStyle customNumberStyle = workbook.CreateCellStyle();
                IDataFormat numberFormatCustom = workbook.CreateDataFormat();
                customNumberStyle.DataFormat = numberFormatCustom.GetFormat("#####");
                customNumberStyle.SetFont(fontNormal);
                customNumberStyle.BorderLeft = BorderStyle.Thin;
                customNumberStyle.BorderBottom = BorderStyle.Thin;
                customNumberStyle.BorderRight = BorderStyle.Thin;
                customNumberStyle.BorderTop = BorderStyle.Thin;

                ICellStyle customDateStyle = workbook.CreateCellStyle();
                IDataFormat dataFormatCustom = workbook.CreateDataFormat();
                customDateStyle.DataFormat = dataFormatCustom.GetFormat("dd-MMM-yyyy");
                customDateStyle.SetFont(fontNormal);
                customDateStyle.BorderLeft = BorderStyle.Thin;
                customDateStyle.BorderBottom = BorderStyle.Thin;
                customDateStyle.BorderRight = BorderStyle.Thin;
                customDateStyle.BorderTop = BorderStyle.Thin;

                string[] sheetsNames = { "Active", "Summary", "Separations" };
                string[] headers = new string[] { }, columns = new string[] { };
                var columns0 = new[] { "EmployeeID", "EmployeeName", "EmailID", "Designation", "Team", "LineManager", "DateOfJoining", "Country", "Location" };
                var headers0 = new[] { "Employee ID", "Employee Name", "Email", "Designation", "Team", "Line Manager", "Date of Joining", "Country", "Location" };
                var columns1 = new[] { "Team", "EmployeeCount" };
                var headers1 = new[] { "Team", "Employee Count" };
                var columns2 = new[] { "EmployeeID", "EmployeeName", "EmailID", "Designation", "Team", "LineManager", "DateOfJoining", "Country", "LastWorkingDate", "Reason" };
                var headers2 = new[] { "Employee ID", "Employee Name", "Email", "Designation", "Team", "Line Manager", "Date of Joining", "Country", "Last Working Day", "Reason" };

                for (int k = 0; k < sheetsNames.Count(); k++)
                {
                    ISheet sheet = workbook.CreateSheet(sheetsNames[k]);
                    XSSFRow headerRow = (XSSFRow)sheet.CreateRow(0);
                    if (k == 0)
                    {
                        headers = headers0;
                        columns = columns0;
                        rows = DBItems.Where(x => x.Status == "Active").OrderBy(y => y.DateOfJoining).ToList();
                        sheet.CreateFreezePane(2, 1);
                    }
                    else if (k == 1)
                    {
                        headers = headers1;
                        columns = columns1;
                        rows = DBItems.Where(x => x.Status == "Active").GroupBy(n => n.Team).Select(n => new HeadCountRptDTO
                        {
                            Team = n.Key,
                            EmployeeCount = n.Count()
                        }).OrderBy(n => n.Team).ToList();
                        rows.Add(new HeadCountRptDTO() { Team = "Grand Total", EmployeeCount = rows.Sum(x => x.EmployeeCount) });
                    }
                    else if (k == 2)
                    {
                        headers = headers2;
                        columns = columns2;
                        rows = DBItems.Where(x => x.Status != "Active").OrderBy(y => y.LastWorkingDate).ToList();
                        sheet.CreateFreezePane(2, 1);
                    }
                    for (int i = 0; i < columns.Length; i++)
                    {
                        ICell cell = headerRow.CreateCell(i);
                        cell.SetCellValue(headers[i]);
                        cell.CellStyle = boldStyle;
                    }
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var rowIndex = i + 1;
                        var row = sheet.CreateRow(rowIndex);
                        for (int j = 0; j < columns.Length; j++)
                        {
                            ICell cell = row.CreateCell(j);
                            var o = rows[i];
                            var type = o?.GetType()?.GetProperty(columns[j]).PropertyType.Name;

                            // Cell Styling          
                            cell.CellStyle = normalStyle;

                            if (o != null)
                            {
                                if (o?.GetType()?.GetProperty(columns[j]) != null)
                                {
                                    if (k == 1 && type == "Int32")// Numeric column for Summary tab
                                    {
                                        cell.SetCellValue(Convert.ToInt32(o?.GetType()?.GetProperty(columns[j])?.GetValue(o, null)?.ToString()));
                                        cell.CellStyle = customNumberStyle;
                                    }
                                    else if (type == "DateTime")
                                    {
                                        if (o?.GetType()?.GetProperty(columns[j])?.GetValue(o, null)?.ToString() != default(DateTime).ToString())
                                        {
                                            cell.SetCellValue(Convert.ToDateTime(o?.GetType()?.GetProperty(columns[j])?.GetValue(o, null)?.ToString()));
                                            cell.CellStyle = customDateStyle;
                                        }
                                        else
                                        {
                                            cell.SetCellValue("");
                                            cell.CellStyle = normalStyle;
                                        }
                                    }
                                    else
                                        cell.SetCellValue(o?.GetType()?.GetProperty(columns[j])?.GetValue(o, null)?.ToString());
                                }
                                else
                                {
                                    if (k == 1 && type == "Int32")// Numeric column for Summary tab
                                        cell.SetCellValue(Convert.ToInt32(0));
                                    else
                                        cell.SetCellValue("");
                                }
                            }
                            else
                                cell.SetCellValue("");
                            if (i == rows.Count - 1 && k == 1)// Last rows for Summary tab
                                cell.CellStyle = boldStyle;
                        }
                    }
                }
                using (FileStream file = new FileStream(string.Concat(FilePath, "\\GSD Headcount Sheet ", DateTime.Now.ToString("dd-MMM-yyyy"), "_GSD Planning Team.", FileType.xlsx), FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(file);
                    file.Close();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                ApplicationTrace.Log("CreateHeadCountRpt", Status.Failed);
                ExceptionHandling.LogException(ex, "CreateHeadCountRpt" + ex.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("CreateHeadCountRpt", Status.Failed);
                ExceptionHandling.LogException(ex, "CreateHeadCountRpt");
                return false;
            }
            ApplicationTrace.Log("CreateHeadCountRpt", Status.Completed);
            return true;
        }

        #endregion HeadCountRpt

        #region Everbridge

        public bool CreateEverbridgeCSV(List<PaycomData> data, string FilePath)
        {
            try
            {
                ApplicationTrace.Log("PushEngineService:CreateEverbridgeCSV", Status.Started);
                List<EverBridgeDTO> records = new List<EverBridgeDTO>();
                foreach (var item in data)
                {
                    records.Add(new EverBridgeDTO()
                    {
                        FirstName = item?.Employee_FirstName,
                        MiddleInitial = !string.IsNullOrEmpty(item?.Employee_MiddleName) ? item?.Employee_MiddleName?.Substring(0, 1) : item?.Employee_MiddleName,
                        LastName = item?.Employee_LastName,
                        ExternalID = item?.ClockSeq,
                        Country = item?.Country == "UK" ? "United Kingdom" : item?.Country == "US" ? "United States" : item?.Country,
                        RecordType = "Employee",
                        SSOUserID = item?.Work_Email,
                        Location1 = item?.WorkLocation,
                        LocationId1 = item?.WorkLocation,
                        EmailAddress1 = item?.Work_Email,
                        End = ""
                    });
                }
                using (var writer = new StreamWriter(string.Concat(FilePath, "\\Everbridge_", DateTime.Now.ToString("yyyyMMddhhmm"), ".", FileType.csv)))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.RegisterClassMap<EverBridgeDTOMap>();
                    csv.WriteRecords(records);
                    writer.Flush();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                ApplicationTrace.Log("PushEngineService:CreateEverbridgeCSV", Status.Failed);
                ExceptionHandling.LogException(ex, "PushEngineService" + ex.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                ApplicationTrace.Log("PushEngineService:CreateEverbridgeCSV", Status.Failed);
                ExceptionHandling.LogException(ex, "PushEngineService:CreateExchangeCSV");
                return false;
            }
            ApplicationTrace.Log("PushEngineService:CreateEverbridgeCSV", Status.Completed);
            return true;
        }

        #endregion Everbridge

        #region DownstreamsCreationGeneric
        public bool UpdateDeltaFlagForDownStream(RunningPaycomActivity activity)
        {
            ApplicationTrace.Log("PushEngineService: UpdateDeltaFlagForDownStream", activity.ToString(), Status.Started);
            List<PaycomData> baseData = new List<PaycomData>();
            var filter = new FilterLinq<PaycomData>();
            List<ConfigurationSettingDetail> predicateSettings = new List<ConfigurationSettingDetail>();
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                predicateSettings = (from det in context.ConfigurationSettingDetail
                                     join p in context.ConfigurationSetting on det.ConfigAppKey equals p.ConfigValueKey
                                     where (det.AdditionalParamName == "Include" || det.AdditionalParamName == "Exclude")
                                     && p.IsActive == true && det.IsActive == true && p.ConfigAppEvent == activity.ToString()
                                     select det).ToList();
            }
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                if (activity == RunningPaycomActivity.Certify)
                {
                    if (filter.GetWherePredicate(predicateSettings) != null)
                        context.PaycomData.Where(filter.GetWherePredicate(predicateSettings)).ToList().ForEach(a => a.Certify_DSFlag = "0");
                    else
                        context.PaycomData.ToList().ForEach(a => a.Certify_DSFlag = "0");
                }
                else if (activity == RunningPaycomActivity.Exchange)
                {
                    if (filter.GetWherePredicate(predicateSettings) != null)
                        context.PaycomData.Where(filter.GetWherePredicate(predicateSettings)).ToList().ForEach(a => a.Exchange_DSFlag = "0");
                    else
                        context.PaycomData.ToList().ForEach(a => a.Exchange_DSFlag = "0");
                }
                context.SaveChanges();
            }
            ApplicationTrace.Log("PushEngineService: UpdateDeltaFlagForDownStream", activity.ToString(), Status.Completed);
            return true;
        }
        public bool PushDownstreams(string paycomActivity, dynamic data, string FilePath)
        {
            Enum.TryParse(paycomActivity, out RunningPaycomActivity CurrentpaycomActivity);
            var response = false;
            switch (CurrentpaycomActivity)
            {
                case RunningPaycomActivity.AD:
                    ActiveDirectoryDTO admin = new ActiveDirectoryDTO();
                    response = CallPaycom_PolymorphicForm(admin, data, FilePath);
                    break;
                case RunningPaycomActivity.Exchange:
                    ExchangeDTO exchange = new ExchangeDTO();
                    response = CallPaycom_PolymorphicForm(exchange, data, FilePath);
                    break;
                case RunningPaycomActivity.HeadCountRpt:
                    HeadCountRptDTO headCountRpt = new HeadCountRptDTO();
                    response = CallPaycom_PolymorphicForm(headCountRpt, data, FilePath);
                    break;
                case RunningPaycomActivity.SD:
                    ServiceDeskDTO sd = new ServiceDeskDTO();
                    response = CallPaycom_PolymorphicForm(sd, data, FilePath);
                    break;
                case RunningPaycomActivity.Certify:
                    CertifyDTO certify = new CertifyDTO();
                    response = CallPaycom_PolymorphicForm(certify, data, FilePath);
                    break;
                case RunningPaycomActivity.EverBridge:
                    EverBridgeDTO everBridge = new EverBridgeDTO();
                    response = CallPaycom_PolymorphicForm(everBridge, data, FilePath);
                    break;
                default:
                    break;
            }
            return response;
        }

        private bool CallPaycom_PolymorphicForm(PaycomBaseDTO paycomBase, dynamic data, string FilePath)
        {
            return paycomBase.CreateDownStreamFile(data, FilePath);
        }

        #endregion DownstreamsCreationGeneric
    }
}