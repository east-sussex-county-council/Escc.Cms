using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using EsccWebTeam.Cms.Placeholders;
using EsccWebTeam.Data.Web;

namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Check that a placeholder does not include consecutive words in all caps
    /// </summary>
    public class AllCapsValidator : BaseValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AllCapsValidator"/> class.
        /// </summary>
        public AllCapsValidator()
        {
            this.Display = ValidatorDisplay.None;
            this.EnableClientScript = false;
        }

        /// <summary>
        /// When overridden in a derived class, this method contains the code to determine whether the value in the input control is valid.
        /// </summary>
        /// <returns>
        /// true if the value in the input control is valid; otherwise, false.
        /// </returns>
        protected override bool EvaluateIsValid()
        {
            var richHtmlPlaceholder = this.NamingContainer.FindControl(this.ControlToValidate) as RichHtmlPlaceholderControl;
            if (richHtmlPlaceholder == null)
            {
                throw new ArgumentException("The ControlToValidate must be a RichHtmlPlaceholderControl");
            }

            // Strip tags to avoid them getting between words, and to avoid matching anything in attributes. First though, finish certain block elements with a 
            // fullstop so that it doesn't run into the following paragraph and get counted as consecutive words. 
            var text = Regex.Replace(richHtmlPlaceholder.EditorHtml, "</(h[1-6]|li)>", ". ");
            text = Html.StripTags(text);

            // Strip any common acronyms, to avoid matching them as false positives. eg NHS.
            var config = ConfigurationManager.GetSection("EsccWebTeam.Cms/GeneralSettings") as NameValueCollection;
            if (config != null)
            {
                if (!String.IsNullOrEmpty(config["Acronyms"]))
                {
                    text = Regex.Replace(text, @"\b(" + config["Acronyms"].Replace(";", "|") + @")\b", String.Empty);
                }
            }

            // Regex matches two or more consecutive words in all caps. Words with numbers are not included because that traps postcodes. 
            // Because we're looking for space after the second word (to avoid matching "EXAMPLE Example"), we need a second test to trap
            // two words in CAPS at the end of a string. And because we don't want to match "Example) ACRONYM" with ")" counted as the first word,
            // we need to require the A-Z and make the punctuation optional. And because we don't want to match the end of a sentence 
            // (eg "this is PROBABLY. OK for once") the punctuation only includes that which always appears mid-sentence.
            const string punctuationBeforeWord = "['\"(]*";
            const string punctuationAfterWord = "[,)'\"]*";
            const string word = punctuationBeforeWord + "[A-Z-']{2,}" + punctuationAfterWord;

            // mid-sentence
            var match = Regex.Match(text, "(" + word + "\\s+){2,}", RegexOptions.Singleline);

            // end of sentence
            if (!match.Success)
            {
                match = Regex.Match(text, "(" + word + "\\s+)+" + word + "[.?!:;]", RegexOptions.Singleline);
            }

            // end of string
            if (!match.Success)
            {
                match = Regex.Match(text, "(" + word + "\\s+)+" + word + "$", RegexOptions.Singleline);
            }

            if (match.Success)
            {
                this.ErrorMessage = "You typed '" + match.Value.Trim() + "'. Don't write in uppercase as it's seen as <span style=\"text-transform:uppercase\">shouting</span> and, for partially sighted users, it's read out one letter at a time.";
            }
            return !match.Success;
        }
    }
}
