using System.Collections.Generic;
using System.Linq;
using GlobalCMSUmbraco.MongoDbRealmAdmin.Api.Tasks;
using GlobalCMSUmbraco.MongoDbRealmAdmin.Cli.Tasks;
using GlobalCMSUmbraco.ProjectsSection.Extensions;
using GlobalCMSUmbraco.ProjectsSection.Repositories;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using GlobalCMSUmbraco.ProjectsSection.Services;
using GlobalCMSUmbraco.ProjectsSection.Services.USync.Handlers;
using GlobalCMSUmbraco.ProjectsSection.Services.USync.Services;
using GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers;
using GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using uSync.Expansions.Core.Services;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Tracking;

namespace GlobalCMSUmbraco.ProjectsSection.Composers
{
    [ComposeAfter(typeof(uSyncCoreComposer))]
    public class ProjectsSectionUserComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // Decoration IProjectsMongoDbService with CachedProjectsMongoDbService to provide a simple level of caching
            var container = composition.Concrete as LightInject.ServiceContainer;
            container.Register<IProjectsMongoDbService, ProjectsMongoDbService>();
            container.Decorate<IProjectsMongoDbService, ProjectsMongoDbServiceWithCaching>();

            composition.Register<IProjectsSectionService, ProjectsSectionService>();

            composition.UnregisterModifyingSyncTrackers();

            composition.Register<ICloneSyncPackService>(factory =>
            {
                var starterKitSyncTrackers = GetStarterKitSyncTrackers(factory);
                var handlers = GetWrappedHandlerCollection<SyncPackStarterKitHandler>(factory, "starterkit", starterKitSyncTrackers);

                var syncHandlerFactory = factory.CreateInstance<SyncHandlerFactory>(handlers);
                var starterKitService = factory.CreateInstance<StarterKitUSyncService>(syncHandlerFactory);
                var syncHandlerService = factory.CreateInstance<SyncHandlerService>(syncHandlerFactory);
                var syncPackService = factory.CreateInstance<SyncPackService>(starterKitService, syncHandlerService);

                var cloneSyncPackService = factory.CreateInstance<CloneSyncPackService>(syncPackService, syncHandlerFactory, syncHandlerService);
                return cloneSyncPackService;

            }, Lifetime.Scope);

            composition.Register<IStarterKitsRepository, StarterKitsDiskFolderRepository>();

            composition.Register<IRealmAdminService, RealmAdminService>();
            composition.Register<ICopyRealmProjectTemplateTask, CopyRealmProjectTemplateTask>();
            composition.Register<IPrepareRealmExportFilesTask, PrepareRealmExportFilesTask>();
            composition.Register<IExportRealmAppTask, ExportRealmAppTask>();
            composition.Register<ICreateRealmUserTask, CreateRealmUserTask>();
            composition.Register<IListAppsTask, ListAppsTask>();
        }

        private static SyncTrackerCollection GetStarterKitSyncTrackers(IFactory factory)
        {
            var starterKitTrackers = new[]
            {
                typeof(ContentTracker),
                typeof(ContentTypeTracker),
                typeof(MediaTracker),
                typeof(MediaTypePrototypeTracker),
                typeof(DataTypePrototypeTracker),
                typeof(DictionaryItemPrototypeTracker)
            }.Select(t => factory.CreateInstance(t) as ISyncTrackerBase);

            return new SyncTrackerCollection(starterKitTrackers);
        }

        /// <summary>
        /// Returns a custom usync service instance with a specific list of handlers, sync trackers etc
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="factory"></param>
        /// <param name="setName"></param>
        /// <param name="syncTrackerCollection"></param>
        /// <returns></returns>
        private static SyncHandlerCollection GetWrappedHandlerCollection<THandler>(IFactory factory, string setName, SyncTrackerCollection syncTrackerCollection) where THandler : class, ISyncItemHandler
        {
            var logger = factory.GetInstance<IProfilingLogger>();
            var config = factory.GetInstance<uSyncConfig>();

            var syncItemFactory = factory.CreateInstance<SyncItemFactory>(syncTrackerCollection);

            var syncHandlers = factory.GetInstance<SyncHandlerCollection>();
            var handlers = config.Settings.HandlerSets.GetHandlerSet(setName, logger).Handlers.Where(x => x.Enabled);
            var starterKitHandlers = syncHandlers.WrapHandlersWith<THandler>(handlers, syncItemFactory, factory);
            var starterKitHandlerCollection = new SyncHandlerCollection(starterKitHandlers);

            return starterKitHandlerCollection;
        }
    }



    public static class Extensions
    {
        public static void UnregisterModifyingSyncTrackers(this Composition composition)
        {
            // get the trackers for modifying xml
            var modifyingTrackers = composition.TypeLoader.GetTypes<IModifyingTracker>(specificAssemblies: new[] { typeof(ProjectsSectionUserComposer).Assembly }).ToList();

            // remove them from usync's built in collection
            composition.WithCollectionBuilder<SyncTrackerCollectionBuilder>().Update(modifyingTrackers, (builder, type) => builder.Remove(type));
        }

        public static IEnumerable<ISyncHandler> WrapHandlersWith<THandler>(this SyncHandlerCollection syncHandlers,
            IEnumerable<HandlerSettings> setHandlers,
            ISyncItemFactory syncItemFactory,
            IFactory factory) where THandler : class, ISyncItemHandler
        {
            foreach (var settings in setHandlers)
            {
                // Get the inner handler type
                var handlerType = syncHandlers.ExtendedHandlers.FirstOrDefault(x => x.Alias.InvariantEquals(settings.Alias))?.GetType();
                if (handlerType == null)
                    continue;

                // create an instance of the handler with the custom sync item factory
                var handler = factory.CreateInstance(handlerType, syncItemFactory);

                // pass inner handler as parameter to our decorating handler
                var wrappedHandler = factory.CreateInstance<THandler>(handler, syncItemFactory) as ISyncHandler;

                yield return wrappedHandler;
            }
        }
    }
}
