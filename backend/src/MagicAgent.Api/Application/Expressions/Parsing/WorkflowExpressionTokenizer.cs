using System.Buffers;
using System.Collections.Generic;
using System.Globalization;

namespace MagicAgent.Api.Application.Expressions.Parsing;

/// <summary>
/// Tokenizes workflow expression text into lexical units consumed by the parser.
/// </summary>
internal sealed class WorkflowExpressionTokenizer
{
    private readonly string _text;
    private readonly int _length;
    private int _position;

    private WorkflowExpressionTokenizer(string text)
    {
        _text = text ?? string.Empty;
        _length = _text.Length;
    }

    public static IReadOnlyList<WorkflowExpressionToken> Tokenize(string text)
    {
        var tokenizer = new WorkflowExpressionTokenizer(text);
        return tokenizer.TokenizeInternal();
    }

    private IReadOnlyList<WorkflowExpressionToken> TokenizeInternal()
    {
        var tokens = new List<WorkflowExpressionToken>();

        while (!IsAtEnd())
        {
            var c = Peek();

            if (char.IsWhiteSpace(c))
            {
                Advance();
                continue;
            }

            var start = _position;

            if (IsIdentifierStart(c))
            {
                tokens.Add(ReadIdentifier(start));
                continue;
            }

            if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek(1))))
            {
                tokens.Add(ReadNumber(start));
                continue;
            }

            if (c is '\'' or '"')
            {
                tokens.Add(ReadString(start));
                continue;
            }

            tokens.Add(ReadSymbol(start));
        }

        tokens.Add(new WorkflowExpressionToken(WorkflowExpressionTokenKind.EndOfInput, string.Empty, _position));
        return tokens;
    }

    private WorkflowExpressionToken ReadIdentifier(int start)
    {
        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            Advance();
        }

        var text = _text[start.._position];
        return new WorkflowExpressionToken(WorkflowExpressionTokenKind.Identifier, text, start);
    }

    private WorkflowExpressionToken ReadNumber(int start)
    {
        void ReadDigits()
        {
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                Advance();
            }
        }

        ReadDigits();

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(Peek(1)))
        {
            Advance(); // consume '.'
            ReadDigits();
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            Advance();

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                Advance();
            }

            ReadDigits();
        }

        var text = _text[start.._position];

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            throw new WorkflowExpressionParseException($"Invalid numeric literal '{text}'.", start);
        }

        return new WorkflowExpressionToken(WorkflowExpressionTokenKind.Number, text, start);
    }

    private WorkflowExpressionToken ReadString(int start)
    {
        var quote = Advance();
        var buffer = ArrayPool<char>.Shared.Rent(_length - start);
        var bufferIndex = 0;

        try
        {
            while (!IsAtEnd())
            {
                var c = Advance();

                if (c == quote)
                {
                    var value = new string(buffer, 0, bufferIndex);
                    return new WorkflowExpressionToken(WorkflowExpressionTokenKind.String, value, start);
                }

                if (c == '\\')
                {
                    if (IsAtEnd())
                    {
                        break;
                    }

                    c = Advance();
                    c = c switch
                    {
                        '"' => '"',
                        '\'' => '\'',
                        '\\' => '\\',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => c,
                    };
                }

                buffer[bufferIndex++] = c;
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        throw new WorkflowExpressionParseException("Unterminated string literal.", start);
    }

    private WorkflowExpressionToken ReadSymbol(int start)
    {
        var c = Advance();

        return c switch
        {
            '(' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.LeftParen, "(", start),
            ')' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.RightParen, ")", start),
            '[' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.LeftBracket, "[", start),
            ']' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.RightBracket, "]", start),
            ',' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Comma, ",", start),
            '.' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Dot, ".", start),
            '+' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Plus, "+", start),
            '-' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Minus, "-", start),
            '*' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Star, "*", start),
            '/' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Slash, "/", start),
            '%' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Percent, "%", start),
            '^' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Caret, "^", start),
            '!' => Match('=')
                ? new WorkflowExpressionToken(WorkflowExpressionTokenKind.NotEquals, "!=", start)
                : new WorkflowExpressionToken(WorkflowExpressionTokenKind.Bang, "!", start),
            '=' => Match('=')
                ? new WorkflowExpressionToken(WorkflowExpressionTokenKind.Equals, "==", start)
                : new WorkflowExpressionToken(WorkflowExpressionTokenKind.Equals, "=", start),
            '<' => Match('=')
                ? new WorkflowExpressionToken(WorkflowExpressionTokenKind.LessThanOrEqual, "<=", start)
                : new WorkflowExpressionToken(WorkflowExpressionTokenKind.LessThan, "<", start),
            '>' => Match('=')
                ? new WorkflowExpressionToken(WorkflowExpressionTokenKind.GreaterThanOrEqual, ">=", start)
                : new WorkflowExpressionToken(WorkflowExpressionTokenKind.GreaterThan, ">", start),
            '&' when Match('&') => new WorkflowExpressionToken(WorkflowExpressionTokenKind.AndAnd, "&&", start),
            '|' when Match('|') => new WorkflowExpressionToken(WorkflowExpressionTokenKind.OrOr, "||", start),
            '?' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Question, "?", start),
            ':' => new WorkflowExpressionToken(WorkflowExpressionTokenKind.Colon, ":", start),
            _ => throw new WorkflowExpressionParseException($"Unexpected character '{c}'.", start),
        };
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _text[_position] != expected)
        {
            return false;
        }

        _position++;
        return true;
    }

    private char Peek(int offset = 0) => (_position + offset) < _length ? _text[_position + offset] : '\0';

    private char Advance()
    {
        if (IsAtEnd())
        {
            return '\0';
        }

        return _text[_position++];
    }

    private bool IsAtEnd() => _position >= _length;

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_' || c == '$';

    private static bool IsIdentifierPart(char c) => IsIdentifierStart(c) || char.IsDigit(c);
}
