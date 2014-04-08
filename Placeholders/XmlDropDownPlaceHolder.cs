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
    /// Displays options from an XML file as a dropdown list in edit mode
    /// </summary>
    /// <remarks>
    /// This placeholder works with a simple xml file which must observe fixed node conventions...
    /// parent node can be called anything you want but each child entry must be an *item* tag and have one child node with 
    /// a *name* tag.
    /// <example>
    /// &lt;parentnode&gt;
    /// &lt;item&gt;
    /// &lt;name&gt;&lt;/name&gt;
    /// &lt;/item&gt;
    /// &lt;item&gt;
    /// &lt;name&gt;&lt;/name&gt;
    /// &lt;/item&gt;
    /// &lt;/parentnode&gt;
    /// </example>
    /// The path to the xml file can be modified as a design view property.
    /// </remarks>
    [ToolboxData("<{0}:XmlDropDownPlaceHolder runat=server></{0}:XmlDropDownPlaceHolder>")]
    [SupportedPlaceholderDefinitionType(typeof(XmlPlaceholderDefinition))]
    public class XmlDropDownPlaceHolder : BasePlaceholderControl
    {
        #region Constructors
        /// <summary>
        /// The class constructor. 
        /// </summary>
        public XmlDropDownPlaceHolder()
        {
        }
        #endregion

        #region Private fields
        /// <summary>
        /// Store for the XmlFile property.The xml file used to generate the Drop Down List items.
        /// </summary>
        string xmlFile;
        /// <summary>
        /// Authoring control class member. Allows authors to select a single value from a Drop down list control.
        /// </summary>
        DropDownList ddlDropDownList;
        /// <summary>
        /// Presentation control class member. Presents the Drop down list choice as a &lt;ul&gt; list item in published mode.
        /// </summary>
        Literal PresentationString;
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
        Description("Xml File for the Drop Down List Placeholder control."),
        Category("Data")]
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
        /// Populates the drop down list control items from the specified xml file .
        /// The item list is cached as an XmlNodeList with a dependency on the xml file.
        /// </summary>
        private void BuildItemsList()
        {
            // method variables
            XmlNodeList items;
            HttpContext context = HttpContext.Current;
            // strip the file name, less the extension, from the path and use as the cache key.
            string cacheKey = XmlFile.Remove(0, XmlFile.LastIndexOf("\\") + 1);
            cacheKey = cacheKey.Remove(cacheKey.LastIndexOf("."), cacheKey.Length - cacheKey.LastIndexOf("."));
            // look for a cached XmlNodeList and add to cache if not in existence
            if (context.Cache[cacheKey] == null)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFile);
                items = xmlDoc.GetElementsByTagName("name");
                context.Cache.Insert(cacheKey, items, new CacheDependency(XmlFile));
            }
            // use cached version if available
            else
            {
                items = context.Cache[cacheKey] as XmlNodeList;
            }
            // Insert a blank list item at index position 0 then
            // append the rest of the list items
            ddlDropDownList.Items.Insert(0, new ListItem("", ""));
            for (int i = 0; i < items.Count; i++)
            {
                ddlDropDownList.Items.Add(new ListItem(items[i].InnerText, items[i].InnerText));
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
            // create our drop down list and call BuildItemsList() to populate it.
            ddlDropDownList = new DropDownList();
            ddlDropDownList.ID = "ddlDropDownList";
            BuildItemsList();

            authoringContainer.Controls.Add(ddlDropDownList);
        }
        /// <summary>
        /// Overrides the base class method CreatePresentationChildControls(BaseModeContainer presentationContainer.
        /// Presents user with the author's drop dowm list selection as a &lt;ul&gt; list item.
        /// </summary>
        /// <param name="presentationContainer">BaseModeContainer. The base class presentation container for the control.</param>
        /// <seealso cref="BaseModeContainer">
        /// BaseModeContainer reference.
        /// </seealso>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            PresentationString = new Literal();
            presentationContainer.Controls.Add(PresentationString);
        }
        /// <summary>
        /// Overrides the base class method LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e).
        /// Loads the CMS stored xml content at author time. Sets the selected value of the Drop down list control
        /// to the previously chosen value.
        /// </summary>
        /// <param name="e">PlaceholderControlEventArgs.</param>
        /// <seealso cref="PlaceholderControlEventArgs">
        /// PlaceholderControlEventArgs reference.
        /// </seealso>
        /// <seealso cref="ddlDropDownList">
        /// DropDownList ddlDropDownList reference.
        /// </seealso>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            EnsureChildControls();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                string xml;
                xml = ((XmlPlaceholder)this.BoundPlaceholder).XmlAsString;
                if (xml.Length > 0)
                {
                    xmlDoc.LoadXml(xml);
                    foreach (XmlNode itemNode in xmlDoc.DocumentElement.ChildNodes)
                    {
                        ListItem item = ddlDropDownList.Items.FindByValue(itemNode.InnerText);
                        item.Selected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Exception overriddenException = new Exception(ex.Message, ex);
                throw overriddenException;
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
        /// <seealso cref="PresentationString">
        /// Literal PresentationString reference.
        /// </seealso>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            EnsureChildControls();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                string xml;
                xml = ((XmlPlaceholder)this.BoundPlaceholder).XmlAsString;
                if (xml.Length > 0)
                {
                    PresentationString.Text = "<ul>";
                    xmlDoc.LoadXml(xml);
                    foreach (XmlNode itemNode in xmlDoc.DocumentElement.ChildNodes)
                    {
                        PresentationString.Text += "<li>" + itemNode.InnerText + "</li>";
                    }
                    PresentationString.Text += "</ul>";
                }
            }
            catch (Exception ex)
            {
                Exception overriddenException = new Exception(ex.Message, ex);
                throw overriddenException;
            }

        }
        /// <summary>
        /// Overrides the base class method SavePlaceholderContent(PlaceholderControlSaveEventArgs e).
        /// Saves the Dropdown selection to the CMS database as xml.
        /// </summary>
        /// <param name="e">PlaceholderControlSaveEventArgs.</param>
        /// <seealso cref="PlaceholderControlSaveEventArgs">
        /// PlaceholderControlSaveEventArgs reference.
        /// </seealso>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            EnsureChildControls();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlNode DocParentNode = xmlDoc.CreateElement("item");
                xmlDoc.AppendChild(DocParentNode);

                foreach (ListItem item in ddlDropDownList.Items)
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
            catch (Exception ex)
            {
                Exception overriddenException = new Exception(ex.Message, ex);
                throw overriddenException;
            }
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