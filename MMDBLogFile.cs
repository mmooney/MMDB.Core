using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Filters;
using Microsoft.Practices.EnterpriseLibrary.Logging.ExtraInformation;

namespace MMDB.Core
{
    public static class MMDBLogFile
    {
        public static void Log(Exception ex)
        {
            Log(ex, null, null);
        }
        public static void Log(Exception ex, string ProcessName)
        {
            Log(ex, ProcessName, ProcessName);
        }
        public static void Log(string Message)
        {
            Log(Message, null, null);
        }
        public static void Log(string Message, string ProcessName, string Location)
        {
            LogEntry le = new LogEntry();
            if (ProcessName != null)
            {
                le.Message += ProcessName + "-" + Location + "\r\n\r\n";
            }
            le.Message += Message;
            le.Severity = System.Diagnostics.TraceEventType.Information;
            le.ProcessName = ProcessName;

            Logger.Write(le);
        }
        public static void Log(Exception ex, string ProcessName, string Location)
        {
            if (ex.InnerException != null)
            {
                Log(ex.InnerException, ProcessName, Location);
            }
            LogEntry le = new LogEntry();
            if (ProcessName != null)
            {
                le.Message += ProcessName + "-" + Location + "\r\n\r\n";
            }
            le.Message += ex.Message;
            le.Message += "\r\nStack Trace:\r\n";
            le.Message += ex.StackTrace;
            le.Severity = System.Diagnostics.TraceEventType.Information;
            le.ProcessName = ProcessName;
            Logger.Write(le);
        }

        public static void View(string logfile)
        {
                        System.Diagnostics.Process.Start(logfile);
        }
    }
}
