using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ValidateProtoIdAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(ValidateProtoIdAnalyzer))]
public sealed class ValidateProtoIdTest
{
    private const string Defs = """
        using System;

        namespace Robust.Shared.Serialization.Manager.Attributes
        {
            [AttributeUsage(AttributeTargets.Field)]
            public sealed class ValidatePrototypeIdAttribute<T> : Attribute
            {
            }
        }

        namespace Robust.Shared.Prototypes
        {
            public readonly struct ProtoId<T>(string Id);
        }
        """;

    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ValidateProtoIdAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        test.TestState.Sources.Add(("Deps.cs", Defs));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using System.Collections.Generic;
            using Robust.Shared.Serialization.Manager.Attributes;
            using Robust.Shared.Prototypes;

            public sealed class Foo
            {
                [ValidatePrototypeIdAttribute<object>]
                public const string Good = "Foobar";

                [ValidatePrototypeIdAttribute<object>]
                public static readonly ProtoId<object> Bad = new("Foobar");

                [ValidatePrototypeIdAttribute<object>]
                public static readonly List<ProtoId<object>> BadCollection = [new("Foobar")];
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(10,6): info RA0033: Redundant [ValidateProtoId<T>] attribute
            VerifyCS.Diagnostic().WithSpan(10, 6, 10, 42),
            // /0/Test0.cs(13,6): info RA0033: Redundant [ValidateProtoId<T>] attribute
            VerifyCS.Diagnostic().WithSpan(13, 6, 13, 42));
    }
}
