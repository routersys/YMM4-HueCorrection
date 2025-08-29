using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;

namespace IntegratedColorChange
{
    public class HueCorrectionEditorViewModel : Bindable, IDisposable
    {
        public class DisplayPointViewModel : Bindable
        {
            public HueControlPoint OriginalPoint { get; }
            public double X { get => _x; set => Set(ref _x, value); }
            private double _x;
            public double Y { get => _y; set => Set(ref _y, value); }
            private double _y;
            public DisplayPointViewModel(HueControlPoint originalPoint) { OriginalPoint = originalPoint; }
        }

        public class IgnoredColorViewModel : Bindable
        {
            public Color Color { get => _color; set => Set(ref _color, value); }
            private Color _color;

            public IgnoredColorViewModel(Color color)
            {
                _color = color;
            }
        }

        public enum EditMode { Luminance, Saturation, Hue }

        private readonly ItemProperty[] itemProperties;
        private readonly INotifyPropertyChanged item;
        private readonly HueCorrectionEffect owner;
        private Point lastClickPosition;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;

        public ImmutableList<HueControlPoint> Points { get; private set; } = ImmutableList<HueControlPoint>.Empty;
        public ObservableCollection<DisplayPointViewModel> DisplayPoints { get; } = new();

        public HueControlPoint? SelectedPoint
        {
            get => selectedPoint;
            set
            {
                if (Set(ref selectedPoint, value))
                {
                    OnPropertyChanged(nameof(IsPointSelected));
                    (DeleteSelectedPointCommand as ActionCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private HueControlPoint? selectedPoint;

        public bool IsPointSelected => SelectedPoint != null;

        public PathGeometry PathData { get => pathData; private set => Set(ref pathData, value); }
        private PathGeometry pathData = new();

        public EditMode CurrentMode { get => currentMode; set { if (Set(ref currentMode, value)) OnModeChanged(); } }
        private EditMode currentMode = EditMode.Luminance;

        public bool IsLuminanceMode { get => CurrentMode == EditMode.Luminance; set { if (value) CurrentMode = EditMode.Luminance; } }
        public bool IsSaturationMode { get => CurrentMode == EditMode.Saturation; set { if (value) CurrentMode = EditMode.Saturation; } }
        public bool IsHueMode { get => CurrentMode == EditMode.Hue; set { if (value) CurrentMode = EditMode.Hue; } }

        public double TimeSliderPosition { get => _timeSliderPosition; set { if (Set(ref _timeSliderPosition, value)) UpdateAnimatedState(); } }
        private double _timeSliderPosition;
        public bool ShowTimeline { get => _showTimeline; set { if (Set(ref _showTimeline, value)) UpdateAnimatedState(); } }
        private bool _showTimeline = true;
        public bool ShowGrid { get => _showGrid; set { if (Set(ref _showGrid, value)) UpdateAnimatedState(); } }
        private bool _showGrid = true;

        public double TimelineX { get => _timelineX; private set => Set(ref _timelineX, value); }
        private double _timelineX;
        private double CanvasWidth { get; set; } = 400;
        private double CanvasHeight { get; set; } = 180;

        public ObservableCollection<double> VerticalGridLines { get; } = [];
        public ObservableCollection<double> HorizontalGridLines { get; } = [];

        public ObservableCollection<IgnoredColorViewModel> IgnoredColors { get; } = new();
        public IgnoredColorViewModel? SelectedIgnoredColor
        {
            get => _selectedIgnoredColor;
            set
            {
                if (Set(ref _selectedIgnoredColor, value))
                {
                    (RemoveIgnoredColorCommand as ActionCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private IgnoredColorViewModel? _selectedIgnoredColor;

        public ICommand SetLuminanceModeCommand { get; }
        public ICommand SetSaturationModeCommand { get; }
        public ICommand SetHueModeCommand { get; }
        public ICommand AddPointCommand { get; }
        public ICommand DeleteSelectedPointCommand { get; }
        public ICommand SizeChangedCommand { get; }
        public ICommand AddIgnoredColorCommand { get; }
        public ICommand RemoveIgnoredColorCommand { get; }

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public HueCorrectionEditorViewModel(ItemProperty[] itemProperties)
        {
            this.itemProperties = itemProperties;
            item = (INotifyPropertyChanged)itemProperties[0].PropertyOwner;
            owner = (HueCorrectionEffect)item;
            item.PropertyChanged += Item_PropertyChanged;
            _dispatcher = Application.Current.Dispatcher;

            SetLuminanceModeCommand = new ActionCommand(_ => true, _ => { CurrentMode = EditMode.Luminance; });
            SetSaturationModeCommand = new ActionCommand(_ => true, _ => { CurrentMode = EditMode.Saturation; });
            SetHueModeCommand = new ActionCommand(_ => true, _ => { CurrentMode = EditMode.Hue; });

            AddPointCommand = new ActionCommand(_ => true, _ => AddPointAt());
            DeleteSelectedPointCommand = new ActionCommand(_ => IsPointSelected && Points.Count > 1, _ => DeleteSelectedPoint());
            SizeChangedCommand = new ActionCommand(p => p is Canvas, p =>
            {
                if (p is Canvas canvas)
                {
                    CanvasWidth = canvas.ActualWidth;
                    CanvasHeight = canvas.ActualHeight;
                    PopulateGridLines();
                    UpdateAnimatedState();
                }
            });

            AddIgnoredColorCommand = new ActionCommand(
                _ => true,
                _ =>
                {
                    OnBeginEdit();
                    IgnoredColors.Add(new IgnoredColorViewModel(Colors.White));
                    SaveChanges();
                    OnEndEdit();
                });

            RemoveIgnoredColorCommand = new ActionCommand(
                _ => SelectedIgnoredColor != null,
                _ =>
                {
                    if (SelectedIgnoredColor == null) return;
                    OnBeginEdit();
                    IgnoredColors.Remove(SelectedIgnoredColor);
                    SaveChanges();
                    OnEndEdit();
                });

            UpdatePointsFromSource();
            UpdateIgnoredColorsFromSource();
            if (SelectedPoint is null && Points.Count > 0)
            {
                SelectedPoint = Points[0];
            }
            PopulateGridLines();
        }

        public void SaveChanges()
        {
            var newColors = IgnoredColors.Select(vm => vm.Color).ToImmutableList();
            SetIgnoredColorsValue(newColors);
        }

        private void OnModeChanged()
        {
            OnPropertyChanged(nameof(IsLuminanceMode));
            OnPropertyChanged(nameof(IsSaturationMode));
            OnPropertyChanged(nameof(IsHueMode));
            UpdateAnimatedState();
        }

        private double NormalizeAngle(double angle)
        {
            angle %= 360;
            return angle < 0 ? angle + 360 : angle;
        }

        private void PopulateGridLines()
        {
            VerticalGridLines.Clear();
            HorizontalGridLines.Clear();

            if (CanvasWidth <= 0 || CanvasHeight <= 0) return;

            double stepX = CanvasWidth / 6.0;
            for (int i = 1; i < 6; i++)
            {
                VerticalGridLines.Add(i * stepX);
            }

            double stepY = CanvasHeight / 6.0;
            for (int i = 1; i < 6; i++)
            {
                HorizontalGridLines.Add(i * stepY);
            }
        }


        public void SetClickPosition(Point position) => lastClickPosition = position;

        private void AddPointAt()
        {
            OnBeginEdit();
            var newAngle = (lastClickPosition.X / CanvasWidth) * 360.0;
            var newPoint = new HueControlPoint(newAngle, 0, 1, 1);
            var newPoints = Points.Add(newPoint);
            SetValue(newPoints);
            SelectedPoint = newPoint;
            OnEndEdit();
        }

        private void DeleteSelectedPoint()
        {
            if (SelectedPoint is null) return;
            OnBeginEdit();
            var newPoints = Points.Remove(SelectedPoint);
            SetValue(newPoints);
            SelectedPoint = newPoints.FirstOrDefault();
            OnEndEdit();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_dispatcher.CheckAccess())
            {
                HandlePropertyChanged(e.PropertyName);
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => HandlePropertyChanged(e.PropertyName)));
            }
        }

        private void HandlePropertyChanged(string? propertyName)
        {
            switch (propertyName)
            {
                case nameof(owner.Points):
                    UpdatePointsFromSource();
                    break;
                case nameof(owner.IgnoredColors):
                    UpdateIgnoredColorsFromSource();
                    break;
                case nameof(owner.CurrentProgress):
                    if (ShowTimeline)
                    {
                        TimeSliderPosition = owner.CurrentProgress;
                    }
                    break;
            }
        }


        private void UpdatePointsFromSource()
        {
            var sourcePoints = itemProperties
                .Select(p => p.GetValue<ImmutableList<HueControlPoint>>())
                .FirstOrDefault();

            if (sourcePoints is null || sourcePoints.SequenceEqual(Points)) return;

            Points = sourcePoints;

            DisplayPoints.Clear();
            foreach (var p in Points)
            {
                DisplayPoints.Add(new DisplayPointViewModel(p));
            }

            if (SelectedPoint is not null && !Points.Contains(SelectedPoint))
            {
                SelectedPoint = Points.FirstOrDefault();
            }
            (DeleteSelectedPointCommand as ActionCommand)?.RaiseCanExecuteChanged();
            UpdateAnimatedState();
        }

        private void UpdateIgnoredColorsFromSource()
        {
            if (itemProperties.Length == 0) return;
            var ownerEffect = (HueCorrectionEffect)itemProperties[0].PropertyOwner;
            var sourceColors = ownerEffect.IgnoredColors;

            if (sourceColors is null) return;

            var currentColors = IgnoredColors.Select(vm => vm.Color);
            if (currentColors.SequenceEqual(sourceColors)) return;

            IgnoredColors.Clear();
            foreach (var color in sourceColors)
            {
                IgnoredColors.Add(new IgnoredColorViewModel(color));
            }
            (RemoveIgnoredColorCommand as ActionCommand)?.RaiseCanExecuteChanged();
        }

        private void SetIgnoredColorsValue(ImmutableList<Color> newColors)
        {
            foreach (var prop in itemProperties)
            {
                if (prop.PropertyOwner is HueCorrectionEffect effect)
                {
                    effect.IgnoredColors = newColors;
                }
            }
        }

        private void SetValue(ImmutableList<HueControlPoint> newPoints)
        {
            foreach (var prop in itemProperties)
            {
                var clonedPoints = newPoints.Select(p => new HueControlPoint(p)).ToImmutableList();
                prop.SetValue(clonedPoints);
            }
        }

        public void CopyToOtherItems()
        {
            var sourcePoints = itemProperties[0].GetValue<ImmutableList<HueControlPoint>>();
            if (sourcePoints is null) return;

            foreach (var prop in itemProperties.Skip(1))
            {
                prop.SetValue(sourcePoints.Select(p => new HueControlPoint(p)).ToImmutableList());
            }
        }

        public void UpdatePointPosition(HueControlPoint point, double dx, double dy)
        {
            if (CanvasWidth <= 0 || CanvasHeight <= 0) return;

            var frame = (int)(TimeSliderPosition * 100);
            const int length = 100;
            const int fps = 60;

            var sortedPoints = Points
                .OrderBy(p => NormalizeAngle(p.Angle.GetValue(frame, length, fps)))
                .ToList();
            var currentIndex = sortedPoints.IndexOf(point);

            double minAngle = 0;
            double maxAngle = 360;

            if (currentIndex > 0)
            {
                minAngle = NormalizeAngle(sortedPoints[currentIndex - 1].Angle.GetValue(frame, length, fps));
            }
            if (currentIndex < sortedPoints.Count - 1)
            {
                maxAngle = NormalizeAngle(sortedPoints[currentIndex + 1].Angle.GetValue(frame, length, fps));
            }

            var currentAngle = NormalizeAngle(point.Angle.GetValue(frame, length, fps));
            var currentX = (currentAngle / 360.0) * CanvasWidth;
            var newX = Math.Clamp(currentX + dx, 0, CanvasWidth);
            var newAngle = (newX / CanvasWidth) * 360.0;

            if (maxAngle < minAngle)
            {
                if (newAngle > maxAngle && newAngle < minAngle)
                {
                    if (Math.Abs(newAngle - maxAngle) < Math.Abs(newAngle - minAngle))
                        newAngle = maxAngle;
                    else
                        newAngle = minAngle;
                }
            }
            else
            {
                newAngle = Math.Clamp(newAngle, minAngle, maxAngle);
            }

            point.Angle.Values[0].Value = newAngle;

            var converter = new AngleToPointConverter();
            var currentY = (double)converter.Convert(new object[] { CurrentMode, point.Angle.Values[0].Value, point.Luminance.Values[0].Value, point.Saturation.Values[0].Value, point.Hue.Values[0].Value, CanvasWidth, CanvasHeight, "Y" }, typeof(double), string.Empty, System.Globalization.CultureInfo.CurrentCulture);
            var newY = Math.Clamp(currentY + dy, 0, CanvasHeight);

            double center = CanvasHeight / 2.0;
            double offsetRatio = (center - newY) / center;

            switch (CurrentMode)
            {
                case EditMode.Luminance: point.Luminance.Values[0].Value = Math.Clamp(1.0 + offsetRatio, 0.0, 2.0); break;
                case EditMode.Saturation: point.Saturation.Values[0].Value = Math.Clamp(1.0 + offsetRatio, 0.0, 2.0); break;
                case EditMode.Hue: point.Hue.Values[0].Value = Math.Clamp(offsetRatio * 180.0, -180.0, 180.0); break;
            }

            CopyToOtherItems();
            UpdateAnimatedState();
        }


        public void UpdateAnimatedState()
        {
            if (CanvasWidth <= 0) return;
            TimelineX = TimeSliderPosition * CanvasWidth;

            var frame = (int)(TimeSliderPosition * 100);
            const int length = 100;
            const int fps = 60;

            foreach (var displayPoint in DisplayPoints)
            {
                var sourcePoint = displayPoint.OriginalPoint;
                var values = new object[]
                {
                    CurrentMode,
                    sourcePoint.Angle.GetValue(frame, length, fps),
                    sourcePoint.Luminance.GetValue(frame, length, fps),
                    sourcePoint.Saturation.GetValue(frame, length, fps),
                    sourcePoint.Hue.GetValue(frame, length, fps),
                    CanvasWidth,
                    CanvasHeight,
                    ""
                };

                var converter = new AngleToPointConverter();
                values[7] = "X";
                displayPoint.X = (double)converter.Convert(values, typeof(double), string.Empty, System.Globalization.CultureInfo.CurrentCulture);
                values[7] = "Y";
                displayPoint.Y = (double)converter.Convert(values, typeof(double), string.Empty, System.Globalization.CultureInfo.CurrentCulture);
            }

            var sortedPoints = Points.OrderBy(p => NormalizeAngle(p.Angle.GetValue(frame, length, fps))).ToList();
            UpdateLines(sortedPoints, frame, length, fps);
        }

        private void UpdateLines(List<HueControlPoint> sortedPoints, int frame, int length, int fps)
        {
            if (sortedPoints.Count < 1)
            {
                PathData = new PathGeometry();
                return;
            }

            var extendedPoints = new List<HueControlPoint>();

            var firstPointAngle = NormalizeAngle(sortedPoints.First().Angle.GetValue(frame, length, fps));
            var lastPointAngle = NormalizeAngle(sortedPoints.Last().Angle.GetValue(frame, length, fps));
            var angleDiff = NormalizeAngle(firstPointAngle - lastPointAngle);

            if (angleDiff > 0)
            {
                var ghostBefore = new HueControlPoint(sortedPoints.Last())
                {
                    Angle = { Values = { new AnimationValue(sortedPoints.Last().Angle.GetValue(frame, length, fps) - 360) } }
                };
                extendedPoints.Add(ghostBefore);
            }

            extendedPoints.AddRange(sortedPoints);

            var ghostAfter = new HueControlPoint(sortedPoints.First())
            {
                Angle = { Values = { new AnimationValue(sortedPoints.First().Angle.GetValue(frame, length, fps) + 360) } }
            };
            extendedPoints.Add(ghostAfter);

            var converter = new AngleToPointConverter();
            var segments = new PathSegmentCollection();

            Point GetPoint(HueControlPoint p)
            {
                var values = new object[]
                {
                    CurrentMode, p.Angle.GetValue(frame, length, fps), p.Luminance.GetValue(frame, length, fps),
                    p.Saturation.GetValue(frame, length, fps), p.Hue.GetValue(frame, length, fps),
                    CanvasWidth, CanvasHeight, ""
                };
                values[7] = "X";
                var x = (double)converter.Convert(values, typeof(double), string.Empty, System.Globalization.CultureInfo.CurrentCulture);
                values[7] = "Y";
                var y = (double)converter.Convert(values, typeof(double), string.Empty, System.Globalization.CultureInfo.CurrentCulture);
                return new Point(x, y);
            }

            var startPoint = GetPoint(extendedPoints[0]);
            for (int i = 1; i < extendedPoints.Count; i++)
            {
                segments.Add(new LineSegment(GetPoint(extendedPoints[i]), true));
            }

            var figure = new PathFigure(startPoint, segments, false);
            PathData = new PathGeometry(new[] { figure });
        }


        public void OnBeginEdit() => BeginEdit?.Invoke(this, EventArgs.Empty);
        public void OnEndEdit() => EndEdit?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }
    }
}