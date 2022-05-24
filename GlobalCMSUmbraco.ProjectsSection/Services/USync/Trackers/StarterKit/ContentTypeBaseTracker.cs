using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    public abstract class ContentTypeBaseTracker<TItem> : SyncXmlTracker<TItem>, ISyncNodeTracker<TItem>
    {
        private static string PROTO_ALIAS_SUFFIX = "__proto";
        private static string PROTO_ALIAS_SUFFIX_FORMAT = "__{0}";

        private static string PROTO_NAME_SUFFIX = " (proto)";
        private static string PROTO_NAME_REPLACEMENT = "";        // if want name at end then replace with " ({0})"

        private static string PROTO_PATH_PREFIX = "Proto";
        private static string PROTO_PATH_REPLACEMENT = "Projects/{0}";

        protected ContentTypeBaseTracker(ISyncSerializer<TItem> serializer) : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>
        {
            TrackingItem.Attribute("Key", "/", "Key"),
            TrackingItem.Single("Name", "/Info/Name"),
            TrackingItem.Attribute("Alias", "/", "Alias"),
            TrackingItem.Single("Allowed at root", "/Info/AllowAtRoot"),
            TrackingItem.Single("Folder", "/Info/Folder"),
            TrackingItem.Many("Property", "/GenericProperties/GenericProperty", "Key", "Name")
        };

        protected override ChangeType GetChangeType(XElement target, XElement source, SyncSerializerOptions options)
        {
            if (target.GetAlias().EndsWith(PROTO_ALIAS_SUFFIX))
            {
                return ChangeType.Create;
            }

            return base.GetChangeType(target, source, options);
        }

        protected override uSyncChange CompareElement(TrackingItem item, XElement targetNode, XElement sourceNode, SyncSerializerOptions syncSerializerOptions)
        {
            switch (item.Name)
            {
                case "Name":
                    var newName = GetProjectSpecificValue(syncSerializerOptions, targetNode.Value, PROTO_NAME_SUFFIX, PROTO_NAME_REPLACEMENT);
                    targetNode.SetValue(newName);
                    break;

                case "Allowed at root":
                    targetNode.SetValue("False");
                    break;

                case "Folder":
                    var newFolder = GetProjectSpecificValue(syncSerializerOptions, targetNode.Value, PROTO_PATH_PREFIX, PROTO_PATH_REPLACEMENT);
                    targetNode.SetValue(newFolder);
                    break;
            }

            return base.CompareElement(item, targetNode, sourceNode, syncSerializerOptions);
        }

        protected override uSyncChange CompareAttribute(TrackingItem item, XAttribute targetAttribute, XAttribute sourceAttribute, SyncSerializerOptions syncSerializerOptions)
        {
            switch (item.Name)
            {
                case "Alias":
                    var alias = GetProjectSpecificValue(syncSerializerOptions, targetAttribute.Value, PROTO_ALIAS_SUFFIX, PROTO_ALIAS_SUFFIX_FORMAT);
                    targetAttribute.SetValue(alias);
                    break;
                    
                case "Key":
                    var aliasValue = targetAttribute.Parent.GetAlias();
                    if (aliasValue.EndsWith(PROTO_ALIAS_SUFFIX)){
                        targetAttribute.SetValue(Guid.NewGuid());
                    }
                    break;
            }

            return base.CompareAttribute(item, targetAttribute, sourceAttribute, syncSerializerOptions);
        }

        protected override IEnumerable<uSyncChange> TrackMultipleKeyedItems(TrackingItem trackingItem, XElement target,
            XElement source, TrackingDirection direction, SyncSerializerOptions syncSerializerOptions)
        {
            if (!trackingItem.Name.Equals("Property"))
                return base.TrackMultipleKeyedItems(trackingItem, target, source, direction, syncSerializerOptions);

            // generate new keys for properties
            var properties = target.XPathSelectElements($"{trackingItem.Path}/Key");
            foreach (var propertyKeyElement in properties)
            {
                propertyKeyElement.SetValue(Guid.NewGuid());
            }

            return Enumerable.Empty<uSyncChange>();

        }
    }
}