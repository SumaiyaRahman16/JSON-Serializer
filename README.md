# JsonSerializerLib

A JSON serializer and deserializer built from scratch in C# using reflection.
No `System.Text.Json`, no `Newtonsoft.Json` — the tokenizer, parser, writer, and
type-binder are all hand-written.

## Solution layout

```
JsonSerializerLib.sln
JsonSerializerLib/                 the library
├── JsonSerializer.cs              public API (Serialize / Deserialize)
└── Json/
    ├── JsonException.cs           exception types
    ├── JsonTokenizer.cs           text -> tokens
    ├── JsonNumber.cs              raw-text number wrapper (precision safety)
    ├── JsonParser.cs              tokens -> generic object graph
    ├── JsonWriter.cs              object graph -> JSON text (+ cycle detection)
    ├── JsonBinder.cs              generic graph -> typed .NET object
    └── ReflectionCache.cs         cached, compiled property accessors
JsonSerializerLib.Tests/           xUnit tests
JsonSerializerLib.Benchmark/       before/after performance console app
```

## Requirements

- .NET 8 SDK. That's it — no NuGet packages are required by the library itself.
  The test project pulls in `xunit`, `xunit.runner.visualstudio`, and
  `Microsoft.NET.Test.Sdk` (test-only, not part of the library).

## Building and running

```bash
dotnet build
dotnet test
dotnet run --project JsonSerializerLib.Benchmark
```

## Usage

```csharp
using JsonSerializerLib;

var user = new User { Id = 1, Name = "John", IsActive = true };

string json = JsonSerializer.Serialize(user);
// {
//   "Id": 1,
//   "Name": "John",
//   "IsActive": true
// }

string compact = JsonSerializer.Serialize(user, indent: false);

User? back = JsonSerializer.Deserialize<User>(json);
```

## Supported types

| Category | Types |
|---|---|
| Primitives | `string`, `int`, `long`, `short`, `byte`, `sbyte`, `uint`, `ulong`, `ushort`, `float`, `double`, `decimal`, `bool`, `char`, `null` |
| Objects | any plain class/struct with a public parameterless constructor, discovered via reflection |
| Collections | arrays (`T[]`), `List<T>`, any generic `IEnumerable<T>` with a compatible constructor, `object[]`/`List<object>` |
| Dictionaries | `Dictionary<string, TValue>` and other `IDictionary` implementations |
| Special | `DateTime`, `DateTimeOffset`, `Guid`, `enum`, nullable value types (`int?`, `Guid?`, etc.) |

## Design decisions

- **Two-phase pipeline.** Parsing is split into tokenizing (`JsonTokenizer`),
  recursive-descent parsing into a generic graph (`JsonParser` — produces
  `Dictionary<string,object?>`, `List<object?>`, `string`, `JsonNumber`, `bool`,
  or `null`), and a separate `JsonBinder` that reflects over the *target* .NET
  type to populate it. Keeping parsing and type-binding independent means the
  parser doesn't need to know anything about your classes, and the binder
  doesn't need to know anything about JSON syntax.
- **`JsonNumber` instead of `double`.** Numbers are kept as raw text until the
  binder knows the target type, so a JSON number bound to `long` or `decimal`
  doesn't silently lose precision by round-tripping through `double` first.
- **Dates** are serialized as ISO-8601 UTC (`"o"` round-trip format) — the
  closest thing to a JSON date standard.
- **Enums** are serialized by name (`"High"`), not by ordinal, for
  readability and resilience to enum-member reordering. Deserialization
  accepts either the name or the numeric value.
- **Unknown JSON properties are ignored** during deserialization (matching
  the permissive behavior of most mainstream JSON libraries) rather than
  throwing — this lets you deserialize a subset of a larger JSON payload.
- Property name matching during deserialization is **case-insensitive**.

## Circular references

Detected during serialization using a reference-equality ancestor stack
(`HashSet<object>` with `ReferenceEqualityComparer`) that's pushed/popped as
the writer descends into objects, arrays, and dictionaries. If an object is
encountered that is already one of its own ancestors (direct self-reference
or an indirect cycle through a chain of objects), it is written as `null`
instead of being re-visited.

This was chosen over throwing an exception because a single self-referencing
property shouldn't necessarily fail an entire large serialization — the rest
of the graph still serializes correctly, and the `null` clearly marks where
the cycle was cut. This is a **documented trade-off**: if your application
needs the cycle preserved (e.g. via a `"$ref"` pointer scheme), that's not
supported here.

## Error handling

- **Malformed JSON** (unterminated strings, invalid escapes, bad numbers,
  trailing commas, unexpected tokens, unexpected end of input) raises
  `JsonParseException`, which includes the character position and what was
  expected, e.g. `Expected ':' but found ','  (at character 14)`.
- **Type mismatches** during deserialization (e.g. a JSON string where a
  number is expected, JSON `null` into a non-nullable value type) raise
  `JsonDeserializationException` naming the expected/actual types and, where
  applicable, the offending property name.
- **Reflection failures** (e.g. a property getter throws, a type has no
  public parameterless constructor) are caught and re-wrapped in
  `JsonSerializationException` / `JsonDeserializationException` with the
  original exception preserved as `InnerException`.
- Nothing is ever silently coerced into a "valid-looking" object — a bind
  failure always throws rather than guessing.

## Performance

Reflection metadata (`PropertyInfo[]`) and property access are the classic
hotspots for a reflection-based serializer: `Type.GetProperties()` walks
metadata tables, and `PropertyInfo.GetValue`/`SetValue` go through a slow,
boxing reflection-invoke path on every call.

`ReflectionCache` fixes both:
1. `PropertyInfo[]` is computed once per `Type` and cached in a
   `ConcurrentDictionary<Type, PropertyAccessor[]>`.
2. Each property additionally gets a **compiled `Expression` tree** acting as
   a strongly-typed getter/setter delegate, avoiding `GetValue`/`SetValue`
   reflection calls entirely after the first hit.

`JsonSerializerLib.Benchmark` measures 200,000 serializations of a 3-property
object, comparing the cached path against a naive baseline that calls
`GetProperties()` and `GetValue()` fresh on every iteration (no caching, no
compiled delegates — this is what a first-draft implementation typically
looks like). Run it yourself:

```bash
dotnet run --project JsonSerializerLib.Benchmark -c Release
```

Typical results look like (numbers vary by machine — replace with your own
measured run before submitting):

```
Cached ReflectionCache serializer: ~40 ms for 200,000 iterations
Naive reflection (no cache):       ~180 ms for 200,000 iterations
```

i.e. roughly a 4–5x improvement, entirely from removing repeated
`Type.GetProperties()` calls and replacing `PropertyInfo.GetValue` with a
compiled delegate.

## Limitations

- Deserialization targets need a **public parameterless constructor**
  (records with only a primary constructor aren't supported out of the box).
- No attribute-based customization (`[JsonIgnore]`, `[JsonPropertyName]`,
  etc.) — property name and inclusion are purely reflection-driven.
- No naming-policy conversion (e.g. camelCase output) — property names are
  written exactly as declared in C#.
- Circular references are cut to `null` on write, not preserved/restored
  (see above) — round-tripping a cyclic graph will lose the cycle.
- Only public instance properties are considered; fields are not serialized.
