using Afiniti.Paycom.DAL;
using Afiniti.Paycom.Shared.Models;
using Afiniti.PaycomEngine.Helpers;
using Newtonsoft.Json;
using Npoi.Mapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using static Afiniti.Paycom.Shared.Enums;

//https://github.com/donnytian/Npoi.Mapper
//https://thecodebuzz.com/read-and-write-excel-file-in-net-core-using-npoi/
//https://dotnet.libhunt.com/compare-npoi-vs-open-xml-sdk?rel=cmp-cmp
//NPOI is one of the most famous and widely used API to read/write excels.

namespace Afiniti.PaycomEngine.Services
{
    public class Data_ScruberAndValidator
    {
        private List<PaycomData_Scrubbed> PaycomDataObj = new List<PaycomData_Scrubbed>();
        private DBLogsService processLogs = new DBLogsService();
        private string fileName = string.Empty;
        public dynamic PrepareDataAfterScrubbingValidations(string FileNameWithPath)
        {
            fileName = Path.GetFileName(FileNameWithPath);
            var dtTable = ReadExcelInDT(FileNameWithPath);
            if (dtTable == null) return false;
            PaycomDataObj = Common.ConvertDataTable<PaycomData_Scrubbed>(dtTable);

            Do_DataScrubbing();
            // insertion into Scrub table after scrubbing
            if (PaycomDataObj.Count > 0)
            {
                ScrubbedDataInsertion();
            }
            Do_MandatoryValidationsOnScrubbedData();
            return true;
        }

        private DataTable ReadExcelInDT(string FileNameWithPath)
        {
            List<string> Excel_ColsList = new List<string>();
            List<string> UnMappedColumnsList = new List<string>();
            List<string> rowList = new List<string>();
            ISheet sheet;
            List<FileStream_BaseColumns> baseColumns = new List<FileStream_BaseColumns>();
            List<PaycomData_Logs_Level1> process_Level1Logs = new List<PaycomData_Logs_Level1>();
            DataTable dtTable = new DataTable();
            using (var stream = new FileStream(FileNameWithPath, FileMode.Open))
            {
                stream.Position = 0;
                XSSFWorkbook xssWorkbook = new XSSFWorkbook(stream);
                xssWorkbook.MissingCellPolicy = MissingCellPolicy.RETURN_NULL_AND_BLANK;
                sheet = xssWorkbook.GetSheetAt(0);
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                if (cellCount > 0)
                {
                    for (int i = 0; i < cellCount; i++)
                    {
                        Excel_ColsList.Add(xssWorkbook.GetSheetAt(0).GetRow(0).Cells[i].StringCellValue);
                    }
                }
                using (var context = new PaycomEngineContext())
                {
                    baseColumns = context.FileStream_BaseColumns.AsNoTracking().Where(x => x.IsActive).ToList();
                }
                for (int j = 0; j < cellCount; j++)//Column names mapping as per configured names in DB
                {
                    ICell cell = headerRow.GetCell(j);
                    if (cell == null || string.IsNullOrWhiteSpace(cell.ToString()))
                    {
                        continue;
                    }
                    //get ColumnKeys as per column names of excel 
                    //BaseColumnName = ColumnNameKey which is as per system
                    //BaseColumnDescription = ColumnNameValue as per Excel column names
                    string colName = string.Empty;
                    colName = baseColumns.FirstOrDefault(x => x.BaseColumnDescription.Trim().ToLower() == cell?.ToString().Trim().ToLower())?.BaseColumnName;
                    if (colName == null || colName == "")//if col is in file but not in DB mapping then maintain its info in log table and proceed file processing
                    {
                        colName = cell?.ToString().Trim();
                        UnMappedColumnsList.Add(colName.Trim());
                    }
                    dtTable.Columns.Add(colName);
                }
                var p_baseColumns = baseColumns.Select(c => c.BaseColumnDescription.Trim().ToLower()).OrderBy(t => t).ToList();
                var p_Excel_ColsList = Excel_ColsList.Except(UnMappedColumnsList).OrderBy(t => t).Select(x => x.ToLower()).ToList();
                bool ColsComparison = Enumerable.SequenceEqual(p_baseColumns, p_Excel_ColsList);
                if (!ColsComparison)// if Excel Col names are not same and equal to DB configured col names then return with error
                {
                    if (p_baseColumns.Any() && p_Excel_ColsList.Any())
                        processLogs.LogMessagesForLevel1InDB($"{string.Concat(string.Join(" , ", p_baseColumns.Except(p_Excel_ColsList).ToList()), " column(s) are missing in Excel file. Please add them in file and try upload again")}", FileNameWithPath, LogType.Error, LogStage.Scrubbing);
                    return null;
                }
                for (int i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }
                    if (row.Cells.All(d => d.CellType == CellType.Blank))
                    {
                        continue;
                    }
                    for (int j = row.FirstCellNum; j < cellCount; j++)
                    {
                        if (row.GetCell(j, MissingCellPolicy.CREATE_NULL_AS_BLANK) != null)//TODO: if required to handle null/blank cells then. for now just log
                        {
                            if (row.GetCell(j)?.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(row.GetCell(j)))
                            {
                                var dateVal = DateTime.FromOADate(row.GetCell(j).NumericCellValue).ToString();
                                rowList.Add(dateVal);
                            }
                            else
                            {
                                rowList.Add(row.GetCell(j)?.ToString());
                            }
                        }
                        else
                        {
                            PaycomData_Logs_Level1 scrub_Log = new PaycomData_Logs_Level1()
                            {
                                LogDetail = string.Concat("Row ", i, " and column ", j, " is null in File: ", fileName),
                                LogDate = DateTime.Now,
                                FileName = fileName,
                                LogType = LogType.Warning.ToString(),
                                LogStage = nameof(LogStage.Scrubbing)
                            };
                            process_Level1Logs.Add(scrub_Log);
                        }
                    }
                    if (rowList.Count > 0)
                    {
                        dtTable.Rows.Add(rowList.ToArray());
                    }
                    rowList.Clear();
                }
            }
            if (process_Level1Logs.Any())
                processLogs.LogMessagesForLevel1InDB(process_Level1Logs);
            if (UnMappedColumnsList.Any())
                processLogs.LogMessagesForLevel1InDB($"{string.Concat(string.Join(" , ", UnMappedColumnsList), " column(s) are available in ", fileName, " Paycom excel file but not in DB schema so its data will not be dumped.")}", FileNameWithPath, LogType.Warning, LogStage.Scrubbing);
            return dtTable;
        }

        private void Do_DataScrubbing()//Scrubbing on Incoming Paycom Data 
        {
            List<PaycomData> DB_Data = new List<PaycomData>();
            using (var context = new PaycomEngineContext())
            {
                DB_Data = context.PaycomData.AsNoTracking().ToList();
            }

            // 1. NULL ClockSeq and mapping of EmployeeCode for it
            List<PaycomData_Logs_Level2> process_Level2Logs = new List<PaycomData_Logs_Level2>();
            var BlankClockSeqs = PaycomDataObj.Where(x => x.ClockSeq == null || string.IsNullOrEmpty(x.ClockSeq)).ToList();
            foreach (var item in BlankClockSeqs)
            {
                item.ClockSeq = item?.Employee_Code;
            }
            if (BlankClockSeqs.Any())
            {
                var EmpCodes = BlankClockSeqs.Select(p => p.Employee_Code).ToArray();
                processLogs.LogMessagesForLevel1InDB($"ClockSeq is missing for {string.Join(",", EmpCodes)} Employee Code(s) in file. So EmployeeCode is mapped as ClockSeq in DB", fileName, LogType.Warning, LogStage.Scrubbing);
                processLogs.LogMessagesForLevel2InDB(string.Join(",", EmpCodes), "ClockSeq is missing for Employee Code ", fileName, LogType.Warning, LogStage.Scrubbing, "ClockSeq#");
            }

            // 2. Resolve Manager NT_Login/Email based on ManagerEmpID   
            foreach (var item in PaycomDataObj.Where(x => x.ManagerEmpID != null && x.ManagerEmpID != ""))
            {
                item.LineMgrEmailID = PaycomDataObj.FirstOrDefault(x => x.Employee_Code == item.ManagerEmpID)?.Work_Email ?? DB_Data.FirstOrDefault(x => x.Employee_Code == item.ManagerEmpID)?.Work_Email;
                item.Manager_NT_Login = PaycomDataObj.FirstOrDefault(x => x.Employee_Code == item.ManagerEmpID)?.NTLogin ?? DB_Data.FirstOrDefault(x => x.Employee_Code == item.ManagerEmpID)?.NTLogin;
                item.ManagerEmpID = PaycomDataObj.FirstOrDefault(x => x.Employee_Code == item.ManagerEmpID)?.ClockSeq ?? DB_Data.FirstOrDefault(x => x.Employee_Code == item.ManagerEmpID)?.ClockSeq;
            };

            // 3. if multiple ClockSeqs for same EMPLOYEE then take the row only which has Active Employee_Status for a ClockSeq
            var getActiveRepeated = PaycomDataObj.GroupBy(x => x.ClockSeq).Where(z => z.Count() > 1).SelectMany(x => x).Where(x => x.Employee_Status == "Active").ToList();
            PaycomDataObj = PaycomDataObj.GroupBy(x => x.ClockSeq).Where(z => z.Count() == 1).SelectMany(x => x).ToList();
            if (getActiveRepeated.Any())
            {
                PaycomDataObj.AddRange(getActiveRepeated);
                var ClockIds = getActiveRepeated.Where(x => x.ClockSeq != "").Select(p => p.ClockSeq).ToArray();
                processLogs.LogMessagesForLevel1InDB($"{string.Join(",", ClockIds)} ClockSeq(s) are duplicate in file. But only Active ones are dumped in DB", fileName, LogType.Warning, LogStage.Scrubbing);
                processLogs.LogMessagesForLevel2InDB(string.Join(",", ClockIds), "ClockSeq is duplicated for ", fileName, LogType.Warning, LogStage.Scrubbing, "ClockSeq#");
            }

            // 4. Set NewEmployee_Flag for Scrub table
            if (DB_Data.Count > 0)
            {
                foreach (var item in PaycomDataObj)
                {
                    item.NewEmployee_Flag = !DB_Data.Any(x => x.ClockSeq == item.ClockSeq);
                };
            }
            foreach (var item in PaycomDataObj)
            {
                item.InsertionDate = DateTime.Now;
                item.InitiatedBy = "PaycomEngineUser";
            };
        }

        //Validations is done on scrubbed Data
        private void Do_MandatoryValidationsOnScrubbedData()//Picks configured mandatory cols from DB and check data for them in each row 
        {
            List<PaycomData_Scrubbed> validatingData = new List<PaycomData_Scrubbed>();
            var filter = new FilterLinq<PaycomData_Scrubbed>();
            string whereFieldColumnList = string.Empty, whereFieldValues = string.Empty, Comparison = string.Empty;
            var mandatoryDBColumnsList = new List<string>();
            using (var context = new PaycomEngineContext())
            {
                mandatoryDBColumnsList = context.FileStream_BaseColumns.AsNoTracking().Where(x => x.IsMandatory == true).OrderBy(y => y.MandatoryCheck_Order).Select(y => y.BaseColumnName).ToList();
            }
            foreach (var mandatoryColumn in mandatoryDBColumnsList)
            {
                using (PaycomEngineContext context = new PaycomEngineContext())
                {
                    //If Line Manger is null in file EXCEPT "MUHAMMAD ZIAULLAH CHISHTI" then log in [PaycomData_Scrub_Logs]
                    if ((new[] { "ManagerEmpID", "Manager_NT_Login", "LineMgrEmailID" }).Contains(mandatoryColumn, StringComparer.OrdinalIgnoreCase))
                        validatingData = context.PaycomData_Scrubbed.Where(x => x.Employee_Status == "Active" && x.ClockSeq != "24968" && x.ValidationFail_Flag != true).Where(filter.GetWherePredicate(mandatoryColumn)).ToList();
                    else
                    {
                        if (filter.GetWherePredicate(mandatoryColumn) != null)
                            validatingData = context.PaycomData_Scrubbed.Where(x => x.Employee_Status == "Active" && x.ValidationFail_Flag != true).Where(filter.GetWherePredicate(mandatoryColumn)).ToList();
                        else
                            validatingData = context.PaycomData_Scrubbed.Where(x => x.Employee_Status == "Active" && x.ValidationFail_Flag != true).ToList();
                    }
                    if (validatingData.Count > 0)
                    {
                        foreach (PaycomData_Scrubbed p in validatingData)
                        {
                            p.ValidationFail_Flag = true;//now the record is marked as Validation failure
                        }
                        context.SaveChanges();
                        processLogs.LogMessagesForLevel2InDB(string.Join(",", validatingData.Select(x => x.ClockSeq)), $"{mandatoryColumn} - Mandatory value is missing for ClockSeq: ", fileName, LogType.Error, LogStage.Validation, mandatoryColumn);
                    }
                }
            }
        }

        private bool ScrubbedDataInsertion()
        {
            string sConnection = string.Empty;
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                context.Database.ExecuteSqlCommand("IF EXISTS(SELECT* FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PaycomData_Scrubbed') TRUNCATE TABLE [PaycomData_Scrubbed]");
                sConnection = context.Database.Connection.ConnectionString;
            }
            DataTable scrubbedInsert_DataTable = Common.ToDataTable(PaycomDataObj);
            using (SqlBulkCopy sqlbc = new SqlBulkCopy(sConnection))
            {
                sqlbc.DestinationTableName = "PaycomData_Scrubbed";
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
                sqlbc.ColumnMappings.Add("InitiatedBy", "InitiatedBy");
                sqlbc.ColumnMappings.Add("InsertionDate", "InsertionDate");
                sqlbc.ColumnMappings.Add("Certify_DSFlag", "Certify_DSFlag");
                sqlbc.ColumnMappings.Add("Exchange_DSFlag", "Exchange_DSFlag");
                sqlbc.ColumnMappings.Add("NewEmployee_Flag", "NewEmployee_Flag");
                sqlbc.ColumnMappings.Add("WorkLocation", "WorkLocation");
                sqlbc.WriteToServer(scrubbedInsert_DataTable);
            }
            return true;
        }

        public string ReadExcelInJson(string FileNameWithPath)//not in use for now
        {
            DataTable dtTable = new DataTable();
            List<string> rowList = new List<string>();
            ISheet sheet;
            using (var stream = new FileStream(FileNameWithPath, FileMode.Open))
            {
                stream.Position = 0;
                XSSFWorkbook xssWorkbook = new XSSFWorkbook(stream);
                sheet = xssWorkbook.GetSheetAt(0);
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                for (int j = 0; j < cellCount; j++)
                {
                    ICell cell = headerRow.GetCell(j);
                    //if (cell == null || string.IsNullOrWhiteSpace(cell.ToString()))
                    //{
                    //    continue;
                    //}
                    dtTable.Columns.Add(cell.ToString());
                }
                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }
                    if (row.Cells.All(d => d.CellType == CellType.Blank))
                    {
                        continue;
                    }
                    for (int j = row.FirstCellNum; j < cellCount; j++)
                    {
                        //  if (row.GetCell(j) != null)
                        //  {
                        //    if (!string.IsNullOrEmpty(row.GetCell(j).ToString()) & !string.IsNullOrWhiteSpace(row.GetCell(j).ToString()))
                        //  {
                        if (row.GetCell(j)?.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(row.GetCell(j)))
                        {
                            //var cellformat = DateUtil.IsCellDateFormatted(row.GetCell(j));
                            //var datecell = row.GetCell(j)?.DateCellValue.ToString();
                            //var nbrcellval = row.GetCell(j)?.NumericCellValue.ToString();

                            var dateVal = DateTime.FromOADate(row.GetCell(j).NumericCellValue).ToString();
                            //var dateVal = row.GetCell(j)?.DateCellValue.ToString();
                            rowList.Add(dateVal);
                        }
                        else
                        {
                            rowList.Add(row.GetCell(j)?.ToString());
                        }

                        // }
                        //   }
                    }
                    if (rowList.Count > 0)
                    {
                        dtTable.Rows.Add(rowList.ToArray());
                    }

                    rowList.Clear();
                }
            }
            return JsonConvert.SerializeObject(dtTable);
        }

        public dynamic ReadExcelInCustomDTO(string FileNameWithPath)//not in use
        {
            IWorkbook workbook;
            using (FileStream file = new FileStream(FileNameWithPath, FileMode.Open, FileAccess.Read))
            {
                workbook = WorkbookFactory.Create(file);
                workbook.MissingCellPolicy = MissingCellPolicy.RETURN_NULL_AND_BLANK;
            }
            // LogColumnsData(workbook, FileNameWithPath);

            var mapper = new Mapper(workbook);
            List<RowInfo<PaycomRequestDTO>> PaycomDataObj = mapper.Take<PaycomRequestDTO>(0)?.ToList();//always read 1st excel sheet           
            if (PaycomDataObj.Where(x => x.ErrorColumnIndex != -1).ToList().Count() == 0)//there is no issue in Excel parsing
            {
                foreach (var item in PaycomDataObj.Where(x => x.Value.ManagerEmpID != null))
                {
                    item.Value.LineMgrEmailID = PaycomDataObj.FirstOrDefault(x => x.Value.Employee_Code == item.Value.ManagerEmpID)?.Value?.Work_Email;
                    item.Value.Manager_NT_Login = PaycomDataObj.FirstOrDefault(x => x.Value.Employee_Code == item.Value.ManagerEmpID)?.Value?.NTLogin;
                    item.Value.ManagerEmpID = PaycomDataObj.FirstOrDefault(x => x.Value.Employee_Code == item.Value.ManagerEmpID)?.Value?.ClockSeq;
                };
                foreach (var item in PaycomDataObj.Where(x => x.Value.ClockSeq == null || string.IsNullOrEmpty(x.Value.ClockSeq)))
                {
                    item.Value.ClockSeq = item?.Value?.Employee_Code;
                }
                //Scrubbing of data, if multiple ClockSeqs then take Last row
                PaycomDataObj = PaycomDataObj.GroupBy(x => x.Value.ClockSeq).Select(y => y.LastOrDefault()).ToList();
                return PaycomDataObj;
            }
            else
            {
                return null;
            }
        }
    }
}