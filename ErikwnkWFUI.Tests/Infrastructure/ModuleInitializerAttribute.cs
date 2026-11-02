#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    // .NET Framework does not ship it; the compiler only needs the name.
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif
