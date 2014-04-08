using System;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Events;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Standard behaviours occurring on CMS events so that they can be updated in one project for all templates
    /// </summary>
    public static class CmsEventBehaviours
    {
        /// <summary>
        /// Run standard actions when a posting is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnPostingCreated(Object sender, CreatedEventArgs e)
        {
            // set the default expiry date for a new posting
            CmsUtilities.SetDefaultExpiry(e.Target as Posting);
        }

        /// <summary>
        /// Run standard actions when a property of a posting is about to change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnPostingPropertyChanging(Object sender, PropertyChangingEventArgs e)
        {
            Posting p = e.Target as Posting;
            CmsUtilities.ApplyExpiryRules(e);
            CmsUtilities.ApplyNamingRules(e);
        }

        /// <summary>
        /// Run standard actions when a posting is submitted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnPostingSubmitting(Object sender, ChangingEventArgs e)
        {
            // get ref to posting
            Posting posting = e.Target as Posting;

            // tidy up XHTML in all placeholders when page is approved
            CmsUtilities.TidyXhtml(posting);

            // Save login name of person submitting
            CmsUtilities.SaveSubmitterInfo(posting);

            // Generate and validate metadata
            CmsUtilities.ApplyMetadata(posting);
        }

        /// <summary>
        /// Run standard actions when a posting is approved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnPostingApproving(Object sender, ChangingEventArgs e)
        {
            Posting posting = e.Target as Posting;

            // tidy up XHTML in all placeholders when page is approved
            CmsUtilities.TidyXhtml(posting);

            // tidy up data for search
            CmsUtilities.TidyDataBeforeApproval(posting);

            // Generate and validate metadata
            CmsUtilities.ApplyMetadata(posting);

            // Save login name of person approving
            CmsUtilities.SaveApproverInfo(posting);
        }
    }
}
