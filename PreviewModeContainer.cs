using System.Web.UI.WebControls;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Container which displays its child controls only when CMS is in Preview mode
    /// </summary>
    public class PreviewModeContainer : PlaceHolder
    {
        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            this.Visible = (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringPreview ||
                WebAuthorContext.Current.Mode == WebAuthorContextMode.PresentationUnpublishedPreview ||
                WebAuthorContext.Current.Mode == WebAuthorContextMode.TemplatePreview);
        }
    }
}
