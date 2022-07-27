using Afiniti.Framework.LoggingTracing;
using System;
using System.Collections.Specialized;
using System.Text;

namespace Afiniti.Paycom.Shared.Models
{

    public static class ExceptionHandling
    {
        private static StringCollection eMessages = new StringCollection();

        public static void LogException(Exception pEx, string pString, string pAdditional = "")//Afiniti logging/tracing assembly 
        {
            pString = String.IsNullOrEmpty(pString) ? "PaycomEngine" : pString;
            WriteException(pEx, pString);
            Log.WriteLog(eMessages, pString);
        }

        private static void WriteException(Exception ex, String CallerName)
        {
            StringBuilder sbTrace = new StringBuilder();
            sbTrace.AppendLine("-----------------------------------------------------------------------------");
            sbTrace.AppendLine("Caller -- " + CallerName);

            sbTrace.AppendLine(DateTime.UtcNow.ToString()).AppendLine("");

            if (ex.GetType() != null)
            {
                sbTrace.AppendLine(ex.GetType().ToString());
            }

            sbTrace.AppendLine(ex.Message);
            sbTrace.AppendLine(ex.StackTrace);


            eMessages.Add(sbTrace.ToString());
            Exception inner = ex.InnerException;

            if (inner != null)
            {
                WriteException(inner, CallerName);
            }
        }
    }

}
