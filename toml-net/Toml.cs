namespace Toml
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;

    using Sprache;

    using global::Toml.Parser;

    public static class Toml
    {
        public static dynamic ParseAsToml(this string str)
        {
            return Parse(str);
        }

        public static dynamic Parse(string str)
        {
            var root = new ExpandoObject().AsDict();

            var parsed = TomlGrammar.Document.Parse(str);
            if (parsed == null)
            {
                return root;
            }
            
            // Load up the root values
            LoadValuesIntoConfig(root, parsed.RootValues, "(root)");

            foreach (var section in parsed.Sections)
            {
                var group = GetKeyGroup(root, section.Path);
                var path = string.Join(".", section.Path);
                LoadValuesIntoConfig(@group, section.Values, path);
            }
            
            return root;
        }

        private static IDictionary<string, object> GetKeyGroup(IDictionary<string, object> root, string[] path)
        {
            var current = root;
            foreach (var part in path)
            {
                object next;
                if (!current.TryGetValue(part, out next))
                {
                    next = new ExpandoObject();
                    current.Add(part, next);
                }
                else if (!(next is ExpandoObject))
                {
                    var message = string.Format(
                        "Duplicate key found while trying to add keygroup " + string.Join(".", path));
                    throw new FormatException(message);
                }

                current = next.AsDict();
            }

            return current;
        }

        private static void LoadValuesIntoConfig(IDictionary<string, object> config, IEnumerable<KeyValue> values, string currentPath)
        {
            foreach (var val in values)
            {
                try
                {
                    config.Add(val.Key, val.Value);
                }
                catch (ArgumentException ex)
                {
                    var message = string.Format(
                        "Duplicate key found under keygroup {0}: {1}", currentPath, val.Key);
                    throw new FormatException(message, ex);
                }
            }
        }

        // Yeah, I know it's lazy. Less noise though.
        private static IDictionary<string, object> AsDict(this ExpandoObject obj)
        {
            return obj;
        }
    }
}
