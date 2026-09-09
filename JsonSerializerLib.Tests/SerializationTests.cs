using JsonSerializerLib;
using Xunit;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}

public class Address
{
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Address HomeAddress { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class SerializationTests
{
    [Fact]
    public void Serializes_Primitives()
    {
        Assert.Equal("42", JsonSerializer.Serialize(42, indent: false));
        Assert.Equal("\"hi\"", JsonSerializer.Serialize("hi", indent: false));
        Assert.Equal("true", JsonSerializer.Serialize(true, indent: false));
        Assert.Equal("null", JsonSerializer.Serialize(null, indent: false));
        Assert.Equal("3.14", JsonSerializer.Serialize(3.14, indent: false));
    }

    [Fact]
    public void Serializes_Object()
    {
        var user = new User { Id = 1, Name = "John", IsActive = true };
        var json = JsonSerializer.Serialize(user, indent: false);
        Assert.Equal("{\"Id\":1,\"Name\":\"John\",\"IsActive\":true}", json);
    }

    [Fact]
    public void Serializes_Nested_Objects()
    {
        var customer = new Customer
        {
            Id = 1,
            Name = "Alice",
            HomeAddress = new Address { City = "NYC", Zip = "10001" },
            Tags = new List<string> { "vip", "beta" }
        };

        var json = JsonSerializer.Serialize(customer, indent: false);
        Assert.Contains("\"HomeAddress\":{\"City\":\"NYC\",\"Zip\":\"10001\"}", json);
        Assert.Contains("\"Tags\":[\"vip\",\"beta\"]", json);
    }

    [Fact]
    public void Serializes_Dictionary()
    {
        var dict = new Dictionary<string, object> { ["a"] = 1, ["b"] = "two" };
        var json = JsonSerializer.Serialize(dict, indent: false);
        Assert.Equal("{\"a\":1,\"b\":\"two\"}", json);
    }
}
