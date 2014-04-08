using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using EsccWebTeam.Cms.Placeholders;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Helper class for working with configurable sections in CMS templates
    /// </summary>
    public static class SectionLayoutManager
    {
        /// <summary>
        /// Gets a value indicating whether the current request is in CMS edit mode.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in edit mode; otherwise, <c>false</c>.
        /// </value>
        private static bool IsEditing { get { return (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew || WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit); } }

        /// <summary>
        /// Parses the placeholder number from the placeholder name, where there is a series of similarly-named placeholders.
        /// </summary>
        /// <param name="placeholderName">Name of the placeholder.</param>
        /// <returns></returns>
        public static string ParsePlaceholderNumber(string placeholderName)
        {
            Match m = Regex.Match(placeholderName, "[0-9]+$");
            if (m.Captures.Count > 0)
            {
                return m.Captures[0].Value.TrimStart('0');
            }
            return String.Empty;
        }

        /// <summary>
        /// Enforces a default layout used for a particular section in edit mode.
        /// </summary>
        /// <param name="placeholders">The placeholders.</param>
        /// <param name="sectionPlaceholder">The section placeholder.</param>
        /// <param name="layoutsConfigSectionName">Name of the layouts config section.</param>
        /// <param name="defaultLayout">The default layout.</param>
        public static void SetDefaultSectionLayout(PlaceholderCollection placeholders, string sectionPlaceholder, string layoutsConfigSectionName, string defaultLayout)
        {
            if (SectionLayoutManager.IsEditing)
            {
                var sectionLayouts = ConfigurationManager.GetSection(layoutsConfigSectionName) as SectionLayoutConfigurationSection;
                if (sectionLayouts == null) throw new ConfigurationErrorsException("Could not find &lt;" + layoutsConfigSectionName + " /&gt; configuration section.");
                if (sectionLayouts.SectionLayouts[defaultLayout] == null) throw new ArgumentException("Could not find " + defaultLayout + " in &lt;" + layoutsConfigSectionName + " /&gt; configuration section.");

                // Preserve backwards compatibility for pages which have not been saved since "sections" were introduced.
                // They were expecting a particular hard-coded layout for each section, and that must be preserved until it's
                // deliberately changed by an editor. Do it OnInit so that it's early enough to bind to the placeholder in edit mode.
                if (placeholders[sectionPlaceholder].Datasource.RawContent.Length == 0)
                {
                    placeholders[sectionPlaceholder].Datasource.RawContent = String.Format("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><Selected><Value>{0}</Value><Text>{1}</Text></Selected>", defaultLayout, sectionLayouts.SectionLayouts[defaultLayout].DisplayName);
                }
            }
        }

        /// <summary>
        /// Gets the selected section layout.
        /// </summary>
        /// <param name="placeholders">The placeholders.</param>
        /// <param name="sectionPlaceholder">The section placeholder.</param>
        /// <param name="defaultLayout">The default layout.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if section placeholder does not contain a value</exception>
        public static string GetSelectedSectionLayout(PlaceholderCollection placeholders, string sectionPlaceholder, string defaultLayout)
        {
            // Make sure we know which usercontrol to load for the section
            string selectedLayout = String.Empty;

            // Hopefully the layout name was saved with the page in a placeholder
            ListItem selectedSectionLayout = DropDownListPlaceholderControl.GetValue(placeholders[sectionPlaceholder] as XmlPlaceholder) as ListItem;
            if (selectedSectionLayout != null) selectedLayout = selectedSectionLayout.Value;

            // If not, the page probably hasn't been saved since this code was introduced, so default to 
            // whichever layout was previously hard-coded into the template.
            // This is for published/unpublished mode. The default placeholder for edit mode is set earlier in SetDefaultSectionLayout, but it's done
            // in a way that only works when editing.
            if (selectedLayout.Length == 0)
            {
                selectedLayout = defaultLayout;
            }

            return selectedLayout;
        }

        /// <summary>
        /// Gets the path to the usercontrol to load for the selected layout
        /// </summary>
        /// <param name="layoutsConfigSectionName">Name of the layouts config section.</param>
        /// <param name="selectedLayout">The selected layout.</param>
        /// <returns></returns>
        public static string UserControlPath(string layoutsConfigSectionName, string selectedLayout)
        {
            // Check we have the data in web.config we need
            var sectionLayouts = ConfigurationManager.GetSection(layoutsConfigSectionName) as SectionLayoutConfigurationSection;
            if (sectionLayouts == null) throw new ConfigurationErrorsException("Could not find <" + layoutsConfigSectionName + "> configuration section.");
            if (sectionLayouts.SectionLayouts[selectedLayout] == null) throw new ArgumentException("Could not find " + selectedLayout + " in <" + layoutsConfigSectionName + " /> configuration section.");

            if (SectionLayoutManager.IsEditing)
            {
                return sectionLayouts.SectionLayouts[selectedLayout].EditControl;
            }
            else
            {
                return sectionLayouts.SectionLayouts[selectedLayout].DisplayControl;
            }
        }

        /// <summary>
        /// Adds a suffix to the id of a placeholder control based on the suffix of its bound placeholder
        /// </summary>
        /// <param name="placeholder"></param>
        /// <param name="placeholderToBind"></param>
        public static void AddSuffixToPlaceholderId(Control placeholder, string placeholderToBind)
        {
            if (placeholder == null) throw new ArgumentNullException("placeholder");

            var previousId = placeholder.ID.ToUpperInvariant();
            placeholder.ID += ParsePlaceholderNumber(placeholderToBind);

            // Update any validators attached to the placeholder as they will not be able to find it using its old ID
            var validators = CmsUtilities.FindControlsOfType<BaseValidator>(placeholder.NamingContainer);
            foreach (BaseValidator validator in validators)
            {
                if (validator.ControlToValidate.ToUpperInvariant() == previousId)
                {
                    validator.ControlToValidate = placeholder.ID;
                }
            }

        }

    }
}
