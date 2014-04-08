using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// CMS placeholder for choosing an option using a checkbox
    /// </summary>
    public class CheckBoxPlaceholderControl : BasePlaceholderControl
    {
        #region Fields

        private CheckBox contentSelect = new CheckBox();
        private LiteralControl contentDisplay = new LiteralControl();
        private string text = String.Empty;
        private bool renderContainerElement;
        private HtmlTextWriterTag elementName;
        private bool displayTextWhenPublished;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether to display the text in presentation mode if the checkbox is checked
        /// </summary>
        public bool DisplayTextWhenPublished
        {
            get
            {
                return this.displayTextWhenPublished;
            }
            set
            {
                this.displayTextWhenPublished = value;
            }
        }

        /// <summary>
        /// Gets or sets the text which appears in presentation mode if the placeholder is selected
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
        /// Gets whether the checkbox was checked. Synonym for HasContent property.
        /// </summary>
        public bool Checked
        {
            get { return this.HasContent; }
        }

        /// <summary>
        /// Gets or sets whether the checkbox is checked by default if no value has been saved
        /// </summary>
        public bool DefaultChecked { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// CMS placeholder for choosing whether to show a single preset line of unformatted text
        /// </summary>
        public CheckBoxPlaceholderControl()
        {
            this.renderContainerElement = false;
            this.elementName = HtmlTextWriterTag.P;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a Checkbox to select
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            this.contentSelect.CssClass = "CheckBoxPlaceholderControl";
            this.contentSelect.ID = "textSelect";
            this.contentSelect.Text = this.Text;
            authoringContainer.Controls.Add(this.contentSelect);
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
        /// Either select the checkbox or not, depending on saved value
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            string xml = ReadXmlFromPlaceholder();

            if (xml.Length > 0)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                this.contentSelect.Checked = Boolean.Parse(xmlDoc.DocumentElement.InnerText);
            }
            else this.contentSelect.Checked = this.DefaultChecked;
        }

        /// <summary>
        /// Reads the XML from either an XmlPlaceholder or an HtmlPlaceholder.
        /// </summary>
        private string ReadXmlFromPlaceholder()
        {
            string xml = String.Empty;
            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
            if (ph != null)
            {
                xml = ph.XmlAsString;
            }
            else
            {
                HtmlPlaceholder html = this.BoundPlaceholder as HtmlPlaceholder;
                if (html != null && html.Html.IndexOf("&lt;") > -1) xml = HttpUtility.HtmlDecode(html.Html);
            }
            return xml;
        }

        /// <summary>
        /// Populate the logical container with the saved text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            if (this.displayTextWhenPublished)
            {
                string xml = ReadXmlFromPlaceholder();
                if (xml.Length > 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);
                    this.Visible = Boolean.Parse(xmlDoc.DocumentElement.InnerText);
                }
                else
                {
                    this.Visible = false;
                }
                this.contentDisplay.Text = this.Text;
            }
            else this.Visible = false;
        }

        /// <summary>
        /// Create a well-formed XML document to save the text
        /// </summary>
        /// <param name="e"></param>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");
            xmlDoc.AppendChild(xmlDec);

            XmlElement rootElement = xmlDoc.CreateElement("Selected");
            xmlDoc.AppendChild(rootElement);
            rootElement.AppendChild(xmlDoc.CreateTextNode(this.contentSelect.Checked.ToString(CultureInfo.CurrentCulture)));

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
            if (ph != null)
            {
                ph.XmlAsString = xmlDoc.InnerXml;
            }
            else
            {
                HtmlPlaceholder html = this.BoundPlaceholder as HtmlPlaceholder;
                if (html != null) html.Html = HttpUtility.HtmlEncode(xmlDoc.InnerXml);
            }
        }


        /// <summary>
        /// Gets whether the author has set the placeholder to be displayed in presentation mode
        /// </summary>
        /// <param name="ph">An XmlPlaceholder which is bound to a CheckBoxPlaceholderControl</param>
        /// <returns>True if selected; false otherwise</returns>
        public static bool GetValue(XmlPlaceholder ph)
        {
            if (ph.XmlAsString.Length > 0)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ph.XmlAsString);
                return Boolean.Parse(xmlDoc.DocumentElement.InnerText);
            }
            return false;
        }

        /// <summary>
        /// Gets whether the author has set the placeholder to be displayed in presentation mode
        /// </summary>
        /// <param name="ph">An HtmlPlaceholder which is bound to a CheckBoxPlaceholderControl</param>
        /// <returns>True if selected; false otherwise</returns>
        public static bool GetValue(HtmlPlaceholder ph)
        {
            if (ph.Html.Length > 0 && ph.Html.IndexOf("&lt;") > -1)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(HttpUtility.HtmlDecode(ph.Html));
                return Boolean.Parse(xmlDoc.DocumentElement.InnerText);
            }
            return false;
        }
        #endregion

        /// <summary>
        /// Gets whether the author has set the placeholder to be displayed in presentation mode
        /// </summary>
        public bool HasContent
        {
            get
            {
                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
                return (ph != null) ? CheckBoxPlaceholderControl.GetValue(ph) : false;
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
                    throw new ApplicationException("CheckBoxPlaceholderControl can only use the following elements: div, span, p, td, dt, li, strong, em, h1, h2, h3, h4, h5, h6");
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
                this.renderContainerElement)
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
                this.renderContainerElement)
            {
                writer.WriteEndTag(this.elementName.ToString().ToLower());
            }
        }

    }
}
