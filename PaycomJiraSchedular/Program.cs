using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaycomJiraSchedular
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (System.Diagnostics.Process myProc in System.Diagnostics.Process.GetProcesses())
            {
                if (myProc.ProcessName == "EXCEL")
                {
                    myProc.Kill();
                    break;
                }
            }
        }
    }
}
