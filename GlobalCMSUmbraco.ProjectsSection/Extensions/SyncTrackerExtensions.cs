using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using GlobalCMSUmbraco.ProjectsSection.Services.USync.Handlers;
using uSync8.BackOffice;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Extensions
{
    public static class SyncTrackerExtensions
    {
        public static IEnumerable<uSyncChange> GetChanges(this ExtendedHandlerConfigPair handlerConfigPair, uSyncPagedImportOptions importOptions, XElement node, XElement currentNode, ISyncItemFactory syncItemFactory = null)
        {
            var options = handlerConfigPair.Settings.GetSerializerOptions(importOptions);
            var itemType = handlerConfigPair.Handler.ItemType;

            if (syncItemFactory != null)
                return syncItemFactory.GetChanges(itemType, currentNode, node, options);
            
            var wrappedHandler = handlerConfigPair.Handler as uSyncHandlerWrapper;
            return wrappedHandler?.GetChanges(node, currentNode, options);
        }

        public static IEnumerable<uSyncChange> GetChanges(this SyncTrackerCollection syncTrackers, Type itemType, XElement node, XElement currentNode, SyncSerializerOptions options)
        {
            return GetChanges(syncTrackers, itemType, nameof(SyncTrackerCollection.GetChanges), node, currentNode, options);
        }

        public static IEnumerable<uSyncChange> GetChanges(this SyncTrackerCollection syncTrackers, Type itemType, XElement node, SyncSerializerOptions options)
        {
            return GetChanges(syncTrackers, itemType, nameof(SyncTrackerCollection.GetChanges), node, options);
        }

        public static IEnumerable<uSyncChange> GetChanges(this ISyncItemFactory syncItemFactory, Type itemType, XElement node, XElement currentNode, SyncSerializerOptions options)
        {
            return GetChanges(syncItemFactory, itemType, nameof(ISyncItemFactory.GetChanges), node, currentNode, options);
        }

        public static IEnumerable<uSyncChange> GetChanges(this ISyncItemFactory syncItemFactory, Type itemType, XElement node, SyncSerializerOptions options)
        {
            return GetChanges(syncItemFactory, itemType, nameof(ISyncItemFactory.GetChanges), node, options);
        }

        private static IEnumerable<uSyncChange> GetChanges<TInstance>(TInstance instance, Type itemType, string methodName, params object[] parameters)
        {
            var methodInfo = typeof(TInstance).GetMethod(methodName, parameters.Select(p => p.GetType()).ToArray());
            var method = methodInfo?.MakeGenericMethod(itemType);

            try
            {
                var result = method?.Invoke(instance, parameters);
                return result as IEnumerable<uSyncChange>;
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            return Enumerable.Empty<uSyncChange>();
        }
    }
}