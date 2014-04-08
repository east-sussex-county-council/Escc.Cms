using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using EsccWebTeam.Cms.Placeholders;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Check that a <see cref="EsccWebTeam.Cms.Placeholders.RichHtmlPlaceholderControl"/> does not contain any links to documents
    /// </summary>
    public class NoDocumentsValidator : CustomValidator
    {
        /// <summary>
        /// Creates a new instance of a <seealso cref="NoDocumentsValidator"/>
        /// </summary>
        public NoDocumentsValidator()
        {
            this.Display = ValidatorDisplay.None;
            this.EnableClientScript = false;
        }

        /// <summary>
        /// Test whether the placeholder contains links to documents
        /// </summary>
        /// <returns></returns>
        protected override bool EvaluateIsValid()
        {
            var placeholder = this.NamingContainer.FindControl(this.ControlToValidate) as RichHtmlPlaceholderControl;
            var matches = Regex.Matches(placeholder.EditorHtml, CmsUtilities.DownloadLinkPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (matches.Count == 0)
            {
                return true;
            }
            else
            {
                if (String.IsNullOrEmpty(this.ErrorMessage))
                {
                    this.ErrorMessage = "You linked to a document, '" + matches[0].Groups["linktext"].Value + "'. Do not link to documents from here.";
                }
                return false;
            }
        }
    }
}
