#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    // The compiler needs this type for records and init-only properties;
    // .NET Framework does not ship it.
    internal static class IsExternalInit
    {
    }
}
#endif
