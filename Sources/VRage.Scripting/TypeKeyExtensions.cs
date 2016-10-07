using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VRage.Scripting
{
    /// <summary>
    ///     Roslyn does not provide a good way to compare a <see cref="Type" /> with an <see cref="ISymbol" />. These
    ///     extensions aim to provide "good enough" comparisons. In addition it adds a few other key types to be used
    ///     for the <see cref="MyScriptWhitelist" />.
    /// </summary>
    internal static class TypeKeyExtensions
    {
        public static string GetWhitelistKey(this ISymbol symbol, TypeKeyQuantity quantity)
        {
            var namespaceSymbol = symbol as INamespaceSymbol;
            if (namespaceSymbol != null)
            {
                return namespaceSymbol.GetWhitelistKey(quantity);
            }
            var typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol != null)
            {
                return typeSymbol.GetWhitelistKey(quantity);
            }
            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                // Account for generic methods, we must check their definitions, not specific implementations
                if (methodSymbol.IsGenericMethod && !methodSymbol.IsDefinition)
                {
                    methodSymbol = methodSymbol.OriginalDefinition;
                    return methodSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                            + ", "
                           + symbol.ContainingAssembly.Name;
                }
            }

            var memberSymbol = (symbol is IEventSymbol || symbol is IFieldSymbol || symbol is IPropertySymbol || symbol is IMethodSymbol) ? symbol : null;
            if (memberSymbol == null)
            {
                throw new ArgumentException("Invalid symbol type: Expected namespace, type or type member", "symbol");
            }

            return memberSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                    + ", "
                   + symbol.ContainingAssembly.Name;
        }

        public static string GetWhitelistKey(this ITypeSymbol symbol, TypeKeyQuantity quantity)
        {
            symbol = ResolveRootType(symbol);

            switch (quantity)
            {
                case TypeKeyQuantity.ThisOnly:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + ", "
                           + symbol.ContainingAssembly.Name;

                case TypeKeyQuantity.AllMembers:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + "+*, "
                           + symbol.ContainingAssembly.Name;

                default:
                    throw new ArgumentOutOfRangeException("quantity", quantity, null);
            }
        }

        static ITypeSymbol ResolveRootType(ITypeSymbol symbol)
        {
            var namedTypeSymbol = symbol as INamedTypeSymbol;
            if (namedTypeSymbol != null && namedTypeSymbol.IsGenericType && !namedTypeSymbol.IsDefinition)
            {
                symbol = namedTypeSymbol.OriginalDefinition;
                //if (symbol.SpecialType == SpecialType.System_Nullable_T)
                //    return namedTypeSymbol.TypeArguments[0];
                return symbol;
            }

            var pointerTypeSymbol = symbol as IPointerTypeSymbol;
            if (pointerTypeSymbol != null)
            {
                return pointerTypeSymbol.PointedAtType;
            }

            return symbol;
        }

        public static string GetWhitelistKey(this INamespaceSymbol symbol, TypeKeyQuantity quantity)
        {
            switch (quantity)
            {
                case TypeKeyQuantity.ThisOnly:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + ", "
                           + symbol.ContainingAssembly.Name;

                case TypeKeyQuantity.AllMembers:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + ".*, "
                           + symbol.ContainingAssembly.Name;

                default:
                    throw new ArgumentOutOfRangeException("quantity", quantity, null);
            }
        }
    }
}