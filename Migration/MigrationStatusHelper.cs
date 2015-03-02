using System;
using Microsoft.ContentManagement.Publishing;

namespace EsccWebTeam.Cms.Migration
{
    /// <summary>
    /// Manage the "Migration status" custom property depending on the values of other migration properties
    /// </summary>
    public class MigrationStatusHelper
    {
        public void UpdateMigrationStatus(Posting posting)
        {
            var migrationStatus = CmsUtilities.GetCustomProperty(posting.CustomProperties, "Migration status");

            if (migrationStatus != null && CanHandleMigrationStatus(migrationStatus.Value))
            {
                var updatedMigrationStatus = WorkOutMigrationStatus(posting);
                if (migrationStatus.Value != updatedMigrationStatus && updatedMigrationStatus != null)
                {
                    migrationStatus.Value = updatedMigrationStatus;
                }
            }
        }

        public string WorkOutMigrationStatus(Posting posting)
        {
            // Check the status is in a state we know how to deal with
            var migrationStatus = FromCustomProperty(posting, "Migration status");

            var copyToReview = FromCustomProperty(posting, "Copy to review site");
            if (String.IsNullOrWhiteSpace(copyToReview))
            {
                return "Unassigned";
            }
            else
            {
                return "Assigned for review";
            }
        }

        /// <summary>
        /// Checks whether this module is currently set up to handle the given migration status.
        /// </summary>
        /// <param name="migrationStatus">The migration status.</param>
        /// <returns></returns>
        public bool CanHandleMigrationStatus(string migrationStatus)
        {
            return (migrationStatus == "Unassigned" || migrationStatus == "Assigned for review");
        }

        private string FromCustomProperty(Posting posting, string propertyName)
        {
            var prop = CmsUtilities.GetCustomProperty(posting.CustomProperties, propertyName);
            return (prop != null) ? prop.Value : String.Empty;
        }
    }
}
