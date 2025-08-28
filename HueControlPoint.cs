using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace IntegratedColorChange
{
    public class HueControlPoint : Animatable
    {
        [Display(Name = "色相", GroupName = "制御点")]
        [AnimationSlider("F1", "°", 0, 360)]
        public Animation Angle { get; } = new(0, 0, 360);

        [Display(Name = "色相補正", GroupName = "制御点")]
        [AnimationSlider("F1", "°", -180, 180)]
        public Animation Hue { get; } = new(0, -180, 180);

        [Display(Name = "彩度", GroupName = "制御点")]
        [AnimationSlider("F2", "", 0, 2)]
        public Animation Saturation { get; } = new(1, 0, 2);

        [Display(Name = "輝度", GroupName = "制御点")]
        [AnimationSlider("F2", "", 0, 2)]
        public Animation Luminance { get; } = new(1, 0, 2);

        public HueControlPoint() { }

        public HueControlPoint(double angle, double hue, double saturation, double luminance)
        {
            Angle.Values[0].Value = angle;
            Hue.Values[0].Value = hue;
            Saturation.Values[0].Value = saturation;
            Luminance.Values[0].Value = luminance;
        }

        public HueControlPoint(HueControlPoint other)
        {
            Angle.CopyFrom(other.Angle);
            Hue.CopyFrom(other.Hue);
            Saturation.CopyFrom(other.Saturation);
            Luminance.CopyFrom(other.Luminance);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [Angle, Hue, Saturation, Luminance];
    }
}