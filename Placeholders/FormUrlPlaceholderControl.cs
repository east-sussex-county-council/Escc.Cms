using System;
using System.Reflection;
using System.Resources;
using System.Web;
using System.Web.UI.HtmlControls;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Placeholder for the URL of an XHTML form, which links to the form using standard link text
    /// </summary>
    public class FormUrlPlaceholderControl : TextPlaceholderControl
    {
        private HtmlAnchor link;
        private ResourceManager resManager;

        /// <summary>
        /// Gets or sets the resource manager key for the link text
        /// </summary>
        public string LinkText { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormUrlPlaceholderControl"/> class.
        /// </summary>
        public FormUrlPlaceholderControl()
        {
            this.LinkText = "FormAttachmentXhtml";
            this.resManager = (ResourceManager)this.Context.Cache.Get("FormPlaceholderControls");
            if (this.resManager == null)
            {
                this.resManager = new ResourceManager("EsccWebTeam.Cms.Properties.Resources", Assembly.GetExecutingAssembly());
                this.Context.Cache.Insert("FormPlaceholderControls", this.resManager, null, DateTime.MaxValue, TimeSpan.FromMinutes(10));
            }
        }


        /// <summary>
        /// Create a link using standard text
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            this.link = new HtmlAnchor();
            this.link.InnerText = this.resManager.GetString(this.LinkText);

            presentationContainer.Controls.Add(this.link);
        }

        /// <summary>
        /// Populate the logical container with the saved text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();
            base.LoadPlaceholderContentForPresentation(e);
            this.link.HRef = HttpUtility.HtmlEncode(this.DisplayControl.Text.Trim());
            this.Visible = (this.link.HRef.Length > 0);
        }


    }
}
