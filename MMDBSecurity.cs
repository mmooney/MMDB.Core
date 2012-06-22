using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Security;
using System.Security.Principal;
using System.Reflection;
using System.Web.Security;
using System.Web.Profile;
using System.Configuration.Provider;
using System.Data;
using System.Security.Permissions;

namespace MMDB.Core 
{
    public class MMDBSecurity : MMDBCoreBase
    {
        public MMDBSecurity()
            : base()
        {
            //Default Constructor
        }
        public MMDBSecurity(string user, string password) : base()
        {
            Login(user, password);            
        }
        public ProfileBase Profile { get; set; }
        public string FirstName 
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["FirstName"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["FirstName"] = value;
            }
        }
        public string LastName
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["LastName"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["LastName"] = value;
            }
        }
        public string Address1
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["Address1"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["Address1"] = value;
            }
        }
        public string Address2
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["Address2"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["Address2"] = value;
            }
        }
        public string City
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["City"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["City"] = value;
            }
        }
        public string State
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["State"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["State"] = value;
            }
        }
        public string PostalCode
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["PostalCode"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["PostalCode"] = value;
            }
        }
        public string HomePhone
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["HomePhone"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["HomePhone"] = value;
            }
        }
        public string WorkPhone
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["WorkPhone"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["WorkPhone"] = value;
            }
        }
        public string CellPhone
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["CellPhone"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["CellPhone"] = value;
            }
        }
        public string AlternateEmail
        {
            get
            {
                return Profile.GetProfileGroup("Demographics")["AlternateEmail"].ToString();
            }
            set
            {
                Profile.GetProfileGroup("Demographics")["AlternateEMail"] = value;
            }
        }
        public string GetProfileValue(string Group, string Key)
        {
            return Profile.GetProfileGroup(Group)[Key].ToString();
        }
        public string GetProfileValue(string Key)
        {
            return Profile.GetPropertyValue(Key).ToString();
        }
        public void SetProfileValue(string Group, string Key, string Value)
        {
            Profile.GetProfileGroup(Group).SetPropertyValue(Key, Value);
            Profile.Save();
        }
        public void SetProfileValue(string Key, string Value)
        {
            Profile.SetPropertyValue(Key, Value);
            Profile.Save();
        }
        public bool ChangeEmailAddress(string newEmailAddress)
        {
            try
            {
                MembershipUser user = Membership.GetUser(Profile.UserName);
                user.Email = newEmailAddress;
                Membership.UpdateUser(user);
                return true;
            }
            catch (Exception err)
            {
                MMDBExceptionHandler.HandleException(err);
                return false;
            }
        }
        public bool CheckRole(string Username, string Rolename)
        {
            try
            {
                return Roles.IsUserInRole(Username, Rolename);
            }
            catch (ArgumentException /*err*/)
            {
                return false;
            }
            catch (Exception err)
            {
                MMDBExceptionHandler.HandleException(err);
                return false;
            }
        }
        
        [PrincipalPermission(SecurityAction.Demand, Role = "MMDBAdministrator")]
        [PrincipalPermission(SecurityAction.Demand, Role = "Admin")]
        public bool AddUserToRole(string Username, string Rolename)
        {
            if (!Roles.RoleExists(Rolename))
            {
                Roles.CreateRole(Rolename);
            }
            try
            {
                Roles.AddUserToRole(Username, Rolename);
                return true;
            }
            catch (Exception err)
            {
                MMDBExceptionHandler.HandleException(err);
                return false;
            }

        }
        
        [PrincipalPermission(SecurityAction.Demand, Role = "MMDBAdministrator")]
        [PrincipalPermission(SecurityAction.Demand, Role = "Admin")]
        public bool RemoveUserFromRole(string Username, string Rolename)
        {
            try
            {
                Roles.RemoveUserFromRole(Username, Rolename);
                return true;
            }
            catch (Exception err)
            {
                MMDBExceptionHandler.HandleException(err);
                return false;
            }
        }
        
        public bool IsAuthorized(string Username, string rule)
        {
            try
            {
                string[] roles = Roles.GetRolesForUser(Username);
                IPrincipal principal = new GenericPrincipal(new GenericIdentity(Username), roles);
                IAuthorizationProvider ruleProvider = AuthorizationFactory.GetAuthorizationProvider("RuleProvider");

                return ruleProvider.Authorize(principal, rule);
            }
            catch (Exception err)
            {
                MMDBLogFile.Log(err);
                return false;
            }
        }
        public bool Login(string Username, string Password)
        {
            bool IsAuthenticated = false;

            try
            {
                IsAuthenticated = Membership.ValidateUser(Username, Password);
                Profile = ProfileBase.Create(Username, IsAuthenticated);
            }
            catch(Exception err)
            {
                MMDBLogFile.Log(err);
                return false;
            }
            return true;
        }

        [PrincipalPermission(SecurityAction.Demand, Role = "MMDBAdministrator")]
        [PrincipalPermission(SecurityAction.Demand, Role = "Admin")]
        public bool CreateUser(string Username, string Password, string Email, string Question, String Answer, out MembershipCreateStatus Status)
        {
            MembershipCreateStatus outStatus = MembershipCreateStatus.ProviderError;
            try
            {
                Membership.CreateUser(Username, Password, Email, Question, Answer, true, out outStatus);
                Profile = ProfileBase.Create(Username, true);
            }
            catch(Exception err)
            {
                MMDBLogFile.Log(err);
                Status = outStatus;
                return false;
            }
            Status = outStatus;
            return true;        
        }

        [PrincipalPermission(SecurityAction.Demand, Role = "MMDBAdministrator")]
        public bool AddRole(string rolename)
        {
            try
            {
                if (!Roles.RoleExists(rolename))
                {
                    Roles.CreateRole(rolename);
                }
            }
            catch(Exception err)
            {
                MMDBLogFile.Log(err);
                return false;
            }
            return true;
        }
        [PrincipalPermission(SecurityAction.Demand, Role = "MMDBAdministrator")]
        public bool RemoveRole(string rolename)
        {
            try
            {
                if (Roles.RoleExists(rolename))
                {
                    Roles.DeleteRole(rolename);
                }
            }
            catch(Exception err)
            {
                MMDBLogFile.Log(err);
                return false;
            }
            return true;        
        }
        [PrincipalPermission(SecurityAction.Demand, Role = "MMDBAdministrator")]
        public bool DeleteUser(string username)
        {
            try
            {
                Membership.DeleteUser(username);
            }
            catch(Exception err)
            {
                MMDBLogFile.Log(err);
                return false;
            }
            return true;        }
        public bool SaveProfile()
        {
            try
            {
                Profile.Save();
            }
            catch(Exception err)
            {
                MMDBLogFile.Log(err);
                return false;
            }
            return true;
        }

        public DateTime GetLastLoginDateTime(string username)
        {
            MembershipUser u = Membership.GetUser(username);
            return u.LastLoginDate.ToUniversalTime();
        }
        public DataSet GetAllUsers(string RoleName)
        {
            DataSet dsMembership = new DataSet("Membership");
            DataTable dtUsers = dsMembership.Tables.Add("Users");
            dtUsers.Columns.Add("IsOnline", Type.GetType("System.Boolean"));
            dtUsers.Columns.Add("UserName", Type.GetType("System.String"));
            dtUsers.Columns.Add("PasswordQuestion", Type.GetType("System.String"));
            dtUsers.Columns.Add("IsLockedOut", Type.GetType("System.Boolean"));
            dtUsers.Columns.Add("Email", Type.GetType("System.String"));
            dtUsers.Columns.Add("LastLoginDate", Type.GetType("System.DateTime"));
            dtUsers.Columns.Add("CreationDate", Type.GetType("System.DateTime"));
            dtUsers.Columns.Add("DisplayValue", Type.GetType("System.String"));
            MembershipUserCollection mu = Membership.GetAllUsers();
            //Add blank row.
            DataRow r;
            r = dtUsers.NewRow();
            r["Username"] = "";
            r["DisplayValue"] = "";
            dtUsers.Rows.Add(r);
            foreach (MembershipUser u in mu)
            {
                if ((CheckRole(u.UserName, RoleName) || RoleName == String.Empty) && u.UserName != "MMDBAdministrator" )
                {

                    ProfileBase p = ProfileBase.Create(u.UserName, true);
                    r = dtUsers.NewRow();
                    r["IsOnline"] = u.IsOnline;
                    r["UserName"] = u.UserName;
                    r["PasswordQuestion"] = u.PasswordQuestion;
                    r["IsLockedOut"] = u.IsLockedOut;
                    r["Email"] = u.Email;
                    r["CreationDate"] = u.CreationDate;
                    r["LastLoginDate"] = u.LastLoginDate;
                    r["DisplayValue"] = u.UserName
                        + "(" + p.GetProfileGroup("Demographics")["FirstName"].ToString()
                        + " "
                        + p.GetProfileGroup("Demographics")["LastName"].ToString()
                        + ")-" + u.Email;
                    dtUsers.Rows.Add(r);
                }
            }
            return dsMembership;
        }
        [PrincipalPermission(SecurityAction.Demand, Role = "MMDBAdministrator")]
        public DataSet GetAllUsers()
        {
            return GetAllUsers(String.Empty);
        }
        public DataSet GetAllRoles(string username)
        {
            DataSet dsMembership = new DataSet("Membership");
            DataTable dtUsers = dsMembership.Tables.Add("Roles");
            dtUsers.Columns.Add("Rolename", Type.GetType("System.String"));
            
            string[] RoleList;
            
            if (username == String.Empty)
            {
                RoleList = Roles.GetAllRoles();
            }
            else
            {
                RoleList = Roles.GetRolesForUser(username);
            }
            //Add blank row.
            DataRow r;
            //r = dtUsers.NewRow();
            //r["Rolename"] = "";
            //dtUsers.Rows.Add(r);

            foreach (string s in RoleList)
            {
                r = dtUsers.NewRow();
                r["Rolename"] = s;
                dtUsers.Rows.Add(r);
            }
            return dsMembership;
        }
        public DataSet GetAllRoles()
        {
            return GetAllRoles(String.Empty);
        }
    }

}
