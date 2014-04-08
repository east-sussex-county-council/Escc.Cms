
using System;
namespace EsccWebTeam.Cms.Permissions
{
    /// <summary>
    /// A channel, gallery or group in Microsoft CMS 2002
    /// </summary>
    public class CmsNode
    {
        /// <summary>
        /// The published URL of the node
        /// </summary>
        public Uri PublishedUrl { get; set; }

        /// <summary>
        /// Gets the unpublished URL of the node
        /// </summary>
        /// <value>The unpublished URL.</value>
        public Uri UnpublishedUrl
        {
            get
            {
                if (Guid == Guid.Empty) return null;
                if (NodeType != CmsNodeType.Channel) return null;

                return new Uri("/NR/exeres/" + Guid + ",frameless.htm?NRMODE=Unpublished&WBCMODE=PresentationUnpublished&wbc_purpose=Basic", UriKind.Relative);
            }
        }

        /// <summary>
        /// The GUID of the CMS node
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets whether the node respresents a channel, gallery or group
        /// </summary>
        public CmsNodeType NodeType { get; set; }
    }
}
