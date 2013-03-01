TOML .NET
=========

This is a sample parser in C# for Tom Preson-Werner's ([@mojombo][1]) TOML markup language, using C#'s `dynamic` support.

The code's not the prettiest, but it successfully parses the sample file.

This sample is based off of commit [#8a7c1bf27f][2] of Tom's TOML spec.

USAGE:
------

This library exposes two methods: the static `Toml.Parse(string)` and extension method `str.ParseAsToml()`.

Both return a `dynamic` (really an `ExpandoObject` underneath),
that is the hash produced by parsing the string.

See the [TOML spec][3] for clarification on this language.

EXAMPLE:
--------

With the following in `config.toml`:

``` toml
[database]
server = "192.168.1.1"
ports = [ 8001, 8001, 8002 ]
connection_max = 5000
enabled = true
```

You can access this config like so:


``` c#
var config = File.ReadAllText("config.toml");
string dbServer = config.database.server;
object[] ports = config.database.ports;
int maxConn = config.database.connection_max;
```

###Note:
Arrays have to be implemented as arrays of objects, because per the
spec they can contain not only single elements like an int or
DateTime, they can also hold other arrays of different types.


TODO:
-----
- [ ] Package up as a nuget file
- [ ] Fix up parser to support streaming



[1]: http://github.com/mojombo
[2]: https://github.com/mojombo/toml/commit/8a7c1bf27fa13b6c381b3bc806df7f5c0add95da
[3]: https://github.com/mojombo/toml
