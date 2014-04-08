using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Validates that a control contains the script for a Twitter widget
    /// </summary>
    public class TwitterScriptValidator : BaseValidator
    {
        /// <summary>
        /// Try to match the script for a Twitter widget. Not foolproof, but should stop people pasting in any old script without checking.
        /// </summary>
        /// <returns></returns>
        protected override bool EvaluateIsValid()
        {
            var control = this.NamingContainer.FindControl(this.ControlToValidate) as SocialMediaPlaceholderControl;
            if (control == null) return true;

            var text = control.TwitterScript.Trim();
            if (String.IsNullOrEmpty(text)) return true;

            return Regex.IsMatch(text, "^<a class=\"twitter-timeline\"[^>]*>.*?</a>\\s*<script>[^<]+platform.twitter.com[^<]+twitter-wjs[^<]+</script>$");
        }
    }
}
