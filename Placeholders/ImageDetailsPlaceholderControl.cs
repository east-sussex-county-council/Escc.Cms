using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.XPath;
using Microsoft.ApplicationBlocks.ExceptionManagement;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Hidden CMS placeholder for saving additional information about images in ImagePlaceholders
    /// </summary>
    public class ImageDetailsPlaceholderControl : BasePlaceholderControl
    {
        #region Constructors

        /// <summary>
        /// CMS placeholder for editing a single line of unformatted text
        /// </summary>
        public ImageDetailsPlaceholderControl()
        {
            this.RenderContainerElement = false;
            this.ElementName = HtmlTextWriterTag.Span;
        }

        #endregion

        #region Empty methods which had to be overridden

        /// <summary>
        /// No controls to create - this is a hidden placeholder
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
        }

        /// <summary>
        /// No controls to create - this is a hidden placeholder
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
        }

        /// <summary>
        /// No controls to populate - this is a hidden placeholder
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
        }

        /// <summary>
        /// No controls to populate - this is a hidden placeholder
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
        }
        #endregion // Empty methods which had to be overridden

        #region Save and retrieve image sizes

        /// <summary>
        /// Create a well-formed XML document to save the image size information
        /// </summary>
        /// <param name="e"></param>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            try
            {
                // Create an XML document with a root <Images /> element
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");
                xmlDoc.AppendChild(xmlDec);

                XmlElement rootElement = xmlDoc.CreateElement("Images");
                xmlDoc.AppendChild(rootElement);

                // Loop through all the placeholder definitions in the template definition
                PlaceholderCollection placeholders = CmsHttpContext.Current.Posting.Placeholders;
                foreach (Placeholder otherPlaceholder in placeholders)
                {
                    // Check whether it's an image placeholder and is not empty
                    ImagePlaceholder imagePlaceholder = otherPlaceholder as ImagePlaceholder;
                    if (imagePlaceholder != null && !String.IsNullOrEmpty(Context.Request.Form["NCPH_" + imagePlaceholder.Name]))
                    {
                        // Create an <Image /> element for each image
                        XmlElement imageElement = xmlDoc.CreateElement("Image");
                        rootElement.AppendChild(imageElement);

                        XmlAttribute placeholderName = xmlDoc.CreateAttribute("Placeholder");
                        placeholderName.Value = imagePlaceholder.Name;
                        imageElement.Attributes.Append(placeholderName);

                        // Get image URL from Request.Form rather that object model, because if the image has just been changed,
                        // this way you get the latest image URL rather than the one there previously
                        XmlAttribute imageUrl = xmlDoc.CreateAttribute("Url");
                        imageUrl.Value = Context.Request.Form["NCPH_" + imagePlaceholder.Name];
                        imageElement.Attributes.Append(imageUrl);

                        XmlAttribute rolloverUrl = xmlDoc.CreateAttribute("RolloverUrl");
                        rolloverUrl.Value = String.Empty;
                        imageElement.Attributes.Append(rolloverUrl);


                        // Find the image resource and get its width and height
                        CmsHttpContext current = CmsHttpContext.Current;
                        Resource resource = CmsUtilities.ParseResourceUrl(imageUrl.Value, current);
                        if (resource != null)
                        {
                            using (Stream stream = resource.OpenReadStream())
                            {
                                using (System.Drawing.Image image = System.Drawing.Image.FromStream(stream))
                                {
                                    XmlAttribute width = xmlDoc.CreateAttribute("Width");
                                    width.Value = image.Width.ToString(CultureInfo.CurrentCulture);
                                    imageElement.Attributes.Append(width);

                                    XmlAttribute height = xmlDoc.CreateAttribute("Height");
                                    height.Value = image.Height.ToString(CultureInfo.CurrentCulture);
                                    imageElement.Attributes.Append(height);
                                }
                            }

                            // Check in the same resource gallery for a rollover, identified by its filename, 
                            // which should be the same but with "rollover" appended before the extension.
                            string rolloverFilename = Path.GetFileNameWithoutExtension(imageUrl.Value).ToLowerInvariant() + "rollover" + Path.GetExtension(imageUrl.Value).ToLowerInvariant();
                            foreach (Resource res in resource.Parent.Resources)
                            {
                                if (res.Name.ToLowerInvariant() == rolloverFilename)
                                {
                                    rolloverUrl.Value = res.Url;
                                    break;
                                }
                            }
                        }

                    }
                }

                // Save XML data on all images in the placeholder. Note that, because this happens when a page is saved,
                // image sizes could turn out to be wrong if an image is replaced in the resource gallery with one of a different size.
                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
                ph.XmlAsString = xmlDoc.InnerXml;
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }

        /// <summary>
        /// Get details of the images on the page
        /// </summary>
        /// <param name="ph">The placeholder containing the iamge data</param>
        /// <returns></returns>
        public static Dictionary<string, ImageInfo> GetValue(XmlPlaceholder ph)
        {
            Dictionary<string, ImageInfo> imageDetails = new Dictionary<string, ImageInfo>();
            try
            {
                if (ph.XmlAsString.Length > 0)
                {
                    // Read each stored image tag and add an object to the collection for each one,
                    // with the placeholder name as the indexer for the collection
                    StringReader reader = new StringReader(ph.XmlAsString);
                    XPathDocument xmlDoc = new XPathDocument(reader);
                    XPathNavigator nav = xmlDoc.CreateNavigator();
                    nav.MoveToRoot();
                    XPathNodeIterator it = nav.Select("/Images/Image");
                    while (it.MoveNext())
                    {
                        try
                        {
                            imageDetails.Add(it.Current.GetAttribute("Placeholder", String.Empty),
                                new ImageInfo(
                                    Int32.Parse(it.Current.GetAttribute("Width", String.Empty)),
                                    Int32.Parse(it.Current.GetAttribute("Height", String.Empty)),
                                    new Uri(it.Current.GetAttribute("Url", String.Empty), UriKind.RelativeOrAbsolute),
                                    it.Current.GetAttribute("RolloverUrl", String.Empty).Length > 0 ? new Uri(it.Current.GetAttribute("RolloverUrl", String.Empty), UriKind.RelativeOrAbsolute) : null
                                    ));
                        }
                        catch (FormatException ex)
                        {
                            NameValueCollection additionalInfo = new NameValueCollection();
                            additionalInfo.Add("Element containing error", it.Current.OuterXml);
                            ExceptionManager.Publish(ex, additionalInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading placeholder contents: " + typeof(ImageDetailsPlaceholderControl).Name, ex);
            }
            return imageDetails;
        }

        /// <summary>
        /// Gets a list of rollover image URLs on the page suitable for inclusion in page metadata.
        /// </summary>
        /// <param name="imageDetails">The image details obtained from <seealso cref="GetValue"/>.</param>
        /// <returns>A string containing GUIDs and URLs designed to work with cms-rollovers.js in the Website Resources project</returns>
        public static string GetRolloverMetadata(Dictionary<string, ImageInfo> imageDetails)
        {
            StringBuilder imageRollovers = new StringBuilder();
            foreach (string key in imageDetails.Keys)
            {
                if (imageDetails[key].RolloverUrl != null && imageDetails[key].RolloverUrl.ToString().Length > 0)
                {
                    if (imageRollovers.Length > 0) imageRollovers.Append(";");
                    imageRollovers.Append(imageDetails[key].Guid.ToString("D", CultureInfo.InvariantCulture).ToUpperInvariant()).Append(";").Append(imageDetails[key].RolloverUrl.ToString());
                }
            }
            return imageRollovers.ToString();
        }


        /// <summary>
        /// Sets a "rollover" CSS class on an image if it had a rollover image in the Resource Gallery at the time the page was saved
        /// </summary>
        /// <param name="imageDetails">The image details obtained from <seealso cref="GetValue"/>.</param>
        /// <param name="imageControl">The image control.</param>
        /// <param name="imagePlaceholderName">Name of the image placeholder.</param>
        public static void DisplayRollover(Dictionary<string, ImageInfo> imageDetails, WebControl imageControl, string imagePlaceholderName)
        {
            if (!imageDetails.ContainsKey(imagePlaceholderName)) return;
            if (imageDetails[imagePlaceholderName].RolloverUrl == null) return;
            if (imageDetails[imagePlaceholderName].RolloverUrl.ToString().Length == 0) return;

            // There is a URL, so add the class
            imageControl.CssClass = ("rollover " + imageControl.CssClass).TrimEnd();
        }

        #endregion // Save and retrieve image sizes

        /// <summary>
        /// Gets whether anything has been saved in this placeholder
        /// </summary>
        public bool HasContent
        {
            get
            {
                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
                return (ph.XmlAsString.Length > 0);
            }
        }

        /// <summary>
        /// Gets or sets whether to render a containing XHTML element in presentation mode (the default CMS behaviour)
        /// </summary>
        public bool RenderContainerElement
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether the XHTML element to use when rendering a container element
        /// </summary>
        public HtmlTextWriterTag ElementName
        {
            get;
            set;
        }

        /// <summary>
        /// Render the beginning of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (WebAuthorContext.Current.Mode != WebAuthorContextMode.AuthoringNew && WebAuthorContext.Current.Mode != WebAuthorContextMode.AuthoringReedit) return;
            base.RenderBeginTag(writer);
        }

        /// <summary>
        /// Render the end of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (WebAuthorContext.Current.Mode != WebAuthorContextMode.AuthoringNew && WebAuthorContext.Current.Mode != WebAuthorContextMode.AuthoringReedit) return;
            base.RenderEndTag(writer);
        }

        #region ImageInfo sub-class

        /// <summary>
        /// Size of an image as determined by this placeholder control
        /// </summary>
        public class ImageInfo
        {
            private Uri imageUrl;
            private Guid guid;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImageInfo"/> class.
            /// </summary>
            /// <param name="width">The width.</param>
            /// <param name="height">The height.</param>
            /// <param name="imageUrl">The image URL.</param>
            /// <param name="rolloverUrl">The rollover URL.</param>
            public ImageInfo(int width, int height, Uri imageUrl, Uri rolloverUrl)
            {
                this.Width = width;
                this.Height = height;
                this.ImageUrl = imageUrl;
                this.RolloverUrl = rolloverUrl;
            }

            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            /// <value>The width.</value>
            public int Width { get; set; }

            /// <summary>
            /// Gets or sets the height.
            /// </summary>
            /// <value>The height.</value>
            public int Height { get; set; }

            /// <summary>
            /// Gets or sets the image URL.
            /// </summary>
            /// <value>The URL.</value>
            public Uri ImageUrl
            {
                get
                {
                    return this.imageUrl;
                }
                set
                {
                    this.imageUrl = value;

                    if (imageUrl != null)
                    {
                        string guid = CmsUtilities.GetGuidFromUrl(this.imageUrl.ToString());
                        if (!String.IsNullOrEmpty(guid)) this.guid = new Guid(guid);
                    }
                }
            }

            /// <summary>
            /// Gets the GUID.
            /// </summary>
            /// <value>The GUID.</value>
            public Guid Guid
            {
                get { return this.guid; }
            }

            /// <summary>
            /// Gets or sets the rollover image URL.
            /// </summary>
            /// <value>The rollover URL.</value>
            public Uri RolloverUrl { get; set; }
        }

        #endregion // ImageSize sub-class
    }
}
