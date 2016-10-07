using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Library.Collections;

namespace VRage.Scripting
{
    /// <summary>
    ///     Contains various utilities used by the scripting engine.
    /// </summary>
    internal static class AnalysisExtensions
    {
        public static ISymbol GetOverriddenSymbol(this ISymbol symbol)
        {
            if (!symbol.IsOverride)
                return null;
            var typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol != null)
                return typeSymbol.BaseType;
            var eventSymbol = symbol as IEventSymbol;
            if (eventSymbol != null)
                return eventSymbol.OverriddenEvent;
            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
                return propertySymbol.OverriddenProperty;
            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null)
                return methodSymbol.OverriddenMethod;
            return null;
        }

        public static bool IsMemberSymbol(this ISymbol symbol)
        {
            return (symbol is IEventSymbol || symbol is IFieldSymbol || symbol is IPropertySymbol || symbol is IMethodSymbol);
        }

        public static BaseMethodDeclarationSyntax WithBody(this BaseMethodDeclarationSyntax item, BlockSyntax body)
        {
            var cons = item as ConstructorDeclarationSyntax;
            if (cons != null)
            {
                return cons.WithBody(body);
            }
            var conv = item as ConversionOperatorDeclarationSyntax;
            if (conv != null)
            {
                return conv.WithBody(body);
            }
            var dest = item as DestructorDeclarationSyntax;
            if (dest != null)
            {
                return dest.WithBody(body);
            }
            var meth = item as MethodDeclarationSyntax;
            if (meth != null)
            {
                return meth.WithBody(body);
            }
            var oper = item as OperatorDeclarationSyntax;
            if (oper != null)
            {
                return oper.WithBody(body);
            }
            throw new ArgumentException("Unknown " + typeof(BaseMethodDeclarationSyntax).FullName, "item");
        }

        public static AnonymousFunctionExpressionSyntax WithBody(this AnonymousFunctionExpressionSyntax item, CSharpSyntaxNode body)
        {
            var anon = item as AnonymousMethodExpressionSyntax;
            if (anon != null)
            {
                return anon.WithBody(body);
            }
            var plam = item as ParenthesizedLambdaExpressionSyntax;
            if (plam != null)
            {
                return plam.WithBody(body);
            }
            var slam = item as SimpleLambdaExpressionSyntax;
            if (slam != null)
            {
                return slam.WithBody(body);
            }
            throw new ArgumentException("Unknown " + typeof(AnonymousFunctionExpressionSyntax).FullName, "item");
        }

        public static bool IsInSource(this ISymbol symbol)
        {
            for (var i = 0; i < symbol.Locations.Length; i++)
            {
                if (!symbol.Locations[i].IsInSource)
                {
                    return false;
                }
            }
            return true;
        }
    }
}