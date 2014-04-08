
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Microsoft.ApplicationBlocks.Data;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Utilities for tracking links from CMS pages
    /// </summary>
    public static class LinkTracker
    {
        /// <summary>
        /// Ensures the link tracker has a record of a posting and all its links
        /// </summary>
        /// <param name="context"></param>
        /// <param name="posting"></param>
        /// <param name="transaction"></param>
        public static void SaveLinksForPosting(CmsContext context, Posting posting, SqlTransaction transaction)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (posting == null) throw new ArgumentNullException("posting");

            var postingUrl = CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished);
            if (postingUrl.StartsWith("/training") ||
                postingUrl.StartsWith("/webauthors") ||
                postingUrl.StartsWith("/help/messages") ||
                postingUrl.StartsWith("/managewebsite") ||
                postingUrl.StartsWith("/recyclebin") ||
                postingUrl.StartsWith("/esccwebteam.cms.webauthor"))
            {
                // We're not interested in web author training and practice areas or the recycle bin. They will be a mess but they're not publicly visible.
                // Manage website isn't publicly visible either, and error pages are *supposed* to return error codes
                return;
            }

            // Clear down links from the page so we can add them from scratch
            SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, "usp_Link_DeleteAllFromPosting", new SqlParameter("@PostingGuid", posting.Guid));

            bool linksFound = false;

            // note who last submitted the page
            CustomProperty submittedBy = CmsUtilities.GetCustomProperty(posting.CustomProperties, "ESCC.LastSubmittedBy");
            string submittedByName = (submittedBy != null) ? submittedBy.Value : null;

            foreach (Placeholder ph in posting.Placeholders)
            {
                // Not interested in checking author notes
                if (ph.Name == "phDefAuthorNotes")
                {
                    continue;
                }

                // find links to channels and postings 
                string html = "";
                if (ph is HtmlPlaceholder)
                {
                    html = ((HtmlPlaceholder)ph).Html;
                }
                else if (ph is ImagePlaceholder)
                {
                    // create some HTML that will fit the regex which follows
                    html = "<a href=\"" + ((ImagePlaceholder)ph).Src + "\">" + ((ImagePlaceholder)ph).Alt + "</a>";
                    if (!String.IsNullOrEmpty(((ImagePlaceholder)ph).Href)) html += "<a href=\"" + ((ImagePlaceholder)ph).Href + "\">[Image]</a>";
                }
                else if (ph is XmlPlaceholder)
                {
                    html = ((XmlPlaceholder)ph).XmlAsString;
                }
                else if (ph is AttachmentPlaceholder)
                {
                    // create some HTML that will fit the regex which follows
                    html = "<a href=\"" + ((AttachmentPlaceholder)ph).Url + "\">" + ((AttachmentPlaceholder)ph).AttachmentText + "</a>";
                }

                const string pattern = @"<a [^>]*href=\""(?<LinkUrl>[^\"">]+)\""[^>]*>(?<LinkText>.*?)</a>";
                var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        string url = match.Groups["LinkUrl"].Value;

                        // remove the href
                        url = url.Replace("href", "");

                        // remove spaces
                        url = url.Replace(" ", "");

                        // remove the equal sign
                        url = url.Replace("=", "");

                        // remove double quotes
                        url = url.Replace("\"", "");

                        // remove querystrings, if any
                        if (url.IndexOf("?", StringComparison.Ordinal) > 0)
                        {
                            url = url.Substring(0, url.IndexOf("?", StringComparison.Ordinal) - 1);
                        }

                        // remove bookmarks, if any
                        if (url.IndexOf("#", StringComparison.Ordinal) > 0)
                        {
                            url = url.Substring(0, url.IndexOf("#", StringComparison.Ordinal) - 1);
                        }

                        // remove .htm
                        url = url.Replace(".htm", "");

                        // check to see if the URL is a channel or a posting
                        ChannelItem ci;
                        try
                        {
                            ci = context.Searches.GetByUrl(CmsUtilities.CorrectPublishedUrl(url));
                        }
                        catch (CmsAccessDeniedException)
                        {
                            // This handler is called in CMS published mode, but this link points to a CMS item we don't have permission for.
                            // Log as an unknown link and carry on.
                            SaveLink(transaction, posting.Guid, CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished), posting.Name, posting.DisplayName, posting.Template.Guid, ph.Name + " placeholder", posting.StateUnapprovedVersion.ToString(), posting.StateApprovedVersion.ToString(), submittedByName, CmsUtilities.LastApprovedDate(posting), posting.ExpiryDate, null, String.Empty, "Unpublished CMS page", match.Groups["LinkUrl"].Value, String.Empty, match.Groups["LinkText"].Value, null);
                            linksFound = true;
                            continue;
                        }

                        if (ci != null)
                        {
                            string destinationPath = ci.Path.Substring(9);
                            if (ci is Posting) destinationPath += ".htm"; // add htm because it's more familiar for editors

                            SaveLink(transaction, posting.Guid, CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished), posting.Name, posting.DisplayName, posting.Template.Guid, ph.Name + " placeholder", posting.StateUnapprovedVersion.ToString(), posting.StateApprovedVersion.ToString(), submittedByName, CmsUtilities.LastApprovedDate(posting), posting.ExpiryDate, null, ci.Guid, ci.GetType().Name, destinationPath, ci.Name, match.Groups["LinkText"].Value, null);
                        }
                        else
                        {
                            // check to see if it's a resource
                            try
                            {
                                Resource res = CmsUtilities.ParseResourceUrl(url, context);
                                if (res != null)
                                {
                                    SaveLink(transaction, posting.Guid, CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished), posting.Name, posting.DisplayName, posting.Template.Guid, ph.Name + " placeholder", posting.StateUnapprovedVersion.ToString(), posting.StateApprovedVersion.ToString(), submittedByName, CmsUtilities.LastApprovedDate(posting), posting.ExpiryDate, null, res.Guid, res.GetType().Name, res.Url, res.Name, match.Groups["LinkText"].Value, res.Size);
                                }
                                else
                                {
                                    // If it's none of those things it's a link to an unknown destination
                                    SaveLink(transaction, posting.Guid, CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished), posting.Name, posting.DisplayName, posting.Template.Guid, ph.Name + " placeholder", posting.StateUnapprovedVersion.ToString(), posting.StateApprovedVersion.ToString(), submittedByName, CmsUtilities.LastApprovedDate(posting), posting.ExpiryDate, null, String.Empty, "Web page", match.Groups["LinkUrl"].Value, String.Empty, match.Groups["LinkText"].Value, null);
                                }
                            }
                            catch (CmsServerException ex)
                            {
                                ex.Data.Add("Posting", posting.Path);
                                ex.Data.Add("Placeholder", ph.Name);
                                ex.Data.Add("Placeholder content", ph.Datasource.RawContent);
                                ex.Data.Add("URL being checked", url);
                                throw;
                            }
                        }
                        linksFound = true;
                    }
                }
            }

            // If there were no links on the page, add a blank record of the posting to the db. This should allow a query
            // to find postings with no inbound links.
            if (!linksFound)
            {
                string postingPath = CmsUtilities.CorrectPublishedUrl(posting.UrlModePublished);
                if (postingPath.StartsWith("/NR/exeres/", StringComparison.OrdinalIgnoreCase))
                {
                    // If the published URL starts with /NR/exeres/ the posting has never been published and the link won't work,
                    // so store the unpublished link instead.
                    postingPath = posting.UrlModeUnpublished;
                }

                SaveLink(transaction, posting.Guid, postingPath, posting.Name, posting.DisplayName, posting.Template.Guid, null, posting.StateUnapprovedVersion.ToString(), posting.StateApprovedVersion.ToString(), submittedByName, CmsUtilities.LastApprovedDate(posting), posting.ExpiryDate, null, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, null);
            }
        }


        /// <summary>
        /// Saves the link.
        /// </summary>
        /// <param name="transaction">A running SQL Server transaction</param>
        /// <param name="sourceGuid">The source GUID.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="sourceName">Name of the source.</param>
        /// <param name="sourceDisplayName">Display name of the source.</param>
        /// <param name="sourceTemplateGuid">The source template GUID.</param>
        /// <param name="sourceLocation">The source location.</param>
        /// <param name="sourceCurrentState">Current workflow state of the source page.</param>
        /// <param name="sourceApprovalState">State of the source approval.</param>
        /// <param name="sourceSubmittedBy">Username of last person to submit the page</param>
        /// <param name="sourceApprovalDate">The source approval date.</param>
        /// <param name="expiryDate">When the source page is set to expire.</param>
        /// <param name="sourceEditUrl">The source edit URL.</param>
        /// <param name="destinationGuid">The destination GUID.</param>
        /// <param name="destinationType">Type of the destination.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <param name="destinationPageName">Name of the destination page.</param>
        /// <param name="destinationDisplayName">Display name of the destination.</param>
        /// <param name="destinationBytes">Size of the destination.</param>
        public static void SaveLink(SqlTransaction transaction, string sourceGuid, string sourcePath, string sourceName, string sourceDisplayName, string sourceTemplateGuid, string sourceLocation, string sourceCurrentState, string sourceApprovalState, string sourceSubmittedBy, DateTime? sourceApprovalDate, DateTime? expiryDate, string sourceEditUrl, string destinationGuid, string destinationType, string destinationPath, string destinationPageName, string destinationDisplayName, int? destinationBytes)
        {
            SqlParameter[] sqlParams = new SqlParameter[18];

            sqlParams[0] = new SqlParameter("@PostingGuid", DBNull.Value);
            if (sourceGuid != null) sqlParams[0].Value = sourceGuid.ToString();
            sqlParams[1] = new SqlParameter("@PostingPath", sourcePath);
            sqlParams[2] = new SqlParameter("@PostingName", sourceName);
            sqlParams[3] = new SqlParameter("@PostingDisplayName", sourceDisplayName);
            sqlParams[4] = new SqlParameter("@TemplateGuid", DBNull.Value);
            if (sourceTemplateGuid != null) sqlParams[4].Value = sourceTemplateGuid.ToString();
            sqlParams[5] = new SqlParameter("@PlaceholderName", DBNull.Value);
            if (!String.IsNullOrEmpty(sourceLocation)) sqlParams[5].Value = sourceLocation;
            sqlParams[6] = new SqlParameter("@StateCurrentVersion", DBNull.Value);
            if (!String.IsNullOrEmpty(sourceCurrentState)) sqlParams[6].Value = sourceCurrentState;
            sqlParams[7] = new SqlParameter("@StateApprovedVersion", DBNull.Value);
            if (!String.IsNullOrEmpty(sourceApprovalState)) sqlParams[7].Value = sourceApprovalState;
            sqlParams[8] = new SqlParameter("@LastSubmittedBy", DBNull.Value);
            if (!String.IsNullOrEmpty(sourceSubmittedBy)) sqlParams[8].Value = sourceSubmittedBy;
            sqlParams[9] = new SqlParameter("@LastApproved", DBNull.Value);
            if (sourceApprovalDate != null) sqlParams[9].Value = sourceApprovalDate;
            sqlParams[10] = new SqlParameter("@ExpiryDate", DBNull.Value);
            if (expiryDate != null) sqlParams[10].Value = expiryDate;
            sqlParams[11] = new SqlParameter("@PlaceholderPath", DBNull.Value);
            if (!String.IsNullOrEmpty(sourceEditUrl)) sqlParams[11].Value = sourceEditUrl;
            sqlParams[12] = new SqlParameter("@LinkGuid", DBNull.Value);
            if (destinationGuid != null) sqlParams[12].Value = destinationGuid.ToString();
            sqlParams[13] = new SqlParameter("@LinkItemType", DBNull.Value);
            if (!String.IsNullOrEmpty(destinationType)) sqlParams[13].Value = destinationType;
            sqlParams[14] = new SqlParameter("@LinkPath", DBNull.Value);
            if (!String.IsNullOrEmpty(destinationPath)) sqlParams[14].Value = destinationPath;
            sqlParams[15] = new SqlParameter("@LinkName", DBNull.Value);
            if (!String.IsNullOrEmpty(destinationPageName)) sqlParams[15].Value = destinationPageName;
            sqlParams[16] = new SqlParameter("@LinkDisplayName", DBNull.Value);
            if (!String.IsNullOrEmpty(destinationDisplayName)) sqlParams[16].Value = destinationDisplayName;
            sqlParams[17] = new SqlParameter("@LinkBytes", DBNull.Value);
            if (destinationBytes != null) sqlParams[17].Value = destinationBytes;

            SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, "usp_Link_Insert", sqlParams);

        }
    }
}
