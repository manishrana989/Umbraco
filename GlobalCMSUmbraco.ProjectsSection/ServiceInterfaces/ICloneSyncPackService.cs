using System.Collections.Generic;
using uSync.Exporter;

namespace GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces
{
    public interface ICloneSyncPackService
    {
        List<ExporterStep> CreateCloneSteps(ExporterRequest exporterRequest, string projectCode = null);

        ExporterResponse Process(ExporterRequest exporterRequest);

        bool CloneAndProcess(string projectCode, string selectedSyncPackFile);
    }
}
