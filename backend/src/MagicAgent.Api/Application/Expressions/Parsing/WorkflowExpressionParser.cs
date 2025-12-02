using System.Collections.Generic;
using System.Globalization;

namespace MagicAgent.Api.Application.Expressions.Parsing;

internal sealed class WorkflowExpressionParser
{
    private readonly IReadOnlyList<WorkflowExpressionToken> _tokens;
    private int _position;

    private WorkflowExpressionParser(IReadOnlyList<WorkflowExpressionToken> tokens)
    {
        _tokens = tokens;
    }

    public static WorkflowExpressionNode Parse(string expression)
    {
        ArgumentException.ThrowIfNullOrEmpty(expression);

        var tokens = WorkflowExpressionTokenizer.Tokenize(expression);
        var parser = new WorkflowExpressionParser(tokens);
        var expressionResult = parser.ParseExpression();
        if (parser.Current.Kind != WorkflowExpressionTokenKind.EndOfInput)
        {
            throw new WorkflowExpressionParseException(
                $"Unexpected trailing token '{parser.Current.Kind}'.",
                parser.Current.Position);
        }

        return expressionResult;
    }

    private WorkflowExpressionNode ParseExpression()
    {
        var expr = ParseBinaryExpression(0);

        if (Match(WorkflowExpressionTokenKind.Question))
        {
            var whenTrue = ParseExpression();
            Expect(WorkflowExpressionTokenKind.Colon, "Expected ':' in conditional expression.");
            var whenFalse = ParseExpression();
            expr = new ConditionalExpressionNode(expr, whenTrue, whenFalse, expr.Position);
        }

        return expr;
    }

    private WorkflowExpressionNode ParseBinaryExpression(int parentPrecedence)
    {
        var left = ParseUnaryExpression();
        left = ParsePostfix(left);

        while (true)
        {
            var (precedence, rightAssociative) = GetBinaryPrecedence(Current.Kind);

            if (precedence < 0 || precedence < parentPrecedence)
            {
                break;
            }

            var op = Advance();
            var nextPrecedence = rightAssociative ? precedence : precedence + 1;
            var right = ParseBinaryExpression(nextPrecedence);
            left = new BinaryExpressionNode(op.Kind, left, right, op.Position);
        }

        return left;
    }

    private WorkflowExpressionNode ParsePostfix(WorkflowExpressionNode expression)
    {
        while (true)
        {
            if (Match(WorkflowExpressionTokenKind.LeftParen))
            {
                var args = new List<WorkflowExpressionNode>();

                if (!Match(WorkflowExpressionTokenKind.RightParen))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    }
                    while (Match(WorkflowExpressionTokenKind.Comma));

                    Expect(WorkflowExpressionTokenKind.RightParen, "Expected ')' after function arguments.");
                }

                expression = new FunctionCallExpressionNode(expression, args, expression.Position);
                continue;
            }

            if (Match(WorkflowExpressionTokenKind.Dot))
            {
                var identifier = Expect(WorkflowExpressionTokenKind.Identifier, "Expected member name after '.'.");
                expression = new MemberAccessExpressionNode(expression, identifier.Text, identifier.Position);
                continue;
            }

            if (Match(WorkflowExpressionTokenKind.LeftBracket))
            {
                var index = ParseExpression();
                Expect(WorkflowExpressionTokenKind.RightBracket, "Expected ']' after index expression.");
                expression = new IndexAccessExpressionNode(expression, index, expression.Position);
                continue;
            }

            break;
        }

        return expression;
    }

    private WorkflowExpressionNode ParseUnaryExpression()
    {
        var token = Current;

        if (token.Kind is WorkflowExpressionTokenKind.Plus or WorkflowExpressionTokenKind.Minus or WorkflowExpressionTokenKind.Bang)
        {
            Advance();
            var operand = ParseUnaryExpression();
            return new UnaryExpressionNode(token.Kind, operand, token.Position);
        }

        return ParsePrimary();
    }

    private WorkflowExpressionNode ParsePrimary()
    {
        var token = Advance();

        return token.Kind switch
        {
            WorkflowExpressionTokenKind.Number => new LiteralExpressionNode(
                WorkflowExpressionValueKind.Number,
                double.Parse(token.Text, CultureInfo.InvariantCulture),
                token.Position),
            WorkflowExpressionTokenKind.String => new LiteralExpressionNode(
                WorkflowExpressionValueKind.String,
                token.Text,
                token.Position),
            WorkflowExpressionTokenKind.Identifier => ParseIdentifierLiteral(token),
            WorkflowExpressionTokenKind.LeftParen => ParseGroupedExpression(token.Position),
            _ => throw new WorkflowExpressionParseException($"Unexpected token '{token.Text}'.", token.Position),
        };
    }

    private WorkflowExpressionNode ParseIdentifierLiteral(WorkflowExpressionToken token)
    {
        var identifier = token.Text;

        if (string.Equals(identifier, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new LiteralExpressionNode(WorkflowExpressionValueKind.Boolean, true, token.Position);
        }

        if (string.Equals(identifier, "false", StringComparison.OrdinalIgnoreCase))
        {
            return new LiteralExpressionNode(WorkflowExpressionValueKind.Boolean, false, token.Position);
        }

        if (string.Equals(identifier, "null", StringComparison.OrdinalIgnoreCase))
        {
            return new LiteralExpressionNode(WorkflowExpressionValueKind.Null, null, token.Position);
        }

        return new IdentifierExpressionNode(identifier, token.Position);
    }

    private WorkflowExpressionNode ParseGroupedExpression(int position)
    {
        var expr = ParseExpression();
        Expect(WorkflowExpressionTokenKind.RightParen, "Expected ')' to close grouped expression.");
        return expr;
    }

    private WorkflowExpressionToken Expect(WorkflowExpressionTokenKind kind, string message)
    {
        if (Current.Kind != kind)
        {
            throw new WorkflowExpressionParseException(message, Current.Position);
        }

        return Advance();
    }

    private bool Match(WorkflowExpressionTokenKind kind)
    {
        if (Current.Kind == kind)
        {
            Advance();
            return true;
        }

        return false;
    }

    private WorkflowExpressionToken Advance()
    {
        if (_position < _tokens.Count)
        {
            return _tokens[_position++];
        }

        return _tokens[^1];
    }

    private WorkflowExpressionToken Current => _tokens[_position];

    private static (int Precedence, bool RightAssociative) GetBinaryPrecedence(WorkflowExpressionTokenKind kind) => kind switch
    {
        WorkflowExpressionTokenKind.Caret => (7, true),
        WorkflowExpressionTokenKind.Star or WorkflowExpressionTokenKind.Slash or WorkflowExpressionTokenKind.Percent => (6, false),
        WorkflowExpressionTokenKind.Plus or WorkflowExpressionTokenKind.Minus => (5, false),
        WorkflowExpressionTokenKind.LessThan or WorkflowExpressionTokenKind.LessThanOrEqual or WorkflowExpressionTokenKind.GreaterThan or WorkflowExpressionTokenKind.GreaterThanOrEqual => (4, false),
        WorkflowExpressionTokenKind.Equals or WorkflowExpressionTokenKind.NotEquals => (3, false),
        WorkflowExpressionTokenKind.AndAnd => (2, false),
        WorkflowExpressionTokenKind.OrOr => (1, false),
        _ => (-1, false),
    };
}
