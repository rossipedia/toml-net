namespace Toml
{
    using System.IO;

    public static class Extensions
    {
        public static dynamic ParseAsToml(this string str)
        {
            using (var reader = new StringReader(str))
            {
                return reader.ParseAsToml();
            }
        }

        public static dynamic ParseAsToml(this TextReader reader)
        {
            return Toml.Parse(reader);
        }

        public static dynamic ParseAsToml(this FileInfo file)
        {
            using (var reader = file.OpenText())
            {
                return Toml.Parse(reader);
            }
        }
    }
}
