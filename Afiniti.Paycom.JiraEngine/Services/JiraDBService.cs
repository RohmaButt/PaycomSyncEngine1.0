using Afiniti.Paycom.DAL;
using Afiniti.Paycom.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.Paycom.JiraEngine.Services
{
    public class JiraDBService
    {
        #region Configs
        public List<ConfigurationSettingDetail> GetJiraConfigs()
        {
            List<ConfigurationSettingDetail> data_ForJira = new List<ConfigurationSettingDetail>();
            using (var context = new PaycomEngineContext())
            {
                var JiraEngineConfigs = (from det in context.ConfigurationSettingDetail.AsNoTracking()
                                         join parent in context.ConfigurationSetting.AsNoTracking() on det.ConfigAppKey equals parent.ConfigValueKey
                                         where parent.IsActive == true && det.IsActive == true && (parent.ConfigAppEvent == "JiraConfigs" || parent.ConfigAppEvent == "WebHooks")
                                         select det).ToList();
                data_ForJira.AddRange(JiraEngineConfigs);
            }
            return data_ForJira;
        }
        public string GetCrowdFromDB()
        {
            using (var context = new PaycomEngineContext())
                return context.ConfigurationSetting.AsNoTracking().FirstOrDefault(x => x.IsActive == true && x.ConfigAppEvent == "CrowdTokenURL").ConfigValue;
        }

        #endregion Configs

        public List<PaycomData_MissingInfo> GetEmployeesWith_BlankNTLoginOrWorkEmail()
        {
            List<PaycomData_MissingInfo> data_ForJira = new List<PaycomData_MissingInfo>();
            using (var context = new PaycomEngineContext())
            {
                data_ForJira = context.PaycomData_MissingInfo.AsNoTracking().Where(x => x.Is_Processed == PaycomData_MissingInfoStatus.NotProcessed.ToString() && x.IsActive == "1" && (string.IsNullOrEmpty(x.Work_Email) || string.IsNullOrEmpty(x.NT_Login))).ToList();
            }
            return data_ForJira;
        }
        public List<Issue_Column> GetTaskColumns(CallModel model)
        {
            var columns = new List<Issue_Column>();
            var activeColumns = GetColumnsFromDb();

            foreach (var prop in model.GetType().GetProperties())
            {
                var matchedColumsn = activeColumns.FirstOrDefault(x => x.ColumnName.ToLower().Trim().Equals(prop.Name.ToLower().Trim()));
                if (matchedColumsn != null)
                {
                    var colVal = prop.GetValue(model, null).ToString();
                    var type = matchedColumsn.ColumnType != null ? Convert.ToInt32(matchedColumsn.ColumnType) : 16;
                    columns.Add(new Issue_Column { Column_Name = matchedColumsn.JiraMapping, Column_Value = colVal, Value_Type = (Column_Type)type });
                }
            }
            return columns;
        }
        public List<JiraTicket_ColumnConfigurations> GetColumnsFromDb()
        {
            using (var dbContext = new PaycomEngineContext())
            {
                var columns = dbContext.JiraTicket_ColumnConfigurations.AsNoTracking().Where(col => col.IsActive && col.IsExternal).ToList().Select(obj => new JiraTicket_ColumnConfigurations
                {
                    ColumnType = obj.ColumnType,
                    ColumnId = obj.ColumnId,
                    ColumnKey = obj.ColumnKey,
                    ColumnName = obj.ColumnName,
                    IsActive = obj.IsActive,
                    IsEncrypted = obj.IsEncrypted,
                    IsExternal = obj.IsExternal,
                    JiraMapping = obj.JiraMapping
                }).ToList();
                return columns;
            }
        }

        public void AddNewTaskToDB(CallModel calldata)
        {
            Task.Factory.StartNew(() => AddDataInDB(calldata)).ContinueWith(m =>
            {
                //log response result
            });
        }
        public int AddDataInDB(CallModel calldata)
        {
            using (var dbContext = new PaycomEngineContext())
            {
                var mainCall = new JiraTickets
                {
                    TicketKey = Guid.NewGuid(),
                    IsActive = true,
                    ReferenceId = calldata.Key,
                    ReferenceURL = calldata.URL,
                    DateTime = DateTime.Now
                };
                dbContext.JiraTickets.Add(mainCall);

                var activeColumns = dbContext.JiraTicket_ColumnConfigurations.AsNoTracking().Where(col => col.IsActive && !col.IsExternal).ToList();
                foreach (var pro in typeof(CallModel).GetProperties())
                {
                    var matchedColumsn = activeColumns.FirstOrDefault(x => x.ColumnName.ToLower().Trim().Equals(pro.Name.ToLower().Trim()));
                    if (matchedColumsn != null)
                    {
                        var callDetails = new JiraTicketDetails
                        {
                            TicketDetailKey = Guid.NewGuid(),
                            TicketKey = mainCall.TicketKey,
                            ColumnKey = matchedColumsn.ColumnKey,
                            IsActive = true,
                            ColumnValue = matchedColumsn.IsEncrypted ? EncryptionClass.Always_Encrypt_Decrypt(pro.GetValue(calldata)?.ToString(), true, JiraEngineConfigService.Org) : pro.GetValue(calldata)?.ToString(),
                            DateTime = DateTime.Now
                        };
                        dbContext.JiraTicketDetails.Add(callDetails);
                    }
                }
                return dbContext.SaveChanges();
            }
        }

        public void UpdateTaskToDB(CallModel calldata)
        {
            Task.Factory.StartNew(() => UpdateDataInDB(calldata)).ContinueWith(m =>
            {
                //log response result
            });
        }

        public void UpdateDataInDB(CallModel calldata)
        {
            using (var dbContext = new PaycomEngineContext())
            {
                var callObj = dbContext.JiraTickets.FirstOrDefault(call => call.ReferenceId == calldata.IssueKey);
                if (callObj != null)
                {
                    var callKey = callObj.TicketKey;
                    var columns = dbContext.JiraTicketDetails.Where(col => col.TicketKey == callKey).ToList();
                    if (columns.Any())
                    {
                        dbContext.JiraTicketDetails.RemoveRange(columns);
                        dbContext.SaveChanges();
                    }
                    // 
                    var activeColumns = dbContext.JiraTicket_ColumnConfigurations.Where(col => col.IsActive && !col.IsExternal).ToList();
                    foreach (var pro in typeof(CallModel).GetProperties())
                    {
                        var matchedColumsn = activeColumns.FirstOrDefault(x => x.ColumnName.ToLower().Trim().Equals(pro.Name.ToLower().Trim()));
                        if (matchedColumsn != null)
                        {
                            var callDetails = new JiraTicketDetails
                            {
                                TicketDetailKey = Guid.NewGuid(),
                                TicketKey = callKey,
                                ColumnKey = matchedColumsn.ColumnKey,
                                IsActive = true,
                                ColumnValue = matchedColumsn.IsEncrypted ? EncryptionClass.Always_Encrypt_Decrypt(pro.GetValue(calldata)?.ToString(), true, JiraEngineConfigService.Org) : pro.GetValue(calldata)?.ToString(),
                                DateTime = DateTime.Now
                            };
                            dbContext.JiraTicketDetails.Add(callDetails);
                        }
                    }
                    dbContext.SaveChanges();
                }
            }
        }
    }
}