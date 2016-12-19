using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRageMath;

namespace VRage.Game.VisualScripting.Utils
{
    public static class MySyntaxFactory
    {

        /// <summary>
        /// Creates using directive from identifiers "System","Collection".
        /// </summary>
        /// <param name="identifiers">Separated identifiers.</param>
        /// <returns>Null if less than 2 identifiers passed.</returns>
        public static UsingDirectiveSyntax UsingStatementSyntax(string @namespace)
        {
            var identifiers = @namespace.Split('.');

            if (identifiers.Length < 2)
                return SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(identifiers[0]));

            var qualifiedName = SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName(identifiers[0]),
                    SyntaxFactory.IdentifierName(identifiers[1])
                );

            for(int index = 2; index < identifiers.Length; index++)
            {
                qualifiedName = SyntaxFactory.QualifiedName(
                        qualifiedName,
                        SyntaxFactory.IdentifierName(identifiers[index])
                    );
            }

            return SyntaxFactory.UsingDirective(qualifiedName);
        }

        /// <summary>
        /// Generic Field declaration creation.
        /// </summary>
        /// <param name="type">Field type.</param>
        /// <param name="fieldVariableName">Name of the field.</param>
        /// <param name="modifiers">Modifiers - null creates public modifier list</param>
        /// <returns>Complete field declaration syntax.</returns>
        public static FieldDeclarationSyntax GenericFieldDeclaration(Type type, string fieldVariableName, SyntaxTokenList? modifiers = null)
        {
            if(modifiers == null)
            {
                modifiers = SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                    );
            }

            return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        GenericTypeSyntax(type),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(fieldVariableName)
                            )
                        )
                    )
                ).WithModifiers(modifiers.Value);
        }

        public static MethodDeclarationSyntax MethodDeclaration(MethodInfo method)
        {
            var parametersSyntax = new List<SyntaxNodeOrToken>();

            var @params = method.GetParameters();
            for (var index = 0; index < @params.Length; index++)
            {
                var parameter = @params[index];
                var syntax = SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier(parameter.Name)
                    ).WithType(GenericTypeSyntax(parameter.ParameterType));
                
                parametersSyntax.Add(syntax);

                // Add comma
                if (index < @params.Length - 1)
                {
                    parametersSyntax.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }
            }

            // Todo: Make it more generic

            return SyntaxFactory.MethodDeclaration(
                    MySyntaxFactory.GenericTypeSyntax(method.ReturnType),
                    SyntaxFactory.Identifier(method.Name)
                ).WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                    )
                ).WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList<ParameterSyntax>(parametersSyntax)
                    )
                ).WithBody(
                    SyntaxFactory.Block()
                );
        }

        /// <summary>
        /// Generic type syntax creation.
        /// </summary>
        /// <param name="type">C# type</param>
        /// <returns>Type Syntax</returns>
        public static TypeSyntax GenericTypeSyntax(Type type)
        {
            if (!type.IsGenericType)
            {
                if (type == typeof(void))
                {
                    return SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.VoidKeyword)
                        );
                }

                return SyntaxFactory.IdentifierName(type.FullName);
            }

            var arguments = type.GetGenericArguments();
            var argumentsSyntaxList = new List<TypeSyntax>();

            foreach (var argument in arguments)
            {
                // For generic types do recursive call
                argumentsSyntaxList.Add(GenericTypeSyntax(argument));
            }

            var genericTypeName = type.Name.Remove(type.Name.IndexOf('`'));

            return SyntaxFactory.GenericName(
                SyntaxFactory.Identifier(genericTypeName), 
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(argumentsSyntaxList)
                )
                );        
        }

        /// <summary>
        /// Creates generic type object creation syntax. new type(argumentExpressions).
        /// </summary>
        /// <param name="type">Type of created object.</param>
        /// <param name="argumentExpressions">Initializer argument expressions.</param>
        /// <returns>Object creatation expression.</returns>
        public static ObjectCreationExpressionSyntax GenericObjectCreation(Type type, IEnumerable<ExpressionSyntax> argumentExpressions = null)
        {
            var argumentList = new List<SyntaxNodeOrToken>();

            var genericArguments = type.GetGenericArguments();

            for (var index = 0; index < genericArguments.Length; index++)
            {
                var identifier = SyntaxFactory.IdentifierName(genericArguments[index].FullName);
                argumentList.Add(identifier);

                if(index < genericArguments.Length - 1)
                    argumentList.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            var genericTypeSyntax = GenericTypeSyntax(type);

            var arguments = new List<ArgumentSyntax>();

            if (argumentExpressions != null)
            {
                foreach (var expression in argumentExpressions)
                {
                    var argument = SyntaxFactory.Argument(expression);
                    arguments.Add(argument);
                }
            }

            return SyntaxFactory.ObjectCreationExpression(
                    genericTypeSyntax
                ).WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(arguments)
                    )
                );
        }

        public static LiteralExpressionSyntax Literal(string typeSignature, string val)
        {
            Debug.Assert(typeSignature != null, "Type signature cannot be null.");
            var type = MyVisualScriptingProxy.GetType(typeSignature);

            if (type != null)
            {
                if(type == typeof(float))
                {
                    var floatVal = string.IsNullOrEmpty(val) ? 0 : float.Parse(val);
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(floatVal));
                }

                if (type == typeof(int) || type == typeof(long))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.ParseToken(val));

                if (type == typeof(bool))
                {
                    var value = MySyntaxFactory.NormalizeBool(val);
                    if (value == "true")
                        return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

                    return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                }
            }

            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(val));
        }

        public static ArgumentSyntax ConstantArgument(string typeSignature, string value)
        {
            var type = MyVisualScriptingProxy.GetType(typeSignature);
            // First handle special cases as Color and Enums
            if (type == typeof(Color) || type.IsEnum)
            {
                return SyntaxFactory.Argument( 
                    SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(typeSignature),
                    SyntaxFactory.IdentifierName(value)
                    ));
            }

            if (type == typeof(Vector3D))
            {
                return SyntaxFactory.Argument(
                    MySyntaxFactory.NewVector3D(value)
                );
            }
            
            return SyntaxFactory.Argument(
                    Literal(typeSignature, value)
                );
        }

        public static ArgumentSyntax ConstantDefaultArgument(Type type)
        {
            if (type.IsClass)
            {
                return SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                    );
            }

            if (type == typeof (int) || type == typeof (float) || type == typeof (long) || type == typeof (double))
            {
                return SyntaxFactory.Argument(
                    Literal(type.Signature(), "0")
                    );
            }

            return SyntaxFactory.Argument(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName(type.Signature())
                    ).WithArgumentList(SyntaxFactory.ArgumentList())
                );
        }

        // new Vector3D(_x,_y,_z)
        public static ObjectCreationExpressionSyntax NewVector3D(string vectorData)
        {
            var splits = vectorData.Split(' ');
            var _x = double.Parse(splits[0].Replace("X:", ""));
            var _y = double.Parse(splits[1].Replace("Y:", ""));
            var _z = double.Parse(splits[2].Replace("Z:", ""));

            return SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName("VRageMath.Vector3D"))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[]{
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(_x))),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(_y))),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(_z)))})));
        }

        /// <summary>
        /// deletageIdentifier += methodName;
        /// </summary>
        /// <param name="deletageIdentifier"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static ExpressionStatementSyntax DelegateAssignment(string deletageIdentifier, string methodName)
        {
            return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        SyntaxFactory.IdentifierName(deletageIdentifier),
                        SyntaxFactory.IdentifierName(methodName)
                    )
                );
        }

        /// <summary>
        /// delegateIdentifier -= methodName
        /// </summary>
        /// <param name="deletageIdentifier"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static ExpressionStatementSyntax DelegateRemoval(string deletageIdentifier, string methodName)
        {
            return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SubtractAssignmentExpression,
                        SyntaxFactory.IdentifierName(deletageIdentifier),
                        SyntaxFactory.IdentifierName(methodName)
                    )
                );
        }

        /// <summary>
        /// Creates public class with empty body.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ClassDeclarationSyntax PublicClass(string name)
        {
            return SyntaxFactory.ClassDeclaration(name)
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                                        )
                                    ).NormalizeWhitespace();
        }

        /// <summary>
        /// Creates basic member declaration with provided name and type.
        /// </summary>
        /// <param name="memberName">Valid name.</param>
        /// <param name="memberType">Valid type.</param>
        /// <returns></returns>
        public static MemberDeclarationSyntax MemberDeclaration(string memberName, string memberType)
        {
            var variableType = SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(memberType)
                    );

            var variableName = SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(memberName)
                                );

            return SyntaxFactory.FieldDeclaration(
                    variableType
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            variableName
                        )
                    )
                ).NormalizeWhitespace();
        }

        /// <summary>
        /// Creates assignment expression for given variableName and value.
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="expressionKind"></param>
        /// <returns></returns>
        public static ExpressionStatementSyntax VariableAssignmentExpression(string variableName, string value, SyntaxKind expressionKind)
        {
            SyntaxToken valueToken;
            bool sign = false;


            if (expressionKind == SyntaxKind.StringLiteralExpression)
            {
                valueToken = SyntaxFactory.Literal(value);
            }
            else if (expressionKind == SyntaxKind.TrueLiteralExpression || expressionKind == SyntaxKind.FalseLiteralExpression)
            {
                return SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(variableName),
                            SyntaxFactory.LiteralExpression(
                                expressionKind
                            )
                        )
                    ).NormalizeWhitespace();
            }
            else
            {
                if (value.Contains('-'))
                {
                    sign = true;
                    value = value.Replace("-", "");
                }

                valueToken = SyntaxFactory.ParseToken(value);
            }

            var literalExp = SyntaxFactory.LiteralExpression(
                            expressionKind,
                            valueToken
                        );

            if (sign)
            {
                return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(variableName),
                        SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.UnaryMinusExpression, 
                            literalExp
                        )
                    )
                ).NormalizeWhitespace();
            }

            return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(variableName),
                        literalExp
                    )
                ).NormalizeWhitespace();
        }

        public static AssignmentExpressionSyntax VariableAssignment(string variableName, ExpressionSyntax rightSide)
        {
            return SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression, 
                    SyntaxFactory.IdentifierName(variableName),
                    rightSide
                );
        }

        public static string NormalizeBool(string value)
        {
            value = value.ToLower();

            if (value == "0") return "false";
            if (value == "1") return "true";

            return value;
        }

        /// <summary>
        /// Creates assignment expression variableName = vectorType(x,y,z);
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="vectorType"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static ExpressionStatementSyntax VectorAssignmentExpression(string variableName, string vectorType, double x, double y, double z)
        {
            // Craete assignment with Object creation
            // vectorVarName = new Vector3(X,Y,Z)
            var argumentX = VectorArgumentSyntax(x);
            var argumentY = VectorArgumentSyntax(y);
            var argumentZ = VectorArgumentSyntax(z);

            var argumentList = SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                        new SyntaxNodeOrToken[]
                            {
                                argumentX,
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                argumentY,
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                argumentZ
                            }
                    )
                );

            var objectCreation = SyntaxFactory.ObjectCreationExpression(
                                        SyntaxFactory.IdentifierName(vectorType)
                                    ).WithArgumentList(
                                        argumentList
                                    );

            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variableName),
                    objectCreation
                )
            ).NormalizeWhitespace();
        }

        /// <summary>
        /// Expression of type "var variableName = new type(values....);"
        /// </summary>
        public static LocalDeclarationStatementSyntax ReferenceTypeInstantiation(string variableName, string type, params LiteralExpressionSyntax[] values)
        {
            var arguments = SyntaxFactory.NodeOrTokenList();

            for (int index = 0; index < values.Length; index++)
            {
                var literalExpressionSyntax = values[index];
                arguments.Add(SyntaxFactory.Argument(literalExpressionSyntax));
                if(index + 1 != values.Length)
                    arguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            var argumentList = SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments)
                );

            var objectCreation = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(type)).WithArgumentList(argumentList);

            return LocalVariable(type, variableName, objectCreation);
        }

        /// <summary>
        /// Creates arguments of type NumericLiteral for given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static ArgumentSyntax VectorArgumentSyntax(double value)
        {
            return SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(value)
                )
            );
        }

        /// <summary>
        /// Craetes expression of type "var resultVariableName = leftSide 'operation' rightSide;"
        /// </summary>
        /// <param name="resultVariableName"></param>
        /// <param name="leftSide"></param>
        /// <param name="rightSide"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public static LocalDeclarationStatementSyntax ArithmeticStatement(string resultVariableName, string leftSide, string rightSide, string operation)
        {
            var varDeclaration = SyntaxFactory.VariableDeclaration(
                     SyntaxFactory.IdentifierName("var")
                 );

            var expression = SyntaxFactory.ParseExpression(leftSide + " " + operation + " " + rightSide);

            var initializer = SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(resultVariableName)
                    ).WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            expression
                        )
                    )
                );

            return SyntaxFactory.LocalDeclarationStatement(
                    varDeclaration.WithVariables(initializer)
                );
        }

        /// <summary>
        /// Creates conditional statement with given statements for true/false clauses and condition syntax.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="statements"></param>
        /// <param name="elseStatements"></param>
        /// <returns></returns>
        public static IfStatementSyntax IfExpressionSyntax(ExpressionSyntax condition, List<StatementSyntax> statements, List<StatementSyntax> elseStatements = null)
        {
            if (elseStatements == null || elseStatements.Count == 0)
                return SyntaxFactory.IfStatement(
                       condition,
                       SyntaxFactory.Block(
                            statements
                       )
                   ).NormalizeWhitespace();

            return SyntaxFactory.IfStatement(
                       condition,
                       SyntaxFactory.Block(
                            statements
                       )
                   ).WithElse(
                       SyntaxFactory.ElseClause(
                           SyntaxFactory.Block(
                                elseStatements
                           )
                       )
                   ).NormalizeWhitespace();
        }

        /// <summary>
        /// Creates conditional statement with given statements for true/false clauses and condition variable name.
        /// </summary>
        /// <param name="conditionVariableName"></param>
        /// <param name="statements"></param>
        /// <param name="elseStatements"></param>
        /// <returns></returns>
        public static IfStatementSyntax IfExpressionSyntax(string conditionVariableName, List<StatementSyntax> statements, List<StatementSyntax> elseStatements)
        {
            return IfExpressionSyntax(SyntaxFactory.IdentifierName(conditionVariableName), statements, elseStatements);
        }

        /// <summary>
        /// Creates declaration statement for expression of "var varName = (type)castedVariableName;"
        /// </summary>
        /// <param name="castedVariableName"></param>
        /// <param name="type"></param>
        /// <param name="resultVariableName"></param>
        /// <returns></returns>
        public static LocalDeclarationStatementSyntax CastExpression(string castedVariableName,string type, string resultVariableName)
        {
            // var
            var varDeclaration = SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var")
                );

            // resultVariableName = (ObjectBuilder.Type)castedVariableName;
            var castVariables = SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(resultVariableName)
                    ).WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.CastExpression(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.ParseToken(type)),
                                SyntaxFactory.IdentifierName(castedVariableName)
                            )
                        )
                    )
                );

            return SyntaxFactory.LocalDeclarationStatement(
                    varDeclaration
                    .WithVariables(
                        castVariables
                    )
                ).NormalizeWhitespace();
        }

        /// <summary>
        /// Creates public void methodName(paramType1 paramName1,..,paramTypeN paramNameN) {}.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="inputParameterNames"></param>
        /// <param name="inputParameterTypes"></param>
        /// <returns></returns>
        public static MethodDeclarationSyntax PublicMethodDeclaration(string methodName, SyntaxKind predefinedReturnType, List<string> inputParameterNames = null,
            List<string> inputParameterTypes = null, List<string> outputParameterNames = null, List<string> outputParameterTypes = null)
        {
            // Create method signature "public void EventName"
            var methodSyntax = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(predefinedReturnType)
                    ),
                    SyntaxFactory.Identifier(methodName)
                ).WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                    )
                );
            List<SyntaxNodeOrToken> parameters = null;
            if (inputParameterNames != null)
            {
                parameters = Parameters(inputParameterNames, inputParameterTypes);
                if(outputParameterNames != null)
                    parameters.AddRange(Parameters(outputParameterNames, outputParameterTypes, true));
            } else if (outputParameterNames != null)
            {
                parameters = Parameters(outputParameterNames, outputParameterTypes, true);
            }

            if(parameters != null)
            {
                var paramList = SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList<ParameterSyntax>(
                        parameters
                        )
                    );

                return methodSyntax.WithParameterList(paramList).WithBody(SyntaxFactory.Block()).NormalizeWhitespace();
            }

            return methodSyntax.WithBody(SyntaxFactory.Block()).NormalizeWhitespace();
        }

        /// <summary>
        /// Craetes parameter list syntax from given parameterNames.
        /// </summary>
        /// <param name="parameterNames"></param>
        /// <param name="types"></param>
        /// <param name="areOutputs">Adds out keyword to every single one.</param>
        /// <returns></returns>
        private static List<SyntaxNodeOrToken> Parameters(List<string> parameterNames, List<string> types, bool areOutputs = false)
        {
            // Create param list in form "type0 arg0, type1 arg1, ..."
            List<SyntaxNodeOrToken> storage = new List<SyntaxNodeOrToken>();

            for (int i = 0; i < parameterNames.Count; i++)
            {
                var typeIdentifier = types[i];
                var parameterName = parameterNames[i];
                var paramSyntax = ParameterSyntax(parameterName, typeIdentifier);

                if (areOutputs)
                    paramSyntax = paramSyntax.WithModifiers(SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.OutKeyword)
                        ));

                storage.Add(paramSyntax);
                if (i < parameterNames.Count - 1)
                    storage.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            return storage;
        }

        /// <summary>
        /// Creates parameter syntax out of given data.
        /// </summary>
        /// <param name="name">Unique variable name within script class.</param>
        /// <param name="typeSyntaxNode"></param>
        /// <returns></returns>
        public static ParameterSyntax ParameterSyntax(string name, string typeIdentifier)
        {
            return SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier(name)
                ).WithType(
                    SyntaxFactory.ParseTypeName(typeIdentifier)
                );
        }

        /// <summary>
        /// Creates syntax for output variable declaration.
        /// Can be used to create var type.
        /// </summary>
        /// <param name="typeData"></param>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public static LocalDeclarationStatementSyntax LocalVariable(string typeData, string variableName, ExpressionSyntax initializer = null)
        {
            var typeDeclaration = SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName(typeData.Length > 0 ? typeData : "var")
                );

            var variableDeclaration = SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(variableName)
                    );

            if(initializer != null)
                variableDeclaration = variableDeclaration.WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                            initializer
                        )
                    );

            return SyntaxFactory.LocalDeclarationStatement(
                    typeDeclaration.WithVariables(SyntaxFactory.SingletonSeparatedList(variableDeclaration))
                ).NormalizeWhitespace();
        }

        public static LocalDeclarationStatementSyntax LocalVariable(Type type, string varName, ExpressionSyntax initializerExpressionSyntax = null)
        {
            var typeSyntax = GenericTypeSyntax(type);

            return SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        typeSyntax, 
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(varName)
                            ).WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    initializerExpressionSyntax
                                )
                            )
                        )    
                    )
                );
        }

        /// <summary>
        ///  Creates syntax of type "Method(arg0,arg1,..);"
        /// </summary>
        /// <param name="className">Used for static method invocation.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="orderedVariableNames">Should be the same order as method signature.</param>
        /// <returns></returns>
        public static InvocationExpressionSyntax MethodInvocation(string methodName, IEnumerable<string> orderedVariableNames, string className = null)
        {
            // Arguments in format of arg0,arg1,...,argN,
            var arguments = new List<SyntaxNodeOrToken>();
            if(orderedVariableNames != null)
                foreach (var inputOutputVariableName in orderedVariableNames)
                {
                    arguments.Add(CreateArgumentSyntax(inputOutputVariableName));
                    arguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }

            // Removal of last ','
            if (arguments.Count > 0)
                arguments.RemoveAt(arguments.Count - 1);

            var argumentList = SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                        arguments
                    )
                );

            // Method(....)
            if(string.IsNullOrEmpty(className))
                return SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName(methodName)
                    ).WithArgumentList(argumentList);

            // ClassName.Method(...)
            return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(className),
                            SyntaxFactory.IdentifierName(methodName)
                        )
                    ).WithArgumentList(argumentList);
        }

        public static InvocationExpressionSyntax MethodInvocationExpressionSyntax(IdentifierNameSyntax methodName, 
                                                                                    ArgumentListSyntax arguments,
                                                                                    IdentifierNameSyntax instance = null)
        {
            InvocationExpressionSyntax invocation = null;
            if (instance == null)
            {
                invocation = SyntaxFactory.InvocationExpression(methodName);
            }
            else
            {
                invocation = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            instance,
                            methodName
                        )
                    );
            }

            return invocation.WithArgumentList(arguments);
        }

        /// <summary>
        /// Craetes simple argument.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ArgumentSyntax CreateArgumentSyntax(string value)
        {
            return SyntaxFactory.Argument(
                SyntaxFactory.ParseExpression(value)
                );
        }

        /// <summary>
        /// Creates assignment for given variable.
        /// variableName = rightSide;
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="rightSide"></param>
        /// <returns></returns>
        public static ExpressionStatementSyntax SimpleAssignment(string variableName, ExpressionSyntax rightSide)
        {
            var assignmentExpression = SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variableName),
                    rightSide
                );

            return SyntaxFactory.ExpressionStatement(assignmentExpression).NormalizeWhitespace();
        }

        /// <summary>
        /// Creates corresponding parameterless constructor to passed class.
        /// </summary>
        /// <param name="classDeclaration"></param>
        /// <returns></returns>
        public static ConstructorDeclarationSyntax Constructor(ClassDeclarationSyntax classDeclaration)
        {
            return SyntaxFactory.ConstructorDeclaration(
                            SyntaxFactory.Identifier(classDeclaration.Identifier.Text)
                        ).WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                            )
                        ).WithBody(
                            SyntaxFactory.Block()
                        ).NormalizeWhitespace();
        }
    }
}
