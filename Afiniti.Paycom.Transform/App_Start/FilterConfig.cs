using System.Web;
using System.Web.Mvc;

namespace Afiniti.Paycom.Transform
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
