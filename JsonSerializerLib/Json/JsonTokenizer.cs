using System.Globalization;
using System.Text;

namespace JsonSerializerLib;

internal enum TokenType
{
    LBrace, RBrace, LBracket, RBracket, Colon, Comma,
    String, Number, True, False, Null, EOF
}

internal readonly struct Token
{
    public TokenType Type { get; }
    public string? Text { get; }
    public int Position { get; }

    public Token(TokenType type, string? text, int position)
    {
        Type = type;
        Text = text;
        Position = position;
    }
}

internal sealed class JsonTokenizer
{
    private readonly string _s;
    private int _i;

    public JsonTokenizer(string s)
    {
        _s = s ?? throw new JsonParseException("Input JSON was null.", 0);
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            SkipWhitespace();
            if (_i >= _s.Length)
            {
                tokens.Add(new Token(TokenType.EOF, null, _i));
                break;
            }

            char c = _s[_i];
            int start = _i;
            switch (c)
            {
                case '{': tokens.Add(new Token(TokenType.LBrace, "{", start)); _i++; break;
                case '}': tokens.Add(new Token(TokenType.RBrace, "}", start)); _i++; break;
                case '[': tokens.Add(new Token(TokenType.LBracket, "[", start)); _i++; break;
                case ']': tokens.Add(new Token(TokenType.RBracket, "]", start)); _i++; break;
                case ':': tokens.Add(new Token(TokenType.Colon, ":", start)); _i++; break;
                case ',': tokens.Add(new Token(TokenType.Comma, ",", start)); _i++; break;
                case '"': tokens.Add(ReadString()); break;
                case 't': ReadLiteral("true", TokenType.True, tokens); break;
                case 'f': ReadLiteral("false", TokenType.False, tokens); break;
                case 'n': ReadLiteral("null", TokenType.Null, tokens); break;
                default:
                    if (c == '-' || char.IsDigit(c)) tokens.Add(ReadNumber());
                    else throw new JsonParseException($"Unexpected character '{c}'", start);
                    break;
            }
        }
        return tokens;
    }

    private void SkipWhitespace()
    {
        while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r'))
            _i++;
    }

    private void ReadLiteral(string literal, TokenType type, List<Token> tokens)
    {
        int start = _i;
        if (_i + literal.Length > _s.Length || _s.Substring(_i, literal.Length) != literal)
            throw new JsonParseException($"Unexpected token, expected literal '{literal}'", start);
        _i += literal.Length;
        tokens.Add(new Token(type, literal, start));
    }

    private Token ReadString()
    {
        int start = _i;
        _i++; 
        var sb = new StringBuilder();
        while (true)
        {
            if (_i >= _s.Length) throw new JsonParseException("Unterminated string literal", start);
            char c = _s[_i];

            if (c == '"') { _i++; break; }

            if (c == '\\')
            {
                _i++;
                if (_i >= _s.Length) throw new JsonParseException("Unterminated escape sequence", start);
                char e = _s[_i];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (_i + 4 >= _s.Length) throw new JsonParseException("Invalid unicode escape", start);
                        string hex = _s.Substring(_i + 1, 4);
                        if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                            throw new JsonParseException($"Invalid unicode escape '\\u{hex}'", start);
                        sb.Append((char)code);
                        _i += 4;
                        break;
                    default:
                        throw new JsonParseException($"Invalid escape character '\\{e}'", start);
                }
                _i++;
            }
            else if (c < 0x20)
            {
                throw new JsonParseException("Control character found in string literal", _i);
            }
            else
            {
                sb.Append(c);
                _i++;
            }
        }
        return new Token(TokenType.String, sb.ToString(), start);
    }

    private Token ReadNumber()
    {
        int start = _i;
        if (_s[_i] == '-') _i++;

        if (_i >= _s.Length || !char.IsDigit(_s[_i]))
            throw new JsonParseException("Invalid number: expected a digit", start);

        if (_s[_i] == '0') _i++;
        else while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;

        if (_i < _s.Length && _s[_i] == '.')
        {
            _i++;
            if (_i >= _s.Length || !char.IsDigit(_s[_i]))
                throw new JsonParseException("Invalid number: expected a digit after decimal point", start);
            while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
        }

        if (_i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E'))
        {
            _i++;
            if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;
            if (_i >= _s.Length || !char.IsDigit(_s[_i]))
                throw new JsonParseException("Invalid number: expected a digit in exponent", start);
            while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
        }

        return new Token(TokenType.Number, _s.Substring(start, _i - start), start);
    }
}
