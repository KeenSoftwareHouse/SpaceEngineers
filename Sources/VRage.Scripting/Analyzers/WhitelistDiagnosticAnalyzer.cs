using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sandbox.ModAPI;
using VRage.Scripting.Rewriters;

namespace VRage.Scripting.Analyzers
{
    /// <summary>
    ///     This analyzer scans a syntax tree for prohibited type and member references.
    /// </summary>
    internal class WhitelistDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        // TODO: Do we translate these messages?
        internal static readonly DiagnosticDescriptor PROHIBITED_MEMBER_RULE
            = new DiagnosticDescriptor("ProhibitedMemberRule", "Prohibited Type Or Member", "The type or member '{0}' is prohibited", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor PROHIBITED_LANGUAGE_ELEMENT_RULE
            = new DiagnosticDescriptor("ProhibitedLanguageElement", "Prohibited Language Element", "The language element '{0}' is prohibited", "Whitelist", DiagnosticSeverity.Error, true);

        readonly MyScriptWhitelist m_whitelist;
        readonly MyWhitelistTarget m_target;
        readonly ImmutableArray<DiagnosticDescriptor> m_supportedDiagnostics = ImmutableArray.Create(PROHIBITED_MEMBER_RULE, PROHIBITED_LANGUAGE_ELEMENT_RULE);

        public WhitelistDiagnosticAnalyzer(MyScriptWhitelist whitelist, MyWhitelistTarget target)
        {
            m_whitelist = whitelist;
            m_target = target;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze,
                //SyntaxKind.FinallyClause,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.QualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName);
        }

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;

            // Injected nodes are not checked.
            if (node.HasAnnotation(InstructionCountingRewriter.INJECTED_ANNOTATION))
            {
                return;
            }

            // The exception finally clause cannot be allowed ingame because it can be used
            // to circumvent the instruction counter exception and crash the game
            if (node.Kind() == SyntaxKind.FinallyClause)
            {
                var kw = ((FinallyClauseSyntax)node).FinallyKeyword;
                if (m_target == MyWhitelistTarget.Ingame)
                {
                    var diagnostic = Diagnostic.Create(PROHIBITED_LANGUAGE_ELEMENT_RULE, kw.GetLocation(), kw.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
                return;
            }

            // We'll check the qualified names on their own.
            if (IsQualifiedName(node.Parent))
            {
                //if (node.Ancestors().Any(IsQualifiedName))
                return;
            }

            var info = context.SemanticModel.GetSymbolInfo(node);
            if (info.Symbol == null)
            {
                return;
            }

            // If they wrote it, they can have it.
            if (info.Symbol.IsInSource())
            {
                return;
            }

            if (!m_whitelist.IsWhitelisted(info.Symbol, m_target))
            {
                var diagnostic = Diagnostic.Create(PROHIBITED_MEMBER_RULE, node.GetLocation(), info.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                context.ReportDiagnostic(diagnostic);
            }
        }

        bool IsQualifiedName(SyntaxNode arg)
        {
            switch (arg.Kind())
            {
                case SyntaxKind.QualifiedName:
                case SyntaxKind.AliasQualifiedName:
                    return true;
            }
            return false;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return m_supportedDiagnostics; }
        }
    }
}