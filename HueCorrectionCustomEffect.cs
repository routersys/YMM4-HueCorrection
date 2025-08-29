using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using System.Windows.Media;

namespace IntegratedColorChange
{
    internal class HueCorrectionCustomEffect : D2D1CustomShaderEffectBase
    {
        public int NumPoints { set => SetValue((int)EffectImpl.Properties.NumPoints, Math.Clamp(value, 0, 16)); }
        public float Factor { set => SetValue((int)EffectImpl.Properties.Factor, value); }
        public float ColorTolerance { set => SetValue((int)EffectImpl.Properties.ColorTolerance, value); }

        public void SetIgnoredColor(int index, Color color)
        {
            if (index < 0 || index >= 16) return;
            var vector = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            SetValue((int)EffectImpl.Properties.IgnoredColor0 + index, vector);
        }

        public int NumIgnoredColors { set => SetValue((int)EffectImpl.Properties.NumIgnoredColors, Math.Clamp(value, 0, 16)); }

        public void SetShaderPoint(int index, float inputHue, float hueShift, float saturation, float luminance)
        {
            if (index < 0 || index >= 16) return;
            Vector4 point = new Vector4(inputHue, hueShift, saturation, luminance);
            SetValue((int)EffectImpl.Properties.Point0 + index, point);
        }

        public HueCorrectionCustomEffect(IGraphicsDevicesAndContext devices) : base(Create<EffectImpl>(devices)) { }

        [StructLayout(LayoutKind.Sequential)]
        public struct ShaderPoint
        {
            public float InputHue; public float HueShift; public float Saturation; public float Luminance;
            public ShaderPoint(float inputHue, float hueShift, float saturation, float luminance)
            {
                InputHue = inputHue; HueShift = hueShift; Saturation = saturation; Luminance = luminance;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ConstantBuffer
        {
            public int NumPoints;
            public float Factor;
            public float ColorTolerance;
            public int NumIgnoredColors;
            public Vector4 IgnoredColor0; public Vector4 IgnoredColor1; public Vector4 IgnoredColor2; public Vector4 IgnoredColor3;
            public Vector4 IgnoredColor4; public Vector4 IgnoredColor5; public Vector4 IgnoredColor6; public Vector4 IgnoredColor7;
            public Vector4 IgnoredColor8; public Vector4 IgnoredColor9; public Vector4 IgnoredColor10; public Vector4 IgnoredColor11;
            public Vector4 IgnoredColor12; public Vector4 IgnoredColor13; public Vector4 IgnoredColor14; public Vector4 IgnoredColor15;
            public ShaderPoint Point0; public ShaderPoint Point1; public ShaderPoint Point2; public ShaderPoint Point3;
            public ShaderPoint Point4; public ShaderPoint Point5; public ShaderPoint Point6; public ShaderPoint Point7;
            public ShaderPoint Point8; public ShaderPoint Point9; public ShaderPoint Point10; public ShaderPoint Point11;
            public ShaderPoint Point12; public ShaderPoint Point13; public ShaderPoint Point14; public ShaderPoint Point15;
        }

        [CustomEffect(1)]
        private class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
        {
            private ConstantBuffer constants;
            protected override void UpdateConstants()
            {
                if (drawInformation is not null)
                {
                    drawInformation.SetPixelShaderConstantBuffer(constants);
                }
            }
            private static byte[] LoadShader()
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("IntegratedColorChange.Shaders.HueCorrectionShader.cso");
                if (stream is null)
                {
                    MessageBox.Show("シェーダーリソース 'IntegratedColorChange.Shaders.HueCorrectionShader.cso' が見つかりません。", "シェーダーエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new FileNotFoundException("Shader resource not found.");
                }
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }

            public EffectImpl() : base(LoadShader()) => constants = new ConstantBuffer();

            public enum Properties
            {
                NumPoints, Factor, ColorTolerance, NumIgnoredColors,
                IgnoredColor0, IgnoredColor1, IgnoredColor2, IgnoredColor3,
                IgnoredColor4, IgnoredColor5, IgnoredColor6, IgnoredColor7,
                IgnoredColor8, IgnoredColor9, IgnoredColor10, IgnoredColor11,
                IgnoredColor12, IgnoredColor13, IgnoredColor14, IgnoredColor15,
                Point0, Point1, Point2, Point3, Point4, Point5, Point6, Point7,
                Point8, Point9, Point10, Point11, Point12, Point13, Point14, Point15
            }

            [CustomEffectProperty(PropertyType.Int32, (int)Properties.NumPoints)]
            public int NumPoints { get => constants.NumPoints; set { constants.NumPoints = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.Factor)]
            public float Factor { get => constants.Factor; set { constants.Factor = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ColorTolerance)]
            public float ColorTolerance { get => constants.ColorTolerance; set { constants.ColorTolerance = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Int32, (int)Properties.NumIgnoredColors)]
            public int NumIgnoredColors { get => constants.NumIgnoredColors; set { constants.NumIgnoredColors = value; UpdateConstants(); } }


            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor0)]
            public Vector4 IgnoredColor0 { get => constants.IgnoredColor0; set { constants.IgnoredColor0 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor1)]
            public Vector4 IgnoredColor1 { get => constants.IgnoredColor1; set { constants.IgnoredColor1 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor2)]
            public Vector4 IgnoredColor2 { get => constants.IgnoredColor2; set { constants.IgnoredColor2 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor3)]
            public Vector4 IgnoredColor3 { get => constants.IgnoredColor3; set { constants.IgnoredColor3 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor4)]
            public Vector4 IgnoredColor4 { get => constants.IgnoredColor4; set { constants.IgnoredColor4 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor5)]
            public Vector4 IgnoredColor5 { get => constants.IgnoredColor5; set { constants.IgnoredColor5 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor6)]
            public Vector4 IgnoredColor6 { get => constants.IgnoredColor6; set { constants.IgnoredColor6 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor7)]
            public Vector4 IgnoredColor7 { get => constants.IgnoredColor7; set { constants.IgnoredColor7 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor8)]
            public Vector4 IgnoredColor8 { get => constants.IgnoredColor8; set { constants.IgnoredColor8 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor9)]
            public Vector4 IgnoredColor9 { get => constants.IgnoredColor9; set { constants.IgnoredColor9 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor10)]
            public Vector4 IgnoredColor10 { get => constants.IgnoredColor10; set { constants.IgnoredColor10 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor11)]
            public Vector4 IgnoredColor11 { get => constants.IgnoredColor11; set { constants.IgnoredColor11 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor12)]
            public Vector4 IgnoredColor12 { get => constants.IgnoredColor12; set { constants.IgnoredColor12 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor13)]
            public Vector4 IgnoredColor13 { get => constants.IgnoredColor13; set { constants.IgnoredColor13 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor14)]
            public Vector4 IgnoredColor14 { get => constants.IgnoredColor14; set { constants.IgnoredColor14 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.IgnoredColor15)]
            public Vector4 IgnoredColor15 { get => constants.IgnoredColor15; set { constants.IgnoredColor15 = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point0)]
            public Vector4 Point0 { get => new Vector4(constants.Point0.InputHue, constants.Point0.HueShift, constants.Point0.Saturation, constants.Point0.Luminance); set { constants.Point0 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point1)]
            public Vector4 Point1 { get => new Vector4(constants.Point1.InputHue, constants.Point1.HueShift, constants.Point1.Saturation, constants.Point1.Luminance); set { constants.Point1 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point2)]
            public Vector4 Point2 { get => new Vector4(constants.Point2.InputHue, constants.Point2.HueShift, constants.Point2.Saturation, constants.Point2.Luminance); set { constants.Point2 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point3)]
            public Vector4 Point3 { get => new Vector4(constants.Point3.InputHue, constants.Point3.HueShift, constants.Point3.Saturation, constants.Point3.Luminance); set { constants.Point3 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point4)]
            public Vector4 Point4 { get => new Vector4(constants.Point4.InputHue, constants.Point4.HueShift, constants.Point4.Saturation, constants.Point4.Luminance); set { constants.Point4 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point5)]
            public Vector4 Point5 { get => new Vector4(constants.Point5.InputHue, constants.Point5.HueShift, constants.Point5.Saturation, constants.Point5.Luminance); set { constants.Point5 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point6)]
            public Vector4 Point6 { get => new Vector4(constants.Point6.InputHue, constants.Point6.HueShift, constants.Point6.Saturation, constants.Point6.Luminance); set { constants.Point6 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point7)]
            public Vector4 Point7 { get => new Vector4(constants.Point7.InputHue, constants.Point7.HueShift, constants.Point7.Saturation, constants.Point7.Luminance); set { constants.Point7 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point8)]
            public Vector4 Point8 { get => new Vector4(constants.Point8.InputHue, constants.Point8.HueShift, constants.Point8.Saturation, constants.Point8.Luminance); set { constants.Point8 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point9)]
            public Vector4 Point9 { get => new Vector4(constants.Point9.InputHue, constants.Point9.HueShift, constants.Point9.Saturation, constants.Point9.Luminance); set { constants.Point9 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point10)]
            public Vector4 Point10 { get => new Vector4(constants.Point10.InputHue, constants.Point10.HueShift, constants.Point10.Saturation, constants.Point10.Luminance); set { constants.Point10 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point11)]
            public Vector4 Point11 { get => new Vector4(constants.Point11.InputHue, constants.Point11.HueShift, constants.Point11.Saturation, constants.Point11.Luminance); set { constants.Point11 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point12)]
            public Vector4 Point12 { get => new Vector4(constants.Point12.InputHue, constants.Point12.HueShift, constants.Point12.Saturation, constants.Point12.Luminance); set { constants.Point12 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point13)]
            public Vector4 Point13 { get => new Vector4(constants.Point13.InputHue, constants.Point13.HueShift, constants.Point13.Saturation, constants.Point13.Luminance); set { constants.Point13 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point14)]
            public Vector4 Point14 { get => new Vector4(constants.Point14.InputHue, constants.Point14.HueShift, constants.Point14.Saturation, constants.Point14.Luminance); set { constants.Point14 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.Point15)]
            public Vector4 Point15 { get => new Vector4(constants.Point15.InputHue, constants.Point15.HueShift, constants.Point15.Saturation, constants.Point15.Luminance); set { constants.Point15 = new ShaderPoint(value.X, value.Y, value.Z, value.W); UpdateConstants(); } }
        }
    }
}