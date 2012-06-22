using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Xml;
using System.Security.Cryptography;

namespace MMDB.Core
{
    public class MMDBLicenser
    {
        /// <summary>
        /// Enumerates the type of the license file may hold one of the following values:
        /// Demo - A demonstration version. Computer ID is not checked
        /// NodeLocked - A license for specific node
        /// </summary>
        public enum LicenseType {
            Demo,
            NodeLocked
        } ;

        /// <summary>
        /// Holds information about a single feature:
        /// A feature is an application part that is licensed. for Example, A flight simulator
        /// without dogfights will be featured as "BASIC" while the dogfight capability will be 
        /// featured as "DOG-FIGHTS"
        /// featureName - The feature's name
        /// timeDepend - is the feature expires
        /// expiration - The feture expiration time
        /// </summary>
        public struct FeatureInfo
        {
            public string featureName;
            public bool timeDepend;
            public DateTime expiration;
            public int maxCount; // Ignored - for future use
        };

        /// <summary>
        /// Information for the whole license
        /// </summary>
        public struct LicenseInfo
        {
            public LicenseType kind;
            public string computerID;
            public string passCode;
            public FeatureInfo[] features;
        };

        /// <summary>
        /// Returns the computer identification string
        /// </summary>
        /// <returns>Computer ID String</returns>
        /// <remarks>
        /// First, this method checks if the computer has a network adapter. If so, it will
        /// extract the physical address of the first network interface.
        /// In case the computer does not a network adapter - it will use the disk label as the 
        /// computer identification
        /// </remarks>
        public static string GetComputerId()
        {
            string result = "";

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                // look for computer network mac-address
                NetworkInterface[] ifaces = NetworkInterface.GetAllNetworkInterfaces();
                PhysicalAddress address = ifaces[0].GetPhysicalAddress();
                byte[] byteAddr = address.GetAddressBytes();
                // Convert it to hex digits separated with "-" sign
                for (int i = 0; i < byteAddr.Length; i++)
                {
                    result += byteAddr[i].ToString("X2");
                    if (i != byteAddr.Length - 1)
                    {
                        result += "-";
                    }
                }
            }
            else
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.DriveType == DriveType.Fixed)
                    {
                        result = drive.VolumeLabel;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a feature license is valid
        /// </summary>
        /// <param name="licensePath">Full path to license file</param>
        /// <param name="featureName">Name of feature to check</param>
        /// <param name="passCode">Passcode - A token between the client and the server</param>
        /// <param name="bThrow">Throw exception in case of failure</param>
        /// <returns></returns>
        public bool IsValid(string licensePath, string featureName, string passCode, bool bThrow)
        {
            string licenseSignature;
            LicenseInfo licenseInformation = GetLicenseFromFile(licensePath, passCode, out licenseSignature);
            licenseInformation.computerID = MMDBLicenser.GetComputerId();
            string signature = CreateSignature(licenseInformation);
            if (signature != licenseSignature)
            {
                if (bThrow)
                {
                    throw (new LicenseException("License information does not match it's signature"));
                }
                return false;
            }

            if (licenseInformation.kind == LicenseType.NodeLocked)
            {
                if (licenseInformation.computerID != GetComputerId())
                {
                    if (bThrow)
                    {
                        throw (new LicenseException("License is customed to a different computer"));
                    }
                    return false;
                }
            }

            foreach (FeatureInfo feature in licenseInformation.features)
            {
                if (feature.featureName == featureName)
                {
                    if (feature.timeDepend)
                    {
                        if (DateTime.Now > feature.expiration)
                        {
                            if (bThrow)
                            {
                                throw (new LicenseException("Feature has been expired"));
                            }
                            return false;
                        }
                    }
                    return true;
                }
            }

            if (bThrow)
            {
                throw (new LicenseException("Feature not found"));
            }
            return false;
        }

        LicenseInfo GetLicenseFromFile(string licensePath, string passCode, out string signature)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(licensePath);
            LicenseInfo licenseInformation;
            licenseInformation.kind =
                (LicenseType)Enum.Parse(
                    typeof(LicenseType),
                    xdoc.DocumentElement["LicenseType"].InnerText,
                    true);
            licenseInformation.computerID = xdoc.DocumentElement["ComputerId"].InnerText;
            licenseInformation.passCode = passCode;
            int nFeatures = xdoc.DocumentElement["Features"].GetElementsByTagName("Feature").Count;
            licenseInformation.features = new FeatureInfo[nFeatures];
            XmlElement elem = (XmlElement)xdoc.DocumentElement["Features"].FirstChild;
            for (int i = 0; i < nFeatures; i++)
            {
                licenseInformation.features[i].featureName = elem.Attributes["Name"].Value;
                licenseInformation.features[i].timeDepend =
                    XmlConvert.ToBoolean(elem.Attributes["IsTimeDepended"].Value);
                if (licenseInformation.features[i].timeDepend)
                {
                    licenseInformation.features[i].expiration =
                        XmlConvert.ToDateTime(
                        elem.Attributes["Expiration"].Value, XmlDateTimeSerializationMode.Local);
                }
                elem = (XmlElement)elem.NextSibling;
            }
            signature = xdoc.DocumentElement["Signature"].InnerText;
            return licenseInformation;
        }

        public string CreateSignature(LicenseInfo licenseInformation)
        {
            SHA384Managed shaM = new SHA384Managed();
            byte[] data;

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)licenseInformation.kind);
            if (licenseInformation.kind == LicenseType.NodeLocked)
            {
                bw.Write(licenseInformation.computerID);
            }
            bw.Write(licenseInformation.passCode);
            foreach (FeatureInfo feature in licenseInformation.features)
            {
                bw.Write(feature.featureName);
                bw.Write(feature.timeDepend);
                if (feature.timeDepend)
                {
                    bw.Write(feature.expiration.ToString());
                }
                bw.Write(feature.maxCount);
            }
            int nLen = (int)ms.Position + 1;
            bw.Close();
            ms.Close();
            data = ms.GetBuffer();

            data = shaM.ComputeHash(data, 0, nLen);

            string result = "";
            foreach (byte dbyte in data)
            {
                result += dbyte.ToString("X2");
            }
            return result;
        }


        public class LicenseException : System.Exception
        {
            public LicenseException(string message)
                : base(message)
            {
            }
        }
    }
}
