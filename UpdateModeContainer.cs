using System.Web.UI.WebControls;
using Microsoft.ContentManagement.Publishing;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Container which displays its child controls only when CMS is in Update mode
    /// </summary>
    public class UpdateModeContainer : PlaceHolder
    {
        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            this.Visible = (CmsHttpContext.Current.Mode == PublishingMode.Update);
        }
    }
}
