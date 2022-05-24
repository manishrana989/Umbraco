using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using uSync8.Core;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    [Weight(-100)]
    public class DictionaryItemPrototypeTracker : SyncXmlTracker<IDictionaryItem>, ISyncNodeTracker<IDictionaryItem>, IModifyingTracker
    {
        public DictionaryItemPrototypeTracker(ISyncSerializer<IDictionaryItem> serializer) : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>
        {
            TrackingItem.Attribute("Key", "/", "Key"),
            TrackingItem.Attribute("Alias", "/", "Alias"),
            TrackingItem.Single("Parent", "./Info/Parent")
        };

        protected override ChangeType GetChangeType(XElement target, XElement source, SyncSerializerOptions options)
        {
            // TODO: if item path doesn't start with project code
            return ChangeType.Create;
        }

        protected override uSyncChange CompareAttribute(TrackingItem item, XAttribute targetAttribute,
            XAttribute sourceAttribute, SyncSerializerOptions syncSerializerOptions)
        {
            var projectCode = syncSerializerOptions.Settings["ProjectCode"];

            switch (item.Name)
            {
                case "Alias":
                    targetAttribute.SetValue(GetProjectScopedPath(targetAttribute.Value, projectCode, '.'));
                    break;
                    
                case "Key":
                    targetAttribute.SetValue(Guid.NewGuid());
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
                case "Parent":
                    targetNode.SetValue(GetProjectScopedPath(targetNode.Value, projectCode, '.'));
                    break;
            }

            return base.CompareElement(item, targetNode, sourceNode, syncSerializerOptions);
        }
    }
}