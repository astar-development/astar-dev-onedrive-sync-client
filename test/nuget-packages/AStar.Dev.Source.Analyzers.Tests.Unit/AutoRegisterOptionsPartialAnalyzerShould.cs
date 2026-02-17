using System.Threading.Tasks;
using AStar.Dev.Source.Analyzers.Tests.Unit.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace AStar.Dev.Source.Analyzers.Tests.Unit;

public sealed class AutoRegisterOptionsPartialAnalyzerShould
{
    [Fact]
    public async Task ReportErrorWhenRecordStructIsNotReadonly()
    {
        const string source = "using System;\n\n" +
                              "namespace AStar.Dev.Source.Generators.Attributes\n" +
                              "{\n" +
                              "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]\n" +
                              "    public sealed class AutoRegisterOptionsAttribute : Attribute\n" +
                              "    {\n" +
                              "        public AutoRegisterOptionsAttribute() { }\n" +
                              "    }\n" +
                              "}\n\n" +
                              "namespace TestNamespace\n" +
                              "{\n" +
                              "    [AStar.Dev.Source.Generators.Attributes.AutoRegisterOptions]\n" +
                              "    public partial record struct MyOptions { }\n" +
                              "}\n";

        DiagnosticResult expected = AnalyzerVerifier<AutoRegisterOptionsPartialAnalyzer>
            .Diagnostic(AutoRegisterOptionsPartialAnalyzer.ReadonlyRecordDiagnosticId)
            .WithLocation(15, 34)
            .WithArguments("MyOptions");

        await AnalyzerVerifier<AutoRegisterOptionsPartialAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NotReportErrorWhenRecordStructIsReadonly()
    {
        const string source = "using System;\n\n" +
                              "namespace AStar.Dev.Source.Generators.Attributes\n" +
                              "{\n" +
                              "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]\n" +
                              "    public sealed class AutoRegisterOptionsAttribute : Attribute\n" +
                              "    {\n" +
                              "        public AutoRegisterOptionsAttribute() { }\n" +
                              "    }\n" +
                              "}\n\n" +
                              "namespace TestNamespace\n" +
                              "{\n" +
                              "    [AStar.Dev.Source.Generators.Attributes.AutoRegisterOptions]\n" +
                              "    public readonly partial record struct MyOptions { }\n" +
                              "}\n";

        await AnalyzerVerifier<AutoRegisterOptionsPartialAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
