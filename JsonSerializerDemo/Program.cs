// using System;
// using JsonSerializerLib;
//
// namespace JsonSerializerDemo;
//
// class Program
// {
//     static void Main(string[] args)
//     {
//         Console.WriteLine("JSON Serializer Test");
//
//         var user = new User
//         {
//             Id = 1,
//             Name = "John",
//             IsActive = true
//         };
//
//         string json = JsonSerializer.Serialize(user);
//
//         Console.WriteLine(json);
//     }
// }
//
// public class User
// {
//     public int Id { get; set; }
//     public string Name { get; set; }
//     public bool IsActive { get; set; }
// }

using System;
using System.Collections.Generic;
using System.Linq;
using JsonSerializerLib;

namespace JsonSerializerDemo;

class Program
{
    static void Main()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("       JSON SERIALIZER DEMO");
        Console.WriteLine("========================================");

        TestPrimitiveValues();
        TestSimpleObject();
        TestNestedObject();
        TestCollections();
        TestDictionary();
        TestSpecialTypes();
        TestDeserialization();
        TestNullableAndEnum();
        TestObjectDeserialization();
        TestErrorHandling();
        TestCircularReference();

        Console.WriteLine("\n========================================");
        Console.WriteLine("           DEMO COMPLETED");
        Console.WriteLine("========================================");
    }

    // 1. Primitive values
    static void TestPrimitiveValues()
    {
        Console.WriteLine("\n[1] Primitive Values");
        Console.WriteLine(JsonSerializer.Serialize("Hello"));
        Console.WriteLine(JsonSerializer.Serialize(42));
        Console.WriteLine(JsonSerializer.Serialize(3.14));
        Console.WriteLine(JsonSerializer.Serialize(true));
        Console.WriteLine(JsonSerializer.Serialize(null));
    }

    // 2. Simple object
    static void TestSimpleObject()
    {
        Console.WriteLine("\n[2] Simple Object");

        var user = new User
        {
            Id = 1,
            Name = "John",
            IsActive = true
        };

        string json = JsonSerializer.Serialize(user);
        Console.WriteLine(json);
    }

    // 3. Nested objects
    static void TestNestedObject()
    {
        Console.WriteLine("\n[3] Nested Object");

        var user = new User
        {
            Id = 2,
            Name = "Alice",
            IsActive = true,
            Address = new Address
            {
                City = "Dhaka",
                Country = "Bangladesh"
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(user));
    }

    // 4. Arrays, List<T>, IEnumerable<T>
    static void TestCollections()
    {
        Console.WriteLine("\n[4] Collections");

        int[] numbers = { 10, 20, 30 };

        var names = new List<string>
        {
            "Alice",
            "Bob",
            "Charlie"
        };

        IEnumerable<int> scores = new[] { 80, 90, 95 };

        Console.WriteLine("Array:");
        Console.WriteLine(JsonSerializer.Serialize(numbers));

        Console.WriteLine("List:");
        Console.WriteLine(JsonSerializer.Serialize(names));

        Console.WriteLine("IEnumerable:");
        Console.WriteLine(JsonSerializer.Serialize(scores));
    }

    // 5. Dictionary
    static void TestDictionary()
    {
        Console.WriteLine("\n[5] Dictionary");

        var data = new Dictionary<string, object>
        {
            ["name"] = "John",
            ["age"] = 25,
            ["active"] = true,
            ["score"] = 95.5
        };

        Console.WriteLine(JsonSerializer.Serialize(data));
    }

    // 6. DateTime, Guid and Enum
    static void TestSpecialTypes()
    {
        Console.WriteLine("\n[6] Special Types");

        var special = new SpecialData
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.Now,
            Status = UserStatus.Active
        };

        Console.WriteLine(JsonSerializer.Serialize(special));
    }

    // 7. JSON -> primitive C# values
    static void TestDeserialization()
    {
        Console.WriteLine("\n[7] Primitive Deserialization");

        string intJson = "123";
        int number = JsonSerializer.Deserialize<int>(intJson);

        string boolJson = "true";
        bool flag = JsonSerializer.Deserialize<bool>(boolJson);

        string stringJson = "\"Hello World\"";
        string? text = JsonSerializer.Deserialize<string>(stringJson);

        Console.WriteLine($"int: {number}");
        Console.WriteLine($"bool: {flag}");
        Console.WriteLine($"string: {text}");
    }

    // 8. Nullable and Enum deserialization
    static void TestNullableAndEnum()
    {
        Console.WriteLine("\n[8] Nullable & Enum Deserialization");

        int? nullableNumber =
            JsonSerializer.Deserialize<int?>("100");

        UserStatus status =
            JsonSerializer.Deserialize<UserStatus>("\"Active\"");

        Console.WriteLine($"Nullable int: {nullableNumber}");
        Console.WriteLine($"Enum: {status}");
    }

    // 9. JSON -> C# object
    static void TestObjectDeserialization()
    {
        Console.WriteLine("\n[9] Object Deserialization");

        string json = """
        {
          "Id": 10,
          "Name": "Michael",
          "IsActive": true,
          "Address": {
            "City": "Sylhet",
            "Country": "Bangladesh"
          }
        }
        """;

        var user = JsonSerializer.Deserialize<User>(json);

        Console.WriteLine($"Id: {user?.Id}");
        Console.WriteLine($"Name: {user?.Name}");
        Console.WriteLine($"IsActive: {user?.IsActive}");
        Console.WriteLine($"City: {user?.Address?.City}");
        Console.WriteLine($"Country: {user?.Address?.Country}");
    }

    // 10. Error handling
    static void TestErrorHandling()
    {
        Console.WriteLine("\n[10] Error Handling");

        try
        {
            JsonSerializer.Deserialize<int>("\"not a number\"");
            Console.WriteLine("FAIL: Error was expected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PASS: Caught error -> {ex.Message}");
        }

        try
        {
            JsonSerializer.Deserialize<User>("{invalid json}");
            Console.WriteLine("FAIL: Error was expected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PASS: Caught error -> {ex.Message}");
        }
    }

    // 11. Circular reference
    static void TestCircularReference()
    {
        Console.WriteLine("\n[11] Circular Reference");

        var person1 = new Person
        {
            Name = "Person 1"
        };

        var person2 = new Person
        {
            Name = "Person 2"
        };

        person1.Friend = person2;
        person2.Friend = person1;

        string json = JsonSerializer.Serialize(person1);

        Console.WriteLine(json);
        Console.WriteLine("PASS: Circular reference handled.");
    }
}


// ============================================================
// Demo Models
// ============================================================

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public bool IsActive { get; set; }

    public Address? Address { get; set; }
}

public class Address
{
    public string City { get; set; } = "";

    public string Country { get; set; } = "";
}

public class SpecialData
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public UserStatus Status { get; set; }
}

public enum UserStatus
{
    Active,
    Inactive,
    Pending
}

public class Person
{
    public string Name { get; set; } = "";

    public Person? Friend { get; set; }
}