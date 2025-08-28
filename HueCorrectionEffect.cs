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

namespace IntegratedColorChange
{
    [VideoEffect("色相補正", ["調整"], ["Hue Correction"])]
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

        protected override IEnumerable<IAnimatable> GetAnimatables() => [.. Points, Factor];
    }
}