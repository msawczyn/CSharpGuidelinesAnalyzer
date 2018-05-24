using System;
using System.Collections.Immutable;
using System.Threading;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CSharpGuidelinesAnalyzer.Rules.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SuffixAsyncMethodCorrectlyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1755";

        private const string Title = "Name of async method should end with Async or TaskAsync";
        private const string MessageFormat = "Name of async {0} '{1}' should end with Async or TaskAsync.";
        private const string Description = "Postfix asynchronous methods with Async or TaskAsync.";

        [NotNull]
        private static readonly AnalyzerCategory Category = AnalyzerCategory.Naming;

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category.DisplayName, DiagnosticSeverity.Warning, true, Description, Category.GetHelpLinkUri(DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeMethod), SymbolKind.Method);
            context.RegisterOperationAction(c => c.SkipInvalid(AnalyzeLocalFunction), OperationKind.LocalFunction);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            AnalyzeSymbol(method, context.Compilation, context.ReportDiagnostic, context.CancellationToken);
        }

        private void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            var operation = (ILocalFunctionOperation)context.Operation;

            AnalyzeSymbol(operation.Symbol, context.Compilation, context.ReportDiagnostic, context.CancellationToken);
        }

        private static void AnalyzeSymbol([NotNull] IMethodSymbol method, [NotNull] Compilation compilation,
            [NotNull] Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (method.IsAsync && !method.Name.EndsWith("Async", StringComparison.Ordinal) && !method.IsSynthesized())
            {
                IMethodSymbol entryPoint = method.MethodKind == MethodKind.Ordinary
                    ? compilation.GetEntryPoint(cancellationToken)
                    : null;

                if (!Equals(method, entryPoint))
                {
                    reportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.GetKind().ToLowerInvariant(),
                        method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
                }
            }
        }
    }
}
