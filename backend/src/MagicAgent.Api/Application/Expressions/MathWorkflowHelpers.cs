using System;

namespace MagicAgent.Api.Application.Expressions.Helpers;

public static class MathWorkflowHelpers
{
    [WorkflowHelper("abs", ReturnType = WorkflowExpressionValueKind.Number, Description = "Absolute value of a number.")]
    [WorkflowHelperParameter("value", Description = "Input number.")]
    public static double Abs(double value) => Math.Abs(value);

    [WorkflowHelper("sqr", ReturnType = WorkflowExpressionValueKind.Number, Description = "Squares a number.")]
    [WorkflowHelperParameter("value", Description = "Input number.")]
    public static double Square(double value) => value * value;

    [WorkflowHelper("sqrt", ReturnType = WorkflowExpressionValueKind.Number, Description = "Square root of a number.")]
    [WorkflowHelperParameter("value", Description = "Input number.")]
    public static double Sqrt(double value) => Math.Sqrt(value);

    [WorkflowHelper("pow", ReturnType = WorkflowExpressionValueKind.Number, Description = "Raises a base to an exponent.")]
    [WorkflowHelperParameter("base", Description = "Base number.")]
    [WorkflowHelperParameter("exponent", Description = "Exponent.")]
    public static double Pow(double @base, double exponent) => Math.Pow(@base, exponent);

    [WorkflowHelper("min", ReturnType = WorkflowExpressionValueKind.Number, Description = "Minimum of two numbers.")]
    [WorkflowHelperParameter("left", Description = "First value.")]
    [WorkflowHelperParameter("right", Description = "Second value.")]
    public static double Min(double left, double right) => Math.Min(left, right);

    [WorkflowHelper("max", ReturnType = WorkflowExpressionValueKind.Number, Description = "Maximum of two numbers.")]
    [WorkflowHelperParameter("left", Description = "First value.")]
    [WorkflowHelperParameter("right", Description = "Second value.")]
    public static double Max(double left, double right) => Math.Max(left, right);
}
