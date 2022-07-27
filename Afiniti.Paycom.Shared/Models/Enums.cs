using System.ComponentModel;

namespace Afiniti.Paycom.Shared
{
    public class Enums
    {
        public enum PaycomData_MissingInfoStatus
        {
            NotProcessed,
            InProgress,
            Processed,
            Failed,
            Error            
        }
        public enum FileType//can be uses to identify downstream file type and Paycom file
        {
            xls,
            xlsx,
            csv
        }
        public enum RunningPaycomActivity
        {
            [Description("Active Directory downstream")]
            AD = 1,

            [Description("Exchange downstream")]
            Exchange = 2,

            [Description("Decibel downstream")]
            Decibel = 3,

            [Description("Certify downstream")]
            Certify = 4,

            [Description("Cornerstone downstream")]
            Cornerstone = 5,

            [Description("Timekeeping pull")]
            TK_Pull = 6,

            [Description("Paycom upload")]
            PushInDB = 7,

            [Description("Paycom Processing")]
            Processing = 8,

            [Description("JIRA ticket creation")]
            JIRA = 9,

            [Description("New Employee downstream")]
            SD = 10,// Service Desk,

            [Description("Validation for downstreams")]
            DownstreamsValidation = 11,

            [Description("Timekeeping push")]
            TK_Push = 12,

            [Description("GSD Headcount downstream Report")]
            HeadCountRpt = 13,

            [Description("Jira Sync Engine")]
            JiraSyncEngine = 14,

            [Description("EverBridge downstream")]
            EverBridge = 15,
        }

        public enum CriteriaExpression
        {
            And = 0,
            Or = 1
        }
        public enum CriteriaComparison
        {
            Include = 0,//Equals = 0,
            Exclude = 1//NotEquals = 1
        }

        public enum LogType
        {
            Error,
            Warning,
            Information
        }

        public enum LogStage
        {
            Scrubbing,
            Validation,
            Processing
        }
        public enum Column_Type
        {
            Cascading = 1,
            DatePicker = 2,
            DateAndTime = 3,
            FreeText = 4,
            GroupPicker = 5,
            MultiGroupPicker = 6,
            Labels = 7,
            MultiSelect = 8,
            MultiUserPicker = 9,
            NumberField = 10,
            ProjectPicker = 11,
            Priority = 12,
            RadioButton = 13,
            SelectList = 14,
            SingleVersionPicker = 15,
            TextField = 16,
            URLField = 17,
            UserPicker = 18,
            VersionPicker = 19,
            Link = 20,
            Components = 21
        }
    }
}
