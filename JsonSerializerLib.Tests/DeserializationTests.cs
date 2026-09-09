using JsonSerializerLib;
using Xunit;

public class DeserializationTests
{
    [Fact]
    public void Roundtrips_Simple_Object()
    {
        var user = new User { Id = 1, Name = "John", IsActive = true };
        var json = JsonSerializer.Serialize(user);
        var back = JsonSerializer.Deserialize<User>(json)!;
        Assert.Equal(user.Id, back.Id);
        Assert.Equal(user.Name, back.Name);
        Assert.Equal(user.IsActive, back.IsActive);
    }

    [Fact]
    public void Deserializes_List()
    {
        var list = JsonSerializer.Deserialize<List<int>>("[1,2,3]")!;
        Assert.Equal(new List<int> { 1, 2, 3 }, list);
    }

    [Fact]
    public void Deserializes_Array()
    {
        var arr = JsonSerializer.Deserialize<int[]>("[1,2,3]")!;
        Assert.Equal(new[] { 1, 2, 3 }, arr);
    }

    [Fact]
    public void Deserializes_Dictionary()
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, int>>("{\"a\":1,\"b\":2}")!;
        Assert.Equal(1, dict["a"]);
        Assert.Equal(2, dict["b"]);
    }

    [Fact]
    public void Deserializes_Nested_Objects()
    {
        const string json = "{\"Id\":1,\"Name\":\"Alice\",\"HomeAddress\":{\"City\":\"NYC\",\"Zip\":\"10001\"},\"Tags\":[\"vip\"]}";
        var customer = JsonSerializer.Deserialize<Customer>(json)!;
        Assert.Equal("NYC", customer.HomeAddress.City);
        Assert.Equal("vip", customer.Tags[0]);
    }

    [Fact]
    public void Deserializes_Guid_DateTime_Enum_Nullable()
    {
        var guid = Guid.NewGuid();
        var json = $"{{\"Id\":\"{guid}\",\"When\":\"2024-01-01T00:00:00Z\",\"Level\":\"High\",\"Score\":null}}";
        var result = JsonSerializer.Deserialize<Special>(json)!;
        Assert.Equal(guid, result.Id);
        Assert.Equal(2024, result.When.Year);
        Assert.Equal(Priority.High, result.Level);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Ignores_Unknown_Properties()
    {
        var user = JsonSerializer.Deserialize<User>("{\"Id\":1,\"Name\":\"X\",\"Extra\":\"ignored\"}")!;
        Assert.Equal(1, user.Id);
    }
}

public enum Priority { Low, Medium, High }

public class Special
{
    public Guid Id { get; set; }
    public DateTime When { get; set; }
    public Priority Level { get; set; }
    public int? Score { get; set; }
}
