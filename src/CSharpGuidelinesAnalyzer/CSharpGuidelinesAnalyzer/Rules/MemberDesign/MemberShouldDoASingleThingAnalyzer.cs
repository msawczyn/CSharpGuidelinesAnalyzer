using System;
using System.Collections.Immutable;
using System.Linq;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CSharpGuidelinesAnalyzer.Rules.MemberDesign
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MemberShouldDoASingleThingAnalyzer : GuidelineAnalyzer
    {
        public const string DiagnosticId = "AV1115";

        private const string Title = "Member or local function contains the word 'and', which suggests it does multiple things";
        private const string MessageFormat = "{0} '{1}' contains the word 'and', which suggests it does multiple things.";
        private const string Description = "A property, method or local function should do only one thing.";

        [NotNull]
        private static readonly AnalyzerCategory Category = AnalyzerCategory.MemberDesign;

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category.Name, DiagnosticSeverity.Warning, true, Description, Category.GetHelpLinkUri(DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly ImmutableArray<SymbolKind> MemberSymbolKinds =
            ImmutableArray.Create(SymbolKind.Property, SymbolKind.Method, SymbolKind.Field, SymbolKind.Event);

        [ItemNotNull]
        private static readonly ImmutableArray<string> WordsBlacklist = ImmutableArray.Create("and");

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeMember), MemberSymbolKinds);
            context.RegisterOperationAction(c => c.SkipInvalid(AnalyzeLocalFunction), OperationKind.LocalFunction);
        }

        private void AnalyzeMember(SymbolAnalysisContext context)
        {
            AnalyzeSymbol(context.Symbol, context.ReportDiagnostic, context.Symbol.Kind.ToString());
        }

        private void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            var operation = (ILocalFunctionOperation)context.Operation;

            AnalyzeSymbol(operation.Symbol, context.ReportDiagnostic, "Local function");
        }

        private static void AnalyzeSymbol([NotNull] ISymbol symbol, [NotNull] Action<Diagnostic> reportDiagnostic,
            [NotNull] string kind)
        {
            if (symbol.IsPropertyOrEventAccessor() || symbol.IsUnitTestMethod())
            {
                return;
            }

            if (ContainsBlacklistedWord(symbol.Name))
            {
                reportDiagnostic(Diagnostic.Create(Rule, symbol.Locations[0], kind, symbol.Name));
            }
        }

        private static bool ContainsBlacklistedWord([NotNull] string name)
        {
            return name.GetWordsInList(WordsBlacklist).Any();
        }
    }
}