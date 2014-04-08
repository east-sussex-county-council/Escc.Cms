using System.Web.UI.HtmlControls;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;
using System.Web.UI;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Attachment placeholder for an alternative download format
    /// </summary>
    [ValidationProperty("AttachmentUrl")]
    public class AlternativeAttachmentPlaceholderControl : SingleAttachmentPlaceholderControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlternativeAttachmentPlaceholderControl"/> class.
        /// </summary>
        public AlternativeAttachmentPlaceholderControl()
        {
        }

        /// <summary>
        /// Override published display format to be "(icon) Also in: format (linked) (size k)"
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(Microsoft.ContentManagement.WebControls.BaseModeContainer presentationContainer)
        {
            AttachmentPlaceholder ph = this.BoundPlaceholder as AttachmentPlaceholder;
            if (ph != null)
            {
                if (ph.Url.Length > 0)
                {
                    // get file extension
                    string format = System.IO.Path.GetExtension(ph.Url).Replace(".", "").ToLower();

                    // get ref to resource
                    string guid = CmsUtilities.GetGuidFromUrl(ph.Url);
                    Resource res = CmsHttpContext.Current.Searches.GetByGuid(guid) as Resource;

                    // start with icon for file format
                    HtmlGenericControl also = new HtmlGenericControl("span");
                    also.Attributes.Add("class", "downloadAlso");
                    also.InnerText = "Also in: ";
                    presentationContainer.Controls.Add(also);
                    //presentationContainer.Controls.Add(icon);

                    // link to file using name of format
                    HtmlAnchor link = new HtmlAnchor();
                    link.Title = "View "; // default action
                    switch (format)
                    {
                        case "rtf":
                            link.InnerText = "Rich text";
                            link.Attributes["type"] = "application/rtf";
                            break;
                        case "doc":
                            link.InnerText = "Word";
                            link.Attributes["type"] = "application/msword";
                            break;
                        case "xls":
                            link.InnerText = "Excel";
                            link.Attributes["type"] = "application/excel";
                            break;
                        case "pdf":
                            link.InnerText = "Acrobat (PDF)";
                            link.Attributes["type"] = "application/pdf";
                            break;
                        case "ppt":
                            link.InnerText = "PowerPoint";
                            link.Attributes["type"] = "application/powerpoint";
                            break;
                        case "mp3":
                            link.InnerText = "MP3";
                            link.Attributes["type"] = "audio/mpeg3";
                            link.Title = "Listen to "; // change action
                            break;
                        case "wma":
                            link.InnerText = "Windows Media";
                            link.Attributes["type"] = "audio/x-ms-wma";
                            link.Title = "Listen to "; // change action
                            break;
                        case "xml":
                            link.InnerText = "XML";
                            link.Attributes["type"] = "text/xml";
                            break;
                        default:
                            link.InnerText = "Alternative format";
                            break;
                    }
                    link.Attributes["class"] = format;
                    link.HRef = ph.Url;
                    string docTitle = (res != null) ? "'" + res.DisplayName + "'" : "this document";
                    link.Title += docTitle + " in " + link.InnerText + " format";
                    presentationContainer.Controls.Add(link);

                    // display the file size in brackets
                    string size = "";
                    if (res != null) size = CmsUtilities.GetResourceFileSize(res);
                    if (size.Length > 0)
                    {
                        HtmlGenericControl sizeElement = new HtmlGenericControl("span");
                        sizeElement.InnerText = " (" + size + ")";
                        sizeElement.Attributes.Add("class", "downloadSize");
                        presentationContainer.Controls.Add(sizeElement);
                    }

                    this.Visible = true;
                }
                else this.Visible = false;

            }
        }

    }
}
