using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    [Weight(-100)]
    public class ContentTypeTracker : ContentTypeBaseTracker<IContentType>, IModifyingTracker
    {
        public ContentTypeTracker(ISyncSerializer<IContentType> serializer) : base(serializer)
        {
        }
    }
}
