using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

#nullable enable

/// <summary>
/// Analyzer that detects redundant uses of <c>[ValidatePrototypeId&lt;T&gt;]</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidateProtoIdAnalyzer : DiagnosticAnalyzer
{
    private const string ValidateProtoIdAttributeType =
        "Robust.Shared.Serialization.Manager.Attributes.ValidatePrototypeIdAttribute`1";

    private static readonly string[] TypedProtoIdTypes =
    [
        "Robust.Shared.Prototypes.EntProtoId",
        "Robust.Shared.Prototypes.EntProtoId`1",
        "Robust.Shared.Prototypes.ProtoId`1",
    ];

    private static readonly DiagnosticDescriptor RuleRedundantValidateAttribute = new(
        Diagnostics.IdUnnecessaryValidateProtoId,
        "Redundant [ValidatePrototypeId<T>] attribute",
        "Redundant [ValidatePrototypeId<T>] attribute",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "[ValidatePrototypeId<T>] is only necessary for fields static containing raw strings, not fields containing ProtoId<T> or similar types.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RuleRedundantValidateAttribute];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var validateProtoIdAttributeType =
                compilationContext.Compilation.GetTypeByMetadataName(ValidateProtoIdAttributeType);
            var typeIEnumerable =
                compilationContext.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
            if (validateProtoIdAttributeType == null)
                return;

            INamedTypeSymbol[] badTypes = TypedProtoIdTypes
                .Select(n => compilationContext.Compilation.GetTypeByMetadataName(n))
                .Where(t => t != null)
                .ToArray()!;

            if (badTypes.Length == 0)
                return;

            compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    if (symbolContext.Symbol is not IFieldSymbol field)
                        return;

                    if (!field.IsStatic)
                        return;

                    if (!HasValidateAttribute(field, validateProtoIdAttributeType, out var attr))
                        return;

                    if (field.Type is not INamedTypeSymbol fieldType)
                        return;

                    if (!HasBadFieldType(fieldType, badTypes))
                    {
                        if (GetSecondTypeForAnalysis(fieldType, typeIEnumerable) is { } second
                            && !HasBadFieldType(second, badTypes))
                            return;
                    }

                    var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? field.Locations[0];
                    symbolContext.ReportDiagnostic(Diagnostic.Create(RuleRedundantValidateAttribute, location));
                },
                SymbolKind.Field);
        });
    }

    private static bool HasValidateAttribute(
        IFieldSymbol field,
        INamedTypeSymbol attributeType,
        [NotNullWhen(true)] out AttributeData? foundAttr)
    {
        var attrs = field.GetAttributes();
        foreach (var attr in attrs)
        {
            if (attr.AttributeClass is not { IsGenericType: true })
                continue;

            var baseGenericType = attr.AttributeClass.ConstructedFrom;
            if (SymbolEqualityComparer.Default.Equals(baseGenericType, attributeType))
            {
                foundAttr = attr;
                return true;
            }
        }

        foundAttr = null;
        return false;
    }

    private static bool HasBadFieldType(INamedTypeSymbol namedType, INamedTypeSymbol[] badSymbols)
    {
        if (namedType.IsGenericType)
            namedType = namedType.ConstructedFrom;

        foreach (var badSymbol in badSymbols)
        {
            if (SymbolEqualityComparer.Default.Equals(badSymbol, namedType))
                return true;
        }

        return false;
    }

    private static INamedTypeSymbol? GetSecondTypeForAnalysis(INamedTypeSymbol type, INamedTypeSymbol typeIEnumerable)
    {
        foreach (var interfaceType in type.AllInterfaces)
        {
            if (interfaceType.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(interfaceType.ConstructedFrom, typeIEnumerable))
            {
                return interfaceType.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }
}
