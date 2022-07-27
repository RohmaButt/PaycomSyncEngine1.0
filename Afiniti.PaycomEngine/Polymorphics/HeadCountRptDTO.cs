using Afiniti.PaycomEngine.Services;
using System;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.PaycomEngine.Polymorphics
{
    public class HeadCountRptDTO : PaycomBaseDTO
    {
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string EmailID { get; set; }
        public string Designation { get; set; }
        public string Team { get; set; }
        public string LineManager { get; set; }
        public DateTime DateOfJoining { get; set; }
        public string Country { get; set; }
        public string Location { get; set; }
        public DateTime LastWorkingDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public int EmployeeCount { get; set; }
        public override dynamic ReadPaycomData()
        {
            PullEngineService PullService = new PullEngineService();
            var data = PullService.GetDownStreamData(RunningPaycomActivity.HeadCountRpt, false);
            return data;
        }

        public override bool CreateDownStreamFile(dynamic data, string FilePath)
        {
            PushEngineService PushService = new PushEngineService();
            if (PushService.CreateHeadCountRpt(data, FilePath))
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
