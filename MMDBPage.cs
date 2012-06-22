/*
	Copyright © 2010 MMDB SOLUTIONS, LLC
	All code contained herein is property of MMDB SOLUTIONS, LLC.  
*/
using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace MMDB.Core
{
    public abstract class MMDBPage : System.Web.UI.Page
    {

        public MMDBPage()
        {
            this.Load += new EventHandler(MMDBPage_Load);
           
        }

        void MMDBPage_Load(object sender, EventArgs e)
        {
            this.CheckSSL();

            this.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        }

        private void CheckSSL()
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ForceSSL"]))
            {
                bool forceSSL;
                if (bool.TryParse(ConfigurationManager.AppSettings["ForceSSL"], out forceSSL))
                {
                    if (forceSSL && !this.Request.IsSecureConnection)
                    {
                        if (this.IsPostBack)
                        {
                            throw new Exception("Postback without SSL when SSL is required");
                        }
                        else
                        {
                            string url = this.Request.Url.ToString();
                            url = url.Replace("http://", "https://");
                            this.Response.Redirect(url);
                        }
                    }
                }
            }
        }

        private string UserName { get; set; }

        public virtual bool ShowHeaderMenu
        {
            get
            {
                bool returnValue = false;
                if (this.Request.IsAuthenticated)
                {
                    returnValue = true;
                }
                return returnValue;
            }
        }

        public string GetStringParameter(string parameterName)
        {
            string returnValue = null;
            if (this.Request.Params.AllKeys.Contains(parameterName, StringComparer.InvariantCultureIgnoreCase))
            {
                returnValue = this.Request.Params[parameterName];
            }
            return returnValue;
        }

        public string GetRequiredStringParameter(string parameterName)
        {
            string returnValue = this.GetStringParameter(parameterName);
            if (string.IsNullOrEmpty(returnValue))
            {
                throw new Exception(string.Format("Missing {0} parameter", returnValue));
            }
            return returnValue;
        }

        public int? GetIntParameter(string parameterName)
        {
            int? returnValue = null;
            if (this.Request.Params.AllKeys.Contains(parameterName, StringComparer.InvariantCultureIgnoreCase))
            {
                string tempString = this.Request.Params[parameterName];
                int tempInt;
                if (!string.IsNullOrEmpty(tempString))
                {
                    if (!int.TryParse(tempString, out tempInt))
                    {
                        throw new Exception(string.Format("Failed to parse integer parameter \"{0}\" for value \"{1}\"", parameterName, tempString));
                    }
                    returnValue = tempInt;
                }
            }
            return returnValue;
        }

        public bool? GetBoolParameter(string parameterName)
        {
            bool? returnValue = null;
            if (this.Request.Params.AllKeys.Contains(parameterName, StringComparer.InvariantCultureIgnoreCase))
            {
                string tempString = this.Request.Params[parameterName];
                bool tempBool;
                if (!string.IsNullOrEmpty(tempString))
                {
                    if (!bool.TryParse(tempString, out tempBool))
                    {
                        throw new Exception(string.Format("Failed to parse bool parameter \"{0}\" for value \"{1}\"", parameterName, tempString));
                    }
                    returnValue = tempBool;
                }
            }
            return returnValue;
        }

        public Guid? GetGuidParameter(string parameterName)
        {
            Guid? returnValue = null;
            if (this.Request.Params.AllKeys.Contains(parameterName, StringComparer.InvariantCultureIgnoreCase))
            {
                string tempString = this.Request.Params[parameterName];
                if (!string.IsNullOrEmpty(tempString))
                {
                    returnValue = new Guid(tempString);
                }
            }
            return returnValue;
        }

        protected Guid GetRequiredGuidParameter(string parameterName)
        {
            Guid? guid = this.GetGuidParameter(parameterName);
            if (!guid.HasValue)
            {
                throw new Exception(string.Format("Missing {0} Parameter", parameterName));
            }
            return guid.Value;
        }

        protected int GetRequiredIntParameter(string parameterName)
        {
            int? value = this.GetIntParameter(parameterName);
            if (!value.HasValue)
            {
                throw new Exception(string.Format("Missing {0} Parameter", parameterName));
            }
            return value.Value;
        }

        private void AddQueryStringParameter(StringBuilder sb, string key, string value)
        {
            if (sb.Length == 0)
            {
                sb.AppendFormat("{0}={1}", this.Server.UrlEncode(key), this.Server.UrlEncode(value));
            }
            else
            {
                sb.AppendFormat("&{0}={1}", this.Server.UrlEncode(key), this.Server.UrlEncode(value));
            }
        }

        protected string GetBackUrl()
        {
            string returnValue = this.GetStringParameter("BackUrl");
            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = returnValue = this.GetStringParameter("ReturnUrl");
            }
            return returnValue;
        }

        protected string GetRequiredBackUrl()
        {
            string returnValue = this.GetStringParameter("BackUrl");
            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = returnValue = this.GetStringParameter("ReturnUrl");

                if (string.IsNullOrEmpty(returnValue))
                {
                    throw new Exception("Missing BackUrl parameter");
                }
            }
            return returnValue;
        }
        public void AddConfirm(Button b, string Text)
        {
            b.Attributes.Add("onclick",
                   "return confirm('" + Text + "');");
        }
        
    }
}
