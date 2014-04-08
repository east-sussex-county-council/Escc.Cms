using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Text;
using System.Web;
using Microsoft.ContentManagement.Publishing;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Ensures that the current CMS request uses the correct host
    /// </summary>
    public class EnforceCmsHostModule : IHttpModule
    {
        /// <summary>
        /// Initialises the specified HTTP application.
        /// </summary>
        /// <param name="httpApp">The HTTP application.</param>
        public void Init(HttpApplication httpApp)
        {
            httpApp.PreRequestHandlerExecute += new EventHandler(this.OnPreRequestHandlerExecute);
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"/>.
        /// </summary>
        public void Dispose()
        {
            // Nothing to do.
        }

        /// <summary>
        /// Called before the main request is executed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void OnPreRequestHandlerExecute(object sender, EventArgs e)
        {
            if (CmsUtilities.IsCmsEnabled())
            {
                CheckHostAndRedirect();
            }
        }

        private static void CheckHostAndRedirect()
        {
            var config = ConfigurationManager.GetSection("EsccWebTeam.Cms/EnforceCmsHostModule") as NameValueCollection;
            if (config == null) return;

            // If there's a specific setting for this host
            Uri currentUrl = GetCurrentRequestUrl();
            string currentHost = currentUrl.Host.ToLowerInvariant();
            if (config[currentHost] != null)
            {
                // Change the host to the preferred alternative
                // Note: don't use UriBuilder because that ends up with Safari (v5 desktop and iPad) showing the port number in the URL
                var preferredUri = new StringBuilder();
                preferredUri.Append(currentUrl.Scheme);
                preferredUri.Append("://");
                preferredUri.Append(config[currentHost]);
                if (!currentUrl.IsDefaultPort) preferredUri.Append(":").Append(currentUrl.Port);
                if (currentUrl.AbsolutePath != "/Channels/") preferredUri.Append(currentUrl.AbsolutePath);
                if (!String.IsNullOrEmpty(currentUrl.Query)) preferredUri.Append(currentUrl.Query);
                if (!String.IsNullOrEmpty(currentUrl.Fragment)) preferredUri.Append(currentUrl.Fragment);

                // ...and redirect
                HttpResponse response = HttpContext.Current.Response;
                response.Status = "301 Moved Permanently";
                response.AddHeader("Location", preferredUri.ToString());
                response.End();
            }
        }

        /// <summary>
        /// Gets the current request URL.
        /// </summary>
        /// <returns></returns>
        private static Uri GetCurrentRequestUrl()
        {
            CmsHttpContext cms = CmsHttpContext.Current;
            if (cms != null && cms.Posting != null)
            {
                Uri requestUrl = HttpContext.Current.Request.Url;
                return new Uri(requestUrl.Scheme + "://" + requestUrl.Host + CmsUtilities.CorrectPublishedUrl(cms.Posting.UrlModePublished));
            }
            else return HttpContext.Current.Request.Url;
        }
    }
}
