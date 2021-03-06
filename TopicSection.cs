﻿using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using EsccWebTeam.Cms.Placeholders;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Common base class for usercontrols which determine the layout of a section on a Standard Topic Page
    /// </summary>
    public abstract class TopicSection : System.Web.UI.UserControl
    {

        /// <summary>
        /// Gets or sets the placeholder to bind for the section layout.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindSection { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 1st image.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindImage01 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 2nd image.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindImage02 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 3rd image.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindImage03 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 1st caption.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindCaption01 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 2nd caption.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindCaption02 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 3rd caption.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindCaption03 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the subtitle.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindSubtitle { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the content.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindContent { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 1st "alt as caption" checkbox.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindAltAsCaption01 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 2nd "alt as caption" checkbox.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindAltAsCaption02 { get; set; }

        /// <summary>
        /// Gets or sets the placeholder to bind for the 3rd "alt as caption" checkbox.
        /// </summary>
        /// <value>The placeholder to bind.</value>
        public string PlaceholderToBindAltAsCaption03 { get; set; }

        /// <summary>
        /// Gets or sets details about images on the page.
        /// </summary>
        /// <value>The image details.</value>
        public Dictionary<string, ImageDetailsPlaceholderControl.ImageInfo> ImageDetails { get; set; }

        /// <summary>
        /// Restricts the width of an image container to that of the image and its border.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="placeholderName">Name of the placeholder.</param>
        /// <param name="borderWidth">Extra widht, in pixels, to add to the container to allow for a border</param>
        public void RestrictImageContainer(HtmlControl container, string placeholderName, int borderWidth)
        {
            if (this.ImageDetails.ContainsKey(placeholderName))
            {
                container.Style.Add("width", (this.ImageDetails[placeholderName].Width + borderWidth).ToString(CultureInfo.CurrentCulture) + "px");
                container.Style.Add("max-width", "100%"); // this allows contained image to scale down with screen size
            }
        }

        /// <summary>
        /// Displays the caption.
        /// </summary>
        /// <param name="captionControl">The caption control.</param>
        /// <param name="imagePlaceholderName">Name of the image placeholder.</param>
        /// <param name="altAsCaptionPlaceholderName">Name of the alt as caption placeholder.</param>
        protected void DisplayCaption(Control captionControl, string imagePlaceholderName, string altAsCaptionPlaceholderName)
        {
            Posting p = CmsHttpContext.Current.Posting;
            if (p == null) return;

            ImagePlaceholder imagePlaceholder = p.Placeholders[imagePlaceholderName] as ImagePlaceholder;

            if (!CmsUtilities.PlaceholderIsEmpty(imagePlaceholder))
            {
                XmlPlaceholder altAsCaptionPlaceholder = p.Placeholders[altAsCaptionPlaceholderName] as XmlPlaceholder;

                bool altAsCaptionKnown = (altAsCaptionPlaceholder.Datasource.RawContent.Length > 0); // for pages which haven't been saved since this was introduced
                bool altAsCaption = CheckBoxPlaceholderControl.GetValue(altAsCaptionPlaceholder);
                if (altAsCaption || !altAsCaptionKnown)
                {
                    captionControl.Visible = false;
                    captionControl.Parent.Controls.Add(new LiteralControl(imagePlaceholder.Alt));
                }
                else
                {
                    captionControl.Visible = true;
                }
            }
        }
    }
}