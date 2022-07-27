using Npoi.Mapper.Attributes;
using System;

namespace Afiniti.Paycom.Shared.Models
{
    public class PaycomRequestDTO
    {
        [Ignore]
        public int PaycomDataId { get; set; }

        [Ignore]
        public System.Guid PaycomDataKey { get; set; }

        [Column("Employee_Code")]
        public string Employee_Code { get; set; }

        [Column("Firstname")]
        public string Employee_FirstName { get; set; }

        [Column("Middlename")]
        public string Employee_MiddleName { get; set; }

        [Column("Lastname")]
        public string Employee_LastName { get; set; }

        [Column("Employee_Name")]
        public string Employee_Name { get; set; }

        [Column("Department")]
        public string Department_Desc { get; set; }

        [Column("Tier_2_Desc")]
        public string Tier_2_Desc { get; set; }

        [Column("Tier_3_Desc")]
        public string Tier_3_Desc { get; set; }

        [Column("Employee_Status")]
        public string Employee_Status { get; set; }

        [Column("ClockSeq_#")]
        public string ClockSeq { get; set; }

        private string _hireDate = String.Empty;
        [Column("Hire_Date")]
        public string Hire_Date
        {
            get
            {
                return this._hireDate ?? "00/00/0000";//string.Empty;
            }
            set
            {
                this._hireDate = value;
            }
        }

        private string _terminationDate = String.Empty;
        [Column("Termination_Date")]
        public string Termination_Date
        {
            get
            {
                return this._terminationDate ?? "00/00/0000";//string.Empty;
            }
            set
            {
                this._terminationDate = value;
            }
        }

        private string _reHireDate = String.Empty;
        [Column("Rehire_Date")]
        public string Rehire_Date
        {
            get
            {
                return this._reHireDate ?? "00/00/0000";//string.Empty;
            }
            set
            {
                this._reHireDate = value;
            }
        }

        private string _fullToPartTime = String.Empty;
        [Column("Full-Time_to_Part-Time_Date")]
        public string FullTime_to_PartTime_Date
        {
            get
            {
                return this._fullToPartTime ?? "00/00/0000";//string.Empty;
            }
            set
            {
                this._fullToPartTime = value;
            }
        }

        private string _partToFullTime = String.Empty;
        [Column("Part-Time_to_Full-Time_Date")]
        public string PartTime_to_FullTime_Date
        {
            get
            {
                return this._partToFullTime ?? "00/00/0000";//string.Empty;
            }
            set
            {
                this._partToFullTime = value;
            }
        }

        [Column("Supervisor_Primary")]
        public string Supervisor_Primary { get; set; }

        private string _lastPositionChange = String.Empty;
        [Column("Last_Position_Change_Date")]
        public string Last_Position_Change_Date
        {
            get
            {
                return this._lastPositionChange ?? "00/00/0000";// string.Empty;
            }
            set
            {
                this._lastPositionChange = value;
            }
        }

        [Column("City")]
        public string City1 { get; set; }

        [Column("Country")]
        public string Country { get; set; }

        [Column("Currency")]
        public string Currency { get; set; }

        [Column("City2")]
        public string City2 { get; set; }

        [Column("NTLogin")]
        public string NTLogin { get; set; }

        [Column("Work_Email")]
        public string Work_Email { get; set; }

        [Column("Position")]
        public string Position { get; set; }

        [Column("Supervisor_Primary_Code")]
        public string ManagerEmpID { get; set; }

        [Column("EntityName")]
        // [Ignore]
        public string EntityName { get; set; }

        //[Ignore]
        public string LineMgrEmailID { get; set; }

        //[Ignore]
        public string Manager_NT_Login { get; set; }

        // [Ignore]
        public string InitiatedBy { get; set; }

        //[Ignore]
        public DateTime DumpDate { get; set; }
       
        [Column("Work_Location")]
        public string WorkLocation { get; set; }

    }
}
