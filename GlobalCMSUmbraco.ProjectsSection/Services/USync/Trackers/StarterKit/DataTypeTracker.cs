using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core.Models;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    public class DataTypePrototypeTracker : SyncXmlTracker<IDataType>, ISyncNodeTracker<IDataType>, IModifyingTracker
    {
        public DataTypePrototypeTracker(ISyncSerializer<IDataType> serializer) : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>
        {
            TrackingItem.Attribute("Key", "/", "Key"),
            TrackingItem.Single("Name", "./Info/Name"),
            TrackingItem.Single("Folder", "./Info/Folder"),
            TrackingItem.Single("Config", "./Config"),
            TrackingItem.Attribute("Alias", "/", "Alias")
        };

        protected override ChangeType GetChangeType(XElement target, XElement source, SyncSerializerOptions options)
        {
            if (target.GetAlias().EndsWith(" (proto)"))
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
                    var newName = GetProjectSpecificValue(syncSerializerOptions, targetNode.Value, " (proto)", " ({0})");
                    targetNode.SetValue(newName);
                    break;

                case "Folder":
                    var newFolder = GetProjectSpecificValue(syncSerializerOptions, targetNode.Value, "Proto", "Projects/{0}");
                    targetNode.SetValue(newFolder);
                    break;

                case "Config":
                    var config = GetProjectSpecificValue(syncSerializerOptions, targetNode.Value, "__proto", "__{0}");
                    targetNode.SetValue(config);
                    break;
            }

            return base.CompareElement(item, targetNode, sourceNode, syncSerializerOptions);
        }

        protected override uSyncChange CompareAttribute(TrackingItem item, XAttribute targetAttribute,
            XAttribute sourceAttribute, SyncSerializerOptions syncSerializerOptions)
        {
            switch (item.Name)
            {
                case "Key":
                    var aliasValue = targetAttribute.Parent?.XPathSelectElement("/")?.Attribute("Alias")?.Value;
                    if (aliasValue?.EndsWith(" (proto)") == true){
                        targetAttribute.SetValue(Guid.NewGuid());
                    }
                    break;

                case "Alias":
                    var newAlias = GetProjectSpecificValue(syncSerializerOptions, targetAttribute.Value, " (proto)", " ({0})");
                    targetAttribute.SetValue(newAlias);
                    break;
            }

            return base.CompareAttribute(item, targetAttribute, sourceAttribute, syncSerializerOptions);
        }
    }
}