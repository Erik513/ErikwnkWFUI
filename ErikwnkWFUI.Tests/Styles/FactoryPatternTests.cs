using System.Reflection;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The pattern of the factories of UIStyles, held for all of them at once: every "...Standard" has a "...Primary" of the same kind, the standard one
/// takes a color set called <c>colors</c> (null by default), the primary one follows the accent and takes none, and the parameters have the same names for the
/// same things. A new factory that breaks the pattern shows up here.
/// </summary>
public class FactoryPatternTests
{
    private static IEnumerable<Type> FactoryClasses()
    {
        return typeof(UIStyles).GetNestedTypes(BindingFlags.Public).Where(type => type.IsAbstract && type.IsSealed);
    }

    private static IEnumerable<MethodInfo> Methods(Type factory)
    {
        return factory.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(method => !method.IsSpecialName);
    }

    // The standard and primary methods are "CreateStandard"/"CreatePrimary", "CreateReadOnlyStandard"/"CreateReadOnlyPrimary", "ShowStandard" ...:
    // the kind is what is in front of the last word.
    private static string Kind(string name, string word) => name.Substring(0, name.Length - word.Length);

    // What may stay without its partner, with the reason (none at the moment).
    private static readonly string[] StandardWithoutPrimary = Array.Empty<string>();

    [Fact]
    public void EveryStandardFactory_HasAPrimaryOneOfTheSameKind_AndTheOtherWayRound()
    {
        List<string> missing = new List<string>();

        foreach (Type factory in FactoryClasses())
        {
            string[] names = Methods(factory).Select(method => method.Name).Distinct().ToArray();

            foreach (string standard in names.Where(name => name.EndsWith("Standard", StringComparison.Ordinal)))
            {
                if (!names.Contains(Kind(standard, "Standard") + "Primary"))
                    missing.Add(factory.Name + "." + standard + " has no primary one");
            }

            foreach (string primary in names.Where(name => name.EndsWith("Primary", StringComparison.Ordinal)))
            {
                if (!names.Contains(Kind(primary, "Primary") + "Standard"))
                    missing.Add(factory.Name + "." + primary + " has no standard one");
            }
        }

        Assert.Empty(missing.Except(StandardWithoutPrimary));
    }

    [Fact]
    public void AStandardFactory_TakesColors_OfTypeUIColorSet_WithNullAsDefault_AndAPrimaryOneTakesNone()
    {
        List<string> broken = new List<string>();

        foreach (Type factory in FactoryClasses())
        {
            foreach (MethodInfo method in Methods(factory))
            {
                ParameterInfo? colors = method.GetParameters().FirstOrDefault(parameter => parameter.Name == "colors");

                if (method.Name.EndsWith("Standard", StringComparison.Ordinal))
                {
                    if (colors == null)
                        broken.Add(factory.Name + "." + method.Name + " has no 'colors' parameter");
                    else if (colors.ParameterType != typeof(UIColorSet))
                        broken.Add(factory.Name + "." + method.Name + ": 'colors' is a " + colors.ParameterType.Name);
                    else if (!colors.HasDefaultValue || colors.DefaultValue != null)
                        broken.Add(factory.Name + "." + method.Name + ": 'colors' has no default of null");
                }
                else if (method.Name.EndsWith("Primary", StringComparison.Ordinal) && colors != null)
                {
                    broken.Add(factory.Name + "." + method.Name + " takes 'colors' (a primary one follows the accent)");
                }
            }
        }

        Assert.Empty(broken);
    }

    [Fact]
    public void AStandardAndItsPrimary_ReturnTheSameType()
    {
        List<string> different = new List<string>();

        foreach (Type factory in FactoryClasses())
        {
            ILookup<string, MethodInfo> byName = Methods(factory).ToLookup(method => method.Name);

            foreach (string standard in byName.Select(group => group.Key).Where(name => name.EndsWith("Standard", StringComparison.Ordinal)))
            {
                string primary = Kind(standard, "Standard") + "Primary";

                if (byName.Contains(primary) && byName[standard].First().ReturnType != byName[primary].First().ReturnType)
                    different.Add(factory.Name + "." + standard + " returns " + byName[standard].First().ReturnType.Name + ", " + primary + " returns " + byName[primary].First().ReturnType.Name);
            }
        }

        Assert.Empty(different);
    }

    [Fact]
    public void TheParametersOfTheFactories_AreNamedAlike_ForTheSameThing()
    {
        // (A color set is "colors", never "colorSet"/"colours"; a size is "size". A single Color may be "color", as in Colors.CreateSet.)
        string[] notAllowed = { "colorSet", "set", "colours", "sz", "dimension" };
        List<string> found = new List<string>();

        foreach (Type factory in FactoryClasses())
        {
            foreach (MethodInfo method in Methods(factory))
            {
                foreach (ParameterInfo parameter in method.GetParameters().Where(parameter => notAllowed.Contains(parameter.Name)))
                    found.Add(factory.Name + "." + method.Name + "(" + parameter.Name + ")");
            }
        }

        Assert.Empty(found);
    }

    [Fact]
    public void EveryParameterOfTypeUIColorSet_IsCalledColors_OrPopupColors()
    {
        List<string> found = new List<string>();

        foreach (Type factory in FactoryClasses())
        {
            foreach (MethodInfo method in Methods(factory))
            {
                foreach (ParameterInfo parameter in method.GetParameters().Where(parameter => parameter.ParameterType == typeof(UIColorSet) && parameter.Name != "colors" && parameter.Name != "popupColors"))
                    found.Add(factory.Name + "." + method.Name + "(" + parameter.Name + ")");
            }
        }

        Assert.Empty(found);
    }
}
