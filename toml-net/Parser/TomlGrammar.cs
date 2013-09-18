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
        // Tokens

        public static readonly Parser<char> Whitespace = Parse.Char(' ').Or(Parse.Char('\t'));

        public static readonly Parser<char> Eol = Parse.Char('\r').Or(Parse.Char('\n'));

        public static readonly Parser<char> WhitespaceOrEol = Whitespace.Or(Eol);

        public static readonly Parser<char> ArrayOpen = Parse.Char('[');
        public static readonly Parser<char> ArrayClose = Parse.Char(']');
        public static readonly Parser<char> KeyGroupOpen = Parse.Char('[');
        public static readonly Parser<char> KeyGroupClose = Parse.Char(']');
        public static readonly Parser<char> Assign = Parse.Char('=');
        public static readonly Parser<char> KeySeparator = Parse.Char('.');
        public static readonly Parser<char> Negative = Parse.Char('-');
        public static readonly Parser<char> ArraySeparator = Parse.Char(',');
        public static readonly Parser<char> CommentStart = Parse.Char('#');

        public static readonly Parser<IEnumerable<char>> Comment = CommentStart.Then(s => Parse.AnyChar.Except(Eol).Many());


        public static Parser<IEnumerable<T>> Sequence<T, U>(this Parser<T> parser, Parser<U> delimiter, bool allowTrailing = false)
        {
            var head = parser.Once();
            var tail = delimiter.Then(d => parser).Many();

            var sequence = allowTrailing
                ? (from h in head
                   from t in tail
                   from d in delimiter.Optional()
                   select h.Concat(t))
                : (from h in head
                   from t in tail
                   select h.Concat(t));

            return sequence.Optional().Select(o => o.GetOrElse(Enumerable.Empty<T>()));
        }

        public static Parser<IEnumerable<T>> BoundedSequence<T, U, V, W>(this Parser<T> parser, Parser<U> open, Parser<V> delim, Parser<W> close, bool allowTrailing = false)
        {
            return from o in open
                   from seq in parser.Sequence(delim, allowTrailing)
                   from c in close
                   select seq;
        }

        public static Parser<char> AnyInLineExcept(char ch)
        {
            return from c in Parse.AnyChar.Except(Parse.Char(ch).Or(Eol))
                   select c;
        }

        public static readonly Parser<IEnumerable<char>> TokenSpace = from s in Whitespace.Many()
                                                                      from c in Comment.Optional()
                                                                      from e in WhitespaceOrEol.Many()
                                                                      select Enumerable.Empty<char>();

        public static readonly Parser<string> ItemSeparator = Eol.AsToken().Many().Text().Select(s => string.Empty);

        public static Parser<T> AsToken<T>(this Parser<T> parser, bool eol = false)
        {
            var ws = Whitespace.Many();
            if (eol) ws = TokenSpace;

            return from leading in ws
                   from item in parser
                   from trailing in ws
                   select item;
        }

        public static Parser<T> AsLToken<T>(this Parser<T> parser)
        {
            return from leading in TokenSpace
                   from item in parser
                   select item;
        }

        public static Parser<T> AsRToken<T>(this Parser<T> parser)
        {
            return from item in parser
                   from trailing in TokenSpace
                   select item;
        }

        public static readonly Parser<char> Quote = Parse.Char('"');

        public static readonly Parser<string> EscapedString = Parse.Regex(@"(?:[^""\\]*(?:\\.[^""\\]*)*)");

        public static readonly Parser<string> EscapedQuotedString = from start in Quote
                                                                    from val in EscapedString
                                                                    from end in Quote
                                                                    select start + val + end;

        public static readonly Parser<string> StringVal = from start in Quote
                                                          from val in EscapedString
                                                          from end in Quote
                                                          select val.Unescape();

        private static readonly Dictionary<string, string> ValidEscapes = new Dictionary<string, string>
        {
            { @"\0", "\0" },
            { @"\t", "\t" },
            { @"\n", "\n" },
            { @"\r", "\r" },
            { @"\""", "\"" },
        };

        private static string Unescape(this string raw)
        {
            var unescaped = Regex.Replace(raw, @"(?<!\\)\\[0tnr""]", m => ValidEscapes[m.Value]);
            var invalid = Regex.Match(unescaped, @"(?<!\\)\\[^\\]");
            if (invalid.Success)
            {
                throw new ParseException("Invalid Escape sequence: " + invalid.Value);
            }

            return unescaped.Replace("\\\\", "\\");
        }

        public static readonly Parser<string> Integral = from sign in Negative.Optional()
                                                         from number in Parse.Number
                                                         select sign.IsEmpty ? number : sign.Get() + number;

        public static readonly Parser<long> IntVal = Integral.Select(long.Parse);

        public static readonly Parser<double> FloatVal = from integral in Integral
                                                         from dot in KeySeparator
                                                         from val in Integral
                                                         select double.Parse(integral + "." + val);

        public static readonly Parser<bool> BoolVal = (from val in Parse.String("true").Or(Parse.String("false")).Text()
                                                       select bool.Parse(val)).AsToken();

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
                                                                 .Or(ArrayVal.Downcast());

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
            var arrayParser = realParser.BoundedSequence(ArrayOpen.AsRToken(), ArraySeparator.AsToken(true), ArrayClose.AsLToken(), true);

            return from elements in arrayParser
                   select elements.ToArray();
        }

        public static readonly Parser<string> Key = Parse.AnyChar.Except(WhitespaceOrEol.Or(Assign)).AtLeastOnce().Text();

        public static readonly Parser<KeyValue> KeyValue = from key in Key
                                                           from sep in Assign.AsToken()
                                                           from v in Value
                                                           select new KeyValue(key, v);

        public static readonly Parser<IEnumerable<KeyValue>> KeyValueList = KeyValue.Sequence(ItemSeparator.AsToken(true));

        public static readonly Parser<string> KeyGroupPart = Parse.AnyChar.Except(
            Whitespace
                .Or(KeyGroupOpen)
                .Or(KeyGroupClose)
                .Or(KeySeparator)).Many().Text();

        public static readonly Parser<string[]> KeyGroup = from keys in KeyGroupPart.BoundedSequence(KeyGroupOpen, KeySeparator, KeyGroupClose)
                                                           select keys.ToArray();


        public static readonly Parser<Section> Section = from path in KeyGroup.AsToken()
                                                         from values in
                                                             (from br in ItemSeparator.AsToken(true)
                                                              from val in KeyValueList
                                                              select val).Optional()
                                                         select new Section(path, values.GetOrDefault());

        public static readonly Parser<IEnumerable<Section>> SectionList = Section.Sequence(ItemSeparator.AsToken(true), true);

        public static readonly Parser<Document> Document = (from rootValues in KeyValueList.AsToken(true)
                                                            from sections in SectionList.AsToken(true)
                                                            select new Document(rootValues, sections)).End();
    }
}
