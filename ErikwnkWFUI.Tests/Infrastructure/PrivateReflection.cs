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
    // Static, not just Instance - InvokePrivate/InvokePrivate&lt;T&gt; are called
    // on an instance either way (extension-method syntax needs a receiver),
    // but several private helpers this reaches for are plain static pure
    // functions (e.g. ListView's own BuildCfHtmlTable/BuildHtmlTable) -
    // MethodInfo.Invoke ignores the target object for a static method, so
    // the same call site works for both kinds once both flags are set.
    private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

    // DeclaredOnly on top of the usual Instance|Static|NonPublic - without
    // it, GetMethod(name, flags) (the overload that doesn't take parameter
    // types) throws AmbiguousMatchException the moment a type declares a
    // private/protected member sharing a base class member's name but not
    // its signature - confirmed live for ListView's own OnColumnWidthChanging
    // handler (object, ColumnWidthChangingEventArgs), which collides on name
    // alone with System.Windows.Forms.ListView's own protected event-raiser
    // OnColumnWidthChanging(ColumnWidthChangingEventArgs) despite taking a
    // different number of parameters - GetMethod(name, flags) doesn't
    // disambiguate by signature at all, it just throws. DeclaredOnly still
    // combines correctly with the manual per-type walk below (still finds a
    // private member declared on some ANCESTOR type, e.g. DataGridView's own
    // PasteFromClipboard when target is a TestableDataGridView subclass) -
    // each individual GetMethod call in the loop just stops considering any
    // OTHER type's same-named members as candidates.
    private const BindingFlags InstanceNonPublicDeclaredOnly = InstanceNonPublic | BindingFlags.DeclaredOnly;

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
            MethodInfo? method = current.GetMethod(methodName, InstanceNonPublicDeclaredOnly);
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
            FieldInfo? field = current.GetField(fieldName, InstanceNonPublicDeclaredOnly);
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

    // Same as the void overload above, for the (rarer) case a test needs
    // the private method's own return value - e.g. a pure calculation like
    // ListBox's GetBackColor/GetInsertPosition, not just a side effect on
    // some field. An explicit array passed as args (rather than relying on
    // the params expansion) is the same array Invoke writes any out/ref
    // parameter back into, so a caller can still read those afterward.
    public static T? InvokePrivate<T>(this object target, string methodName, params object?[] args)
    {
        MethodInfo method = FindMethod(target.GetType(), methodName);

        try
        {
            return (T?)method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            return default;
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
