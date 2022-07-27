using Afiniti.PaycomEngine.Services;

namespace Afiniti.PaycomEngine.Polymorphics
{
    public class PaycomBaseDTO
    {
        public virtual dynamic ReadPaycomData()
        {
            PullEngineService PullService = new PullEngineService();
            var data = PullService.GetDownStreamData();
            return data;
        }

        public virtual bool CreateDownStreamFile( dynamic data, string FilePath)
        {
            return false;
        }
    }
}
