using System;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// TryConvertPastedValue - a pure, static function PasteFromClipboard uses
/// per cell, invoked directly via reflection (PrivateReflection finds
/// static members too). Only its "unparseable text" failure path had any
/// coverage before (through the full PasteFromClipboard flow in
/// DataGridViewPasteTests) - this covers the rest of its branches directly,
/// including the blank-value canBeNull distinction that path never
/// exercised.
/// </summary>
public class DataGridViewPasteValueConversionTests
{
    private static bool TryConvert(string text, Type targetType, out object? convertedValue)
    {
        using WfuiDataGridView grid = new WfuiDataGridView();
        object?[] args = { text, targetType, null };
        bool result = grid.InvokePrivate<bool>("TryConvertPastedValue", args);
        convertedValue = args[2];
        return result;
    }

    [Fact]
    public void Blank_NullableValueType_ClearsIt()
    {
        bool result = TryConvert("", typeof(int?), out object? value);

        Assert.True(result);
        Assert.Null(value);
    }

    [Fact]
    public void Blank_NonNullableValueType_IsSkippedRatherThanCleared()
    {
        bool result = TryConvert("", typeof(int), out object? value);

        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void Blank_ReferenceType_ClearsIt()
    {
        bool result = TryConvert("", typeof(string), out object? value);

        Assert.True(result);
        Assert.Null(value);
    }

    [Fact]
    public void NonBlank_StringTarget_PassesTextThroughUnconverted()
    {
        bool result = TryConvert("hello", typeof(string), out object? value);

        Assert.True(result);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void NonBlank_NumericTarget_ConvertsSuccessfully()
    {
        bool result = TryConvert("42", typeof(int), out object? value);

        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void NonBlank_EnumTarget_ParsesByNameCaseInsensitively()
    {
        bool result = TryConvert("monday", typeof(DayOfWeek), out object? value);

        Assert.True(result);
        Assert.Equal(DayOfWeek.Monday, value);
    }

    [Fact]
    public void NonBlank_UnparseableNumericText_FailsWithoutThrowing()
    {
        bool result = TryConvert("notANumber", typeof(int), out object? value);

        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void NonBlank_NumericTextOverflowingTheTargetType_FailsWithoutThrowing()
    {
        bool result = TryConvert("99999999999", typeof(byte), out object? value);

        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void NonBlank_NullableNumericTarget_ConvertsSuccessfully()
    {
        bool result = TryConvert("7", typeof(int?), out object? value);

        Assert.True(result);
        Assert.Equal(7, value);
    }
}
