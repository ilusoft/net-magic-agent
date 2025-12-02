using FluentAssertions;
using MagicAgent.Api.Application.Expressions.Parsing;

namespace MagicAgent.Api.Tests;

public class ExpressionParserTests
{
    [Fact]
    public void ParsesArithmeticPrecedence()
    {
        var root = WorkflowExpressionParser.Parse("1 + 2 * 3");

        root.Should().BeOfType<BinaryExpressionNode>();

        var sum = (BinaryExpressionNode)root;
        sum.Operator.Should().Be(WorkflowExpressionTokenKind.Plus);
        sum.Left.Should().BeOfType<LiteralExpressionNode>()
            .Which.Value.Should().Be(1d);

        sum.Right.Should().BeOfType<BinaryExpressionNode>();
        var product = (BinaryExpressionNode)sum.Right;
        product.Operator.Should().Be(WorkflowExpressionTokenKind.Star);

        product.Left.Should().BeOfType<LiteralExpressionNode>()
            .Which.Value.Should().Be(2d);

        product.Right.Should().BeOfType<LiteralExpressionNode>()
            .Which.Value.Should().Be(3d);
    }

    [Fact]
    public void ParsesFunctionWithMemberAndIndexAccess()
    {
        var root = WorkflowExpressionParser.Parse("abs(var.order.items[0].price)");

        var call = root.Should().BeOfType<FunctionCallExpressionNode>().Subject;
        call.Target.Should().BeOfType<IdentifierExpressionNode>()
            .Which.Name.Should().Be("abs");

        call.Arguments.Should().ContainSingle();
        var arg = call.Arguments[0];

        var memberPrice = arg.Should().BeOfType<MemberAccessExpressionNode>().Subject;
        memberPrice.MemberName.Should().Be("price");

        var indexAccess = memberPrice.Target.Should().BeOfType<IndexAccessExpressionNode>().Subject;
        indexAccess.Index.Should().BeOfType<LiteralExpressionNode>()
            .Which.Value.Should().Be(0d);

        var memberItems = indexAccess.Target.Should().BeOfType<MemberAccessExpressionNode>().Subject;
        memberItems.MemberName.Should().Be("items");

        var memberOrder = memberItems.Target.Should().BeOfType<MemberAccessExpressionNode>().Subject;
        memberOrder.MemberName.Should().Be("order");

        memberOrder.Target.Should().BeOfType<IdentifierExpressionNode>()
            .Which.Name.Should().Be("var");
    }
}
