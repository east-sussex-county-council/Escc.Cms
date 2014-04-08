using System.Web;
using Microsoft.ContentManagement.Web.Security;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Proxy for <see cref="CmsAuthorizationModule"/> which checks whether this is a CMS machine before loading the module
    /// </summary>
    public class CmsAuthorisationModule : IHttpModule
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
            var realModule = new CmsAuthorizationModule();
            realModule.Init(context);
        }

        #endregion
    }
}
