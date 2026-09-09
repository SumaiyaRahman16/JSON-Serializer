using JsonSerializerLib;
using Xunit;

public class Person
{
    public string Name { get; set; } = "";
    public Person? Friend { get; set; }
}

public class Node
{
    public string Label { get; set; } = "";
    public Node? Next { get; set; }
}

public class CircularReferenceTests
{
    [Fact]
    public void Does_Not_Infinite_Loop_On_Direct_Self_Reference()
    {
        var person = new Person { Name = "Alice" };
        person.Friend = person;

        var json = JsonSerializer.Serialize(person, indent: false);

        Assert.Contains("\"Friend\":null", json);
    }

    [Fact]
    public void Does_Not_Infinite_Loop_On_Indirect_Cycle()
    {
        var a = new Node { Label = "A" };
        var b = new Node { Label = "B" };
        a.Next = b;
        b.Next = a; // cycle through a chain, not a direct self-reference

        var json = JsonSerializer.Serialize(a, indent: false);

        Assert.Contains("\"Label\":\"A\"", json);
        Assert.Contains("\"Label\":\"B\"", json);
        Assert.Contains("null", json); // cycle point cut off with null
    }
}
