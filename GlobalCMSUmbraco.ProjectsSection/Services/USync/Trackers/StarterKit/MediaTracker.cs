using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.PropertyEditors.ValueConverters;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    [Weight(-100)]
    public class MediaTracker : ContentBaseTracker<IMedia>
    {
        private readonly JsonSerializerSettings _serializerSettings;
        private const string MediaFolderNameFormat = "/media/{0}{1}"; //0: original folder name, 1: projectCode

        public MediaTracker(ISyncSerializer<IMedia> serializer) : base(serializer)
        {
            _serializerSettings = new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                FloatParseHandling = FloatParseHandling.Decimal
            };
        }

        public override List<TrackingItem> TrackingItems
        {
            get
            {
                var items = base.TrackingItems;
                items.Add(TrackingItem.Single("umbracoFile", "/Properties/umbracoFile/Value"));
                return items;
            }
        }

        protected override uSyncChange CompareElement(TrackingItem item, XElement targetNode, XElement sourceNode, SyncSerializerOptions syncSerializerOptions)
        {
            var projectCode = syncSerializerOptions.Settings["ProjectCode"];

            switch (item.Name)
            {
                case "umbracoFile":
                    var contentType = GetContentType(targetNode);
                    if (!contentType.Equals("Folder"))
                    {
                        targetNode.SetValue(UpdatePath(targetNode.Value, projectCode));
                    }
                    break;
            }

            return base.CompareElement(item, targetNode, sourceNode, syncSerializerOptions);
        }

        private string UpdatePath(string value, string projectCode)
        {
            var imageCropper = value.DetectIsJson() ? GetImageCropperValue(value) : null;
            var imageSrc = imageCropper != null ? imageCropper.Src : value;
            
            var fileParts = imageSrc.TrimStart("/media/").Split('/');
            fileParts[0] = string.Format(MediaFolderNameFormat, fileParts[0], projectCode.ToLower());
            var newFilePath = string.Join("/", fileParts);

            if (imageCropper == null) 
                return newFilePath;

            imageCropper.Src = newFilePath;
            return JsonConvert.SerializeObject(imageCropper);
        }

        private static string GetContentType(XElement targetNode)
        {
            return targetNode.XPathSelectElement("/Info/ContentType")?.Value ?? string.Empty;
        }

        private ImageCropperValue GetImageCropperValue(string value)
        {
            return JsonConvert.DeserializeObject<ImageCropperValue>(value, _serializerSettings);
        }
    }
}