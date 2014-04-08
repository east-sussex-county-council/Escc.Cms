using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using EsccWebTeam.Cms.Placeholders;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Code-behind for a user control which can be loaded into the "latest" section of a CMS template
    /// </summary>
    public class LatestSection : UserControl
    {
        private bool? isEditing;
        private const string latestHeader = "<p><strong class=\"latest\">Latest</strong> ";
        private const string additionalHeader = "<p><span class=\"alsoToday\">Also today:</span> ";
        string modifiedCmsContent;

        /// <summary>
        /// Gets or sets the container control id.
        /// </summary>
        /// <value>The container control id.</value>
        public Control ContainerControl { get; set; }

        /// <summary>
        /// Gets or sets the latest placeholder control id.
        /// </summary>
        /// <value>The latest placeholder control id.</value>
        public BasePlaceholderControl LatestPlaceholderControl { get; set; }

        /// <summary>
        /// Gets or sets whether to insert a "latest" label. Should be true before the 2011 refresh and false after
        /// </summary>
        /// <value><c>true</c> if [insert latest label]; otherwise, <c>false</c>.</value>
        public bool InsertLatestLabel { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatestSection"/> class.
        /// </summary>
        public LatestSection()
        {
            this.ContainerControl = this.FindControl("latestBox");
            this.LatestPlaceholderControl = this.FindControl("phLatest") as BasePlaceholderControl;
            this.InsertLatestLabel = true;
        }

        /// <summary>
        /// Sets the default section layout for a "latest" section
        /// </summary>
        public static void SetDefaultSectionLayout()
        {
            SectionLayoutManager.SetDefaultSectionLayout(CmsHttpContext.Current.Posting.Placeholders, "phDefLatestSection", "EsccWebTeam.EastSussexCC/LatestSectionLayouts", "Normal");
        }

        /// <summary>
        /// Adds the appropriate latest section layout to the page.
        /// </summary>
        /// <param name="container">The container control to add the section to, which must already be added to the page control tree.</param>
        public static void SetupSection(Control container)
        {
            // Load the usercontrol appropriate to the chosen layout
            string selectedLayout = SectionLayoutManager.GetSelectedSectionLayout(CmsHttpContext.Current.Posting.Placeholders, "phDefLatestSection", "Normal");
            container.Controls.Add(container.Page.LoadControl(SectionLayoutManager.UserControlPath("EsccWebTeam.EastSussexCC/LatestSectionLayouts", selectedLayout)));
        }

        /// <summary>
        /// Gets a value indicating whether this instance is in CMS editing mode.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in CMS editing mode; otherwise, <c>false</c>.
        /// </value>
        protected bool IsEditing
        {
            get
            {
                if (this.isEditing == null)
                {
                    // get current mode
                    WebAuthorContextMode mode = WebAuthorContext.Current.Mode;
                    this.isEditing = (mode == WebAuthorContextMode.AuthoringNew || mode == WebAuthorContextMode.AuthoringReedit);
                }
                return (this.isEditing == true);
            }
        }

        /// <summary>
        /// Handles the PreRender event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_PreRender(object sender, System.EventArgs e)
        {
            // visibility
            this.ContainerControl.Visible = (IsEditing || HasCmsContent || HasAdditionalContent);

            // Automatically insert the word "Latest" if the placeholder is in use
            if (InsertLatestLabel && !IsEditing && this.ContainerControl.Visible)
            {
                if (HasAdditionalContent)
                {
                    // If there's no CMS content it's easy just to modify control collection 
                    if (this.AdditionalContentIsInline)
                    {
                        // Surround additional content with a para if required
                        this.ContainerControl.Controls.AddAt(0, new LiteralControl(latestHeader));
                        this.ContainerControl.Controls.Add(new LiteralControl("</p>"));
                    }
                    else
                    {
                        // Otherwise add the latest header above it
                        this.ContainerControl.Controls.AddAt(0, new LiteralControl(latestHeader + "</p>"));
                    }

                    // Finally, CMS content may be present but expired, so make sure it's hidden
                    this.LatestPlaceholderControl.Visible = HasCmsContent;
                }


                if (this.HasCmsContent)
                {
                    modifiedCmsContent = CmsUtilities.ShouldBePara((CmsHttpContext.Current.Posting.Placeholders["phDefLatest"] as HtmlPlaceholder).Html);

                    // If there's CMS content only then modify the placeholder HTML to insert "Latest" header

                    string header = HasAdditionalContent ? additionalHeader : latestHeader;
                    int pos = modifiedCmsContent.IndexOf("<p>");
                    if (pos == 0)
                    {
                        modifiedCmsContent = header + modifiedCmsContent.Substring(3);
                    }
                    else
                    {
                        modifiedCmsContent = header + "</p>" + modifiedCmsContent;
                    }

                    // Can't set back to placeholder.Html property because not in CMS update mode. Can't set 
                    // placeholderControl.Html at this point either because it would get overwritten. 
                    // Instead, hook up event to set placeholderControl.Html later. The above code had to
                    // run now rather than inside the LoadedContent event, because in LoadedContent we 
                    // can't modify the latestBox.Controls collection.
                    this.LatestPlaceholderControl.LoadedContent += new PlaceholderControlEventHandler(phLatest_LoadedContent);
                }
            }
        }

        /// <summary>
        /// Handles the LoadedContent event of the phLatest control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs"/> instance containing the event data.</param>
        void phLatest_LoadedContent(object sender, PlaceholderControlEventArgs e)
        {
            HtmlPlaceholderControl ph = sender as HtmlPlaceholderControl;
            ph.Html = modifiedCmsContent;
        }

        /// <summary>
        /// Determines whether the latest box has current CMS content to show
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if there is CMS content; otherwise, <c>false</c>.
        /// </returns>
        protected bool HasCmsContent
        {
            get
            {
                // Check for content first
                string cmsContent = (CmsHttpContext.Current.Posting.Placeholders["phDefLatest"] as HtmlPlaceholder).Html;
                if (cmsContent == null || cmsContent.Trim().Length == 0) return false;

                // If there is content, has it expired?
                DateTime expiryDate = DateTimePlaceholderControl.GetValue(CmsHttpContext.Current.Posting.Placeholders["phDefLatestExpiry"] as XmlPlaceholder);
                if (expiryDate == DateTime.MinValue)
                {
                    // No expiry set
                    return true;
                }
                else
                {
                    return (expiryDate > DateTime.Now);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has content other than that entered in CMS.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance has additional content; otherwise, <c>false</c>.
        /// </value>
        protected bool HasAdditionalContent
        {
            get
            {
                foreach (Control control in this.ContainerControl.Controls)
                {
                    // Note: check for PlaceHolder is actually targetting EsccWebTeam.EastSussexGovUK.ContextContainer, but can't reference
                    // that due to circular referencing rules. Shouldn't be any other PlaceHolder objects in a latest section.
                    if (control.Visible
                        && control != this.LatestPlaceholderControl
                        && !(control is BaseModeContainer)
                        && !(control is PlaceHolder)
                        && !(control is LiteralControl && String.IsNullOrEmpty((control as LiteralControl).Text.Trim()))) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether any additional content is inline content which should be surrounded by a paragraph.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if additional content is inline; otherwise, <c>false</c>.
        /// </value>
        protected bool AdditionalContentIsInline { get; set; }

    }
}
