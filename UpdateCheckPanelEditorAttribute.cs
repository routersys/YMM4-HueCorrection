using System.Windows;
using YukkuriMovieMaker.Commons;
using IntegratedColorChange.Controls;

namespace IntegratedColorChange
{
    internal class UpdateCheckPanelEditorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create()
        {
            return new UpdateCheckPanel();
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
        }

        public override void ClearBindings(FrameworkElement control)
        {
        }
    }
}