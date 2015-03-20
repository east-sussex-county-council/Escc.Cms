 /*using System;
using System.Web;
using Escc.Content.Migration.Domain.Entity;
using Microsoft.ContentManagement.Publishing;
using Microsoft.ContentManagement.Publishing.Events;

namespace EsccWebTeam.Cms.Migration
{
   /// <summary>
    /// When a page changes in Microsoft CMS, if the change needs to be migrated kick off the migration process to Umbraco
    /// </summary>
    public class CmsMigrationEventsModule : IHttpModule
    {
        private bool _attachedEvents;

        #region IHttpModule Members

        public void Dispose()
        {
            //clean-up code here.
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }

        #endregion

        private void context_BeginRequest(Object source, EventArgs e)
        {
            if (_attachedEvents) return;

            PostingEvents.Current.CustomPropertyChanging += Current_CustomPropertyChanging;
            PostingEvents.Current.CustomPropertyChanged += Current_CustomPropertyChanged;

            PostingEvents.Current.Created += Current_Created;
            PostingEvents.Current.Moved += Current_Moved;
            PostingEvents.Current.Approved += Current_Approved;
            PostingEvents.Current.Deleted += Current_Deleted;

            // Not tracking Declined because we don't use it
            // Not tracking Submitted or Changed because we're only handling published pages at the moment
            // Not tracking *PropertyChanged because events should always fire for the page too

            _attachedEvents = true;
        }

        private void Current_CustomPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Copy to review site")
            {
                var posting = (e.Target as Posting);
                if (posting == null) return;

                // When this property has changed we need to update the migration status
                var migrationStatusHelper = new MigrationStatusHelper();
                migrationStatusHelper.UpdateMigrationStatus(posting);

                // Because the copy to review date has changed, trigger a new copy to review by removing the copied date
                var copiedToReview = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Copied to review");
                if (copiedToReview != null && !String.IsNullOrEmpty(copiedToReview.Value))
                {
                    copiedToReview.Value = String.Empty;
                }
            }
        }

        private void Current_CustomPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            // This looks redundant, but it's important because when the "copy to review" date changes it updates the "migration status",
            // but then another CustomPropertyChanging event is fired on "migration status" which resets it. Not sure where that event
            // comes from, but this ensures that when it comes it just sets the value to what we want, rather than setting it back to what it was.
            if (e.PropertyName == "Migration status")
            {
                var migrationStatusHelper = new MigrationStatusHelper();
                if (!migrationStatusHelper.CanHandleMigrationStatus(e.PropertyValue.ToString())) return;

                var updatedMigrationStatus = migrationStatusHelper.WorkOutMigrationStatus(e.Target as Posting);
                if (updatedMigrationStatus != null)
                {
                    e.PropertyValue = updatedMigrationStatus;
                }
            }
        }


        private string WorkOutDestinationCms(Posting posting)
        {
            var copiedToReview = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Copied to review");
            var copiedToUserAcceptance = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Copied to live - test");
            var copyToLive = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Copy to live site");
            var copiedToLive = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Copied to live");
            var migrationStatus = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Migration status");

            if (migrationStatus != null && (migrationStatus.Value == "Assigned for review" || migrationStatus.Value == "Changed since review") &&
                copiedToReview != null && String.IsNullOrEmpty(copiedToReview.Value))
            {
                return DeploymentInstance.Review;
            }

            if ((migrationStatus != null && migrationStatus.Value == "Approved") &&
                copiedToUserAcceptance != null && String.IsNullOrEmpty(copiedToUserAcceptance.Value))
            {
                return DeploymentInstance.UserAcceptance;
            }

            if (copyToLive != null && !String.IsNullOrEmpty(copyToLive.Value) &&
                copiedToLive != null && String.IsNullOrEmpty(copiedToLive.Value))
            {
                return DeploymentInstance.Live;
            }

            return null;
        }

        void Current_Created(object sender, CreatedEventArgs e)
        {
            var posting = (e.Target as Posting);
            if (posting == null) return;

            var destinationCms = WorkOutDestinationCms(posting);
            if (!String.IsNullOrEmpty(destinationCms))
            {
                new JobGeneration().CreateJob(posting.Guid, Command.Created, destinationCms);
            }
        }

        void Current_Moved(object sender, MovedEventArgs e)
        {
            var posting = (e.Target as Posting);
            if (posting == null) return;

            var destinationCms = WorkOutDestinationCms(posting);
            if (!String.IsNullOrEmpty(destinationCms))
            {
                new JobGeneration().CreateJob(posting.Guid, Command.Moved, destinationCms);
            }
        }

        void Current_Approved(object sender, ChangedEventArgs e)
        {
            var posting = (e.Target as Posting);
            if (posting == null) return;

            var destinationCms = WorkOutDestinationCms(posting);
            if (!String.IsNullOrEmpty(destinationCms))
            {
                new JobGeneration().CreateJob(posting.Guid, Command.Approved, destinationCms);
            }
        }

        void Current_Deleted(object sender, ChangedEventArgs e)
        {
            var posting = (e.Target as Posting);
            if (posting == null) return;

            var destinationCms = WorkOutDestinationCms(posting);
            if (!String.IsNullOrEmpty(destinationCms))
            {
                new JobGeneration().CreateJob(posting.Guid, Command.Deleted, destinationCms);
            }
        }
    }
}*/
