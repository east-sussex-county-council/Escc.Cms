using System;
using Microsoft.ContentManagement.Publishing;

namespace EsccWebTeam.Cms
{


    /// <summary>
    /// Arguments for a CMS-related event
    /// </summary>
    public class CmsEventArgs : EventArgs
    {
        #region Fields

        private Posting posting;
        private Channel channel;
        private CmsContext context;
        private Placeholder placeholder;
        private ResourceGallery resourceGallery;
        private Resource resource;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the posting for which the event was raised (if applicable)
        /// </summary>
        public Posting Posting
        {
            get
            {
                return this.posting;
            }
            set
            {
                this.posting = value;
            }
        }



        /// <summary>
        /// Gets or sets the channel for which the event was raised
        /// </summary>
        public Channel Channel
        {
            get
            {
                return this.channel;
            }
            set
            {
                this.channel = value;
            }
        }


        /// <summary>
        /// Gets or sets the CmsContext in which the event was raised
        /// </summary>
        public CmsContext Context
        {
            get
            {
                return this.context;
            }
            set
            {
                this.context = value;
            }
        }


        /// <summary>
        /// Gets or sets the placeholder for which the event was raised (if applicable)
        /// </summary>
        public Placeholder Placeholder
        {
            get
            {
                return this.placeholder;
            }
            set
            {
                this.placeholder = value;
            }
        }


        /// <summary>
        /// Gets or sets the resource for which the event was raised (if applicable)
        /// </summary>
        public Resource Resource
        {
            get
            {
                return this.resource;
            }
            set
            {
                this.resource = value;
            }
        }


        /// <summary>
        /// Gets or sets the resource gallery for which the event was raised (if applicable)
        /// </summary>
        public ResourceGallery ResourceGallery
        {
            get
            {
                return this.resourceGallery;
            }
            set
            {
                this.resourceGallery = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Arguments for a CMS-related event
        /// </summary>
        public CmsEventArgs()
        {

        }

        /// <summary>
        /// Arguments for a CMS-related event
        /// </summary>
        /// <param name="context">The CmsContext in which the event was raised</param>
        /// <param name="channel">The channel for which the event was raised</param>
        public CmsEventArgs(CmsContext context, Channel channel)
        {
            this.context = context;
            this.channel = channel;
        }

        /// <summary>
        /// Arguments for a CMS-related event
        /// </summary>
        /// <param name="context">The CmsContext in which the event was raised</param>
        /// <param name="channel">The channel for which the event was raised</param>
        /// <param name="posting">The posting for which the event was raised</param>
        public CmsEventArgs(CmsContext context, Channel channel, Posting posting)
        {
            this.context = context;
            this.channel = channel;
            this.posting = posting;
        }

        /// <summary>
        /// Arguments for a CMS-related event
        /// </summary>
        /// <param name="context">The CmsContext in which the event was raised</param>
        /// <param name="channel">The channel for which the event was raised</param>
        /// <param name="posting">The posting for which the event was raised</param>
        /// <param name="ph">The placeholder for which the event was raised</param>
        public CmsEventArgs(CmsContext context, Channel channel, Posting posting, Placeholder ph)
        {
            this.context = context;
            this.channel = channel;
            this.posting = posting;
            this.placeholder = ph;
        }

        /// <summary>
        /// Arguments for a CMS-related event
        /// </summary>
        /// <param name="context">The CmsContext in which the event was raised</param>
        /// <param name="gallery">The resource gallery for which the event was raised</param>
        public CmsEventArgs(CmsContext context, ResourceGallery gallery)
        {
            this.context = context;
            this.resourceGallery = gallery;
        }

        /// <summary>
        /// Arguments for a CMS-related event
        /// </summary>
        /// <param name="context">The CmsContext in which the event was raised</param>
        /// <param name="gallery">The resource gallery for which the event was raised</param>
        /// <param name="resource">The resource for which the event was raised</param>
        public CmsEventArgs(CmsContext context, ResourceGallery gallery, Resource resource)
        {
            this.context = context;
            this.resourceGallery = gallery;
            this.resource = resource;
        }

        #endregion
    }
}
