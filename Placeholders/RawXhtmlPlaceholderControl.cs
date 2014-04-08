using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// CMS placeholder for adding raw XHTML to a template
    /// </summary>
    public class RawXhtmlPlaceholderControl : BasePlaceholderControl
    {
        TextBox xmlBox = new TextBox();
        PlaceHolder container = new PlaceHolder();
        string xmlDeclaration;
        string defaultXml;
        private string text = String.Empty;

        /// <summary>
        /// Text of the edit control label
        /// </summary>
        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                this.text = value;
            }
        }

        /// <summary>
        /// Gets the default Xml which represents an empty placeholder
        /// </summary>
        public string DefaultXml
        {
            get { return this.xmlDeclaration + this.defaultXml; }
        }

        /// <summary>
        /// CMS placeholder for adding raw XHTML to a template
        /// </summary>
        public RawXhtmlPlaceholderControl()
        {
            this.xmlDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
            this.defaultXml = "<DefaultXHTML />";
            this.xmlBox.ID = this.ID + "EditControl";
        }

        /// <summary>
        /// Create a textbox containing the XHTML
        /// </summary>
        /// <param name="authoringContainer">The placeholder <span></span> element</param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            this.xmlBox.TextMode = TextBoxMode.MultiLine;
            if (String.IsNullOrEmpty(this.Text))
            {
                using (var container = new HtmlGenericControl("div"))
                {
                    container.Attributes["class"] = "rawXhtmlPlaceholder";
                    container.Controls.Add(this.xmlBox);
                    authoringContainer.Controls.Add(container);
                }
            }
            else
            {
                using (Label label = new Label())
                {
                    label.AssociatedControlID = this.xmlBox.ID;
                    label.Attributes["class"] = "rawXhtmlPlaceholder";

                    label.Controls.Add(new LiteralControl(this.Text));
                    label.Controls.Add(this.xmlBox);
                    authoringContainer.Controls.Add(label);
                }
            }
        }

        /// <summary>
        /// Display the XHTML when in published mode
        /// </summary>
        /// <param name="presentationContainer">The placeholder <span></span> element</param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            presentationContainer.Controls.Add(this.container);
        }

        /// <summary>
        /// Load the XHTML for editing
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            this.xmlBox.Text = ((XmlPlaceholder)this.BoundPlaceholder).XmlAsString.Replace(this.xmlDeclaration, "").Replace(this.defaultXml, "");
            this.Visible = true;
        }

        /// <summary>
        /// Load the XHTML into the page
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            string xml = ((XmlPlaceholder)this.BoundPlaceholder).XmlAsString.Replace(this.xmlDeclaration, "").Replace(this.defaultXml, "");
            if (xml.Trim().Length > 0)
            {
                this.container.Controls.Add(new LiteralControl(xml));
                this.Visible = true;
            }
            else this.Visible = false;
        }

        /// <summary>
        /// Save changes to the XML when the page is saved
        /// </summary>
        /// <param name="e"></param>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            this.EnsureChildControls();

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;

            if (this.xmlBox.Text.Trim().Length > 0)
            {
                // get XML from text box
                ph.XmlAsString = this.xmlDeclaration + this.xmlBox.Text;
            }
            else
            {
                // save default XML to avoid "root element not found" error when there's no content
                ph.XmlAsString = this.xmlDeclaration + this.defaultXml;
            }
        }

        /// <summary>
        /// No span tag
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
        }

        /// <summary>
        /// No span tag
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
        }
    }
}
