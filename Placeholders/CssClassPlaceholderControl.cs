using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using EsccWebTeam.HouseStyle;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// CMS placeholder to select a CSS class from a list of available classes
    /// </summary>
    public class CssClassPlaceholderControl : BasePlaceholderControl
    {
        private DropDownList classList;
        private Label cssLabel;
        private string cssClasses;
        private string selectedCssClass;
        private string text = "Appearance: ";
        private bool classLoaded;

        /// <summary>
        /// Gets or sets the dropdown list label in authoring mode
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
        /// Gets the CSS class selected using the control
        /// </summary>
        public string SelectedCssClass
        {
            get
            {
                if (this.classLoaded) return this.selectedCssClass;
                else return this.GetValue();
            }
        }

        /// <summary>
        /// Gets or sets a comma-separated list of CSS class names in CamelCase
        /// </summary>
        public string CssClasses
        {
            get
            {
                return this.cssClasses;
            }
            set
            {
                this.cssClasses = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CssClassPlaceholderControl"/> class.
        /// </summary>
        public CssClassPlaceholderControl()
        {
        }

        /// <summary>
        /// Notifies placeholder controls to add the controls used for editing the <see cref="T:Microsoft.ContentManagement.Publishing.Placeholder"/> content into the authorContainer.
        /// </summary>
        /// <param name="authoringContainer">A Container for authoring mode in which to add controls.</param>
        /// <remarks>
        /// 	<para>The controls described in <paramref name="authoringContainer"/> are used to
        /// edit the placeholder content for this control.</para>
        /// 	<para> Derived classes must implement this method, which is called during the creation of the child controls in authoring modes. The placeholder control must add the controls used for editing the <see cref="T:Microsoft.ContentManagement.Publishing.Placeholder"/> content to the Controls collection of <paramref name="authoringContainer"/>.</para>
        /// </remarks>
        /// <seealso cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.CreatePresentationChildControls(Microsoft.ContentManagement.WebControls.BaseModeContainer)"/>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            this.classList = new DropDownList();
            this.classList.Items.Add(new ListItem());
            if (this.cssClasses.Length > 0)
            {
                string[] cssClasses = this.cssClasses.Split(',');
                foreach (string cssClass in cssClasses)
                {
                    ListItem item = new ListItem(Case.PascalToTitle(cssClass).Replace("-", " "), cssClass);
                    this.classList.Items.Add(item);
                }
            }
            this.classList.ID = "cssClassList";

            this.cssLabel = new Label();
            this.cssLabel.AssociatedControlID = this.classList.ID;
            this.cssLabel.Controls.Add(new LiteralControl(this.text));
            this.cssLabel.Controls.Add(this.classList);
            authoringContainer.Controls.Add(this.cssLabel);
        }

        /// <summary>
        /// Notifies placeholder controls to add the controls used for displaying the <see cref="T:Microsoft.ContentManagement.Publishing.Placeholder"/> content into the <paramref name="presentationContainer"/>.
        /// </summary>
        /// <param name="presentationContainer">A Container that holds controls used in presentation mode to display the placeholder content.</param>
        /// <remarks>
        /// Derived classes must implement this method, which is called during the creation of the child controls in presentation modes. The placeholder control must add the controls used for displaying the <see cref="T:Microsoft.ContentManagement.Publishing.Placeholder"/> content to the Controls collection of <paramref name="presentationContainer"/>.
        /// </remarks>
        /// <seealso cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.CreateAuthoringChildControls(Microsoft.ContentManagement.WebControls.BaseModeContainer)"/>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {

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
            this.EnsureChildControls();

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;

            if (ph.XmlAsString.Length > 0)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ph.XmlAsString);
                this.classList.SelectedItem.Selected = false;
                ListItem selectedItem = this.classList.Items.FindByValue(xmlDoc.DocumentElement.InnerText);
                if (selectedItem != null) selectedItem.Selected = true;
            }
        }

        /// <summary>
        /// Causes the placeholder control to load the specific data
        /// from the <see cref="P:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.BoundPlaceholder"/> into its local data and presentation child controls.
        /// </summary>
        /// <param name="e">A <see cref="T:Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs"/> that contains the event data.</param>
        /// <remarks>
        /// 	<para> Derived classes must implement this method, which is called
        /// to load the data from the <see cref="P:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.BoundPlaceholder"/> into the local
        /// data in the control and in its presentation child controls. The specific types
        /// of data for the particular <see cref="T:Microsoft.ContentManagement.Publishing.Placeholder"/> type are read at this point and stored
        /// locally for display or for programmatic changing. This method is called
        /// immediately after the <see cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.OnLoadingContent(Microsoft.ContentManagement.WebControls.PlaceholderControlCancelEventArgs)"/> event, if it is
        /// not cancelled, and then after the <see cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.LoadPlaceholderContentForPresentation(Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs)"/>
        /// method finishes, the <see cref="M:Microsoft.ContentManagement.WebControls.BasePlaceholderControl.OnLoadedContent(Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs)"/>
        /// event is raised.</para>
        /// 	<para> This method is only called when presentation controls are visible. </para>
        /// </remarks>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;

            if (ph.XmlAsString.Length > 0)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ph.XmlAsString);
                this.selectedCssClass = xmlDoc.DocumentElement.InnerText;
                this.classLoaded = true;
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
            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");
            xmlDoc.AppendChild(xmlDec);

            XmlElement rootElement = xmlDoc.CreateElement("Text");
            xmlDoc.AppendChild(rootElement);
            rootElement.AppendChild(xmlDoc.CreateTextNode(this.classList.SelectedItem.Value));

            ph.XmlAsString = xmlDoc.InnerXml;
        }


        /// <summary>
        /// Gets the CSS class selected by the content author
        /// </summary>
        /// <returns></returns>
        public string GetValue()
        {
            if (this.classLoaded) return this.selectedCssClass;

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
            this.selectedCssClass = GetValue(ph);
            this.classLoaded = true;
            return this.selectedCssClass;
        }

        /// <summary>
        /// Gets the CSS class selected by the content author
        /// </summary>
        /// <returns></returns>
        public static string GetValue(Placeholder placeholder)
        {
            var xml = placeholder as XmlPlaceholder;
            if (xml != null && !String.IsNullOrEmpty(xml.XmlAsString))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml.XmlAsString);
                return xmlDoc.DocumentElement.InnerText;
            }
            else return String.Empty;
        }

        /// <summary>
        /// Gets whether anything has been saved in this placeholder
        /// </summary>
        public bool HasContent
        {
            get
            {
                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;

                if (ph.XmlAsString.Length > 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(ph.XmlAsString);
                    return (xmlDoc.DocumentElement.InnerText.Length > 0);
                }
                else return false;
            }
        }

        /// <summary>
        /// Override to prevent span tag
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(System.Web.UI.HtmlTextWriter writer)
        {
        }

        /// <summary>
        /// Override to prevent span tag
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(System.Web.UI.HtmlTextWriter writer)
        {
        }
    }
}
