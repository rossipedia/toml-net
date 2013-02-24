namespace Toml
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class Extensions
    {
        private static readonly Regex CommentExpression = new Regex(@"#.*$", RegexOptions.Compiled);
        private static readonly Regex KeyGroupExpression = new Regex(@"^\[(\w+(\.\w+)*)]$", RegexOptions.Compiled);
        private static readonly Regex ValueExpression = new Regex(@"^([^\s]+)\s*=(.+)$", RegexOptions.Compiled);

        private static readonly Regex IntValueExpression = new Regex(@"^\d+$", RegexOptions.Compiled);
        private static readonly Regex FloatValueExpression = new Regex(@"^\d+\.\d+?$", RegexOptions.Compiled);
        private static readonly Regex BoolValueExpression = new Regex(@"^(true|false)$", RegexOptions.Compiled);
        private static readonly Regex DateTimeValueExpression = new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", RegexOptions.Compiled);
        private static readonly Regex StringValueExpression = new Regex(@"^"".*""$", RegexOptions.Compiled);
        private static readonly Regex ArrayValueExpression = new Regex(@"^\[[^\]]+\]$", RegexOptions.Compiled);

        private static readonly Dictionary<Regex, Func<string, object>> ParserMap = new Dictionary<Regex, Func<string, object>>
        {
            { IntValueExpression, s => int.Parse(s) },
            { FloatValueExpression, s => double.Parse(s) },
            { BoolValueExpression, s => bool.Parse(s) },
            { DateTimeValueExpression, s => DateTime.ParseExact(
                s, 
                "yyyy-MM-ddTHH:mm:ssZ", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal) },
            { StringValueExpression, ParseString },
            { ArrayValueExpression, ParseArray }
        };

        public static dynamic ParseAsToml(this string str)
        {
            Contract.Requires(str != null);
            using (var reader = new StringReader(str))
            {
                return Parse(reader);
            }
        }

        private static dynamic Parse(TextReader reader)
        {
            var rootConfig = (IDictionary<string, object>)new ExpandoObject();
            var current = rootConfig;
            var currentPath = string.Empty;

            var lineNo = 0;
            while (true)
            {
                try
                {
                    var line = reader.ReadLine();
                    lineNo++;
                    if (line == null)
                    {
                        // Eof
                        break;
                    }

                    line = StripWhitespaceAndComments(line);
                    if (line == string.Empty)
                    {
                        continue; // We skip blank lines
                    }

                    var keyGroupMatch = KeyGroupExpression.Match(line);
                    if (keyGroupMatch.Success)
                    {
                        var path = keyGroupMatch.Groups[1].Value;
                        current = BuildKeyGroup(rootConfig, path);
                        currentPath = path;
                        continue;
                    }

                    var valueMatch = ValueExpression.Match(line);
                    if (valueMatch.Success)
                    {
                        var key = valueMatch.Groups[1].Value;
                        var value = valueMatch.Groups[2].Value.Trim();
                        ThrowIfKeyExists(current, key, currentPath);
                        current.Add(key, ParseValue(value));
                        continue;
                    }

                    ThrowInvalidValueFound(line);
                }
                catch (FormatException ex)
                {
                    var message = string.Format("Error, Line: {0}, Message: {1}", lineNo, ex.Message);
                    throw new FormatException(message, ex);
                }
            }

            return rootConfig;
        }

        private static void ThrowIfKeyExists(IDictionary<string, object> current, string key, string currentPath)
        {
            if (!current.ContainsKey(key))
            {
                return;
            }

            var message = string.Format("Key {0} already exists under {1}", key, currentPath.Trim() == string.Empty ? "root" : "path " + currentPath);
            throw new FormatException(message);
        }

        private static object ParseString(string value)
        {
            return value.First() == '"' && value.Last() == '"' && !value.Any(char.IsControl)
                       ? Regex.Unescape(value.Substring(1, value.Length - 2))
                       : null;
        }

        private static object ParseArray(string value)
        {
            // Simple for now (not-nested)
            var values = value.Substring(1, value.Length - 2).Split(',').Select(s => s.Trim()).ToArray();
            if (values.Length == 0)
            {
                return new object[0];
            }

            var parser = FindParserForValue(values[0]);
            var objects = values.Select(s => s.Trim()).Select(parser).ToArray();

            // Null means invalid parse
            if (objects.Any(o => o == null))
            {
                return null;
            }

            // Determine type of objects
            var type = objects[0].GetType();
            var typedArray = Array.CreateInstance(type, objects.Length);
            Array.Copy(objects, typedArray, objects.Length);
            return typedArray;
        }

        private static object ParseValue(string value)
        {
            var parser = FindParserForValue(value);
            if (parser == null)
            {
                ThrowInvalidValueFound(value);
                return null; // Won't ever hit
            }

            return parser(value);
        }

        private static void ThrowInvalidValueFound(string value)
        {
            throw new FormatException(string.Format("Invalid Value: {0}", value));
        }

        private static Func<string, object> FindParserForValue(string value)
        {
            return ParserMap.Where(kv => kv.Key.IsMatch(value)).Select(kv => kv.Value).FirstOrDefault();
        }

        private static IDictionary<string, object> BuildKeyGroup(IDictionary<string, object> rootConfig, string keyGroupPath)
        {
            var current = rootConfig;
            foreach (var part in keyGroupPath.Split('.'))
            {
                object existing;
                if (current.TryGetValue(part, out existing) && !(existing is ExpandoObject))
                {
                    var message = string.Format("Key {0} has already been defined.", part);
                    throw new FormatException(message);
                }

                if (existing == null)
                {
                    existing = new ExpandoObject();
                    current.Add(part, existing);
                }
                
                current = (ExpandoObject)existing;
            }

            return current;
        }

        private static string StripWhitespaceAndComments(string line)
        {
            return CommentExpression.Replace(line, string.Empty).Trim();
        }
    }
}
