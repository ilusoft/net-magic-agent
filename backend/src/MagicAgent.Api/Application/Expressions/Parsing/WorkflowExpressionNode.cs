using System.Collections.Generic;

namespace MagicAgent.Api.Application.Expressions.Parsing;

internal abstract record WorkflowExpressionNode(int Position);

internal sealed record LiteralExpressionNode(
    WorkflowExpressionValueKind ValueKind,
    object? Value,
    int Position) : WorkflowExpressionNode(Position);

internal sealed record IdentifierExpressionNode(
    string Name,
    int Position) : WorkflowExpressionNode(Position);

internal sealed record UnaryExpressionNode(
    WorkflowExpressionTokenKind Operator,
    WorkflowExpressionNode Operand,
    int Position) : WorkflowExpressionNode(Position);

internal sealed record BinaryExpressionNode(
    WorkflowExpressionTokenKind Operator,
    WorkflowExpressionNode Left,
    WorkflowExpressionNode Right,
    int Position) : WorkflowExpressionNode(Position);

internal sealed record ConditionalExpressionNode(
    WorkflowExpressionNode Condition,
    WorkflowExpressionNode WhenTrue,
    WorkflowExpressionNode WhenFalse,
    int Position) : WorkflowExpressionNode(Position);

internal sealed record FunctionCallExpressionNode(
    WorkflowExpressionNode Target,
    IReadOnlyList<WorkflowExpressionNode> Arguments,
    int Position) : WorkflowExpressionNode(Position);

internal sealed record MemberAccessExpressionNode(
    WorkflowExpressionNode Target,
    string MemberName,
    int Position) : WorkflowExpressionNode(Position);

internal sealed record IndexAccessExpressionNode(
    WorkflowExpressionNode Target,
    WorkflowExpressionNode Index,
    int Position) : WorkflowExpressionNode(Position);
