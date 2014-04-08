using System;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using EsccWebTeam.Cms.Properties;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Attachment placeholder which checks the type of the attachment
    /// </summary>
    [ValidationProperty("AttachmentUrl")]
    public class XhtmlAttachmentPlaceholderControl : SingleAttachmentPlaceholderControl
    {
        private string[] allowedExtensions;
        private string text;
        private bool renderContainerElement;
        private HtmlTextWriterTag elementName;
        private bool renderFormat;

        /// <summary>
        /// Gets or sets whether to display the format of the document
        /// </summary>
        public bool RenderFormat
        {
            get
            {
                return this.renderFormat;
            }
            set
            {
                this.renderFormat = value;
            }
        }

        /// <summary>
        /// Gets whether anything has been saved in this placeholder
        /// </summary>
        public bool HasContent
        {
            get
            {
                AttachmentPlaceholder ph = this.BoundPlaceholder as AttachmentPlaceholder;
                return (ph != null && ph.Url.Length > 0);
            }
        }

        /// <summary>
        /// Gets or sets the field label in authoring mode
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
        /// Gets or sets a comma-separated list of file extensions which this placeholder will accept
        /// </summary>
        public string AllowedExtensions
        {
            get
            {
                return String.Join(",", this.allowedExtensions);
            }
            set
            {
                this.allowedExtensions = value.Split(',');
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
                    value == HtmlTextWriterTag.Td
                    )
                {
                    this.elementName = value;
                }
                else
                {
                    throw new ApplicationException("XhtmlAttachmentPlaceholderControl can only use the following elements: div, span, p, td, dt, li");
                }
            }
        }

        /// <summary>
        /// Attachment placeholder which checks the type of the attachment
        /// </summary>
        public XhtmlAttachmentPlaceholderControl()
        {
            this.text = "";
            this.allowedExtensions = new string[0];
            this.renderContainerElement = false;
            this.elementName = HtmlTextWriterTag.Div;
            this.renderFormat = true;
        }

        /// <summary>
        /// Present as a standard AttachmentPlaceholderControl, preceded by a label
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            // create a label for the field
            if (this.text.Length > 0)
            {
                HtmlGenericControl label = new HtmlGenericControl("label");
                label.Attributes["for"] = this.ID + "_AuthoringModeControlsContainer_AttachmentEditControl";
                label.Attributes["class"] = "AttachmentPlaceholder";
                label.InnerText = this.text;
                authoringContainer.Controls.Add(label);
            }

            // draw the basic attachment placeholder control
            base.CreateAuthoringChildControls(authoringContainer);
        }


        /// <summary>
        /// Present as a link, followed by a span with format and size info
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            AttachmentPlaceholder ph = this.BoundPlaceholder as AttachmentPlaceholder;
            if (ph != null)
            {
                if (ph.Url.Length > 0)
                {
                    // get ref to resource
                    string guid = CmsUtilities.GetGuidFromUrl(ph.Url);
                    Resource res = CmsHttpContext.Current.Searches.GetByGuid(guid) as Resource;

                    HtmlAnchor link = new HtmlAnchor();
                    link.HRef = ph.Url;
                    link.InnerText = ph.AttachmentText;
                    presentationContainer.Controls.Add(link);

                    // display the file size in brackets
                    string size = "";
                    if (res != null) size = CmsUtilities.GetResourceFileSize(res);
                    if (size.Length > 0)
                    {
                        StringBuilder downloadDetail = new StringBuilder(" <span ");
                        if (this.renderFormat)
                        {
                            string extension = System.IO.Path.GetExtension(ph.Url).Substring(1).ToLowerInvariant();
                            this.CssClass = extension;
                            downloadDetail.Append("class=\"downloadDetail\">(");
                            switch (extension)
                            {
                                case "doc":
                                    downloadDetail.Append(Resources.AttachmentFormatDOC);
                                    link.Attributes["type"] = "application/msword";
                                    break;
                                case "pdf":
                                    downloadDetail.Append(Resources.AttachmentFormatPDF);
                                    link.Attributes["type"] = "application/pdf";
                                    break;
                                case "rtf":
                                    downloadDetail.Append(Resources.AttachmentFormatRTF);
                                    link.Attributes["type"] = "application/rtf";
                                    break;
                                case "xls":
                                    downloadDetail.Append(Resources.AttachmentFormatXLS);
                                    link.Attributes["type"] = "application/excel";
                                    break;
                                case "mp3":
                                    downloadDetail.Append(Resources.AttachmentFormatMP3);
                                    link.Attributes["type"] = "audio/mpeg";
                                    break;

                            }
                            downloadDetail.Append(" &#8211; <span class=\"downloadSize\">").Append(size).Append("</span>)</span>");
                        }
                        else
                        {
                            downloadDetail.Append("class=\"downloadSize\">(").Append(size).Append(")</span>)");
                        }
                        presentationContainer.Controls.Add(new LiteralControl(downloadDetail.ToString()));
                    }

                    this.Visible = true;
                }
                else this.Visible = false;

            }

        }

        /// <summary>
        /// Before saving, check that the selected file matches the required file extension
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSavingContent(PlaceholderControlSavingEventArgs e)
        {
            AttachmentPlaceholder ph = this.BoundPlaceholder as AttachmentPlaceholder;
            if (this.AttachmentUrl.Length > 0 && this.allowedExtensions.Length > 0)
            {
                bool allowed = false;
                foreach (string ext in this.allowedExtensions)
                {
                    if (this.AttachmentUrl.ToLower().EndsWith("." + ext.ToLower()))
                    {
                        allowed = true;
                        break;
                    }
                }
                e.Cancel = !allowed;
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
        /// </summary>
        /// <param name="e"></param>
        /// <nodoc/>
        /// <remarks>TAGGED_AS_NODOC</remarks>
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

            if (placeholderIsVisible) base.SavePlaceholderContent(e);
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
