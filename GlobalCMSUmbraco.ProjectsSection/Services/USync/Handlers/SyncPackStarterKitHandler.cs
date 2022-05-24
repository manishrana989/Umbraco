using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GlobalCMSUmbraco.ProjectsSection.Extensions;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using uSync.Expansions.Core;
using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Handlers
{
    [HideFromTypeFinder]
    public class SyncPackStarterKitHandler : uSyncHandlerWrapper
    {
        private readonly AppCaches caches;

        public SyncPackStarterKitHandler(ISyncExtendedHandler innerHandler, ISyncItemFactory syncItemFactory, AppCaches caches) : base(innerHandler, syncItemFactory)
        {
            this.caches = caches;
        }

        public override IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions importOptions)
        {
            if (InnerHandler is ISyncItemHandler)
            {
                var serializerOptions = settings.GetSerializerOptions(importOptions);
                serializerOptions.Settings["ProjectCode"] = GetProjectCode(filename);

                // pass in a copy of the xml, then make and compare changes
                var original = new XElement(node);
                var changes = GetChanges(node, original, serializerOptions).ToList();
                if (changes.Any(c => c.Change > ChangeDetailType.NoChange))
                {
                    // write the updated file with the new key
                    node.Save(filename);
                    var directory  = Path.GetDirectoryName(filename);
                    if (directory != null)
                    {
                        // moving may not actually be necessary
                        var newFilename = Path.Combine(directory, $"{node.GetKey()}.config");
                        File.Move(filename, newFilename);
                        filename = newFilename;
                    }
                }

                // TODO: is starter kit always a create change type? 
                var action = GetSyncAction(ChangeType.Create, node.GetAlias(), node.GetAlias(), filename);
                action.key = node.GetKey();
                action.FileName = filename;
                action.Details = changes;
                action.Message = $"{action.Change}";

                return action.AsEnumerableOfOne();
            }

            var actions = ReportElement(node);
            return actions;
        }

        private string GetProjectCode(string filename)
        {
            var xmlFile = new FileInfo(filename);
            var handlerFolder = uSyncPath.SyncPackHandlerFolder("", InnerHandler.DefaultFolder);
            var importFolder = xmlFile.DirectoryName.TrimEnd(handlerFolder);
            var importId = new DirectoryInfo(importFolder).Name;

            return caches.RequestCache.Get($"{importId}_ProjectCode") as string;
        }

        private uSyncAction GetSyncAction(ChangeType changeType, string name, string message, string filename)
        {
            var action = uSyncAction.SetAction(true, name, ItemType, changeType, message, null, filename);
            action.HandlerAlias = Alias;

            return action;
        }
    }
}
