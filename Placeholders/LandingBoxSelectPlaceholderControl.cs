using System;
using System.Web.UI;
using System.Web.UI.WebControls;
//Add reference to the Mcms API
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Used on "Landing Page with Box" template to provide a controlled list of titles for its pull-out box
    /// </summary>
    /// <exception cref="System.InvalidCastException">Thrown if bound placeholder is not a <see cref="Microsoft.ContentManagement.Publishing.Extensions.Placeholders.HtmlPlaceholder">Microsoft.ContentManagement.Publishing.Extensions.Placeholders.HtmlPlaceholder</see></exception>
    [ToolboxData("<{0}:LandingBoxSelectPlaceholderControl runat=server></{0}:LandingBoxSelectPlaceholderControl>")]
    public class LandingBoxSelectPlaceholderControl : Microsoft.ContentManagement.WebControls.BasePlaceholderControl
    {
        private DropDownList ddlList;
        private LiteralControl lblDisplay;
        private string options = "";

        /// <summary>
        /// Gets or sets a vertical-bar-separated list of text options
        /// </summary>
        public string Options
        {
            get
            {
                return this.options;
            }
            set
            {
                this.options = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LandingBoxSelectPlaceholderControl"/> class.
        /// </summary>
        public LandingBoxSelectPlaceholderControl()
        {
        }

        /// <summary>
        ///		Add child controls for authoring.
        /// </summary>
        /// 
        protected override void CreateAuthoringChildControls(BaseModeContainer authorContainer)
        {
            //The DropDown that is used
            ddlList = new DropDownList();
            string[] optionsText = this.options.Split('|');

            foreach (string option in optionsText)
            {
                ListItem li = new ListItem();
                li.Text = option;
                li.Value = option;
                ddlList.Items.Add(li);
            }

            ddlList.Items.Insert(0, new ListItem());

            //Add the DropDown that renders the choices
            authorContainer.Controls.Add(this.ddlList);
        }

        /// <summary>
        ///		Add child controls for presentation mode.
        /// </summary>
        /// 
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            //The label that is used to display the choice made in the DropDown
            lblDisplay = new LiteralControl();
            //Add the label
            presentationContainer.Controls.Add(this.lblDisplay);
        }

        /// <summary>
        /// Gets or sets the text being displayed by the placeholder
        /// </summary>
        public string Text
        {
            get
            {
                if (this.lblDisplay != null)
                    return this.lblDisplay.Text;
                else return null;
            }

            set
            {
                if (this.lblDisplay != null) this.lblDisplay.Text = value;
            }
        }

        /// <summary>
        ///		Load the Placeholder contents for authoring.
        /// </summary>
        ///
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            EnsureChildControls();
            try
            {
                HtmlPlaceholder pHtml = (HtmlPlaceholder)this.BoundPlaceholder;
                // Retrieve content from Mcms Html placeholder.
                string htmlSelectedContent = pHtml.Text;
                //Show selection in DropDownList
                ddlList.ClearSelection();
                ddlList.Items.FindByText(htmlSelectedContent).Selected = true;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }
        }

        /// <summary>
        ///		Load the Placeholder contents for presentation.
        /// </summary>
        ///
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            EnsureChildControls();

            HtmlPlaceholder pHtml = (HtmlPlaceholder)this.BoundPlaceholder;
            lblDisplay.Text = pHtml.Text;
        }

        /// <summary>
        ///		Save the placeholder contents to the Mcms placeholder.
        /// </summary>
        /// 
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            HtmlPlaceholder pHtml = (HtmlPlaceholder)this.BoundPlaceholder;

            //Error check for null placholder
            if (!pHtml.Equals(null))
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

                // Set the contents of the Placeholder
                if (placeholderIsVisible) pHtml.Html = ddlList.SelectedItem.ToString();
            }
        }
    }
}
