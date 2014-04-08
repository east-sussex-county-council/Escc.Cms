using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using EsccWebTeam.Data.ActiveDirectory;
using EsccWebTeam.Data.Web;
using Microsoft.ContentManagement.Publishing.Extensions.Placeholders;
using Microsoft.ContentManagement.WebControls;
using System.Collections.Generic;

namespace EsccWebTeam.Cms.Placeholders
{
    /// <summary>
    /// Image placeholder which checks the type of the image
    /// </summary>
    [ValidationProperty("ImageUrl")]
    public class XhtmlImagePlaceholderControl : SingleImagePlaceholderControl
    {
        private bool renderContainerElement;
        private HtmlTextWriterTag elementName;
        private HtmlImage image;
        private HtmlAnchor link;
        private string associatedMapId;

        /// <summary>
        /// Gets or sets the XHTML id for an associated image map
        /// </summary>
        public string AssociatedMapId
        {
            get
            {
                return this.associatedMapId;
            }
            set
            {
                this.associatedMapId = value;
            }
        }

        /// <summary>
        /// Gets whether anything has been saved in this placeholder
        /// </summary>
        public bool HasContent
        {
            get
            {
                ImagePlaceholder ph = this.BoundPlaceholder as ImagePlaceholder;
                return (ph != null && ph.Src.Length > 0);
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
                    value == HtmlTextWriterTag.Span
                    )
                {
                    this.elementName = value;
                }
                else
                {
                    throw new ApplicationException(this.GetType().ToString() + " can only use the following elements: div, span");
                }
            }
        }

        /// <summary>
        /// Image placeholder which checks the type of the image
        /// </summary>
        public XhtmlImagePlaceholderControl()
        {
            this.renderContainerElement = false;
            this.elementName = HtmlTextWriterTag.Span;
        }


        /// <summary>
        /// Present as a standard Image
        /// </summary>
        /// <param name="presentationContainer"></param>
        protected override void CreatePresentationChildControls(BaseModeContainer presentationContainer)
        {
            this.image = new HtmlImage();

            ImagePlaceholder ph = this.BoundPlaceholder as ImagePlaceholder;
            if (ph != null && ph.Href != null && ph.Href.Length > 0)
            {
                this.link = new HtmlAnchor();
                this.link.Controls.Add(this.image);
                presentationContainer.Controls.Add(this.link);
            }
            else
            {
                presentationContainer.Controls.Add(this.image);
            }
        }

        /// <summary>
        /// Create authoring controls, but only if the user has permission to edit images in the CMS
        /// </summary>
        /// <param name="authoringContainer"></param>
        protected override void CreateAuthoringChildControls(BaseModeContainer authoringContainer)
        {
            if (UserHasPermissionToEdit())
            {
                base.CreateAuthoringChildControls(authoringContainer);
            }
            else
            {
                CreatePresentationChildControls(authoringContainer);
            }
        }

        /// <summary>
        /// Load the image into the authoring controls, but only if the user has permission to edit images in the CMS
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForAuthoring(PlaceholderControlEventArgs e)
        {
            if (UserHasPermissionToEdit())
            {
                base.LoadPlaceholderContentForAuthoring(e);
            }
            else
            {
                LoadPlaceholderContentForPresentation(e);
            }
        }

        /// <summary>
        /// Set image properties from properties of base placeholder
        /// </summary>
        /// <param name="e"></param>
        protected override void LoadPlaceholderContentForPresentation(PlaceholderControlEventArgs e)
        {
            ImagePlaceholder ph = this.BoundPlaceholder as ImagePlaceholder;
            if (ph != null)
            {
                if (ph.Src.Length == 0 || ph.Alt.Length == 0)
                {
                    this.Visible = false;
                }
                else
                {
                    if (this.CssClass.Length > 0)
                    {
                        if (this.link != null)
                        {
                            this.link.Attributes["class"] = this.CssClass;
                        }
                        else
                        {
                            this.image.Attributes["class"] = this.CssClass;
                        }
                    }
                    if (this.link != null)
                    {
                        var href = ph.Href;
                        if (href.Contains("/elibrary/go.aspx"))
                        {
                            var parsedUri = Iri.MakeAbsolute(new Uri(href, UriKind.RelativeOrAbsolute));
                            var queryString = Iri.SplitQueryString(parsedUri.Query);
                            href = CmsUtilities.RewriteElibraryLink(queryString);
                        }
                        this.link.HRef = HttpUtility.HtmlEncode(href);
                    }
                    this.image.Src = ph.Src;
                    this.image.Alt = ph.Alt;

                    // Add image map reference if set
                    if (this.associatedMapId != null && this.associatedMapId.Length > 0)
                    {
                        this.image.Attributes["usemap"] = "#" + this.associatedMapId;
                    }
                }
            }
            else this.Visible = false;

            // If we're in edit mode but the user can't edit images, we just show them the image. 
            // But if there is no image selected we need to show some indication that the placeholder is there,
            // otherwise the layout of some templates doesn't make sense. Use the default image that comes with CMS.
            if (!this.Visible && CmsUtilities.IsEditing && !UserHasPermissionToEdit())
            {
                this.image.Src = "~/CMS/WebAuthor/Client/PlaceholderControlSupport/Images/InsertImageHere.gif";
                this.image.Alt = "An image can go here, but you do not have permission to add one";
                this.Visible = true;
            }
        }


        /// <summary>
        /// Fix links as they're being saved in the image placeholder
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSavingContent(PlaceholderControlSavingEventArgs e)
        {
            if (this.NavigateUrl != null && this.NavigateUrl.Length > 0)
            {
                this.NavigateUrl = CmsUtilities.FixHostHeaderLinks(this.NavigateUrl);
                this.NavigateUrl = this.NavigateUrl.Replace("http://esccwebsite", String.Empty);
                this.NavigateUrl = this.NavigateUrl.Replace("http://webcontent", String.Empty);
            }


            // Support validation of page
            Page.Validate();
            if (!Page.IsValid)
            {
                e.Cancel = true;
            }
            else
            {
                base.OnSavingContent(e);
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        /// <nodoc/>
        /// <remarks>TAGGED_AS_NODOC</remarks>
        protected override void SavePlaceholderContent(PlaceholderControlSaveEventArgs e)
        {
            if (UserHasPermissionToEdit())
            {
                // Prevent saving if the control is not visible. It will always wipe out the data otherwise. 
                // Can't just test .Visible though, because that can be true even when an ancestor control 
                // is invisible, effectively making this invisible too.
                //
                // Important to get this right because it allows a technique where you have two copies of the
                // placeholder on the page which show up in different circumstances. Without this, if the 
                // second one is the hidden one, it overwrites the content from the first one with an empty string.
                Control checkControl = this;
                bool placeholderIsVisible = true;

                while (placeholderIsVisible && checkControl != null)
                {
                    placeholderIsVisible = placeholderIsVisible && checkControl.Visible;
                    checkControl = checkControl.Parent;
                }

                if (placeholderIsVisible) base.SavePlaceholderContent(e);
            }
        }

        /// <summary>
        /// Checks whether the current user has permission to edit images in CMS
        /// </summary>
        /// <returns></returns>
        private static bool UserHasPermissionToEdit()
        {
            var config = ConfigurationManager.GetSection("EsccWebTeam.Cms/SecuritySettings") as NameValueCollection;
            if (config != null && !String.IsNullOrEmpty(config["PermissionToEditImages"]))
            {
                var hasPermission = WebUserPermissions.UserIsInGroup(config["PermissionToEditImages"]);
                if (!hasPermission)
                {
                    var users = new List<string>(config["PermissionToEditImages"].ToUpperInvariant().Split(';'));
                    hasPermission = users.Contains(HttpContext.Current.Request.LogonUserIdentity.Name.ToUpperInvariant());
                }
                return hasPermission;
            }
            return true;
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
