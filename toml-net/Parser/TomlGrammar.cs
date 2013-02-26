namespace Toml.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sprache;

    internal static class TomlGrammar
    {
        public static readonly Parser<char> Whitespace = Parse.Char(' ').Or(Parse.Char('\t'));

        public static Parser<char> WsChar(char ch)
        {
            return from s in Whitespace.Many()
                   from c in Parse.AnyChar
                   where c == ch
                   select c;
        }

        public static readonly Parser<char> Eol = Parse.Char('\r').Or(Parse.Char('\n'));

        public static readonly Parser<char> WhitespaceOrEol = Whitespace.Or(Eol);

        public static readonly Parser<IEnumerable<char>> BlankLine = from c in Whitespace.Many()
                                                                     from end in Eol
                                                                     select Enumerable.Empty<char>();

        public static readonly Parser<IEnumerable<char>> ItemSeparator = from end in Eol
                                                                         from lines in BlankLine.Many()
                                                                         select Enumerable.Empty<char>();

        public static Parser<T> Tok<T>(this Parser<T> parser, bool eol = false)
        {
            if (parser == null)
            {
                throw new ArgumentNullException("parser");
            }

            var ws = Whitespace;
            if (eol)
            {
                ws = ws.Or(Eol);
            }

            return from leading in ws.Many()
                   from item in parser
                   from trailing in ws.Many()
                   select item;
        }

        public static readonly Parser<string> EscapedString = Parse.Regex(@"(?:[^""\\]*(?:\\.[^""\\]*)*)");

        public static readonly Parser<string> StringVal = from start in Parse.Char('"')
                                                          from val in EscapedString
                                                          from end in Parse.Char('"')
                                                          select val.Unescape();

        private static readonly Dictionary<string, string> ValidEscapes = new Dictionary<string, string>
        {
            { "\\0", "\0" },
            { "\\t", "\t" },
            { "\\n", "\n" },
            { "\\r", "\r" },
            { "\\\"", "\"" }
        };

        private static string Unescape(this string raw)
        {
            var unescaped = Regex.Replace(raw, @"\\[0tnr""]", m => ValidEscapes[m.Value]);
            var invalid = Regex.Match(unescaped, @"\\[^\\]");
            if (invalid.Success)
            {
                throw new ParseException("Invalid Escape sequence: " + invalid.Value);
            }

            return unescaped.Replace("\\\\", "\\");
        }

        public static readonly Parser<string> Integral = from sign in Parse.Char('-').Optional()
                                                         from number in Parse.Number
                                                         select sign.IsEmpty ? number : sign.Get() + number;

        public static readonly Parser<long> IntVal = Integral.Select(long.Parse);

        public static readonly Parser<double> FloatVal = from integral in Integral
                                                         from dot in Parse.Char('.')
                                                         from val in Integral
                                                         select double.Parse(integral + "." + val);

        public static readonly Parser<bool> BoolVal = from val in Parse.String("true").Or(Parse.String("false")).Text()
                                                      select bool.Parse(val);

        public static readonly Parser<DateTime> DateTimeVal = from val in Parse.Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z")
                                                              select DateTime.ParseExact(
                                                                  val,
                                                                  "yyyy-MM-ddTHH:mm:ssZ",
                                                                  CultureInfo.InvariantCulture,
                                                                  DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        public static readonly Parser<object[]> IntArrayVal = ArrayParser(IntVal);

        public static readonly Parser<object[]> StringArrayVal = ArrayParser(StringVal);

        public static readonly Parser<object[]> FloatArrayVal = ArrayParser(FloatVal);

        public static readonly Parser<object[]> BoolArrayVal = ArrayParser(BoolVal);

        public static readonly Parser<object[]> DateTimeArrayVal = ArrayParser(DateTimeVal);

        public static readonly Parser<object[]> ArrayVal = IntArrayVal.DownCast()
                                                                      .Or(StringArrayVal.DownCast())
                                                                      .Or(FloatArrayVal.DownCast())
                                                                      .Or(BoolArrayVal.DownCast())
                                                                      .Or(DateTimeArrayVal.DownCast());

        public static readonly Parser<object> Value = DateTimeVal.Downcast()
                                                                 .Or(StringVal.Downcast())
                                                                 .Or(FloatVal.Downcast())
                                                                 .Or(IntVal.Downcast())
                                                                 .Or(BoolVal.Downcast())
                                                                 .Or(ArrayVal.Downcast()).Tok();

        private static Parser<object[]> DownCast<T>(this Parser<T[]> p)
        {
            return p.Select(a => a.Cast<object>().ToArray());
        }

        private static Parser<object> Downcast<T>(this Parser<T> p)
        {
            return p.Select(i => (object)i);
        }

        public static Parser<object[]> ArrayParser<T>(Parser<T> valParser)
        {
            var realParser = valParser.Select(i => (object)i).Or(Parse.Ref(() => ArrayVal));

            return from open in Parse.Char('[').Tok(true)
                   from head in realParser.Once()
                   from tail in
                       (from sep in Parse.Char(',').Tok(true)
                        from val in realParser
                        select val).Many()
                   from final in Parse.Char(',').Tok(true).Optional()
                   from close in Parse.Char(']').Tok(true)
                   select head.Concat(tail).ToArray();
        }

        public static readonly Parser<string> Key = Parse.AnyChar.Except(WhitespaceOrEol.Or(Parse.Char('='))).AtLeastOnce().Text().Tok();

        public static readonly Parser<KeyValue> KeyValue = from key in Key
                                                           from sep in Parse.Char('=').Tok()
                                                           from v in Value
                                                           select new KeyValue(key, v);

        public static readonly Parser<IEnumerable<KeyValue>> KeyValueList = from head in KeyValue.Once()
                                                                            from tail in
                                                                                (from sep in ItemSeparator
                                                                                 from item in KeyValue
                                                                                 select item).Many()
                                                                            select head.Concat(tail);

        public static readonly Parser<string> KeyGroupPart = Parse.AnyChar.Except(
            Whitespace
                .Or(Parse.Char('['))
                .Or(Parse.Char(']'))
                .Or(Parse.Char('.'))).Many().Text();

        public static readonly Parser<string[]> KeyGroup = from open in Parse.Char('[')
                                                           from head in KeyGroupPart.Once()
                                                           from tail in
                                                               (from dot in Parse.Char('.')
                                                                from part in KeyGroupPart
                                                                select part).Many()
                                                           from close in Parse.Char(']')
                                                           select head.Concat(tail).ToArray();

        public static readonly Parser<Section> Section = from path in KeyGroup.Tok()
                                                         from values in
                                                             (from br in ItemSeparator
                                                              from val in KeyValueList
                                                              select val).Optional()
                                                         select new Section(path, values.GetOrDefault());

        public static readonly Parser<IEnumerable<Section>> SectionList = from head in Section.Once()
                                                                          from tail in
                                                                              (from sep in ItemSeparator
                                                                               from item in Section
                                                                               select item).Many()
                                                                          select head.Concat(tail);


        public static readonly Parser<string> StrippedLine = from line in Parse.CharExcept('#').Many().Text().Tok()
                                                             from comment in
                                                                 (from start in Parse.Char('#')
                                                                  from trail in Parse.AnyChar.Except(Eol).Many()
                                                                  select string.Empty).Optional()
                                                             from eol in Eol.Optional()
                                                             select eol.IsEmpty ? line : line + eol.Get();

        public static readonly Parser<Document> Document = (from leading in ItemSeparator.Optional()
                                                            from rootValues in KeyValueList
                                                            from sections in ItemSeparator.Then(s => SectionList).Optional()
                                                            select new Document(rootValues, sections.GetOrDefault()))
                                                            .Or(SectionList.Optional().Select(o => new Document(null, o.GetOrDefault())))
                                                            .Then(d => from i in ItemSeparator.Optional() select d)
                                                            .CommentStripped().End();



        public static readonly Parser<string> CommentStripper = from lines in StrippedLine.Many()
                                                                select string.Join(string.Empty, lines);

        public static Parser<T> CommentStripped<T>(this Parser<T> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException("parser");
            }

            return i =>
            {
                // Strip first
                var result = CommentStripper(i);
                if (!result.WasSuccessful)
                {
                    return Result.Failure<T>(i, "Could not remove comments", new string[0]);
                }

                return parser(new Input(result.Value));
            };
        }
    }
}