using System;
using System.Collections.Generic;
using System.Xml.Linq;
using GlobalCMSUmbraco.ProjectsSection.Extensions;
using Umbraco.Core;
using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Handlers
{
    public abstract class uSyncHandlerWrapper : ISyncExtendedHandler, ISyncItemHandler
    {
        protected ISyncItemFactory SyncItemFactory {get; }
        protected ISyncItemHandler InnerSyncItemHandler { get; }
        protected ISyncExtendedHandler InnerHandler {get; }

        protected uSyncHandlerWrapper(ISyncExtendedHandler innerHandler, ISyncItemFactory syncItemFactory)
        {
            SyncItemFactory = syncItemFactory;
            InnerHandler = innerHandler;
            InnerSyncItemHandler = innerHandler as ISyncItemHandler;
        }

        public virtual void Initialize(HandlerSettings settings)
        {
            InnerHandler.Initialize(settings);
        }

        public virtual IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings settings, SyncUpdateCallback callback)
        {
            return InnerHandler.ExportAll(folder, settings, callback);
        }

        public virtual IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings settings, bool force, SyncUpdateCallback callback)
        {
            return InnerHandler.ImportAll(folder, settings, force, callback);
        }

        public virtual IEnumerable<uSyncAction> Report(string folder, HandlerSettings settings, SyncUpdateCallback callback)
        {
            return InnerHandler.Report(folder, settings, callback);
        }

        public virtual string Alias => InnerHandler.Alias;

        public virtual string Name => InnerHandler.Name;

        public virtual int Priority => InnerHandler.Priority;

        public virtual string DefaultFolder => InnerHandler.DefaultFolder;

        public virtual string Icon => InnerHandler.Icon;

        public virtual Type ItemType => InnerHandler.ItemType;

        public virtual string Group => InnerHandler.Group;

        public virtual string EntityType => InnerHandler.EntityType;

        public virtual string TypeName => InnerHandler.TypeName;

        public virtual bool Enabled
        {
            get => InnerHandler.Enabled;
            set => InnerHandler.Enabled = value;
        }

        public virtual HandlerSettings DefaultConfig
        {
            get => InnerHandler.DefaultConfig;
            set => InnerHandler.DefaultConfig = value;
        }

        public virtual IEnumerable<uSyncAction> Import(string file, HandlerSettings settings, bool force)
        {
            return InnerHandler.Import(file, settings, force);
        }

        public virtual IEnumerable<uSyncAction> Report(string file, HandlerSettings settings)
        {
            return InnerHandler.Report(file, settings);
        }

        public virtual IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings)
        {
            return InnerHandler.Export(id, folder, settings);
        }

        public virtual IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings)
        {
            return InnerHandler.Export(udi, folder, settings);
        }

        public virtual SyncAttempt<XElement> GetElement(Udi udi)
        {
            return InnerHandler.GetElement(udi);
        }

        public virtual IEnumerable<uSyncAction> ImportElement(XElement element, bool force)
        {
            return InnerHandler.ImportElement(element, force);
        }

        public virtual IEnumerable<uSyncAction> ReportElement(XElement element)
        {
            return InnerHandler.ReportElement(element);
        }

        public virtual IEnumerable<uSyncDependency> GetDependencies(int id, DependencyFlags flags)
        {
            return InnerHandler.GetDependencies(id, flags);
        }

        public virtual IEnumerable<uSyncDependency> GetDependencies(Guid key, DependencyFlags flags)
        {
            return InnerHandler.GetDependencies(key, flags);
        }

        public virtual IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
        {
            return InnerSyncItemHandler?.ReportElement(node, filename, settings, options);
        }

        public virtual IEnumerable<uSyncAction> ImportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
        {
            return InnerSyncItemHandler?.ImportElement(node, filename, settings, options);
        }

        public virtual IEnumerable<uSyncAction> ImportSecondPass(uSyncAction action, HandlerSettings settings, uSyncImportOptions options)
        {
            return InnerSyncItemHandler?.ImportSecondPass(action, settings, options);
        }

        public void Terminate(HandlerSettings settings)
        {
            InnerSyncItemHandler?.Terminate(settings);
        }

        public virtual IEnumerable<uSyncChange> GetChanges(
            XElement node,
            XElement currentNode,
            SyncSerializerOptions options, ISyncItemFactory syncItemFactory = null)
        {
            var factory = syncItemFactory ?? SyncItemFactory;
            return factory.GetChanges(ItemType, node, currentNode, options);
        }
    }
}