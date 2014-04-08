using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.XPath;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// CMS placeholder for choosing social media options
    /// </summary>
    [ValidationProperty("TwitterScript")]
    public class SocialMediaPlaceholderControl : BasePlaceholderControl
    {
        #region Fields

        private TextBox twitterSearch;
        private TextBox twitterWidget;
        private TextBox facebookLike;
        private CheckBox facebookLikeFaces;
        private CheckBox facebookLikeFeed;
        private LiteralControl contentDisplay;
        private RadioButtonList order;

        #endregion

        #region Methods

        /// <summary>
        /// Gets or sets the script pasted in for a Twitter widget
        /// </summary>
        public string TwitterScript
        {

            get
            {
                if (this.twitterWidget != null)
                {
                    return this.twitterWidget.Text;
                }
                else
                {
                    return String.Empty;
                }
            }
            set
            {
                if (this.twitterWidget != null)
                {
                    this.twitterWidget.Text = value;
                }
                else
                {
                    throw new InvalidOperationException("Twitter script is not editable at this point");
                }
            }
        }

        /// <summary>
        /// Create a TextBox to edit the text
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            using (var container = new HtmlGenericControl("div"))
            {
                authoringContainer.Controls.Add(container);

                using (var h2 = new HtmlGenericControl("h2"))
                {
                    h2.InnerText = "Social media";
                    container.Controls.Add(h2);
                }

                // Twitter widget - create on twitter.com and copy the code
                var twitterWidgetLabel = new Label();
                twitterWidgetLabel.Text = "Twitter widget code (copy from <a href=\"http://twitter.com/settings/widgets\">http://twitter.com/settings/widgets</a>): ";
                container.Controls.Add(twitterWidgetLabel);

                this.twitterWidget = new TextBox();
                this.twitterWidget.TextMode = TextBoxMode.MultiLine;
                this.twitterWidget.ID = "twitterCode";
                container.Controls.Add(this.twitterWidget);
                twitterWidgetLabel.AssociatedControlID = this.twitterWidget.ID;

                // Twitter search - obsolete because Twitter crippled it to show only old tweets, but leave for
                // backwards compatibility until everyone's switched to new widget.
                var twitterSearchLabel = new Label();
                twitterSearchLabel.Text = "Twitter search (obsolete): ";
                container.Controls.Add(twitterSearchLabel);

                this.twitterSearch = new TextBox();
                this.twitterSearch.ID = "twitterText";
                container.Controls.Add(this.twitterSearch);
                twitterSearchLabel.AssociatedControlID = this.twitterSearch.ID;

                // Facebook Like box
                var facebookLikeLabel = new Label();
                facebookLikeLabel.Text = "Facebook page: ";
                container.Controls.Add(facebookLikeLabel);

                this.facebookLike = new TextBox();
                this.facebookLike.ID = "facebookLike";
                container.Controls.Add(this.facebookLike);
                facebookLikeLabel.AssociatedControlID = this.facebookLike.ID;

                using (var facebookOptions = new HtmlGenericControl("div"))
                {
                    facebookOptions.Attributes["class"] = "radioButtonList";
                    container.Controls.Add(facebookOptions);

                    this.facebookLikeFaces = new CheckBox();
                    this.facebookLikeFaces.ID = "facebookLikeFaces";
                    this.facebookLikeFaces.Text = "Show faces";
                    facebookOptions.Controls.Add(facebookLikeFaces);

                    this.facebookLikeFeed = new CheckBox();
                    this.facebookLikeFeed.ID = "facebookLikeFeed";
                    this.facebookLikeFeed.Text = "Show news feed";
                    facebookOptions.Controls.Add(facebookLikeFeed);
                }

                // Layout
                this.order = new RadioButtonList();
                this.order.RepeatDirection = RepeatDirection.Horizontal;
                this.order.RepeatLayout = RepeatLayout.Flow;
                this.order.CssClass = "radioButtonList";
                this.order.Items.Add("Twitter first");
                this.order.Items.Add("Facebook first");
                container.Controls.Add(this.order);
            }

        }

        /// <summary>
        /// Create a logical container to display the text
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            // Add literal as target for any custom HTML
            this.contentDisplay = new LiteralControl();
            presentationContainer.Controls.Add(this.contentDisplay);
        }

        /// <summary>
        /// Populate the editing TextBox with either the saved text or the default text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            var value = SocialMediaPlaceholderControl.GetValue(this.BoundPlaceholder);

            // HTML is stored as HTML encoded string because it's not well-formed XML, so decode it before display
            this.twitterWidget.Text = HttpUtility.HtmlDecode(value.TwitterWidget);
            this.twitterSearch.Text = value.TwitterSearch;
            if (value.FacebookLikeUrl != null) this.facebookLike.Text = value.FacebookLikeUrl.ToString();
            this.facebookLikeFaces.Checked = value.FacebookShowFaces;
            this.facebookLikeFeed.Checked = value.FacebookShowFeed;

            if (value.TwitterSearchPosition == 1 || value.FacebookLikePosition == 2)
            {
                this.order.ClearSelection();
                this.order.Items[0].Selected = true;
            }
            else if (value.FacebookLikePosition == 1 || value.TwitterSearchPosition == 2)
            {
                this.order.ClearSelection();
                this.order.Items[1].Selected = true;
            }
        }

        /// <summary>
        /// Populate the logical container with the saved text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            var value = SocialMediaPlaceholderControl.GetValue(this.BoundPlaceholder);
            this.contentDisplay.Text += String.Format(CultureInfo.CurrentCulture, "<p>Twitter widget: {0}</p>", String.IsNullOrEmpty(value.TwitterWidget) ? "None" : "yes");
            this.contentDisplay.Text += String.Format(CultureInfo.CurrentCulture, "<p>Twitter search (obsolete): {0}</p>", String.IsNullOrEmpty(value.TwitterSearch) ? "None" : "'" + value.TwitterSearch + "'");
            this.contentDisplay.Text += String.Format(CultureInfo.CurrentCulture, "<p>Facebook page: {0}</p>", (value.FacebookLikeUrl == null) ? "None" : value.FacebookLikeUrl.ToString());
            this.contentDisplay.Text += String.Format(CultureInfo.CurrentCulture, "<p>Facebook show faces: {0}</p>", value.FacebookShowFaces ? "yes" : "no");
            this.contentDisplay.Text += String.Format(CultureInfo.CurrentCulture, "<p>Facebook show feed: {0}</p>", value.FacebookShowFeed ? "yes" : "no");
            this.contentDisplay.Text += String.Format(CultureInfo.CurrentCulture, "<p>Show Facebook first?: {0}</p>", value.FacebookLikePosition == 1 ? "yes" : "no");

        }

        /// <summary>
        /// Create a well-formed XML document to save the text
        /// </summary>
        /// <param name="e"></param>
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

            if (placeholderIsVisible)
            {
                // Create basic XML document to hold options
                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");
                xmlDoc.AppendChild(xmlDec);

                XmlElement rootElement = xmlDoc.CreateElement("SocialMedia");
                xmlDoc.AppendChild(rootElement);

                // Save options for Twitter search
                if (!String.IsNullOrEmpty(this.twitterSearch.Text.Trim()))
                {
                    var twitterSearchXml = xmlDoc.CreateElement("TwitterSearch");
                    rootElement.AppendChild(twitterSearchXml);

                    var search = xmlDoc.CreateAttribute("search");
                    search.Value = SecurityElement.Escape(this.twitterSearch.Text.Trim());
                    twitterSearchXml.Attributes.Append(search);

                    var position = xmlDoc.CreateAttribute("position");
                    position.Value = (this.order.SelectedIndex == 0) ? "1" : "2";
                    twitterSearchXml.Attributes.Append(position);
                }

                // Save code for Twitter widget. 
                var twitterCode = this.twitterWidget.Text.Trim();
                if (!String.IsNullOrEmpty(twitterCode))
                {
                    // Ensure the "Opt-out of tailoring Twitter" option is always turned on. 
                    if (twitterCode.IndexOf("data-dnt") == -1)
                    {
                        twitterCode = twitterCode.Replace("<a ", "<a data-dnt=\"true\" ");
                    }

                    var twitterWidgetXml = xmlDoc.CreateElement("TwitterWidget");
                    // Store HTML as HTML encoded string because it's not well-formed XML.
                    twitterWidgetXml.InnerXml = HttpUtility.HtmlEncode(twitterCode);
                    rootElement.AppendChild(twitterWidgetXml);

                    var position = xmlDoc.CreateAttribute("position");
                    position.Value = (this.order.SelectedIndex == 0) ? "1" : "2";
                    twitterWidgetXml.Attributes.Append(position);
                }


                // Save options for Facebook Like box
                if (!String.IsNullOrEmpty(this.facebookLike.Text.Trim()))
                {
                    var facebookLikeXml = xmlDoc.CreateElement("FacebookLike");
                    rootElement.AppendChild(facebookLikeXml);

                    var pageUrl = xmlDoc.CreateAttribute("page-url");
                    pageUrl.Value = SecurityElement.Escape(this.facebookLike.Text.Trim());
                    facebookLikeXml.Attributes.Append(pageUrl);

                    var faces = xmlDoc.CreateAttribute("show-faces");
                    faces.Value = this.facebookLikeFaces.Checked ? "true" : "false";
                    facebookLikeXml.Attributes.Append(faces);

                    var feed = xmlDoc.CreateAttribute("show-feed");
                    feed.Value = this.facebookLikeFeed.Checked ? "true" : "false";
                    facebookLikeXml.Attributes.Append(feed);

                    var position = xmlDoc.CreateAttribute("position");
                    position.Value = (this.order.SelectedIndex == 1) ? "1" : "2";
                    facebookLikeXml.Attributes.Append(position);
                }

                // Save the completed XML
                ph.XmlAsString = xmlDoc.InnerXml;
            }
        }


        /// <summary>
        /// Support validation of CMS pages
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSavingContent(PlaceholderControlSavingEventArgs e)
        {
            this.EnsureChildControls();

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

        #endregion

        /// <summary>
        /// Render the beginning of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(System.Web.UI.HtmlTextWriter writer)
        {
            // do nothing
        }

        /// <summary>
        /// Render the end of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(System.Web.UI.HtmlTextWriter writer)
        {
            // do nothing
        }


        /// <summary>
        /// Gets the settings stored by an instance of a <see cref="SocialMediaPlaceholderControl"/>.
        /// </summary>
        /// <param name="placeholder">The placeholder.</param>
        /// <returns></returns>
        public static SocialMediaPlaceholderValue GetValue(Placeholder placeholder)
        {
            if (placeholder == null) throw new ArgumentNullException("placeholder");

            XmlPlaceholder ph = placeholder as XmlPlaceholder;
            if (ph == null) throw new ArgumentException("placeholder must be an XmlPlaceholder", "placeholder");

            var value = new SocialMediaPlaceholderValue();

            if (ph.XmlAsString.Length > 0)
            {
                var xmlDoc = new XPathDocument(new StringReader(ph.XmlAsString));
                var nav = xmlDoc.CreateNavigator();

                nav = nav.SelectSingleNode("/SocialMedia/TwitterSearch");
                if (nav != null)
                {
                    value.TwitterSearch = nav.GetAttribute("search", String.Empty);
                    try
                    {
                        value.TwitterSearchPosition = Convert.ToInt32(nav.GetAttribute("position", String.Empty));
                    }
                    catch (FormatException)
                    {

                    }
                }

                nav = xmlDoc.CreateNavigator();
                nav = nav.SelectSingleNode("/SocialMedia/TwitterWidget");
                if (nav != null)
                {
                    value.TwitterWidget = nav.InnerXml;
                    try
                    {
                        // override the older TwitterSearchPosition with this
                        value.TwitterSearchPosition = Convert.ToInt32(nav.GetAttribute("position", String.Empty));
                    }
                    catch (FormatException)
                    {

                    }
                }

                nav = xmlDoc.CreateNavigator();
                nav = nav.SelectSingleNode("/SocialMedia/FacebookLike");
                if (nav != null)
                {
                    try
                    {
                        value.FacebookLikeUrl = new Uri(nav.GetAttribute("page-url", String.Empty));
                    }
                    catch (UriFormatException)
                    {

                    }
                    try
                    {
                        value.FacebookShowFaces = Boolean.Parse(nav.GetAttribute("show-faces", String.Empty));
                    }
                    catch (FormatException)
                    {

                    }
                    try
                    {
                        value.FacebookShowFeed = Boolean.Parse(nav.GetAttribute("show-feed", String.Empty));
                    }
                    catch (FormatException)
                    {

                    }

                    try
                    {
                        value.FacebookLikePosition = Convert.ToInt32(nav.GetAttribute("position", String.Empty));
                    }
                    catch (FormatException)
                    {

                    }
                }
            }

            return value;
        }
    }

    /// <summary>
    /// The settings stored in an instance of a <see cref="SocialMediaPlaceholderControl"/>
    /// </summary>
    public class SocialMediaPlaceholderValue
    {
        /// <summary>
        /// Gets or sets the term to search for on Twitter.
        /// </summary>
        /// <value>The search term.</value>
        public string TwitterSearch { get; set; }

        /// <summary>
        /// Gets or sets the position of the Twitter search widget.
        /// </summary>
        /// <value>The position.</value>
        public int TwitterSearchPosition { get; set; }

        /// <summary>
        /// Gets or sets the widget code copied from twitter.com
        /// </summary>
        public string TwitterWidget { get; set; }

        /// <summary>
        /// Gets or sets the URL of a Facebook page.
        /// </summary>
        /// <value>The Facebook page.</value>
        public Uri FacebookLikeUrl { get; set; }

        /// <summary>
        /// Gets or sets whether to show faces in the Facebook Like box.
        /// </summary>
        /// <value><c>true</c> to show faces; otherwise, <c>false</c>.</value>
        public bool FacebookShowFaces { get; set; }

        /// <summary>
        /// Gets or sets whether show a news feed in the Facebook like box.
        /// </summary>
        /// <value><c>true</c> to show feed; otherwise, <c>false</c>.</value>
        public bool FacebookShowFeed { get; set; }

        /// <summary>
        /// Gets or sets the position of the Facebook Like box.
        /// </summary>
        /// <value>The facebook like position.</value>
        public int FacebookLikePosition { get; set; }
    }
}
