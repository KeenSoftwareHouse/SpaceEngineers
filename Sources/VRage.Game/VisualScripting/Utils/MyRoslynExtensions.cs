using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VRage.Game.VisualScripting.Utils
{
    public static class MyRoslynExtensions
    {
        /// <summary>
        /// Checks symbol history for ancestor of the type.
        /// </summary>
        /// <param name="derivedType"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDerivedTypeOf(this ITypeSymbol derivedType, ITypeSymbol type)
        {
            return IsDerivedTypeRecursive(derivedType, type);
        }

        private static bool IsDerivedTypeRecursive(ITypeSymbol derivedType, ITypeSymbol type)
        {
            if (derivedType == type) return true;
            if (derivedType.BaseType == null) return false;

            return IsDerivedTypeRecursive(derivedType.BaseType, type);
        }

        /// <summary>
        /// Checks for sequnce dependency by Attribute.
        /// </summary>
        /// <param name="methodSyntax"></param>
        /// <returns></returns>
        public static bool IsSequenceDependent(this MethodDeclarationSyntax methodSyntax)
        {
            if (methodSyntax.AttributeLists.Count > 0)
            { 
                foreach (var attributeSyntax in methodSyntax.AttributeLists.First().Attributes)
                {
                    if (attributeSyntax.Name.ToString() == "VisualScriptingMember")
                    {
                        if (attributeSyntax.ArgumentList == null)
                            return false;

                        return attributeSyntax.ArgumentList.Arguments.First().Expression.Kind() == SyntaxKind.TrueLiteralExpression;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks for Static modification in method syntax.
        /// </summary>
        /// <param name="methodSyntax"></param>
        /// <returns></returns>
        public static bool IsStatic(this MethodDeclarationSyntax methodSyntax)
        {
            return methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        /// <summary>
        /// Looks class that is the method container.
        /// </summary>
        /// <param name="methodSyntax"></param>
        /// <returns></returns>
        public static ClassDeclarationSyntax DeclaringClass(this MethodDeclarationSyntax methodSyntax)
        {
            if (methodSyntax.Parent is ClassDeclarationSyntax)
                return methodSyntax.Parent as ClassDeclarationSyntax;

            return null;
        }

        /// <summary>
        /// Creates string that can be further loaded from string.
        /// Full signature with parameters.
        /// </summary>
        /// <param name="syntax"></param>
        /// <returns></returns>
        public static string SerializeToObjectBuilder(this MethodDeclarationSyntax syntax)
        {
            var classDeclaration = DeclaringClass(syntax);
            var namespaceDeclaration = classDeclaration.Parent as NamespaceDeclarationSyntax;

            return namespaceDeclaration.Name + "." + classDeclaration.Identifier.Text + "." + syntax.Identifier.Text + syntax.ParameterList.ToFullString();
        }

        /// <summary>
        /// Creates string representation of type symbol that can be further
        /// reconstructed. Full namespace + name.
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <returns></returns>
        public static string SerializeToObjectBuilder(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.ContainingNamespace.Name + "." + typeSymbol.MetadataName;
        }

        /// <summary>
        /// Checks parameter for Out keyword.
        /// </summary>
        /// <param name="paramSyntax"></param>
        /// <returns></returns>
        public static bool IsOut(this ParameterSyntax paramSyntax)
        {
            return paramSyntax.Modifiers.Any(SyntaxKind.OutKeyword);
        }

        /// <summary>
        /// Comapres two types for equality by theire name.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="another"></param>
        /// <returns></returns>
        public static bool LiteComparator(this ITypeSymbol current, ITypeSymbol another)
        {
            if (current.Name == another.Name)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Is type of string?
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsString(this ITypeSymbol symbol)
        {
            if (symbol == null) return false;
            return symbol.MetadataName == "String";
        }

        /// <summary>
        /// Is type of integer?
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsInt(this ITypeSymbol symbol)
        {
            if (symbol == null) return false;
            return symbol.MetadataName == "Int32" || symbol.MetadataName == "Int64";
        }

        /// <summary>
        /// Is type of float?
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsFloat(this ITypeSymbol symbol)
        {
            if (symbol == null) return false;
            return symbol.MetadataName == "Single";
        }

        /// <summary>
        /// Is type of Bool?
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsBool(this ITypeSymbol symbol)
        {
            if (symbol == null) return false;
            return symbol.MetadataName == "Boolean";
        }
    }
}