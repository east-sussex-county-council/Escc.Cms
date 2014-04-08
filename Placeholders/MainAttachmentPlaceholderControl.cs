using System.Web;
using System.Web.UI.HtmlControls;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;
using System.Web.UI;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Attachment placeholder for main download format - includes description and format
    /// </summary>
    [ValidationProperty("AttachmentUrl")]
    public class MainAttachmentPlaceholderControl : SingleAttachmentPlaceholderControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainAttachmentPlaceholderControl"/> class.
        /// </summary>
        public MainAttachmentPlaceholderControl() { }

        /// <summary>
        /// Standard implementation of this method for this specific
        /// control as required by the <see cref="T:Microsoft.ContentManagement.WebControls.BasePlaceholderControl"/>.
        /// </summary>
        /// <param name="presentationContainer"></param>
        /// <nodoc/>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            AttachmentPlaceholder ph = this.BoundPlaceholder as AttachmentPlaceholder;
            if (ph != null)
            {
                if (ph.Url.Length > 0)
                {
                    // build a definition list just for this item
                    HtmlGenericControl dl = new HtmlGenericControl("dl");
                    HtmlGenericControl dt = new HtmlGenericControl("dt");
                    HtmlGenericControl dd = new HtmlGenericControl("dd");
                    dl.Controls.Add(dt);
                    dl.Controls.Add(dd);

                    // get file extension
                    string format = System.IO.Path.GetExtension(ph.Url).Replace(".", "").ToLower();
                    dt.Attributes["class"] = format;

                    // get ref to resource
                    string guid = CmsUtilities.GetGuidFromUrl(ph.Url);
                    Resource res = CmsHttpContext.Current.Searches.GetByGuid(guid) as Resource;

                    // link to file using name of format
                    HtmlAnchor link = new HtmlAnchor();
                    switch (format)
                    {
                        case "rtf":
                            format = "Rich text";
                            link.Attributes["type"] = "application/rtf";
                            break;
                        case "doc":
                            format = "Word";
                            link.Attributes["type"] = "application/msword";
                            break;
                        case "xls":
                            format = "Excel";
                            link.Attributes["type"] = "application/excel";
                            break;
                        case "pdf":
                            format = "Acrobat (PDF)";
                            link.Attributes["type"] = "application/pdf";
                            break;
                        case "ppt":
                            format = "PowerPoint";
                            link.Attributes["type"] = "application/powerpoint";
                            break;
                        case "xml":
                            format = "XML";
                            link.Attributes["type"] = "text/xml";
                            break;
                        default:
                            format = "Alternative format";
                            break;
                    }
                    link.HRef = ph.Url;
                    link.InnerText = (res != null) ? res.DisplayName : ph.AttachmentText;
                    link.Title = "View '" + link.InnerText + "' in " + format + " format";
                    dt.Controls.Add(link);

                    // display the file size in brackets
                    string size = "";
                    if (res != null) size = CmsUtilities.GetResourceFileSize(res);
                    if (size.Length > 0)
                    {
                        HtmlGenericControl sizeElement = new HtmlGenericControl("span");
                        sizeElement.InnerText = " (" + size + ")";
                        sizeElement.Attributes.Add("class", "downloadSize");
                        dt.Controls.Add(sizeElement);
                    }

                    // display the description, if present
                    if (res != null && res.Description.Length > 0)
                    {
                        dd.InnerHtml = HttpUtility.HtmlEncode(res.Description);
                        dd.Visible = true;
                    }
                    else dd.Visible = false;

                    presentationContainer.Controls.Add(dl);

                    this.Visible = true;
                }
                else this.Visible = false;

            }

        }

        /// <summary>
        /// Override to prevent span tag
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(System.Web.UI.HtmlTextWriter writer)
        {
        }

        /// <summary>
        /// Override to prevent span tag
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(System.Web.UI.HtmlTextWriter writer)
        {
        }



    }
}
