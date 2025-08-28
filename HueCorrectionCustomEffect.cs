using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace IntegratedColorChange
{
    internal class HueCorrectionCustomEffect : D2D1CustomShaderEffectBase
    {
        public int NumPoints { set => SetValue((int)EffectImpl.Properties.NumPoints, Math.Clamp(value, 0, 16)); }
        public float Factor { set => SetValue((int)EffectImpl.Properties.Factor, value); }

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
            public Vector2 Padding;
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
                NumPoints, Factor,
                Point0, Point1, Point2, Point3, Point4, Point5, Point6, Point7,
                Point8, Point9, Point10, Point11, Point12, Point13, Point14, Point15
            }

            [CustomEffectProperty(PropertyType.Int32, (int)Properties.NumPoints)]
            public int NumPoints { get => constants.NumPoints; set { constants.NumPoints = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.Factor)]
            public float Factor { get => constants.Factor; set { constants.Factor = value; UpdateConstants(); } }

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