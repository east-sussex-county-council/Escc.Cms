
namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Time this snapshot of a posting represents
    /// </summary>
    public enum SnapshotType
    {
        /// <summary>
        /// Overnight update on Tues-Fri
        /// </summary>
        Daily,

        /// <summary>
        /// Overnight update on Monday morning
        /// </summary>
        Weekly,

        /// <summary>
        /// Update when the posting is saved
        /// </summary>
        OnSave,
    }
}
