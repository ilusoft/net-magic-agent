using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace MagicAgent.Api.Application.Expressions.Helpers;

public static class ArrayWorkflowHelpers
{
    [WorkflowHelper(
        "addToArray",
        ReturnType = WorkflowExpressionValueKind.Json,
        Description = "Returns a new array with the provided item inserted at the specified index (defaults to the end).")]
    [WorkflowHelperParameter("newItem", Description = "Value to insert into the array.")]
    [WorkflowHelperParameter("orgArray", Description = "Existing array. When omitted, a new array is created.", IsOptional = true)]
    [WorkflowHelperParameter("idx", Description = "Zero-based index where the value should be inserted.", IsOptional = true)]
    public static JsonArray AddToArray(
        WorkflowExpressionValue newItem,
        JsonNode? orgArray = null,
        double idx = double.NaN)
    {
        var items = ToList(orgArray);
        var valueNode = ConvertValueToNode(newItem);
        var insertIndex = ResolveInsertIndex(idx, items.Count);
        items.Insert(insertIndex, valueNode);
        return BuildArray(items);
    }

    [WorkflowHelper(
        "removeFromArray",
        ReturnType = WorkflowExpressionValueKind.Json,
        Description = "Removes an item from the array. Removes the first occurrence by default or all when requested.")]
    [WorkflowHelperParameter("itemToRemove", Description = "Value to remove from the array.")]
    [WorkflowHelperParameter("orgArray", Description = "Array to remove the value from.")]
    [WorkflowHelperParameter("removeAll", Description = "If true, removes all occurrences instead of the first.", IsOptional = true)]
    public static JsonArray RemoveFromArray(
        WorkflowExpressionValue itemToRemove,
        JsonNode? orgArray,
        bool removeAll = false)
    {
        var items = ToList(orgArray);
        if (items.Count == 0)
        {
            return BuildArray(items);
        }

        var match = ConvertValueToNode(itemToRemove);

        if (removeAll)
        {
            items.RemoveAll(item => NodesEqual(item, match));
        }
        else
        {
            var index = items.FindIndex(item => NodesEqual(item, match));
            if (index >= 0)
            {
                items.RemoveAt(index);
            }
        }

        return BuildArray(items);
    }

    [WorkflowHelper(
        "indexOnArray",
        ReturnType = WorkflowExpressionValueKind.Number,
        Description = "Searches the array and returns the index of the first matching item (or last when requested). Returns -1 when not found.")]
    [WorkflowHelperParameter("itemToSearch", Description = "Value to locate inside the array.")]
    [WorkflowHelperParameter("array", Description = "Array to search in.")]
    [WorkflowHelperParameter("startFromEnd", Description = "If true, searches from the end of the array.", IsOptional = true)]
    public static double IndexOnArray(
        WorkflowExpressionValue itemToSearch,
        JsonNode? array,
        bool startFromEnd = false)
    {
        if (array is not JsonArray jsonArray || jsonArray.Count == 0)
        {
            return -1;
        }

        var match = ConvertValueToNode(itemToSearch);

        if (startFromEnd)
        {
            for (var i = jsonArray.Count - 1; i >= 0; i--)
            {
                if (NodesEqual(jsonArray[i], match))
                {
                    return i;
                }
            }
        }
        else
        {
            for (var i = 0; i < jsonArray.Count; i++)
            {
                if (NodesEqual(jsonArray[i], match))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    [WorkflowHelper(
        "replaceElement",
        ReturnType = WorkflowExpressionValueKind.Json,
        Description = "Replaces occurrences of a value inside the array. Replaces the first match by default or all when requested.")]
    [WorkflowHelperParameter("itemToSearch", Description = "Existing value to replace.")]
    [WorkflowHelperParameter("array", Description = "Array that contains the value to replace.")]
    [WorkflowHelperParameter("itemToReplace", Description = "New value that should replace the match.")]
    [WorkflowHelperParameter("replaceAll", Description = "If true, replaces every occurrence instead of the first.", IsOptional = true)]
    public static JsonArray ReplaceElement(
        WorkflowExpressionValue itemToSearch,
        JsonNode? array,
        WorkflowExpressionValue itemToReplace,
        bool replaceAll = false)
    {
        var items = ToList(array);
        if (items.Count == 0)
        {
            return BuildArray(items);
        }

        var match = ConvertValueToNode(itemToSearch);
        var replacement = ConvertValueToNode(itemToReplace);

        for (var i = 0; i < items.Count; i++)
        {
            if (!NodesEqual(items[i], match))
            {
                continue;
            }

            items[i] = replacement;

            if (!replaceAll)
            {
                break;
            }
        }

        return BuildArray(items);
    }

    [WorkflowHelper(
        "subArray",
        ReturnType = WorkflowExpressionValueKind.Json,
        Description = "Returns a subset of the array starting at index 0 or from the end when inverted.")]
    [WorkflowHelperParameter("array", Description = "Array to slice.")]
    [WorkflowHelperParameter("numItems", Description = "Number of items to include in the result.")]
    [WorkflowHelperParameter("invert", Description = "If true, counts from the end of the array.", IsOptional = true)]
    public static JsonArray SubArray(JsonNode? array, double numItems, bool invert = false)
    {
        var items = ToList(array);
        if (items.Count == 0)
        {
            return new JsonArray();
        }

        var take = NormalizeCount(numItems, items.Count);
        if (take == 0)
        {
            return new JsonArray();
        }

        var result = new List<JsonNode?>();

        if (!invert)
        {
            for (var i = 0; i < take && i < items.Count; i++)
            {
                result.Add(items[i]);
            }
        }
        else
        {
            var start = Math.Max(items.Count - take, 0);
            for (var i = start; i < items.Count; i++)
            {
                result.Add(items[i]);
            }
        }

        return BuildArray(result);
    }

    [WorkflowHelper(
        "concatArrays",
        ReturnType = WorkflowExpressionValueKind.Json,
        Description = "Concatenates two arrays and returns a new combined array.")]
    [WorkflowHelperParameter("array1", Description = "First array. Treated as empty when omitted.")]
    [WorkflowHelperParameter("array2", Description = "Second array. Treated as empty when omitted.")]
    public static JsonArray ConcatArrays(JsonNode? array1, JsonNode? array2)
    {
        var combined = new List<JsonNode?>();
        combined.AddRange(ToList(array1));
        combined.AddRange(ToList(array2));
        return BuildArray(combined);
    }

    private static List<JsonNode?> ToList(JsonNode? source)
    {
        var items = new List<JsonNode?>();

        if (source is JsonArray array)
        {
            foreach (var item in array)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static JsonArray BuildArray(IEnumerable<JsonNode?> items)
    {
        var array = new JsonArray();
        foreach (var item in items)
        {
            array.Add(CloneNode(item));
        }

        return array;
    }

    private static JsonNode? ConvertValueToNode(WorkflowExpressionValue? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Kind switch
        {
            WorkflowExpressionValueKind.Json => CloneNode(value.JsonValue),
            WorkflowExpressionValueKind.Number => JsonValue.Create(value.NumberValue ?? 0d),
            WorkflowExpressionValueKind.Boolean => JsonValue.Create(value.BooleanValue ?? false),
            WorkflowExpressionValueKind.DateTime => JsonValue.Create((value.DateTimeValue ?? DateTimeOffset.MinValue).ToString("O")),
            WorkflowExpressionValueKind.Null => null,
            _ => JsonValue.Create(value.StringValue ?? string.Empty),
        };
    }

    private static bool NodesEqual(JsonNode? left, JsonNode? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return JsonNode.DeepEquals(left, right);
    }

    private static JsonNode? CloneNode(JsonNode? node) => node?.DeepClone();

    private static int ResolveInsertIndex(double index, int length)
    {
        if (double.IsNaN(index))
        {
            return length;
        }

        var rounded = (int)Math.Round(index, MidpointRounding.AwayFromZero);

        if (rounded <= 0)
        {
            return 0;
        }

        if (rounded >= length)
        {
            return length;
        }

        return rounded;
    }

    private static int NormalizeCount(double requested, int maxLength)
    {
        if (maxLength <= 0)
        {
            return 0;
        }

        var rounded = (int)Math.Round(requested, MidpointRounding.AwayFromZero);

        if (rounded <= 0)
        {
            return 0;
        }

        return Math.Min(rounded, maxLength);
    }
}
