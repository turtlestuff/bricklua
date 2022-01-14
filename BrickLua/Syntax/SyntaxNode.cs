using System.Reflection;

namespace BrickLua.CodeAnalysis.Syntax;

public abstract record SyntaxNode(in SequenceRange Location)
{
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
                    var child = (SyntaxNode) property.GetValue(source)!;
                    if (child is { })
                        yield return child;
                }
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                {
                    var children = (IEnumerable<SyntaxNode>) property.GetValue(source)!;
                    foreach (var child in children)
                        if (child is { })
                            yield return child;
                }
                else if (typeof(FunctionBody).IsAssignableFrom(property.PropertyType))
                {
                    var body = (FunctionBody) property.GetValue(source)!;
                    foreach (var param in body.ParameterNames)
                        if (param is { })
                            yield return param;

                    yield return body.Body;
                }
                else if (typeof(FunctionName).IsAssignableFrom(property.PropertyType))
                {
                    var name = (FunctionName) property.GetValue(source)!;
                    foreach (var dottedName in name.DottedNames)
                        if (dottedName is { })
                            yield return dottedName;

                    if (name.FieldName is { })
                        yield return name.FieldName;
                }
                else if (typeof(FunctionBody).IsAssignableFrom(property.PropertyType))
                {
                    var body = (FunctionBody) property.GetValue(source)!;
                    foreach (var param in body.ParameterNames)
                    {
                        if (param is { })
                            yield return param;
                    }

                    yield return body.Body;
                }
                else if (typeof(IEnumerable<LocalVariableDeclaration>).IsAssignableFrom(property.PropertyType))
                {
                    var decls = (IEnumerable<LocalVariableDeclaration>) property.GetValue(source)!;

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
