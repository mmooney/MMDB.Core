using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Filters;
using Microsoft.Practices.EnterpriseLibrary.Logging.ExtraInformation;
using System.Data;
using System.Data.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer;
using System.IO;
using System.Text.RegularExpressions;
using System.Transactions;

namespace MMDB.Core.DBBuild
{
    public class DBHelper
    {
        //Directory constants
        protected const string StoredProcedureDirectory = "StoredProcedures";
        protected const string StoredProcedureFile = "StoredProcedures.lst";
        protected const string ViewDirectory = "Views";
        protected const string ViewsFile = "Views.lst";
        protected const string FunctionDirectory = "UserFunctions";
        protected const string FunctionsFile = "Functions.lst";
        protected const string InstallDirectory = "";
        protected const string UpdateDirectory = "ChangeScripts";
        protected const string TriggerDirectory = "Triggers";
        protected const string TriggersFile = "Triggers.lst";
        protected const string InstallFile = "Install.lst";
        protected const string DatabaseDirectory = "Database";
        protected const string CreateFile = "Database.sql";
        protected const string TableFile = "Tables.lst";
        protected const string TableDirectory = "Tables";
        protected const string IndexFile = "Indexes.lst";
        protected const string ForeignKeyFile = "ForeignKeys.lst";
        protected const string IndexDirectory = "Indexes";
        protected const string ForeignKeyDirectory = "ForeignKeys";
        protected const string ChangeScriptDirectory = "ChangeScripts";
        protected const string SeedDataDirectory = "SeedData";
        protected const string SeedDataFile = "SeedData.lst";
        protected const string SeedDataFileExtension = ".SeedData.sql";
        protected const string PartitionSchemeFile = "PartitionScheme.lst";
        protected const string PartitionFunctionFile = "PartitionFunction.lst";

        public string ScriptPath { get; set; }
        public bool CommandLine { get; set; }

        public string DatabasePath { get; set; }
        protected string _Status = "";
        public string Status
        {
            get { return _Status; }
            set { _Status = value; }
        }
        private string _Server = "";
        private string _Username = "";
        private string _Password = "";
        private string _Database = "";
        private bool _IntegratedSecurity = false;
        private string _BackupPath = "";
        private bool _Encrypt = false;

        public string Server
        {
            get
            {
                return _Server;
            }
            set
            {
                _Server = value;
            }
        }
        public List<KeyValueItem> KeyValues 
        { 
            get; 
            set;
        }

        public string Username
        {
            get
            {
                return _Username;
            }
            set
            {
                _Username = value;
            }
        }
        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                _Password = value;
            }
        }
        private int _Timeout = 0;
        public int Timeout { get { return _Timeout; } set { _Timeout = value; } }
        public string Database
        {
            get
            {
                return _Database;
            }
            set
            {
                _Database = value;
            }
        }
        public bool IntegratedSecurity
        {
            get { return _IntegratedSecurity; }
            set { _IntegratedSecurity = value; }
        }
        public string BackupPath
        {
            get
            {
                return _BackupPath;
            }
            set
            {
                _BackupPath = value;
            }
        }
        public bool Encrypt
        {
            get
            {
                return _Encrypt;
            }
            set
            {
                _Encrypt = value;
            }
        }

        public string GetConnectionString()
        {
            System.Data.SqlClient.SqlConnectionStringBuilder builder =
              new System.Data.SqlClient.SqlConnectionStringBuilder();
            builder["Data Source"] = Server;
            if (IntegratedSecurity)
            {
                builder["integrated Security"] = true;
            }
            else
            {
                builder["User id"] = Username;
                builder["Password"] = Password;
            }
            builder["Initial Catalog"] = Database;
            return builder.ConnectionString;
        }
        protected void Log(Exception ex)
        {
            Log(ex, null, null);
        }
        protected void Log(Exception ex, string ProcessName)
        {
            Log(ex, ProcessName, ProcessName);
        }
        protected void Log(string Message)
        {
            Log(Message, null, null);
        }
        protected void Log(string Message, string ProcessName, string Location)
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
        protected void Log(Exception ex, string ProcessName, string Location)
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
        protected void WriteStatus(string StatusText)
        {
            _Status += StatusText;
            _Status += "\r\n";
            Log(StatusText);
        }
        Database db;
        Microsoft.SqlServer.Management.Smo.Server SMOServer;
        protected void ProcessScript(string Script)
        {
            try
            {
                if (SMOServer == null)
                {
                    SMOServer = new Microsoft.SqlServer.Management.Smo.Server();
                    SMOServer.ConnectionContext.NonPooledConnection = true;
                    SMOServer.ConnectionContext.AutoDisconnectMode = AutoDisconnectMode.DisconnectIfPooled;
                }
                if (Script.Contains("CREATE DATABASE"))
                {
                    //Replace the dbname here becuase it is a parm in the script.
                    Script = Script.Replace("[[DB_NAME]]", Database);
                    Script = Script.Replace("[[DB_PATH]]", DatabasePath);
                    //In this case, we need to build a new connection string, pointing to master.
                    string temp = this.Database;
                    this.Database = "master";
                    ServerConnection conn = new ServerConnection(new System.Data.SqlClient.SqlConnection(GetConnectionString()));
                    SMOServer.ConnectionContext.ConnectionString = conn.ConnectionString;
                    db = SMOServer.Databases["master"];
                    this.Database = temp;
                    SMOServer = new Microsoft.SqlServer.Management.Smo.Server();
                    SMOServer.ConnectionContext.NonPooledConnection = true;
                    SMOServer.ConnectionContext.AutoDisconnectMode = AutoDisconnectMode.DisconnectIfPooled;
                    SMOServer.ConnectionContext.ConnectionString = conn.ConnectionString;

                    if (Script.Contains("DROP DATABASE"))
                    {
                        if (SMOServer.Databases.Contains(Database) && Script.Contains("--##KILL ALL PROCESSES##--"))
                        {
                            SMOServer.KillAllProcesses(Database);
                        }

                    }
                    //Database is not yet created.  We will NOT delete the database in this step.  That can be added to the script.  
                    //We don't want to cause undo stress if someone clicks install.
                    db.ExecuteNonQuery(Script);
                }
                else
                {
                    ServerConnection conn = new ServerConnection(new System.Data.SqlClient.SqlConnection(GetConnectionString()));
                    SMOServer.ConnectionContext.ConnectionString = conn.ConnectionString;
//                    SMOServer.ConnectionContext.ConnectTimeout = _Timeout;
                    SMOServer.ConnectionContext.StatementTimeout = _Timeout;
                    //Connect to the DB Requested

                    SMOServer.ConnectionContext.BeginTransaction();
                    try
                    {
                        db = SMOServer.Databases[Database];
                        db.ExecuteNonQuery(Script);
                        SMOServer.ConnectionContext.CommitTransaction();
                    }
                    catch (Exception err)
                    {
                        WriteStatus("Error:  " + err.Message);
                        try
                        {
                            SMOServer.ConnectionContext.RollBackTransaction();
                        }
                        catch (Exception err1)
                        {
                            WriteStatus("Error:  " + err1.Message);
                        }
                        throw err;
                    }
                }
            }//try
            catch (Exception err)
            {
                MMDBLogFile.Log(err);
                WriteStatus("Error:  " + err.Message);
                throw (err);
            }
            finally
            {
                SMOServer.ConnectionContext.Disconnect();
                db = null;
                SMOServer = null;
            }
        }

        protected void RunScript(string Filename)
        {
            string Script = "";
            if (Filename.Substring(1, 1) == ":")
            {
                try
                {
	                using(StreamReader sr = new StreamReader(Filename))
	                {
						Script = sr.ReadToEnd();
                        //Handle Replacements
                        if (KeyValues != null)
                        {
                            foreach (KeyValueItem k in KeyValues)
                            {
                                Script = Script.Replace(k.Key, k.Value);
                            }
                        }
					}
                }
                catch (FileNotFoundException ex)
                {
                    string Message = "ERROR:  File Not Found: " + Filename.Replace(ScriptPath, "") + ")  ";
                    WriteStatus(Message);
                    WriteStatus("************PROCESS ENDING WITH ERROR*************");
                    throw (new DBBuildException(Message, ex));
                }
            }
            try 
            {
	            ProcessScript(Script);
			}
			catch(Exception err)
			{
                string message = "ERROR: " + err.Message;
                WriteStatus(message);
                Exception innerException = err.InnerException;
                while(innerException != null)
                {
					message = "\t - " + innerException.Message;
					WriteStatus(message);
					innerException = innerException.InnerException;
                }
                WriteStatus("************PROCESS ENDING WITH ERROR*************");
                throw (new DBBuildException(message, err));
			}
        }
        private string digits(string input, int mindigits)
        {
            string tmpstring = input;

            while (tmpstring.Length < mindigits)
            {
                tmpstring = "0" + tmpstring;
            }

            return tmpstring;
        }
        protected void EncryptObjects()
        {
            Microsoft.SqlServer.Management.Smo.Server SMOServer = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(new System.Data.SqlClient.SqlConnection(GetConnectionString())));

            Database db;
            db = SMOServer.Databases[Database];

            foreach (StoredProcedure s in db.StoredProcedures)
            {
                if (!s.IsSystemObject)
                {
                    s.TextMode = false;
                    s.IsEncrypted = true;
                    s.Alter();
                }
            }
            foreach (View v in db.Views)
            {
                if (!v.IsSystemObject)
                {
                    v.TextMode = false;
                    v.IsEncrypted = true;
                    v.Alter();
                }
            }
            foreach (UserDefinedFunction u in db.UserDefinedFunctions)
            {
                if (!u.IsSystemObject)
                {
                    u.TextMode = false;
                    u.IsEncrypted = true;
                    u.Alter();
                }
            }
            foreach (Trigger t in db.Triggers)
            {
                if (!t.IsSystemObject)
                {
                    t.TextMode = false;
                    t.IsEncrypted = true;
                    t.Alter();
                }
            }
        }
        public void BackupDatabase()
        {
            try
            {
                Microsoft.SqlServer.Management.Smo.Server SMOServer = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(new System.Data.SqlClient.SqlConnection(GetConnectionString())));

                if (!Directory.Exists(_BackupPath))
                {
                    Directory.CreateDirectory(BackupPath);
                }
                string filename = _BackupPath;
                filename += @"\" + _Database + @"_" + digits(DateTime.Now.Year.ToString(), 4) +
                    digits(DateTime.Now.Month.ToString(), 2) +
                    digits(DateTime.Now.Day.ToString(), 2) +
                    digits(DateTime.Now.Hour.ToString(), 2) +
                    digits(DateTime.Now.Minute.ToString(), 2) +
                    digits(DateTime.Now.Second.ToString(), 2) +
                    @".bak";

                Backup backup = new Backup();
                backup.Action = BackupActionType.Database;
                backup.Database = _Database;
                backup.Devices.Add(new BackupDeviceItem(filename, DeviceType.File));
                backup.Initialize = true;
                backup.Checksum = true;
                backup.ContinueAfterError = true;
                backup.Incremental = false;
                backup.LogTruncation = BackupTruncateLogType.Truncate;
                // Perform backup
                backup.SqlBackup(SMOServer);
            }
            catch (Exception err)
            {
                MMDB.Core.MMDBLogFile.Log(err);
                throw (err);
            }
        }

		protected string BuildScriptFilePath(string relativeDirectory, string fileName)
		{
			string fullDirectory = Path.Combine(ScriptPath,relativeDirectory);
			if(string.IsNullOrEmpty(fileName))
			{
				return fullDirectory;
			}
			else 
			{
				return Path.Combine(fullDirectory,fileName);
			}
		}

		protected string BuildRelativeFilePath(string fullFilePath)
		{
			string returnValue = fullFilePath.Replace(this.ScriptPath,"");
			if(!string.IsNullOrEmpty(returnValue))
			{
				if(returnValue[0] == '\\')
				{
					returnValue = returnValue.Substring(1);
				}
			}
			return returnValue;
		}
		
    }

}

