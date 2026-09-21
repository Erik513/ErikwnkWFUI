using System.Reflection;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// Invokes DataGridView's own private editing operations (PasteFromClipboard,
/// InsertBlankRow, DeleteRows, DeleteItem) directly, the same way the library
/// itself already reaches into other types' internals via reflection
/// (DataSourceReflectionCache, EnableDoubleBuffering). These stay private
/// because a consumer never calls them directly - they only ever run from a
/// key press, a context-menu click, or the delete-glyph column - so exposing
/// them as internal/protected just for tests isn't worth widening the real
/// public surface for.
/// </summary>
internal static class PrivateReflection
{
    private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

    // GetMethod/GetField with just Instance|NonPublic only look at members
    // DECLARED on the exact type - a private member declared on a base
    // class (e.g. DataGridView's own PasteFromClipboard, when target is a
    // TestableDataGridView subclass) is invisible unless the hierarchy is
    // walked by hand; reflection never flattens private inherited members
    // the way it does public ones.
    private static MethodInfo FindMethod(Type type, string methodName)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            MethodInfo? method = current.GetMethod(methodName, InstanceNonPublic);
            if (method != null)
            {
                return method;
            }
        }

        throw new MissingMethodException(type.FullName, methodName);
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            FieldInfo? field = current.GetField(fieldName, InstanceNonPublic);
            if (field != null)
            {
                return field;
            }
        }

        throw new MissingFieldException(type.FullName, fieldName);
    }

    public static void InvokePrivate(this object target, string methodName, params object?[] args)
    {
        MethodInfo method = FindMethod(target.GetType(), methodName);

        try
        {
            method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    public static T? GetPrivateField<T>(this object target, string fieldName)
    {
        return (T?)FindField(target.GetType(), fieldName).GetValue(target);
    }

    public static void SetPrivateField<T>(this object target, string fieldName, T value)
    {
        FindField(target.GetType(), fieldName).SetValue(target, value);
    }
}
