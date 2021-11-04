using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using PictureRenderer.Profiles;

namespace PictureRenderer.Optimizely
{
    public static class XhtmlStringExtensions
    {
        public static XhtmlString RenderImageAsPicture(this XhtmlString xhtmlString, int maxWidth = 1024)
        {
            var ctxModeResolver = ServiceLocator.Current.GetInstance<EPiServer.Web.IContextModeResolver>();
            if (ctxModeResolver.CurrentMode == ContextMode.Edit)
            {
                return xhtmlString;
            }

            //todo: extend regex so that it doesn't match img element inside picture element (even though that would be a very rare edge case). https://www.regular-expressions.info/lookaround.html
            var processedText = Regex.Replace(xhtmlString.ToInternalString(), "(<img.*?>)", m => GetPictureFromImg(m.Groups[1].Value, maxWidth));

            return new XhtmlString(processedText);
        }

        private static string GetPictureFromImg(string imgElement, int maxWidth)
        {
            var src = Regex.Match(imgElement, "src=\"(.*?)\"").Groups[1].Value;
            var alt = Regex.Match(imgElement, "alt=\"(.*?)\"").Groups[1].Value;
            var cssClass = Regex.Match(imgElement, "class=\"(.*?)\"").Groups[1].Value;
            int.TryParse(Regex.Match(imgElement, "width=\"(.*?)\"").Groups[1].Value, out var width);
            int.TryParse(Regex.Match(imgElement, "height=\"(.*?)\"").Groups[1].Value, out var height);

            //image width must not exceed max width
            var actualWidth = width > maxWidth ? maxWidth : width;

            var imgUrl = UrlResolver.Current.GetUrl(src);

            var tinyMcePictureProfile = new ImageSharpProfile()
            {
                SrcSetWidths = new[] { actualWidth },
                Sizes = new[] { $"{actualWidth}px" },
                AspectRatio = Math.Round((double)width / height, 3),
            };

            return PictureRenderer.Picture.Render(imgUrl, tinyMcePictureProfile, alt, cssClass);
        }

        //private static string AddResizeToImg(string imgElement)
        //{
        //    var width = Regex.Match(imgElement, "width=\"(.*?)\"").Groups[1];
        //    var height = Regex.Match(imgElement, "height=\"(.*?)\"").Groups[1];

        //    return Regex.Replace(imgElement, "(.aspx)", $"$1?width={width}&height={height}&quality=80");
        //}
    }
}
