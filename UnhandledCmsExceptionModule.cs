using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net.Mail;
using System.Web;
using EsccWebTeam.Exceptions.Publishers;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Publishes any unhandled exceptions to the registered publishers
    /// </summary>
    public class UnhandledCmsExceptionModule : IHttpModule
    {
        #region IHttpModule Members

        /// <summary>
        /// Inits the specified app.
        /// </summary>
        /// <param name="app">The app.</param>
        public void Init(HttpApplication app)
        {
            app.Error += new EventHandler(app_Error);
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"/>.
        /// </summary>
        public void Dispose()
        {
        }

        #endregion

        /// <summary>
        /// Handles the Error event of the application.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void app_Error(object sender, EventArgs e)
        {
            var queryString = HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString.ToString());
            var exception = HttpContext.Current.Server.GetLastError().InnerException;

            if (exception.GetType() == typeof(System.Runtime.InteropServices.COMException)
                && exception.Message == "The current user does not have rights to the requested item."
                && queryString.StartsWith("404;", StringComparison.Ordinal)
                && queryString.Contains("?NRMODE=Unpublished&wbc_purpose=Basic&WBCMODE=PresentationUnpublished")
                && HttpContext.Current.Request.UrlReferrer != null)
            {
                // Trap and report the error that occurs only on the public website when web authors link to a URL like
                // http://www.eastsussex.gov.uk/childrenandfamilies/childcare/parentsandcarers/childrencentres/eastbourne/0121C0B3-8CA5-4A44-B845-FC68F61ABDD0.htm?NRMODE=Unpublished&wbc_purpose=Basic&WBCMODE=PresentationUnpublished
                using (var mail = new MailMessage())
                {
                    var securityConfig = ConfigurationManager.GetSection("EsccWebTeam.Cms/SecuritySettings") as NameValueCollection;
                    if (securityConfig == null || String.IsNullOrEmpty(securityConfig["ErrorNotificationEmail"]))
                    {
                        throw new ConfigurationErrorsException("The 'ErrorNotificationEmail' setting in the EsccWebTeam.Cms/SecuritySettings of web.config was not found");
                    }

                    mail.To.Add(securityConfig["ErrorNotificationEmail"]);
                    mail.Subject = "Broken link on www.eastsussex.gov.uk";
                    mail.Body = "There's a broken link on " +
                        HttpContext.Current.Request.UrlReferrer +
                        Environment.NewLine + Environment.NewLine +
                        "It links to " +
                        queryString.Substring(4).Replace(":80", String.Empty);

                    var smtp = new SmtpClient();
                    smtp.Send(mail);
                }

            }
            else
            {
                ExceptionUtility.PublishUnhandledException();
            }
        }
    }
}
