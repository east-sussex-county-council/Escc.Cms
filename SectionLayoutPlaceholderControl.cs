using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using EsccWebTeam.Cms.Placeholders;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Select a usercontrol to use as the layout for a section of a CMS template
    /// </summary>
    public class SectionLayoutPlaceholderControl : DropDownListPlaceholderControl
    {
        private string itemConfigSection;

        /// <summary>
        /// Gets or sets the name of a config section to read the items from
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// 	<example>
        /// 		<para>You can use any name for your configuration section, but it must have the following definition.</para>
        /// &lt;configSections&gt;
        /// &lt;section name="ExampleSection" type="EsccWebTeam.Cms.SectionLayoutConfigurationSection, EsccWebTeam.Cms, Version=1.0.0.0, Culture=neutral, PublicKeyToken=06fad7304560ae6f" /&gt;
        /// &lt;/configSections&gt;
        /// &lt;ExampleSection&gt;
        ///     &lt;SectionLayouts&gt;
        ///         &lt;add name=&quot;key&quot; displayName=&quot;Display name&quot; displayControl=&quot;~/path/to/usercontrol.ascx&quot; editControl=&quot;~/path/to/usercontrol.ascx&quot; /&gt;
        ///     &lt;/SectionLayouts&gt;
        /// &lt;/ExampleSection&gt;
        /// </example>
        /// </remarks>
        public override string ItemConfigSection
        {
            get { return base.ItemConfigSection; }
            set
            {
                this.itemConfigSection = value;

                var itemsInConfig = ConfigurationManager.GetSection(value) as SectionLayoutConfigurationSection;
                if (itemsInConfig != null)
                {
                    this.Items.Clear();
                    foreach (SectionLayoutConfigurationElement element in itemsInConfig.SectionLayouts)
                    {
                        this.Items.Add(new ListItem(element.DisplayName, element.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Get the selected value and try to re-select it
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(Microsoft.ContentManagement.WebControls.PlaceholderControlEventArgs e)
        {
            // Let the correct item be selected
            base.LoadPlaceholderContentForAuthoring(e);

            // Remove file paths from the displayed list item value
            foreach (ListItem item in this.Items)
            {
                if (item.Text.IndexOf(";") > -1)
                {
                    item.Text = item.Text.Substring(0, item.Text.IndexOf(";"));
                }
            }
        }

        /// <summary>
        /// Create a list and its label to select from in edit mode
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(Microsoft.ContentManagement.WebControls.BaseModeContainer authoringContainer)
        {
            authoringContainer.Controls.Add(new LiteralControl("<div class=\"editHelp sectionLayout\">"));
            base.CreateAuthoringChildControls(authoringContainer);
            authoringContainer.Controls.Add(new LiteralControl("</div>"));
        }

        /// <summary>
        /// Ensure nothing is displayed for this placeholder in published mode
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(Microsoft.ContentManagement.WebControls.BaseModeContainer presentationContainer)
        {
            // there are none
        }

        /// <summary>
        /// Renders the HTML opening tag of the control to the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter"/> that represents the output stream to render HTML content on the client.</param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            // no span tag
        }

        /// <summary>
        /// Renders the HTML closing tag of the control into the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter"/> that represents the output stream to render HTML content on the client.</param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            // no span tag
        }
    }
}
