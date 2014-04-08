using System;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// CMS placeholder for editing a single line of unformatted text
    /// </summary>
    [ValidationProperty("Text")]
    public class TextPlaceholderControl : BasePlaceholderControl
    {
        #region Fields

        private TextBox contentEdit = new TextBox();
        private LiteralControl contentDisplay = new LiteralControl();
        private string defaultText = "";
        private string validationExpression = "";
        private bool renderContainerElement;
        private HtmlTextWriterTag elementName;
        private bool hasContent;
        private bool hasContentSet;
        private string editText;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the control used to display text in presentation mode
        /// </summary>
        /// <value>The display control.</value>
        protected LiteralControl DisplayControl
        {
            get { return contentDisplay; }
        }

        /// <summary>
        /// Gets the edit control.
        /// </summary>
        /// <value>The edit control.</value>
        public TextBox EditControl
        {
            get
            {
                return this.contentEdit;
            }
        }

        /// <summary>
        /// Gets or sets the text entered into the control.
        /// </summary>
        /// <value>The text.</value>
        public string Text
        {
            get { return this.contentEdit.Text; }
            set { this.contentEdit.Text = value; }
        }

        /// <summary>
        /// Gets or sets the edit control's text label.
        /// </summary>
        /// <value>The text label.</value>
        public string EditText
        {
            get { return this.editText; }
            set { this.editText = value; }
        }

        /// <summary>
        /// Gets or sets the default text which appears in the placeholder when the page is created
        /// </summary>
        public string DefaultText
        {
            get
            {
                return this.defaultText;
            }
            set
            {
                this.defaultText = value;
            }
        }

        /// <summary>
        /// Gets or sets the regular expression used to validate the placeholder content
        /// </summary>
        public string ValidationExpression
        {
            get
            {
                return this.validationExpression;
            }
            set
            {
                this.validationExpression = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// CMS placeholder for editing a single line of unformatted text
        /// </summary>
        public TextPlaceholderControl()
        {
            this.renderContainerElement = false;
            this.elementName = HtmlTextWriterTag.Span;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a TextBox to edit the text
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            this.contentEdit.CssClass = "singleLineTextPlaceholderControl";
            this.contentEdit.ID = "textEdit";

            if (this.editText != null && this.editText.Length > 0)
            {
                Label editLabel = new Label();
                editLabel.Text = this.editText + " ";
                authoringContainer.Controls.Add(editLabel);
                authoringContainer.Controls.Add(this.contentEdit);
                editLabel.AssociatedControlID = this.contentEdit.ID;
            }
            else
            {
                authoringContainer.Controls.Add(this.contentEdit);
            }
        }

        /// <summary>
        /// Create a logical container to display the text
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            presentationContainer.Controls.Add(this.contentDisplay);
        }

        /// <summary>
        /// Populate the editing TextBox with either the saved text or the default text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;

            // This will be the case when a saved page is edited.
            // Even if the placeholder is left blank when saving, its XML string will still have 
            // an XML declaration and empty root element.
            if (ph.XmlAsString.Length > 0)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ph.XmlAsString);
                this.contentEdit.Text = xmlDoc.DocumentElement.InnerText;
            }
            else if (this.defaultText.Length > 0)
            {
                this.contentEdit.Text = this.defaultText;
            }

        }

        /// <summary>
        /// Populate the logical container with the saved text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
            XmlDocument xmlDoc = new XmlDocument();
            if (ph.XmlAsString.Length > 0)
            {
                xmlDoc.LoadXml(ph.XmlAsString);
                this.contentDisplay.Text = xmlDoc.DocumentElement.InnerText;
            }

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
                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");
                xmlDoc.AppendChild(xmlDec);

                XmlElement rootElement = xmlDoc.CreateElement("Text");
                xmlDoc.AppendChild(rootElement);
                rootElement.AppendChild(xmlDoc.CreateTextNode(this.contentEdit.Text));

                ph.XmlAsString = xmlDoc.InnerXml;
            }
        }


        /// <summary>
        /// If a validation template has been specified and the value doesn't match, cancel the save
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSavingContent(PlaceholderControlSavingEventArgs e)
        {
            this.EnsureChildControls();

            if (this.contentEdit.Text.Length > 0 && this.validationExpression.Length > 0 && !Regex.IsMatch(this.contentEdit.Text, this.validationExpression))
            {
                e.Cancel = true;
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
        /// Get the text value from a SingleLineTextPlaceholderControl
        /// </summary>
        /// <param name="ph">An XmlPlaceholder which is bound to a SingleLineTextPlaceholderControl</param>
        /// <returns>The text value entered into the control</returns>
        public static string GetValue(XmlPlaceholder ph)
        {

            if (ph.XmlAsString.Length > 0)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ph.XmlAsString);
                return xmlDoc.DocumentElement.InnerText;
            }

            return String.Empty;
        }

        #endregion

        /// <summary>
        /// Gets whether anything has been saved in this placeholder
        /// </summary>
        public bool HasContent
        {
            get
            {
                if (this.hasContentSet) return this.hasContent;

                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
                string phValue = TextPlaceholderControl.GetValue(ph);
                this.hasContent = (phValue != null && phValue.Length > 0);
                this.hasContentSet = true;
                return this.hasContent;
            }
        }

        /// <summary>
        /// Gets or sets whether to render a containing XHTML element in presentation mode (the default CMS behaviour)
        /// </summary>
        public bool RenderContainerElement
        {
            get
            {
                return this.renderContainerElement;
            }
            set
            {
                this.renderContainerElement = value;
            }
        }


        /// <summary>
        /// Gets or sets whether the XHTML element to use when rendering a container element
        /// </summary>
        public HtmlTextWriterTag ElementName
        {
            get
            {
                return this.elementName;
            }
            set
            {
                if (
                    value == HtmlTextWriterTag.Div ||
                    value == HtmlTextWriterTag.Span ||
                    value == HtmlTextWriterTag.P ||
                    value == HtmlTextWriterTag.Dt ||
                    value == HtmlTextWriterTag.Li ||
                    value == HtmlTextWriterTag.Td ||
                    value == HtmlTextWriterTag.Strong ||
                    value == HtmlTextWriterTag.Em ||
                    value == HtmlTextWriterTag.H1 ||
                    value == HtmlTextWriterTag.H2 ||
                    value == HtmlTextWriterTag.H3 ||
                    value == HtmlTextWriterTag.H4 ||
                    value == HtmlTextWriterTag.H5 ||
                    value == HtmlTextWriterTag.H6
                    )
                {
                    this.elementName = value;
                }
                else
                {
                    throw new ApplicationException("SingleLineTextPlaceholderControl can only use the following elements: div, span, p, td, dt, li, strong, em, h1, h2, h3, h4, h5, h6");
                }
            }
        }

        /// <summary>
        /// Render the beginning of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew ||
                WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit ||
                (this.HasContent && this.renderContainerElement))
            {
                writer.WriteBeginTag(this.elementName.ToString().ToLower());
                writer.WriteAttribute("id", this.ID);
                if (this.CssClass.Length > 0) writer.WriteAttribute("class", this.CssClass);
                writer.Write(">");
            }
        }

        /// <summary>
        /// Render the end of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew ||
                WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit ||
                (this.HasContent && this.renderContainerElement))
            {
                writer.WriteEndTag(this.elementName.ToString().ToLower());
            }
        }

    }
}
