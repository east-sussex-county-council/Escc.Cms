using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using eastsussexgovuk.webservices.TextXhtml.HouseStyle;
using EsccWebTeam.Cms.Placeholders;
using EsccWebTeam.Data.Web;
using EsccWebTeam.Egms;
using EsccWebTeam.HouseStyle;
using Microsoft.ApplicationBlocks.Data;
using Microsoft.ApplicationBlocks.ExceptionManagement;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Events;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;
using Microsoft.ContentManagement.WebControls.ConsoleControls;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Enhanced base class for CMS templates
    /// </summary>
    public class CmsTemplate : Page
    {
        #region Common setup for all CMS pages
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init"/> event to initialize the page.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnInit(EventArgs e)
        {
            // support machines without CMS installed
            if (CmsUtilities.IsCmsEnabled())
            {
                try
                {
                    try
                    {
                        base.OnInit(e);
                    }
                    catch (WebAuthorException ex)
                    {
                        // If a CMS template is requested directly, respond with 400 Bad Request and stop. Otherwise the built-in
                        // response is 500 which tells the requester the problem is at our end, not with their request.
                        if (ex.Message == "This operation requires the context of a Posting.  The request must have a valid Posting Url or QueryString so that the CmsHttpContext.Posting will not be null.")
                        {
                            Http.Status400BadRequest();
                            Response.End();
                        }
                        else throw;
                    }

                    SetupPageChecks();

                    SupportCmsScanner();

                    SupportLinkTracker();

                    SetMetadataFromPosting();

                    SupportTinyMCEEditor();

                    SupportHttpCaching();

                    this.PreRenderComplete += new EventHandler(CheckForInvalidMetadata_PreRenderComplete);

                    FormatDateProperties();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // ignore this error - it's a by-design part of Response.Redirect and Server.Transfer
                }
                catch (Exception ex)
                {
                    ExceptionManager.Publish(ex);
                }
            }
            else
            {
                // No change for non-CMS machines
                base.OnInit(e);
            }
        }

        /// <summary>
        /// Parses and formats date ptoperties in a way friendly for editors
        /// </summary>
        private void FormatDateProperties()
        {
            var dateProperties = new List<string> { "Copied to live", "Copied to live - test", "Copied to review", "Copy to live site", "Copy to review site" };
            PostingEvents.Current.CustomPropertyChanging += (sender, args) =>
                {
                    if (dateProperties.Contains(args.PropertyName))
                    {
                        var selectedDate = DateTimeFormatter.ParseDate(args.PropertyValue.ToString());
                        if (selectedDate.HasValue)
                        {
                            var formattedDate = DateTimeFormatter.ShortBritishDate(selectedDate.Value);
                            if (args.PropertyValue != formattedDate)
                            {
                                args.PropertyValue = formattedDate;
                            }
                        }
                    }
                };
        }

        /// <summary>
        /// When a page changes update the link tracker database which powers the "Find links in CMS" tool
        /// </summary>
        private void SupportLinkTracker()
        {
            if (CmsHttpContext.Current.Mode == PublishingMode.Update)
            {
                WebAuthorContext.Current.SavePostingEvent += new WebAuthorPostingEventHandler(UpdateLinkTracker_SavePostingEvent);
                PostingEvents.Current.Deleted += new ChangedEventHandler(UpdateLinkTracker_Deleted);
            }
        }

        /// <summary>
        /// Update the link tracker with changes to the page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UpdateLinkTracker_SavePostingEvent(object sender, WebAuthorPostingEventArgs e)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["CmsSupport"].ConnectionString))
            {
                conn.Open();
                using (var t = conn.BeginTransaction())
                {
                    LinkTracker.SaveLinksForPosting(CmsHttpContext.Current, e.Posting, t);
                    t.Commit();
                }
            }
        }

        /// <summary>
        /// Delete the page from the link tracker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UpdateLinkTracker_Deleted(object sender, ChangedEventArgs e)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["CmsSupport"].ConnectionString))
            {
                SqlHelper.ExecuteNonQuery(conn, CommandType.StoredProcedure, "usp_Posting_Delete", new SqlParameter("@PostingGuid", (e.Target as Posting).Guid));
            }
        }


        /// <summary>
        /// Supports HTTP caching based on upload times.
        /// </summary>
        private void SupportHttpCaching()
        {
            // Only do this if it's enabled in web.config
            if (!CmsUtilities.IsHttpCachingEnabled()) return;

            // Never use HTTP caching for anyone who can edit the site
            if (CmsHttpContext.Current.UserCanModifySite)
            {
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetMaxAge(new TimeSpan(0));
                Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
                return;
            }

            // How long is this page fresh for? We upload at 11am and 4pm, but actually the time period is about 11-1pm and 4-6pm.
            // During those times, pages can be fresh for 10 minutes. Outside those times they can be fresh until the next upload.
            TimeSpan freshFor;
            if (DateTime.Now.Hour == 11 || DateTime.Now.Hour == 12 || DateTime.Now.Hour == 16 || DateTime.Now.Hour == 17)
            {
                freshFor = new TimeSpan(0, 10, 0);
            }
            else if (DateTime.Now.Hour == 13 || DateTime.Now.Hour == 14 || DateTime.Now.Hour == 15)
            {
                freshFor = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 16, 0, 0).Subtract(DateTime.Now);
            }
            else if (DateTime.Now.Hour < 11)
            {
                freshFor = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 11, 0, 0).Subtract(DateTime.Now);
            }
            else
            {
                var tomorrow = DateTime.Today.AddDays(1);
                freshFor = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 11, 0, 0).Subtract(DateTime.Now);
            }

            // But what if it's the weekend? Then there won't be an upload until Monday at 11am.
            if (DateTime.Now.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour > 17)
            {
                var monday = DateTime.Today.AddDays(3);
                freshFor = new DateTime(monday.Year, monday.Month, monday.Day, 11, 0, 0).Subtract(DateTime.Now);
            }
            else if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
            {
                var monday = DateTime.Today.AddDays(2);
                freshFor = new DateTime(monday.Year, monday.Month, monday.Day, 11, 0, 0).Subtract(DateTime.Now);
            }
            else if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                var monday = DateTime.Today.AddDays(1);
                freshFor = new DateTime(monday.Year, monday.Month, monday.Day, 11, 0, 0).Subtract(DateTime.Now);
            }

            // Based on upload times the page will be fresh until this date
            var freshUntil = DateTime.Now.Add(freshFor);

            // But what if it expires sooner than that?
            var posting = CmsUtilities.Posting;
            if (posting != null && posting.ExpiryDate < freshUntil)
            {
                // If posting has already expired, don't cache at all
                if (posting.ExpiryDate <= DateTime.Now) return;

                // Otherwise set cache to expire when the posting expires
                freshFor = posting.ExpiryDate.Subtract(DateTime.Now);
                freshUntil = DateTime.Now.Add(freshFor);
            }

            // Or if there's a latest placeholder which expires sooner?
            if (posting != null && posting.Placeholders["phDefLatestExpiry"] != null)
            {
                var expiryDate = DateTimePlaceholderControl.GetValue(posting.Placeholders["phDefLatestExpiry"] as XmlPlaceholder);
                if (expiryDate > DateTime.Now && expiryDate < freshUntil)
                {
                    freshFor = expiryDate.Subtract(DateTime.Now);
                    freshUntil = DateTime.Now.Add(freshFor);
                }
            }

            // Allow specific postings to override this using a custom property
            var cacheProperty = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Cache");
            if (cacheProperty != null)
            {
                DateTime expiryDate;
                switch (cacheProperty.Value)
                {
                    case "5 minutes":
                        expiryDate = DateTime.Now.AddMinutes(5);
                        if (expiryDate < freshUntil)
                        {
                            freshFor = new TimeSpan(0, 5, 0);
                            freshUntil = expiryDate;
                        }
                        break;
                    case "10 minutes":
                        expiryDate = DateTime.Now.AddMinutes(10);
                        if (expiryDate < freshUntil)
                        {
                            freshFor = new TimeSpan(0, 10, 0);
                            freshUntil = expiryDate;
                        }
                        break;
                    case "30 minutes":
                        expiryDate = DateTime.Now.AddMinutes(30);
                        if (expiryDate < freshUntil)
                        {
                            freshFor = new TimeSpan(0, 30, 0);
                            freshUntil = expiryDate;
                        }
                        break;
                    case "1 hour":
                        expiryDate = DateTime.Now.AddHours(1);
                        if (expiryDate < freshUntil)
                        {
                            freshFor = new TimeSpan(1, 0, 0);
                            freshUntil = expiryDate;
                        }
                        break;
                }
            }

            // Cache the page
            Response.Cache.SetCacheability(HttpCacheability.Public);
            Response.Cache.SetExpires(freshUntil);
            try
            {
                Response.Cache.SetMaxAge(freshFor);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ex.Data.Add("freshFor", freshFor.ToString());
                ex.Data.Add("freshUntil", freshUntil.ToString(CultureInfo.CurrentCulture));

                if (posting != null)
                {
                    ex.Data.Add("Posting expires", posting.ExpiryDate.ToString(CultureInfo.CurrentCulture));

                    if (posting.Placeholders["phDefLatestExpiry"] != null)
                    {
                        var expiryDate = DateTimePlaceholderControl.GetValue(posting.Placeholders["phDefLatestExpiry"] as XmlPlaceholder);
                        ex.Data.Add("Latest expires", expiryDate.ToString(CultureInfo.CurrentCulture));
                    }

                    if (cacheProperty != null)
                    {
                        ex.Data.Add("Cache custom property", cacheProperty.Value);
                    }
                }

                throw ex;
            }
        }

        /// <summary>
        /// Looks for a metadata control and populates it with fields read from the current Posting.
        /// </summary>
        /// <returns></returns>
        private bool SetMetadataFromPosting()
        {
            if (Page.Header == null) return false;
            return SetMetadataFromPosting(Page.Header.Controls);
        }

        /// <summary>
        /// Looks for a metadata control and populates it with fields read from the current Posting.
        /// </summary>
        /// <param name="controls">The control collection to look through.</param>
        /// <returns></returns>
        private bool SetMetadataFromPosting(ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                var metadata = control as MetadataControl;
                if (metadata != null)
                {
                    // get a cms context
                    CmsHttpContext cms = CmsHttpContext.Current;
                    bool postingFound = (cms.Posting != null);

                    // get metadata from CMS
                    if (postingFound)
                    {
                        if (this.Title.Length == 0)
                        {
                            this.Title = cms.Posting.DisplayName; // fallback
                            if (cms.Posting.Placeholders["phDefTitle"] != null)
                            {
                                HtmlPlaceholder ph = cms.Posting.Placeholders["phDefTitle"] as HtmlPlaceholder;
                                if (ph != null)
                                {
                                    this.Title = Html.StripTags(ph.Html);
                                }
                                else
                                {
                                    XmlPlaceholder phXml = cms.Posting.Placeholders["phDefTitle"] as XmlPlaceholder;
                                    if (phXml != null) this.Title = TextPlaceholderControl.GetValue(phXml);
                                }
                            }
                        }

                        // Title gets HTML encoded when output, but decode first to guard against double encoding
                        this.Title = HttpUtility.HtmlDecode(this.Title);

                        if (metadata.Description.Length == 0) metadata.Description = cms.Posting.Description;
                        if (metadata.Keywords.Length == 0)
                        {
                            CustomProperty keywordProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "eGMS.subject.keyword");
                            if (keywordProp != null) metadata.Keywords = keywordProp.Value;
                        }
                        if (metadata.SpatialCoverage.Length == 0)
                        {
                            CustomProperty spatialProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "DC.coverage");
                            if (spatialProp != null) metadata.SpatialCoverage = spatialProp.Value;
                        }
                        if (metadata.Creator.Length == 0)
                        {
                            CustomProperty creatorProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "DC.creator");
                            if (creatorProp != null) metadata.Creator = creatorProp.Value;
                        }
                        if (metadata.Publisher.Length == 0)
                        {
                            CustomProperty publishProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "DC.publisher");
                            if (publishProp != null) metadata.Publisher = publishProp.Value;
                        }
                        if (metadata.Language.Length == 0)
                        {
                            CustomProperty langProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "DC.language");
                            if (langProp != null) metadata.Language = langProp.Value;
                        }

                        if (metadata.SystemId.Length == 0) metadata.SystemId = cms.Posting.Guid;
                        metadata.PageUrl = new Uri(Request.Url.Scheme + "://" + this.Request.Url.Host + CmsUtilities.CorrectPublishedUrl(CmsHttpContext.Current.Posting.UrlModePublished));
                        if (!cms.Posting.IsRobotIndexable) metadata.IsInSearch = false;
                        if (metadata.DateCreated.Length == 0) metadata.DateCreated = DateTimeFormatter.ISODate(cms.Posting.CreatedDate);
                        if (metadata.DateIssued.Length == 0)
                        {
                            CustomProperty issuedProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "DCTERMS.issued");
                            if (issuedProp != null)
                            {
                                metadata.DateIssued = issuedProp.Value;
                            }
                            else metadata.DateIssued = DateTimeFormatter.ISODate(cms.Posting.StartDate);
                        }
                        if (metadata.DateModified.Length == 0) metadata.DateModified = DateTimeFormatter.ISODate(cms.Posting.LastModifiedDate);
                        if (metadata.DateReview.Length == 0 && cms.Posting.ExpiryDate.Year != 3000) metadata.DateReview = DateTimeFormatter.ISODate(cms.Posting.ExpiryDate);

                        // IPSV 
                        CustomProperty ipsvProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "IPSV preferred");
                        if (ipsvProp != null && ipsvProp.Value.Length > 0) metadata.IpsvPreferredTerms = ipsvProp.Value;

                        CustomProperty ipsvNonProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "IPSV non-preferred");
                        if (ipsvNonProp != null && ipsvNonProp.Value.Length > 0) metadata.IpsvNonPreferredTerms = ipsvNonProp.Value;

                        // LGSL
                        CustomProperty lgslProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "LGSL numbers");
                        if (lgslProp != null && lgslProp.Value.Length > 0) metadata.LgslNumbers = lgslProp.Value;

                        // Geo.Easting
                        CustomProperty geoEastingProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "Geo.Easting");
                        if (geoEastingProp != null && geoEastingProp.Value.Length > 0) metadata.Easting = Convert.ToInt32(geoEastingProp.Value);

                        // Geo.Northing
                        CustomProperty geoNorthingProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "Geo.Northing");
                        if (geoNorthingProp != null && geoNorthingProp.Value.Length > 0) metadata.Northing = Convert.ToInt32(geoNorthingProp.Value);

                        //ESCC.IsContactDoc
                        CustomProperty isContactDocProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "ESCC.IsContactDoc");
                        if (isContactDocProp != null && isContactDocProp.Value.Length > 0) metadata.IsContactDoc = Convert.ToBoolean(isContactDocProp.Value);
                    }

                    return true;
                }

                // Are any of the child controls a metadata control?
                if (SetMetadataFromPosting(control.Controls)) return true;
            }

            return false;
        }

        /// <summary>
        /// Handles the PreRenderComplete event to check for invalid metadata
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void CheckForInvalidMetadata_PreRenderComplete(object sender, EventArgs e)
        {
            // If viewing unpublished mode write out invalid metadata. The tag will be picked up by JavaScript in the console.
            // Do it on PreRenderComplete event to avoid error about not being allow to add controls to page head.
            CmsHttpContext cms = CmsHttpContext.Current;
            if (cms.Posting != null && WebAuthorContext.Current.Mode == WebAuthorContextMode.PresentationUnpublished)
            {
                CustomProperty invalidProp = CmsUtilities.GetCustomProperty(cms.Posting.CustomProperties, "Metadata invalid");
                if (invalidProp != null && invalidProp.Value.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<meta name=\"metadataInvalid\" content=\"");
                    sb.Append(invalidProp.Value);
                    sb.Append("\" />").Append(Environment.NewLine);
                    if (sb.Length > 0) Page.Header.Controls.Add(new LiteralControl(sb.ToString()));
                }
            }
        }

        /// <summary>
        /// Sets up checks which take place when the page is saved
        /// </summary>
        private void SetupPageChecks()
        {
            if (CmsHttpContext.Current.Mode == PublishingMode.Update)
            {
                WebAuthorContext.Current.SavePostingEvent += new WebAuthorPostingEventHandler(CheckPage_SavingContent);
                WebAuthorContext.Current.SavePostingEvent += new WebAuthorPostingEventHandler(SaveSnapshot_SavePostingEvent);
                WebAuthorContext.Current.InvokedAction += new InvokedActionEventHandler(SavePostingEdit_InvokedAction);
            }
        }


        /// <summary>
        /// Record that a posting has been submitted or approved, and by whom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SavePostingEdit_InvokedAction(object sender, ActionEventArgs e)
        {
            if (e.Action is SubmitAction)
            {
                CmsUtilities.SavePostingEdit(CmsUtilities.Posting, WorkflowStage.Submit);
            }
            else if (e.Action is ApproveAction)
            {
                CmsUtilities.SavePostingEdit(CmsUtilities.Posting, WorkflowStage.Approve);
            }
        }

        /// <summary>
        /// Apply metadata rules as the page is saved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckPage_SavingContent(object sender, WebAuthorPostingEventArgs e)
        {
            // Applying metadata should run on submit and approve too to catch any postings where
            // metadata entered after page saved
            CmsUtilities.ApplyMetadata(e.Posting);
        }

        /// <summary>
        /// Saves a snapshot analysis of a page as it is saved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveSnapshot_SavePostingEvent(object sender, WebAuthorPostingEventArgs e)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["CmsSupport"].ConnectionString))
            {
                CmsUtilities.SavePostingSnapshot(e.Posting, DateTime.Now, SnapshotType.OnSave, conn);
                CmsUtilities.SavePostingEdit(e.Posting, WorkflowStage.Save);
            }
        }

        /// <summary>
        /// Supports the CMS scanner which checks for pages that have not uploaded properly
        /// </summary>
        private void SupportCmsScanner()
        {
            if (CmsHttpContext.Current == null) return;
            if (CmsHttpContext.Current.Posting == null) return;

            // Provide a way to view the content hash in a browser, for test purposes
            if (String.IsNullOrEmpty(HttpContext.Current.Request.QueryString["CmsContentHash"]))
            {
                if (HttpContext.Current.Request.HttpMethod != "HEAD") return;
                if (HttpContext.Current.Request.UserAgent != "EsccWebTeam.Cms.CmsScanner") return;
            }

            string postingHash = CmsUtilities.GetPostingContentHash(CmsHttpContext.Current, CmsHttpContext.Current.Posting);
            if (String.IsNullOrEmpty(postingHash)) return;
            HttpContext.Current.Response.AddHeader("X-ESCC-CmsContentHash", postingHash);
        }


        /// <summary>
        /// Adds the scripts necessary to use TinyMCE as a rich text editor in CMS
        /// </summary>
        private void SupportTinyMCEEditor()
        {
            if (!CmsUtilities.IsEditing) return;

            var scripts = FindContentPlaceholder("javascript");
            if (scripts == null) return;

            scripts.Controls.Add(new LiteralControl("<!--[if IE]><script src=\"/MCMS/CMS/WebAuthor/Client/PlaceholderControlSupport/AuthFormClientIE.js\"></script><![endif]-->" + "[if IE]><script src=\"/MCMS/CMS/WebAuthor/Client/PlaceholderControlSupport/AuthFormClientIE.js\"></script><![endif]-->" +
                                "<script src=\"/EsccWebTeam.Cms.WebAuthor/tiny_mce/tiny_mce.js\"></script>" +
                                "<script src=\"/EsccWebTeam.Cms.WebAuthor/Placeholders/TinyMCE.js\"></script>"));
        }

        #endregion

        #region Validation

        private bool sitewideValidatorsCreated;

        /// <summary>
        /// Set up standard validators before validating the page
        /// </summary>
        public override void Validate()
        {
            // Do this here because it allows the template to create controls dynamically in Page_Load, 
            // then immediately after that this runs, finds them and validates them.
            if (!sitewideValidatorsCreated) CreateSitewideValidators();

            // Do the validation
            base.Validate();
        }

        /// <summary>
        /// Create validators which apply to all placeholder controls of a given type
        /// </summary>
        private void CreateSitewideValidators()
        {
            if (!CmsUtilities.IsEditing) return;

            var contentPlaceholder = FindContentPlaceholder("content");
            var placeholdersToValidate = CmsUtilities.FindControlsOfType<RichHtmlPlaceholderControl>(contentPlaceholder);
            foreach (RichHtmlPlaceholderControl placeholderControl in placeholdersToValidate)
            {
                if (PlaceholderControlHasId(placeholderControl))
                {
                    if (placeholderControl.ApplyStandardValidation)
                    {
                        CreateCustomValidator(placeholderControl, new ServerValidateEventHandler(ClickHereLink_ServerValidate));
                        CreateCustomValidator(placeholderControl, new ServerValidateEventHandler(UrlAsLinkTextValidator_ServerValidate));
                        placeholderControl.NamingContainer.Controls.Add(new AllCapsValidator { ControlToValidate = placeholderControl.ID });
                        CreateCustomValidator(placeholderControl, new ServerValidateEventHandler(VisitLink_ServerValidate));
                        CreateCustomValidator(placeholderControl, new ServerValidateEventHandler(MoreLink_ServerValidate));
                    }

                    // Always apply because it replaces an unhelpful error message from CMS - which you'd get anyway - with a helpful one
                    CreateCustomValidator(placeholderControl, new ServerValidateEventHandler(CheckPermissionsForLinksInHtml_ServerValidate));
                }
            }

            placeholdersToValidate = CmsUtilities.FindControlsOfType<SingleImagePlaceholderControl>(contentPlaceholder);
            foreach (XhtmlImagePlaceholderControl placeholderControl in placeholdersToValidate)
            {
                if (PlaceholderControlHasId(placeholderControl))
                {
                    CreateCustomValidator(placeholderControl, new ServerValidateEventHandler(CheckPermissionsForImage_ServerValidate));
                }
            }

            placeholdersToValidate = CmsUtilities.FindControlsOfType<SingleAttachmentPlaceholderControl>(contentPlaceholder);
            foreach (BasePlaceholderControl placeholderControl in placeholdersToValidate)
            {
                if (PlaceholderControlHasId(placeholderControl))
                {
                    CreateCustomValidator(placeholderControl, new ServerValidateEventHandler(CheckPermissionsForAttachment_ServerValidate));
                }
            }

            this.sitewideValidatorsCreated = true;
        }

        /// <summary>
        /// Check that an HTML placeholder does not contain links to resources to which the current user does not have permission
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        static void CheckPermissionsForLinksInHtml_ServerValidate(object source, ServerValidateEventArgs args)
        {
            var validator = source as BaseValidator;
            var placeholder = validator.NamingContainer.FindControl(validator.ControlToValidate) as RichHtmlPlaceholderControl;

            if (String.IsNullOrEmpty(placeholder.EditorHtml))
            {
                args.IsValid = true;
            }
            else
            {
                args.IsValid = true;
                var matches = Regex.Matches(placeholder.EditorHtml, CmsUtilities.DownloadLinkPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var errorMessage = GetErrorMessageIfResourceInaccessible(m.Groups["url"].Value, "document", m.Groups["linktext"].Value);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        args.IsValid = false;

                        // If there are error messages for multiple links within a placeholder, join them together with list item HTML because
                        // the error messages are going to appear in a ValidationSummary
                        if (!String.IsNullOrEmpty(validator.ErrorMessage)) validator.ErrorMessage += "</li><li>";
                        validator.ErrorMessage += errorMessage;
                    }
                }
            }
        }

        /// <summary>
        /// Check that an image placeholder does not contain an image to which the current user does not have permission
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        static void CheckPermissionsForImage_ServerValidate(object source, ServerValidateEventArgs args)
        {
            var validator = source as BaseValidator;
            var placeholder = validator.NamingContainer.FindControl(validator.ControlToValidate) as SingleImagePlaceholderControl;

            if (String.IsNullOrEmpty(placeholder.ImageUrl))
            {
                args.IsValid = true;
            }
            else
            {
                var errorMessage = GetErrorMessageIfResourceInaccessible(placeholder.ImageUrl, "image");
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    args.IsValid = false;
                    validator.ErrorMessage = errorMessage;
                }
                else
                {
                    args.IsValid = true;
                }
            }
        }

        /// <summary>
        /// Check that an attachment placeholder does not contain a document to which the current user does not have permission
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        static void CheckPermissionsForAttachment_ServerValidate(object source, ServerValidateEventArgs args)
        {
            var validator = source as BaseValidator;
            var placeholder = validator.NamingContainer.FindControl(validator.ControlToValidate) as SingleAttachmentPlaceholderControl;

            if (String.IsNullOrEmpty(placeholder.AttachmentUrl))
            {
                args.IsValid = true;
            }
            else
            {
                var errorMessage = GetErrorMessageIfResourceInaccessible(placeholder.AttachmentUrl, "document");
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    args.IsValid = false;
                    validator.ErrorMessage = errorMessage;
                }
                else
                {
                    args.IsValid = true;
                }
            }
        }

        /// <summary>
        /// Checks whether linking to a resource would result in an error because the resource has been deleted or the Author doesn't have permission to use it.
        /// </summary>
        /// <param name="resourceUrl">The URL being linked to</param>
        /// <param name="resourceType">"image" or "document"</param>
        /// <param name="linkText">The text used to link to a resource</param>
        /// <returns>The error message to display, or <c>null</c> if no error</returns>
        private static string GetErrorMessageIfResourceInaccessible(string resourceUrl, string resourceType, string linkText = null)
        {
            var resourceGuid = CmsUtilities.GetGuidFromUrl(resourceUrl);
            if (!String.IsNullOrEmpty(resourceGuid))
            {
                var cms = CmsHttpContext.Current;

                var hierarchyItem = cms.Searches.GetByGuid(resourceGuid);
                if (hierarchyItem is Posting)
                {
                    // It's a local attachment uploaded directly to the posting, so no error
                    return null;
                }

                var resource = hierarchyItem as Resource;
                if (resource != null)
                {
                    linkText = String.IsNullOrEmpty(linkText) ? resource.DisplayName : linkText;
                    if (resource.Path.StartsWith("/Archive Folder", StringComparison.Ordinal))
                    {
                        // The resource has been deleted using Resource Manager
                        return "The " + resourceType + " '" + linkText + "' has been deleted from Resource Manager. Please remove it from the page and try to save again.";
                    }
                    else if (!resource.CanUseForAuthoring)
                    {
                        // The resource hasn't been deleted, but the Author group the current user belongs to doesn't have permission to its Resource Gallery
                        return "You don't have permission to use the " + resourceType + " '" + resource.DisplayName + "'. Please remove it and try to save the page again.";
                    }
                    else
                    {
                        // The resource is there and we have permission, so no error
                        return null;
                    }
                }
                else
                {
                    // If the resource is not found it's been deleted using Site Manager
                    linkText = String.IsNullOrEmpty(linkText) ? Path.GetFileName(resourceUrl) : linkText;
                    return "The " + resourceType + " '" + linkText + "' has been deleted from Resource Manager. Please remove it from the page and try to save again.";
                }
            }
            return null;
        }

        /// <summary>
        /// Checks that a placeholder control has its ID property set
        /// </summary>
        /// <param name="placeholderControl"></param>
        /// <returns></returns>
        private static bool PlaceholderControlHasId(BasePlaceholderControl placeholderControl)
        {
            // You can't validate a placeholder control with no ID. If it's a local request throw an exception to alert the developer.
            // If not, skip this control silently so that this code doesn't break existing templates.
            if (String.IsNullOrEmpty(placeholderControl.ID))
            {
                if (HttpContext.Current.Request.IsLocal)
                {
                    throw new InvalidOperationException("The " + placeholderControl.GetType().ToString() + " for placeholder " + placeholderControl.PlaceholderToBind + " could not be validated because its ID is not set. Set the ID property.");
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check that a placeholder does not include any links that use the URL as the link text
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        static void UrlAsLinkTextValidator_ServerValidate(object source, ServerValidateEventArgs args)
        {
            var validator = source as BaseValidator;
            var richHtmlPlaceholder = validator.NamingContainer.FindControl(validator.ControlToValidate) as RichHtmlPlaceholderControl;

            // Convert YouTube links which would be embedded, otherwise they are pulled up as a false positive
            var htmlToValidate = richHtmlPlaceholder.EditorHtml;
            htmlToValidate = MediaUtilities.RecogniseAndEmbedYouTubeUrl(htmlToValidate, 0, 0);

            var match = Regex.Match(htmlToValidate, "<a [^>]*>(?<LinkText>(http://|https://|www.|/)[^ ]+)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            args.IsValid = !match.Success;

            if (match.Success)
            {
                validator.ErrorMessage = "You linked to '" + match.Groups["LinkText"].Value + "'. Don't use the address of a web page as your link text. You should normally use the main heading of the destination page as your link text.";
            }
        }

        /// <summary>
        /// Check that a placeholder does not include any "click here" links
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private static void ClickHereLink_ServerValidate(object source, ServerValidateEventArgs args)
        {
            var validator = source as BaseValidator;
            var richHtmlPlaceholder = validator.NamingContainer.FindControl(validator.ControlToValidate) as RichHtmlPlaceholderControl;
            var anythingExceptEndAnchor = "((?!</a>).)*";

            var match = Regex.Match(richHtmlPlaceholder.EditorHtml, "<a [^>]*>(?<LinkText>" + anythingExceptEndAnchor + @"\bclick\s+here\b" + anythingExceptEndAnchor + ")</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                match = Regex.Match(richHtmlPlaceholder.EditorHtml, "<a [^>]*>(?<LinkText>" + @"\s*here\s*)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }

            args.IsValid = !match.Success;

            if (match.Success)
            {
                validator.ErrorMessage = "You linked to '" + match.Groups["LinkText"].Value + "'. Links must make sense on their own, out of context. Linking to 'click here' or anything similar doesn't do that. You should normally use the main heading of the destination page as your link text.";
            }
        }

        /// <summary>
        /// Check that a placeholder does not include any "visit..." links
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private static void VisitLink_ServerValidate(object source, ServerValidateEventArgs args)
        {
            // Regex matches any link starting with the word "visit" in it. 

            var validator = source as BaseValidator;
            var richHtmlPlaceholder = validator.NamingContainer.FindControl(validator.ControlToValidate) as RichHtmlPlaceholderControl;
            var anythingExceptEndAnchor = "((?!</a>).)*";
            var anythingExceptVisit = "((?!visit).)*";

            var match = Regex.Match(richHtmlPlaceholder.EditorHtml, "<a [^>]*href=['\"]" + anythingExceptVisit + "['\"][^>]*>(?<LinkText>Visit\\s+" + anythingExceptEndAnchor + ")</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            args.IsValid = !match.Success;

            if (match.Success)
            {
                validator.ErrorMessage = "You linked to '" + match.Groups["LinkText"].Value + "'. You don't need to start links with 'visit'. You should normally use the main heading of the destination page as your link text.";
            }
        }

        /// <summary>
        /// Check that a placeholder does not include any "More..." links
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private static void MoreLink_ServerValidate(object source, ServerValidateEventArgs args)
        {
            // Regex matches any link where the A-Z characters are "More". 

            var validator = source as BaseValidator;
            var richHtmlPlaceholder = validator.NamingContainer.FindControl(validator.ControlToValidate) as RichHtmlPlaceholderControl;

            var match = Regex.Match(richHtmlPlaceholder.EditorHtml, "<a [^>]*>(?<LinkText>More[^A-Z]*)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            args.IsValid = !match.Success;

            if (match.Success)
            {
                validator.ErrorMessage = "You linked to '" + match.Groups["LinkText"].Value + "'. Links must make sense on their own, out of context. Linking to 'More' doesn't do that. Link to 'More about [your subject]' instead.";
            }
        }

        /// <summary>
        /// Adds a custom validator to the supplied placeholder
        /// </summary>
        /// <param name="cmsPlaceholder"></param>
        /// <param name="eventHandler"></param>
        private static void CreateCustomValidator(Control cmsPlaceholder, ServerValidateEventHandler eventHandler)
        {
            var valid = new CustomValidator();
            valid.ControlToValidate = cmsPlaceholder.ID;
            valid.Display = ValidatorDisplay.None;
            valid.EnableClientScript = false;
            valid.ServerValidate += eventHandler;
            cmsPlaceholder.NamingContainer.Controls.Add(valid);
        }

        /// <summary>
        /// Finds a content placeholder on the current master page
        /// </summary>
        /// <param name="contentPlaceholderId"></param>
        /// <returns></returns>
        private ContentPlaceHolder FindContentPlaceholder(string contentPlaceholderId)
        {
            if (Master == null) return null;
            ContentPlaceHolder contentPlaceholder = null;
            var masterPage = Master;
            while (contentPlaceholder == null && masterPage != null)
            {
                contentPlaceholder = masterPage.FindControl(contentPlaceholderId) as ContentPlaceHolder;
                masterPage = masterPage.Master;
            }
            return contentPlaceholder;
        }

        #endregion

        #region Catch and publish exceptions on page load
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            // Catch and publish exceptions on page load. This means that if there's an error in the code,
            // the page still loads with 200 OK rather than going to the 500 page. For CMS pages that's good
            // as usually the content will load, even if any additional code doesn't run. For other pages
            // that's bad as it's usually the additional code that is the main purpose of the page, so better 
            // that they go to the 500 page.
            try
            {
                base.OnLoad(e);
            }
            catch (ThreadAbortException)
            {
                // ignore this error - it's a by-design part of Response.Redirect and Server.Transfer
                Thread.ResetAbort();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        #endregion // Catch and publish exceptions on page load

        #region Fix the HTML output by CMS

        /// <summary>
        /// Fix the HTML output by CMS
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            // support machines without CMS installed
            if (CmsUtilities.IsCmsEnabled())
            {
                FixCmsPage(writer);
            }
            else
            {
                base.Render(writer);
            }
        }

        /// <summary>
        /// Fixes the CMS postback to go to the posting URL rather than the template name, which doesn't exist in the channels where it's used
        /// </summary>
        /// <param name="writer">The writer.</param>
        private void FixCmsPage(System.Web.UI.HtmlTextWriter writer)
        {
            // Always do this in edit mode to support postback of controls within console
            CmsHttpContext cms = CmsHttpContext.Current;
            if (cms.Posting != null)
            {
                // Get the HTML to be rendered by CMS
                TextWriter tempWriter = new StringWriter(CultureInfo.CurrentCulture);
                base.Render(new HtmlTextWriter(tempWriter));
                string modifiedHtml = tempWriter.ToString();

                if (cms.Mode == PublishingMode.Unpublished || cms.Mode == PublishingMode.Update)
                {
                    // Replace the form action, which contains the template file name, with the page name so that it posts back to itself
                    modifiedHtml = Regex.Replace(modifiedHtml, "action=." + Path.GetFileName(cms.Posting.Template.SourceFile) + "[^ ]+ ", "action=\"" + cms.Posting.UrlModeUpdate + "\" ");
                    modifiedHtml = modifiedHtml.Replace(" __CMS_PostbackForm.action = __CMS_CurrentUrl;", "");
                }
                else if (cms.Mode == PublishingMode.Published && cms.Posting != null)
                {
                    // Replace the form action, which contains the template file name, with the page name so that it posts back to itself.
                    modifiedHtml = Regex.Replace(modifiedHtml, "action=\"[/A-Za-z0-9]*" + Path.GetFileName(cms.Posting.Template.SourceFile) + "[^ ]+ ", "action=\"" + CmsUtilities.CorrectPublishedUrl(cms.Posting.UrlModePublished) + "\" ");
                    modifiedHtml = modifiedHtml.Replace(" __CMS_PostbackForm.action = __CMS_CurrentUrl;", "");

                    // Not sure why it's different but the school template has the right page name, but the ugly query string
                    modifiedHtml = Regex.Replace(modifiedHtml, "action=\"" + cms.Posting.Name + @".htm\?NRMODE=Published&amp;NRNODEGUID=" + Server.UrlEncode(cms.Posting.Guid) + "&amp;NRORIGINALURL=" + Server.UrlEncode(CmsUtilities.CorrectPublishedUrl(cms.Posting.UrlModePublished)) + "&amp;NRCACHEHINT=[A-Za-z]+\"", "action=\"" + cms.Posting.Name + ".htm\"", RegexOptions.Singleline);
                }

                // Remove deprecated language attribute from all CMS scripts
                modifiedHtml = modifiedHtml.Replace("language=\"javascript\" type=\"text/javascript\"", "type=\"text/javascript\"");

                // Remove unnecessary CMS script
                if (!CmsHttpContext.Current.UserCanModifySite)
                {
                    modifiedHtml = Regex.Replace(modifiedHtml, "<script type=\"text/javascript\">\\s*<!--\\s*var __CMS_PostbackForm = document.forms" + @"\['aspnetForm'\];.*?var __CMS_CurrentUrl = .*?</script>", String.Empty, RegexOptions.Singleline);
                }

                // Send new HTML to be rendered instead
                writer.Write(modifiedHtml);
            }
            else
            {
                base.Render(writer);
            }
        }
        #endregion // Fix the HTML output by CMS

    }
}
