using Afiniti.PaycomEngine.Services;
using System;
using System.Collections.Generic;

namespace Afiniti.PaycomEngine.Polymorphics
{
    public class TK_DTO : PaycomBaseDTO
    {
        public string EmployeeId { get; set; }//ClockSeq
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string DepartmentName { get; set; }//Department_Desc
        public string RoleName { get; set; }//Position
        public string LegalEntityName { get; set; }//EntityName                                                
        public DateTime? HireDate { get; set; }//Hire_Date
        public DateTime? LastDateOfWork { get; set; }//Termination_Date
        public string Status { get; set; }//Employee_Status
        public List<TK_DTO> Subordinates { get; set; }
        public string Employee_Code { get; set; }//paycom Employee_Code
        public string UserName { get; set; }//NTLogin
        public string UserEmailID { get; set; }//Work_Email
        public string Location { get; set; }
        public string Supervisor_Primary { get; set; }
        public string ManagerEmpID { get; set; }
        public string LineMgrEmailID { get; set; }
        public string Manager_NT_Login { get; set; }

        public DateTime? FirstValidityDate { get; set; }
        public DateTime? LastValidityDate { get; set; }

        public override dynamic ReadPaycomData()
        {
            PullEngineService PullService = new PullEngineService();
            var data = PullService.PullDownStream_TK();
            return data;
        }

    }
}
