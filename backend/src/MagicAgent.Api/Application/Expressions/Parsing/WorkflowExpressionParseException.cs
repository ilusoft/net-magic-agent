using System;

namespace MagicAgent.Api.Application.Expressions.Parsing;

public sealed class WorkflowExpressionParseException : Exception
{
    public WorkflowExpressionParseException(string message, int position)
        : base(message)
    {
        Position = position;
    }

    public int Position { get; }
}
