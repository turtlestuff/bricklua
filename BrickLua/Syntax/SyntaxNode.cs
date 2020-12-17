//  
//  Copyright (C) 2020 John Tur
//  
//  This file is part of BrickLua, a simple Lua 5.4 CIL compiler.
//  
//  BrickLua is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//  
//  BrickLua is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//  
//  You should have received a copy of the GNU Lesser General Public License
//  along with BrickLua.  If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BrickLua.Syntax
{
    public abstract class SyntaxNode
    {
        protected SyntaxNode(in SequenceRange location)
        {
            Location = location;
        }

        public SequenceRange Location { get; }

        public void WriteTo(TextWriter writer)
        {
            PrettyPrint(writer, this);
        }

        static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
        {
            static IEnumerable<SyntaxNode> GetChildren(SyntaxNode source)
            {
                var properties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))
                    {
                        var child = (SyntaxNode)property.GetValue(source)!;
                        if (child is { })
                            yield return child;
                    }
                    else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                    {
                        var children = (IEnumerable<SyntaxNode>)property.GetValue(source)!;
                        foreach (var child in children)
                            if (child is { })
                                yield return child;
                    }
                    else if (typeof(FunctionBody).IsAssignableFrom(property.PropertyType))
                    {
                        var body = (FunctionBody)property.GetValue(source)!;
                        foreach (var param in body.ParameterNames)
                            if (param is { })
                                yield return param;

                        yield return body.Body;
                    }
                    else if (typeof(FunctionName).IsAssignableFrom(property.PropertyType))
                    {
                        var name = (FunctionName)property.GetValue(source)!;
                        foreach (var dottedName in name.DottedNames)
                            if (dottedName is { })
                                yield return dottedName;

                        if (name.FieldName is { })
                            yield return name.FieldName;
                    }
                    else if (typeof(FunctionBody).IsAssignableFrom(property.PropertyType))
                    {
                        var body = (FunctionBody)property.GetValue(source)!;
                        foreach (var param in body.ParameterNames)
                        {
                            if (param is { })
                                yield return param;
                        }

                        yield return body.Body;
                    }
                    else if (typeof(IEnumerable<LocalVariableDeclaration>).IsAssignableFrom(property.PropertyType))
                    {
                        var decls = (IEnumerable<LocalVariableDeclaration>)property.GetValue(source)!;

                        foreach (var decl in decls)
                        {
                            yield return decl.Name;

                            if (decl.Attribute is { })
                                yield return decl.Attribute;
                        }
                    }

                }
            }

            var isToConsole = writer == Console.Out;
            var marker = isLast ? "└──" : "├──";

            if (isToConsole)
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write(indent);
            writer.Write(marker);

            if (isToConsole)
                Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;

            var name = node.GetType().GetProperties().FirstOrDefault(a => a.PropertyType == typeof(SyntaxKind)) is { } prop ?
                prop.GetValue(node)!.ToString()! :
                node.GetType().Name;

            if (name.EndsWith("ExpressionSyntax", StringComparison.InvariantCulture))
                name = name[..^"ExpressionSyntax".Length];
            else if (name.EndsWith("StatementSyntax", StringComparison.InvariantCulture))
                name = name[..^"StatementSyntax".Length];

            writer.Write(name);

            if (node is SyntaxToken { Value: var val })
            {
                writer.Write(" ");
                writer.Write(val);
            }

            if (isToConsole)
                Console.ResetColor();

            writer.WriteLine();

            indent += isLast ? "   " : "│  ";

            var children = GetChildren(node).ToArray();

            var lastChild = children.LastOrDefault();

            foreach (var child in children)
                PrettyPrint(writer, child, indent, child == lastChild);
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }
    }
}