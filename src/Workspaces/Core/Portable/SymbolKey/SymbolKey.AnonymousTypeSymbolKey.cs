﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class AnonymousTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.IsAnonymousType);

                var properties = symbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
                var propertyTypes = properties.SelectAsArray(p => p.Type);
                var propertyNames = properties.SelectAsArray(p => p.Name);
                var propertyIsReadOnly = properties.SelectAsArray(p => p.SetMethod == null);
                var propertyLocations = properties.SelectAsArray(p => FirstOrDefault(p.Locations));

                visitor.WriteSymbolKeyArray(propertyTypes);
                visitor.WriteStringArray(propertyNames);
                visitor.WriteBooleanArray(propertyIsReadOnly);
                visitor.WriteLocationArray(propertyLocations);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string failureReason)
            {
                using var propertyTypes = reader.ReadSymbolKeyArray<ITypeSymbol>(out var propertyTypesFailureReason);
                using var propertyNames = reader.ReadStringArray();
                using var propertyIsReadOnly = reader.ReadBooleanArray();
                using var propertyLocations = reader.ReadLocationArray(out var propertyLocationsFailureReason);

                if (propertyTypesFailureReason != null)
                {
                    failureReason = $"({nameof(AnonymousTypeSymbolKey)} {nameof(propertyTypes)} failed -> {propertyTypesFailureReason})";
                    return default;
                }

                if (propertyLocationsFailureReason != null)
                {
                    failureReason = $"({nameof(AnonymousTypeSymbolKey)} {nameof(propertyLocations)} failed -> {propertyLocationsFailureReason})";
                    return default;
                }

                if (!propertyTypes.IsDefault)
                {
                    try
                    {
                        var anonymousType = reader.Compilation.CreateAnonymousTypeSymbol(
                            propertyTypes.ToImmutable(), propertyNames.ToImmutable(),
                            propertyIsReadOnly.ToImmutable(), propertyLocations.ToImmutable());
                        failureReason = null;
                        return new SymbolKeyResolution(anonymousType);
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                failureReason = null;
                return new SymbolKeyResolution(reader.Compilation.ObjectType);
            }
        }
    }
}
