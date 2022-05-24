using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;

namespace DCHMediaPicker.Core.PropertyEditors
{
    [DataEditor(alias: Constants.PropertyEditorAlias, name: "DCH Media Picker", view: "~/App_Plugins/DCHMediaPicker/Views/editor.html", Group = "pickers", Icon = "icon-picture", ValueType = "JSON")]
    public class MediaPickerPropertyEditor : DataEditor
    {
        public MediaPickerPropertyEditor(ILogger logger)
            : base(logger)
        { }

        protected override IConfigurationEditor CreateConfigurationEditor() => new MediaPickerConfigurationEditor();

        public class MediaPickerConfigurationEditor : ConfigurationEditor<MediaPickerConfiguration>
        {

        }
    }
}