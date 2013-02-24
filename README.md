TOML .NET
=========

This is a sample parser in C# for Tom Preson-Werner's ([@mojombo][1]) TOML markup language, using C#'s `dynamic` support.

The code's not the prettiest, but it successfully parses the sample file.

[1]: http://github.com/mojombo

USAGE:
------

If you have your doc in a string variable, simply call the `ParseAsToml` extension method:

```c#
string toml = ...;
dynamic config = toml.ParseAsToml();
// Alternative:
dynamic config = Toml.ParseString(toml);
```

There are also extensions methods for `System.IO.FileInfo` and `System.IO.TextWriter` with the same signature as well:

```c#
var file = new FileInfo(...);
dynamic config = file.ParseAsToml();
// or
dynamic config = Toml.ParseFile(pathToFile);

using(var reader = File.OpenRead(...))
{
	dynamic config = reader.ParseAsToml();
    // or
    dynamic config = Toml.Parse(reader);
}
```

TODO:
-----
* Implement support for nested arrays
* Refactor, refactor, refactor
* More tests
* Package up as a nuget file