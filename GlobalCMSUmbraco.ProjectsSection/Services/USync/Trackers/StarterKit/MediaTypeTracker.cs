using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace GlobalCMSUmbraco.ProjectsSection.Services.USync.Trackers.StarterKit
{
    [Weight(-100)]
    public class MediaTypePrototypeTracker : ContentTypeBaseTracker<IMediaType>, IModifyingTracker
    {
        public MediaTypePrototypeTracker(ISyncSerializer<IMediaType> serializer) : base(serializer)
        {
        }
    }
}
