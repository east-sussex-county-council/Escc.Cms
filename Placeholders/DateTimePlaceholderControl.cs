using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using eastsussexgovuk.webservices.TextXhtml.HouseStyle;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// CMS placeholder for editing a date and time
    /// </summary>
    public class DateTimePlaceholderControl : BasePlaceholderControl
    {
        #region Fields

        private DropDownList dayEdit;
        private DropDownList monthEdit;
        private DropDownList yearEdit;
        private DropDownList hourEdit;
        private DropDownList minEdit;
        private LiteralControl contentDisplay;
        private LiteralControl on;
        private bool renderContainerElement;
        private HtmlTextWriterTag elementName;
        private int firstYear = DateTime.Now.Year + 1;
        private int lastYear = DateTime.Now.AddYears(-10).Year;
        private int defaultHour = 0;
        private int defaultMinutes = 0;
        private int defaultYear = DateTime.Now.Year;
        private bool defaultToBlank;

        private DateTime selectedTime;
        //private bool authoringRenderDate;
        private bool presentationRenderDate;
        private bool authoringRenderTime;
        private bool presentationRenderTime;
        private bool required = true;

        /// <summary>
        /// Use a blank date as the default value (not possible if <seealso cref="Required"/> is set to <c>true</c>)
        /// </summary>
        /// <value><c>true</c> if default is a blank date; otherwise, <c>false</c>.</value>
        public bool DefaultToBlank
        {
            get { return defaultToBlank; }
            set { defaultToBlank = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DateTimePlaceholderControl"/> is required.
        /// </summary>
        /// <value><c>true</c> if required; otherwise, <c>false</c>.</value>
        public bool Required
        {
            get
            {
                return this.required;
            }
            set
            {
                this.required = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to write out the time in presentation mode
        /// </summary>
        public bool PresentationRenderTime
        {
            get
            {
                return this.presentationRenderTime;
            }
            set
            {
                this.presentationRenderTime = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to ask for the time in authoring mode
        /// </summary>
        public bool AuthoringRenderTime
        {
            get
            {
                return this.authoringRenderTime;
            }
            set
            {
                this.authoringRenderTime = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to write out the date in presentation mode
        /// </summary>
        public bool PresentationRenderDate
        {
            get
            {
                return this.presentationRenderDate;
            }
            set
            {
                this.presentationRenderDate = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to ask for the date in authoring mode
        /// </summary>
        public bool AuthoringRenderDate
        {
            get
            {
                throw new NotImplementedException("AuthoringRenderDate isn't done yet - feel free to implement it");
                //return this.authoringRenderDate;
            }
            set
            {
                throw new NotImplementedException("AuthoringRenderDate isn't done yet - feel free to implement it");
                //this.authoringRenderDate = value;
            }
        }

        /// <summary>
        /// Gets or sets the year which should be selected by default
        /// </summary>
        public int DefaultYear
        {
            get
            {
                return this.defaultYear;
            }
            set
            {
                this.defaultYear = value;
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the minutes which should be selected by default
        /// </summary>
        public int DefaultMinutes
        {
            get
            {
                return this.defaultMinutes;
            }
            set
            {
                this.defaultMinutes = value;
            }
        }

        /// <summary>
        /// Gets or sets the hour which should be selected by default
        /// </summary>
        public int DefaultHour
        {
            get
            {
                return this.defaultHour;
            }
            set
            {
                this.defaultHour = value;
            }
        }

        /// <summary>
        /// Gets or sets the first year to show in the year DropDownList
        /// </summary>
        public int FirstYear
        {
            get { return this.firstYear; }
            set
            {
                this.firstYear = value;
                this.GenerateYearOptions();
            }
        }

        /// <summary>
        /// Gets or sets the last year to show in the year DropDownList
        /// </summary>
        public int LastYear
        {
            get { return this.lastYear; }
            set
            {
                this.lastYear = value;
                this.GenerateYearOptions();
            }
        }

        #region Constructors

        /// <summary>
        /// CMS placeholder for editing a single line of unformatted text
        /// </summary>
        public DateTimePlaceholderControl()
        {
            this.renderContainerElement = false;
            this.presentationRenderDate = true;
            this.presentationRenderTime = true;
            //this.authoringRenderDate = true;
            this.authoringRenderTime = true;
            this.elementName = HtmlTextWriterTag.Span;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a series of dropdown lists to edit the date and time
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            string displayText;

            // populate day dropdown
            this.dayEdit = new DropDownList();
            this.dayEdit.ID = "Day";

            if (!this.required) dayEdit.Items.Add(new ListItem());

            for (short i = 1; i <= 31; i++)
            {
                ListItem item = new ListItem(i.ToString(), i.ToString());

                // select default value
                if (i == DateTime.Now.Day && (!this.defaultToBlank || this.required))
                {
                    if (dayEdit.SelectedItem != null) dayEdit.SelectedItem.Selected = false;
                    item.Selected = true;
                }

                dayEdit.Items.Add(item);
            }

            // populate month names dropdown
            this.monthEdit = new DropDownList();
            string[] monthNames = System.Globalization.DateTimeFormatInfo.CurrentInfo.MonthNames;
            int monthVal;

            if (!this.required) monthEdit.Items.Add(new ListItem());

            for (int i = 0; i < monthNames.Length; i++)
            {
                if (monthNames[i].Length > 0)
                {
                    monthVal = i + 1;
                    ListItem item = new ListItem(monthNames[i], monthVal.ToString());

                    // select default value
                    if ((i + 1) == DateTime.Now.Month && (!this.defaultToBlank || this.required))
                    {
                        if (monthEdit.SelectedItem != null) monthEdit.SelectedItem.Selected = false;
                        item.Selected = true;
                    }

                    monthEdit.Items.Add(item);
                }
            }


            // populate year dropdowns
            this.yearEdit = new DropDownList();
            this.GenerateYearOptions();


            // populate hours dropdown
            this.hourEdit = new DropDownList();

            if (!this.required) this.hourEdit.Items.Add(new ListItem());

            for (short i = 0; i <= 23; i++)
            {
                displayText = i.ToString();
                if (i < 10) displayText = "0" + displayText;

                ListItem item = new ListItem(displayText, i.ToString());

                // select default value
                if (i == this.defaultHour && (!this.defaultToBlank || this.required))
                {
                    if (hourEdit.SelectedItem != null) hourEdit.SelectedItem.Selected = false;
                    item.Selected = true;
                }

                hourEdit.Items.Add(item);
            }

            // populate minutes dropdown
            this.minEdit = new DropDownList();

            if (!this.required) this.minEdit.Items.Add(new ListItem());

            for (short i = 0; i <= 59; i++)
            {
                displayText = i.ToString();
                if (i < 10) displayText = "0" + displayText;

                ListItem item = new ListItem(displayText, i.ToString());

                // select default value
                if (i == this.defaultMinutes && (!this.defaultToBlank || this.required))
                {
                    if (minEdit.SelectedItem != null) minEdit.SelectedItem.Selected = false;
                    item.Selected = true;
                }

                minEdit.Items.Add(item);
            }

            // create "on" text
            this.on = new LiteralControl(" on ");

            authoringContainer.Controls.Add(this.hourEdit);
            authoringContainer.Controls.Add(this.minEdit);
            authoringContainer.Controls.Add(this.on);
            authoringContainer.Controls.Add(this.dayEdit);
            authoringContainer.Controls.Add(this.monthEdit);
            authoringContainer.Controls.Add(this.yearEdit);
        }

        /// <summary>
        /// Whenever range of years changes, we need to regenerate the options in the dropdown list
        /// </summary>
        private void GenerateYearOptions()
        {
            WebAuthorContext ctx = WebAuthorContext.Current;

            if ((ctx.Mode == WebAuthorContextMode.AuthoringNew || ctx.Mode == WebAuthorContextMode.AuthoringReedit) && this.yearEdit != null)
            {

                this.yearEdit.Items.Clear();

                if (!this.required) this.yearEdit.Items.Add(new ListItem());

                if (this.firstYear <= this.lastYear)
                {
                    for (int i = firstYear; i <= this.lastYear; i++)
                    {
                        ListItem item = new ListItem(i.ToString(), i.ToString());

                        // select default value
                        if (i == this.defaultYear && (!this.defaultToBlank || this.required))
                        {
                            if (this.yearEdit.SelectedItem != null) this.yearEdit.SelectedItem.Selected = false;
                            item.Selected = true;
                        }

                        this.yearEdit.Items.Add(item);
                    }
                }
                else
                {
                    for (int i = firstYear; i >= this.lastYear; i--)
                    {
                        ListItem item = new ListItem(i.ToString(), i.ToString());

                        // select default value
                        if (i == this.defaultYear && (!this.defaultToBlank || this.required))
                        {
                            if (this.yearEdit.SelectedItem != null) this.yearEdit.SelectedItem.Selected = false;
                            item.Selected = true;
                        }

                        this.yearEdit.Items.Add(item);
                    }
                }

                // Reselect any date that was previously selected
                //			this.SelectDate();
            }
        }

        /*		/// <summary>
                /// Select a date in the dropdown lists
                /// </summary>
                private void SelectDate()
                {
                    if (this.dateToSelect != DateTime.MinValue)
                    {
                        this.dayList.SelectedItem.Selected = false;
                        ListItem dayToSelect = this.dayList.Items.FindByValue(this.dateToSelect.Day.ToString());
                        if (dayToSelect != null) dayToSelect.Selected = true;

                        this.monthList.SelectedItem.Selected = false;
                        ListItem monthToSelect = this.monthList.Items.FindByValue(this.dateToSelect.Month.ToString());
                        if (monthToSelect != null) monthToSelect.Selected = true;

                        this.yearList.SelectedItem.Selected = false;
                        ListItem yearToSelect = this.yearList.Items.FindByValue(this.dateToSelect.Year.ToString());
                        if (yearToSelect != null) yearToSelect.Selected = true;
                    }
                }
        */
        /// <summary>
        /// Create a logical container to display the text
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            this.contentDisplay = new LiteralControl();
            presentationContainer.Controls.Add(this.contentDisplay);
        }

        /// <summary>
        /// Populate the editing TextBox with either the saved text or the default text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();

            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;

            // This will be the case when a saved page is edited.
            if (ph.XmlAsString.Length > 0)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ph.XmlAsString);

                XmlNode node = xmlDoc.SelectSingleNode("/Text/Day");
                ListItem toSelect = this.dayEdit.Items.FindByValue(node.InnerText);
                if (toSelect != null)
                {
                    foreach (ListItem item in this.dayEdit.Items) item.Selected = false;
                    toSelect.Selected = true;
                }

                node = xmlDoc.SelectSingleNode("/Text/Month");
                toSelect = this.monthEdit.Items.FindByValue(node.InnerText);
                if (toSelect != null)
                {
                    foreach (ListItem item in this.monthEdit.Items) item.Selected = false;
                    toSelect.Selected = true;
                }

                node = xmlDoc.SelectSingleNode("/Text/Year");
                toSelect = this.yearEdit.Items.FindByValue(node.InnerText);
                if (toSelect != null)
                {
                    foreach (ListItem item in this.yearEdit.Items) item.Selected = false;
                    toSelect.Selected = true;
                }

                node = xmlDoc.SelectSingleNode("/Text/Hour");
                toSelect = this.hourEdit.Items.FindByValue(node.InnerText);
                if (toSelect != null)
                {
                    foreach (ListItem item in this.hourEdit.Items) item.Selected = false;
                    toSelect.Selected = true;
                }

                node = xmlDoc.SelectSingleNode("/Text/Minute");
                toSelect = this.minEdit.Items.FindByValue(node.InnerText);
                if (toSelect != null)
                {
                    foreach (ListItem item in this.minEdit.Items) item.Selected = false;
                    toSelect.Selected = true;
                }
            }

            if (!this.authoringRenderTime)
            {
                this.hourEdit.Visible = false;
                this.minEdit.Visible = false;
                this.on.Visible = false;
            }

            this.Visible = true;

        }

        /// <summary>
        /// Populate the logical container with the saved text
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            this.EnsureChildControls();


            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;

            this.selectedTime = DateTimePlaceholderControl.GetValue(ph);

            if (!this.required && this.selectedTime == DateTime.MinValue)
            {
                this.Visible = false;
            }
            else
            {
                if (this.presentationRenderDate && this.presentationRenderTime)
                {
                    this.contentDisplay.Text = DateTimeFormatter.FullBritishDateWithDayAndTime(this.selectedTime);
                }
                else if (this.presentationRenderDate && !this.presentationRenderTime)
                {
                    this.contentDisplay.Text = DateTimeFormatter.FullBritishDateWithDay(this.selectedTime);
                }
                else if (!this.presentationRenderDate && this.presentationRenderTime)
                {
                    this.contentDisplay.Text = DateTimeFormatter.Time(this.selectedTime);
                }
                this.Visible = true;
            }

        }


        /// <summary>
        /// Create a well-formed XML document to save the text
        /// </summary>
        /// <param name="e"></param>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");
            xmlDoc.AppendChild(xmlDec);

            XmlElement rootElement = xmlDoc.CreateElement("Text");
            xmlDoc.AppendChild(rootElement);

            XmlElement dayElement = xmlDoc.CreateElement("Day");
            dayElement.AppendChild(xmlDoc.CreateTextNode(this.dayEdit.SelectedValue));
            rootElement.AppendChild(dayElement);

            XmlElement monthElement = xmlDoc.CreateElement("Month");
            monthElement.AppendChild(xmlDoc.CreateTextNode(this.monthEdit.SelectedValue));
            rootElement.AppendChild(monthElement);

            XmlElement yearElement = xmlDoc.CreateElement("Year");
            yearElement.AppendChild(xmlDoc.CreateTextNode(this.yearEdit.SelectedValue));
            rootElement.AppendChild(yearElement);

            var hour = String.Empty;
            var minute = String.Empty;
            var hasDate = (!String.IsNullOrEmpty(this.dayEdit.SelectedValue) && !String.IsNullOrEmpty(this.monthEdit.SelectedValue) && !String.IsNullOrEmpty(this.yearEdit.SelectedValue));
            if (hasDate)
            {
                if (!String.IsNullOrEmpty(this.hourEdit.SelectedValue))
                {
                    // if hour specified, use that and default minutes to 00 if not supplied
                    hour = this.hourEdit.SelectedValue;
                    minute = String.IsNullOrEmpty(this.minEdit.SelectedValue) ? "00" : this.minEdit.SelectedValue;
                }
                else
                {
                    // if hour missing, use 11.59pm
                    hour = "23";
                    minute = "59";
                }
            }

            XmlElement hourElement = xmlDoc.CreateElement("Hour");
            hourElement.AppendChild(xmlDoc.CreateTextNode(hour));
            rootElement.AppendChild(hourElement);

            XmlElement minElement = xmlDoc.CreateElement("Minute");
            minElement.AppendChild(xmlDoc.CreateTextNode(minute));
            rootElement.AppendChild(minElement);

            ph.XmlAsString = xmlDoc.InnerXml;
        }


        /// <summary>
        /// Get the date and time value from a DateTimePlaceholderControl
        /// </summary>
        /// <param name="ph">An XmlPlaceholder which is bound to a DateTimePlaceholderControl</param>
        /// <returns>The date and time value entered into the control, or DateTime.MinValue</returns>
        public static DateTime GetValue(XmlPlaceholder ph)
        {
            if (ph.XmlAsString.Length == 0) return DateTime.MinValue; // new field added to placeholder definition has no XML on existing pages

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(ph.XmlAsString);

            XmlNode dayNode = xmlDoc.SelectSingleNode("/Text/Day");
            XmlNode monthNode = xmlDoc.SelectSingleNode("/Text/Month");
            XmlNode yearNode = xmlDoc.SelectSingleNode("/Text/Year");
            XmlNode hourNode = xmlDoc.SelectSingleNode("/Text/Hour");
            XmlNode minNode = xmlDoc.SelectSingleNode("/Text/Minute");

            if (dayNode != null && monthNode != null && yearNode != null && hourNode != null && minNode != null)
            {
                if (yearNode.InnerText.Length > 0 && monthNode.InnerText.Length > 0 && dayNode.InnerText.Length > 0 && hourNode.InnerText.Length > 0 && minNode.InnerText.Length > 0)
                {
                    try
                    {
                        return new DateTime(Int32.Parse(yearNode.InnerText), Int32.Parse(monthNode.InnerText), Int32.Parse(dayNode.InnerText), Int32.Parse(hourNode.InnerText), Int32.Parse(minNode.InnerText), 0);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Treat invalid date as no date
                        return DateTime.MinValue;
                    }
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
            else return DateTime.MinValue;
        }

        #endregion

        /// <summary>
        /// Gets whether anything has been saved in this placeholder
        /// </summary>
        public bool HasContent
        {
            get
            {
                XmlPlaceholder ph = this.BoundPlaceholder as XmlPlaceholder;
                return (DateTimePlaceholderControl.GetValue(ph) != DateTime.MinValue);
            }
        }

        /// <summary>
        /// Gets or sets whether to render a containing XHTML element in presentation mode (the default CMS behaviour)
        /// </summary>
        public bool RenderContainerElement
        {
            get
            {
                return this.renderContainerElement;
            }
            set
            {
                this.renderContainerElement = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the XHTML element to use when rendering a container element
        /// </summary>
        public HtmlTextWriterTag ElementName
        {
            get
            {
                return this.elementName;
            }
            set
            {
                if (
                    value == HtmlTextWriterTag.Div ||
                    value == HtmlTextWriterTag.Span ||
                    value == HtmlTextWriterTag.P ||
                    value == HtmlTextWriterTag.Dt ||
                    value == HtmlTextWriterTag.Li ||
                    value == HtmlTextWriterTag.Td ||
                    value == HtmlTextWriterTag.Strong ||
                    value == HtmlTextWriterTag.Em ||
                    value == HtmlTextWriterTag.H1 ||
                    value == HtmlTextWriterTag.H2 ||
                    value == HtmlTextWriterTag.H3 ||
                    value == HtmlTextWriterTag.H4 ||
                    value == HtmlTextWriterTag.H5 ||
                    value == HtmlTextWriterTag.H6
                    )
                {
                    this.elementName = value;
                }
                else
                {
                    throw new ApplicationException("DateTimePlaceholderControl can only use the following elements: div, span, p, td, dt, li, strong, em, h1, h2, h3, h4, h5, h6");
                }
            }
        }

        /// <summary>
        /// Render the beginning of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew ||
                WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit ||
                this.renderContainerElement)
            {
                writer.WriteBeginTag(this.elementName.ToString().ToLower());
                writer.WriteAttribute("id", this.ID);
                if (this.CssClass.Length > 0) writer.WriteAttribute("class", this.CssClass);
                writer.Write(">");
            }
        }

        /// <summary>
        /// Render the end of a container element if requested, or if editing
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(System.Web.UI.HtmlTextWriter writer)
        {
            if (WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringNew ||
                WebAuthorContext.Current.Mode == WebAuthorContextMode.AuthoringReedit ||
                this.renderContainerElement)
            {
                writer.WriteEndTag(this.elementName.ToString().ToLower());
            }
        }

    }
}
