using System;
using System.ComponentModel;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;
using Microsoft.ContentManagement.WebControls.Design;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// This placeholder works with a simple xml file which must observe fixed node conventions...
    /// parent node can be called anything you want but each child entry must be an *item* tag and have one child node with 
    /// a *name* tag. e.g.
    /// <parentnode>
    /// <item>
    /// <name></name>
    /// </item>
    /// <item>
    /// <name></name>
    /// </item>
    /// </parentnode>
    /// The path to the xml file can be modified as a design view property.
    /// </summary>
    [ToolboxData("<{0}:CheckBoxListPlaceholder runat=server></{0}:CheckBoxListPlaceholder>")]
    [SupportedPlaceholderDefinitionType(typeof(XmlPlaceholderDefinition))]
    public class CheckBoxListPlaceholder : BasePlaceholderControl
    {
        #region Constructors
        /// <summary>
        /// The class constructor. 
        /// </summary>
        public CheckBoxListPlaceholder()
        {
        }
        #endregion

        #region Private fields
        /// <summary>
        /// Store for the XmlFile property.The xml file used to generate the Drop Down List items.
        /// </summary>
        string xmlFile;
        /// <summary>
        /// Authoring control class member. Allows authors to select values from a Check Box list control.
        /// </summary>
        CheckBoxList cblCBL;
        /// <summary>
        /// Presentation control class member. Presents the Drop down list choice as &lt;ul&gt; list items in published mode.
        /// </summary>
        Literal PresentationList;
        #endregion

        #region Public Properties
        /// <summary>
        /// The XmlFile property.
        /// </summary>
        /// /// <value>
        /// string representing the path to the xml file.
        /// </value>
        /// <seealso cref="XmlDropDownPlaceHolder()">
        /// Description of required xml file format.
        /// </seealso>
        [Browsable(true),
        Description("Xml File for the Check Box Placeholder control."),
        Category("Appearance")]
        public string XmlFile
        {
            get { return xmlFile; }
            set { xmlFile = value; }
        }
        //Allow user to change the value of property - in design view
        bool ShouldSerializeXmlFile() { return true; }
        #endregion

        #region Private Functions
        /// <summary>
        /// Description for BuildItemsList().
        /// Populates the check box list control items from the specified xml file .
        /// The item list is cached as an XmlNodeList with a dependency on the xml file.
        /// </summary>
        private void BuildItemsList()
        {
            XmlNodeList items;
            HttpContext context = HttpContext.Current;
            string cacheKey = XmlFile.Remove(0, XmlFile.LastIndexOf("\\") + 1);
            cacheKey = cacheKey.Remove(cacheKey.LastIndexOf("."), cacheKey.Length - cacheKey.LastIndexOf("."));
            if (context.Cache[cacheKey] == null)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFile);
                items = xmlDoc.GetElementsByTagName("name");
                context.Cache.Insert(cacheKey, items, new CacheDependency(XmlFile));
            }
            else
            {
                items = context.Cache[cacheKey] as XmlNodeList;
            }
            for (int i = 0; i < items.Count; i++)
            {
                cblCBL.Items.Add(new ListItem(items[i].InnerText, items[i].InnerText));
            }
        }
        #endregion

        #region BasePlaceholder Method Signatures
        /// <summary>
        /// Overrides the base class method CreateAuthoringChildControls(BaseModeContainer authoringContainer).
        /// Allows user to select from a predetermined list of items at author time.
        /// </summary>
        /// <param name="authoringContainer">BaseModeContainer. The base class authoring container for the control.</param>
        /// <seealso cref="BaseModeContainer">
        /// BaseModeContainer reference.
        /// </seealso>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            cblCBL = new CheckBoxList();
            cblCBL.ID = "cblCBL";
            cblCBL.CssClass = "radioButtonList";
            cblCBL.RepeatDirection = RepeatDirection.Horizontal;
            cblCBL.RepeatLayout = RepeatLayout.Flow;
            BuildItemsList();

            authoringContainer.Controls.Add(cblCBL);
        }
        /// <summary>
        /// Overrides the base class method CreatePresentationChildControls(BaseModeContainer presentationContainer.
        /// Presents user with the author's check box list selections as &lt;ul&gt; list items.
        /// </summary>
        /// <param name="presentationContainer">BaseModeContainer. The base class presentation container for the control.</param>
        /// <seealso cref="BaseModeContainer">
        /// BaseModeContainer reference.
        /// </seealso>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            PresentationList = new Literal();
            presentationContainer.Controls.Add(PresentationList);
        }
        /// <summary>
        /// Overrides the base class method LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e).
        /// Loads the CMS stored xml content at author time. Sets the selected values of the check box list control
        /// to the previously chosen values.
        /// </summary>
        /// <param name="e">PlaceholderControlEventArgs.</param>
        /// <seealso cref="PlaceholderControlEventArgs">
        /// PlaceholderControlEventArgs reference.
        /// </seealso>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            EnsureChildControls();
            XmlDocument xmlDoc = new XmlDocument();
            string xml;
            xml = ((XmlPlaceholder)this.BoundPlaceholder).XmlAsString;
            if (xml.Length > 0)
            {
                xmlDoc.LoadXml(xml);
                foreach (XmlNode itemNode in xmlDoc.DocumentElement.ChildNodes)
                {
                    ListItem item = cblCBL.Items.FindByValue(itemNode.InnerText);
                    item.Selected = true;
                }
            }

        }
        /// <summary>
        /// Overrides the base class method LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e).
        /// Loads the CMS stored xml content for presentation. Populates the private class member used for presentation.
        /// </summary>
        /// <param name="e">PlaceholderControlEventArgs.</param>
        /// <seealso cref="PlaceholderControlEventArgs">
        /// PlaceholderControlEventArgs reference.
        /// </seealso>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            EnsureChildControls();

            XmlDocument xmlDoc = new XmlDocument();
            string xml;
            xml = ((XmlPlaceholder)this.BoundPlaceholder).XmlAsString;
            if (xml.Length > 0)
            {
                PresentationList.Text = "<ul>";
                xmlDoc.LoadXml(xml);
                PresentationList.Visible = false;
                foreach (XmlNode itemNode in xmlDoc.DocumentElement.ChildNodes)
                {
                    PresentationList.Text += "<li>" + itemNode.InnerText + "</li>";
                    PresentationList.Visible = true;
                }
                PresentationList.Text += "</ul>";
            }

        }
        /// <summary>
        /// Overrides the base class method SavePlaceholderContent(PlaceholderControlSaveEventArgs e).
        /// Saves the Check box list selections to the CMS database as xml.
        /// </summary>
        /// <param name="e">PlaceholderControlSaveEventArgs.</param>
        /// <seealso cref="PlaceholderControlSaveEventArgs">
        /// PlaceholderControlSaveEventArgs reference.
        /// </seealso>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            EnsureChildControls();

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode DocParentNode = xmlDoc.CreateElement("item");
            xmlDoc.AppendChild(DocParentNode);

            foreach (ListItem item in cblCBL.Items)
            {
                if (item.Selected == true)
                {
                    XmlNode itemNode = xmlDoc.CreateElement("item");
                    itemNode.InnerText = item.Text;
                    DocParentNode.AppendChild(itemNode);
                }
            }

            ((XmlPlaceholder)this.BoundPlaceholder).XmlAsString = xmlDoc.InnerXml.ToString();

        }

        /// <summary>
        /// Renders the HTML opening tag of the control to the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter"/> that represents the output stream to render HTML content on the client.</param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            bool isEditing = (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew || WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit);
            if (isEditing) base.RenderBeginTag(writer);
        }

        /// <summary>
        /// Renders the HTML closing tag of the control into the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter"/> that represents the output stream to render HTML content on the client.</param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            bool isEditing = (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew || WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit);
            if (isEditing) base.RenderEndTag(writer);
        }
        #endregion
    }
}
