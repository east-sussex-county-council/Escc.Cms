namespace EsccWebTeam.Cms.Permissions
{
    /// <summary>
    /// Permission levels used by Microsoft CMS 2002
    /// </summary>
    public enum CmsRole
    {
        /// <summary>
        /// Authors can create, edit and submit pages
        /// </summary>
        Author = 3,

        /// <summary>
        /// Editors can create, edit, submit and approve pages
        /// </summary>
        Editor = 4,

        /// <summary>
        /// Channel managers can create, edit, submit and approve pages, and create new channels
        /// </summary>
        ChannelManager = 8,

        /// <summary>
        /// Resource managers can add and remove files from resource manager
        /// </summary>
        ResourceManager = 6
    }
}