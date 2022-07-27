namespace Afiniti.Paycom.Shared
{
    public class ReadFileRequestModel
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
    public class CrowdUserObj
    {
        public string CrowdSSOToken { get; set; }
        public string Email { get; set; }
        public int AuthenticationCode { get; set; }
        public int ApprovalStatus { get; set; }
        public string UserName { get; set; }
        public object UserKey { get; set; }
        public object RemoteKey { get; set; }
        public string JSessionID { get; set; }
        public string AuthenticationMetaData { get; set; }
    }
}
