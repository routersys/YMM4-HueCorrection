using System;
using System.IO;
using System.Reflection;

namespace IntegratedColorChange
{
    internal static class ShaderResourceLoader
    {
        public static byte[] GetShaderResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"IntegratedColorChange.Shaders.{name}";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception($"Resource {resourceName} not found.");
            }
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}