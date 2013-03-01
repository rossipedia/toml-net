namespace Toml.Tests
{
    using NUnit.Framework;

    using Sprache;

    using global::Toml.Parser;

    [TestFixture]
    public class GrammarErrorTests
    {
        [Test]
        public void TestErrors()
        {
            var input = @"[error]   if you didn't catch this, your parser is broken
string = ""Anything other than tabs, spaces and newline after a keygroup or key value pair has ended should produce an error unless it is a comment""   like this
array = [
            ""This might most likely happen in multiline arrays"",
            Like here,
            ""or here,
            and here""
            ]     End of array comment, forgot the #
number = 3.14  pi <--again forgot the #";
            Assert.Throws<ParseException>(() => TomlGrammar.Document.Parse(input));
        }
    }
}
