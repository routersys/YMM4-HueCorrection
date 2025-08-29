using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;
using System.Collections.Generic;
using YukkuriMovieMaker.Commons;
using System.ComponentModel.DataAnnotations;
using System.Collections.Immutable;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Controls;
using System.Text.Json.Serialization;
using IntegratedColorChange.Controls;
using System.Windows.Media;

namespace IntegratedColorChange
{
    [VideoEffect("色相補正", ["拡張"], ["Hue Correction"], IsAviUtlSupported = false)]
    public class HueCorrectionEffect : VideoEffectBase
    {
        public override string Label => "色相補正";

        [Display(GroupName = "色相補正", Name = "", Order = -1)]
        [UpdateCheckPanelEditor]
        public bool UpdateCheckPlaceholder { get; set; }

        [Display(Name = "制御点", GroupName = "色相補正")]
        [HueCorrectionEditor(PropertyEditorSize = PropertyEditorSize.FullWidth)]
        public ImmutableList<HueControlPoint> Points { get => points; set => Set(ref points, value); }
        private ImmutableList<HueControlPoint> points = ImmutableList.Create(
            new HueControlPoint(10, 0, 1, 1),
            new HueControlPoint(350, 0, 1, 1)
        );

        [Display(Name = "係数", GroupName = "色相補正")]
        [AnimationSlider("F2", "", 0, 1)]
        public Animation Factor { get; } = new(1, 0, 1);

        [Display(Name = "無視する色", GroupName = "絶対色")]
        public ImmutableList<Color> IgnoredColors { get => ignoredColors; set => Set(ref ignoredColors, value); }
        private ImmutableList<Color> ignoredColors = ImmutableList<Color>.Empty;

        [Display(Name = "色の範囲", GroupName = "絶対色")]
        [AnimationSlider("F2", "", 0, 1)]
        public Animation ColorTolerance { get; } = new(0.1, 0, 1);


        [JsonIgnore]
        public double CurrentProgress { get => _currentProgress; set => Set(ref _currentProgress, value); }
        private double _currentProgress = 0.0;


        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }


        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new HueCorrectionEffectProcessor(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [.. Points, Factor, ColorTolerance];
    }
}