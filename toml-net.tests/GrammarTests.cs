namespace Toml.Tests
{
    using System;
    using System.IO;
    using System.Linq;

    using NUnit.Framework;

    using Sprache;

    using global::Toml.Parser;

    [TestFixture]
    internal class GrammarTests
    {
        [Test]
        public void CanParseArrayWithTrailingComma()
        {
            var input = "[1, 2, 3, ]";
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.IsTrue(new[] { 1L, 2, 3 }.SequenceEqual(result.Cast<long>()));
        }

        [Test]
        public void CanParseBoolValues()
        {
            var input = "true";
            var result = TomlGrammar.BoolVal.Parse(input);
            Assert.IsTrue(result);

            input = "false";
            result = TomlGrammar.BoolVal.Parse(input);
            Assert.IsFalse(result);
        }

        [Test]
        public void CanParseDateTimeValue()
        {
            var input = "1979-05-27T07:32:00Z";
            var result = TomlGrammar.DateTimeVal.Parse(input);
            Assert.AreEqual(new DateTime(1979, 05, 27, 07, 32, 00, DateTimeKind.Utc), result);
        }

        [Test]
        public void CanParseEmptyAsWhitespace()
        {
            var result = TomlGrammar.Whitespace.Many().Parse(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void CanParseEmptySection()
        {
            var input = "[foo]";
            var result = TomlGrammar.Section.Parse(input);
            Assert.IsNotNull(result.Path);
            Assert.IsNotNull(result.Values);
            Assert.IsEmpty(result.Values);
        }

        [Test]
        public void CanParseEmptyStringAsDocument()
        {
            var input = string.Empty;
            var result = TomlGrammar.Document.Parse(input);
            Assert.NotNull(result);
        }

        [Test]
        public void CanParseFloat64()
        {
            var input = "123.456";
            var result = TomlGrammar.FloatVal.Parse(input);
            Assert.AreEqual(123.456, result);
        }

        [Test]
        public void CanParseInt64()
        {
            var input = "123";
            var result = TomlGrammar.IntVal.Parse(input);
            Assert.AreEqual(123L, result);
        }

        [Test]
        public void CanParseItemSeparator()
        {
            var input = "#foo \n   \n\n";
            var result = TomlGrammar.ItemSeparator.Text().Parse(input);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void CanParseSequenceWithItemSeparator()
        {
            var input = "\n\nfoo\n  \n  \t\nbar\n\n";
            var item = Parse.String("foo").Or(Parse.String("bar")).Text();
            var parser = from b in TomlGrammar.ItemSeparator.Many()
                         from i in item.Sequence(TomlGrammar.ItemSeparator.AtLeastOnce(), true)
                         select i;

            var result = parser.End().Parse(input).ToArray();
            Assert.IsTrue(result.SequenceEqual(new[] { "foo", "bar" }));
        }

        [Test]
        public void CanParseSingleEolAsItemSeparator()
        {
            var input = "\n";
            var result = TomlGrammar.ItemSeparator.Text().Parse(input);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void CanParseKey()
        {
            var input = "foo";
            var result = TomlGrammar.Key.Parse(input);
            Assert.AreEqual("foo", result);
        }

        [Test]
        public void CanParseKeyGroup()
        {
            var input = "[foo]";
            var result = TomlGrammar.KeyGroup.Parse(input).ToList();
            Assert.IsNotEmpty(result);
            Assert.AreEqual("foo", result[0]);
        }

        [Test]
        public void CanParseKeyGroupPart()
        {
            var input = "foo";
            var result = TomlGrammar.KeyGroupPart.Parse(input).ToList();
            Assert.AreEqual(input, result);
        }

        [Test]
        public void CanParseKeyValue()
        {
            var input = "foo = 1";
            var result = TomlGrammar.KeyValue.Parse(input);
            Assert.AreEqual("foo", result.Key);
            Assert.AreEqual(1, result.Value);
        }

        [Test]
        public void CanParseKeyValueWithFloat()
        {
            var input = "foo = 1.23";
            var result = TomlGrammar.KeyValue.Parse(input);
            Assert.AreEqual("foo", result.Key);
            Assert.AreEqual(1.23, result.Value);
        }

        [Test]
        public void CanParseKeyValueWithNoSpace()
        {
            var input = "foo=1";
            var result = TomlGrammar.KeyValue.Parse(input);
            Assert.AreEqual("foo", result.Key);
            Assert.AreEqual(1, result.Value);
        }

        [Test]
        public void CanParseKeyValueWithString()
        {
            var input = "foo = \"bar\"";
            var result = TomlGrammar.KeyValue.Parse(input);
            Assert.AreEqual("foo", result.Key);
            Assert.AreEqual("bar", result.Value);
        }

        [Test]
        public void CanParseMultiLineArrays()
        {
            var input = "[\n1,\n2,\n3\n]";
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.NotNull(result);
        }

        [Test]
        public void CanParseMultiLineIndentedArrays()
        {
            var input = "[\n   1,\n\t2,\n   \t\t   3\n]";
            var result = TomlGrammar.IntArrayVal.Parse(input);
            Assert.NotNull(result);
        }

        [Test]
        public void CanParseNegativeFloat64()
        {
            var input = "-123.456";
            var result = TomlGrammar.FloatVal.Parse(input);
            Assert.AreEqual(-123.456, result);
        }

        [Test]
        public void CanParseNegativeInt64()
        {
            var input = "-123";
            var result = TomlGrammar.IntVal.Parse(input);
            Assert.AreEqual(-123L, result);
        }

        [Test]
        public void CanParseNestedArrayOfDiffType()
        {
            var input = "[1, 2, [\"foo\", \"bar\"]]";
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);

            Assert.IsInstanceOf<Array>(result[2]);
            Assert.IsTrue(
                new[] { "foo", "bar" }.SequenceEqual(((object[])result[2]).Cast<string>()));
        }

        [Test]
        public void CanParseNestedArrayOfSameType()
        {
            var input = "[1, 2, [3, 4]]";
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);

            Assert.IsInstanceOf<Array>(result[2]);
            Assert.IsTrue(new[] { 3L, 4 }.SequenceEqual(((object[])result[2]).Cast<long>()));
        }

        [Test]
        public void CanParseNestedKeyGroup()
        {
            var input = "[foo.bar]";
            var result = TomlGrammar.KeyGroup.Parse(input).ToList();
            Assert.IsNotEmpty(result);
            Assert.AreEqual("foo", result[0]);
            Assert.AreEqual("bar", result[1]);
        }

        [Test]
        public void CanParseSampleDocument()
        {
            var input = Resources.LoadEmbeddedSampleTomlFile();
            var result = TomlGrammar.Document.Parse(input);
            Assert.NotNull(result);
        }

        [Test]
        public void CanParsHARDMODE_DUNDUNDUN()
        {
            var input = Resources.LoadEmbeddedHardTomlFile();
            var result = TomlGrammar.Document.Parse(input);
            Assert.NotNull(result);
        }

        [Test]
        public void CanParseSection()
        {
            var input = "[foo]\nbar=1 #foo\n\n#test\nbar=1";
            var result = TomlGrammar.Section.End().Parse(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Path);
            Assert.NotNull(result.Values);
            Assert.AreEqual(2, result.Values.Length);
            Assert.AreEqual("bar", result.Values[0].Key);
            Assert.AreEqual(1, result.Values[0].Value);
            Assert.AreEqual("bar", result.Values[1].Key);
            Assert.AreEqual(1, result.Values[1].Value);
        }

        [Test]
        public void CanParseSingleSectionAsDocument()
        {
            var input = "[foo]\nbar=1 #foo\n\n#test\nbar=1";
            var result = TomlGrammar.Document.Parse(input);
            Assert.NotNull(result);
        }

        [Test]
        public void CanParseSectionList()
        {
            var input = @"[foo]
bar=1
[shaz]
bot=false";
            var result = TomlGrammar.SectionList.End().Parse(input).ToArray();
            Assert.NotNull(result);
            Assert.AreEqual(2, result.Length);

            Assert.AreEqual("foo", result[0].Path[0]);
            Assert.AreEqual("bar", result[0].Values[0].Key);
            Assert.AreEqual(1, result[0].Values[0].Value);

            Assert.AreEqual("shaz", result[1].Path[0]);
            Assert.AreEqual("bot", result[1].Values[0].Key);
            Assert.IsFalse((bool)result[1].Values[0].Value);
        }

        [Test]
        public void CanParseSectionListAsToken()
        {
            var input = @"  #foo
[foo]
bar=1 #foo bar

[shaz]
bot=false";
            var result = TomlGrammar.SectionList.AsToken(true).End().Parse(input).ToArray();
            Assert.NotNull(result);
            Assert.AreEqual(2, result.Length);

            Assert.AreEqual("foo", result[0].Path[0]);
            Assert.AreEqual("bar", result[0].Values[0].Key);
            Assert.AreEqual(1, result[0].Values[0].Value);

            Assert.AreEqual("shaz", result[1].Path[0]);
            Assert.AreEqual("bot", result[1].Values[0].Key);
            Assert.IsFalse((bool)result[1].Values[0].Value);
        }

        [Test]
        public void CanParseSimpleArray()
        {
            var input = "[1, 2, 3]";
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.IsTrue(new[] { 1L, 2, 3 }.SequenceEqual(result.Cast<long>()));
        }

        [Test]
        public void CanParseSimpleDocument()
        {
            var input = @"#testing
root = ""foo""

[key1]
foo = ""bar""  #test

  [key2]
  alpha = ""omega""
";
            var result = TomlGrammar.Document.Parse(input);
            Assert.NotNull(result);
        }

        [Test]
        public void CanParseSingleKeyGroupAsDocument()
        {
            var input = "[foo]";
            var result = TomlGrammar.Document.Parse(input);
            Assert.NotNull(result);
            Assert.AreEqual(1, result.Sections.Length);
            Assert.AreEqual("foo", result.Sections[0].Path[0]);
        }

        [Test]
        public void CanParseSingleKeyValueAsDocument()
        {
            var input = "\n\nfoo = 1";
            var result = TomlGrammar.Document.Parse(input);
            Assert.NotNull(result);
            Assert.IsEmpty(result.Sections);
            Assert.IsNotEmpty(result.RootValues);
            Assert.AreEqual(1, result.RootValues.Length);
            Assert.AreEqual("foo", result.RootValues[0].Key);
            Assert.AreEqual(1, result.RootValues[0].Value);
        }

        [Test]
        public void CanParseString()
        {
            var input = "\"foo\"";
            var result = TomlGrammar.StringVal.Parse(input);
            Assert.AreEqual("foo", result);
        }

        [Test]
        public void CanParseStringWithEmbeddedComment()
        {
            var input = "\"foo #bar\"";
            var result = TomlGrammar.StringVal.Parse(input);
            Assert.AreEqual("foo #bar", result);
        }

        [Test]
        public void CanParseStringWithEscapedChars()
        {
            var input = "\"foo \\\"\\0\\t\\n\\r\\\\\"";
            var result = TomlGrammar.StringVal.Parse(input);
            Assert.AreEqual("foo \"\0\t\n\r\\", result);
        }

        [Test]
        public void CanParseStringWithEscapedQuotes()
        {
            var input = "\"foo \\\"bar\\\" fizz \\\"buzz\\\"\"";
            var result = TomlGrammar.StringVal.Parse(input);
            Assert.AreEqual("foo \"bar\" fizz \"buzz\"", result);
        }

        [Test]
        public void CanParseWhitespace()
        {
            var input = "\t\t\t   ";
            var result = TomlGrammar.Whitespace.Many().Parse(input);
            Assert.AreEqual(input, result);
        }

        [Test]
        public void CannotParseArrayWithDifferentTypes()
        {
            var input = "[1, 1.23]";
            Assert.Throws<ParseException>(() => TomlGrammar.ArrayVal.Parse(input));
        }

        [Test]
        public void LTokenDoesntConsumeWhitespaceAfterToken()
        {
            var parser = from t in TomlGrammar.ArrayOpen.AsLToken()
                         from c in Parse.AnyChar
                         select new { t, c };

            var input = "\r\n\t [\r\n\t foo";
            var result = parser.Parse(input);
            Assert.AreEqual('[', result.t);
            Assert.AreEqual('\r', result.c);
        }

        [Test]
        public void RTokenRemovesComments()
        {
            var parser = TomlGrammar.ArrayOpen.AsRToken();
            var input = "[ #foobar";
            var result = parser.Parse(input);
            Assert.AreEqual('[', result);
        }

        [Test]
        public void LTokenRemovesComments()
        {
            var parser = TomlGrammar.ArrayOpen.AsLToken();
            var input = "#foobar\n[";
            var result = parser.Parse(input);
            Assert.AreEqual('[', result);
        }
        
        [Test]
        public void TokenParseStripsWhitespace()
        {
            var input = "\t\t\t    foo   \t\t  \t\t  ";
            var result =
                Parse.AnyChar.Except(TomlGrammar.Whitespace).Many().Text().AsToken().Parse(input);
            Assert.AreEqual("foo", result);
        }

        [Test]
        public void TokenParseStripsWhitespaceAndTrailingComment()
        {
            var input = "\t\t\t    foo   \t\t  #foo ";
            var result =
                Parse.AnyChar.Except(TomlGrammar.Whitespace).Many().Text().AsToken().Parse(input);
            Assert.AreEqual("foo", result);
        }

        [Test]
        public void TokenParseStripsWhitespaceAndComments()
        {
            var input = "#foo\nbar\n#baz";
            var result =
                Parse.AnyChar.Except(TomlGrammar.WhitespaceOrEol).Many().Text().AsToken(true).Parse(input);
            Assert.AreEqual("bar", result);
        }

        [Test]
        public void TwoLTokenParsersSkipLeadingWhitespace()
        {
            var parser = from t in Parse.Char('[').AsLToken()
                         from c in Parse.AnyChar.AsLToken()
                         select new { t, c };

            var input = "\r\n\t [\r\n\t foo";
            var result = parser.Parse(input);
            Assert.AreEqual('[', result.t);
            Assert.AreEqual('f', result.c);
        }

        [Test]
        public void CanParseSequence()
        {
            var elem = Parse.Regex(@"\w+").AsToken();
            var delim = Parse.Char(',');

            var input = "this, is, a, test";
            var result = elem.Sequence(delim).Parse(input).ToArray();
            Assert.AreEqual(4, result.Length);
            Assert.IsTrue(result.SequenceEqual(new[] { "this", "is", "a", "test" }));
        }

        [Test]
        public void CanParseSequenceWithTrailing()
        {
            var elem = TomlGrammar.IntVal;
            var input = "1,2,3,4,";
            var result = elem.Sequence(TomlGrammar.ArraySeparator, true).Parse(input).ToArray();
            Assert.AreEqual(4, result.Length);
            Assert.IsTrue(result.SequenceEqual(new[] { 1L, 2, 3, 4 }));
        }

        [Test]
        public void CanParseBoundedSequence()
        {
            var elem = TomlGrammar.IntVal.AsToken();
            var delim = TomlGrammar.ArraySeparator.AsToken();
            var open = TomlGrammar.ArrayOpen.AsRToken();
            var close = TomlGrammar.ArrayClose.AsRToken();

            var result = elem.BoundedSequence(open, delim, close).Parse("[1, 2, 3, 4]").ToArray();
            Assert.AreEqual(4, result.Length);
            Assert.IsTrue(result.SequenceEqual(new[] { 1L, 2, 3, 4 }));
        }

        [Test]
        public void CanParseBoundedSequenceWithTrailing()
        {
            var elem = TomlGrammar.IntVal.AsToken();
            var delim = TomlGrammar.ArraySeparator.AsToken();
            var open = TomlGrammar.ArrayOpen.AsRToken();
            var close = TomlGrammar.ArrayClose.AsRToken();

            var result = elem.BoundedSequence(open, delim, close, true).Parse("[1, 2, 3, 4, ]").ToArray();
            Assert.AreEqual(4, result.Length);
            Assert.IsTrue(result.SequenceEqual(new[] { 1L, 2, 3, 4 }));
        }

        [Test]
        public void CanParseSequenceWithLines()
        {
            var elem = Parse.Regex(@"\w+").AsToken();
            var delim = Parse.Char(',').AsToken(true);

            var input = @"this, 
is, 
a, 
test";
            var result = elem.Sequence(delim).Parse(input).ToArray();
            Assert.AreEqual(4, result.Length);
            Assert.IsTrue(result.SequenceEqual(new[] { "this", "is", "a", "test" }));
        }
    }
}
