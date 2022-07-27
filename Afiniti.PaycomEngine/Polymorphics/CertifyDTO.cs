using Afiniti.PaycomEngine.Services;

namespace Afiniti.PaycomEngine.Polymorphics
{
    public class CertifyDTO : PaycomBaseDTO
    {
        public string Employee_Code { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Department_Desc { get; set; }
        public string Tier_2_Desc { get; set; }
        public string Tier_3_Desc { get; set; }
        public string ClockSeq { get; set; }
        public string Hire_Date { get; set; }
        public string Termination_Date { get; set; }
        public string Supervisor_Primary { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Currency { get; set; }
        public string City2 { get; set; }
        public string Work_Email { get; set; }
        public string Position { get; set; }
        public string ManagerEmpID { get; set; }
        public string EntityName { get; set; }
        public string LineMgrEmailID { get; set; }
        public override dynamic ReadPaycomData()
        {
            PullEngineService PullService = new PullEngineService();
            var data = PullService.GetDownStreamData(Paycom.Shared.Enums.RunningPaycomActivity.Certify, false);
            return data;
        }

        public override bool CreateDownStreamFile(dynamic data, string FilePath)
        {
            PushEngineService PushService = new PushEngineService();
            if (PushService.CreateCertifyExcelFile(data, FilePath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
