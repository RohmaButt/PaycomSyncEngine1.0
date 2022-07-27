using Afiniti.PaycomEngine.Services;
using CsvHelper.Configuration;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.PaycomEngine.Polymorphics
{
    public sealed class EverBridgeDTOMap : ClassMap<EverBridgeDTO>
    {
        public EverBridgeDTOMap()
        {
            Map(m => m.FirstName).Name("First Name", "FirstName");
            Map(m => m.MiddleInitial).Name("Middle Initial", "MiddleInitial");
            Map(m => m.LastName).Name("Last Name", "LastName");
            Map(m => m.ExternalID).Name("External ID", "ExternalID");
            Map(m => m.Country).Name("Country", "Country");
            Map(m => m.RecordType).Name("Record Type", "RecordType");
            Map(m => m.SSOUserID).Name("SSO User ID", "SSOUserID");
            Map(m => m.Location1).Name("Location 1", "Location1");
            Map(m => m.LocationId1).Name("Location Id 1", "LocationId1");
            Map(m => m.EmailAddress1).Name("Email Address 1", "EmailAddress1");
            Map(m => m.End).Name("END", "End");
        }
    }
    public class EverBridgeDTO : PaycomBaseDTO
    {
        public string FirstName { get; set; }
        public string MiddleInitial { get; set; }
        public string LastName { get; set; }
        public string ExternalID { get; set; }
        public string Country { get; set; }
        public string RecordType { get; set; }
        public string SSOUserID { get; set; }
        public string Location1 { get; set; }
        public string LocationId1 { get; set; }
        public string EmailAddress1 { get; set; }
        public string End { get; set; }
        public override dynamic ReadPaycomData()
        {
            PullEngineService PullService = new PullEngineService();
            var data = PullService.GetDownStreamData(RunningPaycomActivity.EverBridge, false);
            return data;
        }
        public override bool CreateDownStreamFile(dynamic data, string FilePath)
        {
            PushEngineService PushService = new PushEngineService();
            if (PushService.CreateEverbridgeCSV(data, FilePath))
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
