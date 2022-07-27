namespace Afiniti.PaycomEngine.Polymorphics
{
    public class CornerStoneDTO : PaycomBaseDTO
    {
        public string Employee_Code { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Department_Desc { get; set; }
        public string Tier_3_Desc { get; set; }
        public string Employee_Status { get; set; }
        public string ClockSeq { get; set; }
        public string Hire_Date { get; set; }
        public string Termination_Date { get; set; }
        public string Country { get; set; }
        public string City2 { get; set; }
        public string Work_Email { get; set; }
        public string Position { get; set; }
        public string ManagerEmpID { get; set; }

        public override bool CreateDownStreamFile(dynamic data, string FilePath)
        {
            return false;
        }
    }
}
