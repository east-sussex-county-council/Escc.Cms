using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.XPath;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;
using Microsoft.ContentManagement.WebControls.Design;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// CMS placeholder control for selecting an option from a dropdown list
    /// </summary>
    [SupportedPlaceholderDefinitionType(typeof(XmlPlaceholderDefinition))]
    public class DropDownListPlaceholderControl : BasePlaceholderControl
    {
        private DropDownList list;
        private Label listLabel;
        private string listId = "phList";
        private string labelText = "Select one";
        private Literal display;
        private char itemDivider = ';';
        private char valueTextDivider = '|';
        private string itemConfigSection;

        /// <summary>
        /// Gets or sets a label for the list, which must be at least one character in length
        /// </summary>
        [Browsable(true)]
        public string Label
        {
            get { return this.labelText; }
            set { if (value != null && value.Trim().Length > 0) this.labelText = value.Trim(); }

        }

        /// <summary>
        /// Gets the collection of items to choose from
        /// </summary>
        /// <remarks>You can't use this collection to .Add() list items from the code behind of a page. The placeholder won't save its
        /// data as the .SelectedItem property isn't populated, apparently due to problems with the page lifecycle. You simply can't add 
        /// the items in code as early as .NET does when you declare them on the page.</remarks>
        [Browsable(false)]
        public ListItemCollection Items
        {
            get
            {
                if (this.list == null) this.list = new DropDownList();
                return this.list.Items;
            }
        }

        /// <summary>
        /// Gets or sets the collection of items to choose from in the following format: value1|text1;value2|text2;value3|text3
        /// </summary>
        [Browsable(true)]
        public string ItemText
        {
            get
            {
                if (this.list == null) return String.Empty;

                // Build up a string from each list item
                //
                // NOTE: may not be exactly the string set by the user, because they can set the value 
                // and have the text implied, but here it's output explicitly.
                //
                // eg setting value1;value2 comes back out as value1|value1;value2|value2
                StringBuilder sb = new StringBuilder();
                foreach (ListItem li in this.list.Items)
                {
                    if (sb.Length > 0) sb.Append(this.itemDivider);
                    sb.Append(li.Value);
                    sb.Append(this.valueTextDivider);
                    sb.Append(li.Text);
                }

                return sb.ToString();
            }
            set
            {
                if (value != null)
                {
                    string trimmed = value.Trim();
                    if (trimmed.Length > 0)
                    {
                        // Split the text into strings representing each list item
                        string[] items = trimmed.Split(this.itemDivider);
                        int len = items.Length;

                        // Create an array of list items
                        ListItem[] newItems = new ListItem[len];

                        for (int i = 0; i < len; i++)
                        {
                            // Split item into value and text
                            string[] valueText = items[i].Split(this.valueTextDivider);
                            newItems[i] = new ListItem();

                            // Set value and text of list item - or if there's only one value, use it for both
                            if (valueText.Length > 0) newItems[i].Value = valueText[0];
                            if (valueText.Length > 1) newItems[i].Text = valueText[1]; else newItems[i].Text = valueText[0];
                        }

                        // Reset the list to use the new items
                        if (this.list != null)
                        {
                            this.list.Items.Clear();
                        }
                        else
                        {
                            this.list = new DropDownList();
                        }
                        this.list.Items.AddRange(newItems);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of a config section to read the items from
        /// </summary>
        /// <remarks>
        /// <example>
        /// <para>You can use any name for your configuration section, but it must have the following definition.</para>
        ///   &lt;configSections&gt;
        ///       &lt;section name=&quot;ExampleSection&quot; type=&quot;System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089&quot; /&gt;
        ///   &lt;/configSections&gt;
        ///   &lt;ExampleSection&gt;
        ///     &lt;add key=&quot;item1value&quot; value=&quot;Item 1&quot; /&gt;
        ///     &lt;add key=&quot;item2value&quot; value=&quot;Item 2&quot; /&gt;
        ///   &lt;/ExampleSection&gt;
        /// </example>
        /// </remarks>
        public virtual string ItemConfigSection
        {
            get { return this.itemConfigSection; }
            set
            {
                this.itemConfigSection = value;

                NameValueCollection itemsInConfig = ConfigurationManager.GetSection(value) as NameValueCollection;
                if (itemsInConfig != null)
                {
                    if (this.list != null)
                    {
                        this.list.Items.Clear();
                    }
                    else
                    {
                        this.list = new DropDownList();
                    }

                    foreach (string key in itemsInConfig.AllKeys) this.list.Items.Add(new ListItem(itemsInConfig[key], key));
                }
            }
        }

        /// <summary>
        /// CMS placeholder control for selecting an option from a dropdown list
        /// </summary>
        public DropDownListPlaceholderControl()
        {

        }

        /// <summary>
        /// Create a list and its label to select from in edit mode
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            // Create dropdown list
            if (this.list == null) this.list = new DropDownList();
            this.list.ID = this.listId;

            // Create label for dropdown list
            this.listLabel = new Label();
            this.listLabel.Text = this.labelText + " ";
            this.listLabel.AssociatedControlID = this.listId;

            // Add label and list to control collection
            authoringContainer.Controls.Add(this.listLabel);
            authoringContainer.Controls.Add(this.list);

        }

        /// <summary>
        /// Get the selected value and try to re-select it
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            // Get current saved list item
            ListItem li = DropDownListPlaceholderControl.GetValue(this.BoundPlaceholder as XmlPlaceholder);

            if (li != null && this.AuthoringChildControlsAreAvailable)
            {
                // Look for a list item with the same value and text
                ListItem toSelect = this.list.Items.FindByValue(li.Value);
                if (toSelect != null && toSelect.Text == li.Text)
                {
                    // Match found, so select it
                    if (this.list.SelectedItem != null) this.list.SelectedItem.Selected = false;
                    toSelect.Selected = true;
                }
            }
        }

        /// <summary>
        /// Create a Literal to display the text of the selected item
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            this.display = new Literal();
            presentationContainer.Controls.Add(this.display);
        }


        /// <summary>
        /// Get the text of the selected item and display it
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            if (this.PresentationChildControlsAreAvailable)
            {
                ListItem li = DropDownListPlaceholderControl.GetValue(this.BoundPlaceholder as XmlPlaceholder);
                if (this.display != null && li != null)
                {
                    this.display.Text = li.Text;
                }
            }
        }

        /// <summary>
        /// Save the selected ListItem by serialising it as XML
        /// </summary>
        /// <param name="e"></param>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            ListItem li = this.list.SelectedItem;
            if (li == null) li = new ListItem(); // if none selected, save a blank

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes"));

            XmlElement rootElement = xmlDoc.CreateElement("Selected");
            xmlDoc.AppendChild(rootElement);

            XmlElement valueElement = xmlDoc.CreateElement("Value");
            rootElement.AppendChild(valueElement);
            valueElement.AppendChild(xmlDoc.CreateTextNode(li.Value));

            XmlElement textElement = xmlDoc.CreateElement("Text");
            rootElement.AppendChild(textElement);
            textElement.AppendChild(xmlDoc.CreateTextNode(li.Text));

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
            ph.XmlAsString = xmlDoc.OuterXml;
        }

        /// <summary>
        /// Gets the ListItem selected using the placeholder
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static ListItem GetValue(XmlPlaceholder x)
        {
            if (x == null || x.XmlAsString.Length == 0) return null;

            ListItem li = new ListItem();

            XPathDocument xp = new XPathDocument(new StringReader(x.XmlAsString));
            XPathNavigator nav = xp.CreateNavigator();
            nav.MoveToRoot(); // <?xml ... ?>
            nav.MoveToFirstChild();  // <Selected>
            nav.MoveToFirstChild(); // <Value>
            li.Value = nav.Value;
            nav.MoveToNext(); // <Text>
            li.Text = nav.Value;

            return li;
        }

        /// <summary>
        /// Gets whether a value has been saved using the dropdown list
        /// </summary>
        public bool HasContent
        {
            get
            {
                ListItem li = DropDownListPlaceholderControl.GetValue(this.BoundPlaceholder as XmlPlaceholder);
                return (li.Value.Length > 0);
            }
        }
    }
}
