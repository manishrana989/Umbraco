using Umbraco.Core.Configuration;
using uSync.Expansions.Core.Physical;
using uSync.Expansions.Core.Services;
using uSync8.BackOffice;
using uSync8.BackOffice.Services;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Services
{
    public class StarterKitSyncPackService : SyncPackService
    {
        public StarterKitSyncPackService(
            SyncFileService syncFileService, 
            SyncPackFileService syncPackFileService, 
            SyncMediaFileService uSyncMediaFileService, 
            uSyncService uSyncService, 
            SyncHandlerService handlerService, 
            IGlobalSettings settings) : base(syncFileService, syncPackFileService, uSyncMediaFileService, uSyncService, handlerService, settings)
        {
        }
        
    }
}
