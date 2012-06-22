using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using System.IO;
using System.Collections.Specialized;
using System.Collections;
using EntLib = Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using Microsoft.Practices.EnterpriseLibrary.Common;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.ExtraInformation;
using System.Data.Common;
using System.Data;
using System.Threading;
using MMDB.Core.ExtensionMethods;

namespace MMDB.Core.DBBuild
{
    public class DBScriptHelper : DBHelper
    {
        public DBScriptHelper(string ServerText, string DatabaseText, string UsernameText, string PasswordText, string PathText)
        {
            Server = ServerText;
            Database = DatabaseText;
            Username = UsernameText;
            Password = PasswordText;
            ScriptPath = PathText;
            _Status = "";
            SetDefaultScripting();
        }
        public DBScriptHelper()
        {
            ScriptPath = "";
            _Status = "";
            Server = "";
            Username = "";
            Password = "";
            Database = "";
            //            IntegratedSecurity = false;
            SetDefaultScripting();
            Log("Called DBScriptHelper constructor");
        }

        public enum ScriptType
        {
            Table,
            Procedure,
            View,
            Function,
            Trigger
        }
        private void SetDefaultScripting()
        {
            ShouldScriptData = true;
            ShouldScriptForeignKeys = true;
            ShouldScriptFunctions = true;
            ShouldScriptIndexes = true;
            ShouldScriptProcedures = true;
            ShouldScriptTables = true;
            ShouldScriptTriggers = true;
            ShouldScriptViews = true;
            ShouldScriptCreateDatabase = true;
        }
        public bool ShouldScriptTables { get; set; }
        public bool ShouldScriptForeignKeys { get; set; }
        public bool ShouldScriptIndexes { get; set; }
        public bool ShouldScriptTriggers { get; set; }
        public bool ShouldScriptProcedures { get; set; }
        public bool ShouldScriptCreateDatabase { get; set; }
        public bool ShouldScriptViews { get; set; }
        public bool ShouldScriptData { get; set; }
        public bool ShouldScriptFunctions { get; set; }
        Thread thread_CreateDatabase;
        Thread thread_Tables;
        Thread thread_ForeignKeys;
        Thread thread_Indexes;
        Thread thread_Procedures;
        Thread thread_Triggers;
        Thread thread_Functions;
        Thread thread_Views;
        Thread thread_Data;
        Thread thread_PartitionScheme;
        Thread thread_PartitionFunction;

        public void Script()
        {
            //            Server s = new Server(DBServer);
            //            Database db = s.Databases(DatabaseName);
            //            Scripter scripter = new Scripter(s);


            try
            {


#if DEBUG
                if (ShouldScriptCreateDatabase)
                {
                    ScriptCreateDatabase();
                    ScriptPartitionFunction();
                    ScriptPartitionScheme();
                }
                if (ShouldScriptTables)
                { ScriptTables(); }
                if (ShouldScriptForeignKeys)
                { ScriptForeignKeys(); }
                if (ShouldScriptIndexes)
                {ScriptIndexes();}
                if (ShouldScriptProcedures)
                {ScriptProcedures();}
                if (ShouldScriptTriggers)
                {ScriptTriggers();}
                if (ShouldScriptFunctions)
                {ScriptFunctions();}
                if (ShouldScriptViews)
                {ScriptViews();}
                if (ShouldScriptData)
                { ScriptData(); }
#else
                if (ShouldScriptCreateDatabase)
                {
                    thread_CreateDatabase = new Thread(new ThreadStart(ScriptCreateDatabase));
                    thread_CreateDatabase.Start();
                    thread_Tables = new Thread(new ThreadStart(ScriptTables));
                    thread_Tables.Start();
                    thread_PartitionFunction = new Thread(new ThreadStart(ScriptPartitionFunction));
                    thread_PartitionFunction.Start();
                    thread_PartitionScheme = new Thread(new ThreadStart(ScriptPartitionScheme));
                    thread_PartitionScheme.Start();
                    
                }
                if (ShouldScriptForeignKeys)
                {
                    thread_ForeignKeys = new Thread(new ThreadStart(ScriptForeignKeys));
                    thread_ForeignKeys.Start();
                }
                if (ShouldScriptIndexes)
                {
                    thread_Indexes = new Thread(new ThreadStart(ScriptIndexes));
                    thread_Indexes.Start();
                }
                if (ShouldScriptProcedures)
                {
                    thread_Procedures = new Thread(new ThreadStart(ScriptProcedures));
                    thread_Procedures.Start();
                }
                if (ShouldScriptTriggers)
                {
                    thread_Triggers = new Thread(new ThreadStart(ScriptTriggers));
                    thread_Triggers.Start();
                }
                if (ShouldScriptFunctions)
                {
                    thread_Functions = new Thread(new ThreadStart(ScriptFunctions));
                    thread_Functions.Start();
                }
                if (ShouldScriptViews)
                {
                    thread_Views = new Thread(new ThreadStart(ScriptViews));
                    thread_Views.Start();
                }
                if (ShouldScriptData)
                {
                    thread_Data = new Thread(new ThreadStart(ScriptData));
                    thread_Data.Start();
                }
#endif

            }
            catch (Exception err)
            {
                throw err;
            }
        }
        public bool IsScripting()
        {
            try
            {
                return
                    thread_CreateDatabase.IsAlive ||
                    thread_Tables.IsAlive ||
                    thread_ForeignKeys.IsAlive ||
                    thread_Indexes.IsAlive ||
                    thread_Procedures.IsAlive ||
                    thread_Triggers.IsAlive ||
                    thread_Functions.IsAlive ||
                    thread_Views.IsAlive ||
                    thread_Data.IsAlive;
            }
            catch //(NullReferenceException err)
            {
                //The threads have never been fired up, so return false;
                return false;
            }
        }
        public void CancelScripting()
        {
            if (thread_CreateDatabase.IsAlive) { thread_CreateDatabase.Abort(); }
            if (thread_Tables.IsAlive) { thread_Tables.Abort(); }
            if (thread_ForeignKeys.IsAlive) { thread_ForeignKeys.Abort(); }
            if (thread_Indexes.IsAlive) { thread_Indexes.Abort(); }
            if (thread_Procedures.IsAlive) { thread_Procedures.Abort(); }
            if (thread_Triggers.IsAlive) { thread_Triggers.Abort(); }
            if (thread_Functions.IsAlive) { thread_Functions.Abort(); }
            if (thread_Views.IsAlive) { thread_Views.Abort(); }
            if (thread_Data.IsAlive) { thread_Data.Abort(); }

        }
        //TODO:  Make this called from the script directory.
        string ChangeManagementTable = "CREATE TABLE _ChangeManagement (id int identity(1,1), MajorNumber int, MinorNumber int, RevisionNumber int, PatchNumber int, AppliedDateTime datetime default getdate())\r\nGO\r\n";// +
        //"INSERT INTO _ChangeManagement (MajorNumber, MinorNumber, RevisionNumber, PatchNumber) VALUES(1,0,0,0)";
        private void ScriptCreateDatabase()
        {
            //For now, write a basic row where it belongs.
            string command = "IF EXISTS(SELECT * FROM master.dbo.sysdatabases where name = '[[DB_NAME]]')\r\n";
            command += "BEGIN\r\n";
            command += "DROP DATABASE [[DB_NAME]]\r\n";
            command += "END\r\n";
            command += "GO\r\n";
            //command += "CREATE DATABASE [[DB_NAME]]";

            StringCollection strcoll = GetDatabase().Script();
            foreach (String str in strcoll)
            {
                command += str.Replace(Database, "[[DB_NAME]]");
                command += "\r\nGO\r\n";
            }

            CreateDirectory(Path.Combine(ScriptPath, ChangeScriptDirectory));
            CreateDirectory(Path.Combine(ScriptPath, SeedDataDirectory));

            string databaseDirectory = Path.Combine(ScriptPath, DatabaseDirectory);
            CreateDirectory(databaseDirectory);
            string databaseFilename = Path.Combine(databaseDirectory, CreateFile);

            using (StreamWriter sw = new StreamWriter(databaseFilename))
            {
                sw.WriteLine(command);
                sw.Close();
            }
            string relativedatabaseFilePath = this.BuildRelativeFilePath(databaseFilename);
            WriteStatus("File written:  " + relativedatabaseFilePath);


            //Now create install.lst
            string installFilename = Path.Combine(ScriptPath, InstallFile);
            StringCollection sc = new StringCollection();
            sc.Add(Path.Combine(DatabaseDirectory, CreateFile));
            sc.Add(Path.Combine(DatabaseDirectory, PartitionFunctionFile));
            sc.Add(Path.Combine(DatabaseDirectory, PartitionSchemeFile));
            sc.Add(Path.Combine(TableDirectory, TableFile));
            sc.Add(Path.Combine(SeedDataDirectory, SeedDataFile));
            sc.Add(Path.Combine(ForeignKeyDirectory, ForeignKeyFile));
            sc.Add(Path.Combine(IndexDirectory, IndexFile));
            WriteFile(installFilename, sc);
            Log("File Written:  " + installFilename);
        }
        private void CreateDirectory(string filepath)
        {
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
            StringCollection FileCollection = new StringCollection();
        }

        private Database GetDatabase()
        {
            Server s = null;  //We just don't do anything with it.
            return GetDatabase(ref s);
        }
        private Database GetDatabase(ref Server srv)
        {
            try
            {
                Microsoft.SqlServer.Management.Common.ServerConnection conn = new Microsoft.SqlServer.Management.Common.ServerConnection(new System.Data.SqlClient.SqlConnection(GetConnectionString()));

                Server s = new Server(conn);
                s.SetDefaultInitFields(typeof(StoredProcedure), "IsSystemObject");
                s.SetDefaultInitFields(typeof(Table), "IsSystemObject");
                s.SetDefaultInitFields(typeof(View), "IsSystemObject");
                s.SetDefaultInitFields(typeof(UserDefinedFunction), "IsSystemObject");
                s.SetDefaultInitFields(typeof(Trigger), "IsSystemObject");
                srv = s; //Return the server as well, in the evnet we are using dependency walkaer.
                return s.Databases[Database];
            }
            catch (Exception err)
            {
                MMDB.Core.MMDBLogFile.Log(err);
                return null;
            }
        }
        public StringCollection GetTableList()
        {
            try
            {
                Database db = GetDatabase();
                StringCollection sc = new StringCollection();

                foreach (Table t in db.Tables)
                {
                    sc.Add(t.Name);
                }

                return sc;
            }
            catch (Exception err)
            {
                MMDB.Core.MMDBLogFile.Log(err);
                return null;
            }
        }
        private void ScriptTables()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.Triggers = false;
                so.DdlBodyOnly = false;
                so.SchemaQualify = true;
                so.AllowSystemObjects = false;
                so.ClusteredIndexes = false;
                so.Indexes = false;
                so.DriAllConstraints = false;
                so.DriAllKeys = false;
                so.DriChecks = true;
                so.DriDefaults = true;
                so.DriForeignKeys = false;
                so.DriIndexes = false;
                so.DriNonClustered = false;
                so.DriPrimaryKey = true;
                so.DriUniqueKeys = false;
                so.AnsiPadding = false;
                #endregion
                Database db = GetDatabase();

                string filepath = Path.Combine(ScriptPath, TableDirectory);

                StringCollection FileCollection = new StringCollection();
                StringCollection sc = new StringCollection();
                CreateDirectory(filepath);
                foreach (Table t in db.Tables)
                {
                    if (t.Name != "_ChangeManagement")
                    {

                        string tableFileName = Path.Combine(filepath, t.Schema.ToString() + "." + t.Name.ToString() + ".sql");
                        sc = t.Script(so);
                        WriteFile(tableFileName, sc);
                        string relativeTableFilePath = this.BuildRelativeFilePath(tableFileName);
                        FileCollection.Add(relativeTableFilePath);
                        WriteStatus("File written:  " + relativeTableFilePath);
                    }

                }
                //Now, add a special table for change management
                sc.Clear();
                sc.Add(ChangeManagementTable);
                string cmFilePath = Path.Combine(filepath, "_ChangeManagement.sql");
                WriteFile(cmFilePath, sc);
                string relativeCmFilePath = this.BuildRelativeFilePath(cmFilePath);
                FileCollection.Add(relativeCmFilePath);
                WriteStatus("File written:  " + relativeCmFilePath);

                string tableListFilePath = this.BuildScriptFilePath(TableDirectory, TableFile);
                WriteFile(tableListFilePath, FileCollection);
                string relativeTableListFilePath = this.BuildRelativeFilePath(tableListFilePath);
                WriteStatus("File written:  " + relativeCmFilePath);

            }
            catch (Exception err)
            {
                string name = "ScriptTables";
                WriteStatus(name + "-" + Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {

                    MMDB.Core.MMDBLogFile.Log(err, name);
                    throw new TableScriptingException("A table scripting exception has been found.  Review the log for more information.", err);
                }
            }
        }

        private void ScriptForeignKeys()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.Triggers = false;
                so.DdlBodyOnly = true;
                so.SchemaQualify = true;
                so.AllowSystemObjects = false;
                so.ClusteredIndexes = false;
                so.Indexes = false;
                so.DriAllConstraints = false;
                so.DriAllKeys = false;
                so.DriChecks = false;
                so.DriForeignKeys = true;
                so.DriIndexes = false;
                so.DriNonClustered = false;
                so.DriPrimaryKey = false;
                so.DriUniqueKeys = false;
                so.AnsiPadding = false;
                #endregion
                Database db = GetDatabase();

                string filepath = Path.Combine(ScriptPath, ForeignKeyDirectory);

                StringCollection FileCollection = new StringCollection();
                StringCollection sc = new StringCollection();
                CreateDirectory(filepath);
                foreach (Table t in db.Tables)
                {
                    foreach (ForeignKey f in t.ForeignKeys)
                    {
                        string fkFilePath = Path.Combine(filepath, t.Schema.ToString() + "." + f.Name.ToString() + ".sql");
                        sc = f.Script(so);
                        WriteFile(fkFilePath, sc);
                        string relativeFkFilePath = this.BuildRelativeFilePath(fkFilePath);
                        FileCollection.Add(relativeFkFilePath); // We want a relative path here.
                        WriteStatus("File written:  " + relativeFkFilePath);
                    }
                }
                string fkListFilePath = this.BuildScriptFilePath(ForeignKeyDirectory, ForeignKeyFile);
                WriteFile(fkListFilePath, FileCollection);
                string relativeFkListFilePath = this.BuildRelativeFilePath(fkListFilePath);
                WriteStatus("File written:  " + relativeFkListFilePath);
            }
            catch (Exception err)
            {
                string name = "ScriptForeignKeys";
                WriteStatus(name + "-" + Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus(Thread.CurrentThread.ThreadState.ToString());
                    if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                    {
                        WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                        MMDB.Core.MMDBLogFile.Log(err, name);
                        throw new ForeignKeyScriptingException("A foreign key scripting exception has been found.  Please review the log for more information.", err);
                    }
                }
            }
        }
        private void ScriptIndexes()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.Triggers = false;
                so.DdlBodyOnly = false;
                so.SchemaQualify = true;
                so.AllowSystemObjects = false;
                so.ClusteredIndexes = true;
                so.Indexes = true;
                so.DriAllConstraints = false;
                so.DriAllKeys = false;
                so.DriChecks = false;
                so.DriForeignKeys = false;
                so.DriIndexes = false;
                so.DriNonClustered = false;
                so.DriPrimaryKey = false;
                so.DriUniqueKeys = false;
                so.AnsiPadding = false;
                #endregion
                Database db = GetDatabase();

                string filepath = Path.Combine(ScriptPath, IndexDirectory);

                StringCollection FileCollection = new StringCollection();
                StringCollection sc = new StringCollection();
                CreateDirectory(filepath);
                foreach (Table t in db.Tables)
                {
                    foreach (Index i in t.Indexes)
                    {
                        string indexFilePath = Path.Combine(filepath, t.Schema.ToString() + "." + i.Name.ToString() + ".sql");
                        sc = i.Script(so);
                        bool Skip = false;
                        foreach (string s in sc)
                        {
                            //Primary Keys are written with table.                    
                            //So, that being said, continue here.
                            if (s.Contains("PRIMARY KEY"))
                            {
                                Skip = true;
                                continue;
                            }
                        }
                        if (!Skip)
                        {
                            WriteFile(indexFilePath, sc);
                            string relativeIndexFilePath = this.BuildRelativeFilePath(indexFilePath);
                            FileCollection.Add(relativeIndexFilePath); // We want a relative path here.
                            WriteStatus("File written:  " + relativeIndexFilePath);
                        }
                    }
                }
                string indexListFilePath = this.BuildScriptFilePath(IndexDirectory, IndexFile);
                WriteFile(indexListFilePath, FileCollection);
                string relativeIndexListFilePath = this.BuildRelativeFilePath(indexListFilePath);
                WriteStatus("File written:  " + relativeIndexListFilePath);
            }
            catch (Exception err)
            {
                string name = "ScriptIndexes";
                WriteStatus(name + "-" + Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, name);
                    throw new IndexScriptingException("A index scripting exception has been found.  Please review the log for more information.", err);
                }
            }

        }

        private void ScriptProcedures()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.NoExecuteAs = true;
                so.ScriptDrops = false;
                so.SchemaQualify = true;
                so.AllowSystemObjects = false;


                #endregion

                Server srv = new Server();
                Database db = GetDatabase(ref srv);
                srv.SetDefaultInitFields(typeof(View), "IsSystemObject");

                UrnCollection urns = new UrnCollection();

                foreach (StoredProcedure v in db.StoredProcedures)
                {
                    // exclude these objects        
                    if (v.IsSystemObject) continue;
//                    if (v.Name.StartsWith("aspnet_")) continue;
                    urns.Add(v.Urn);
                }


                string filepath = this.BuildScriptFilePath(StoredProcedureDirectory, null);

                StringCollection FileCollection = new StringCollection();
                CreateDirectory(filepath);

                if (urns.Count > 0)
                {
                    DependencyWalker depwalker = new Microsoft.SqlServer.Management.Smo.DependencyWalker(srv);
                    DependencyTree tree = depwalker.DiscoverDependencies(urns, true);
                    DependencyCollection depcoll = depwalker.WalkDependencies(tree);
                    foreach (DependencyCollectionNode dep in depcoll)
                    {
                        if (dep.Urn.Type == "StoredProcedure")
                        {
                            foreach (StoredProcedure t in db.StoredProcedures)
                            {
                                if (t.Name.ToString() == dep.Urn.GetAttribute("Name").ToString())
                                {
                                    if (!t.IsEncrypted)
                                    {
                                        string spFilePath = Path.Combine(filepath, t.Schema.ToString() + "." + t.Name.ToString() + ".sql");
                                        so.ScriptDrops = true;

                                        StringCollection drop = t.Script(so);
                                        so.ScriptDrops = false;
                                        StringCollection create = t.Script();
                                        StringCollection sc = new StringCollection();
                                        sc.Add("IF EXISTS(SELECT * FROM SYSOBJECTS WHERE XTYPE='P' AND NAME='" + t.Name.ToString() + "')\r\nBEGIN\r\n");

                                        foreach (string s in drop)
                                        {
                                            sc.Add(s);
                                        }
                                        sc.Add("\r\nEND\r\nGO\r\n");
                                        foreach (string s in create)
                                        {
                                            sc.Add(s);
                                            sc.Add("GO\r\n");
                                        }

                                        WriteFile(spFilePath, sc);
                                        string relativeSpFilePath = this.BuildRelativeFilePath(spFilePath);
                                        FileCollection.Add(relativeSpFilePath); // We want a relative path here.
                                        WriteStatus("File written:  " + relativeSpFilePath);
                                    }
                                    else
                                    {
                                        WriteStatus("WARNING: Stored Procedures is NOT scripted.  It was found to be encrypted and cannot be scripted.  (" + t.Name + ")");
                                    }

                                    break;
                                }
                            }
                        }
                    }
                } //if urns.Count > 0
                string spListFilePath = this.BuildScriptFilePath(StoredProcedureDirectory, StoredProcedureFile);
                WriteFile(spListFilePath, FileCollection);
                string relativeSpListFilePath = this.BuildRelativeFilePath(spListFilePath);
                WriteStatus("File written:  " + relativeSpListFilePath);
            }
            catch (Exception err)
            {
                string name = "ScriptProcedures";
                WriteStatus(name + "-" + Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, name);
                    throw new StoredProcedureScriptingException("A stored procedure key scripting exception has been found.  Please review the log for more information.", err);
                }

            }


        }
        private void ScriptTriggers()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.NoExecuteAs = true;
                so.ScriptDrops = false;
                so.SchemaQualify = true;
                so.AllowSystemObjects = false;
                so.Triggers = true;

                #endregion

                Server srv = new Server();
                Database db = GetDatabase(ref srv);
                srv.SetDefaultInitFields(typeof(View), "IsSystemObject");

                UrnCollection urns = new UrnCollection();

                foreach (Table v in db.Tables)
                {
                    foreach (Trigger t in v.Triggers)
                    {
                        // exclude these objects        
                        if (t.IsSystemObject) continue;

                        if (t.Name.StartsWith("aspnet_")) continue;
                        urns.Add(t.Urn);
                    }
                }

                string filepath = this.BuildScriptFilePath(TriggerDirectory, null);

                StringCollection FileCollection = new StringCollection();
                CreateDirectory(filepath);
                foreach (Table v in db.Tables)
                {
                    foreach (Trigger t in v.Triggers)
                    {
                        if (!t.IsSystemObject)
                        {
                            string triggerFilePath = Path.Combine(filepath, v.Schema + "." + t.Name.ToString() + ".sql");
                            so.ScriptDrops = true;

                            StringCollection drop = t.Script(so);
                            so.ScriptDrops = false;
                            t.TextMode = false;
                            StringCollection create = t.Script();
                            StringCollection sc = new StringCollection();
                            if (t.IsEnabled)
                            {
                                sc.Add("IF EXISTS(SELECT * FROM SYSOBJECTS WHERE XTYPE='TR' AND NAME='" + t.Name.ToString() + "')\r\nBEGIN\r\n");
                                foreach (string s in drop)
                                {
                                    sc.Add(s);
                                }
                                sc.Add("\r\nEND\r\nGO\r\n");
                                foreach (string s in create)
                                {
                                    sc.Add(s);
                                    sc.Add("GO\r\n");
                                }

                                WriteFile(triggerFilePath, sc);
                                string relativeTriggerFilePath = this.BuildRelativeFilePath(triggerFilePath);
                                FileCollection.Add(relativeTriggerFilePath); // We want a relative path here.
                                WriteStatus("File written:  " + relativeTriggerFilePath);
                            }
                        }

                        else
                        {
                            WriteStatus("WARNING:  is NOT scripted.  It was found to be encrypted and cannot be scripted.  (" + t.Name + ")");
                        }
                    }
                }
                string triggerListFilePath = this.BuildScriptFilePath(TriggerDirectory, TriggersFile);
                WriteFile(triggerListFilePath, FileCollection);
                string relativeTriggerListFilePath = this.BuildRelativeFilePath(triggerListFilePath);
                WriteStatus("File written:  " + relativeTriggerListFilePath);
            }
            catch (Exception err)
            {
                string name = "ScriptTriggers";
                WriteStatus(name + "-" + Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, name);
                    throw new TriggerScriptingException("A trigger scripting exception has been found.  Please review the log for more information.", err);
                }
            }

        }
        private List<SeedDataItem> _ScriptInsertTables = new List<SeedDataItem>();

        public List<SeedDataItem> ScriptInsertTables
        {
            get
            {
                return _ScriptInsertTables;
            }
            set
            {
                _ScriptInsertTables = value;
            }
        }

        private void ScriptData()
        {
            try
            {
                string filepath = Path.Combine(ScriptPath, SeedDataDirectory);
                StringCollection FileCollection = new StringCollection();
                StringCollection sc = new StringCollection();
                CreateDirectory(filepath);

                //some logic for large seeddata files.  Break them into 1,000 inserts per file.
                int RowCount = 2000;
                int FileCount = 0;
                string OriginalDataFilePath = "";
                string dataListFilePath = this.BuildScriptFilePath(SeedDataDirectory, SeedDataFile);
                foreach (SeedDataItem s in _ScriptInsertTables)
                {
                    string seedDataFilePath = Path.Combine(filepath, s.SeedDataElement + SeedDataFileExtension);
                    OriginalDataFilePath = seedDataFilePath;
                    StringCollection tableInserts = ScriptDataTable(s.SeedDataElement);
                    FileCount = (tableInserts.Count / RowCount) + 1; //how many files should we build?
                    for (int i = 0; i < FileCount; i++)
                    {
                        string newpath = seedDataFilePath.Replace(SeedDataFileExtension, i.ToString() + SeedDataFileExtension);
                        StringCollection seeddata = new StringCollection();
                        bool hasIdentity = GetTableHasIdentity(s.SeedDataElement);
                        if (hasIdentity)
                        {
                            seeddata.Add("SET IDENTITY_INSERT " + s.SeedDataElement + " ON");
                        }
                        seeddata.Add(tableInserts.GetRange(RowCount * i, (RowCount * (i + 1)) - 1));
                        if (hasIdentity)
                        {
                            seeddata.Add("SET IDENTITY_INSERT " + s.SeedDataElement + " OFF");
                        }
                        WriteFile(newpath, seeddata);
                        string relativeSeedDataFilePath = this.BuildRelativeFilePath(newpath);
                        FileCollection.Add(relativeSeedDataFilePath);
                        WriteStatus("File written:  " + relativeSeedDataFilePath);
                    }
                }
                WriteFile(dataListFilePath, FileCollection);
                string relativeDataListFilePath = this.BuildRelativeFilePath(dataListFilePath);
                WriteStatus("File written:  " + relativeDataListFilePath);
            }
            catch (Exception err)
            {
                string name = "ScriptData";
                WriteStatus(name + "-" + Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, name);
                    throw new TriggerScriptingException("A data scripting exception has been found.  Please review the log for more information.", err);
                }
            }
        }
        private StringCollection ScriptDataTable(string TableName)
        {
            string filename = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\Scripts\sp_generate_inserts.sql";
            //First make sure we have the appropriate scriptdata proc in master.
            //            	@table_name varchar(776),  		-- The table/view for which the INSERT statements will be generated using the existing data
            //	            @target_table varchar(776) = NULL, 	-- Use this parameter to specify a different table name into which the data will be inserted
            //	            @include_column_list bit = 1,		-- Use this parameter to include/ommit column list in the generated INSERT statement
            //	            @from varchar(800) = NULL, 		-- Use this parameter to filter the rows based on a filter condition (using WHERE)
            //	            @include_timestamp bit = 0, 		-- Specify 1 for this parameter, if you want to include the TIMESTAMP/ROWVERSION column's data in the INSERT statement
            //	            @debug_mode bit = 0,			-- If @debug_mode is set to 1, the SQL statements constructed by this procedure will be printed for later examination
            //	            @owner varchar(64) = NULL,		-- Use this parameter if you are not the owner of the table
            //	            @ommit_images bit = 0,			-- Use this parameter to generate INSERT statements by omitting the 'image' columns
            //	            @ommit_identity bit = 0,		-- Use this parameter to ommit the identity columns
            //	            @top int = NULL,			-- Use this parameter to generate INSERT statements only for the TOP n rows
            //	            @cols_to_include varchar(8000) = NULL,	-- List of columns to be included in the INSERT statement
            //	            @cols_to_exclude varchar(8000) = NULL,	-- List of columns to be excluded from the INSERT statement
            //	            @disable_constraints bit = 0,		-- When 1, disables foreign key constraints and enables them after the INSERT statements
            //	            @ommit_computed_cols bit = 0		-- When 1, computed columns will not be included in the INSERT statement
            filename = filename.Replace(@"file:\", "");
            RunScript(filename);

            //Now call it.
            StringCollection sc = new StringCollection();
            try
            {
                bool hasIdentity = GetTableHasIdentity(TableName);
                if (hasIdentity)
                {
                    //                    sc.Add(string.Format("SET IDENTITY_INSERT [{0}] ON", TableName));
                }
                SqlDatabase db = new SqlDatabase(GetConnectionString());

                string sqlCommand = "MMDB_ScriptData";
                DbCommand dbCommand = db.GetStoredProcCommand(sqlCommand);

                //We could add more parms if ever needed.
                db.AddInParameter(dbCommand, "vchTableName", DbType.String, TableName);


                DataSet ds = db.ExecuteDataSet(dbCommand);

                foreach (DataRow r in ds.Tables[0].Rows)
                {
                    sc.Add(r[0].ToString().Replace("'NULL'", "NULL"));
                }

                if (hasIdentity)
                {
                    //                    sc.Add(string.Format("SET IDENTITY_INSERT [{0}] OFF", TableName));
                }
            }
            catch (Exception ex)
            {
                Log(ex, "UpdateChangeScript");
            }

            return sc;

        }

        private bool GetTableHasIdentity(string tableName)
        {
            bool returnValue = false;
            Database db = GetDatabase();
            Table t = db.Tables[tableName];
            foreach (Column c in t.Columns)
            {
                if (c.Identity)
                {
                    returnValue = true;
                    break;
                }
            }
            return returnValue;
        }
        private void ScriptFunctions()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.NoExecuteAs = true;
                so.ScriptDrops = false;
                so.SchemaQualify = true;
                so.AllowSystemObjects = false;
                #endregion

                Server srv = new Server();
                Database db = GetDatabase(ref srv);
                srv.SetDefaultInitFields(typeof(View), "IsSystemObject");

                UrnCollection urns = new UrnCollection();

                foreach (UserDefinedFunction v in db.UserDefinedFunctions)
                {
                    // exclude these objects        
                    if (v.IsSystemObject) continue;
                    if (v.Name.StartsWith("aspnet_")) continue;
                    urns.Add(v.Urn);
                }

                string filepath = Path.Combine(ScriptPath, FunctionDirectory);
                CreateDirectory(filepath);

                StringCollection FileCollection = new StringCollection();
                CreateDirectory(filepath);
                if (urns.Count > 0)
                {
                    DependencyWalker depwalker = new Microsoft.SqlServer.Management.Smo.DependencyWalker(srv);
                    DependencyTree tree = depwalker.DiscoverDependencies(urns, true);
                    DependencyCollection depcoll = depwalker.WalkDependencies(tree);
                    foreach (DependencyCollectionNode dep in depcoll)
                    {
                        if (dep.Urn.Type == "UserDefinedFunction")
                        {
                            foreach (UserDefinedFunction t in db.UserDefinedFunctions)
                            {
                                if (t.Name.ToString() == dep.Urn.GetAttribute("Name").ToString())
                                {
                                    if (!t.IsEncrypted)
                                    {
                                        string functionFilePath = Path.Combine(filepath, t.Schema + "." + t.Name.ToString() + ".sql");
                                        so.ScriptDrops = true;

                                        StringCollection drop = t.Script(so);
                                        so.ScriptDrops = false;
                                        StringCollection create = t.Script();
                                        StringCollection sc = new StringCollection();
                                        sc.Add("IF EXISTS(SELECT * FROM SYSOBJECTS WHERE XTYPE='FN' AND NAME='" + t.Name.ToString() + "')\r\nBEGIN\r\n");
                                        foreach (string s in drop)
                                        {
                                            sc.Add(s);
                                        }
                                        sc.Add("\r\nEND\r\nGO\r\n");
                                        foreach (string s in create)
                                        {
                                            sc.Add(s);
                                            sc.Add("GO\r\n");
                                        }

                                        WriteFile(functionFilePath, sc);
                                        string relativeFunctionFilePath = this.BuildRelativeFilePath(functionFilePath);
                                        FileCollection.Add(relativeFunctionFilePath); // We want a relative path here.
                                        WriteStatus("File written:  " + relativeFunctionFilePath);
                                        break;
                                    }
                                    else
                                    {
                                        WriteStatus("WARNING: Function is NOT scripted.  It was found to be encrypted and cannot be scripted.  (" + t.Name + ")");
                                    }

                                }
                            }
                        }
                    }

                    //string functionListFilePath = this.BuildScriptFilePath(FunctionDirectory,FunctionsFile);
                    //WriteFile(functionListFilePath, FileCollection);
                    //string relativeFunctionListFilePath = this.BuildRelativeFilePath(functionListFilePath);
                    //WriteStatus("File written:  " + relativeFunctionListFilePath);
                } //if urns.Count > 0
                string functionListFilePath = this.BuildScriptFilePath(FunctionDirectory, FunctionsFile);
                WriteFile(functionListFilePath, FileCollection);
                string relativeFunctionListFilePath = this.BuildRelativeFilePath(functionListFilePath);
                WriteStatus("File written:  " + relativeFunctionListFilePath);
            }
            catch (Exception err)
            {
                WriteStatus(Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, "ScriptFunction");
                    throw new FunctionScriptingException("A function scripting exception has been found.  Please review the log for more information.", err);
                }
            }
        }
        private void ScriptViews()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.IncludeHeaders = false;
                //            so.NoExecuteAs = true;
                so.AllowSystemObjects = true;
                so.NoCommandTerminator = false;
                #endregion
                Server srv = null;
                Database db = GetDatabase(ref srv);
                srv.SetDefaultInitFields(typeof(View), "IsSystemObject");

                UrnCollection urns = new UrnCollection();

                foreach (View v in db.Views)
                {
                    // exclude these objects        
                    if (v.IsSystemObject) continue;
                    if (v.Name.StartsWith("aspnet_")) continue;
                    urns.Add(v.Urn);
                }
                string filepath = this.BuildScriptFilePath(ViewDirectory, null);
                CreateDirectory(filepath);

                StringCollection FileCollection = new StringCollection();

                if (urns.Count > 0)
                {
                    DependencyWalker depwalker = new Microsoft.SqlServer.Management.Smo.DependencyWalker(srv);
                    DependencyTree tree = depwalker.DiscoverDependencies(urns, true);
                    DependencyCollection depcoll = depwalker.WalkDependencies(tree);
                    foreach (DependencyCollectionNode dep in depcoll)
                    {
                        if (dep.Urn.Type == "View")
                        {
                            foreach (View t in db.Views)
                            {
                                if (t.Name.ToString() == dep.Urn.GetAttribute("Name").ToString())
                                {
                                    if (!t.IsEncrypted)
                                    {
                                        string viewFilePath = Path.Combine(filepath, t.Schema + "." + t.Name.ToString() + ".sql");
                                        so.ScriptDrops = true;

                                        StringCollection drop = t.Script(so);
                                        so.ScriptDrops = false;
                                        StringCollection create = t.Script();
                                        StringCollection sc = new StringCollection();
                                        sc.Add("IF EXISTS(SELECT * FROM SYSOBJECTS WHERE XTYPE='V' AND NAME='" + t.Name.ToString() + "')\r\nBEGIN\r\n");
                                        foreach (string s in drop)
                                        {
                                            sc.Add(s);
                                        }
                                        sc.Add("\r\nEND\r\nGO\r\n");
                                        foreach (string s in create)
                                        {
                                            sc.Add(s);
                                            sc.Add("GO\r\n");
                                        }


                                        WriteFile(viewFilePath, sc);
                                        string relativeViewFilePath = this.BuildRelativeFilePath(viewFilePath);
                                        FileCollection.Add(relativeViewFilePath); // We want a relative path here.
                                        WriteStatus("File written:  " + relativeViewFilePath);
                                        break;
                                    }
                                    else
                                    {
                                        WriteStatus("WARNING: View is NOT scripted.  It was found to be encrypted and cannot be scripted.  (" + t.Name + ")");
                                    }
                                }
                            }
                        }
                    }

                } //if urns.Count > 0
                string viewListFilePath = this.BuildScriptFilePath(ViewDirectory, ViewsFile);
                WriteFile(viewListFilePath, FileCollection);
                string relativeViewListFilePath = this.BuildRelativeFilePath(viewListFilePath);
                WriteStatus("File written:  " + relativeViewListFilePath);
            }
            catch (Exception err)
            {
                WriteStatus(Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, "ScriptView");
                    throw new ViewScriptingException("A View scripting exception has been found.  Please review the log for more information.", err);
                }
            }
        }
        private void WriteFile(string Filename, StringCollection sc)
        {
            string text = "";
            bool NothingWritten = true;
            using (StreamWriter sw = new StreamWriter(Filename))
            {
                foreach (Object o in sc)
                {
                    if (o.ToString().Contains("ALTER "))
                    {
                        //Only one of these two should work.
                        text = o.ToString() + "\r\nGO";
                    }
                    else if (o.ToString().Contains("alter "))
                    {
                        //Only one of these two should work.
                        text = o.ToString().Replace("alter ", "GO\r\nalter ");
                    }
                    //REMOVE BECAUSE SOPHISTICATED PROCEDURES HAVE CREATE TABLES, INDEXES, ETC.

                    //if (o.ToString().Contains("CREATE ") && !o.ToString().Contains("CREATE TABLE"))
                    //{
                    //    //Only one of these two should work.
                    //    text = o.ToString().Replace("CREATE ", "GO\r\nCREATE ");
                    //}
                    //else if (o.ToString().Contains("create ") && !o.ToString().Contains("CREATE TABLE"))
                    //{
                    //    //Only one of these two should work.
                    //    text = o.ToString().Replace("create ", "GO\r\ncreate ");
                    //}
                    if (text.Length == 0)
                    {
                        text = o.ToString();
                    }

                    sw.WriteLine("{0}", text);
                    NothingWritten = false;
                    text = "";
                }
                if (NothingWritten)
                {
                    sw.Write(" ");
                }
                sw.Flush();
                sw.Close();
            }
        }
        private void ScriptPartitionFunction()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.IncludeHeaders = false;
                //            so.NoExecuteAs = true;
                so.AllowSystemObjects = true;
                so.NoCommandTerminator = false;
                #endregion
                Server srv = null;
                Database db = GetDatabase(ref srv);

                UrnCollection urns = new UrnCollection();

                foreach (PartitionFunction v in db.PartitionFunctions)
                {
                    // exclude these objects     
                    urns.Add(v.Urn);
                }
                string filepath = this.BuildScriptFilePath(DatabaseDirectory, null);
                CreateDirectory(filepath);

                StringCollection FileCollection = new StringCollection();

                if (urns.Count > 0)
                {
                    DependencyWalker depwalker = new Microsoft.SqlServer.Management.Smo.DependencyWalker(srv);
                    DependencyTree tree = depwalker.DiscoverDependencies(urns, true);
                    DependencyCollection depcoll = depwalker.WalkDependencies(tree);
                    foreach (DependencyCollectionNode dep in depcoll)
                    {
                        if (dep.Urn.Type == "PartitionFunction")
                        {
                            foreach (PartitionFunction t in db.PartitionFunctions)
                            {
                                if (t.Name.ToString() == dep.Urn.GetAttribute("Name").ToString())
                                {
                                    string PartitionFunctionFilePath = Path.Combine(filepath, t.Name.ToString() + ".sql");
                                    so.ScriptDrops = true;

                                    StringCollection drop = t.Script(so);
                                    so.ScriptDrops = false;
                                    StringCollection create = t.Script();
                                    StringCollection sc = new StringCollection();
                                    foreach (string s in create)
                                    {
                                        sc.Add(s);
                                        sc.Add("GO\r\n");
                                    }

                                    WriteFile(PartitionFunctionFilePath, sc);
                                    string relativePartitionFunctionFilePath = this.BuildRelativeFilePath(PartitionFunctionFilePath);
                                    FileCollection.Add(relativePartitionFunctionFilePath); // We want a relative path here.
                                    WriteStatus("File written:  " + relativePartitionFunctionFilePath);
                                    break;
                                }
                            }
                        }
                    }
                } //if urns.Count > 0
                string PartitionFunctionListFilePath = this.BuildScriptFilePath(DatabaseDirectory, PartitionFunctionFile);
                WriteFile(PartitionFunctionListFilePath, FileCollection);
                string relativePartitionFunctionListFilePath = this.BuildRelativeFilePath(PartitionFunctionListFilePath);
                WriteStatus("File written:  " + relativePartitionFunctionListFilePath);
            }
            catch (Exception err)
            {
                WriteStatus(Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, "ScriptPartitionFunction");
                    throw new PartitionFunctionScriptingException("A Partition Scheme scripting exception has been found.  Please read the log for more information.", err);
                }
            }
        }
        private void ScriptPartitionScheme()
        {
            try
            {
                #region Options
                ScriptingOptions so = new ScriptingOptions();
                so.IncludeHeaders = false;
                //            so.NoExecuteAs = true;
                so.AllowSystemObjects = true;
                so.NoCommandTerminator = false;
                #endregion
                Server srv = null;
                Database db = GetDatabase(ref srv);

                UrnCollection urns = new UrnCollection();

                foreach (PartitionScheme v in db.PartitionSchemes)
                {
                    // exclude these objects     
                    urns.Add(v.Urn);
                }
                string filepath = this.BuildScriptFilePath(DatabaseDirectory, null);
                CreateDirectory(filepath);

                StringCollection FileCollection = new StringCollection();

                if (urns.Count > 0)
                {
                    DependencyWalker depwalker = new Microsoft.SqlServer.Management.Smo.DependencyWalker(srv);
                    DependencyTree tree = depwalker.DiscoverDependencies(urns, true);
                    DependencyCollection depcoll = depwalker.WalkDependencies(tree);
                    foreach (DependencyCollectionNode dep in depcoll)
                    {
                        if (dep.Urn.Type == "PartitionScheme")
                        {
                            foreach (PartitionScheme t in db.PartitionSchemes)
                            {
                                if (t.Name.ToString() == dep.Urn.GetAttribute("Name").ToString())
                                {
                                    string partitionschemeFilePath = Path.Combine(filepath, t.Name.ToString() + ".sql");
                                    so.ScriptDrops = true;

                                    StringCollection drop = t.Script(so);
                                    so.ScriptDrops = false;
                                    StringCollection create = t.Script();
                                    StringCollection sc = new StringCollection();
                                    //sc.Add("IF EXISTS(SELECT * FROM SYSOBJECTS WHERE XTYPE='V' AND NAME='" + t.Name.ToString() + "')\r\nBEGIN\r\n");
                                    //foreach (string s in drop)
                                    //{
                                    //    sc.Add(s);
                                    //}
                                    //sc.Add("\r\nEND\r\nGO\r\n");
                                    foreach (string s in create)
                                    {
                                        sc.Add(s);
                                        sc.Add("GO\r\n");
                                    }


                                    WriteFile(partitionschemeFilePath, sc);
                                    string relativePartitionSchemeFilePath = this.BuildRelativeFilePath(partitionschemeFilePath);
                                    FileCollection.Add(relativePartitionSchemeFilePath); // We want a relative path here.
                                    WriteStatus("File written:  " + relativePartitionSchemeFilePath);
                                    break;
                                }
                                else
                                {
                                    WriteStatus("WARNING: Partition Scheme is NOT scripted.  It was found to be encrypted and cannot be scripted.  (" + t.Name + ")");
                                }
                            }
                        }
                    }

                } //if urns.Count > 0
                string partitionschemeListFilePath = this.BuildScriptFilePath(DatabaseDirectory, PartitionSchemeFile);
                WriteFile(partitionschemeListFilePath, FileCollection);
                string relativepartitionschemeListFilePath = this.BuildRelativeFilePath(partitionschemeListFilePath);
                WriteStatus("File written:  " + relativepartitionschemeListFilePath);
            }
            catch (Exception err)
            {
                WriteStatus(Thread.CurrentThread.ThreadState.ToString());
                if (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    WriteStatus("An error may have been encountered.  Please check the log file for more details.");
                    MMDB.Core.MMDBLogFile.Log(err, "ScriptPartitionScheme");
                    throw new PartitionSchemeScriptingException("A Partition Scheme scripting exception has been found.  Please rePartitionScheme the log for more information.", err);
                }
            }
        }
    }
}
