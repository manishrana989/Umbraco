using System;
using System.Collections.Generic;
using Umbraco.Core.Composing;

namespace GlobalCMSUmbraco.ProjectsSection.Extensions
{
    public static class CollectionBuilderExtensions
    {
        public static void Update<TBuilder, TItem>(this TBuilder builder, IEnumerable<TItem> items, Action<TBuilder, TItem> action) where TBuilder : ICollectionBuilder, new()
        {
            foreach (var item in items)
            {
                action.Invoke(builder, item);
            }
        }
    }
}
