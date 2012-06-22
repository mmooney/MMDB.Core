using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using EntLib = Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using Microsoft.Practices.EnterpriseLibrary.Common;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.ExtraInformation;
using System.Data.Common;
using System.Data;

namespace MMDB.Core.DBBuild
{
    public class DBBuildHelper : DBHelper
    {
        public DBBuildHelper(string ServerText, string DatabaseText, string UsernameText, string PasswordText, string PathText, int TimeOutInt)
        {
            Server = ServerText;
            Database = DatabaseText;
            Username = UsernameText;
            Password = PasswordText;
            ScriptPath = PathText;
            Timeout = TimeOutInt;
            _Status = "";

        }        

        public DBBuildHelper(string ServerText, string DatabaseText, string UsernameText, string PasswordText, string PathText)
        {
            Server = ServerText;
            Database = DatabaseText;
            Username = UsernameText;
            Password = PasswordText;
            ScriptPath = PathText;
            _Status = "";
        }        
        public string Version { get; set; }
        public bool SkipMissingChangeScripts { get; set; }
        public DBBuildHelper()
        {
            ScriptPath = "";
            _Status = "";
            Server = "";
            Username = "";
            Password = "";
            Database = "";
            SkipMissingChangeScripts = false;
            Timeout = 30; //Default to 30 second timeout
            //            IntegratedSecurity = false;
            //            Log("Called DBBuildHelper constructor");
        }
        public void BuildInstall()
        {
            _Status = "";
            WriteStatus("-----Build Install STARTING.");
            string lstFilePath = Path.Combine(ScriptPath, InstallFile);
            ProcessLSTFile(lstFilePath);
            WriteStatus("-----Build Install COMPLETE.");
//            BuildUpdate(false);
        }
        private void ProcessLSTFile(string path)
        {
            List<string> filelist = new List<string>();
            string Filename = "";
            using (StreamReader sr = new StreamReader(path))
            {
                while (sr.Peek() >= 0)
                {
                    Filename = sr.ReadLine();
                    if (Filename.Contains(".lst"))
                    {
                        //First we need to process everything above.
                        ProcessFileList(filelist);
                        filelist.Clear(); //Clear it down, so we don't double run stuff.
                        string filePath = Path.Combine(ScriptPath, Filename);
                        ProcessLSTFile(filePath);
                    }
                    else if (Filename.Contains(".sql"))
                    {
                        string filePath = Path.Combine(ScriptPath, Filename);
                        filelist.Add(filePath);
                    }
                }
            }
            ProcessFileList(filelist);
        }
        public void BuildUpdate()
        {
            try
            {
                BuildUpdate(false);
            }
            catch (Exception ex)
            {
                Log(ex);
                throw (ex);
            }
        }
        public List<string> GetChangeScriptList()
        {
            return GetChangeScriptList(true);
        }
        public List<string> GetChangeScriptList(bool fullpath)
        {
            List<string> filelist = new List<string>();
            
            string fullUpdateDirectory = Path.Combine(ScriptPath, UpdateDirectory);
            foreach (string s in Directory.GetFiles(fullUpdateDirectory))
            {
                //Add to collection to ENSURE proper order, but only add if not yet applied.
                //We also need to ensure they follow the same convention.
                //xx.xx.xx.xx.sql
                Regex match = new Regex(@"^[0-9][0-9]\.[0-9][0-9]\.[0-9][0-9]\.[0-9][0-9].*\.sql", RegexOptions.IgnoreCase);
                if (match.IsMatch(s.Substring(s.LastIndexOf(@"\")+1)))
                {
                    if (!fullpath)
                    {
                        filelist.Add(s.Replace(fullUpdateDirectory, "").Replace(@"\", "").Replace(@".sql", ""));
                    }
                    else
                    {
                        if (Version != String.Empty)
                        {
                        string fn = s.Replace(fullUpdateDirectory, "").Replace(@"\", "").Replace(@".sql", "");

                        string[] filepart = fn.Split('.');
                        int MajorNumber = Convert.ToInt32(filepart[0]);
                        int MinorNumber = Convert.ToInt32(filepart[1]);
                        int RevisionNumber = Convert.ToInt32(filepart[2]);
                        int PatchNumber = Convert.ToInt32(filepart[3]);
                        int FullNumber = (MajorNumber*1000000)
                                            + (MinorNumber * 10000) 
                                            + (RevisionNumber * 100)
                                            + PatchNumber;
                        int VersionFullNumber = 0;
                        try
                        {
                            string[] version = Version.Split('.');
                            int VersionMajorNumber = Convert.ToInt32(version[0]);
                            int VersionMinorNumber = Convert.ToInt32(version[1]);
                            int VersionRevisionNumber = Convert.ToInt32(version[2]);
                            int VersionPatchNumber = Convert.ToInt32(version[3]);
                            VersionFullNumber = (VersionMajorNumber * 1000000)
                                                + (VersionMinorNumber * 10000)
                                                + (VersionRevisionNumber * 100)
                                                + VersionPatchNumber;
                        }
                        catch(Exception err)
                        {
//                            MMDBLogFile.Log(err);
                            VersionFullNumber = FullNumber;
                        }
                            if (VersionFullNumber >= FullNumber)
                            {
                            filelist.Add(s);
                            }
                        }
                        else
                        {
                                                 filelist.Add(s);
                        }

                    }
                }
            }
            filelist.Sort();  //Sort for proper ordering
            return filelist;
        }
        private void BuildUpdate(bool ClearStatus)
        {
            if (ClearStatus)
            {
                _Status = "";
            }
            WriteStatus("-----Build Update STARTING...");
            List<string> filelist = GetChangeScriptList();
            
            RunChangeScripts(filelist);

            WriteStatus("-----Build Update COMPLETE.");
        }
        public void BuildCompile()
        {
            BuildCompile(false);
        }
        private void BuildCompile(bool ClearStatus)
        {
            WriteStatus("-----Build Compile STARTING...");

            ProcessLSTFile(BuildScriptFilePath(TriggerDirectory, TriggersFile));
            ProcessLSTFile(BuildScriptFilePath(FunctionDirectory, FunctionsFile));
            ProcessLSTFile(BuildScriptFilePath(ViewDirectory, ViewsFile));
            ProcessLSTFile(BuildScriptFilePath(StoredProcedureDirectory, StoredProcedureFile));
            if (Encrypt)
            {
                EncryptObjects();
            }
            WriteStatus("-----Build Compile COMPLETE.");
        }

        private void BuildChangeScriptProc()
        {
            string CreateCheckProc = "CREATE PROCEDURE _CheckVersion\r\n" +
                "@MajorNumber int,\r\n" +
                "@MinorNumber int,\r\n" +
                "@RevisionNumber int,\r\n" +
                "@PatchNumber int,\r\n" +
                "@ReturnValue int OUTPUT\r\n" +
                "AS\r\n" +
                "SET @ReturnValue = 0\r\n" +
                "IF EXISTS(SELECT * FROM _ChangeManagement)\r\n" +
                "BEGIN\r\n" +
                "	IF NOT EXISTS(SELECT * FROM _ChangeManagement WHERE " +
                "(MajorNumber=@MajorNumber and MinorNumber=@MinorNumber and RevisionNumber=@RevisionNumber and PatchNumber=@PatchNumber-1)" +
                "OR (MajorNumber=@MajorNumber and MinorNumber=@MinorNumber and RevisionNumber=@RevisionNumber-1 and @PatchNumber=0)\r\n" +
                "OR (MajorNumber=@MajorNumber and MinorNumber=@MinorNumber-1 and @RevisionNumber=0 and @PatchNumber=0)\r\n" +
                "OR (MajorNumber=@MajorNumber-1 and @MinorNumber=1 and @RevisionNumber=0 and @PatchNumber=0)\r\n" +
                ")\r\n" +
                "	BEGIN\r\n" +
                "		SET @ReturnValue = 2\r\n" +
                "	END\r\n" +
                "	IF EXISTS(SELECT * FROM _ChangeManagement WHERE MajorNumber=@MajorNumber and MinorNumber=@MinorNumber and RevisionNumber=@RevisionNumber and PatchNumber=@PatchNumber)\r\n" +
                "	BEGIN\r\n" +
                "		SET @ReturnValue = 1\r\n" +
                "	END\r\n" +
                "END\r\n";


            string DropCheckProc = "IF EXISTS(SELECT * FROM sysobjects where name = '_CheckVersion' and xtype='P') BEGIN DROP PROCEDURE _CheckVersion END";

            string CreateUpdateProc = "CREATE PROCEDURE _UpdateVersion\r\n" +
                "@MajorNumber int,\r\n" +
                "@MinorNumber int,\r\n" +
                "@RevisionNumber int,\r\n" +
                "@PatchNumber int\r\n" +
                "AS\r\n" +
                "INSERT INTO _ChangeManagement(MajorNumber, MinorNumber, RevisionNumber, PatchNumber) VALUES(@MajorNumber, @MinorNumber, @RevisionNumber, @PatchNumber)";

            string DropUpdateProc = "IF EXISTS(SELECT * FROM sysobjects where name = '_UpdateVersion' and xtype='P') BEGIN DROP PROCEDURE _UpdateVersion END";

            //build it.
            ProcessScript(DropCheckProc);
            ProcessScript(CreateCheckProc);
            ProcessScript(DropUpdateProc);
            ProcessScript(CreateUpdateProc);

        }
        private void ProcessFileList(List<string> filelist)
        {
            foreach (string s in filelist)
            {
                WriteStatus("Processing:  " + s);
                RunScript(s);
            }
        }
        private void RunChangeScripts(List<string> filelist)
        {
            BuildChangeScriptProc();
            foreach (string fullFilePath in filelist)
            {
                //Look up to see if the file has been processed.
                //Filename should be like 00.00.00.00.sql

                string fileName = Path.GetFileName(fullFilePath);
                string[] filepart = fileName.Split('.');
                int MajorNumber = Convert.ToInt32(filepart[0]);
                int MinorNumber = Convert.ToInt32(filepart[1]);
                int RevisionNumber = Convert.ToInt32(filepart[2]);
                int PatchNumber = Convert.ToInt32(filepart[3]);

                //This statement looks to see if it was run yet, BUT more importantly, if it SHOULD run.  
                //It will throw an exception if hte file should not yet have run.
                if (ShouldChangeScriptRun(MajorNumber, MinorNumber, RevisionNumber, PatchNumber))
                {
                    //if not run, run it.
                    WriteStatus("Processing file: " + fullFilePath);
                    RunScript(fullFilePath);
                    //Update that it has been run.
                    UpdateChangeScript(MajorNumber, MinorNumber, RevisionNumber, PatchNumber);
                }
                else
                {
                    Log("File already applied:  " + fileName);
                }
            }
        }

        private void UpdateChangeScript(int MajorNumber, int MinorNumber, int RevisionNumber, int PatchNumber)
        {
            try
            {

                SqlDatabase db = new SqlDatabase(GetConnectionString());

                string sqlCommand = "_UpdateVersion";
                DbCommand dbCommand = db.GetStoredProcCommand(sqlCommand);

                db.AddInParameter(dbCommand, "MajorNumber", DbType.Int32, MajorNumber);
                db.AddInParameter(dbCommand, "MinorNumber", DbType.Int32, MinorNumber);
                db.AddInParameter(dbCommand, "RevisionNumber", DbType.Int32, RevisionNumber);
                db.AddInParameter(dbCommand, "PatchNumber", DbType.Int32, PatchNumber);


                db.ExecuteNonQuery(dbCommand);
            }
            catch (Exception ex)
            {
                Log(ex, "UpdateChangeScript");
            }

        }

        private bool ShouldChangeScriptRun(int MajorNumber, int MinorNumber, int RevisionNumber, int PatchNumber)
        {
            try
            {

                SqlDatabase db = new SqlDatabase(GetConnectionString());

                string sqlCommand = "_CheckVersion";
                DbCommand dbCommand = db.GetStoredProcCommand(sqlCommand);

                db.AddInParameter(dbCommand, "MajorNumber", DbType.Int32, MajorNumber);
                db.AddInParameter(dbCommand, "MinorNumber", DbType.Int32, MinorNumber);
                db.AddInParameter(dbCommand, "RevisionNumber", DbType.Int32, RevisionNumber);
                db.AddInParameter(dbCommand, "PatchNumber", DbType.Int32, PatchNumber);
                db.AddOutParameter(dbCommand, "ReturnValue", DbType.Int32, 0);

                int DataFoundIndicator = 0;

                db.ExecuteNonQuery(dbCommand);
                DataFoundIndicator = Convert.ToInt32(db.GetParameterValue(dbCommand, "ReturnValue"));

                if (DataFoundIndicator == 0)
                {
                    //This means the previous one was found, but the current one was not.
                    return true;
                }
                if (DataFoundIndicator == 1)
                {
                    //This means the current one was not found.
                    return false;
                }
                if (SkipMissingChangeScripts)
                {
                    //If flag is set to skip,then allow skips....
                    Log("File not found for change script (skipped flag set):  " + String.Format("{0:00}", MajorNumber) + "." +
                        String.Format("{0:00}",MinorNumber) + "." + 
                        String.Format("{0:00}",RevisionNumber) + "." + 
                        String.Format("{0:00}",PatchNumber));

                    return true;
                }
                
                throw (new DBBuildException("A file is found to process, but the previous one was not found as processed. "));
            }
            catch (DBBuildException ex)
            {
                Log(ex, "ChangeScriptRun", "DBBuildException");
                throw (ex);
            }
            catch (Exception ex)
            {
                Log(ex, "ChangeScriptRun");
                return false;
            }

        }

    }
}
