using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MMDB.Core;
using System.Configuration;
using System.Deployment.Application;
using System.IO;

namespace MMDB.Core
{
    public class MMDBLicense
    {
        string _ComputerID = "";
        public string ComputerID
        {
            get
            {
                MMDBEncryption.CypherKey = "MMD8S0lution3";
                return MMDBEncryption.Encrypt(_ComputerID, true);
            }
            set
            {
                MMDBEncryption.CypherKey = "MMD8S0lution3";
                _ComputerID = MMDBEncryption.Decrypt(value, true);
            }
        }
        public string FeatureName { get; set; }
        const string Passcode = "MMDBSolutionsLLC3333";

        public bool IsValid()
        {
            bool returnvalue = false;
            MMDBLicenser l = new MMDBLicenser();
            try
            {
                string LicensePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\" +
                    ConfigurationSettings.AppSettings["LicenseFile"].ToString();
                returnvalue = l.IsValid(LicensePath, FeatureName, Passcode, false);
            }
            catch (Exception err)
            {
                MMDBLogFile.Log(err.Message);
            }
            return returnvalue;
        }
        public bool CheckDemoLicense(int days)
        {
            MMDB.Core.MMDBEncryption.CypherKey = Passcode;
            string key = "";
            key = MMDB.Core.MMDBEncryption.Encrypt("MMDB LICENSE DEMO LICENSE KEY", true);
            string DemoLicense = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.ToString().Replace(@"file:///", "").Replace(@"/", @"\")) + @"\";
            if(!File.Exists(DemoLicense+@"..\Demo.key") && File.Exists(DemoLicense+@".\Scripts\Demo.key"))
            {
                File.Copy(DemoLicense+@".\Scripts\Demo.key", DemoLicense+@"..\Demo.key");
                File.Delete(DemoLicense+@".\Scripts\Demo.key");
            }
            DemoLicense+=@"..\Demo.key";
            if (!File.Exists(DemoLicense))
            {
                return false;
            }
            List<string> lines = new List<string>();

            using (StreamReader r = new StreamReader(DemoLicense))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            if (MMDBEncryption.Decrypt(lines[0], true) == "MMDB LICENSE DEMO LICENSE KEY")
            {
                //Delete file and rewrite.
                File.Delete(DemoLicense);
                string[] datetime = new string[1];
                datetime[0] = MMDB.Core.MMDBEncryption.Encrypt(DateTime.Now.Date.ToString(), true);
                File.WriteAllLines(DemoLicense, datetime);
                return true;
            }
            else
            {
                //Found file.  Read it.
                string startdate = MMDBEncryption.Decrypt(lines[0], true);
                DateTime dt = Convert.ToDateTime(startdate).AddDays(days);
                if (dt > DateTime.Now)
                {
                    return true;
                }
                return false;
            }

        }
    }
}
