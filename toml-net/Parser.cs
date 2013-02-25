// Parser framework taken from: 
// http://blogs.msdn.com/b/lukeh/archive/2007/08/19/monadic-parser-combinators-using-c-3-0.aspx

// ReSharper disable CheckNamespace
namespace Toml.Parser
// ReSharper restore CheckNamespace
{
    using System;
    using System.Globalization;
    using System.Linq;

    using Sprache;

    public static class TomlGrammar
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

        public static readonly Parser<string> WhitespaceOrEol = Whitespace.Or(Eol).Many().Text();

        public static readonly Parser<string> Comment = (from start in Parse.Char('#')
                                                         from comment in Parse.AnyChar.Except(Eol).Many()
                                                         select comment).Text();

        public static readonly Parser<string> Integral = from sign in Parse.Char('-').Optional()
                                                         from number in Parse.Number
                                                         select sign.IsEmpty ? number : sign.Get() + number;

        public static readonly Parser<long> IntVal = Integral.Select(long.Parse);

        public static readonly Parser<double> FloatVal = from integral in Integral
                                                         from dec in
                                                             (from dot in Parse.Char('.')
                                                              from val in IntVal
                                                              select val)
                                                         select double.Parse(integral + "." + dec);


        public static readonly Parser<bool> BoolVal = from val in Parse.String("true").Or(Parse.String("false")).Text()
                                                      select bool.Parse(val);


        public static readonly Parser<DateTime> DateTimeVal = from val in Parse.Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z")
                                                              select DateTime.ParseExact(
                                                                  val,
                                                                  "yyyy-MM-ddTHH:mm:ssZ",
                                                                  CultureInfo.InvariantCulture,
                                                                  DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        public static readonly Parser<object> Value = IntVal.Select(l => (object)l);

        public static Parser<object[]> IntArrayVal = ArrayParser(IntVal);

        public static Parser<object[]> FloatArrayVal = ArrayParser(FloatVal);

        public static Parser<object[]> BoolArrayVal = ArrayParser(BoolVal);

        public static Parser<object[]> DateTimeArrayVal = ArrayParser(DateTimeVal);

        public static Parser<object[]> ArrayVal = IntArrayVal.DownCast()
                                                    .Or(FloatArrayVal.DownCast())
                                                    .Or(BoolArrayVal.DownCast())
                                                    .Or(DateTimeArrayVal.DownCast());

        private static Parser<object[]> DownCast<T>(this Parser<T[]> p)
        {
            return p.Select(a => a.Cast<object>().ToArray());
        }

        public static Parser<object[]> ArrayParser<T>(Parser<T> valParser)
        {
            var realParser = valParser.Select(i => (object)i).Or(Parse.Ref(() => ArrayVal));

            return from open in WsChar('[')
                   from s1 in WhitespaceOrEol
                   from head in realParser
                   from elem in
                       (from sep in WsChar(',')
                        from s2 in WhitespaceOrEol
                        from val in realParser
                        select val).Many()
                   from final in WsChar(',').Optional()
                   from br in Eol.Many()
                   from close in WsChar(']')
                   select new[] { head }.Concat(elem).ToArray();
        }
    }
}
