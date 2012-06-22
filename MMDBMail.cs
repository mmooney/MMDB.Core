using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net;
using System.Configuration;

namespace MMDB.Core
{
    public class MMDBMail
    {
        public string ToAddress { get; set; }
        public string FromAddress { get; set; }
        public string Subject { get; set; }
        public string MessageText { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public bool Encrypted { get; set; }
        public bool EnableSSL { get; set; }
        public string AuthenticationType { get; set; }
        public int Port { get; set; }

        public MMDBMail()
        {
        }

        public MMDBMail(string To, string From, string SubjectText, string MessageTxt)
        {
            Encrypted = false;
            Port = 25;
            AuthenticationType = "Basic";
            EnableSSL = false;
            ToAddress = To;
            FromAddress = From;
            Subject = SubjectText;
            MessageText = MessageTxt;
            //Get values out of the config
        }
        public void SetServer()
        {
            if (!Encrypted)
            {
                _SMTPUsername = ConfigurationSettings.AppSettings["SMTPUsername"].ToString();
                _SMTPPassword = ConfigurationSettings.AppSettings["SMTPPassword"].ToString();
                _SMTPServer = ConfigurationSettings.AppSettings["SMTPServer"].ToString();
            }
            else
            {
                MMDB.Core.MMDBEncryption.CypherKey = "MMD8S0lution3";
                _SMTPUsername = MMDBEncryption.Decrypt(ConfigurationSettings.AppSettings["SMTPUsername"].ToString(), true);
                _SMTPPassword = MMDBEncryption.Decrypt(ConfigurationSettings.AppSettings["SMTPPassword"].ToString(), true);
                _SMTPServer = MMDBEncryption.Decrypt(ConfigurationSettings.AppSettings["SMTPServer"].ToString(), true);
            }
        }
        private string _SMTPUsername = "";
        private string _SMTPPassword = "";
        private string _SMTPServer = "";
        public bool SendMail()
        {
                MailMessage message = new MailMessage(FromAddress, ToAddress, Subject, MessageText);
                SetServer();
				SmtpClient client = new SmtpClient();
				client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = EnableSSL;
                client.Port = Port;
                
                if (Encrypted)
                {
                }
                message.CC.Add(CC);
                message.Bcc.Add(BCC);
				client.Host = _SMTPServer;
				NetworkCredential credential = new NetworkCredential(_SMTPUsername,_SMTPPassword);
				credential.GetCredential(client.Host, Port, AuthenticationType);
				client.UseDefaultCredentials = false;
				client.Credentials = credential;
                message.ReplyTo = message.From;
				try 
				{
					client.Send(message);
				}
				catch(Exception err)
				{
                    MMDBExceptionHandler.HandleException(err);
                    throw(err);
                }
            return true;
        }
    }

}
