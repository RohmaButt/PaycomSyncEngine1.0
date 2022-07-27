using System;

namespace Afiniti.Paycom.DAL
{
    public partial class PaycomData
    {
        //https://docs.microsoft.com/en-us/dotnet/api/system.object.equals?redirectedfrom=MSDN&view=netframework-4.8#overloads
        //To compare objects for their values we can override the Equals() and GetHashcode() methods:
        //we dont need to check reference and only values need to check so we can skip GetHashCode() method and can override only Equals method to compare values of both Objs
        public override bool Equals(System.Object obj)
        {
            if (obj == null)
                return false;
            PaycomData p = obj as PaycomData;
            if ((object)p == null)
                return false;
            return (WorkLocation == p.WorkLocation) && (Employee_Code == p.Employee_Code) && (Employee_FirstName == p.Employee_FirstName) && (Employee_LastName == p.Employee_LastName) && (Employee_Name == p.Employee_Name)
                        && (Department_Desc == p.Department_Desc) && (Tier_2_Desc == p.Tier_2_Desc) && (Tier_3_Desc == p.Tier_3_Desc) && (Employee_Status == p.Employee_Status) &&
                      (Employee_MiddleName == p.Employee_MiddleName) && (ClockSeq == p.ClockSeq) && (Supervisor_Primary == p.Supervisor_Primary) && (City1 == p.City1) &&
                      (City2 == p.City2) && (NTLogin == p.NTLogin) && (Work_Email == p.Work_Email) && (Position == p.Position) && (ManagerEmpID == p.ManagerEmpID) && (EntityName == p.EntityName)
                      && (LineMgrEmailID == p.LineMgrEmailID) && (Manager_NT_Login == p.Manager_NT_Login) && (Country == p.Country) && (Currency == p.Currency)
                  && (Hire_Date == p.Hire_Date) && (Termination_Date == p.Termination_Date) && (Rehire_Date == p.Rehire_Date) && (FullTime_to_PartTime_Date == p.FullTime_to_PartTime_Date)
                   && (PartTime_to_FullTime_Date == p.PartTime_to_FullTime_Date) && (Last_Position_Change_Date == p.Last_Position_Change_Date);
        }
        public override int GetHashCode()
        {
            return
                (WorkLocation ?? String.Empty).GetHashCode() ^ (Employee_Code ?? String.Empty).GetHashCode() ^ (Employee_FirstName ?? String.Empty).GetHashCode() ^ (Employee_LastName ?? String.Empty).GetHashCode() ^
                (Employee_Name ?? String.Empty).GetHashCode() ^ (Department_Desc ?? String.Empty).GetHashCode() ^ (Employee_MiddleName ?? String.Empty).GetHashCode() ^
                (Tier_2_Desc ?? String.Empty).GetHashCode() ^ (Tier_3_Desc ?? String.Empty).GetHashCode() ^ (Employee_Status ?? String.Empty).GetHashCode() ^
                (ClockSeq ?? String.Empty).GetHashCode() ^ (Supervisor_Primary ?? String.Empty).GetHashCode() ^ (City1 ?? String.Empty).GetHashCode() ^
                (City2 ?? String.Empty).GetHashCode() ^ (NTLogin ?? String.Empty).GetHashCode() ^ (Work_Email ?? String.Empty).GetHashCode() ^
                (Position ?? String.Empty).GetHashCode() ^ (ManagerEmpID ?? String.Empty).GetHashCode() ^ (EntityName ?? String.Empty).GetHashCode() ^
                (LineMgrEmailID ?? String.Empty).GetHashCode() ^ (Manager_NT_Login ?? String.Empty).GetHashCode() ^ (Country ?? String.Empty).GetHashCode() ^
                (Currency ?? String.Empty).GetHashCode() ^ (Hire_Date ?? String.Empty).GetHashCode() ^ (Termination_Date ?? String.Empty).GetHashCode() ^
                (Rehire_Date ?? String.Empty).GetHashCode() ^ (FullTime_to_PartTime_Date ?? String.Empty).GetHashCode() ^ (PartTime_to_FullTime_Date ?? String.Empty).GetHashCode() ^
                (Last_Position_Change_Date ?? String.Empty).GetHashCode();
        }
    }
}
