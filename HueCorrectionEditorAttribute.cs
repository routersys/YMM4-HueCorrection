using System;
using System.Windows;
using YukkuriMovieMaker.Commons;

namespace IntegratedColorChange
{
    public class HueCorrectionEditorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create()
        {
            return new HueCorrectionEditor();
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            if (control is not HueCorrectionEditor editor)
                return;
            editor.DataContext = new HueCorrectionEditorViewModel(itemProperties);
        }

        public override void ClearBindings(FrameworkElement control)
        {
            if (control is not HueCorrectionEditor editor)
                return;

            if (editor.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
            editor.DataContext = null;
        }
    }
}