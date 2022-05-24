using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core.Composing;
using uSync8.Core;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    [Weight(-100)]
    public abstract class ContentBaseTracker<TEntity> : SyncXmlTracker<TEntity>, ISyncNodeTracker<TEntity>, IModifyingTracker
    {
        private static string PROTO_CONTENT_TYPE_ALIAS_SUFFIX = "__proto";
        private static string PROTO_CONTENT_TYPE_ALIAS_FORMAT = "__{0}";

        protected ContentBaseTracker(ISyncSerializer<TEntity> serializer) : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>
        {
            TrackingItem.Attribute("Alias", "/", "Alias"),
            TrackingItem.Attribute("NodeName (Default)", "./Info/NodeName", "Default"),
            TrackingItem.Attribute("Published (Default)", "./Info/Published", "Default"),
            TrackingItem.Single("Path", "./Info/Path"),
            TrackingItem.Single("ContentType", "./Info/ContentType"),
            TrackingItem.Attribute("Key", "/", "Key"),
        };

        protected override ChangeType GetChangeType(XElement target, XElement source, SyncSerializerOptions options)
        {
            // all content from starter kits should be 'created'
            return ChangeType.Create;
        }

        protected override uSyncChange CompareAttribute(TrackingItem item, XAttribute targetAttribute,
            XAttribute sourceAttribute, SyncSerializerOptions syncSerializerOptions)
        {
            var projectCode = syncSerializerOptions.Settings["ProjectCode"];

            switch (item.Name)
            {
                case "Key":
                    targetAttribute.SetValue(Guid.NewGuid());
                    break;

                case "Alias":
                case "NodeName (Default)":
                    var rootElement = targetAttribute.Parent?.XPathSelectElement("/");

                    if (IsRootNode(rootElement) && IsValidContentTypeForRootNode(rootElement))
                    {
                        targetAttribute.SetValue(projectCode);
                    }
                    break;

                case "Published (Default)":
                    targetAttribute.SetValue(false);
                    break;
            }

            return base.CompareAttribute(item, targetAttribute, sourceAttribute, syncSerializerOptions);
        }

        protected override uSyncChange CompareElement(TrackingItem item, XElement targetNode, XElement sourceNode,
            SyncSerializerOptions syncSerializerOptions)
        {
            var projectCode = syncSerializerOptions.Settings["ProjectCode"];

            switch (item.Name)
            {
                case "Path":
                    var path = GetProjectScopedPath( targetNode.Value.TrimStart('/'), projectCode, '/');
                    targetNode.SetValue($"/{path}");
                    break;

                case "ContentType":
                    var contentType = GetProjectSpecificValue(syncSerializerOptions, targetNode.Value, "__proto", "__{0}");
                    targetNode.SetValue(contentType);
                    break;
            }

            return base.CompareElement(item, targetNode, sourceNode, syncSerializerOptions);
        }

        private static bool IsRootNode(XElement target)
        {
            var parent = target?.XPathSelectElement("/Info/Parent");
            return parent?.Attribute("Key")?.Value.Equals("00000000-0000-0000-0000-000000000000") == true;
        }

        private static bool IsValidContentTypeForRootNode(XElement target)
        {
            var contentTypeNode = target?.XPathSelectElement("Info/ContentType");
            return contentTypeNode?.Value.EndsWith(PROTO_CONTENT_TYPE_ALIAS_SUFFIX) == true;
        }
    }
}