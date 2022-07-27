using Afiniti.PaycomEngine.Services;

namespace Afiniti.PaycomEngine.Polymorphics
{
    public class ActiveDirectoryDTO : PaycomBaseDTO
    {
        public string Employee_Code { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Department_Desc { get; set; }
        public string ClockSeq { get; set; }
        public string City { get; set; }
        public string NTLogin { get; set; }
        public string Work_Email { get; set; }
        public string Position { get; set; }
        public string ManagerEmpID { get; set; }
        public string EntityName { get; set; }
        public string LineMgrEmailID { get; set; }
        public string Manager_NT_Login { get; set; }

        public override dynamic ReadPaycomData()
        {
            PullEngineService PullService = new PullEngineService();
            var data = PullService.GetDownStreamData(Paycom.Shared.Enums.RunningPaycomActivity.AD, false);
            return data;
        }

        public override bool CreateDownStreamFile(dynamic data, string FilePath)
        {
            PushEngineService PushService = new PushEngineService();
            if (PushService.CreateAD_CSV(data, FilePath))
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
