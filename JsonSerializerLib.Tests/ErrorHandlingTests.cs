using JsonSerializerLib;
using Xunit;

public class ErrorHandlingTests
{
    [Fact]
    public void Throws_On_Trailing_Comma()
    {
        var ex = Assert.Throws<JsonParseException>(() => JsonSerializer.Deserialize<User>("{\"Id\":1,}"));
        Assert.Contains("character", ex.Message);
    }

    [Fact]
    public void Throws_On_Unterminated_String()
    {
        Assert.Throws<JsonParseException>(() => JsonSerializer.Deserialize<string>("\"abc"));
    }

    [Fact]
    public void Throws_On_Malformed_Number()
    {
        Assert.Throws<JsonParseException>(() => JsonSerializer.Deserialize<double>("1.2.3"));
    }

    [Fact]
    public void Throws_On_Type_Mismatch()
    {
        Assert.Throws<JsonDeserializationException>(() => JsonSerializer.Deserialize<int>("\"not a number\""));
    }

    [Fact]
    public void Throws_On_Null_Into_NonNullable_ValueType()
    {
        Assert.Throws<JsonDeserializationException>(() => JsonSerializer.Deserialize<int>("null"));
    }

    [Fact]
    public void Throws_On_Empty_Input()
    {
        Assert.Throws<JsonParseException>(() => JsonSerializer.Deserialize<User>(""));
    }

    [Fact]
    public void Throws_On_Unexpected_Trailing_Content()
    {
        Assert.Throws<JsonParseException>(() => JsonSerializer.Deserialize<int>("1 2"));
    }
}
