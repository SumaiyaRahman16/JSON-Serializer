# JsonSerializerLib

A custom JSON serializer and deserializer built from scratch in C# using reflection.

It does not use `System.Text.Json`, `Newtonsoft.Json`, or any third-party JSON library for the actual serialization and deserialization.

## Project Structure

```text
JsonSerializerLib/
├── JsonSerializer.cs       # Main public API
└── Json/
    ├── JsonTokenizer.cs    # Breaks JSON into tokens
    ├── JsonParser.cs       # Parses JSON
    ├── JsonWriter.cs       # Creates JSON text
    ├── JsonBinder.cs       # Converts JSON to C# objects
    ├── JsonNumber.cs       # Handles JSON numbers
    ├── JsonException.cs    # Custom exceptions
    └── ReflectionCache.cs  # Improves reflection performance

JsonSerializerLib.Tests/    # Unit tests
JsonSerializerLib.Benchmark/# Performance tests
```

## Requirements

* .NET 8 SDK
* No external libraries are required for the main serializer.

## How to Run

Build the project:

```bash
dotnet build
```

Run the tests:

```bash
dotnet test
```

Run the benchmark:

```bash
dotnet run --project JsonSerializerLib.Benchmark -c Release
```

## Basic Usage

```csharp
using JsonSerializerLib;

var user = new User
{
    Id = 1,
    Name = "John",
    IsActive = true
};

string json = JsonSerializer.Serialize(user);

User? result = JsonSerializer.Deserialize<User>(json);
```

## Supported Features

* Primitive types such as `string`, `int`, `double`, `bool`, etc.
* Classes and structs using reflection
* Nested objects
* Arrays and lists
* Dictionaries
* `DateTime` and `DateTimeOffset`
* `Guid`
* Enums
* Nullable types
* Serialization and deserialization
* Invalid JSON and type-mismatch error handling
* Circular reference detection

## Design

The serializer uses a simple pipeline:

```text
C# Object
    ↓
JsonWriter
    ↓
JSON Text
```

For deserialization:

```text
JSON Text
    ↓
Tokenizer
    ↓
Parser
    ↓
JsonBinder
    ↓
C# Object
```

Reflection is used to automatically read and create object properties, so classes do not need special serialization code.

A reflection cache is also used to avoid repeatedly looking up the same property information, improving performance.

## Circular References

Circular references are detected during serialization to prevent infinite loops.

For example:

```text
Person → Friend → Person
```

When a cycle is detected, that value is written as `null`.

## Limitations

* Only public instance properties are serialized.
* Deserialization requires a public parameterless constructor.
* Fields are not serialized.
* Custom attributes such as `[JsonIgnore]` are not supported.
* Circular references are replaced with `null`.

## Performance

The project includes a benchmark comparing normal reflection with the optimized `ReflectionCache` implementation.

The benchmark should be run before submission so the README can be updated with the actual results from the user's machine.
