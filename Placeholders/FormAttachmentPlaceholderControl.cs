using System;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Attachment placeholder for a form uploaded to a form download page
    /// </summary>
    [ValidationProperty("AttachmentUrl")]
    public class FormAttachmentPlaceholderControl : SingleAttachmentPlaceholderControl
    {
        private FormAttachmentType attachmentType;
        private ResourceManager resManager;

        /// <summary>
        /// Gets or sets whether this placeholder is for a PDF, an RTF, or an RTF which requires a signature
        /// </summary>
        public FormAttachmentType AttachmentType
        {
            get
            {
                return this.attachmentType;
            }
            set
            {
                this.attachmentType = value;
            }
        }

        /// <summary>
        /// Attachment placeholder for a form uploaded to a form download page
        /// </summary>
        public FormAttachmentPlaceholderControl()
        {
            this.resManager = (ResourceManager)this.Context.Cache.Get("FormPlaceholderControls");
            if (this.resManager == null)
            {
                this.resManager = new ResourceManager("EsccWebTeam.Cms.Properties.Resources", Assembly.GetExecutingAssembly());
                this.Context.Cache.Insert("FormPlaceholderControls", this.resManager, null, DateTime.MaxValue, TimeSpan.FromMinutes(10));
            }
        }

        /// <summary>
        /// Present as a standard AttachmentPlaceholderControl, preceded by a label
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            // create a label for the type of document we want
            HtmlGenericControl label = new HtmlGenericControl("label");
            label.Attributes["for"] = this.ID + "_AuthoringModeControlsContainer_AttachmentEditControl";
            label.InnerText = this.resManager.GetString("FormAttachment" + this.attachmentType.ToString());
            authoringContainer.Controls.Add(label);

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

                    // AttachmentText property holds an string identifier for a .NET RESX resource
                    StringBuilder resKey = new StringBuilder("FormAttachment");
                    resKey.Append(ph.AttachmentText);

                    HtmlAnchor link = new HtmlAnchor();
                    link.HRef = ph.Url;
                    link.InnerText = this.resManager.GetString(resKey.ToString());

                    resKey.Append("Title");
                    StringBuilder linkTitle = new StringBuilder(this.resManager.GetString(resKey.ToString()));
                    if (linkTitle != null && linkTitle.Length > 0) link.Title = linkTitle.Append(" (").Append(this.resManager.GetString("Format" + this.attachmentType.ToString())).Append(")").ToString();
                    presentationContainer.Controls.Add(link);

                    // display the file size in brackets
                    string size = "";
                    if (res != null) size = CmsUtilities.GetResourceFileSize(res);
                    if (size.Length > 0)
                    {
                        resKey = resKey.Replace("Title", "Suffix");
                        HtmlGenericControl sizeElement = new HtmlGenericControl("span");
                        sizeElement.InnerHtml = new StringBuilder().Append(" (").Append(this.resManager.GetString("Format" + this.attachmentType.ToString())).Append(" &#8211; <span class=\"downloadSize\">").Append(size).Append("</span>)").Append(this.resManager.GetString(resKey.ToString())).ToString();
                        sizeElement.Attributes.Add("class", "downloadDetail");
                        presentationContainer.Controls.Add(sizeElement);
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
            if (this.AttachmentUrl.Length > 0)
            {
                if (this.attachmentType == FormAttachmentType.Pdf)
                {
                    if (!this.AttachmentUrl.ToLower().EndsWith(".pdf")) e.Cancel = true;
                }
                else if (this.attachmentType == FormAttachmentType.Rtf || this.attachmentType == FormAttachmentType.RtfAndSign)
                {
                    if (!this.AttachmentUrl.ToLower().EndsWith(".rtf")) e.Cancel = true;
                }
                else if (this.attachmentType == FormAttachmentType.Xls || this.attachmentType == FormAttachmentType.XlsPrint)
                {
                    if (!this.AttachmentUrl.ToLower().EndsWith(".xls")) e.Cancel = true;
                }

            }

            base.OnSavingContent(e);
        }



        /// <summary>
        /// Use the attachment text to store the attachment type, which is used to retrieve standard attachment text for that type
        /// </summary>
        /// <param name="e"></param>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            base.SavePlaceholderContent(e);

            AttachmentPlaceholder ph = this.BoundPlaceholder as AttachmentPlaceholder;
            ph.AttachmentText = this.attachmentType.ToString();
        }

        /// <summary>
        /// The type of document expected in this placeholder
        /// </summary>
        public enum FormAttachmentType
        {
            /// <summary>
            /// PDF document to be printed and sent by post
            /// </summary>
            Pdf,

            /// <summary>
            /// RTF document to be filled in an emailed back
            /// </summary>
            Rtf,

            /// <summary>
            /// RTF document to be printed, signed and posted
            /// </summary>
            RtfAndSign,

            /// <summary>
            /// Excel file to print and post
            /// </summary>
            XlsPrint,

            /// <summary>
            /// Excel file to return by email
            /// </summary>
            Xls
        }


    }
}
