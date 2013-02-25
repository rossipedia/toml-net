// ReSharper disable ConvertToConstant.Local
namespace Toml.Tests
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using Sprache;

    using Toml.Parser;

    [TestFixture]
    internal class GrammarTests
    {
        [Test]
        public void CanParseInt64()
        {
            var input = "123";
            var result = TomlGrammar.IntVal.Parse(input);
            Assert.AreEqual(123L, result);
        }

        [Test]
        public void CanParseFloat64()
        {
            var input = "123.456";
            var result = TomlGrammar.FloatVal.Parse(input);
            Assert.AreEqual(123.456, result);
        }

        [Test]
        public void CanParseNegativeInt64()
        {
            var input = "-123";
            var result = TomlGrammar.IntVal.Parse(input);
            Assert.AreEqual(-123L, result);
        }

        [Test]
        public void CanParseNegativeFloat64()
        {
            var input = "-123.456";
            var result = TomlGrammar.FloatVal.Parse(input);
            Assert.AreEqual(-123.456, result);
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
        public void CanParseSimpleArray()
        {
            var input = "[1, 2, 3]";
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.IsTrue(new[] { 1L, 2, 3 }.SequenceEqual(result.Cast<long>()));
        }

        [Test]
        public void CanParseArrayWithTrailingComma()
        {
            var input = "[1, 2, 3, ]";
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.IsTrue(new[] { 1L, 2, 3 }.SequenceEqual(result.Cast<long>()));
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
        public void CannotParseArrayWithDifferentTypes()
        {
            var input = "[1, 1.23]";
            Assert.Throws<ParseException>(() => TomlGrammar.ArrayVal.Parse(input));
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
            var result = TomlGrammar.ArrayVal.Parse(input);
            Assert.NotNull(result);
        }
    }
}
// ReSharper restore ConvertToConstant.Local