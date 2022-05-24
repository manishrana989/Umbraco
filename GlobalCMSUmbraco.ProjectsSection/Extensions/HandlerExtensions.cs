using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web.Composing;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;

namespace GlobalCMSUmbraco.ProjectsSection.Extensions
{
    public static class HandlerExtensions
    {
        public static IEnumerable<ISyncExtendedHandler> WrapSyncHandlers(this SyncHandlerCollection syncHandlers,
            IList<HandlerSet> handlerSets, string setName, 
            Type handlerWrapperType, 
            IProfilingLogger logger, Func<ISyncExtendedHandler, ISyncExtendedHandler> factory = null)
        {
            var set = handlerSets.GetHandlerSet(setName, logger);
            if (set == null) 
                yield break;

            foreach (var settings in set.Handlers.Where(x => x.Enabled))
            {
                // Get the inner handler 
                var handler = syncHandlers.ExtendedHandlers.FirstOrDefault(x => x.Alias.InvariantEquals(settings.Alias));

                // decorate with our handler
                factory = factory ?? (h => Current.Factory.CreateInstance(handlerWrapperType, h) as ISyncExtendedHandler);

                yield return factory(handler);
            }
        }

        public static HandlerSet GetHandlerSet(this IList<HandlerSet> handlerSets, string setName, ILogger logger)
        {
            var set = handlerSets.FirstOrDefault(x => x.Name.InvariantEquals(setName));
            if (set != null && set.Handlers.Count != 0) 
                return set;

            logger.Warn<SyncHandlerFactory>("No Handlers configured for requested set {setName}", setName);
            return default;
        }
    }
}