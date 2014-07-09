using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using eastsussexgovuk.webservices.TextXhtml.HouseStyle;
using EsccWebTeam.HouseStyle;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Replacement for out-of-the-box HtmlPlaceholderControl, which uses TinyMCE for editing
    /// </summary>
    [ValidationProperty("EditorHtml")]
    public class RichHtmlPlaceholderControl : BasePlaceholderControl
    {
        private TextBox editControl;
        private PlaceHolder displayControl;
        private CmsHttpContext cms;
        private string channelUrl = "";
        private string defaultValue;
        private bool visibleSet;
        private Collection<string> allowedElements = new Collection<string>();
        private ITemplate headerTemplate;
        private ITemplate footerTemplate;


        /// <summary>
        /// Header which will be displayed if there is content in the field
        /// </summary>
        [TemplateContainer(typeof(XhtmlContainer))]
        public ITemplate HeaderTemplate
        {
            get { return this.headerTemplate; }
            set { this.headerTemplate = value; }
        }

        /// <summary>
        /// Footer which will be displayed if there is content in the field
        /// </summary>
        [TemplateContainer(typeof(XhtmlContainer))]
        public ITemplate FooterTemplate
        {
            get { return this.footerTemplate; }
            set { this.footerTemplate = value; }
        }

        /// <summary>
        /// Gets or sets the allowed XHTML elements
        /// </summary>
        /// <value>The allowed elements.</value>
        public Collection<string> AllowedElements
        {
            get { return allowedElements; }
        }

        /// <summary>
        /// Gets or sets the allowed elements as a semi-colon separated string
        /// </summary>
        /// <value>The allowed elements list.</value>
        /// <remarks>This allows the list to be set declaritively</remarks>
        public string AllowedElementsList
        {
            get
            {
                string[] tags = new string[this.allowedElements.Count];
                allowedElements.CopyTo(tags, 0);
                return String.Join(";", tags);
            }
            set
            {
                this.allowedElements.Clear();
                string[] tagNames = value.Split(';');
                foreach (string tagName in tagNames)
                {
                    this.allowedElements.Add(tagName);
                }
            }

        }

        /// <summary>
        /// Gets or sets the default text to be inserted when a new posting is created
        /// </summary>
        public string DefaultValue
        {
            get
            {
                return this.defaultValue;
            }
            set
            {
                this.defaultValue = value;
            }
        }

        /// <summary>
        /// Gets whether anything has been saved in this placeholder
        /// </summary>
        public bool HasContent
        {
            get
            {
                HtmlPlaceholder ph = this.BoundPlaceholder as HtmlPlaceholder;
                if (ph == null || ph.Html == null) return false;
                return (ph.Html.Trim().Length > 0);
            }
        }

        /// <summary>
        /// Gets or sets the width in pixels of the edit control in authoring mode
        /// </summary>
        public int EditControlWidth { get; set; }

        /// <summary>
        /// Gets or sets the height in pixels of the edit control in authoring mode
        /// </summary>
        public int EditControlHeight { get; set; }

        /// <summary>
        /// Gets or sets the class applied to the body of the rich text editor.
        /// </summary>
        /// <value>The edit control class.</value>
        public string EditControlClass { get; set; }

        /// <summary>
        /// Gets or sets whether to force the XHTML to be enclosed in paragraphs, rather than left inline
        /// </summary>
        public bool Paragraphs { get; set; }

        /// <summary>
        /// Gets or sets the HTML element to use when rendering a container element
        /// </summary>
        public string ElementName { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a server control is rendered as UI on the page
        /// </summary>
        public override bool Visible
        {
            get
            {
                if (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew ||
                    WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit)
                {
                    return true;
                }
                else return base.Visible;
            }
            set
            {
                base.Visible = value;
                this.visibleSet = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to embed videos instead of linking to them.
        /// </summary>
        /// <value><c>true</c> to embed videos; otherwise, <c>false</c>.</value>
        public bool EmbedVideo { get; set; }

        /// <summary>
        /// Gets or sets the HTML to be displayed.
        /// </summary>
        /// <value>The HTML to be displayed.</value>
        /// <remarks>
        /// 	<para>This read-write property is a value local to this control. The property is loaded from and written to the Html property of the <see cref="T:Microsoft.ContentManagement.Publishing.Extensions.Placeholders.HtmlPlaceholder"/> object that this control is bound to when appropriate. It may have a different value than the placeholder object if the value was changed programmatically after the data was loaded. For example, this can be modified in the OnSaving and OnLoaded event handlers.</para>
        /// 	<para>This property is not available at design time.</para>
        /// </remarks>
        public string Html
        {
            get
            {
                var ph = this.BoundPlaceholder as HtmlPlaceholder;
                return ph.Html;
            }
            set
            {

                var ph = this.BoundPlaceholder as HtmlPlaceholder;
                ph.Html = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTML in the edit control during authoring.
        /// </summary>
        /// <value>The editor HTML.</value>
        public string EditorHtml
        {
            get
            {
                if (this.editControl == null) return String.Empty;
                return this.editControl.Text;
            }
            set
            {
                if (this.editControl == null) throw new InvalidOperationException("The edit control is not active. Try your code at a different point in the page lifecycle.");
                this.editControl.Text = value;
            }
        }

        /// <summary>
        /// Apply the standard sitewide validation to this placeholder
        /// </summary>
        public bool ApplyStandardValidation { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically convert the first letter to uppercase
        /// </summary>
        public bool UppercaseFirstLetter { get; set; }

        /// <summary>
        /// Replacement for out-of-the-box HtmlPlaceholderControl, which builds in tidying of XHTML on save
        /// </summary>
        public RichHtmlPlaceholderControl()
            : base()
        {
            this.Paragraphs = true;
            this.EmbedVideo = true;
            this.ApplyStandardValidation = true;
        }

        /// <summary>
        /// Render the beginning of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (CmsUtilities.IsEditing || !String.IsNullOrEmpty(this.ElementName))
            {
                string tag = String.IsNullOrEmpty(this.ElementName) ? "div" : this.ElementName.ToLowerInvariant();

                writer.WriteBeginTag(tag);
                writer.WriteAttribute("id", this.ID);
                if (this.CssClass != null && this.CssClass.Length > 0) writer.WriteAttribute("class", this.CssClass);
                if (this.ToolTip != null && this.ToolTip.Length > 0 && CmsUtilities.IsEditing) writer.WriteAttribute("title", this.ToolTip);
                writer.Write(">");
            }

            if (this.headerTemplate != null && (CmsUtilities.IsEditing || HasContent))
            {
                XhtmlContainer header = new XhtmlContainer();
                headerTemplate.InstantiateIn(header);
                header.RenderControl(writer);
            }

        }

        /// <summary>
        /// Render the end of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (this.footerTemplate != null && (CmsUtilities.IsEditing || HasContent))
            {
                XhtmlContainer footer = new XhtmlContainer();
                footerTemplate.InstantiateIn(footer);
                footer.RenderControl(writer);
            }

            if (CmsUtilities.IsEditing || !String.IsNullOrEmpty(this.ElementName))
            {
                string tag = String.IsNullOrEmpty(this.ElementName) ? "div" : this.ElementName.ToLowerInvariant();
                writer.WriteEndTag(tag);
            }
        }


        /// <summary>
        /// Allow the setting of a default value using the DefaultValue property
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPopulatingDefaultContent(PlaceholderControlCancelEventArgs e)
        {
            if (!e.Cancel)
            {
                this.Html = this.defaultValue;
            }
        }

        /// <summary>
        /// Causes this placeholder control to save its local data into its <see cref="P:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.BoundPlaceholder"/>.
        /// </summary>
        /// <param name="e">A <see cref="T:Microsoft.ContentManagement.WebControls.PlaceholderControlSaveEventArgs"/> that contains the save event data from the WebAuthorContext.</param>
        /// <remarks>
        /// Derived classes must implement this method, which is called to save the local data of the control into the <see cref="P:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.BoundPlaceholder"/>. The specific types of data for the particular <see cref="T:Microsoft.ContentManagement.Publishing.Placeholder"/> type are saved into the <see cref="P:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.BoundPlaceholder"/> at this point from the local values in this control. This is triggered by the WebAuthorContext save and preview events. This method is called immediately after the placeholder SavingContent event, if it is not cancelled, and then after the SavePlaceholderContent method finishes, the SavedContent event is raised.
        /// </remarks>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            // Prevent saving if the control is not visible. It will always wipe out the data otherwise. 
            // Can't just test .Visible though, because that can be true even when an ancestor control 
            // is invisible, effectively making this invisible too.
            //
            // Important to get this right because it allows a technique where you have two copies of the
            // placeholder on the page which show up in different circumstances. Without this, if the 
            // second one is the hidden one, it overwrites the content from the first one with an empty string.
            Control checkControl = this;
            bool placeholderIsVisible = true;

            while (placeholderIsVisible && checkControl != null)
            {
                placeholderIsVisible = placeholderIsVisible && checkControl.Visible;
                checkControl = checkControl.Parent;
            }

            if (placeholderIsVisible) this.Html = this.editControl.Text;
        }

        /// <summary>
        /// Tidy up the XHTML when saving the placeholder contents
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnSavingContent(PlaceholderControlSavingEventArgs e)
        {
            if (this.editControl.Text.Length > 0)
            {
                this.editControl.Text = EsccWebTeam.Data.Web.Html.FixTinyMceOutput(this.editControl.Text);
                this.editControl.Text = CmsUtilities.TidyXhtml(this.editControl.Text);
                if (this.Paragraphs) this.editControl.Text = EsccWebTeam.Data.Web.Html.FormatAsHtmlParagraphs(this.editControl.Text);
                this.editControl.Text = Regex.Replace(this.editControl.Text, "<blockquote>(?<Blockquote>.*?)</blockquote>", new MatchEvaluator(Blockquote_MatchEvaluator), RegexOptions.Singleline);
                this.editControl.Text = ForceFirstLetterToUppercase(this.editControl.Text);

                // HACK: Recognise and mark up election links. Page also needs the OpenElectionData topic section to create the container tag for this RDFa.
                this.editControl.Text = this.editControl.Text.Replace(" about=\"http://openelectiondata.org/id/elections/21/2005-05-05\" rel=\"foaf:isPrimaryTopicOf\" typeof=\"openelection:Election\"", String.Empty);
                this.editControl.Text = this.editControl.Text.Replace(" about=\"http://openelectiondata.org/id/elections/21/2009-06-04\" rel=\"foaf:isPrimaryTopicOf\" typeof=\"openelection:Election\"", String.Empty);
                this.editControl.Text = this.editControl.Text.Replace(" about=\"http://openelectiondata.org/id/elections/21/2013-05-02\" rel=\"foaf:isPrimaryTopicOf\" typeof=\"openelection:Election\"", String.Empty);
                this.editControl.Text = Regex.Replace(this.editControl.Text, "( href=\"[/a-zA-Z0-9]+/election2005/default.aspx\")", "$1 about=\"http://openelectiondata.org/id/elections/21/2005-05-05\" rel=\"foaf:isPrimaryTopicOf\" typeof=\"openelection:Election\"");
                this.editControl.Text = Regex.Replace(this.editControl.Text, "( href=\"[/a-zA-Z0-9]+/election2009/default.aspx\")", "$1 about=\"http://openelectiondata.org/id/elections/21/2009-06-04\" rel=\"foaf:isPrimaryTopicOf\" typeof=\"openelection:Election\"");
                this.editControl.Text = Regex.Replace(this.editControl.Text, "( href=\"[/a-zA-Z0-9]+/election2013/default.aspx\")", "$1 about=\"http://openelectiondata.org/id/elections/21/2013-05-02\" rel=\"foaf:isPrimaryTopicOf\" typeof=\"openelection:Election\"");


                if (this.allowedElements.Count > 0)
                {
                    // Find all HTML tags in the placeholder
                    MatchCollection xhtmlTags = Regex.Matches(this.editControl.Text, "(?<tagStart></?)(?<tagName>[a-z]+)(?<tagEnd> .*?>|>)", RegexOptions.IgnoreCase);

                    // Strip any tags not allowed
                    foreach (Match tag in xhtmlTags)
                    {
                        if (!this.allowedElements.Contains(tag.Groups["tagName"].Value))
                        {
                            this.editControl.Text = this.editControl.Text.Replace(tag.Value, String.Empty);
                        }
                    }
                }
            }

            // Support validation of page
            Page.Validate();
            if (!Page.IsValid)
            {
                e.Cancel = true;
            }
            else
            {
                base.OnSavingContent(e);
            }
        }

        /// <summary>
        /// Ensures the first letter in the placeholder is uppercase, eg for placeholders containing a heading
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string ForceFirstLetterToUppercase(string html)
        {
            var index = 0;
            var length = html.Length;
            var outOfTag = true;

            while (index < length)
            {
                if (html[index] == '<')
                {
                    outOfTag = false;
                }
                else if (html[index] == '>')
                {
                    outOfTag = true;
                }
                else if (outOfTag)
                {
                    // Include numbers even though ToUpper() won't change them, because if a placeholder starts with a number
                    // the following letter is probably not the start of a sentence or heading (eg 11am)
                    var match = Regex.Match(html[index].ToString(), "^[A-Za-z0-9]$", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        var corrected = (html.Substring(0, index) + html.Substring(index, 1).ToUpper(CultureInfo.CurrentCulture));
                        if (length > index) corrected += html.Substring(index + 1);
                        return corrected;
                    }
                }
                index++;
            }

            return html;
        }

        /// <summary>
        /// Add extra HTML to blockquotes to hang styles on
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private string Blockquote_MatchEvaluator(Match m)
        {
            var content = m.Groups["Blockquote"].Value;
            content = content.Replace("<div>", String.Empty);
            content = content.Replace("</div>", String.Empty);
            content = content.Replace(" class=\"first\"", String.Empty);
            content = content.Replace("<br />", String.Empty);

            var pos = content.IndexOf("<p>", StringComparison.Ordinal);
            if (pos > -1) content = content.Substring(0, pos) + "<p class=\"first\">" + content.Substring(pos + 3);

            return "<blockquote><div>" + content.Trim() + "</div></blockquote>";
        }

        /// <summary>
        /// When displaying in presentation mode, hide the placeholder including its containing element if the placeholder is empty
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            // Always need to fix these links in published mode, because sometimes CMS shows the
            // GUID based link in unpublished mode and wrong-format link only in published mode
            string displayHtml = CmsUtilities.FixHostHeaderLinks(this.Html);

            // Recognise and embed YouTube videos and Flickr set slideshows
            // Unfortunately they get surrounded by paragraph element, but removing or replacing that element breaks IE
            if (this.EmbedVideo)
            {
                displayHtml = MediaUtilities.RecogniseAndEmbedYouTubeUrl(displayHtml, 450, 318);
                displayHtml = MediaUtilities.RecogniseAndEmbedFlickrUrl(displayHtml, 450, 450);
            }

            if (this.cms == null) this.cms = CmsHttpContext.Current;
            if (CmsUtilities.IsSecureChannel(cms.Channel))
            {
                // In SSL channels, change internal links to non-SSL
                displayHtml = Regex.Replace(displayHtml, "href=\"(?<url>/[^\"]+)", new MatchEvaluator(this.ConvertUrlsInSslChannel), RegexOptions.IgnoreCase);
            }

            // Display size and type of inline downloads.
            // Do this on page load rather than save because downloads are routinely replaced in Resource Manager, changing the size, without updating the page.
            displayHtml = CmsUtilities.ParseAndRewriteDownloadLinks(displayHtml);

            // When you link to a page that's never been published, the original placeholder converted the link to an unpublished link.
            // Need to replicate that behaviour.
            if (cms.Mode == PublishingMode.Unpublished)
            {
                displayHtml = Regex.Replace(displayHtml, " href=\"" + @"(?<url>[A-Fa-f0-9]{8,8}-[A-Fa-f0-9]{4,4}-[A-Fa-f0-9]{4,4}-[A-Fa-f0-9]{4,4}-[A-Fa-f0-9]{12,12})\.htm" + "\"", new MatchEvaluator(DisplayCmsLink_MatchEvaluator), RegexOptions.IgnoreCase);
            }

            // Recognise proxy elibrary links and rewrite them to the actual link
            displayHtml = CmsUtilities.ParseAndRewriteElibraryLinks(displayHtml);

            // Recognise and style calendar links
            displayHtml = Regex.Replace(displayHtml, " href=\"([^\"]+" + @"\." + "calendar[^\"]*)\"", " class=\"hcal\" href=\"$1\"");

            // Recognise and style social media links
            displayHtml = CmsUtilities.ParseLinksByHostAndApplyClass(displayHtml, "twitter.com", "twitter");
            displayHtml = CmsUtilities.ParseLinksByHostAndApplyClass(displayHtml, "facebook.com", "facebook");
            displayHtml = CmsUtilities.ParseLinksByHostAndApplyClass(displayHtml, "youtube.com", "youtube");
            displayHtml = CmsUtilities.ParseLinksByHostAndApplyClass(displayHtml, "flickr.com", "flickr");

            // Recognise email links and redirect to email form
            displayHtml = RewriteEmailLinksToUseForm(displayHtml);

            this.displayControl.Controls.Add(new LiteralControl(displayHtml));
        }

        /// <summary>
        /// Ensure email address links consistently point to our form rather than requiring the user has an email client.
        /// </summary>
        /// <param name="displayHtml"></param>
        /// <returns></returns>
        private static string RewriteEmailLinksToUseForm(string displayHtml)
        {
            // Complex because, as a first line of defence, most email links have been converted into entities.
            const string mailto = "(mailto:|&#0109;&#0097;&#0105;&#0108;&#0116;&#0111;&#0058;)";
            const string anythingExceptEndAnchor = "((?!</a>).)*";

            displayHtml = Regex.Replace(displayHtml,
                    "(<a data-unpublished=\"false\" [^>]*href=\")" + mailto + "([^\"]*)(\"[^>]*>)" +
                    anythingExceptEndAnchor + "</a>",
                    match =>
                    {
                        // Get the email address, decoding from entities
                        var email = HttpUtility.HtmlDecode(match.Groups[3].Value);

                        // Get the link text. The regex used to match it allows for child tags, but matches
                        // character by character so we have to reassemble it. It may still be entities at this point.
                        var linkText = new StringBuilder();
                        foreach (Capture capture in match.Groups[5].Captures)
                        {
                            linkText.Append(capture.Value);
                        }

                        // If the link text is the email address we can't pass it in the URL as entities because that
                        // looks like a XSS attack. But we don't want to put an email address unencoded into the current
                        // page either. So instead, try to turn the first part of the email address into a real name.
                        var linkTextForUrl = HttpUtility.HtmlDecode(linkText.ToString());
                        if (linkTextForUrl.Contains("@"))
                        {
                            linkTextForUrl = linkTextForUrl.Substring(0, linkTextForUrl.IndexOf("@", StringComparison.Ordinal));
                            linkTextForUrl = Case.ToTitleCase(linkTextForUrl.Replace(".", " "));
                        }

                        // Get URL of form and HTML encode it as we reassable the link
                        var formUrl = UriFormatter.GetWebsiteEmailFormUri(email, linkTextForUrl, HttpContext.Current.Request.Url).ToString();
                        return match.Groups[1].Value + HttpUtility.HtmlEncode(formUrl) + match.Groups[4].Value + linkText + "</a>";
                    });
            return displayHtml;
        }

        /// <summary>
        /// When you link to a page that's never been published, change link to an unpublished link.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private string DisplayCmsLink_MatchEvaluator(Match match)
        {
            var link = match.Groups["url"].Value;

            var target = CmsHttpContext.Current.Searches.GetByGuid("{" + link + "}");
            if (target != null)
            {
                var posting = target as Posting;
                if (posting != null)
                {
                    link = posting.Url;
                }
                else
                {
                    var channel = target as Channel;
                    if (channel != null) link = channel.Url;
                }
            }

            return " href=\"" + link + ".htm\"";
        }

        /// <summary>
        /// </summary>
        /// <param name="presentationContainer"></param>
        /// <nodoc/>
        /// <remarks>TAGGED_AS_NODOC</remarks>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            // Don't show placeholder if there's nothing in it
            if (!this.visibleSet) this.Visible = this.HasContent;
            this.displayControl = new PlaceHolder();
            presentationContainer.Controls.Add(this.displayControl);
        }


        /// <summary>
        /// </summary>
        /// <param name="authoringContainer"></param>
        /// <nodoc/>
        /// <remarks>TAGGED_AS_NODOC</remarks>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            // Always show placeholder in edit mode
            this.Visible = true;
            this.editControl = new TextBox();
            this.editControl.ID = this.ID + "_text";
            this.editControl.TextMode = TextBoxMode.MultiLine;
            if (this.EditControlWidth > 0)
            {
                this.editControl.Style["width"] = this.EditControlWidth.ToString(CultureInfo.InvariantCulture) + "px";
            }
            else
            {
                // If not overidden, use a sensible, responsive default
                this.editControl.Style["width"] = "100%";
            }
            if (this.EditControlHeight > 0) this.editControl.Style["height"] = this.EditControlHeight.ToString(CultureInfo.InvariantCulture) + "px";
            if (!String.IsNullOrEmpty(this.EditControlClass)) this.editControl.Attributes["data-editorClass"] = this.EditControlClass;

            // Add classes representing capabilities
            var classes = new List<string> { "htmlPlaceholder" };
            if (CmsUtilities.IsEditing)
            {
                var ph = ((this.BoundPlaceholder as HtmlPlaceholder).Definition as HtmlPlaceholderDefinition);
                switch (ph.Formatting)
                {
                    case HtmlPlaceholderDefinition.SourceFormatting.TextMarkup:
                        classes.Add("textMarkup");
                        break;
                    case HtmlPlaceholderDefinition.SourceFormatting.HtmlStyles:
                        classes.Add("htmlStyles");
                        break;
                    case HtmlPlaceholderDefinition.SourceFormatting.TextMarkupAndHtmlStyles:
                        classes.Add("textMarkup");
                        classes.Add("htmlStyles");
                        break;
                    case HtmlPlaceholderDefinition.SourceFormatting.FullFormatting:
                        classes.Add("textMarkup");
                        classes.Add("htmlStyles");
                        classes.Add("fullFormatting");
                        break;
                }
                if (ph.AllowAttachments) classes.Add("allowAttachments");
                if (ph.AllowHyperlinks) classes.Add("allowHyperlinks");
                if (ph.AllowImages) classes.Add("allowImages");
                if (ph.AllowLineBreaks) classes.Add("allowLineBreaks");
            }

            var classArray = new string[classes.Count];
            classes.CopyTo(classArray);
            this.editControl.CssClass = String.Join(" ", classArray);

            authoringContainer.Controls.Add(this.editControl);
        }

        /// <summary>
        /// Causes this placeholder control to load the specific data
        /// from the <see cref="P:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.BoundPlaceholder"/> into its local data and authoring child controls.
        /// </summary>
        /// <param name="e">A <see cref="T:Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs"/> that contains the event data.</param>
        /// <remarks>
        /// Derived classes must implement this abstract method, which
        /// is called to load the data from the <see cref="P:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.BoundPlaceholder"/>
        /// into the local data in the
        /// control and the authoring child controls. The specific types of data for the
        /// particular <see cref="T:Microsoft.ContentManagement.Publishing.Placeholder"/> type are read at this point and stored locally for
        /// display or for programmatic changing. This method is called immediately after
        /// the <see cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.OnLoadingContent(Microsoft.ContentManagement.WebControls.PlaceholderControlCancelEventArgs)"/>
        /// event, if it is not cancelled, and then after the <see cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.LoadPlaceholderContentForAuthoring(Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs)"/>
        /// method finishes, the <see cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.OnLoadedContent(Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs)"/> event is raised.
        /// This method is only called when authoring controls are visible and there was no
        /// postback containing the authoring data.
        /// </remarks>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            this.editControl.Text = this.Html;
        }

        /// <summary>
        /// Takes a matched internal link and, if it's not in the current channel, prepends the http protocol and current host name
        /// </summary>
        /// <param name="m">The matched link</param>
        /// <returns>Modified link</returns>
        /// <remarks>
        /// Designed to work with virtual URIs beginning at the site root with /.
        /// 
        /// Known not to work in the following situations:
        /// <list type="">
        /// <item>where the link is to another channel which requires SSL (the link will be altered to use http://)</item>
        /// <item>where an absolute URI is linked (ie, including prototcol and host)</item>
        /// <item>where a relative URI is linked</item>
        /// </list>
        /// 
        /// Hopefully these situations will be avoidable - don't want to waste time on rare cases with code running every time the placeholder is accessed
        /// </remarks>
        private string ConvertUrlsInSslChannel(Match m)
        {
            if (this.cms == null) this.cms = CmsHttpContext.Current;
            if (this.channelUrl.Length == 0) this.channelUrl = CmsUtilities.CorrectPublishedUrl(CmsHttpContext.Current.Channel.UrlModePublished);
            string matchedUrl = m.Groups["url"].ToString();
            if (matchedUrl.Replace(this.channelUrl, "").IndexOf("/") == -1)
            {
                return "href=\"" + matchedUrl;
            }
            else
            {
                // For people that can edit the site, changing the link triggers the warning that there's an "unpublished" link on the page,
                // from CmsTipsForEditors.js in EsccWebTEam.Cms.WebAuthor project, so include an attribute which JavaScript can look for to know that the link is OK.
                var unpublished = String.Empty;
                if (CmsHttpContext.Current.UserCanModifySite)
                {
                    unpublished = "data-unpublished=\"false\" ";
                }

                return unpublished + "href=\"" + Uri.UriSchemeHttp + "://" + this.Context.Request.Url.Host + matchedUrl;
            }
        }

        /// <summary>
        /// Container for templated controls
        /// </summary>
        private class XhtmlContainer : PlaceHolder, INamingContainer { }
    }
}
