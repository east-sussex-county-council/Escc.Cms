using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Xml;
using eastsussexgovuk.webservices.EgmsWebMetadata;
using eastsussexgovuk.webservices.TextXhtml.HouseStyle;
using Escc.TextStatistics;
using EsccWebTeam.Cms.Placeholders;
using EsccWebTeam.Data.Web;
using EsccWebTeam.Data.Xml;
using EsccWebTeam.HouseStyle;
using EsccWebTeam.NavigationControls;
using Microsoft.ApplicationBlocks.Data;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Events;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Business logic functions for working with MSCMS
    /// </summary>
    public static class CmsUtilities
    {
        #region Getting a CMS context

        /// <summary>
        /// Determines whether CMS is enabled and it's therefore OK to access the CMS API.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if CMS is enabled; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsCmsEnabled()
        {
            // Look in config. If CMS not explicitly enabled, it's disabled. Can also be explicitly disabled to allow override of higher-level web.config.
            var config = ConfigurationManager.GetSection("EsccWebTeam.Cms/GeneralSettings") as NameValueCollection;
            if (config == null || String.IsNullOrEmpty(config["CmsEnabled"]) || config["CmsEnabled"].ToLowerInvariant() == "false") return false;

            // If there's a value which isn't "false", it should be machine names to enable on
            var machines = new List<string>(config["CmsEnabled"].Split(';'));
            for (var i = 0; i < machines.Count; i++) machines[i] = machines[i].ToUpperInvariant();
            return (machines.Contains(Environment.MachineName.ToUpperInvariant()));
        }

        /// <summary>
        /// Determines whether HTTP caching is enabled for CMS pages 
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if CMS caching is enabled; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsHttpCachingEnabled()
        {
            // Look in config. If caching not explicitly enabled, it's disabled. Can also be explicitly disabled to allow override of higher-level web.config.
            var config = ConfigurationManager.GetSection("EsccWebTeam.Cms/GeneralSettings") as NameValueCollection;
            if (config == null || String.IsNullOrEmpty(config["HttpCachingEnabled"]) || config["HttpCachingEnabled"].ToLowerInvariant() != "true") return false;

            return true;
        }

        /// <summary>
        /// Gets a CmsApplicationContext using the admin login details in web.config
        /// </summary>
        /// <returns></returns>
        public static CmsApplicationContext GetAdminContext()
        {
            if (CmsHttpContext.Current != null)
            {
                return CmsUtilities.GetAdminContext(CmsHttpContext.Current.Mode);
            }
            else
            {
                return CmsUtilities.GetAdminContext(PublishingMode.Published);
            }
        }

        /// <summary>
        /// Gets a CmsApplicationContext using the admin login details in web.config
        /// </summary>
        /// <param name="mode">The publishing mode to open the context with</param>
        /// <returns></returns>
        public static CmsApplicationContext GetAdminContext(PublishingMode mode)
        {
            // Get a CMS context using web.config settings
            CmsApplicationContext cms = new CmsApplicationContext();
            cms.AuthenticateAsUser(String.Format("WinNT://{0}/{1}", ConfigurationManager.AppSettings["CmsAdminDomain"], ConfigurationManager.AppSettings["CmsAdminUser"]), ConfigurationManager.AppSettings["CmsAdminPassword"], mode);
            return cms;
        }

        /// <summary>
        /// Shortcut to check whether page is in either of CMS's editing modes
        /// </summary>
        /// <returns>True in new page or edit page mode; false otherwise</returns>
        public static bool IsEditing
        {
            get { return (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew || WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit); }
        }

        /// <summary>
        /// Shortcut to check whether page is in any of CMS's preview modes
        /// </summary>
        /// <returns>True in editing preview, unpublished preview or template preview; false otherwise</returns>
        public static bool IsPreviewing
        {
            get { return (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringPreview || WebAuthorContext.Current.Mode == WebAuthorContextMode.PresentationUnpublishedPreview || WebAuthorContext.Current.Mode == WebAuthorContextMode.TemplatePreview); }
        }

        /// <summary>
        /// Shortcut to check whether page is displaying as it would when published
        /// </summary>
        /// <returns>False in new page or edit page mode; true otherwise</returns>
        public static bool IsViewing
        {
            get { return !IsEditing; }
        }

        /// <summary>
        /// Check whether this page is displaying in published mode
        /// </summary>
        public static bool IsPublished
        {
            get { return (CmsHttpContext.Current != null && CmsHttpContext.Current.Mode == PublishingMode.Published); }
        }

        /// <summary>
        /// Shortcut to getting the current CMS posting
        /// </summary>
        public static Posting Posting
        {
            get { return (CmsHttpContext.Current != null) ? CmsHttpContext.Current.Posting : null; }
        }

        /// <summary>
        /// Shortcut to getting the placeholders of the current posting
        /// </summary>
        public static PlaceholderCollection Placeholders
        {
            get
            {
                return (Posting != null) ? Posting.Placeholders : null;
            }
        }

        /// <summary>
        /// Shortcut to getting the current CMS channel
        /// </summary>
        public static Channel Channel
        {
            get { return (CmsHttpContext.Current != null) ? CmsHttpContext.Current.Channel : null; }
        }


        /// <summary>
        /// Shortcut to check whether page is being created
        /// </summary>
        /// <returns>True in new page mode; false otherwise</returns>
        public static bool IsCreating
        {
            get { return (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew); }
        }
        #endregion // Shortcuts into CMS context

        #region Work with CMS URLs
        /// <summary>
        /// Get the CMS GUID from a CMS URL, surrounded by curly brackets
        /// </summary>
        /// <param name="url">A CMS URL</param>
        /// <returns>CMS GUID surrounded by curly brackets</returns>
        public static string GetGuidFromUrl(string url)
        {
            var guidPattern = "(?<GUID>[0-9A-F]{8,8}-[0-9A-F]{4,4}-[0-9A-F]{4,4}-[0-9A-F]{4,4}-[0-9A-F]{12,12})";

            // Resource Gallery URLs start "/NR/rdonlyres/" but can come from TinyMCE as ../rdonlyres/;
            // Posting URLs start "/NR/exeres/", "NRNODEGUID=%7b" but can come from TinyMCE as ../exeres/;

            Match m = Regex.Match(url, "(/rdonlyres/|/exeres/|NRNODEGUID=%7b)" + guidPattern);
            if (m.Success) return "{" + m.Groups["GUID"].Value + "}";

            // Recognise a Posting URL
            m = Regex.Match(url, "^" + guidPattern + @"(,frameless)?\.htm");
            if (m.Success) return "{" + m.Groups["GUID"].Value + "}";

            return String.Empty;
        }

        /// <summary>
        /// Attempts to resolve a channel Guid to a published URL
        /// </summary>
        /// <param name="guid">String that represents the channel Guid</param>
        /// <returns>URI or Null</returns>
        public static Uri GetChannelUrlFromGuid(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                CmsContext cms = CmsHttpContext.Current;

                HierarchyItem hi = cms.Searches.GetByGuid(guid);
                if (hi != null)
                {

                    // is it a channel?
                    Channel c = hi as Channel;
                    if (c != null)
                    {


                        return new Uri(c.UrlModePublished, UriKind.RelativeOrAbsolute);
                    }
                }

            }
            return null;
        }


        /// <summary>
        /// Attempts to resolve a partial or complete URL into a Channel
        /// </summary>
        /// <param name="urlToParse">String to be recognised as a channel - it can be as simple as "yourcouncil" for a top-level channel</param>
        /// <param name="cms">CMS HTTP or Application context</param>
        /// <returns>A channel, or null</returns>
        public static Channel ParseChannelUrl(string urlToParse, CmsContext cms)
        {
            // See if there's a GUID in the URL
            string cmsGuid = CmsUtilities.GetGuidFromUrl(urlToParse);
            if (cmsGuid.Length > 0)
            {
                HierarchyItem hi = cms.Searches.GetByGuid(cmsGuid);
                if (hi != null)
                {
                    // is it a channel?
                    Channel c = hi as Channel;
                    if (c != null) return c;

                    // is it a posting?
                    Posting p = hi as Posting;
                    if (p != null)
                    {
                        return p.Parent;
                    }
                }
            }

            // Strip off protocol and domain
            if (urlToParse.StartsWith("http"))
            {
                Uri uriOfChannel = new Uri(urlToParse);
                urlToParse = uriOfChannel.PathAndQuery;
            }

            // Make sure path starts with /Channels
            if (!urlToParse.StartsWith("/")) urlToParse = String.Format("/{0}", urlToParse);
            urlToParse = String.Format("/Channels{0}", urlToParse);

            // Strip off any page name
            int lastSlashPos = urlToParse.LastIndexOf("/");
            if (lastSlashPos > -1)
            {
                if (urlToParse.IndexOf(".", lastSlashPos) > -1) urlToParse = urlToParse.Substring(0, lastSlashPos + 1);
            }

            return cms.Searches.GetByPath(urlToParse) as Channel;
        }

        /// <summary>
        /// Attempts to resolve a partial or complete URL into a ResourceGallery
        /// </summary>
        /// <param name="urlToParse">String to be recognised as a resource gallery - it can be as simple as "documents" for a top-level channel</param>
        /// <param name="cms">CMS HTTP or Application context</param>
        /// <returns>A resource gallery, or null</returns>
        public static ResourceGallery ParseResourceGalleryUrl(string urlToParse, CmsContext cms)
        {
            // See if there's a GUID in the URL
            string cmsGuid = CmsUtilities.GetGuidFromUrl(urlToParse);
            if (cmsGuid.Length > 0)
            {
                HierarchyItem hi = cms.Searches.GetByGuid(cmsGuid);
                if (hi != null)
                {
                    // is it a resource gallery?
                    ResourceGallery g = hi as ResourceGallery;
                    if (g != null) return g;

                    // is it a resource?
                    Resource r = hi as Resource;
                    if (r != null)
                    {
                        return r.Parent;
                    }
                }
            }

            // Strip off protocol and domain
            if (urlToParse.StartsWith("http"))
            {
                Uri uriOfGallery = new Uri(urlToParse);
                urlToParse = uriOfGallery.PathAndQuery;
            }

            // Make sure path starts with /Resources
            if (!urlToParse.StartsWith("/", StringComparison.Ordinal)) urlToParse = String.Format("/{0}", urlToParse);
            if (!urlToParse.StartsWith("/Resources", StringComparison.OrdinalIgnoreCase)) urlToParse = String.Format("/Resources{0}", urlToParse);

            // Strip off any resource name
            int lastSlashPos = urlToParse.LastIndexOf("/");
            if (lastSlashPos > -1)
            {
                if (urlToParse.IndexOf(".", lastSlashPos) > -1) urlToParse = urlToParse.Substring(0, lastSlashPos + 1);
            }

            return cms.Searches.GetByPath(urlToParse) as ResourceGallery;
        }

        /// <summary>
        /// Attempts to resolve a partial or complete URL into a CMS Resource
        /// </summary>
        /// <param name="urlToParse">String to be recognised as a resource, should contain a GUID for the resource</param>
        /// <param name="cms">CMS HTTP or Application context</param>
        /// <returns>A CMS resource, or null</returns>
        public static Resource ParseResourceUrl(string urlToParse, CmsContext cms)
        {
            string guidFromUrl = CmsUtilities.GetGuidFromUrl(urlToParse);
            if (guidFromUrl.Length > 0)
            {
                HierarchyItem byGuid = cms.Searches.GetByGuid(guidFromUrl);
                if (byGuid != null)
                {
                    Resource resource = byGuid as Resource;
                    if (resource != null)
                    {
                        return resource;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the current CMS channel, even if the current page is not in CMS
        /// </summary>
        /// <returns></returns>
        public static Channel GetCurrentChannel()
        {
            CmsHttpContext cms = CmsHttpContext.Current;
            Channel ch = cms.Channel;
            if (ch == null)
            {
                // if outside CMS, remove any filename & querystring from the end of the url, and use remaining url to get the channel
                string channelUrl = HttpContext.Current.Request.RawUrl;
                if (channelUrl.IndexOf("?") > -1) channelUrl = channelUrl.Substring(0, channelUrl.IndexOf("?"));
                if (!channelUrl.EndsWith("/")) channelUrl = channelUrl.Substring(0, channelUrl.LastIndexOf("/"));

                // if it starts with /NR/RDONLYRES/ it'll be a resource, not a channel
                if (!channelUrl.StartsWith("/NR/RDONLYRES/", StringComparison.OrdinalIgnoreCase))
                {
                    ch = cms.Searches.GetByUrl(channelUrl) as Channel;
                }
            }

            if (ch == null && HttpContext.Current.Request.RawUrl.Contains("NRNODEGUID"))
            {
                // If in CMS but we've used the file extension to rewrite the path and load something else, the current channel isn't 
                // recognised, so get the original posting GUID from the raw URL and use that
                var qs = Iri.SplitQueryString(HttpContext.Current.Request.RawUrl);
                var posting = cms.Searches.GetByGuid(HttpUtility.UrlDecode(qs["NRNODEGUID"])) as Posting;
                if (posting != null) ch = posting.Parent;
            }
            return ch;
        }

        /// <summary>
        /// Gets whether the supplied channel should be served over HTTPS
        /// </summary>
        /// <param name="ch">Channel to test</param>
        /// <returns><c>true</c> to use SSL; <c>false</c> otherwise</returns>
        public static bool IsSecureChannel(Channel ch)
        {
            var sslChannel = ch.IsImportant;
            if (!sslChannel)
            {
                CustomProperty ssl = CmsUtilities.GetCustomProperty(ch.CustomProperties, "SSL");
                sslChannel = (ssl != null);
            }
            return sslChannel;
        }

        /// <summary>
        /// Gets the URL of the supplied posting as a <see>System.Uri</see>
        /// </summary>
        /// <param name="p">A posting</param>
        /// <returns>The URL of the posting, corrected for host headers</returns>
        public static Uri GetPostingUrl(Posting p)
        {
            string correctedUrl = CmsUtilities.CorrectPublishedUrl(p.Url);
            if (correctedUrl.StartsWith("/") && HttpContext.Current != null)
            {
                // Any published mode URL is now a virtual URL starting with /
                // but it needs a protocol and domain to be returned as a System.Uri
                Uri pageUrl = HttpContext.Current.Request.Url;
                correctedUrl = pageUrl.Scheme + "://" + pageUrl.Host + correctedUrl;
            }
            return new Uri(correctedUrl);
        }

        #endregion // Work with CMS URLs

        #region Work with CMS resources
        /// <summary>
        /// Get the size (in kilobytes) of an item in the Resource Gallery
        /// </summary>
        /// <param name="res">The resource to return the size of</param>
        /// <returns>string with the size in kilobytes followed by a lowercase k, eg 123k</returns>
        public static string GetResourceFileSize(Resource res)
        {
            string size = "";

            // get ref to resource gallery item
            if (res != null)
            {
                // convert bytes to kbytes
                double kSize = Math.Round((double)(res.Size / 1024));
                size = kSize.ToString() + "k";
            }
            else
            {
                throw new ArgumentException("Resource does not represent an item in a CMS Resource Gallery");
            }

            return size;

        }


        #endregion // Work with CMS resources

        #region Work with CMS custom properties
        /// <summary>
        /// Gets a custom property, if it exists
        /// </summary>
        /// <param name="cpc">The custom property collection of a Posting or Channel</param>
        /// <param name="Name">The name of the custom property which might exist</param>
        /// <returns>The custom property, or null if it does not exist</returns>
        /// <remarks>
        /// This is more efficient than <code>if (posting.CustomProperties["PropertyName"] != null)</code>. 
        /// See http://blogs.technet.com/stefan_gossner/archive/2005/07/25/408178.aspx
        /// </remarks>
        public static CustomProperty GetCustomProperty(CustomPropertyCollection cpc, string Name)
        {
            foreach (CustomProperty cp in cpc)
            {
                if (cp.Name == Name)
                    return cp;
            }
            return null;
        }

        #endregion // Work with CMS custom properties

        #region Work with CMS Postings

        /// <summary>
        /// Date value which indicates that a posting never expires
        /// </summary>
        public static DateTime NeverExpires { get { return new DateTime(3000, 1, 1); } }

        /// <summary>
        /// Gets a hash code representing the content of the posting, so that it can be compared to see whether it's changed
        /// </summary>
        /// <param name="cmsContext">The CMS context.</param>
        /// <param name="posting">The posting to hash</param>
        /// <returns></returns>
        public static string GetPostingContentHash(CmsContext cmsContext, Posting posting)
        {
            string placeholderContent = GetPostingContentToCompare(new Guid(posting.Guid));

            // This is debug code for spotting difference between versions of a posting. Because of how this method is called,
            // it'll only run when the querystring parameter "CmsContentHash" is provided for testing.
            if (HttpContext.Current != null) HttpContext.Current.Response.Write("<!--" + placeholderContent + "-->");

            // Return a hash of all the placeholder content for comparison
            return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(placeholderContent, "MD5");
        }

        /// <summary>
        /// Gets the raw posting content to compare with another posting.
        /// </summary>
        /// <param name="postingGuid">The posting GUID.</param>
        /// <returns></returns>
        public static string GetPostingContentToCompare(Guid postingGuid)
        {
            // Get the hash for the most recently approved version, not the work-in-progress version.
            // In Published mode there are no revisions available so always use the current, which must be the last approved anyway
            /*if (cmsContext.Mode != PublishingMode.Published && posting.State != PostingState.Published)
            {
                PostingCollection revisions = posting.Revisions();
                if (revisions.Count == 0) return null; // page has never been approved

                posting = revisions[revisions.Count - 1];
            }*/


            // Always get the hash in published mode since a) it's to find out if the published version is live and b) unpublished mode is a pain to work with.
            CmsApplicationContext context = new CmsApplicationContext();
            context.AuthenticateAsCurrentUser(PublishingMode.Published);
            Posting p = context.Searches.GetByGuid("{" + postingGuid.ToString() + "}") as Posting;
            if (p == null) return null;

            // Get a hash of all the placeholder content joined together
            StringBuilder postingContent = new StringBuilder();
            foreach (Placeholder ph in p.Placeholders)
            {
                if (ph.Name == "phDefAuthorNotes") continue; // it's never displayed on public site, so we don't mind what happens here
                postingContent.Append(ph.Datasource.RawContent);
            }

            // The "manage CMS URLs" setting on placeholders creates a problem, because it changes the content of placeholders
            // depending on whether you're in Published or Unpublished mode. The aim is to have comparable text for a placeholder
            // whatever mode it's viewed in, so we try to convert all CMS-managed URLs into correct, published-mode URLs.

            // STEP 1: Try to convert unpublished links to published links using the CMS API to look up the posting
            consistentUrlForHash_Context = context; // cmsContext;
            string placeholderContent = Regex.Replace(postingContent.ToString(), " (href|src)=\"([^\"]*)\"", new MatchEvaluator(ConsistentUrlForHash_MatchEvaluator));

            // STEP 2: CMS manages published/unpublished URLs in title attributes too. Since link titles are stripped for display
            // anyway, simplest solution is just to remove them for comparison as well.
            placeholderContent = Regex.Replace(placeholderContent, "<a title=\"([^\"]*)\"", "<a");

            // STEP 3: Posting may also have been uploaded due to changes in custom properties or publishing dates, so add those to the comparison.
            // Use standard date formatter to eliminate any mismatch between machine settings.
            foreach (CustomProperty prop in p.CustomProperties)
            {
                placeholderContent += prop.Value;
            }
            placeholderContent += DateTimeFormatter.Iso8601DateTime(p.StartDate);
            placeholderContent += DateTimeFormatter.Iso8601DateTime(p.ExpiryDate);

            // STEP 4: Placeholders may or may not preserve the end of line markers in the original content, so eliminate them for comparison
            placeholderContent = placeholderContent.Replace(Environment.NewLine, "  ");

            return placeholderContent;
        }

        private static CmsContext consistentUrlForHash_Context;

        /// <summary>
        /// Ensures CMS URLs managed by CMS are converted back to published mode URLs for the purpose of hashing
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private static string ConsistentUrlForHash_MatchEvaluator(Match match)
        {
            string attributeName = match.Groups[1].Value;
            string linkDestination = match.Groups[2].Value;
            linkDestination = linkDestination.TrimEnd('#'); // creates a problem if the code below tidies up the unpublished version, but this is left on the end of the published version
            if (linkDestination.StartsWith("/NR/"))
            {
                HierarchyItem hierarchyItem = consistentUrlForHash_Context.Searches.GetByUrl(linkDestination);
                if (hierarchyItem != null)
                {
                    Posting posting = hierarchyItem as Posting;
                    if (posting != null)
                    {
                        linkDestination = posting.UrlModePublished;
                    }
                    else
                    {
                        Channel channel = hierarchyItem as Channel;
                        if (channel != null)
                        {
                            linkDestination = channel.UrlModePublished;
                        }
                    }
                }
                else
                {
                    string guid = CmsUtilities.GetGuidFromUrl(linkDestination);
                    if (!String.IsNullOrEmpty(guid))
                    {
                        Resource res = consistentUrlForHash_Context.Searches.GetByGuid(guid) as Resource;
                        if (res == null)
                        {
                            // If it's not a posting or channel or resource, but it's got a guid, it's probably an attachment 
                            // uploaded on a page. Attachments on a page have a number in the URL which is different on staging 
                            // and live, so remove that part of the URL for comparison purposes. 
                            linkDestination = Regex.Replace(linkDestination, "/NR/rdonlyres/([A-Z0-9-]{36,36})/[0-9]{2,}/", "/NR/rdonlyres/$1/");
                        }
                        else
                        {
                            // When a resource is replaced in Resource Manager, existing links on pages remain the same in 
                            // unpublished mode, but are automatically converted to the new filename in published mode.
                            // This means when comparing links between published and unpublished mode, even a long time after
                            // the document is replaced, the link can only be trusted up to and including the GUID.
                            linkDestination = linkDestination.Substring(0, 51);
                        }
                    }
                }
            }

            return " " + attributeName + "=\"" + CmsUtilities.CorrectPublishedUrl(linkDestination) + "\"";
        }

        /// <summary>
        /// Gets the last date the posting was approved
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static DateTime? LastApprovedDate(Posting p)
        {
            if (p == null) throw new ArgumentNullException("p");

            try
            {
                PostingCollection revisions = p.Revisions();
                if (revisions.Count == 0) return null; // page has never been approved

                Posting lastApproved = revisions[revisions.Count - 1];
                return lastApproved.RevisionDate;
            }
            catch (CmsServerException)
            {
                // This happens when calling this method on a new posting being saved with a local attachment
                return null;
            }
        }

        /// <summary>
        /// Analyse the content of a posting and generate statistics
        /// </summary>
        /// <param name="posting"></param>
        /// <returns></returns>
        public static PostingAnalysis AnalysePostingContent(Posting posting)
        {
            if (posting == null) throw new ArgumentNullException("posting");

            var analysis = new PostingAnalysis();

            var content = new StringBuilder();
            foreach (Placeholder placeholder in posting.Placeholders)
            {
                // ignore placeholders which aren't shown to the public or aren't prose
                if (placeholder.Name == "phDefAuthorNotes" || placeholder.Name == "defSurveyHtml")
                {
                    continue;
                }

                if (placeholder is XmlPlaceholder)
                {
                    // The only XmlPlaceholder with prose in is the TextPlaceholderControl, so look for instances we should process and reject the rest.
                    if (!Regex.IsMatch(placeholder.Name, "^(phDefTitle|phDefSubtitle|phDefCaption|defSubtitle|phDefIssueTitle|phDefItemTitle|phDefPlace)[0-9]*$"))
                    {
                        continue;
                    }
                }

                // Get the HTML stored in the placeholder
                string placeholderContent = placeholder.Datasource.RawContent;

                // Add up instances of tags before they get stripped
                analysis.TotalHeading1 += Regex.Matches(placeholderContent, "<h1[ >]", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;
                analysis.TotalHeading2 += Regex.Matches(placeholderContent, "<h2[ >]", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;
                analysis.TotalHeading3 += Regex.Matches(placeholderContent, "<h3[ >]", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;
                analysis.TotalHeading4 += Regex.Matches(placeholderContent, "<h4[ >]", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;
                analysis.TotalHeading5 += Regex.Matches(placeholderContent, "<h5[ >]", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;
                analysis.TotalLinks += Regex.Matches(placeholderContent, "<a ", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;
                analysis.TotalDocuments += Regex.Matches(placeholderContent, "<a [^>]*href=\"[^\"]+.(pdf|rtf|doc|docx|dot|dotx|xls|xlsx|xlt|xltx|csv|ppt|pptx|pps|ppsx|pot|potx)\"", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;
                analysis.TotalLinksToAnchors += Regex.Matches(placeholderContent, "<a [^>]*href=\"[^\"]*#[^\"]+\"", RegexOptions.Singleline | RegexOptions.IgnoreCase).Count;

                // All these tags should be preceeded by a full stop otherwise the words they contain are run into the next sentence when tags are stripped.
                // We should only put in a full stop if punctuation doesn't already exist, so use a MatchEvaluator to find it.
                placeholderContent = Regex.Replace(placeholderContent, @"(?<PreviousCharacter>.)\s*</(li|p|h1|h2|h3|h4|h5|h6|dd)>", new MatchEvaluator(AddFullStop_MatchEvaluator), RegexOptions.Singleline);

                // Remove HTML and decode entities to turn into plain text
                placeholderContent = Html.StripTags(placeholderContent);
                placeholderContent = HttpUtility.HtmlDecode(placeholderContent);

                // Remove characters used for shortcodes in newsletter template
                placeholderContent = placeholderContent.Replace("[", String.Empty).Replace("]", String.Empty);

                // Remove URLs
                placeholderContent = Regex.Replace(placeholderContent, "https?://[^ ]+", String.Empty);

                if (!String.IsNullOrEmpty(placeholderContent))
                {
                    // separate with punctuation and a space so words in consecutive placeholders aren't concatenated either as one word or the same sentence
                    var punctuation = new char[] { '.', '?', '!', ';', ':', ')' };
                    content.Append(placeholderContent.TrimEnd(punctuation).Trim()).Append(". ").Append(Environment.NewLine).Append(Environment.NewLine);

                    // if the presence of this placeholder means a hard-coded heading in the template is shown, increment the number of headings on the page
                    if (Regex.IsMatch(placeholder.Name, "^(phDefTitle|defTitle|phDefCllr|phDefSchoolInfo|phDefPhase|phDefLibName|defAlert)$"))
                    {
                        analysis.TotalHeading1++;
                    }
                    else if (Regex.IsMatch(placeholder.Name, "^(defHeading|defPart|phDefListTitle|phDefSubtitle|defSubtitle|phDefPlace|defReport|defApply|defPay)[0-9]*(Title[0-9]+)?$"))
                    {
                        analysis.TotalHeading2++;
                    }
                    else if (Regex.IsMatch(placeholder.Name, "^(phDefItemTitle|phDefEventTitle)[0-9]*$"))
                    {
                        analysis.TotalHeading3++;
                    }
                }
            }
            string html = content.ToString();

            var textStats = new TextStatistics();
            analysis.FleschKincaidReadingEase = textStats.FleschKincaidReadingEase(html);
            analysis.AverageGradeLevel = textStats.AverageGradeLevel(html);
            analysis.ContentLength = content.Length;

            return analysis;
        }

        private static string AddFullStop_MatchEvaluator(Match m)
        {
            var punctuation = new char[] { '.', '?', '!', ';', ':', ')' };
            return m.Groups["PreviousCharacter"].Value.TrimEnd(punctuation) + ". ";
        }

        /// <summary>
        /// Saves details of the state of a posting for monitoring quality
        /// </summary>
        /// <param name="posting"></param>
        /// <param name="snapshotDate"></param>
        /// <param name="type"></param>
        /// <param name="connection">Existing SQL connection potentially tied to a transaction</param>
        public static void SavePostingSnapshot(Posting posting, DateTime snapshotDate, SnapshotType type, SqlConnection connection)
        {
            if (posting == null) throw new ArgumentNullException("posting");

            if (TemplateIsExcludedFromSnapshots(posting.Template.Guid)) return;
            if (ChannelIsExcludedFromSnapshots(posting.Parent.Guid)) return;

            // note who last submitted the page
            CustomProperty submittedBy = CmsUtilities.GetCustomProperty(posting.CustomProperties, "ESCC.LastSubmittedBy");
            string submittedByName = (submittedBy != null) ? submittedBy.Value : null;

            var analysis = CmsUtilities.AnalysePostingContent(posting);
            DateTime? lastApproved = null;
            lastApproved = CmsUtilities.LastApprovedDate(posting);

            var sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@SnapshotType", type.ToString().ToLower()));
            sqlParams.Add(new SqlParameter("@SnapshotDate", snapshotDate));
            sqlParams.Add(new SqlParameter("@PostingGuid", posting.Guid));
            sqlParams.Add(new SqlParameter("@PostingPath", CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished)));
            sqlParams.Add(new SqlParameter("@PostingDisplayName", posting.DisplayName));
            sqlParams.Add(new SqlParameter("@TemplateGuid", posting.Template.Guid));
            sqlParams.Add(new SqlParameter("@StateCurrentVersion", posting.StateUnapprovedVersion));
            sqlParams.Add(new SqlParameter("@StateApprovedVersion", posting.StateApprovedVersion));
            sqlParams.Add(new SqlParameter("@LastSubmittedBy", submittedByName));
            sqlParams.Add(new SqlParameter("@LastApproved", lastApproved));
            sqlParams.Add(new SqlParameter("@ExpiryDate", posting.ExpiryDate));
            sqlParams.Add(new SqlParameter("@ContentLength", analysis.ContentLength));
            sqlParams.Add(new SqlParameter("@TotalHeadings", analysis.TotalHeadings));
            sqlParams.Add(new SqlParameter("@TotalHeading1", analysis.TotalHeading1));
            sqlParams.Add(new SqlParameter("@TotalHeading2", analysis.TotalHeading2));
            sqlParams.Add(new SqlParameter("@TotalHeading3", analysis.TotalHeading3));
            sqlParams.Add(new SqlParameter("@TotalHeading4", analysis.TotalHeading4));
            sqlParams.Add(new SqlParameter("@TotalHeading5", analysis.TotalHeading5));
            sqlParams.Add(new SqlParameter("@TotalLinks", analysis.TotalLinks));
            sqlParams.Add(new SqlParameter("@TotalDocuments", analysis.TotalDocuments));
            sqlParams.Add(new SqlParameter("@TotalLinksToAnchors", analysis.TotalLinksToAnchors));
            sqlParams.Add(new SqlParameter("@FleschKincaidReadingEase", analysis.FleschKincaidReadingEase));
            sqlParams.Add(new SqlParameter("@AverageGradeLevel", analysis.AverageGradeLevel));

            SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, "usp_PostingSnapshot_Insert", sqlParams.ToArray());
        }

        private static bool ChannelIsExcludedFromSnapshots(string channelGuid)
        {
            return GuidIsExcludedFromSnapshots("EsccWebTeam.Cms/ChannelsExcludedFromSnapshots", channelGuid);
        }

        private static bool TemplateIsExcludedFromSnapshots(string templateGuid)
        {
            return GuidIsExcludedFromSnapshots("EsccWebTeam.Cms/TemplatesExcludedFromSnapshots", templateGuid);
        }

        private static bool GuidIsExcludedFromSnapshots(string configSection, string nodeGuid)
        {
            var config = ConfigurationManager.GetSection(configSection) as NameValueCollection;
            if (config == null) return false;

            foreach (var guid in config.AllKeys)
            {
                if (guid == nodeGuid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Records that a change was made to a posting for monitoring activity levels
        /// </summary>
        /// <param name="posting"></param>
        /// <param name="stage"></param>
        public static void SavePostingEdit(Posting posting, WorkflowStage stage)
        {
            WindowsPrincipal currentUser = (WindowsPrincipal)Thread.CurrentPrincipal;

            var sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@PostingGuid", posting.Guid));
            sqlParams.Add(new SqlParameter("@PostingPath", CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished)));
            sqlParams.Add(new SqlParameter("@EditedBy", currentUser.Identity.Name));
            sqlParams.Add(new SqlParameter("@WorkflowStage", (int)stage));

            SqlHelper.ExecuteNonQuery(new SqlConnection(ConfigurationManager.ConnectionStrings["CmsSupport"].ConnectionString), CommandType.StoredProcedure, "usp_Edit_Insert", sqlParams.ToArray());
        }

        #endregion

        #region Work with CMS PostingCollections

        /// <summary>
        /// Gets the posting which will be displayed if the published URL of the channel is requested
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns><c>Posting</c>, or <c>null</c> if the channel is null or empty</returns>
        public static Posting DefaultPostingInChannel(Channel channel)
        {
            if (channel == null || channel.Postings.Count == 0) return null;

            // If "Sort items in channel" has been used then at least one posting will have a SortOrdinal of > 0.
            // channel.DefaultPostingName is empty, and postings not in sorted order by default
            var channelItemsSorted = false;
            foreach (Posting posting in channel.Postings)
            {
                if (posting.SortOrdinal > 0)
                {
                    channelItemsSorted = true;
                    break;
                }
            }

            // If that's the case, sort the collection by SortOrdinal to get the default posting. 
            if (channelItemsSorted)
            {
                var postings = channel.Postings;
                postings.SortByOrdinal();
                return postings[0];
            }
            else
            {
                // But if "Sort items in channel" hasn't been used, sorting by SortOrdinal - which is 0 for all of them - 
                // won't necessarily give the right result. Just taking the first posting from the unsorted collection
                // doesn't give the right result either. Don't think it's ChangeDate or RevisionDate because the default
                // page doesn't change if you update a posting. Best guess so far is to take the posting with the earliest 
                // creation date, but of course there's no built-in way to do that.
                //
                // This definitely isn't the complete answer though, because if the default posting is "Hidden when published" 
                // it's still the default posting, but it's hidden from the published mode API and therefore not returned by this method.
                var postingList = new List<Posting>();
                foreach (Posting posting in channel.Postings)
                {
                    postingList.Add(posting);
                }
                postingList.Sort(ComparePostingsByDateCreatedAscending);
                return postingList[0];
            }
        }

        /// <summary>
        /// Compares one posting to another by the date it was created, allowing sorting.
        /// </summary>
        private static Comparison<Posting> ComparePostingsByDateCreatedAscending
        {
            get
            {
                return new Comparison<Posting>(ComparePostingByDateCreatedAscendingComparison);
            }
        }

        /// <summary>
        /// Compares two postings by date created with older postings sorted first.
        /// </summary>
        /// <param name="object1">The object1.</param>
        /// <param name="object2">The object2.</param>
        /// <returns></returns>
        private static int ComparePostingByDateCreatedAscendingComparison(Posting object1, Posting object2)
        {
            if (object1 == null) throw new ArgumentNullException("object1");
            if (object2 == null) throw new ArgumentNullException("object2");
            return object1.CreatedDate.Ticks.CompareTo(object2.CreatedDate.Ticks);
        }


        /// <summary>
        /// Check whether a posting with the specified name exists in the specified channel
        /// </summary>
        /// <param name="postingName">The name of a posting, as entered when saving</param>
        /// <param name="channel">A CMS Channel</param>
        /// <returns>GUID of posting, or null</returns>
        public static string PostingExistsInChannel(string postingName, Channel channel)
        {
            string guid = null;

            foreach (Posting posting in channel.Postings)
            {
                if (posting.Name.ToLower().Trim() == postingName.ToLower().Trim())
                {
                    guid = posting.Guid;
                    break;
                }
            }

            return guid;
        }

        #endregion // Work with CMS PostingCollections

        #region Work with page expiry

        /// <summary>
        /// Set the expiry date of a Posting to the default expiry date
        /// </summary>
        /// <param name="posting">The Posting to work with</param>
        public static void SetDefaultExpiry(Posting posting)
        {
            // Postings are set never to expire by default, so this doesn't change the value. However, it does
            // cause the PropertyChanging event to fire for the ExpiryDate property. The ApplyExpiryRules method
            // should be attached to that, and that controls the default posting ExpiryDate.
            posting.ExpiryDate = CmsUtilities.NeverExpires;
        }

        /// <summary>
        /// Control which expiry dates it's possible to set
        /// </summary>
        /// <param name="e">The PropertyChangingEventArgs from the CmsPosting_PropertyChanging event</param>
        /// <remarks>
        /// <para>Expiry limits should be applied as follows:</para>
        /// <list type="bullet">
        /// <item>If nothing set in web.config, allow any expiry date including never</item>
        /// <item>If a default number of months is set in web.config, apply that default</item>
        /// <item>If &quot;never&quot; or a number of months is set for the channel or one of its parents in web.config, apply that instead of the default</item>
        /// <item>If &quot;never&quot; or a number of months is set for the template in web.config, apply that instead of the default or the channel setting</item>
        /// </list>
        /// <example>
        /// <para>The web.config needs the sections defined using a standard section handler.</para>
        /// <code>
        ///	&lt;configuration&gt;
        ///		&lt;configSections&gt;
        ///			&lt;sectionGroup name=&quot;EsccWebTeam.Cms&quot;&gt;
        ///				&lt;section name=&quot;ChannelExpiry&quot; type=&quot;System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089&quot; /&gt;
        ///				&lt;section name=&quot;TemplateExpiry&quot; type=&quot;System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089&quot; /&gt;
        ///			&lt;/sectionGroup&gt;
        ///		&lt;/configSections&gt;
        ///	&lt;/configuration&gt;
        /// </code>
        /// <para>You can then set values of "never" or a number of months as a default, for any channel URL or for any template GUID</para>
        /// <code>
        ///	&lt;EsccWebTeam.Cms&gt;
        ///		&lt;ChannelExpiry&gt;
        ///			&lt;add key=&quot;default&quot; value=&quot;6&quot; /&gt;
        ///			&lt;add key=&quot;yourcouncil/pressoffice/pressreleases&quot; value=&quot;never&quot; /&gt;
        ///			&lt;add key=&quot;new&quot; value=&quot;12&quot; /&gt;
        ///		&lt;/ChannelExpiry&gt;
        ///		&lt;TemplateExpiry&gt;
        ///			&lt;add key=&quot;{EEA2326F-033C-46c4-ADC7-BC0EE65C2C72}&quot; value=&quot;never&quot; /&gt;
        ///			&lt;add key=&quot;{31171519-6173-40cc-A3EF-AA5227DC0581}&quot; value=&quot;12&quot; /&gt;
        ///		&lt;/TemplateExpiry&gt;
        ///	&lt;/EsccWebTeam.Cms&gt;
        /// </code>
        /// </example>
        /// </remarks>
        public static void ApplyExpiryRules(PropertyChangingEventArgs e)
        {
            // Trap ExpiryDate changes
            if (e.PropertyName == "ExpiryDate")
            {
                // Disallow expiry dates > (now + permitted expiry interval)
                Posting p = e.Target as Posting;

                NameValueCollection channelExpiry = ConfigurationManager.GetSection("EsccWebTeam.Cms/ChannelExpiry") as NameValueCollection;
                NameValueCollection templateExpiry = ConfigurationManager.GetSection("EsccWebTeam.Cms/TemplateExpiry") as NameValueCollection;

                if (p != null)
                {
                    DateTime expiryMax = DateTime.MaxValue; // allow "never" by default

                    if (channelExpiry != null)
                    {
                        // Gets the tidied-up URL of the channel
                        string channelUrl = CmsUtilities.CorrectPublishedUrl(p.Parent.UrlModePublished);
                        int lastSlash = channelUrl.LastIndexOf("/") - 1;
                        if (lastSlash > 0)
                        {
                            channelUrl = channelUrl.Substring(1, lastSlash).ToLower(CultureInfo.CurrentCulture);
                        }

                        int slashIndex;
                        if (channelUrl.Length > 0)
                        {
                            // Look for an entry relating to each folder of the URL, starting with the most specific.
                            // Have to start with the most specific to allow overriding deeper in the hierarchy.
                            while (channelExpiry[channelUrl] == null)
                            {
                                slashIndex = channelUrl.LastIndexOf("/");
                                if (slashIndex == -1)
                                {
                                    channelUrl = "default";
                                    break;
                                }
                                channelUrl = channelUrl.Substring(0, slashIndex);
                            }
                        }

                        // Check whether there's an specific entry for the channel
                        if (channelExpiry[channelUrl] != null)
                        {
                            // Channel entry could be "never" to allow never
                            if (channelExpiry[channelUrl].ToLower(CultureInfo.CurrentCulture) == "never")
                            {
                                // leave as "never"
                            }
                            else
                            {
                                // should be a number of months
                                expiryMax = DateTime.Now.AddMonths(Convert.ToInt16(channelExpiry[channelUrl]));
                            }
                        }
                    }


                    // Allow specific setting for template to override setting for channel/default setting
                    if (templateExpiry != null && templateExpiry[p.Template.Guid] != null)
                    {
                        if (templateExpiry[p.Template.Guid].ToLower(CultureInfo.CurrentCulture) == "never")
                        {
                            expiryMax = DateTime.MaxValue;
                        }
                        else
                        {
                            // should be a number of months
                            expiryMax = DateTime.Now.AddMonths(Convert.ToInt16(templateExpiry[p.Template.Guid]));
                        }
                    }

                    // If max expiry is not "never",and is less than the date chosen, reset expiry to max
                    if (expiryMax != DateTime.MaxValue && DateTime.Compare((DateTime)e.PropertyValue, expiryMax) > 0)
                    {
                        // Reset to max
                        e.PropertyValue = expiryMax;
                    }
                    // else leave as 'changed-to' value
                }
            }
        }

        #endregion // Work with page expiry

        #region Apply rules on save/submit/approve
        /// <summary>
        /// When the name of a CMS Posting is being changed, check that it complies with naming rules, is not a duplicate page name etc...
        /// </summary>
        /// <param name="e">The PropertyChangingEventArgs from the CmsPosting_PropertyChanging event</param>
        public static void ApplyNamingRules(PropertyChangingEventArgs e)
        {
            Posting posting = CmsHttpContext.Current.Posting;

            // prevent invalid or duplicate page names
            if (e.PropertyName == "Name")
            {
                string pageName = e.PropertyValue.ToString().ToLower(CultureInfo.CurrentCulture); // lowercase
                if (pageName.EndsWith(".htm")) pageName = pageName.Substring(0, pageName.Length - 4); // no .htm.htm pages

                // allow only whitelisted characters
                pageName = pageName.Replace(" ", "-");
                pageName = Regex.Replace(pageName, "[^a-z0-9-]", String.Empty);

                //*** ensure the name is not a duplicate ***//

                // first number to add, if duplicate found
                int i = 1;

                // remember original name so we get name1, name2, name3 instead of name1, name12, name123
                string originalPageName = pageName;

                // check for duplicate posting
                string postingGuid = CmsUtilities.PostingExistsInChannel(pageName, posting.Parent);

                // if not null, duplicate found
                while (postingGuid != null)
                {
                    // check that the "duplicate" is not the current posting (not sure if that's even possible, but better to be safe)
                    if (postingGuid != posting.Guid)
                    {
                        // add a number to the entered name if it's a duplicate, and re-check
                        pageName = originalPageName + i.ToString();
                        postingGuid = CmsUtilities.PostingExistsInChannel(pageName, posting.Parent);
                        i++;
                    }
                    else break;
                }
                e.PropertyValue = pageName;
            }
        }

        /// <summary>
        /// Tidy up data of a Posting before as it is approved
        /// </summary>
        /// <param name="posting">The Posting to work with</param>
        public static void TidyDataBeforeApproval(Posting posting)
        {
            // tidy up data for search
            string temp = posting.Description;
            temp = temp.Replace("–", "-");
            temp = temp.Replace("'", "'");
            posting.Description = temp;
        }

        /// <summary>
        /// Save the login name of the person approving a posting to a custom property
        /// </summary>
        /// <param name="posting">The posting being submitted</param>
        public static void SaveApproverInfo(Posting posting)
        {
            if (posting.CustomProperties["ESCC.LastSubmittedBy"] != null)
            {
                var securityConfig = ConfigurationManager.GetSection("EsccWebTeam.Cms/SecuritySettings") as NameValueCollection;
                if (securityConfig == null || String.IsNullOrEmpty(securityConfig["WebTeamGroup"]))
                {
                    throw new ConfigurationErrorsException("The 'WebTeamGroup' setting in the EsccWebTeam.Cms/SecuritySettings of web.config was not found");
                }

                var currentUser = (WindowsPrincipal)Thread.CurrentPrincipal;
                if (!currentUser.IsInRole(securityConfig["WebTeamGroup"]))
                {
                    posting.CustomProperties["ESCC.LastSubmittedBy"].Value = currentUser.Identity.Name;

                    UpdateCreatorFromFindStaff(posting, currentUser.Identity.Name);
                }
            }
        }

        /// <summary>
        /// Save the login name of the person submitting a posting to a custom property
        /// </summary>
        /// <param name="posting">The posting being submitted</param>
        public static void SaveSubmitterInfo(Posting posting)
        {
            if (posting.CustomProperties["ESCC.LastSubmittedBy"] != null)
            {
                WindowsPrincipal currentUser = (WindowsPrincipal)Thread.CurrentPrincipal;
                posting.CustomProperties["ESCC.LastSubmittedBy"].Value = currentUser.Identity.Name;

                UpdateCreatorFromFindStaff(posting, currentUser.Identity.Name);
            }
        }

        private static void UpdateCreatorFromFindStaff(Posting posting, string username)
        {
            var prop = CmsUtilities.GetCustomProperty(posting.CustomProperties, "DC.creator");
            if (prop == null) return;

            using (var findStaff = new FindStaffWebService.FindStaffWebService())
            {
                findStaff.UseDefaultCredentials = true;
                var employee = findStaff.EmployeeByUsername(username, false, 0);
                if (employee != null && employee.Jobs.Length > 0)
                {
                    var creator = employee.Jobs[0].JobTitle;
                    if (employee.Jobs[0].Teams.Length > 0)
                    {
                        creator += ", " + employee.Jobs[0].Teams[0].Name;
                    }
                    if (employee.Jobs[0].ContactNumbers.Length > 0)
                    {
                        creator += ", " + employee.Jobs[0].ContactNumbers[0].NationalNumber;
                    }
                    prop.Value = creator;
                }
            }
        }

        /// <summary>
        /// Validate and generate metadata in custom properties
        /// </summary>
        /// <param name="p"></param>
        public static void ApplyMetadata(Posting p)
        {
            if (p != null)
            {
                EsdControlledList ipsvXml = EsdControlledList.GetControlledList("Ipsv");
                CustomProperty ipsvProp = CmsUtilities.GetCustomProperty(p.CustomProperties, "IPSV preferred");
                CustomProperty invalidProp = CmsUtilities.GetCustomProperty(p.CustomProperties, "Metadata invalid");
                StringBuilder invalidText = new StringBuilder();

                // If there are IPSV preferred terms, get non-preferred terms 
                if (ipsvProp != null && ipsvProp.Value.Length > 0)
                {
                    CustomProperty ipsvNonProp = CmsUtilities.GetCustomProperty(p.CustomProperties, "IPSV non-preferred");
                    StringBuilder ipsvText = new StringBuilder();
                    StringBuilder ipsvNonText = new StringBuilder();

                    string[] ipsvTerms = ipsvProp.Value.Trim(new char[] { ';', ' ' }).Split(';');
                    foreach (string ipsvTerm in ipsvTerms)
                    {
                        EsdTermCollection terms = ipsvXml.GetTerms(ipsvTerm.Trim());
                        if (terms.Count > 0)
                        {
                            terms.AppendText(ipsvText);

                            if (ipsvNonProp != null)
                            {
                                EsdTermCollection nonPrefs = ipsvXml.GetNonPreferredTerms(terms[0].Id);
                                nonPrefs.AppendText(ipsvNonText);
                            }
                        }
                        else
                        {
                            if (invalidText.Length > 0) invalidText.Append("; ");
                            invalidText.Append(String.Format("IPSV: {0}", ipsvTerm.Trim()));
                        }
                    }

                    ipsvProp.Value = ipsvText.ToString();
                    if (ipsvNonProp != null) ipsvNonProp.Value = ipsvNonText.ToString();
                }


                // If there are LGSL terms, get the LGSL numbers 

                // Load XML for LGSL
                EsdControlledList lgslXml = EsdControlledList.GetControlledList("Lgsl");

                // Get custom properties for LGSL
                CustomProperty lgslProp = CmsUtilities.GetCustomProperty(p.CustomProperties, "LGSL");
                CustomProperty lgslNumsProp = CmsUtilities.GetCustomProperty(p.CustomProperties, "LGSL numbers");

                // StringBuilders to build validated property values
                StringBuilder lgslText = new StringBuilder();
                StringBuilder lgslNumsText = new StringBuilder();

                if (lgslProp != null && lgslProp.Value.Length > 0)
                {
                    string[] lgslTerms = lgslProp.Value.Trim(new char[] { ';', ' ' }).Split(';');
                    foreach (string lgslTerm in lgslTerms)
                    {
                        EsdTermCollection terms = lgslXml.GetTerms(lgslTerm.Trim());
                        if (terms.Count > 0)
                        {
                            terms.AppendText(lgslText);
                            terms.AppendIds(lgslNumsText);
                        }
                        else
                        {
                            if (invalidText.Length > 0) invalidText.Append("; ");
                            invalidText.Append(String.Format("LGSL: {0}", lgslTerm.Trim()));
                        }
                    }

                    lgslProp.Value = lgslText.ToString();
                    if (lgslNumsProp != null) lgslNumsProp.Value = lgslNumsText.ToString();
                }

                // save validation errors
                if (invalidProp != null) invalidProp.Value = invalidText.ToString();
            }
        }

        #endregion // Apply rules on save/submit/approve

        #region Estimating export size

        /// <summary>
        /// Records details of the approval for later analysis
        /// </summary>
        /// <param name="posting">The posting.</param>
        [Obsolete("This doesn't do anything any more...")]
        public static void RecordApproval(Posting posting)
        {
        }

        #endregion // Estimating export size

        #region Work with CMS errors

        /// <summary>
        /// Handles errors thrown by CMS
        /// </summary>
        public static void HandleCmsErrors()
        {
            HttpRequest request = HttpContext.Current.Request;
            HttpServerUtility server = HttpContext.Current.Server;

            // Is this a request for a CMS resource to which the user doesn't have permission?
            // Most likely cause is a link to an unpublished page. If so, email more useful error.
            Exception ex = server.GetLastError().InnerException;
            if (ex.GetType() == typeof(COMException) &&
                ex.Message == "The current user does not have rights to the requested item." &&
                request.QueryString["NRMODE"] != null &&
                request.QueryString["NRMODE"] != "Published")
            {
                // Stop the error bubbling further
                server.ClearError();

                // Send a more informative email
                var securityConfig = ConfigurationManager.GetSection("EsccWebTeam.Cms/SecuritySettings") as NameValueCollection;
                if (securityConfig == null || String.IsNullOrEmpty(securityConfig["ErrorNotificationEmail"]))
                {
                    throw new ConfigurationErrorsException("The 'ErrorNotificationEmail' setting in the EsccWebTeam.Cms/SecuritySettings of web.config was not found");
                }
                if (String.IsNullOrEmpty(securityConfig["BaseUrlForEditing"]))
                {
                    throw new ConfigurationErrorsException("The 'BaseUrlForEditing' setting in the EsccWebTeam.Cms/SecuritySettings of web.config was not found");
                }

                var sb = new StringBuilder();
                sb.Append("Someone has clicked on a link to an unpublished page on the public website.").Append(Environment.NewLine).Append(Environment.NewLine);
                if (request.UrlReferrer != null)
                {
                    sb.Append("The link appears on this page:").Append(Environment.NewLine).Append(Environment.NewLine);
                    sb.Append(request.UrlReferrer).Append(Environment.NewLine).Append(Environment.NewLine);
                }
                sb.Append("The link leads to this page:").Append(Environment.NewLine).Append(Environment.NewLine);
                sb.Append(securityConfig["BaseUrlForEditing"]).Append(request.Url.PathAndQuery).Append(Environment.NewLine).Append(Environment.NewLine);

                var email = new MailMessage(Environment.MachineName + "@eastsussex.gov.uk", securityConfig["ErrorNotificationEmail"]);
                email.Subject = "Broken CMS link on public website";
                email.Body = sb.ToString();
                var smtp = new SmtpClient();
                smtp.Send(email);

                // Show the user the published version of the page
                try
                {
                    HttpContext.Current.Response.StatusCode = 301;
                    HttpContext.Current.Response.Status = "301 Moved Permanently";
                    HttpContext.Current.Response.AddHeader("Location", request.Url.PathAndQuery.Replace("NRMODE=Unpublished", "NRMODE=Published").Replace("NRMODE=Update", "NRMODE=Published"));
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
            }
        }

        #endregion

        private static Collection<string> topLevelUrls;

        /// <summary>
        /// We're using "Map channel names to host header names" but not the style of link it creates, so correct for it
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string FixHostHeaderLinks(string text)
        {
            foreach (string url in CmsUtilities.TopLevelUrls)
            {
                text = text.Replace("http://" + url + "/", "/" + url + "/");
            }
            return text;
        }

        /// <summary>
        /// Uppercase the first letter after an HTML tag
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private static string FirstLetterToUppercase_MatchEvaluator(Match m)
        {
            return m.Groups["tag"].Value + m.Groups["firstletter"].Value.ToUpper(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// CMS sometimes omits the <p></p> tag around paragraphs. This restores it.
        /// </summary>
        /// <param name="text">XHTML string to parse and correct</param>
        public static string ShouldBePara(string text)
        {
            if (text.Length > 0)
            {
                // add <p> element around one-paragraph placeholders
                if (
                    text.ToLower().IndexOf("<p>") == -1 &&
                    text.ToLower().IndexOf("<p ") == -1
                    )
                {
                    text = "<p>" + text + "</p>";
                }

                // add <p> element to first para of multi-para placeholder where it is missing
                string firstTwo = text.Trim().Substring(0, 2);
                if (text.ToLower().IndexOf("<p>") > 0 &&
                    firstTwo != "<p" &&
                    firstTwo != "<h" &&
                    text.IndexOf("\n") > -1)
                {
                    text = "<p>" + text;
                    text = text.Substring(0, text.IndexOf("\n") - 1) + "</p>" + text.Substring(text.IndexOf("\n"));
                }

                // tidy up side-effect: para around block elements
                text = Regex.Replace(text, @"<p>\s*<(ul|ol|h1|h2|h3|h4|h5|h6|blockquote)( [^>]+)?>", "<$1$2>");
                text = Regex.Replace(text, @"<(/?)(ul|ol|h1|h2|h3|h4|h5|h6|blockquote)( [^>]+)?>\s*</p>", "<$1$2$3>");

                // or just a para on its own
                if (text.Trim() == "<p>") text = String.Empty;
            }

            return text;
        }

        /// <summary>
        /// Delegate for Regex used in TidyXhtml, used to lowercase XHTML elements
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private static string ConvertElementsToLowercase(Match m)
        {
            return "<" + m.Groups["slash"].Value + m.Groups["element"].Value.ToLower() + m.Groups["attributes"].Value + ">";
        }

        /// <summary>
        /// Delegate for Regex used in TidyXhtml, used to strip all attributes from a tag except the class attribute
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private static string KeepOnlyClassAttribute_MatchEvaluator(Match match)
        {
            // Parse attributes into a collection
            Dictionary<string, string> singleValueAttributes = new Dictionary<string, string>();
            Dictionary<string, List<string>> multiValueAttributes = new Dictionary<string, List<string>>();
            XmlFragmentParser.ParseAttributes(match.Groups["Attributes"].ToString(), singleValueAttributes, multiValueAttributes, XmlFragmentParser.XhtmlMultiValuedAttributes, new string[] { " " }, null);

            // Throw away all attributes except class, and for class throw away anything not in the whitelist
            singleValueAttributes.Clear();
            var filteredClasses = new List<string>();

            var config = ConfigurationManager.GetSection("EsccWebTeam.Cms/GeneralSettings") as NameValueCollection;
            var whiteList = new List<string>((config == null || String.IsNullOrEmpty(config["AllowedClasses"])) ? null : config["AllowedClasses"].Split(';'));

            foreach (string key in multiValueAttributes.Keys)
            {
                if (key == "class")
                {
                    foreach (string className in multiValueAttributes["class"])
                    {
                        if (whiteList.Contains(className))
                        {
                            filteredClasses.Add(className);
                        }
                    }
                }
                else
                {
                    multiValueAttributes.Remove(key);
                }
            }
            multiValueAttributes["class"] = filteredClasses;
            return XmlFragmentParser.RebuildTag(match.Groups["Tag"].Value, singleValueAttributes, multiValueAttributes);
        }

        /// <summary>
        /// When a document is submitted, tidy the XHTML produced by CMS.
        /// </summary>
        /// <param name="posting">The Posting being submitted</param>
        public static void TidyXhtml(Posting posting)
        {
            if (posting != null)
            {
                foreach (object ph in posting.Placeholders)
                {
                    if (ph is HtmlPlaceholder)
                    {
                        HtmlPlaceholder XhtmlPh = ph as HtmlPlaceholder;
                        XhtmlPh.Html = CmsUtilities.TidyXhtml(XhtmlPh.Html);
                    }
                }

            }
        }

        /// <summary>
        /// Combine the methods of tidying the XHTML produced by CMS into one method, adding more recent enhacements
        /// </summary>
        /// <param name="text">XHTML to tidy</param>
        /// <returns>TIdied XHTML</returns>
        public static string TidyXhtml(string text)
        {
            // get rid of non-breaking spaces
            text = text.Replace("&nbsp;", " ");

            // get rid of irrelevant Word round-tripping XML
            text = Regex.Replace(text, @"</?(o|v|w):[a-zA-Z0-9-]+(\W+[a-z:]+=[^>]+)*>", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<\?xml[^>]+>", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</?st1:[^>]+>", String.Empty, RegexOptions.IgnoreCase);

            // get rid of unwanted elements and attributes (mostly Word styling)
            text = Regex.Replace(text, @"<(li|h1|h2|h3|h4|h5|h6|br|blockquote)\W+[a-z:]+=[^>]+>", "<$1>", RegexOptions.IgnoreCase); // remove attributes
            text = Regex.Replace(text, @"<(?<Tag>ul|p|div)(?<Attributes> [^>]*)>", KeepOnlyClassAttribute_MatchEvaluator, RegexOptions.Singleline & RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<(/?)(span|u|s|strike|address|abbr|dir|sup)(\s[^>]+)*>", String.Empty, RegexOptions.IgnoreCase); // remove tags (note: dl, dt, dd not here because used by links list template; div used for blockquotes)
            text = Regex.Replace(text, @"<(/?)b(\s[^>]+)*>", "<$1strong>", RegexOptions.IgnoreCase); // change b to strong
            text = Regex.Replace(text, @"<(/?)i(\s[^>]+)*>", "<$1em>", RegexOptions.IgnoreCase); // change i to em

            // fix xhtml
            text = text.Replace("<br>", "<br />");

            // get rid of unwanted spacing
            text = Regex.Replace(text, @"<p>\s?</p>", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<p>\s+<p>", "<p>", RegexOptions.IgnoreCase);
            text = text.Trim();

            // Fix problem where Word lists pasted into TinyMCE can end up as:
            //
            // <li><br />
            // <div class="MsoNormal">text here</div>
            // </li>
            //
            // Class has been removed at this point by KeepOnlyClassAttribute_MatchEvaluator 
            text = Regex.Replace(text, "<li>\\s*(<br />)+", "<li>");
            text = Regex.Replace(text, "<li>\\s*<div>\\s*", "<li>");
            text = Regex.Replace(text, "\\s*</div>\\s*</li>", "</li>");

            // convert elements to lowercase
            text = Regex.Replace(text, @"<(?<slash>/?)(?<element>[A-Z0-9]+)(?<attributes>\s[^>]+)*>", new MatchEvaluator(ConvertElementsToLowercase), RegexOptions.IgnoreCase);

            // close list items
            text = Regex.Replace(text, @"(\s*)<li>", "</li>$1<li>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "</li></li>", "</li>"); // correct above where fixed already by CmsUtilities.ShouldBeUnorderedList
            text = Regex.Replace(text, "<(u|o)l([^>]*)></li>", "<$1l$2>", RegexOptions.IgnoreCase);

            // get rid of irrelevant DreamWeaver/Studio HTML
            text = text.Replace("<!-- #EndEditable -->", String.Empty);
            text = text.Replace("<!--StartFragment -->", String.Empty);

            // strip targets, names and styles
            text = Regex.Replace(text, @" target=[^>][a-z_]+[^>]", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @" name=\\?[^>][a-z0-9_]+\\?[^>]", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, " style=\"" + @"[A-Za-z0-9_:;\- #%,\.]+" + "\"", String.Empty, RegexOptions.IgnoreCase);

            // strip extra attributes inserted by TinyMCE
            text = Regex.Replace(text, " data-mce-href=\".*?\"", String.Empty, RegexOptions.IgnoreCase);

            // Strip tags which require attributes if they don't have any attributes (it happens)
            text = Regex.Replace(text, @"<a\s*>([^<]*)</a>", "$1", RegexOptions.IgnoreCase);

            // Strip empty tags which should not be empty.
            // Potential match of mixed tags (eg matching <h1></h2>) is acceptable 
            // because these tags shouldn't be incorrectly nested anyway, so it won't
            // make the problem any worse than it already was
            text = Regex.Replace(text, @"<(h1|h2|h3|h4|h5|h6|a|p|b|strong|i|em|blockquote|ul|ol|li)(\s[^>]+)*>(\s*|<br />)</(h1|h2|h3|h4|h5|h6|a|p|b|strong|i|em|blockquote|ul|ol|li)>", "", RegexOptions.IgnoreCase);

            // Get rid of Word bullets
            text = text.Replace("&#183; ", String.Empty);
            text = text.Replace("&middot; ", String.Empty);

            /*** standard phrases are in web.config ***/
            var replaceConfig = ConfigurationManager.GetSection("EsccWebTeam.Cms/ReplaceOnSave") as NameValueCollection;
            if (replaceConfig != null)
            {
                foreach (string pattern in replaceConfig)
                {
                    text = text.Replace(pattern, replaceConfig[pattern]);
                }
            }

            replaceConfig = ConfigurationManager.GetSection("EsccWebTeam.Cms/ReplaceOnSaveRegex") as NameValueCollection;
            if (replaceConfig != null)
            {
                foreach (string pattern in replaceConfig)
                {
                    text = Regex.Replace(text, pattern, replaceConfig[pattern]);
                }
            }

            /*** bug fixes, ones with side-effects, and ones which use server-side info remain in the code ***/

            // fix bug which removes spaces after links
            text = Regex.Replace(text, @"</a>([a-z0-9(<-])", "</a> $1", RegexOptions.IgnoreCase);

            // quotes - replace curly with standard because they cause problems with reading the page as XML
            text = text.Replace("“", "\"");
            text = text.Replace("&#8217;", "'");
            text = text.Replace("&lsquo;", "'");
            text = text.Replace("&rsquo;", "'");

            // Move fullstops outside links
            text = text.Replace(".</a>", "</a>.");

            // en dashes
            text = Regex.Replace(text, @"([a-z0-9>)])\s-\s([(<a-z0-9])", "$1 &#8211; $2", RegexOptions.IgnoreCase); // replace between clauses
            text = Regex.Replace(text, @"([0-9])-([0-9])", "$1&#8211;$2"); // replace numerical ranges
            text = text.Replace("&ndash;", "&#8211;"); // so the page can be read as XML

            // undo previous within urls
            const string urlNDashPattern = @"([a-z]:/)?(/?)([^\s]*)([0-9]+)&\#8211;([0-9])";
            while (Regex.IsMatch(text, urlNDashPattern, RegexOptions.IgnoreCase))
                text = Regex.Replace(text, urlNDashPattern, "$1$2$3$4-$5", RegexOptions.IgnoreCase);

            // Hide email addresses from at least the dumber spam-bots by converting all characters to entity references
            text = Regex.Replace(text, @"(?<emailAddress>mailto:[a-z0-9.-_]+@[a-z0-9.-_]+.>[a-z0-9.-_]+@[a-z0-9.-_]+)</a>", new MatchEvaluator(ConvertEmailToEntities), RegexOptions.IgnoreCase);

            // remove internal server names
            text = CmsUtilities.FixHostHeaderLinks(text);

            // headings should start with a captial letter
            text = Regex.Replace(text, "(?<tag><h[1|2|3|4|5|6]>)(?<firstletter>[a-z])", FirstLetterToUppercase_MatchEvaluator);


            // TinyMCE converts internal links to relative links. Unfortunately they're relative to edit mode, which by default is always in /NR/exeres/, 
            // so they always start with ../../top-level-folder/. If using an alternative page layout then the edit mode uses the template path, which
            // could be a flexible number of levels down the hierarchy. That's all fine if you happen to be the same number of levels down in the site, 
            // but otherwise it breaks the links. This fix removes the initial ../ as many times as needed, leaving a link that's relative to the root of the site.
            foreach (string url in CmsUtilities.TopLevelUrls)
            {
                int lengthBefore;
                do
                {
                    lengthBefore = text.Length;
                    text = text.Replace("../" + url + "/", "/" + url + "/"); // all but the last one
                    text = text.Replace("..//" + url + "/", "/" + url + "/"); // the last one
                }
                while (text.Length != lengthBefore);
            }

            // Also fix a similar issue when TinyMCE links to items in the Resource Gallery
            text = text.Replace("../rdonlyres/", "/NR/rdonlyres/");

            // Recognise and replace links to content in unpublished mode
            text = Regex.Replace(text, " href=\"(?<URL>[^\"]+NRMODE=Unpublished[^\"]+)\"", RewriteUnpublishedLinks_MatchEvaluator, RegexOptions.Singleline);

            // ellipsis - this takes several passes as the match stops when it finds 3 rather than greedily matching all consecutive . characters
            // Must be after the TinyMCE ../../ fix above, otherwise it changes those URLs
            text = text.Replace("…", "&#8230;");
            text = Regex.Replace(text, @"\.{2,}", "&#8230;");
            text = Regex.Replace(text, @"&#8230;\.+", "&#8230;");
            text = Regex.Replace(text, @"(&#8230;){2,}", "&#8230;");

            return text;
        }


        /// <summary>
        /// Rewrites links to unpublished postings as their published equivalent, if available.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private static string RewriteUnpublishedLinks_MatchEvaluator(Match match)
        {
            var link = match.Groups["URL"].Value;
            var guid = CmsUtilities.GetGuidFromUrl(link);
            if (!String.IsNullOrEmpty(guid))
            {
                // If it's a link to an anchor on a CMS page, that should be the current page so just keep the anchor
                var pos = link.IndexOf("#", StringComparison.Ordinal);
                if (pos > -1)
                {
                    link = link.Substring(pos);
                }
                else
                {
                    // Otherwise use the published URL of the page
                    var posting = CmsHttpContext.Current.Searches.GetByGuid(guid) as Posting;
                    if (posting != null) link = CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished);
                }
            }
            return " href=\"" + link + "\"";
        }

        /// <summary>
        /// Delegate for Regex used in TidyXhtml, used to obfuscate email addresses
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private static string ConvertEmailToEntities(Match m)
        {
            string text = m.Groups["emailAddress"].Value.ToLower().Replace(".", "&#0046;");
            text = text.Replace(":", "&#0058;");
            text = text.Replace("@", "&#0064;");
            text = text.Replace("a", "&#0097;");
            text = text.Replace("b", "&#0098;");
            text = text.Replace("c", "&#0099;");
            text = text.Replace("d", "&#0100;");
            text = text.Replace("e", "&#0101;");
            text = text.Replace("f", "&#0102;");
            text = text.Replace("g", "&#0103;");
            text = text.Replace("h", "&#0104;");
            text = text.Replace("i", "&#0105;");
            text = text.Replace("j", "&#0106;");
            text = text.Replace("k", "&#0107;");
            text = text.Replace("l", "&#0108;");
            text = text.Replace("m", "&#0109;");
            text = text.Replace("n", "&#0110;");
            text = text.Replace("o", "&#0111;");
            text = text.Replace("p", "&#0112;");
            text = text.Replace("q", "&#0113;");
            text = text.Replace("r", "&#0114;");
            text = text.Replace("s", "&#0115;");
            text = text.Replace("t", "&#0116;");
            text = text.Replace("u", "&#0117;");
            text = text.Replace("v", "&#0118;");
            text = text.Replace("w", "&#0119;");
            text = text.Replace("x", "&#0120;");
            text = text.Replace("y", "&#0121;");
            text = text.Replace("z", "&#0122;");
            return text + "</a>";
        }

        /// <summary>
        /// Detect whether there is any content in a placeholder
        /// </summary>
        /// <param name="ph"></param>
        /// <returns></returns>
        public static bool PlaceholderIsEmpty(object ph)
        {

            // try it as an HTML placeholder
            HtmlPlaceholder phHtml = ph as HtmlPlaceholder;
            if (phHtml != null)
            {
                return (
                    (phHtml.Html == null) ||
                    (phHtml.Html.Trim() == "") ||
                    (phHtml.Html.Trim().ToLower() == "&nbsp;") ||
                    (phHtml.Html.Trim().ToLower() == "<p>&nbsp;</p>") ||
                    (Regex.IsMatch(phHtml.Html.Trim(), @"^<p><a\s?[^>]*></a></p>$", RegexOptions.IgnoreCase)) ||
                    (Regex.IsMatch(phHtml.Html.Trim(), @"^<ul>\s*<li>\s*</li>\s*</ul>$", RegexOptions.IgnoreCase))
                    );
            }

            // try it as an Image placeholder
            ImagePlaceholder phImage = ph as ImagePlaceholder;
            if (phImage != null)
            {
                return (
                    phImage.Src == "" ||
                    phImage.Alt == "" ||
                    phImage.Src == null ||
                    phImage.Alt == null
                    );
            }

            // try it as Attachment placeholder
            AttachmentPlaceholder phAttach = ph as AttachmentPlaceholder;
            if (phAttach != null)
            {
                return (phAttach.Url == "" || phAttach.Url == null);
            }

            // try it as XmlPlaceholder
            XmlPlaceholder phXml = ph as XmlPlaceholder;
            if (phXml != null)
            {
                if (phXml.XmlAsString.Length == 0) return true;
                else
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(phXml.XmlAsString);
                    return (xmlDoc.DocumentElement == null || xmlDoc.DocumentElement.InnerXml.Length == 0);
                }
            }

            return true;
        }

        /// <summary>
        /// Returns an list of top-level folders/channels on the site
        /// </summary>
        public static Collection<string> TopLevelUrls
        {
            get
            {
                if (CmsUtilities.topLevelUrls == null)
                {
                    // create a list of top level urls
                    CmsUtilities.topLevelUrls = new Collection<string>();
                    Channel rootChannel = null;
                    if (HttpContext.Current != null)
                    {
                        rootChannel = CmsHttpContext.Current.RootChannel;
                    }
                    else
                    {
                        var con = new CmsApplicationContext();
                        con.AuthenticateAsCurrentUser();
                        rootChannel = con.RootChannel;
                    }

                    foreach (Channel c in rootChannel.Channels)
                    {
                        CmsUtilities.topLevelUrls.Add(c.Name);
                    }
                    foreach (Posting p in rootChannel.Postings)
                    {
                        CmsUtilities.topLevelUrls.Add(p.Name + ".htm");
                    }


                }

                return CmsUtilities.topLevelUrls;
            }
        }

        /// <summary>
        /// With "Map channel names to host header names" turned on, urls point to the top-level channel as if it were the server name. This fixes them to point to the current server.
        /// </summary>
        /// <param name="url">The UrlPublished property of a Posting</param>
        /// <returns>A corrected URL string</returns>
        public static string CorrectPublishedUrl(string url)
        {
            if (url.StartsWith("http://") && url.Length > 7)
            {
                string domain = (url.IndexOf("/", 7) != -1) ? url.Substring(7, url.IndexOf("/", 7) - 7) : url.Substring(7);
                if (CmsUtilities.TopLevelUrls.Contains(domain))
                {
                    url = url.Substring(6); // make it a virtual url starting with /
                    int qPos = url.IndexOf("?");
                    if (qPos > -1) url = url.Substring(0, qPos); // trim query string
                }
            }
            else if (url.StartsWith("https://") && url.Length > 8)
            {
                string domain = (url.IndexOf("/", 8) != -1) ? url.Substring(8, url.IndexOf("/", 8) - 8) : url.Substring(8);
                if (CmsUtilities.TopLevelUrls.Contains(domain))
                {
                    url = url.Substring(7); // make it a virtual url starting with /
                    int qPos = url.IndexOf("?");
                    if (qPos > -1) url = url.Substring(0, qPos); // trim query string	
                }
            }

            return url;
        }

        /// <summary>
        /// The unpublished URL from CMS sometimes returns a version which doesn't show the edit console. This fixes it.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public static string CorrectUnpublishedUrl(string url)
        {
            if (url.EndsWith(".htm?NRMODE=Unpublished", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(0, url.Length - 23) + ",frameless.htm?NRMODE=Unpublished&WBCMODE=PresentationUnpublished&wbc_purpose=Basic";
            }
            else if (url.Contains("WBCMODE=PresentationUnpublished") && !url.Contains("NRMODE=Unpublished"))
            {
                // Posting.UrlModePublished for expired pages is missing this parameter
                url = url + "&NRMODE=Unpublished";
            }
            else if (Regex.IsMatch(url, @"^/NR/exeres/[A-Z0-9]{8}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{12}\.htm$"))
            {
                url = url.Substring(0, url.Length - 4) + ",frameless.htm?NRMODE=Unpublished&WBCMODE=PresentationUnpublished&wbc_purpose=Basic";
            }
            return url;
        }

        #region Images and captions

        /// <summary>
        /// Pulls out Alt text from image placeholder and adds it below with a caption
        /// </summary>
        /// <param name="placeholderId"></param>
        /// <param name="container"></param>
        public static void DisplayImageTextAsCaption(string placeholderId, HtmlGenericControl container)
        {
            // get a CMS context
            CmsHttpContext cms = CmsHttpContext.Current;

            // Check for anything that could be null and fail silently, because we get occasional NullReferenceExceptions, usually from bots
            // It's probably cms.Posting that's null as that can happen when bots hit the site
            if (cms == null || cms.Posting == null || cms.Posting.Placeholders == null || cms.Posting.Placeholders[placeholderId] == null || container == null)
            {
                return;
            }

            // add caption
            if (!CmsUtilities.PlaceholderIsEmpty(cms.Posting.Placeholders[placeholderId]))
            {
                ImagePlaceholder ph = cms.Posting.Placeholders[placeholderId] as ImagePlaceholder;
                container.Controls.Add(new LiteralControl(ph.Alt));

                // TODO: Set container width based on image width, to force captions to wrap and to correct display in Mozilla
            }
        }


        /// <summary>
        /// Shows the caption.
        /// </summary>
        /// <param name="imagePlaceholder">The image placeholder.</param>
        /// <param name="altAsCaptionPlaceholder">The alt as caption placeholder.</param>
        /// <param name="captionControl">The caption control.</param>
        /// <param name="altTextControl">The alt text control.</param>
        public static void ShowCaption(Placeholder imagePlaceholder, Placeholder altAsCaptionPlaceholder, Control captionControl, HtmlContainerControl altTextControl)
        {
            bool useAltAsCaption = CheckBoxPlaceholderControl.GetValue(altAsCaptionPlaceholder as XmlPlaceholder);
            if (useAltAsCaption)
            {
                ImagePlaceholder image = imagePlaceholder as ImagePlaceholder;
                if (image != null) altTextControl.InnerText = HttpUtility.HtmlDecode(image.Alt);
            }
            //copy alt text
            captionControl.Visible = (!useAltAsCaption || CmsUtilities.IsEditing);
            altTextControl.Visible = (useAltAsCaption || CmsUtilities.IsEditing);
        }

        #endregion  // Images and captions

        /// <summary>
        /// Where an entire placeholder should be an unordered list of links, this prevents the author from having to remember.
        /// </summary>
        /// <param name="html">XHTML to be converted to an unordered list. Elements should already be converted to lowercase.</param>
        /// <param name="listClass">The list class.</param>
        /// <returns>Modifed XHTML</returns>
        public static string ShouldBeUnorderedList(string html, string listClass)
        {
            MatchCollection matches = Regex.Matches(html, "(?<tag><a [^>]*>)(?<linktext>.*?)</a>", RegexOptions.Singleline & RegexOptions.IgnoreCase);
            if (matches.Count == 0) return String.Empty; // we want a list of links so, no links, no list

            StringBuilder list = new StringBuilder("<ul");
            if (!String.IsNullOrEmpty(listClass)) list.Append(" class=\"").Append(listClass).Append("\"");
            list.Append(">");
            list.Append(Environment.NewLine);

            foreach (Match m in matches)
            {
                list.Append("<li>").Append(m.Groups["tag"].Value).Append(Case.FirstToUpper(m.Groups["linktext"].Value)).Append("</a></li>").Append(Environment.NewLine);
            }

            list.Append("</ul>");
            return list.ToString();
        }

        /// <summary>
        /// Automatically format links found in a placeholder as two lists of links, with classes applied to allow display as columns.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.ContentManagement.WebControls.PlaceholderControlSavingEventArgs"/> instance containing the event data.</param>
        /// <exception cref="ArgumentException">Thrown if the type of placeholder control is not compatible</exception>
        public static void PlaceholderIsUnorderedListsOfLinks_SavingContent(object sender, PlaceholderControlSavingEventArgs e)
        {
            string html;
            var ph2 = sender as RichHtmlPlaceholderControl;

            if (ph2 != null) html = ph2.EditorHtml;
            else throw new ArgumentException("PlaceholderIsUnorderedListsOfLinks_SavingContent should be attached to a EsccWebTeam.Cms.Placeholders.RichHtmlPlaceholderControl. The type used was " + sender.GetType().ToString(), "sender");

            int firstListPos = html.IndexOf("<ul");
            int lastListPos = html.LastIndexOf("<ul");
            if (firstListPos > -1 && firstListPos != lastListPos)
            {
                // multiple lists, find the join
                int firstListEnds = html.IndexOf("</ul>");
                if (firstListEnds == -1) return; // screwed-up HTML, give up before we make a mess!
                string firstHalf = html.Substring(0, firstListEnds);
                string secondHalf = html.Substring(firstListEnds);

                // grab links, style as lists
                firstHalf = CmsUtilities.ShouldBeUnorderedList(firstHalf, "first");
                secondHalf = CmsUtilities.ShouldBeUnorderedList(secondHalf, "second");

                // and put back together
                ph2.EditorHtml = firstHalf + secondHalf;
            }
            else
            {
                // oherwise grab links and distribute into two lists
                var listHtml = CmsUtilities.ShouldBeUnorderedList(html, String.Empty);
                var listItems = Regex.Matches(listHtml, "(<li[^>]*>.*?</li>)");
                if (listItems.Count > 1)
                {
                    int itemsPerList = (int)Math.Floor(((decimal)(listItems.Count / 2)));
                    int extraItem = (listItems.Count % 2);

                    var lists = new StringBuilder("<ul class=\"first\">");
                    for (var i = 0; i < (itemsPerList + extraItem); i++) lists.Append(listItems[i].Value);
                    lists.Append("</ul><ul class=\"second\">");
                    for (var i = (itemsPerList + extraItem); i < ((itemsPerList * 2) + extraItem); i++) lists.Append(listItems[i].Value);
                    lists.Append("</ul>");

                    ph2.EditorHtml = lists.ToString();
                }
                else
                {
                    // unless there's only one link, then leave it in its one list
                    ph2.EditorHtml = listHtml;
                }
            }
        }

        /// <summary>
        /// Parses the links to a given host in some HTML and applies a class.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <param name="host">The host.</param>
        /// <param name="className">Name of the class.</param>
        /// <returns></returns>
        public static string ParseLinksByHostAndApplyClass(string html, string host, string className)
        {
            return Regex.Replace(html, " href=\"(https?://(www.)?" + host + "[^\"]+)\"", " class=\"" + className + "\" href=\"$1\"");
        }


        /// <summary>
        /// Find all controls within a container control that are of a given type <c>T</c>
        /// </summary>
        /// <param name="containerControl"></param>
        /// <returns></returns>
        internal static List<Control> FindControlsOfType<T>(Control containerControl)
        {
            var foundControls = new List<Control>();
            if (containerControl != null)
            {
                foreach (Control childControl in containerControl.Controls)
                {
                    if (childControl is T)
                    {
                        foundControls.Add(childControl);
                    }
                    foundControls.AddRange(FindControlsOfType<T>(childControl));
                }
            }
            return foundControls;
        }

        #region Rewrite elibrary links

        /// <summary>
        /// Rewrites proxied elibrary links in a string of HTML to avoid a redirect and enable Google In-Page Analytics to work
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns></returns>
        public static string ParseAndRewriteElibraryLinks(string html)
        {
            return Regex.Replace(html, "<a (?<attributes1>[^>]*)href=\"(?<url>[a-z://.]*/libraries/elibrary/go.aspx[^>]*)\"(?<attributes2>[^>]*)>", new MatchEvaluator(CmsUtilities.RewriteElibraryLinks_MatchEvaluator), RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Helper for <see cref="ParseAndRewriteElibraryLinks"/>
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private static string RewriteElibraryLinks_MatchEvaluator(Match match)
        {
            var parsedUri = new Uri(HttpUtility.HtmlDecode(match.Groups["url"].Value), UriKind.RelativeOrAbsolute);
            parsedUri = Iri.MakeAbsolute(parsedUri);
            var queryString = Iri.SplitQueryString(parsedUri.Query);
            var rewrittenUrl = CmsUtilities.RewriteElibraryLink(queryString);
            return "<a " + match.Groups["attributes1"].Value + "href=\"" + rewrittenUrl + "\"" + match.Groups["attributes2"].Value + ">";
        }

        /// <summary>
        /// Uses a set of data about an elibrary search to build up an elibrary URL
        /// </summary>
        /// <param name="queryValues">The query values.</param>
        /// <returns></returns>
        /// <remarks>
        /// Created as a way to handle the need to mass-update elibrary links as their format changed. Link to an unchanging intermediate format then,
        /// when the target links change, we can simply update the code here in one go rather than modifying every link.
        /// </remarks>
        public static string RewriteElibraryLink(Dictionary<string, string> queryValues)
        {
            string isbn = queryValues.ContainsKey("i") ? queryValues["i"] : String.Empty;
            if (!String.IsNullOrEmpty(isbn)) isbn = isbn.ToUpperInvariant(); // could be old alpha-numeric RCN which must be uppercase

            var targetUrl = String.Empty;
            var elibraryBaseUrl = "https://e-library.eastsussex.gov.uk";

            // Title search...
            if (queryValues.ContainsKey("a") && queryValues["a"] == "title")
            {
                // ...for one title
                if (queryValues.ContainsKey("q") && !String.IsNullOrEmpty(isbn))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY4_NAME=SBN&ENTRY4=" + isbn;
                }
                // ...for titles matching x
                else if (queryValues.ContainsKey("q"))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY_NAME=TIH&ENTRY=" + queryValues["q"] + "&ENTRY_TYPE=K&NRECS=20&SEARCH_FORM=%2Fcgi-bin%2Fspydus.exe%2FMSGTRN%2FOPAC%2FTITLE&CF=GEN&ISGLB=0&GQ=" + queryValues["q"];
                }
                // title search screen
                else
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/TITLE";
                }
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "editions" && queryValues.ContainsKey("q"))
            {
                // ... for all editions of a particular title (eg paperback, hardback and large print)
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY_NAME=TIH&ENTRY=" + queryValues["q"] + "&ENTRY_TYPE=K&NRECS=20&SEARCH_FORM=%2Fcgi-bin%2Fspydus.exe%2FMSGTRN%2FOPAC%2FTITLE&CF=GEN&ISGLB=0&GQ=" + queryValues["q"];
            }
            // Keyword search...
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "key")
            {
                // ... for one item
                if (queryValues.ContainsKey("q") && !String.IsNullOrEmpty(isbn))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY4_NAME=SBN&ENTRY4=" + isbn;
                }
                // ... for items matching x
                else if (queryValues.ContainsKey("q"))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY_NAME=BS&ENTRY=" + queryValues["q"] + "&ENTRY_TYPE=K&NRECS=20&SORTS=HBT.SOVR&SEARCH_FORM=%2Fcgi-bin%2Fspydus.exe%2FMSGTRN%2FOPAC%2FBSEARCH&CF=GEN&ISGLB=0&GQ=" + queryValues["q"];
                }
                // keyword search screen
                else
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/BSEARCH";
                }
            }
            // Author search...
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "authors" && queryValues.ContainsKey("q"))
            {
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY_NAME=AUH&ENTRY=" + queryValues["q"] + "&ENTRY_TYPE=K&NRECS=20&SEARCH_FORM=%2Fcgi-bin%2Fspydus.exe%2FMSGTRN%2FOPAC%2FAUTHOR&CF=GEN&ISGLB=0";
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "author")
            {
                // ... for one item
                if (queryValues.ContainsKey("q") && !String.IsNullOrEmpty(isbn))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY4_NAME=SBN&ENTRY4=" + isbn;
                }
                // ... for all items by one author
                else if (queryValues.ContainsKey("q"))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY_NAME=AUH&ENTRY=" + queryValues["q"] + "&ENTRY_TYPE=K&NRECS=20&SEARCH_FORM=%2Fcgi-bin%2Fspydus.exe%2FMSGTRN%2FOPAC%2FAUTHOR&CF=GEN&ISGLB=0&GQ=" + queryValues["q"];
                }
                // author search screen
                else
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/AUTHOR";
                }
            }
            // ISBN search...
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "isbn")
            {
                // ...for one item
                if (queryValues.ContainsKey("q") && !String.IsNullOrEmpty(isbn))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY4_NAME=SBN&ENTRY4=" + isbn;
                }
                // ... for items matching x (apparently a book issued, withdrawn and re-issued can have the same ISBN)
                else if (queryValues.ContainsKey("q"))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY_NAME=BS&ENTRY=" + queryValues["q"] + "&ENTRY_TYPE=K&NRECS=20&SORTS=HBT.SOVR&SEARCH_FORM=%2Fcgi-bin%2Fspydus.exe%2FMSGTRN%2FOPAC%2FBSEARCH&CF=GEN&ISGLB=0&GQ=" + queryValues["q"];
                }
                // advanced search screen
                else
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/COMB";
                }
            }
            // A series of books...
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "series")
            {
                // ...one item in the series
                if (queryValues.ContainsKey("q") && !String.IsNullOrEmpty(isbn))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY4_NAME=SBN&ENTRY4=" + isbn;
                }
                //... the whole series
                else if (queryValues.ContainsKey("q"))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY3_NAME=SE&ENTRY3=" + queryValues["q"];
                }
            }
            // A subject of books by Dewey number...
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "class")
            {
                // ...for one item
                if (queryValues.ContainsKey("q") && !String.IsNullOrEmpty(isbn))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY4_NAME=SBN&ENTRY4=" + isbn;
                }
                // ...for a series of matching Dewey numbers (* wildcards can be used)
                else if (queryValues.ContainsKey("q"))
                {
                    targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BIBENQ?ENTRY4_NAME=DDC&ENTRY4=" + queryValues["q"];
                }
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "loans")
            {
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/LOGINB";
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "reservations")
            {
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/ENQ/OPAC/BRWENQ/3385697?QRY=%233385697&QRYTEXT=My%20details";
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "account")
            {
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/LOGINB";
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "personal")
            {
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/FULL/OPAC/BRWENQ/3385710/2352363,1?FMT=PD";
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "history")
            {
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/LOGINB";
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "reference")
            {
                targetUrl = elibraryBaseUrl + "/cgi-bin/spydus.exe/MSGTRN/OPAC/ODBS";
            }
            else if (queryValues.ContainsKey("a") && queryValues["a"] == "basket")
            {
                targetUrl = elibraryBaseUrl + @"/cgi-bin/spydus.exe/ENQ/OPAC/RSVCENQ/3385698?QRY=RVP01\2352363 - MAJOR:RVP18 %2B ((RVC01\0 - MINOR:08301) / (08301\< ((08301\< (RVP01\2352363 %2B MINOR:08301)) %2B MINOR:ITR05)))&QRYTEXT=Reservations Not Yet Available&SORTS=RVP.DTE&FMT=WR&SETLVL=SET&NRECS=30&SEARCH_FORM=/cgi-bin/spydus.exe/FULL/OPAC/BRWENQ/3385698/2352363,1";
            }
            // No-one should see this page so, if none of those match, redirect to the e-library home page
            else
            {
                targetUrl = elibraryBaseUrl + "/";
            }

            return targetUrl;
        }
        #endregion // Rewrite elibrary links

        #region Add file details to download links

        /// <summary>
        /// Regex pattern to recognise a download link within a string of HTML
        /// </summary>
        public static string DownloadLinkPattern
        {
            get
            {
                const string cmsUrl = "/NR/rdonlyres/[A-Fa-f0-9]{8,8}-[A-Fa-f0-9]{4,4}-[A-Fa-f0-9]{4,4}-[A-Fa-f0-9]{4,4}-[A-Fa-f0-9]{12,12}[^\"]+";
                const string pdfUrl = @"[A-Za-z0-9_\-/:\.?&=#%]+\." + "pdf";
                const string anythingExceptEndAnchor = "((?!</a>).)*";

                return "<a [^>]*href=\"(?<url>" + cmsUrl + "|" + pdfUrl + ")\"[^>]*>(?<linktext>" + anythingExceptEndAnchor + ")</a>";
            }
        }

        /// <summary>
        /// Display size and type of inline downloads linked within a string of HTML.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns></returns>
        public static string ParseAndRewriteDownloadLinks(string html)
        {
            return Regex.Replace(html, "(?<listitem><li>\\s*)?" + DownloadLinkPattern, new MatchEvaluator(DisplayDownloadDetail_MatchEvaluator), RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// For a link which looks like a Resource Gallery link, add the file details
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private static string DisplayDownloadDetail_MatchEvaluator(Match match)
        {
            // Build a standard document link control
            var linkControl = new FileLinkControl();
            linkControl.NavigateUrl = new Uri(match.Groups["url"].Value, UriKind.RelativeOrAbsolute);
            linkControl.InnerText = match.Groups["linktext"].Value;
            linkControl.ShowDetailsInsideLink = true;
            linkControl.UseDefaultClasses = false;
            linkControl.RecognisedFileTypesOnly = false;

            // For people that can edit the site, changing the link triggers the warning that there's an "unpublished" link on the page,
            // from Console.js in EsccWebTEam.Cms.WebAuthor project, so preserve the attribute which JavaScript can look for to know that the link is OK.
            if (match.Value.Contains(" data-unpublished=\"false\""))
            {
                linkControl.Attributes["data-unpublished"] = "false";
            }

            // If we've matched a link that looks like an item in the Resource Gallery see if we can find it, and get the type and size.
            string guid = CmsUtilities.GetGuidFromUrl(match.Groups["url"].Value);
            if (String.IsNullOrEmpty(guid))
            {
                var res = CmsHttpContext.Current.Searches.GetByGuid(guid) as Resource;
                if (res != null)
                {
                    linkControl.FileSize = res.Size;
                }
            }

            // Return link, with list item if captured
            if (!String.IsNullOrEmpty(match.Groups["listitem"].Value))
            {
                return "<li class=\"download\">" + linkControl.ToString();
            }
            else
            {
                return linkControl.ToString();
            }
        }

        #endregion // Add file details to download links
    }
}
