namespace Toml.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using NUnit.Framework;

    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void NonNullStringShouldReturnNonNullConfig()
        {
            var config = string.Empty.ParseAsToml();
            Assert.NotNull(config);
        }

        [Test]
        public void ShouldParseKeyGroup()
        {
            var config = "[test]".ParseAsToml();

            Assert.NotNull(config.test);
        }

        [Test]
        public void ShouldParseNestedKeyGroup()
        {
            var config = "[foo.bar]".ParseAsToml();

            Assert.NotNull(config.foo);
            Assert.NotNull(config.foo.bar);
        }

        [Test]
        public void ShouldParseMultipleKeyGroups()
        {
            var config = "[foo]\n[bar]".ParseAsToml();

            Assert.NotNull(config.foo);
            Assert.NotNull(config.bar);
        }

        [Test]
        public void ShouldParseIntValue()
        {
            var config = "foo=1".ParseAsToml();

            Assert.AreEqual(1, config.foo);
            Assert.IsInstanceOf<long>(config.foo);
        }

        [Test]
        public void ShouldParseNegIntValue()
        {
            var config = "foo=-1".ParseAsToml();

            Assert.AreEqual(-1, config.foo);
        }

        [Test]
        public void ShouldParseFloatValue()
        {
            var config = "foo=1.23".ParseAsToml();

            Assert.AreEqual(1.23, config.foo);
            Assert.IsInstanceOf<double>(config.foo);
        }

        [Test]
        public void ShouldParseNegFloatValue()
        {
            var config = "foo=-1.23".ParseAsToml();

            Assert.AreEqual(-1.23, config.foo);
        }

        [Test]
        public void ShouldParseTrueValue()
        {
            var config = "foo=true".ParseAsToml();

            Assert.IsTrue(config.foo);
        }

        [Test]
        public void ShouldParseFalseValue()
        {
            var config = "foo=false".ParseAsToml();

            Assert.IsFalse(config.foo);
        }

        [Test]
        public void ShouldParseSimpleStringValue()
        {
            var config = "foo=\"bar\"".ParseAsToml();

            Assert.AreEqual("bar", config.foo);
        }

        [Test]
        public void ShouldParseStringValueWithEscapedChars()
        {
            var config = "foo=\"Line1\\nLine2\\tFoo \\\"Bar\\\"\"".ParseAsToml();

            Assert.AreEqual("Line1\nLine2\tFoo \"Bar\"", config.foo);
        }

        [Test]
        public void ShouldParseZuluDateTime()
        {
            var config = "foo=2013-02-24T01:13:00Z".ParseAsToml();

            Assert.AreEqual(new DateTime(2013, 02, 24, 01, 13, 00, DateTimeKind.Utc), config.foo);
        }

        [Test]
        public void ShouldParseArrayOfInt()
        {
            var config = "foo=[1, 2, 3]".ParseAsToml();

            Assert.IsTrue(new object[] { 1L, 2L, 3L }.SequenceEqual((object[])config.foo));
        }

        [Test]
        public void ShouldParseArrayOfFloat()
        {
            var config = "foo=[1.1, 2.2, 3.3]".ParseAsToml();

            Assert.IsTrue(new object[] { 1.1, 2.2, 3.3 }.SequenceEqual((object[])config.foo));
        }

        [Test]
        public void ShouldParseArrayOfString()
        {
            var config = "foo=[\"foo\", \"bar\"]".ParseAsToml();

            Assert.IsTrue(new object[] { "foo", "bar" }.SequenceEqual((object[])config.foo));
        }


        [Test]
        public void ShouldParseArrayOfBool()
        {
            var config = "foo=[true, false]".ParseAsToml();

            Assert.IsTrue(new object[] { true, false }.SequenceEqual((object[])config.foo));
        }

        [Test]
        public void ShouldParseArrayOfDateTime()
        {
            var config = "foo=[2013-02-24T01:13:00Z, 2013-01-30T22:30:15Z]".ParseAsToml();

            var expected = new object[]
            {
                new DateTime(2013, 02, 24, 01, 13, 00, DateTimeKind.Utc), 
                new DateTime(2013, 01, 30, 22, 30, 15, DateTimeKind.Utc)
            };
            Assert.IsTrue(expected.SequenceEqual((object[])config.foo));
        }

        [Test]
        public void ShouldIgnoreComments()
        {
            var config = "#foo\n[bob]#bar".ParseAsToml();
            Assert.NotNull(config);
            Assert.NotNull(config.bob);
        }

        [Test]
        public void ShouldParseSampleConfig()
        {
            var config = Resources.LoadEmbeddedSampleTomlFile().ParseAsToml();

            Assert.NotNull(config);
            Assert.AreEqual("TOML Example", config.title);

            Assert.NotNull(config.owner);
            Assert.AreEqual("Tom Preston-Werner", config.owner.name);
            Assert.AreEqual("GitHub", config.owner.organization);
            Assert.AreEqual("GitHub Cofounder & CEO\nLikes tater tots and beer.", config.owner.bio);
            Assert.AreEqual(new DateTime(1979, 05, 27, 07, 32, 00, DateTimeKind.Utc), config.owner.dob);

            Assert.NotNull(config.database);
            Assert.AreEqual("192.168.1.1", config.database.server);
            Assert.IsTrue(new object[] { 8001L, 8001L, 8002L }.SequenceEqual((object[])config.database.ports));
            Assert.AreEqual(5000, config.database.connection_max);
            Assert.IsTrue((bool)config.database.enabled);

            Assert.NotNull(config.servers);

            Assert.NotNull(config.servers.alpha);
            Assert.AreEqual("10.0.0.1", config.servers.alpha.ip);
            Assert.AreEqual("eqdc10", config.servers.alpha.dc);

            Assert.NotNull(config.servers.beta);
            Assert.AreEqual("10.0.0.2", config.servers.beta.ip);
            Assert.AreEqual("eqdc10", config.servers.beta.dc);
        }

        [Test]
        public void ShouldParseHardConfig()
        {
            var config = Resources.LoadEmbeddedHardTomlFile().ParseAsToml();
            Assert.NotNull(config);

            Assert.AreEqual("You'll hate me after this - #", config.the.test_string);
            Assert.NotNull(config.the.hard);
            Assert.IsTrue(new object[] { "] ", " # " }.SequenceEqual((object[])config.the.hard.test_array));
            Assert.IsTrue(new object[] { "Test #11 ]proved that", "Experiment #9 was a success" }.SequenceEqual((object[])config.the.hard.test_array2));
            Assert.AreEqual(" Same thing, but with a string #", config.the.hard.another_test_string);
            Assert.AreEqual(" And when \"'s are in the string, along with # \"", config.the.hard.harder_test_string);
            var dict = (IDictionary<string, object>)config.the.hard;
            Assert.NotNull(dict["bit#"]);
            dynamic thehardbit = dict["bit#"];
            var harddict = (IDictionary<string, object>)thehardbit;
            Assert.AreEqual(harddict["what?"], "You don't think some user won't do that?");

            Assert.IsTrue(new object[] { "]" }.SequenceEqual((object[])thehardbit.multi_line_array));
        }

        [Test]
        public void DuplicateKeysShouldThrow()
        {
            var config = "[test]\nfoo=\"bar\"\n[test.foo]\nshaz=0";
            Assert.Throws<FormatException>(() => config.ParseAsToml());
        }
    }
}
