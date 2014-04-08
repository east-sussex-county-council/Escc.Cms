
namespace EsccWebTeam.Cms
{
    /// <summary>
    /// Statistics generating from analysing the content of a posting
    /// </summary>
    public class PostingAnalysis
    {
        /// <summary>
        /// Total length of the HTML in all placeholders
        /// </summary>
        public int ContentLength { get; set; }

        /// <summary>
        /// Total number of HTML headers in all placeholders
        /// </summary>
        public int TotalHeadings { get { return TotalHeading1 + TotalHeading2 + TotalHeading3 + TotalHeading4 + TotalHeading5; } }

        /// <summary>
        /// Total number of h1 elements in all placeholders
        /// </summary>
        public int TotalHeading1 { get; set; }

        /// <summary>
        /// Total number of h2 elements in all placeholders
        /// </summary>
        public int TotalHeading2 { get; set; }

        /// <summary>
        /// Total number of h3 elements in all placeholders
        /// </summary>
        public int TotalHeading3 { get; set; }

        /// <summary>
        /// Total number of h4 elements in all placeholders
        /// </summary>
        public int TotalHeading4 { get; set; }

        /// <summary>
        /// Total number of h5 elements in all placeholders
        /// </summary>
        public int TotalHeading5 { get; set; }

        /// <summary>
        /// Total number of a elements with an href attribute in all placeholders
        /// </summary>
        public int TotalLinks { get; set; }

        /// <summary>
        /// Total number of a elements in all placeholders which link to a list of common extensions belonging to documents
        /// </summary>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Total number of a elements which include a fragment identifier in the href attribute
        /// </summary>
        public int TotalLinksToAnchors { get; set; }

        /// <summary>
        /// Measure of reading ease of the page
        /// </summary>
        public double FleschKincaidReadingEase { get; set; }

        /// <summary>
        /// Average education level needed to read the page
        /// </summary>
        public double AverageGradeLevel { get; set; }
    }
}
