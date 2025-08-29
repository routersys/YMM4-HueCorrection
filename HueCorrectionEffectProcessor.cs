using System;
using System.Linq;
using System.Windows;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace IntegratedColorChange
{
    internal class HueCorrectionEffectProcessor : IVideoEffectProcessor
    {
        private readonly HueCorrectionEffect item;
        private readonly IGraphicsDevicesAndContext devices;
        private readonly HueCorrectionCustomEffect? effect;
        private readonly ID2D1Image? output;
        private ID2D1Image? input;

        public ID2D1Image Output => output ?? input ?? throw new NullReferenceException();

        public HueCorrectionEffectProcessor(IGraphicsDevicesAndContext devices, HueCorrectionEffect item)
        {
            this.item = item;
            this.devices = devices;

            try
            {
                effect = new HueCorrectionCustomEffect(devices);
                if (!effect.IsEnabled)
                {
                    effect.Dispose();
                    effect = null;
                }
                else
                {
                    output = effect.Output;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"色相補正カスタムエフェクトの作成に失敗しました。\n{ex.Message}", "エフェクトエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                effect = null;
            }
        }

        public void SetInput(ID2D1Image? input)
        {
            this.input = input;
            try
            {
                effect?.SetInput(0, input, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エフェクト入力の設定中にエラーが発生しました。\n{ex.Message}", "エフェクトエラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ClearInput()
        {
            try
            {
                effect?.SetInput(0, null, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エフェクト入力のクリア中にエラーが発生しました。\n{ex.Message}", "エフェクトエラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double NormalizeAngle(double angle)
        {
            angle %= 360;
            return angle < 0 ? angle + 360 : angle;
        }

        public DrawDescription Update(EffectDescription effectDescription)
        {
            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            if (length > 0)
            {
                item.CurrentProgress = (double)frame / length;
            }
            else
            {
                item.CurrentProgress = 0;
            }

            if (effect is null)
            {
                return effectDescription.DrawDescription;
            }

            try
            {
                var activePoints = item.Points.Where(p => p != null).ToList();

                if (activePoints.Count == 0)
                {
                    effect.NumPoints = 0;
                }
                else
                {
                    var evaluatedPoints = activePoints.Select(point => new
                    {
                        Angle = NormalizeAngle(point.Angle.GetValue(frame, length, fps)),
                        Hue = point.Hue.GetValue(frame, length, fps),
                        Saturation = Math.Max(0.0, point.Saturation.GetValue(frame, length, fps)),
                        Luminance = Math.Max(0.0, point.Luminance.GetValue(frame, length, fps))
                    }).OrderBy(p => p.Angle).ToList();

                    effect.NumPoints = Math.Min(evaluatedPoints.Count, 16);

                    for (int i = 0; i < 16; i++)
                    {
                        if (i < evaluatedPoints.Count)
                        {
                            var point = evaluatedPoints[i];
                            effect.SetShaderPoint(i,
                                (float)point.Angle,
                                (float)point.Hue,
                                (float)point.Saturation,
                                (float)point.Luminance);
                        }
                        else
                        {
                            effect.SetShaderPoint(i, 0, 0, 1, 1);
                        }
                    }
                }

                effect.Factor = (float)item.Factor.GetValue(frame, length, fps);
                effect.ColorTolerance = (float)item.ColorTolerance.GetValue(frame, length, fps);

                var ignoredColors = item.IgnoredColors;
                effect.NumIgnoredColors = Math.Min(ignoredColors.Count, 16);
                for (int i = 0; i < 16; i++)
                {
                    if (i < ignoredColors.Count)
                    {
                        effect.SetIgnoredColor(i, ignoredColors[i]);
                    }
                    else
                    {
                        effect.SetIgnoredColor(i, System.Windows.Media.Colors.Transparent);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"色相補正エフェクトの更新中にエラーが発生しました。\n{ex.Message}", "エフェクトエラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return effectDescription.DrawDescription;
        }

        public void Dispose()
        {
            try
            {
                output?.Dispose();
                effect?.SetInput(0, null, true);
                effect?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"色相補正エフェクトの破棄中にエラーが発生しました。\n{ex.Message}", "エフェクトエラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}