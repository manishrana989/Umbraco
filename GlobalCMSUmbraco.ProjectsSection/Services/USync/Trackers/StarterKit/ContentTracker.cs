using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    [Weight(-100)]
    public class ContentTracker : ContentBaseTracker<IContent>
    {
        public ContentTracker(ISyncSerializer<IContent> serializer) : base(serializer)
        {
        }
    }
}
