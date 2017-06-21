using System;
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
        /// Creates a call to determine wheather or not is given script instance dead.
        /// </summary>
        /// <returns></returns>
        ExpressionSyntax IsDeadCall()
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    AnnotatedIdentifier(typeof(IlInjector).FullName),
                    (SimpleNameSyntax)AnnotatedIdentifier("IsDead"))
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
            //var decl = node as MethodDeclarationSyntax;
            //if (decl != null && decl.ExpressionBody != null)
            //Inflex correction: Original implementation missdetected expression bodied methods as statement bodied in certain cases, causing NPE and ultimately game crash
            if (node.Body != null)
            {
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
            var method = node as MethodDeclarationSyntax;
            if (method != null)
            {
                var returnType = method.ReturnType as PredefinedTypeSyntax;
                var isVoid = returnType != null && returnType.Keyword.IsKind(SyntaxKind.VoidKeyword);

                return method.WithExpressionBody(null)
                             .WithBody(CreateDelegateMethodBody(method.ExpressionBody.Expression, blockResumeLocation, isVoid == false));
            }

            var opOperator = node as OperatorDeclarationSyntax;
            if (opOperator != null)
            {
                return opOperator.WithExpressionBody(null)
                                 .WithBody(CreateDelegateMethodBody(opOperator.ExpressionBody.Expression, blockResumeLocation, true));
            }

            var conversionOperator = node as ConversionOperatorDeclarationSyntax;
            if (conversionOperator != null)
            {
                return conversionOperator.WithExpressionBody(null)
                                         .WithBody(CreateDelegateMethodBody(conversionOperator.ExpressionBody.Expression, blockResumeLocation, true));
            }

            if (node is ConstructorDeclarationSyntax || node is DestructorDeclarationSyntax)
            {
                //Ctor or Dtor with null body??
                //VisitConstructorDeclaration and VisitDestructorDeclaration should handle this case
                throw new ArgumentException("Constructors and destructors have to have bodies!", "node"); //nameof(node));
            }

            throw new ArgumentException("Unknown " + node.GetType().FullName, "node"); //nameof(node));
        }


        /// <summary>
        /// Replaces an expression based method declaration with a block based one in order to facilitate instruction and method call chain counting.
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
        /// Generates dead checking if statement with injected body block 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="resumeLocation"></param>
        /// <returns></returns>
        StatementSyntax DeadCheckIfStatement(StatementSyntax body, FileLinePositionSpan resumeLocation)
        {
            return SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        IsDeadCall()),
                    InjectedBlock(body, resumeLocation)
                );
        }

        /// <summary>
        /// Generates uniq identifier based on location.
        /// Cross fingers and prey for no user identifier collisions
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        static string GenerateUniqIdentifier(FileLinePositionSpan location)
        {
            return "IWantToCollideWithHiddenCompilerVariable_" + location.StartLinePosition.Line + "_" + location.StartLinePosition.Character;
        }

        /// <summary>
        /// Generates finally clause with injected block
        /// </summary>
        /// <param name="body"></param>
        /// <param name="resumeLocation"></param>
        /// <returns></returns>
        FinallyClauseSyntax InjectedFinally(StatementSyntax body, FileLinePositionSpan resumeLocation)
        {
            return SyntaxFactory.FinallyClause(
                SyntaxFactory.Block(DeadCheckIfStatement(body, resumeLocation))
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

        public override SyntaxNode VisitCatchClause(CatchClauseSyntax node)
        {
            // Goal: Inject instruction counter, but also inject an auto catcher for all exceptions that have been marked
            // as unblockable in the compiler.
            if (node.Span.IsEmpty || node.Block.Span.IsEmpty)
                return base.VisitCatchClause(node);

            var blockResumeLocation = GetBlockResumeLocation(node.Block);
            node = (CatchClauseSyntax)base.VisitCatchClause(node);

            if(node.Declaration == null)
            {
                node = node.WithDeclaration(
                    SyntaxFactory.CatchDeclaration(
                        SyntaxFactory.ParseTypeName(typeof(Exception).FullName),
                        SyntaxFactory.Identifier(GenerateUniqIdentifier(blockResumeLocation))
                    )
                );
            }
            else if(node.Declaration.Type.IsMissing)
            {
                //Invalid catch declaration. Let compiler deal with it
                return node;
            }
            else if(node.Declaration.Identifier.IsKind(SyntaxKind.None))
            {
                node = node.WithDeclaration(
                    node.Declaration.WithIdentifier(
                        SyntaxFactory.Identifier(GenerateUniqIdentifier(blockResumeLocation))
                    )
                );
            }

            var exceptionIdentifier = node.Declaration.Identifier.ValueText;
            var compilerExceptionsFilterConditions = SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                SyntaxFactory.ParenthesizedExpression(
                    this.m_compiler.UnblockableIngameExceptions.Aggregate<Type, BinaryExpressionSyntax>(null, (aggregation, current) =>
                    {
                        var isTypeExpression = SyntaxFactory.BinaryExpression(
                            SyntaxKind.IsExpression,
                            SyntaxFactory.IdentifierName(exceptionIdentifier),
                            AnnotatedIdentifier(current.FullName)
                        );

                        if(aggregation == null)
                            return isTypeExpression;

                        return SyntaxFactory.BinaryExpression(
                            SyntaxKind.LogicalOrExpression,
                            isTypeExpression,
                            aggregation
                        );
                    })
                )
            );

            if (node.Filter == null)
                node = node.WithFilter(SyntaxFactory.CatchFilterClause(compilerExceptionsFilterConditions));
            else
            {
                node = node.WithFilter(
                    SyntaxFactory.CatchFilterClause(
                        SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression,
                            SyntaxFactory.ParenthesizedExpression(
                                node.Filter.FilterExpression
                            ),
                            compilerExceptionsFilterConditions
                        )
                    )
                );
            }
            return node.WithBlock(InjectedBlock(node.Block, blockResumeLocation));
        }

        public override SyntaxNode VisitFinallyClause(FinallyClauseSyntax node)
        {
            if(node.Span.IsEmpty || node.Block.Span.IsEmpty)
                return base.VisitFinallyClause(node);

            var blockResumeLocation = GetBlockResumeLocation(node.Block);
            node = (FinallyClauseSyntax)base.VisitFinallyClause(node);
            return InjectedFinally(node.Block, blockResumeLocation);
        }

        //Goal: Rewrite each `using` statement to try/finally construct to allow easy dead checking
        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
        {
            var usingLocation = GetBlockResumeLocation(node);

            ExpressionSyntax disposable = null;
            StatementSyntax disposableInstantiation = null;
            if(node.Declaration != null && node.Declaration.Variables.Count > 0)
             {

                disposableInstantiation = SyntaxFactory.LocalDeclarationStatement(node.Declaration);
                disposable = SyntaxFactory.IdentifierName(
                    node.Declaration.Variables[0].Identifier
                );
             }
            else if(node.Expression != null)
            {
                disposable = SyntaxFactory.IdentifierName(GenerateUniqIdentifier(usingLocation));
                disposableInstantiation = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        new SeparatedSyntaxList<VariableDeclaratorSyntax>().Add(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(GenerateUniqIdentifier(usingLocation)),
                                default(BracketedArgumentListSyntax),
                                SyntaxFactory.EqualsValueClause(
                                    node.Expression
                                )
                            )
 
                        )
                    )
                );
            }
 
            if(disposable == null ||
               node.Statement.IsMissing ||
               node.UsingKeyword.IsMissing ||
               node.OpenParenToken.IsMissing ||
               node.CloseParenToken.IsMissing)
             {
                //Invalid using declaration. Let compiler deal with it
                return base.VisitUsingStatement(node);
             }
 

            var blockResumeLocation = GetBlockResumeLocation(node.Statement);
            node = (UsingStatementSyntax)base.VisitUsingStatement(node);

            return SyntaxFactory.Block(
                disposableInstantiation,
                SyntaxFactory.TryStatement(
                InjectedBlock(node.Statement, blockResumeLocation),
                default(SyntaxList<CatchClauseSyntax>),
                InjectedFinally(
                    SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    disposable,
                                    SyntaxFactory.IdentifierName("Dispose")
                                )
                            )
                        )
                    ),
                    usingLocation
                )
            )
            );
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

            var resumeLocation = node.GetLocation().GetMappedLineSpan();

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
            if (node.Body == null && node.ExpressionBody == null)
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

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            if(node.ExpressionBody == null)
                return base.VisitIndexerDeclaration(node);

            if(node.AccessorList != null)
            {
                //Invalid indexer declaration. No need to pass it further. Let compiler deal with it directly.
                return node;
            }

            var blockResumeLocation = GetBlockResumeLocation(node.ExpressionBody);
            node = (IndexerDeclarationSyntax)base.VisitIndexerDeclaration(node);
            return node.WithExpressionBody(null)
                       .WithAccessorList(
                            SyntaxFactory.AccessorList(
                                new SyntaxList<AccessorDeclarationSyntax>().Add(
                                    SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        CreateDelegateMethodBody(
                                            node.ExpressionBody.Expression,
                                            blockResumeLocation,
                                            hasReturnValue: true
                                        )
                                    )
                                )
                            )
                        );
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