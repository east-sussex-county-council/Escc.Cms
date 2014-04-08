using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ContentManagement.Publishing;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Traverse the CMS channel or resource hierarchy in a memory-efficient way
    /// </summary>
    public class CmsTraverser
    {
        #region Traversal of the CMS hierarchy

        private Stack<string> traverseChannelStack;
        private Stack<string> traverseGalleryStack;

        /// <summary>
        /// Traverse every channel, posting and placeholder in a CMS site, starting with the root. Raise an event for each one.
        /// </summary>
        /// <param name="mode">The CMS mode to connect as</param>
        /// <param name="connectAsAdmin">if set to <c>true</c> connect as admin, otherwise connect as the current user.</param>
        /// <remarks><c>CmsApplicationContext</c> is used and disposed regularly to prevent an <c>OutOfMemoryException</c> when traversing
        /// a large CMS site. Based on code by Stefan Goßner from <a href="http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx">http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx</a>.</remarks>
        public void TraverseSite(PublishingMode mode, bool connectAsAdmin)
        {
            if (traverseChannelStack == null) traverseChannelStack = new Stack<string>();

            using (CmsApplicationContext cmsContext = connectAsAdmin ? CmsUtilities.GetAdminContext(mode) : new CmsApplicationContext())
            {
                if (!connectAsAdmin) cmsContext.AuthenticateAsCurrentUser(mode);
                traverseChannelStack.Push(cmsContext.RootChannel.Guid);
            }

            this.OnTraversingSite();
            this.TraverseChannel(mode, connectAsAdmin, null);
            this.OnTraversedSite();
        }

        /// <summary>
        /// Traverse every resource gallery and resource in a CMS resource hierarchy, starting with the root. Raise an event for each one.
        /// </summary>
        /// <param name="mode">The CMS mode to connect as</param>
        /// <param name="connectAsAdmin">if set to <c>true</c> connect as admin, otherwise connect as the current user.</param>
        /// <remarks><c>CmsApplicationContext</c> is used and disposed regularly to prevent an <c>OutOfMemoryException</c> when traversing
        /// a large CMS site. Based on code by Stefan Goßner from <a href="http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx">http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx</a>.</remarks>
        public void TraverseResourceTree(PublishingMode mode, bool connectAsAdmin)
        {
            if (traverseGalleryStack == null) traverseGalleryStack = new Stack<string>();

            using (CmsApplicationContext cmsContext = connectAsAdmin ? CmsUtilities.GetAdminContext(mode) : new CmsApplicationContext())
            {
                if (!connectAsAdmin) cmsContext.AuthenticateAsCurrentUser(mode);
                traverseGalleryStack.Push(cmsContext.RootResourceGallery.Guid);
            }

            this.OnTraversingResourceTree();
            this.TraverseResourceGallery(mode, connectAsAdmin, null);
            this.OnTraversedResourceTree();

        }

        /// <summary>
        /// Traverse every channel, posting and placeholder in a CMS site, starting with the given channel. Raise an event for each one.
        /// </summary>
        /// <param name="mode">The CMS mode to connect as</param>
        /// <param name="connectAsAdmin">if set to <c>true</c> connect as admin, otherwise connect as current user.</param>
        /// <param name="startingChannel">The guid of the channel to start at</param>
        /// <remarks><c>CmsApplicationContext</c> is used and disposed regularly to prevent an <c>OutOfMemoryException</c> when traversing
        /// a large CMS site. Based on code by Stefan Goßner from <a href="http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx">http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx</a>.</remarks>
        public void TraverseChannel(PublishingMode mode, bool connectAsAdmin, Guid? startingChannel)
        {
            if (traverseChannelStack == null) traverseChannelStack = new Stack<string>();
            if (startingChannel != null) traverseChannelStack.Push("{" + startingChannel.ToString() + "}");

            while (traverseChannelStack.Count > 0)
            {
                using (CmsApplicationContext cmsContext = connectAsAdmin ? CmsUtilities.GetAdminContext(mode) : new CmsApplicationContext())
                {

                    if (!connectAsAdmin) cmsContext.AuthenticateAsCurrentUser(mode);
                    string nextChannelGuid = traverseChannelStack.Pop();
                    Channel channel = cmsContext.Searches.GetByGuid(nextChannelGuid) as Channel;

                    this.OnTraversingChannel(cmsContext, channel);

                    foreach (Posting posting in channel.Postings) this.TraversePosting(cmsContext, channel, posting);
                    foreach (Channel subChannel in channel.Channels) traverseChannelStack.Push(subChannel.Guid);

                    this.OnTraversedChannel(cmsContext, channel);
                }
                // lets explicitly call the garbage collector to keep memory consumption small
                System.GC.Collect();

            }
        }

        /// <summary>
        /// Traverse every placeholder in a CMS posting. Raise an event for each one.
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="channel">The channel the posting is in</param>
        /// <param name="posting">The posting to traverse</param>
        public void TraversePosting(CmsContext context, Channel channel, Posting posting)
        {
            if (context == null) throw new ArgumentNullException("context", "The supplied CMS context was null. Please supply a valid CMS context or use an alternative method overload.");
            if (channel == null) throw new ArgumentNullException("channel", "The supplied CMS channel was null. Please supply a valid CMS channel.");
            if (posting == null) throw new ArgumentNullException("posting", "The supplied CMS posting was null. Please supply a valid CMS posting.");

            this.OnTraversingPosting(context, channel, posting);

            foreach (Placeholder ph in posting.Placeholders) this.TraversePlaceholder(context, channel, posting, ph);

            this.OnTraversedPosting(context, channel, posting);
        }

        /// <summary>
        /// Traverse every placeholder in a CMS posting. Raise an event for each one.
        /// </summary>
        /// <param name="channel">The channel the posting is in</param>
        /// <param name="posting">The posting to traverse</param>
        public void TraversePosting(Channel channel, Posting posting)
        {
            this.TraversePosting(CmsHttpContext.Current, channel, posting);
        }

        /// <summary>
        /// Traverse a single placeholder placeholder in a CMS posting. Raise events to indicate traversal.
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="channel">The channel the posting is in</param>
        /// <param name="posting">The posting the placeholder is part of</param>
        /// <param name="ph">The placeholder to traverse</param>
        /// <remarks>This is a helper method for <c>this.TraversePosting</c></remarks>
        private void TraversePlaceholder(CmsContext context, Channel channel, Posting posting, Placeholder ph)
        {
            if (context == null) throw new ArgumentNullException("context", "The supplied CMS context was null. Please supply a valid CMS context or use an alternative method overload.");
            if (channel == null) throw new ArgumentNullException("channel", "The supplied CMS channel was null. Please supply a valid CMS channel.");
            if (posting == null) throw new ArgumentNullException("posting", "The supplied CMS posting was null. Please supply a valid CMS posting.");
            if (ph == null) throw new ArgumentNullException("ph", "The supplied CMS placeholder was null. Please supply a valid CMS placeholder.");

            this.OnTraversingPlaceholder(context, channel, posting, ph);
            this.OnTraversedPlaceholder(context, channel, posting, ph);
        }

        /// <summary>
        /// Traverse every gallery and resource in a CMS Resource Gallery, starting with the given gallery. Raise an event for each one.
        /// </summary>
        /// <param name="mode">The CMS mode to connect as</param>
        /// <param name="connectAsAdmin">if set to <c>true</c> connect as admin, otherwise connect as current user.</param>
        /// <param name="startingGallery">The guid of the gallery to start with. Only this gallery and its descendants will be traversed.</param>
        /// <remarks><c>CmsApplicationContext</c> is used and disposed regularly to prevent an <c>OutOfMemoryException</c> when traversing
        /// a large CMS site. Based on code by Stefan Goßner from <a href="http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx">http://blogs.technet.com/stefan_gossner/archive/2005/03/07/386526.aspx</a>.</remarks>
        public void TraverseResourceGallery(PublishingMode mode, bool connectAsAdmin, Guid? startingGallery)
        {
            if (traverseGalleryStack == null) traverseGalleryStack = new Stack<string>();
            if (startingGallery != null) traverseGalleryStack.Push("{" + startingGallery.ToString() + "}");

            while (traverseGalleryStack.Count > 0)
            {
                using (CmsApplicationContext cmsContext = connectAsAdmin ? CmsUtilities.GetAdminContext(mode) : new CmsApplicationContext())
                {
                    if (!connectAsAdmin) cmsContext.AuthenticateAsCurrentUser(mode);
                    string nextGalleryGuid = traverseGalleryStack.Pop();
                    ResourceGallery gallery = cmsContext.Searches.GetByGuid(nextGalleryGuid) as ResourceGallery;

                    this.OnTraversingResourceGallery(cmsContext, gallery);

                    foreach (Resource res in gallery.Resources) this.TraverseResource(cmsContext, gallery, res);
                    foreach (ResourceGallery subGallery in gallery.ResourceGalleries) traverseGalleryStack.Push(subGallery.Guid);

                    this.OnTraversedResourceGallery(cmsContext, gallery);
                }
                // lets explicitly call the garbage collector to keep memory consumption small
                System.GC.Collect();

            }
        }

        /// <summary>
        /// Traverse a single resource in a CMS resource gallery. Raise events to indicate traversal.
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="gallery">The gallery to start with. Only this gallery and its descendants will be traversed.</param>
        /// <param name="resource">The resource to traverse.</param>
        public void TraverseResource(CmsContext context, ResourceGallery gallery, Resource resource)
        {
            if (context == null) throw new ArgumentNullException("context", "The supplied CMS context was null. Please supply a valid CMS context or use an alternative method overload.");
            if (gallery == null) throw new ArgumentNullException("gallery", "The supplied CMS resource gallery was null. Please supply a valid CMS resource gallery.");
            if (resource == null) throw new ArgumentNullException("resource", "The supplied CMS resource was null. Please supply a valid CMS resource.");

            this.OnTraversingResource(context, gallery, resource);
            this.OnTraversedResource(context, gallery, resource);
        }

        /// <summary>
        /// Traverse a single resource in a CMS resource gallery. Raise events to indicate traversal.
        /// </summary>
        /// <param name="gallery">The gallery to start with. Only this gallery and its descendants will be traversed.</param>
        /// <param name="resource">The resource to traverse.</param>
        public void TraverseResource(ResourceGallery gallery, Resource resource)
        {
            this.TraverseResource(CmsHttpContext.Current, gallery, resource);
        }
        #endregion

        #region Traversal of CMS hierarchy (events)

        /// <summary>
        /// Event indicating that a traversal of entire CMS hierarchy has begun at root channel
        /// </summary>
        public event CmsEventHandler TraversingSite;

        /// <summary>
        /// Event indicating that a traversal of entire CMS hierarchy has completed
        /// </summary>
        public event CmsEventHandler TraversedSite;

        /// <summary>
        /// Event indicating that a traversal of the CMS channel hierarchy has reached a channel
        /// </summary>
        public event CmsEventHandler TraversingChannel;

        /// <summary>
        /// Event indicating that a traversal of the CMS channel hierarchy has completed traversing a channel
        /// </summary>
        public event CmsEventHandler TraversedChannel;

        /// <summary>
        /// Event indicating that a traversal of the CMS channel hierarchy has reached a posting
        /// </summary>
        public event CmsEventHandler TraversingPosting;

        /// <summary>
        /// Event indicating that a traversal of the CMS channel hierarchy has completed traversing a posting
        /// </summary>
        public event CmsEventHandler TraversedPosting;

        /// <summary>
        /// Event indicating that a traversal of the CMS channel hierarchy has reached a placeholder
        /// </summary>
        public event CmsEventHandler TraversingPlaceholder;

        /// <summary>
        /// Event indicating that a traversal of the CMS channel hierarchy has completed traversing a placeholder
        /// </summary>
        public event CmsEventHandler TraversedPlaceholder;

        /// <summary>
        /// Event indicating that a traversal of entire CMS resource hierarchy has begun at root channel
        /// </summary>
        public event CmsEventHandler TraversingResourceTree;

        /// <summary>
        /// Event indicating that a traversal of entire CMS resource hierarchy has completed
        /// </summary>
        public event CmsEventHandler TraversedResourceTree;

        /// <summary>
        /// Event indicating that a traversal of the CMS resource hierarchy has reached a resource gallery
        /// </summary>
        public event CmsEventHandler TraversingResourceGallery;

        /// <summary>
        /// Event indicating that a traversal of the CMS resource hierarchy has completed traversing a resource gallery
        /// </summary>
        public event CmsEventHandler TraversedResourceGallery;

        /// <summary>
        /// Event indicating that a traversal of the CMS resource hierarchy has reached a resource
        /// </summary>
        public event CmsEventHandler TraversingResource;

        /// <summary>
        /// Event indicating that a traversal of the CMS resource hierarchy has completed traversing a resource
        /// </summary>
        public event CmsEventHandler TraversedResource;


        #endregion Traversal of CMS hierarchy (event delegates)

        #region Traversal of CMS hierarchy (OnEvent methods)

        /// <summary>
        /// Raise an event indicating that a traversal of entire CMS hierarchy is about to begin at root channel
        /// </summary>
        private void OnTraversingSite()
        {
            if (this.TraversingSite != null) this.TraversingSite(this.GetType(), new CmsEventArgs());
        }

        /// <summary>
        /// Raise an event indicating that a traversal of entire CMS hierarchy has completed
        /// </summary>
        private void OnTraversedSite()
        {
            if (this.TraversedSite != null) this.TraversedSite(this.GetType(), new CmsEventArgs());
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS channel hierarchy has reached a channel
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="channel">The channel to traverse</param>
        private void OnTraversingChannel(CmsContext context, Channel channel)
        {
            if (this.TraversingChannel != null) this.TraversingChannel(this.GetType(), new CmsEventArgs(context, channel));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS channel hierarchy has completed traversing a channel
        /// </summary>
        /// <param name="context">The authenticated context used for the traversal</param>
        /// <param name="channel">The channel traversed</param>
        private void OnTraversedChannel(CmsContext context, Channel channel)
        {
            if (this.TraversedChannel != null) this.TraversedChannel(this.GetType(), new CmsEventArgs(context, channel));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS channel hierarchy has reached a posting
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="channel">The channel the posting is in</param>
        /// <param name="posting">The posting to traverse</param>
        private void OnTraversingPosting(CmsContext context, Channel channel, Posting posting)
        {
            if (this.TraversingPosting != null) this.TraversingPosting(this.GetType(), new CmsEventArgs(context, channel, posting));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS channel hierarchy has completed traversing a posting
        /// </summary>
        /// <param name="context">The authenticated context used for the traversal</param>
        /// <param name="channel">The channel the posting is in</param>
        /// <param name="posting">The posting traversed</param>
        private void OnTraversedPosting(CmsContext context, Channel channel, Posting posting)
        {
            if (this.TraversedPosting != null) this.TraversedPosting(this.GetType(), new CmsEventArgs(context, channel, posting));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS channel hierarchy has reached a placeholder
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="channel">The channel the posting is in</param>
        /// <param name="posting">The posting the placeholder is part of</param>
        /// <param name="ph">The placeholder to traverse</param>
        private void OnTraversingPlaceholder(CmsContext context, Channel channel, Posting posting, Placeholder ph)
        {
            if (this.TraversingPlaceholder != null) this.TraversingPlaceholder(this.GetType(), new CmsEventArgs(context, channel, posting, ph));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS channel hierarchy has completed traversing a placeholder
        /// </summary>
        /// <param name="context">The authenticated context used for the traversal</param>
        /// <param name="channel">The channel the posting is in</param>
        /// <param name="posting">The posting the placeholder is part of</param>
        /// <param name="ph">The placeholder traversed</param>
        private void OnTraversedPlaceholder(CmsContext context, Channel channel, Posting posting, Placeholder ph)
        {
            if (this.TraversedPlaceholder != null) this.TraversedPlaceholder(this.GetType(), new CmsEventArgs(context, channel, posting, ph));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS resource hierarchy has begun with the root gallery
        /// </summary>
        private void OnTraversingResourceTree()
        {
            if (this.TraversingResourceTree != null) this.TraversingResourceTree(this.GetType(), new CmsEventArgs());
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the entire CMS resource hierarchy has completed
        /// </summary>
        private void OnTraversedResourceTree()
        {
            if (this.TraversedResourceTree != null) this.TraversedResourceTree(this.GetType(), new CmsEventArgs());
        }


        /// <summary>
        /// Raise an event indicating that a traversal of the CMS resource hierarchy has reached a resource gallery
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="gallery">The gallery to traverse</param>
        private void OnTraversingResourceGallery(CmsContext context, ResourceGallery gallery)
        {
            if (this.TraversingResourceGallery != null) this.TraversingResourceGallery(this.GetType(), new CmsEventArgs(context, gallery));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS resource hierarchy has completed traversing a resource gallery
        /// </summary>
        /// <param name="context">The authenticated context used for the traversal</param>
        /// <param name="gallery">The gallery traversed</param>
        private void OnTraversedResourceGallery(CmsContext context, ResourceGallery gallery)
        {
            if (this.TraversedResourceGallery != null) this.TraversedResourceGallery(this.GetType(), new CmsEventArgs(context, gallery));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS resource hierarchy has reached a resource
        /// </summary>
        /// <param name="context">The authenticated context to use for the traversal</param>
        /// <param name="gallery">The gallery the resource is in</param>
        /// <param name="resource">The resource to traverse</param>
        private void OnTraversingResource(CmsContext context, ResourceGallery gallery, Resource resource)
        {
            if (this.TraversingResource != null) this.TraversingResource(this.GetType(), new CmsEventArgs(context, gallery, resource));
        }

        /// <summary>
        /// Raise an event indicating that a traversal of the CMS resource hierarchy has completed traversing a resource
        /// </summary>
        /// <param name="context">The authenticated context used for the traversal</param>
        /// <param name="gallery">The gallery the resource is in</param>
        /// <param name="resource">The resource traversed</param>
        private void OnTraversedResource(CmsContext context, ResourceGallery gallery, Resource resource)
        {
            if (this.TraversedResource != null) this.TraversedResource(this.GetType(), new CmsEventArgs(context, gallery, resource));
        }

        #endregion Traversal of CMS hierarchy (OnEvent methods)
    }
}
