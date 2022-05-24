using Umbraco.Core.Logging;
using uSync8.BackOffice;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Services
{
    public class StarterKitUSyncService : uSyncService
    {
        private readonly SyncHandlerFactory handlerFactory;
        private readonly SyncFileService syncFileService;

        public StarterKitUSyncService(
            SyncHandlerFactory handlerFactory,
            IProfilingLogger logger,
            SyncFileService syncFileService)
            : base(handlerFactory, logger, syncFileService)
        {
            this.handlerFactory = handlerFactory;
            this.syncFileService = syncFileService;
        }
    }
}
