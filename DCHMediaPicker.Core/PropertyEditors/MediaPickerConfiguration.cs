using Umbraco.Core.PropertyEditors;

namespace DCHMediaPicker.Core.PropertyEditors
{
    public class MediaPickerConfiguration
    {
        [ConfigurationField("minItems", "Minimum Items", "number", Description = "The minimum number of items")]
        public int MinItems { get; set; }

        [ConfigurationField("maxItems", "Maximum Items", "number", Description = "The maximum number of items")]
        public int MaxItems { get; set; }
    }
}
