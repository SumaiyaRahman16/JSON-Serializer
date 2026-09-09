namespace JsonSerializerLib;

internal sealed class JsonParser
{
    private readonly List<Token> _tokens;
    private int _pos;

    public JsonParser(string json)
    {
        _tokens = new JsonTokenizer(json).Tokenize();
        _pos = 0;
    }

    public object? Parse()
    {
        var value = ParseValue();
        Expect(TokenType.EOF, "end of input");
        return value;
    }

    private Token Current => _tokens[_pos];
    private Token Advance() => _tokens[_pos++];

    private Token Expect(TokenType type, string what)
    {
        if (Current.Type != type)
            throw new JsonParseException(
                $"Expected {what} but found '{Current.Text ?? Current.Type.ToString()}'", Current.Position);
        return Advance();
    }

    private object? ParseValue()
    {
        switch (Current.Type)
        {
            case TokenType.LBrace: return ParseObject();
            case TokenType.LBracket: return ParseArray();
            case TokenType.String: return Advance().Text;
            case TokenType.Number: return new JsonNumber(Advance().Text!);
            case TokenType.True: Advance(); return true;
            case TokenType.False: Advance(); return false;
            case TokenType.Null: Advance(); return null;
            default:
                throw new JsonParseException(
                    $"Unexpected token '{Current.Text ?? Current.Type.ToString()}'", Current.Position);
        }
    }

    private Dictionary<string, object?> ParseObject()
    {
        var dict = new Dictionary<string, object?>();
        Expect(TokenType.LBrace, "'{'");
        if (Current.Type == TokenType.RBrace) { Advance(); return dict; }

        while (true)
        {
            var key = Expect(TokenType.String, "a property name string");
            Expect(TokenType.Colon, "':'");
            dict[key.Text!] = ParseValue();

            if (Current.Type == TokenType.Comma) { Advance(); continue; }
            break;
        }
        Expect(TokenType.RBrace, "'}'");
        return dict;
    }

    private List<object?> ParseArray()
    {
        var list = new List<object?>();
        Expect(TokenType.LBracket, "'['");
        if (Current.Type == TokenType.RBracket) { Advance(); return list; }

        while (true)
        {
            list.Add(ParseValue());
            if (Current.Type == TokenType.Comma) { Advance(); continue; }
            break;
        }
        Expect(TokenType.RBracket, "']'");
        return list;
    }
}
