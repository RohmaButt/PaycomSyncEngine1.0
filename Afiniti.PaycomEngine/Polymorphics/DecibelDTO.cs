namespace Afiniti.PaycomEngine.Polymorphics
{
    public class DecibelDTO : PaycomBaseDTO
    {
        public string Employee_Code { get; set; }

        public override bool CreateDownStreamFile(dynamic data, string FilePath)
        {
            return false;
        }
    }
}
