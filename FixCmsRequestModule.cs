using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Web;
using EsccWebTeam.Data.Web;
using Microsoft.ContentManagement.Publishing;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Module to include in CMS projects which tries to sort out any odd requests and return the correct or preferred page instead of an error
    /// </summary>
    /// <remarks>
    /// <para>To ensure that requests are served using the appropriate protocol (HTTP or HTTPS), add the following setting to web.config.
    /// The decision on whether to use SSL is made based on the Important Channel property or (for backward compatibility) the presence or 
    /// absence of a custom channel property, which must be named "SSL".</para>
    /// <example>
    /// <code>
    /// &lt;configuration&gt;
    ///
    ///    &lt;configSections&gt;
    ///       &lt;sectionGroup name=&quot;EsccWebTeam.Cms&quot;&gt;
    ///          &lt;section name=&quot;SecuritySettings&quot; type=&quot;System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089&quot; /&gt;
    ///       &lt;/sectionGroup&gt;
    ///    &lt;/configSections&gt;
    ///
    ///    &lt;EsccWebTeam.Cms&gt;
    ///       &lt;SecuritySettings&gt;
    ///          &lt;add key=&quot;EnableRedirectToHttp&quot; value=&quot;true&quot; /&gt;
    ///          &lt;add key=&quot;EnableRedirectToHttps&quot; value=&quot;true&quot; /&gt;
    ///       &lt;/SecuritySettings&gt;
    ///    &lt;/EsccWebTeam.Cms&gt;
    ///
    /// &lt;/configuration&gt;
    /// </code>
    /// </example>
    /// </remarks>
    public class FixCmsRequestModule : IHttpModule
    {
        /// <summary>
        /// You will need to configure this module in the web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpModule Members

        public void Dispose()
        {
            //clean-up code here.
        }

        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpApplication"/> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application</param>
        public void Init(HttpApplication context)
        {
            context.PreRequestHandlerExecute += new EventHandler(this.OnPreRequestHandlerExecute);
        }

        #endregion

        /// <summary>
        /// Called before the main request is executed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void OnPreRequestHandlerExecute(object sender, EventArgs e)
        {
            // support machines without CMS installed
            if (!CmsUtilities.IsCmsEnabled()) return;

            FixIncorrectProtocol();
            FixPublishedUrl();
            UseCanonicalUrl();
            FixEncodingErrors();
        }


        /// <summary>
        /// Redirect to or from the HTTPS protocol depending on whether a channel is an Important Channel or has a custom property named "SSL"
        /// </summary>
        /// <remarks>
        /// If a channel has a custom property named "SSL", and redirecting is enabled in web.config, redirect the current request to use
        /// SSL if it doesn't already. Similarly if the "SSL" property is not present but the current request uses SSL, redirect to HTTP.
        /// Allow this to be controlled by web.config to make it easier to test pages on servers where no SSL certificate is installed.
        /// Use the Important Channel setting as a better alternative to the "SSL" property, because editors can set it.
        /// </remarks>
        private void FixIncorrectProtocol()
        {
            Channel ch = CmsUtilities.GetCurrentChannel();
            if (ch != null)
            {
                NameValueCollection securityConfig = ConfigurationManager.GetSection("EsccWebTeam.Cms/SecuritySettings") as NameValueCollection;
                bool redirectToHttp = (securityConfig != null && securityConfig["EnableRedirectToHttp"] != null) ? Boolean.Parse(securityConfig["EnableRedirectToHttp"]) : false;
                bool redirectToHttps = (securityConfig != null && securityConfig["EnableRedirectToHttps"] != null) ? Boolean.Parse(securityConfig["EnableRedirectToHttps"]) : false;

                if (redirectToHttp || redirectToHttps)
                {
                    var sslChannel = CmsUtilities.IsSecureChannel(ch);
                    var context = HttpContext.Current;
                    string urlWithoutScheme = context.Request.Url.ToString().Substring(context.Request.Url.Scheme.Length);

                    if (CmsHttpContext.Current.Posting != null && CmsHttpContext.Current.Mode == PublishingMode.Published)
                    {
                        // If this is a CMS posting it should redirect based on the published URL, not the ugly URL,
                        urlWithoutScheme = "://" + context.Request.Url.Host + CmsUtilities.CorrectPublishedUrl(CmsHttpContext.Current.Posting.UrlModePublished);
                    }

                    // Ensure there are no new lines in the URL - have seen this from the Yandex spider
                    urlWithoutScheme = urlWithoutScheme.Replace(Environment.NewLine, String.Empty);

                    if (redirectToHttps && sslChannel && context.Request.Url.Scheme != Uri.UriSchemeHttps)
                    {
                        Http.Status301MovedPermanently(new Uri(Uri.UriSchemeHttps + urlWithoutScheme));
                    }
                    else if (redirectToHttp && !sslChannel && context.Request.Url.Scheme == Uri.UriSchemeHttps)
                    {
                        Http.Status301MovedPermanently(new Uri(Uri.UriSchemeHttp + urlWithoutScheme));
                    }
                }
            }
        }


        /// <summary>
        /// After switching from Edit mode to Published mode in CMS, you get an ugly URL. This detects it and shows the correct one.
        /// </summary>
        public static void FixPublishedUrl()
        {
            // check that URL is a problem before creating CMS context
            HttpContext ctx = HttpContext.Current;
            if (ctx.Request.QueryString["NRORIGINALURL"] != null &&
                        ctx.Request.QueryString["NRORIGINALURL"].StartsWith("/NR/exeres"))
            {
                // get a CMS context
                CmsHttpContext cms = CmsHttpContext.Current;

                //  correct the url after switch to live
                if (cms != null && cms.Posting != null && cms.Mode == PublishingMode.Published)
                {
                    // Send redirection headers to inform user agent of permanent change of URL
                    ctx.Response.Status = "301 Moved Permanently";
                    ctx.Response.AddHeader("Location", CmsUtilities.CorrectPublishedUrl(cms.Posting.UrlModePublished));
                    ctx.Response.End();
                }
            }
        }

        /// <summary>
        /// Search engines and Google Analytics can see the default page for each channel either with or without its
        /// page name. Correct to a preferred version to avoid being seen as two separate pages.
        /// </summary>
        public static void UseCanonicalUrl()
        {
            // check that URL is a CMS page before creating a CMS context
            HttpContext ctx = HttpContext.Current;
            if (ctx.Request.QueryString["NRORIGINALURL"] == null) return;

            // get a CMS context
            CmsHttpContext cms = CmsHttpContext.Current;

            //  only correct the url in published mode
            if (cms == null || cms.Posting == null || cms.Mode != PublishingMode.Published) return;

            // if this is the channel home page, redirect to he posting URL instead of the plain channel URL.
            var defaultPostingInChannel = CmsUtilities.DefaultPostingInChannel(cms.Channel);

            // Check that this is the channel home page
            if (defaultPostingInChannel == null || defaultPostingInChannel.Guid != cms.Posting.Guid) return;

            // If it was already requested with the posting name, no need to redirect
            var requestUri = new Uri(ctx.Request.QueryString["NRORIGINALURL"], UriKind.RelativeOrAbsolute);
            var absoluteRequest = Iri.MakeAbsolute(requestUri, ctx.Request.Url, true);
            if (absoluteRequest.AbsolutePath.EndsWith(Path.GetFileName(cms.Posting.Url), StringComparison.OrdinalIgnoreCase)) return;

            // Make an absolute version of the posting URI, excluding the querystring which has all the CMS parameters
            // Actually it would be neater to use the channel URL, but that would lead to thousands of 
            // broken link reports for co-ords as their "default.htm" links started pointing to 301 redirects.
            string postingUrl = CmsUtilities.CorrectPublishedUrl(cms.Posting.Url);

            // But we might've added a querystring other than the CMS one which needs to be preserved
            var hasQueryString = false;
            foreach (string key in ctx.Request.QueryString.AllKeys)
            {
                if (key != "NRORIGINALURL" && key != "NRMODE" && key != "NRNODEGUID" && key != "NRCACHEHINT")
                {
                    postingUrl += (hasQueryString) ? "&" : "?";
                    postingUrl += key + "=" + ctx.Request.QueryString[key];
                    hasQueryString = true;
                }
            }

            var absoluteUrl = Iri.MakeAbsolute(new Uri(postingUrl, UriKind.RelativeOrAbsolute), ctx.Request.Url, true);

            // This is one we need to redirect
            Http.Status301MovedPermanently(absoluteUrl);
        }


        /// <summary>
        /// Redirect if the encoding of ampersands in the request URL gets messed up - not sure how it happens
        /// </summary>
        /// <example>
        /// Example of URL being fixed: http://localhost/educationandlearning/Templates/Schools/School.aspx?NRMODE=Published&amp;NRNODEGUID={54BA2A05-6FDD-455F-BCEB-94CC959CB002}&amp;NRORIGINALURL=/educationandlearning/schools/secondary/chailey8454042.htm&amp;NRCACHEHINT=NoModifyGuest
        /// </example>
        private static void FixEncodingErrors()
        {
            HttpContext ctx = HttpContext.Current;
            HttpRequest r = ctx.Request;
            if (!String.IsNullOrEmpty(r.QueryString["amp;NRNODEGUID"]) &&
                !String.IsNullOrEmpty(r.QueryString["amp;NRORIGINALURL"]) &&
                !String.IsNullOrEmpty(r.QueryString["amp;NRCACHEHINT"]))
            {
                // Get a CMS context to look up the published URL of the posting
                CmsHttpContext cms = CmsHttpContext.Current;
                if (cms != null)
                {
                    Posting p = cms.Searches.GetByGuid(r.QueryString["amp;NRNODEGUID"]) as Posting;
                    if (p != null)
                    {
                        // Send redirection headers to inform user agent of permanent change of URL
                        ctx.Response.Status = "301 Moved Permanently";
                        ctx.Response.AddHeader("Location", CmsUtilities.CorrectPublishedUrl(p.UrlModePublished));
                        ctx.Response.End();
                    }
                }
            }
        }
    }
}
