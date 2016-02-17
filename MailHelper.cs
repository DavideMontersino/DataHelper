using System;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.Web.UI;
using Nts.DataHelper.CustomWeb;
using System.Web;
using System.Net;
using System.Web.Script.Serialization;

namespace Nts.DataHelper
{
    public class MailHelper
    {
        public static void Log(string msg)
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\EmailErrors\\");
           File.AppendAllText(dir + "log.txt", "\r\n" + CurDateTime.Now().Value + ": " + msg + "\r\n");
        }
        //the one to use
        public static void SendMail(string from, string to, CustomUserControl control, string subject)
        {
            var sw = new StringWriter();
            control.ForceLoad();
            control.RenderControl(new HtmlTextWriter(sw));

            SendMail(from, to,

                sw.ToString()
            , subject);
        }

        public static string GetUrl(Uri emailUrl, HttpContext context)
        {
            var request = WebRequest.Create(emailUrl);
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\EmailErrors\\");
            System.IO.File.AppendAllText(dir + "log.txt", emailUrl.ToString() + '\n');
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var dataStream = response.GetResponseStream();
                
                
                var reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                return responseFromServer;
            }
            catch (Exception e )
            {
                System.IO.File.AppendAllText(dir + "log.txt", "error retrieving uri" + e.Message + '\n');
                return "";
            }


        }
        public static Uri GetUri(string pageUrl, HttpContext context)
        {
            Uri ret;
            Uri.TryCreate(
                context.Request.Url.Scheme + System.Uri.SchemeDelimiter + context.Request.Url.Host +
                ((context.Request.Url.IsDefaultPort ? "" : ":" + context.Request.Url.Port) + 
                 context.Request.ApplicationPath +
                pageUrl),
                UriKind.RelativeOrAbsolute,
                out ret
                );
            return ret;
        }
        public static void SendMail(string from, string to, string html, string subject)
        {
            MailHelper.Log("starting to send email to " + to );
            //string from = ConfigurationManager.AppSettings[ConfigOptions.EmailFrom];
            var bcc = ConfigurationManager.AppSettings["EmailBcc"];


            MailMessage mm;

            if (bool.Parse(ConfigurationManager.AppSettings["TestMode"]))
            {

                subject = "TEST email to " + to + ": " + subject.Trim().Replace("\n", "").Replace("\r", "");
                to = bcc;
                mm = new MailMessage(from, to, subject, html);

            }
            else
            {
                mm = new MailMessage(from, to, subject, html);
                mm.Bcc.Add(bcc);
            }

            mm.IsBodyHtml = true;
            var logMsg = CurDateTime.Now().Value + ": sending mail '" + mm.Subject + "' to " + to;
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\EmailErrors\\");
            try
            {
                var client = new SmtpClient();
                client.Send(mm);
                logMsg += "[success]\n";
            }
            
            catch (Exception e)
            {
                var smtpException = e as SmtpException;

                if (smtpException != null)
                {
                    logMsg += "[error (" + e.GetType().Name + ")]: \n" + smtpException.Message +" code:" + smtpException.StatusCode +"\n";
                  //  var json = new JavaScriptSerializer().Serialize(smtpException.Data);
                  //  logMsg += "\n\n" + json + "\n\n";
                }
                else {
                    logMsg += "[error (" + e.GetType().Name + ")]: \n" + e.Message + "\n";
                }
                

                var client2 = new SmtpClient();

                client2.PickupDirectoryLocation = dir;
                client2.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                mm.Headers["Oggetto"] = mm.Subject;
                Directory.CreateDirectory(dir);

                client2.Send(mm);
                mm.Subject += "(Email Error)";

            }
            System.IO.File.AppendAllText(dir + "log.txt", logMsg);
        }
    }
}
