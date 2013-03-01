namespace Toml.Tests
{
    using System.IO;

    public static class Resources
    {
        internal static string LoadEmbeddedSampleTomlFile()
        {
            var s = typeof(ConfigTests).Assembly.GetManifestResourceStream("Toml.Tests.Resources.example.toml");
            using (var reader = new StreamReader(s))
            {
                return reader.ReadToEnd();
            }
        }

        internal static string LoadEmbeddedHardTomlFile()
        {
            var s = typeof(ConfigTests).Assembly.GetManifestResourceStream("Toml.Tests.Resources.hard_example.toml");
            using (var reader = new StreamReader(s))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
