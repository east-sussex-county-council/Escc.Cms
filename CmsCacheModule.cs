using System.Web;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Proxy for <see cref="Microsoft.ContentManagement.Web.Caching.CmsCacheModule"/> which checks whether this is a CMS machine before loading the module
    /// </summary>
    public class CmsCacheModule : IHttpModule
    {
        #region IHttpModule Members

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"/>.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpApplication"/> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application</param>
        public void Init(HttpApplication context)
        {
            if (CmsUtilities.IsCmsEnabled())
            {
                LoadModule(context);
            }
        }

        private static void LoadModule(HttpApplication context)
        {
            var realModule = new Microsoft.ContentManagement.Web.Caching.CmsCacheModule();
            realModule.Init(context);
        }

        #endregion
    }
}
