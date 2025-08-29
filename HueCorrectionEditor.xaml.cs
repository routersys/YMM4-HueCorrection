using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YukkuriMovieMaker.Commons;

namespace IntegratedColorChange
{
    public partial class HueCorrectionEditor : UserControl, IPropertyEditorControl
    {
        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public HueCorrectionEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is HueCorrectionEditorViewModel oldVm)
            {
                oldVm.PropertyChanged -= ViewModel_PropertyChanged;
                oldVm.BeginEdit -= OnViewModelBeginEdit;
                oldVm.EndEdit -= OnViewModelEndEdit;
            }
            if (e.NewValue is HueCorrectionEditorViewModel newVm)
            {
                newVm.PropertyChanged += ViewModel_PropertyChanged;
                newVm.BeginEdit += OnViewModelBeginEdit;
                newVm.EndEdit += OnViewModelEndEdit;
                CreateHueGradientImage();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HueCorrectionEditorViewModel.CurrentMode))
            {
                CreateHueGradientImage();
            }
        }

        private void OnViewModelBeginEdit(object? sender, EventArgs e) => BeginEdit?.Invoke(this, e);
        private void OnViewModelEndEdit(object? sender, EventArgs e) => EndEdit?.Invoke(this, e);

        private void CreateHueGradientImage()
        {
            int width = (int)ControlPointCanvas.ActualWidth;
            int height = (int)ControlPointCanvas.ActualHeight;

            if (width <= 0) width = 400;
            if (height <= 0) height = 120;

            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            var pixels = new byte[width * height * 4];
            int halfHeight = height / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double hue = (double)x / width;
                    Color color;

                    if (y < halfHeight)
                    {
                        color = HslToRgb(hue, 1.0, 0.5);
                    }
                    else
                    {
                        color = HslToRgb(hue, 0.8, 0.35);
                    }

                    int index = (y * width + x) * 4;
                    pixels[index] = color.B;
                    pixels[index + 1] = color.G;
                    pixels[index + 2] = color.R;
                    pixels[index + 3] = 255;
                }
            }

            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            HueGradientImage.Source = wb;
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            h -= Math.Floor(h);
            s = Math.Max(0, Math.Min(1, s));
            l = Math.Max(0, Math.Min(1, l));

            double r, g, b;
            if (Math.Abs(s) < 0.001)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = HueToRgb(p, q, h + 1.0 / 3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3.0);
            }
            return Color.FromRgb(
                (byte)Math.Round(Math.Max(0, Math.Min(255, r * 255))),
                (byte)Math.Round(Math.Max(0, Math.Min(255, g * 255))),
                (byte)Math.Round(Math.Max(0, Math.Min(255, b * 255)))
            );
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: HueCorrectionEditorViewModel.DisplayPointViewModel dpvm } && DataContext is HueCorrectionEditorViewModel vm)
            {
                vm.SelectedPoint = dpvm.OriginalPoint;
                BeginEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: HueCorrectionEditorViewModel.DisplayPointViewModel dpvm } && DataContext is HueCorrectionEditorViewModel vm)
            {
                vm.UpdatePointPosition(dpvm.OriginalPoint, e.HorizontalChange, e.VerticalChange);
            }
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            EndEdit?.Invoke(this, EventArgs.Empty);
        }

        private void ControlPointCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is HueCorrectionEditorViewModel vm)
            {
                vm.SetClickPosition(e.GetPosition(ControlPointCanvas));
            }
        }

        private void PropertiesEditor_BeginEdit(object? sender, EventArgs e)
        {
            BeginEdit?.Invoke(this, e);
        }

        private void PropertiesEditor_EndEdit(object? sender, EventArgs e)
        {
            if (DataContext is HueCorrectionEditorViewModel vm)
            {
                vm.CopyToOtherItems();
                vm.UpdateAnimatedState();
            }
            EndEdit?.Invoke(this, e);
        }

        private void ColorPicker_BeginEdit(object? sender, EventArgs e)
        {
            if (DataContext is HueCorrectionEditorViewModel vm)
            {
                vm.OnBeginEdit();
            }
        }

        private void ColorPicker_EndEdit(object? sender, EventArgs e)
        {
            if (DataContext is HueCorrectionEditorViewModel vm)
            {
                vm.SaveChanges();
                vm.OnEndEdit();
            }
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is true) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Visibility.Visible);
        }
    }

    public class IsNotNullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}