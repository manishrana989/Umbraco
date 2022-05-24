using DCHMediaPicker.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Logging;

namespace DCHMediaPicker.Core.Converters
{
    public class MediaPickerValueConverter : PropertyValueConverterBase
    {
        private readonly ILogger _logger;

        public MediaPickerValueConverter(ILogger logger)
        {
            _logger = logger;
        }

        public override bool IsConverter(IPublishedPropertyType propertyType) => propertyType.EditorAlias.Equals(Constants.PropertyEditorAlias);

        public override Type GetPropertyValueType(IPublishedPropertyType propertyType) => typeof(IEnumerable<MediaItem>);

        public override object ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object inter, bool preview)
        {
            if (inter == null)
            {
                return Enumerable.Empty<MediaItem>();
            }

            try
            {
                return JsonConvert.DeserializeObject<IEnumerable<MediaItem>>(inter.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error<MediaPickerValueConverter>(ex, "Failed to convert DCHMediaPickerItem {ex}");
            }

            return Enumerable.Empty<MediaItem>();
        }
    }
}
