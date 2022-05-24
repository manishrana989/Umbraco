using GlobalCMSUmbraco.ExternalApi.Services;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace GlobalCMSUmbraco.ExternalApi.Composers
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class ExternalApiUserComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<IBackgroundTasksService, BackgroundTasksService>();
        }

    }
}
