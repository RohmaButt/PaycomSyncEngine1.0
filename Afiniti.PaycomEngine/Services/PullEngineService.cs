using Afiniti.Framework.LoggingTracing;
using Afiniti.Paycom.DAL;
using Afiniti.PaycomEngine.Helpers;
using Afiniti.PaycomEngine.Polymorphics;
using System;
using System.Collections.Generic;
using System.Linq;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.PaycomEngine.Services
{
    public class PullEngineService
    {
        #region ExcelRead  
        public dynamic PullPaycomDataInDTO(string FileNameWithPath)
        {
            ApplicationTrace.Log("PullEngineService: ReadExcelDataInDTO", Status.Started);
            Data_ScruberAndValidator scrubber_ValidatorHelper = new Data_ScruberAndValidator();
            var response = scrubber_ValidatorHelper.PrepareDataAfterScrubbingValidations(FileNameWithPath);
            ApplicationTrace.Log("PullEngineService: ReadExcelDataInDTO", Status.Completed);
            return response;
        }

        public string ReadExcelDataInJson(string FileNameWithPath)//not in use for now
        {
            ApplicationTrace.Log("PullEngineService: ReadExcelDataInJson", Status.Started);
            Data_ScruberAndValidator helper = new Data_ScruberAndValidator();
            var response = helper.ReadExcelInJson(FileNameWithPath);
            ApplicationTrace.Log("PullEngineService: ReadExcelDataInJson", Status.Completed);
            return response;
        }

        #endregion ExcelRead

        #region DSs_Read

        public int ValidateMandatoryDataForDSsGeneration()//Picks configured mandatory cols from DB and check value for them in each row 
        {
            int rowCount = 0;
            List<PaycomData> validatingData = new List<PaycomData>();
            var filter = new FilterLinq<PaycomData>();
            string whereFieldColumnList = string.Empty, whereFieldValues = string.Empty, Comparison = string.Empty;
            var mandatoryDBColumnsList = new List<string>();
            using (var context = new PaycomEngineContext())
                mandatoryDBColumnsList = context.FileStream_BaseColumns.AsNoTracking().Where(x => x.IsMandatory == true).OrderBy(y => y.MandatoryCheck_Order).Select(y => y.BaseColumnName).ToList();
            bool ValidationsFlag = EngineAPIConfigService.EngineAPIConfigs.FirstOrDefault(x => x.ConfigAppEvent == "DownstreamsValidation").IsActive == true;
            if (ValidationsFlag && mandatoryDBColumnsList.Count > 0)
            {
                foreach (var mandatoryColumn in mandatoryDBColumnsList)
                {
                    using (PaycomEngineContext context = new PaycomEngineContext())
                    {
                        //If Line Manger is null in file EXCEPT "MUHAMMAD ZIAULLAH CHISHTI" then log in [PaycomData_Scrub_Logs]
                        if ((new[] { "ManagerEmpID", "Manager_NT_Login", "LineMgrEmailID" }).Contains(mandatoryColumn, StringComparer.OrdinalIgnoreCase))
                            validatingData = context.PaycomData.AsNoTracking().Where(x => x.Employee_Status == "Active" && x.ClockSeq != "24968").Where(filter.GetWherePredicate(mandatoryColumn)).ToList();
                        else
                        {
                            if (filter.GetWherePredicate(mandatoryColumn) != null)
                                validatingData = context.PaycomData.AsNoTracking().Where(x => x.Employee_Status == "Active").Where(filter.GetWherePredicate(mandatoryColumn)).ToList();
                            else
                                validatingData = context.PaycomData.AsNoTracking().Where(x => x.Employee_Status == "Active").ToList();
                        }
                        if (validatingData.Count > 0)
                            rowCount += rowCount;
                    }
                }
            }
            return rowCount;
        }
        public List<PaycomData> GetDownStreamData(RunningPaycomActivity activity, bool CertifyFullData)
        {
            ApplicationTrace.Log("PullEngineService: GetDownStreamData", activity.ToString(), Status.Started);
            List<PaycomData> baseData = new List<PaycomData>();
            var filter = new FilterLinq<PaycomData>();
            List<ConfigurationSettingDetail> predicateSettings = new List<ConfigurationSettingDetail>();
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                if (CertifyFullData == true && activity == RunningPaycomActivity.Certify)
                {
                    predicateSettings = (from det in context.ConfigurationSettingDetail
                                         join p in context.ConfigurationSetting on det.ConfigAppKey equals p.ConfigValueKey
                                         where (det.AdditionalParamName == "Include" || det.AdditionalParamName == "Exclude") && det.ParamName != "Certify_DSFlag"
                                         && p.IsActive == true && det.IsActive == true && p.ConfigAppEvent == activity.ToString()
                                         select det).ToList();
                }
                else // Exchange, Everbridge
                {
                    predicateSettings = (from det in context.ConfigurationSettingDetail
                                         join p in context.ConfigurationSetting on det.ConfigAppKey equals p.ConfigValueKey
                                         where (det.AdditionalParamName == "Include" || det.AdditionalParamName == "Exclude")
                                         && p.IsActive == true && det.IsActive == true && p.ConfigAppEvent == activity.ToString()
                                         select det).ToList();
                }
            }
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                if (filter.GetWherePredicate(predicateSettings) != null)
                    baseData = context.PaycomData.AsNoTracking().Where(filter.GetWherePredicate(predicateSettings)).ToList();
                else
                    baseData = context.PaycomData.AsNoTracking().ToList();
            }
            if (activity == RunningPaycomActivity.HeadCountRpt)
                baseData.OrderBy(x => x.Hire_Date);
            ApplicationTrace.Log("PullEngineService: GetDownStreamData", activity.ToString(), Status.Completed);
            return baseData;
        }
        public dynamic GetDownStreamData()//polymorphic base class read as its generic read for all DSs
        {
            ApplicationTrace.Log("PullEngineService: GetDownStreamData", Status.Started);
            dynamic baseData = null;
            using (var context = new PaycomEngineContext())
            {
                baseData = context.PaycomData.AsNoTracking().ToList();
            }
            ApplicationTrace.Log("PullEngineService: GetDownStreamData", Status.Completed);
            return baseData;
        }
        public dynamic GetDownStreamDataServiceDesk()
        {
            ApplicationTrace.Log("PullEngineService: GetDownStreamDataServiceDesk", Status.Started);
            using (var context = new PaycomEngineContext())
            {
                var baseData = from e in context.PaycomData_Scrubbed.AsNoTracking()
                               join scrub in context.PaycomData_Scrubbed.AsNoTracking() on e.ClockSeq equals scrub.ClockSeq
                               where scrub.NewEmployee_Flag == true && scrub.ValidationFail_Flag == false
                               select new ServiceDeskDTO
                               {
                                   Employee_Code = e.Employee_Code,
                                   FirstName = e.Employee_FirstName,
                                   MiddleName = e.Employee_MiddleName,
                                   LastName = e.Employee_LastName,
                                   Department_Desc = e.Department_Desc,
                                   ClockSeq = e.ClockSeq,
                                   City = e.City1,
                                   NTLogin = e.NTLogin,
                                   Work_Email = e.Work_Email,
                                   Position = e.Position,
                                   ManagerEmpID = e.ManagerEmpID,
                                   EntityName = e.EntityName,
                                   Manager_NT_Login = e.Manager_NT_Login,
                                   LineMgrEmailID = e.LineMgrEmailID
                               };
                ApplicationTrace.Log("PullEngineService: GetDownStreamDataServiceDesk", Status.Completed);
                return baseData.ToList();
            }
        }
        private static List<TK_DTO> FlatToHierarchyLINQ(List<TK_DTO> list, string parentId)
        {
            return (from i in list
                    where i.ManagerEmpID == parentId
                    select new TK_DTO
                    {
                        ManagerEmpID = i.ManagerEmpID,
                        EmployeeId = i.EmployeeId,
                        FirstName = i.FirstName,
                        LastName = i.LastName,
                        MiddleName = i.MiddleName,
                        HireDate = i.HireDate,
                        LastDateOfWork = i.LastDateOfWork,
                        DepartmentName = i.DepartmentName,
                        RoleName = i.RoleName,
                        LegalEntityName = i.LegalEntityName,
                        Status = i.Status,
                        Subordinates = FlatToHierarchyLINQ(list, i.EmployeeId)
                    }).ToList();
        }
        public List<TK_DTO> PullDownStream_TK()//All data is pulled in customized DTO
        {
            ApplicationTrace.Log("PullEngineService: PullDownStream_TK", Status.Started);
            List<PaycomData> baseData = null;
            using (var context = new PaycomEngineContext())
            {
                baseData = context.PaycomData.AsNoTracking().ToList();
            }
            List<TK_DTO> items = new List<TK_DTO>();
            List<TK_DTO> Subs = new List<TK_DTO>();
            foreach (var item in baseData)
            {
                items.Add(new TK_DTO()
                {
                    FirstName = item?.Employee_FirstName,
                    MiddleName = item?.Employee_MiddleName,
                    LastName = item?.Employee_LastName,
                    DepartmentName = item?.Tier_3_Desc,
                    Status = item?.Employee_Status,
                    EmployeeId = item?.ClockSeq == null || string.IsNullOrEmpty(item?.ClockSeq) ? item.Employee_Code : item?.ClockSeq,
                    Employee_Code = item?.Employee_Code,
                    HireDate = item.Hire_Date != "00/00/0000" ? Convert.ToDateTime(item.Hire_Date) : (DateTime?)null,
                    LastDateOfWork = item?.Termination_Date != "00/00/0000" ? Convert.ToDateTime(item.Termination_Date) : (DateTime?)null,
                    UserName = item?.NTLogin,
                    UserEmailID = item?.Work_Email,
                    LegalEntityName = item?.EntityName == "" ? "Afiniti US" : item?.EntityName == "Afiniti PK" ? "Afiniti Software Solutions (Private) Limited" : item?.EntityName,
                    RoleName = item?.Position,
                    Location = item?.Country,
                    Supervisor_Primary = item?.Supervisor_Primary,
                    ManagerEmpID = item?.ManagerEmpID,
                    LineMgrEmailID = item?.LineMgrEmailID,
                    Manager_NT_Login = item?.Manager_NT_Login,
                    FirstValidityDate = item?.InsertionDate,
                    LastValidityDate = item?.ValidityDate,
                    Subordinates = Subs
                });
            }
            //Replace EntityName:       'Afiniti PK'    ==>  'Afiniti Software Solutions(Private) Limited' ==> [DONE]
            //Replace EntityName:       ''              ==>  'Afiniti US'                                  ==> [DONE]     
            //Replace DepartmentName:   'GSD'           ==>  'Global Data' while Tier_2_Desc = 'Global Data', Tier_3_Desc = 'Global Data' remains same  ==> [TODO]

            ApplicationTrace.Log("PullEngineService: FlatToHierarchyLINQ", Status.Started);
            var data = FlatToHierarchyLINQ(items, "");
            ApplicationTrace.Log("PullEngineService: FlatToHierarchyLINQ", Status.Completed);
            ApplicationTrace.Log("PullEngineService: PullDownStream_TK", Status.Completed);
            return data;
        }
        public dynamic PullDownStreamData(string paycomActivity)//All data is pulled. it will call BaseClass DS DTO to read data
        {
            ApplicationTrace.Log("PullEngineService: PullDownStreamData", Status.Started);
            dynamic response = null;
            Enum.TryParse(paycomActivity, out RunningPaycomActivity CurrentpaycomActivity);
            switch (CurrentpaycomActivity)
            {
                case RunningPaycomActivity.TK_Pull:
                    TK_DTO tK_DTO = new TK_DTO();
                    response = CallPaycom_PolymorphicForm(tK_DTO);
                    break;
                case RunningPaycomActivity.AD:
                    ActiveDirectoryDTO adminDTO = new ActiveDirectoryDTO();
                    response = CallPaycom_PolymorphicForm(adminDTO);
                    break;
                case RunningPaycomActivity.SD:
                    ServiceDeskDTO serviceDeskDTO = new ServiceDeskDTO();
                    response = CallPaycom_PolymorphicForm(serviceDeskDTO);
                    break;
                case RunningPaycomActivity.Exchange:
                    ExchangeDTO exchangeDTO = new ExchangeDTO();
                    response = CallPaycom_PolymorphicForm(exchangeDTO);
                    break;
                case RunningPaycomActivity.HeadCountRpt:
                    HeadCountRptDTO headCountRptDTO = new HeadCountRptDTO();
                    response = CallPaycom_PolymorphicForm(headCountRptDTO);
                    break;
                case RunningPaycomActivity.Certify:
                    CertifyDTO certifyDTO = new CertifyDTO();
                    response = CallPaycom_PolymorphicForm(certifyDTO);
                    break;
                case RunningPaycomActivity.EverBridge:
                    EverBridgeDTO everBridgeDTO = new EverBridgeDTO();
                    response = CallPaycom_PolymorphicForm(everBridgeDTO);
                    break;
                default:
                    break;
            }
            if (response != null)
            {
                ApplicationTrace.Log("PullEngineService: PullDownStreamData", Status.Completed);
                return response;
            }
            else
            {
                ApplicationTrace.Log("PullEngineService: PullDownStream", Status.Failed);
                return false;
            }
        }
        private dynamic CallPaycom_PolymorphicForm(PaycomBaseDTO paycomBase)
        {
            return paycomBase.ReadPaycomData();
        }

        #endregion DSs_Read

        public List<ConfigurationSettingDTO> GetEngineConfigurations()
        {
            using (PaycomEngineContext context = new PaycomEngineContext())
            {
                var configData = context.ConfigurationSetting
                                   .Select(parent => new ConfigurationSettingDTO
                                   {
                                       ConfigAppEvent = parent.ConfigAppEvent,
                                       ConfigValue = parent.ConfigValue,
                                       ConfigValueKey = parent.ConfigValueKey,
                                       Email = parent.Email,
                                       CcEmail = parent.CcEmail,
                                       IsActive = parent.IsActive == true,
                                       Children = context.ConfigurationSettingDetail
                                       .Where(child => child.ConfigAppKey == parent.ConfigValueKey)
                                       .Select(child => new ConfigurationSettingDetailDTO
                                       {
                                           ConfigAppKey = child.ConfigAppKey,
                                           ParamName = child.ParamName,
                                           ParamValue = child.ParamValue,
                                           AdditionalParamName = child.AdditionalParamName,
                                           AdditionalParamValue = child.AdditionalParamValue,
                                           IsActive = child.IsActive == true,
                                           Summary = child.Summary
                                       }).ToList(),
                                   }).ToList();
                return configData;
            }
        }
    }
}