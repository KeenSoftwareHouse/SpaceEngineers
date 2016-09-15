using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using VRage.Compiler;

namespace VRage.Scripting.Rewriters
{
    class InstructionCountingRewriter : CSharpSyntaxRewriter
    {
        internal static readonly SyntaxAnnotation INJECTED_ANNOTATION = new SyntaxAnnotation("Injected");

        /// <summary>
        ///     Injected nodes should not be whitelist checked, so they are tagged with an
        ///     annotation to allow the whitelist analyzer to skip them.
        /// </summary>
        /// <param name="identifierName"></param>
        /// <returns></returns>
        static NameSyntax AnnotatedIdentifier(string identifierName)
        {
            var dotIndex = identifierName.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                return Annotated(SyntaxFactory.QualifiedName(
                    AnnotatedIdentifier(identifierName.Substring(0, dotIndex)),
                    Annotated(SyntaxFactory.IdentifierName(identifierName.Substring(dotIndex + 1)))
                    ));
            }
            return Annotated(SyntaxFactory.IdentifierName(identifierName));
        }

        /// <summary>
        ///     Injected nodes should not be whitelist checked, so they are tagged with an
        ///     annotation to allow the whitelist analyzer to skip them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        static T Annotated<T>(T node) where T : SyntaxNode
        {
            return node.WithAdditionalAnnotations(INJECTED_ANNOTATION);
        }

        readonly CSharpCompilation m_compilation;

        readonly MyScriptCompiler m_compiler;
        SemanticModel m_semanticModel;
        SyntaxTree m_syntaxTree;

        public InstructionCountingRewriter(MyScriptCompiler compiler, CSharpCompilation compilation, SyntaxTree syntaxTree)
        {
            m_compiler = compiler;
            m_compilation = compilation;
            m_syntaxTree = syntaxTree;
        }

        /// <summary>
        ///     Creates a call to the instruction counter.
        /// </summary>
        /// <returns></returns>
        StatementSyntax InstructionCounterCall()
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        AnnotatedIdentifier(typeof(IlInjector).FullName),
                        (SimpleNameSyntax)AnnotatedIdentifier("CountInstructions"))
                    )
                );
        }

        /// <summary>
        ///     Creates a call to the call chain depth counter.
        /// </summary>
        /// <returns></returns>
        StatementSyntax EnterMethodCall()
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        AnnotatedIdentifier(typeof(IlInjector).FullName),
                        (SimpleNameSyntax)AnnotatedIdentifier("EnterMethod"))
                    )
                );
        }

        /// <summary>
        ///     Creates a call to the call chain depth counter.
        /// </summary>
        /// <returns></returns>
        StatementSyntax ExitMethodCall()
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        AnnotatedIdentifier(typeof(IlInjector).FullName),
                        (SimpleNameSyntax)AnnotatedIdentifier("ExitMethod"))
                    )
                );
        }

        /// <summary>
        ///     Creates a code block which is barricaded behind line directives, so they will for all intents and purposes
        ///     be invisible for the compiler when reporting errors.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resumePosition"></param>
        /// <param name="syntax"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        T Barricaded<T>(string path, LinePosition resumePosition, T syntax) where T : SyntaxNode
        {
            return syntax
                .WithTrailingTrivia(
                    SyntaxFactory.Whitespace("\n"),
                    SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Literal(resumePosition.Line + 1),
                        SyntaxFactory.Literal(path), true)),
                    SyntaxFactory.Whitespace("\n" + new string(' ', resumePosition.Character + 1))
                );
        }

        /// <summary>
        ///     Creates a code block which is barricaded behind line directives, so they will for all intents and purposes
        ///     be invisible for the compiler when reporting errors.
        /// </summary>
        /// <param name="resumePosition"></param>
        /// <param name="syntax"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        SyntaxToken Barricaded(string path, LinePosition resumePosition, SyntaxToken syntax)
        {
            return syntax
                .WithTrailingTrivia(
                    SyntaxFactory.Whitespace("\n"),
                    SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Literal(resumePosition.Line + 1),
                        SyntaxFactory.Literal(path), true)),
                    SyntaxFactory.Whitespace("\n" + new string(' ', resumePosition.Character + 1))
                );
        }

        /// <summary>
        ///     Gets the locations to generate #line pragmas for to generate correct error messages, to be used with the
        ///     <see cref="InjectedBlock" /> method.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        FileLinePositionSpan GetBlockResumeLocation(SyntaxNode node)
        {
            var block = node as BlockSyntax;
            if (block != null)
            {
                if (block.Statements.Count == 0)
                {
                    return block.CloseBraceToken.GetLocation().GetMappedLineSpan();
                }
                return block.Statements[0].GetLocation().GetMappedLineSpan();
            }

            return node.GetLocation().GetMappedLineSpan();
        }

        /// <summary>
        ///     Either injects counter methods into an existing block syntax, or generates a block syntax to place the counter
        ///     method in.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="resumeLocation"></param>
        /// <param name="injection"></param>
        /// <returns></returns>
        BlockSyntax InjectedBlock(StatementSyntax node, FileLinePositionSpan resumeLocation, StatementSyntax injection = null)
        {
            injection = injection ?? InstructionCounterCall();
            var block = node as BlockSyntax;
            if (block != null)
            {
                return block.WithStatements(block.Statements.Insert(0, Barricaded(resumeLocation.Path, resumeLocation.StartLinePosition, injection)));
            }

            var syntaxList = new SyntaxList<StatementSyntax>()
                .Add(Barricaded(resumeLocation.Path, resumeLocation.StartLinePosition, injection))
                .Add(node);

            return SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                syntaxList,
                Barricaded(resumeLocation.Path, resumeLocation.EndLinePosition, SyntaxFactory.Token(SyntaxKind.CloseBraceToken)));
        }

        /// <summary>
        ///     Generates the instruction counter and call chain depth counter for any form of method (except properties which must
        ///     be handled by themselves).
        /// </summary>
        /// <param name="node"></param>
        /// <param name="blockResumeLocation"></param>
        /// <returns></returns>
        SyntaxNode ProcessMethod(BaseMethodDeclarationSyntax node, FileLinePositionSpan blockResumeLocation)
        {
            var decl = node as MethodDeclarationSyntax;
            if (decl != null && decl.ExpressionBody != null)
            {
                var returnType = decl.ReturnType as PredefinedTypeSyntax;
                var isVoid = returnType != null && returnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                  return decl
                      .WithExpressionBody(null)
                      .WithBody(
                    CreateDelegateMethodBody(decl.ExpressionBody.Expression, blockResumeLocation, !isVoid)
                    );
            }
            
            return node.WithBody(
                Barricaded(blockResumeLocation.Path, blockResumeLocation.EndLinePosition,
                    SyntaxFactory.Block(
                        InstructionCounterCall(),
                        EnterMethodCall(),
                        SyntaxFactory.TryStatement(
                            SyntaxFactory.Block(
                                Barricaded(blockResumeLocation.Path, blockResumeLocation.StartLinePosition, SyntaxFactory.Token(SyntaxKind.OpenBraceToken)),
                                new SyntaxList<StatementSyntax>()
                                    .Add(node.Body),
                                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                                ),
                            default(SyntaxList<CatchClauseSyntax>),
                            Annotated(
                                SyntaxFactory.FinallyClause(
                                    SyntaxFactory.Block(
                                        ExitMethodCall()
                                        )
                                    )
                                )
                            )
                        )
                    ));
        }

        /// <summary>
        /// Replaces an expression based method declaration with a block based one in order to facilitate instruction counting.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="blockResumeLocation"></param>
        /// <param name="hasReturnValue"></param>
        /// <returns></returns>
        BlockSyntax CreateDelegateMethodBody(ExpressionSyntax expression, FileLinePositionSpan blockResumeLocation, bool hasReturnValue)
        {
            if (hasReturnValue)
            {
                return Barricaded(blockResumeLocation.Path, blockResumeLocation.EndLinePosition,
                    SyntaxFactory.Block(
                        InstructionCounterCall(),
                        EnterMethodCall(),
                        SyntaxFactory.TryStatement(
                            SyntaxFactory.Block(
                                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                                new SyntaxList<StatementSyntax>()
                                    .Add(SyntaxFactory.ReturnStatement(
                                        Barricaded(blockResumeLocation.Path, blockResumeLocation.StartLinePosition, SyntaxFactory.Token(SyntaxKind.ReturnKeyword)),
                                        expression,
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                                        )),
                                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                                ),
                            default(SyntaxList<CatchClauseSyntax>),
                            Annotated(
                                SyntaxFactory.FinallyClause(
                                    SyntaxFactory.Block(
                                        ExitMethodCall()
                                        )
                                    )
                                )
                            )
                        )
                    );
            }
            return Barricaded(blockResumeLocation.Path, blockResumeLocation.EndLinePosition,
                SyntaxFactory.Block(
                    InstructionCounterCall(),
                    EnterMethodCall(),
                    SyntaxFactory.TryStatement(
                        SyntaxFactory.Block(
                            Barricaded(blockResumeLocation.Path, blockResumeLocation.StartLinePosition, SyntaxFactory.Token(SyntaxKind.OpenBraceToken)),
                            new SyntaxList<StatementSyntax>()
                                .Add(SyntaxFactory.ExpressionStatement(expression)),
                            SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                            ),
                        default(SyntaxList<CatchClauseSyntax>),
                        Annotated(
                            SyntaxFactory.FinallyClause(
                                SyntaxFactory.Block(
                                    ExitMethodCall()
                                    )
                                )
                            )
                        )
                    )
                );
        }

        /// <summary>
        ///     Generates the instruction counter and call chain depth counter for any form of delegate (anonymous methods,
        ///     lambdas).
        /// </summary>
        /// <param name="node"></param>
        /// <param name="blockResumeLocation"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        SyntaxNode ProcessAnonymousFunction(AnonymousFunctionExpressionSyntax node, FileLinePositionSpan blockResumeLocation, INamedTypeSymbol type)
        {
            var bodyBlock = node.Body as BlockSyntax;
            if (bodyBlock != null)
            {
                node = node.WithBody(InjectedBlock(bodyBlock, blockResumeLocation));
                return node;
            }

            if (type == null || type.DelegateInvokeMethod == null)
            {
                // We don't know what's going on here, probably some kind of syntax error. Let the compiler deal with it.
                return node;
            }
            return node.WithBody(
                CreateDelegateMethodBody((ExpressionSyntax)node.Body, blockResumeLocation, !type.DelegateInvokeMethod.ReturnsVoid)
                );
        }

        //public override SyntaxNode Visit(SyntaxNode node)
        //{
        //    if (node == null)
        //    {
        //        return null; // ???
        //    }
        //    Debug.WriteLine(node.Kind());
        //    Debug.Indent();
        //    if (node is IdentifierNameSyntax)
        //    {
        //        Debug.WriteLine(node.GetText());
        //    }
        //    node = base.Visit(node);
        //    Debug.Unindent();
        //    return node;
        //}

        public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
        {
            // Goal: Inject instruction counter, but also inject an auto catcher for all exceptions that have been marked
            // as unblockable in the compiler.

            var blockResumeLocation = GetBlockResumeLocation(node.Block);
            var successiveBlockLocation = GetBlockResumeLocation((SyntaxNode)node.Catches.FirstOrDefault() ?? node.Finally);
            var catchClauseLocations = node.Catches.Select(c => GetBlockResumeLocation(c.Block)).ToArray();
            var finallyLocation = node.Finally != null ? GetBlockResumeLocation(node.Finally.Block) : new FileLinePositionSpan();

            node = (TryStatementSyntax)base.VisitTryStatement(node);

            node = node.WithBlock(InjectedBlock(node.Block, blockResumeLocation));

            var catches = new SyntaxList<CatchClauseSyntax>();
            foreach (var exceptionType in m_compiler.UnblockableIngameExceptions)
            {
                catches = catches.Add(Barricaded(successiveBlockLocation.Path, successiveBlockLocation.StartLinePosition, SyntaxFactory.CatchClause(
                    SyntaxFactory.CatchDeclaration(AnnotatedIdentifier(exceptionType.FullName)),
                    null,
                    SyntaxFactory.Block(
                        SyntaxFactory.ThrowStatement())
                    )));
            }

            for (var i = 0; i < node.Catches.Count; i++)
            {
                var catchClause = node.Catches[i];
                var resumeLocation = catchClauseLocations[i];
                catches = catches.Add(catchClause.WithBlock(InjectedBlock(catchClause.Block, resumeLocation)));
            }

            node = node.WithCatches(catches);

            if (node.Finally != null)
            {
                node = node.WithFinally(node.Finally.WithBlock(InjectedBlock(node.Finally.Block, finallyLocation)));
            }

            return node;
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Statement);

            node = (IfStatementSyntax)base.VisitIfStatement(node);
            node = node.WithStatement(InjectedBlock(node.Statement, blockResumeLocation));
            return node;
        }

        public override SyntaxNode VisitElseClause(ElseClauseSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Statement);

            node = (ElseClauseSyntax)base.VisitElseClause(node);

            // We leave "else if" alone.
            if (node.Statement.Kind() == SyntaxKind.IfStatement)
            {
                return node;
            }
            node = node.WithStatement(InjectedBlock(node.Statement, blockResumeLocation));
            return node;
        }

        public override SyntaxNode VisitGotoStatement(GotoStatementSyntax node)
        {
            if (node.CaseOrDefaultKeyword.Kind() != SyntaxKind.None)
                return base.VisitGotoStatement(node);

            var resumeLocation =  node.GetLocation().GetMappedLineSpan();

            node = (GotoStatementSyntax)base.VisitGotoStatement(node);

            return InjectedBlock(node, resumeLocation);
        }

        public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node)
        {
            var resumeLocation = node.Statements.Count > 0 ? node.Statements[0].GetLocation().GetMappedLineSpan() : new FileLinePositionSpan();

            node = (SwitchSectionSyntax)base.VisitSwitchSection(node);
            if (node.Statements.Count > 0)
            {
                node = node.WithStatements(node.Statements.Insert(0,
                    Barricaded(resumeLocation.Path, resumeLocation.StartLinePosition, InstructionCounterCall())));
            }
            return node;
        }

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Statement);

            node = (DoStatementSyntax)base.VisitDoStatement(node);

            node = node.WithStatement(InjectedBlock(node.Statement, blockResumeLocation));
            return node;
        }

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Statement);

            node = (WhileStatementSyntax)base.VisitWhileStatement(node);

            node = node.WithStatement(InjectedBlock(node.Statement, blockResumeLocation));
            return node;
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Statement);

            node = (ForEachStatementSyntax)base.VisitForEachStatement(node);

            node = node.WithStatement(InjectedBlock(node.Statement, blockResumeLocation));
            return node;
        }

        public override SyntaxNode VisitForStatement(ForStatementSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Statement);

            node = (ForStatementSyntax)base.VisitForStatement(node);

            node = node.WithStatement(InjectedBlock(node.Statement, blockResumeLocation));
            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.ExpressionBody != null)
            {
                var blockResumeLocation = GetBlockResumeLocation(node.ExpressionBody);

                node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node);
                return node.WithExpressionBody(null)
                    .WithAccessorList(SyntaxFactory.AccessorList(new SyntaxList<AccessorDeclarationSyntax>()
                        .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, CreateDelegateMethodBody(node.ExpressionBody.Expression, blockResumeLocation, true)))
                    ));
            }
            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.Body == null)
            {
                return base.VisitAccessorDeclaration(node);
            }

            var blockResumeLocation = GetBlockResumeLocation(node.Body);

            node = (AccessorDeclarationSyntax)base.VisitAccessorDeclaration(node);

            return node.WithBody(
                Barricaded(blockResumeLocation.Path, blockResumeLocation.EndLinePosition,
                    SyntaxFactory.Block(
                        InstructionCounterCall(),
                        EnterMethodCall(),
                        SyntaxFactory.TryStatement(
                            SyntaxFactory.Block(
                                Barricaded(blockResumeLocation.Path, blockResumeLocation.StartLinePosition, SyntaxFactory.Token(SyntaxKind.OpenBraceToken)),
                                new SyntaxList<StatementSyntax>()
                                    .Add(node.Body),
                                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                                ),
                            default(SyntaxList<CatchClauseSyntax>),
                            Annotated(
                                SyntaxFactory.FinallyClause(
                                    SyntaxFactory.Block(
                                        ExitMethodCall()
                                        )
                                    )
                                )
                            )
                        )
                    ));
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Body == null)
            {
                return base.VisitConstructorDeclaration(node);
            }

            var blockResumeLocation = GetBlockResumeLocation(node.Body);

            node = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node);

            return ProcessMethod(node, blockResumeLocation);
        }

        public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            if (node.Body == null)
            {
                return base.VisitDestructorDeclaration(node);
            }

            var blockResumeLocation = GetBlockResumeLocation(node.Body);

            node = (DestructorDeclarationSyntax)base.VisitDestructorDeclaration(node);

            return ProcessMethod(node, blockResumeLocation);
        }

        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            if (node.Body == null && node.ExpressionBody != null)
            {
                return base.VisitOperatorDeclaration(node);
            }
            
            var blockResumeLocation = GetBlockResumeLocation((SyntaxNode)node.Body ?? node.ExpressionBody);

            node = (OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node);

            return ProcessMethod(node, blockResumeLocation);
        }

        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            if (node.Body == null && node.ExpressionBody == null)
            {
                return base.VisitConversionOperatorDeclaration(node);
            }

            var blockResumeLocation = GetBlockResumeLocation((SyntaxNode)node.Body ?? node.ExpressionBody);

            node = (ConversionOperatorDeclarationSyntax)base.VisitConversionOperatorDeclaration(node);

            return ProcessMethod(node, blockResumeLocation);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Body == null && node.ExpressionBody == null)
            {
                return base.VisitMethodDeclaration(node);
            }

            var blockResumeLocation = GetBlockResumeLocation((SyntaxNode)node.Body ?? node.ExpressionBody);

            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            return ProcessMethod(node, blockResumeLocation);
        }

        public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Body);
            var type = m_semanticModel.GetTypeInfo(node).ConvertedType as INamedTypeSymbol;

            node = (AnonymousMethodExpressionSyntax)base.VisitAnonymousMethodExpression(node);
            return ProcessAnonymousFunction(node, blockResumeLocation, type);
        }

        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Body);
            var type = m_semanticModel.GetTypeInfo(node).ConvertedType as INamedTypeSymbol;

            node = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node);
            return ProcessAnonymousFunction(node, blockResumeLocation, type);
        }

        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            var blockResumeLocation = GetBlockResumeLocation(node.Body);
            var type = m_semanticModel.GetTypeInfo(node).ConvertedType as INamedTypeSymbol;

            node = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node);
            return ProcessAnonymousFunction(node, blockResumeLocation, type);
        }

        /// <summary>
        ///     Creates a new rewritten syntax tree with instruction- and call chain depth counting.
        /// </summary>
        /// <returns></returns>
        public async Task<SyntaxTree> Rewrite()
        {
            var root = (CSharpSyntaxNode)await m_syntaxTree.GetRootAsync().ConfigureAwait(false);
            m_semanticModel = m_compilation.GetSemanticModel(m_syntaxTree);
            root = (CSharpSyntaxNode)Visit(root);
            return CSharpSyntaxTree.Create(root, path: m_syntaxTree.FilePath);
        }
    }
}