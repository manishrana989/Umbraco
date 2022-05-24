using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers
{
    public abstract class SyncXmlTracker<TObject>
    {
        protected ISyncSerializer<TObject> Serializer {get; }

        private const string seperator = "/";

        protected SyncXmlTracker(ISyncSerializer<TObject> serializer)
        {
            this.Serializer = serializer;
        }

        public IList<TrackingItem> Items { get; set; }

        public IEnumerable<uSyncChange> GetChanges(XElement target)
            => GetChanges(target, new SyncSerializerOptions());

        public IEnumerable<uSyncChange> GetChanges(XElement target, SyncSerializerOptions options)
        {
            var item = Serializer.FindItem(target);
            if (item != null)
            {
                var attempt = SerializeItem(item, options);
                if (attempt.Success)
                    return GetChanges(target, attempt.Item, options);

            }

            return GetChanges(target, XElement.Parse("<blank/>"), options);
        }

        private SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
        {
            if (Serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.Serialize(item, options);

#pragma warning disable CS0618 // Type or member is obsolete
            return Serializer.Serialize(item);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public virtual IEnumerable<uSyncChange> GetChanges(XElement target, XElement source, SyncSerializerOptions options)
        {
            if (Serializer.IsEmpty(target))
                return GetEmptyFileChange(target, source).AsEnumerableOfOne();

            if (!Serializer.IsValid(target))
                return uSyncChange.Error("", "Invalid File", target.Name.LocalName).AsEnumerableOfOne();

            var changeType = GetChangeType(target, source, options);
            if (changeType == ChangeType.NoChange)
                return uSyncChange.NoChange("", target.GetAlias()).AsEnumerableOfOne();

            return CalculateDiffrences(target, source, options);
        }

        protected uSyncChange GetEmptyFileChange(XElement target, XElement source)
        {
            if (source == null) return uSyncChange.NoChange("", target.GetAlias());

            var action = target.Attribute("Change").ValueOrDefault(SyncActionType.None);
            switch (action)
            {
                case SyncActionType.Delete:
                    return uSyncChange.Delete(target.GetAlias(), "Delete", target.GetAlias());
                case SyncActionType.Rename:
                    return uSyncChange.Update(target.GetAlias(), "Rename", target.GetAlias(), "New name");
                default:
                    return uSyncChange.NoChange("", target.GetAlias());
            }
        }

        protected virtual ChangeType GetChangeType(XElement target, XElement source, SyncSerializerOptions options)
        {
            switch (Serializer)
            {
                case ISyncNodeSerializer<TObject> nodeSerializer:
                    return nodeSerializer.IsCurrent(target, source, options);
                case ISyncOptionsSerializer<TObject> optionSerializer:
                    return optionSerializer.IsCurrent(target, options);
                default:
                    return Serializer.IsCurrent(target);
            }
        }


        /// <summary>
        ///  actually kicks off here, if you have two xml files that are diffrent. 
        /// </summary>
        protected IEnumerable<uSyncChange> CalculateDiffrences(XElement target, XElement source,
            SyncSerializerOptions syncSerializerOptions)
        {
            var changes = new List<uSyncChange>();

            foreach (var trackingItem in TrackingItems)
            {
                var differences = CalculateDifference(trackingItem, target, source, syncSerializerOptions);
                changes.AddRange(differences);
            }

            return changes;
        }

        protected virtual IEnumerable<uSyncChange> CalculateDifference(TrackingItem trackingItem, XElement target, XElement source, SyncSerializerOptions syncSerializerOptions)
        {
            var changes = new List<uSyncChange>();
            if (trackingItem.SingleItem)
            {
                var targetToSourceChanges = TrackSingleItem(trackingItem, target, source, TrackingDirection.TargetToSource, syncSerializerOptions);
                var sourceToTargetChanges = TrackSingleItem(trackingItem, source, target, TrackingDirection.SourceToTarget, syncSerializerOptions);

                changes.AddNotNull(targetToSourceChanges);
                changes.AddNotNull(sourceToTargetChanges);
            }
            else
            {
                var targetToSourceChanges = TrackMultipleKeyedItems(trackingItem, target, source, TrackingDirection.TargetToSource, syncSerializerOptions);
                var sourceToTargetChanges = TrackMultipleKeyedItems(trackingItem, source, target, TrackingDirection.SourceToTarget, syncSerializerOptions);

                changes.AddRange(targetToSourceChanges);
                changes.AddRange(sourceToTargetChanges);
            }

            return changes;
        }

        protected virtual uSyncChange TrackSingleItem(TrackingItem item, XElement target, XElement source,
            TrackingDirection direction, SyncSerializerOptions syncSerializerOptions)
        {
            var sourceNode = item.Path == "/" ? source : source.XPathSelectElement(item.Path);
            var targetNode = item.Path == "/" ? target : target.XPathSelectElement(item.Path);

            if (sourceNode != null)
            {
                if (targetNode == null)
                {
                    // value is missing, this is a delete or create depending on compare direction
                    return AddMissingChange(item.Path, item.Name, sourceNode.ValueOrDefault(string.Empty), direction);
                }

                // only track updates when tracking target to source. 
                if (direction == TrackingDirection.TargetToSource)
                {
                    if (item.HasAttributes())
                    {
                        var targetAttribute = targetNode.Attribute(item.AttributeKey);
                        var sourceAttribute = sourceNode.Attribute(item.AttributeKey);
                        
                        var change = CompareAttribute(item, targetAttribute, sourceAttribute, syncSerializerOptions);
                        return change;
                    }
                    else
                    {
                        var change = CompareElement(item, targetNode, sourceNode, syncSerializerOptions);
                        return change;
                    }
                }
            }

            return null;
        }

        protected virtual uSyncChange CompareAttribute(TrackingItem item, XAttribute targetAttribute,
            XAttribute sourceAttribute, SyncSerializerOptions syncSerializerOptions)
        {
            var targetValue = targetAttribute.ValueOrDefault(string.Empty);
            var sourceValue = sourceAttribute.ValueOrDefault(string.Empty);

            return CompareValue(targetValue, sourceValue, item.Path, item.Name, item.MaskValue);
        }

        protected virtual IEnumerable<uSyncChange> TrackMultipleKeyedItems(TrackingItem trackingItem, XElement target,
            XElement source, TrackingDirection direction, SyncSerializerOptions syncSerializerOptions)
        {
            var changes = new List<uSyncChange>();

            var sourceItems = source.XPathSelectElements(trackingItem.Path);

            foreach (var sourceNode in sourceItems)
            {
                // make the selection path for this item.
                var itemPath = trackingItem.Path.Replace("*", sourceNode.Parent.Name.LocalName) + MakeSelectionPath(sourceNode, trackingItem.Keys);

                var itemName = trackingItem.Name.Replace("*", sourceNode.Parent.Name.LocalName) + 
                               MakeSelectionName(sourceNode, String.IsNullOrWhiteSpace(trackingItem.ValueKey) ? trackingItem.Keys : trackingItem.ValueKey);

                var targetNode = target.XPathSelectElement(itemPath);

                if (targetNode == null)
                {
                    var value = sourceNode.ValueOrDefault(string.Empty);
                    if (!string.IsNullOrEmpty(trackingItem.ValueKey))
                        value = GetKeyValue(sourceNode, trackingItem.ValueKey);

                    // missing, we add either a delete or create - depending on tracking direction
                    var differences = AddMissingChange(trackingItem.Path, itemName, value, direction);
                    changes.AddNotNull(differences);
                }

                // only track updates when tracking target to source. 
                else if (direction == TrackingDirection.TargetToSource)
                {
                    // check the node to see if its an update. 
                    var differences = CompareNode(targetNode, sourceNode, trackingItem.Path, itemName, trackingItem.MaskValue);
                    changes.AddRange(differences);
                }
            }

            return changes;
        }

        private string MakeSelectionPath(XElement node, string keys)
        {
            if (keys == "#") return node.Name.LocalName;
            var selectionPath = "";

            var keyList = keys.ToDelimitedList();

            foreach (var key in keyList)
            {
                var value = GetKeyValue(node, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    selectionPath += $"[{key} = {EscapeXPathString(value)}]";
                }

            }

            return selectionPath.Replace("][", " and ");
        }

        private string EscapeXPathString(string value)
        {
            if (!value.Contains("'"))
                return '\'' + value + '\'';

            if (!value.Contains("\""))
                return '"' + value + '"';

            return "concat('" + value.Replace("'", "',\"'\",'") + "')";
        }


        private string MakeSelectionName(XElement node, string keys)
        {
            var names = new List<string>();
            var keyList = keys.ToDelimitedList();
            foreach (var key in keyList)
            {
                var value = GetKeyValue(node, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    names.Add(value);
                }
            }

            if (names.Count > 0) return $" ({string.Join(" ", names)})";
            return "";
        }

        private string GetKeyValue(XElement node, string key)
        {
            if (key == "#") return node.Name.LocalName;
            if (key.StartsWith("@")) return node.Attribute(key.Substring(1)).ValueOrDefault(string.Empty);
            return node.Element(key).ValueOrDefault(string.Empty);
        }

        protected virtual IEnumerable<uSyncChange> CompareNode(XElement target, XElement source, string path, string name, bool maskValue)
        {
            var changes = new List<uSyncChange>();

            // compare attributes
            foreach (var sourceAttribute in source.Attributes())
            {
                var sourceAttributeName = sourceAttribute.Name.LocalName;

                var targetValue = target.Attribute(sourceAttributeName).ValueOrDefault(string.Empty);
                var sourceValue = sourceAttribute.Value;

                var differences = CompareValue(targetValue, sourceValue, path + $"{seperator}{sourceAttributeName}", $"{name} > {sourceAttributeName}", maskValue);
                changes.AddNotNull(differences);
            }

            if (source.HasElements)
            {
                // compare elements. 
                foreach (var sourceElement in source.Elements())
                {
                    var sourceElementName = sourceElement.Name.LocalName;

                    var targetValue = target.Element(sourceElementName).ValueOrDefault(string.Empty);
                    var sourceValue = sourceElement.Value;

                    var differences = CompareValue(targetValue, sourceValue, path + $"{seperator}{sourceElementName}", $"{name} > {sourceElementName}", maskValue);
                    changes.AddNotNull(differences);
                }
            }
            else
            {
                var targetValue = target.ValueOrDefault(string.Empty);
                var sourceValue = source.ValueOrDefault(string.Empty);

                var differences = CompareValue(targetValue, sourceValue, path, name, maskValue);
                changes.AddNotNull(differences);
            }

            return changes;
        }

        protected virtual uSyncChange CompareElement(TrackingItem item, XElement targetNode, XElement sourceNode,
            SyncSerializerOptions syncSerializerOptions)
        {
            var targetValue = targetNode.ValueOrDefault(string.Empty);
            var sourceValue = sourceNode.ValueOrDefault(string.Empty);

            return CompareValue(targetValue, sourceValue, item.Path, item.Name, item.MaskValue);
        }

        private uSyncChange CompareValue(string target, string source, string path, string name, bool maskValue)
        {
            if (source.DetectIsJson())
            {
                return JsonChange(target, source, path, name, maskValue);
            }
            else
            {
                return StringChange(target, source, path, name, maskValue);
            }
        }

        protected virtual uSyncChange JsonChange(string target, string source, string path, string name, bool maskValue)
        {
            try
            {
                var sourceJson = JsonConvert.DeserializeObject<JToken>(source);
                var targetJson = JsonConvert.DeserializeObject<JToken>(target);

                if (JToken.DeepEquals(sourceJson, targetJson)) return null;

                return uSyncChange.Update(path, name, sourceJson, targetJson);
            }
            catch
            {
                return StringChange(target, source, path, name, maskValue);
            }
        }

        protected virtual uSyncChange StringChange(string target, string source, string path, string name, bool maskValue)
        {
            if (source.Equals(target)) return null;
            return uSyncChange.Update(path, name, maskValue ? "*****" : source, maskValue ? "*****" : target);
        }

        /// <summary>
        ///  Adds a change when a value is missing
        /// </summary>
        /// <remarks>
        ///  Depending on the direction of the comapre this will add a delete (when value is mising from target)
        ///  or a create (when value is missing from source).
        /// </remarks>
        private uSyncChange AddMissingChange(string path, string name, string value, TrackingDirection direction)
        {
            switch (direction)
            {
                case TrackingDirection.TargetToSource:
                    return uSyncChange.Delete(path, name, value);
                case TrackingDirection.SourceToTarget:
                    return uSyncChange.Create(path, name, value);
            }

            return null;
        }

        public virtual List<TrackingItem> TrackingItems { get; }

        protected string GetProjectSpecificValue(SyncSerializerOptions options, string value, string suffix, string replacement)
        {
            if (!options.Settings.TryGetValue("ProjectCode", out var projectCode)) 
                return value;

            return value.Replace(suffix, string.Format(replacement, projectCode));
        }

        protected static string GetProjectScopedPath(string value, string projectCode, char separator)
        {
            // Does this actually need to be checked? If it's level 0, then the split below will still set the correct value
            //var levelAttribute = targetAttribute.Parent?.Attribute("Level");
            //var isRootItem = levelAttribute.ValueOrDefault("-1").Equals("0");
            //if (isRootItem)
            //    return "<PROJECT_CODE>";

            var aliases = value.Split(separator);
            aliases[0] = projectCode;
            return string.Join(separator.ToString(), aliases);
        }
    }
}