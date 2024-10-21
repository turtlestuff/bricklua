using System.Buffers;

using BrickLua.CodeAnalysis.Syntax;

while (Console.ReadLine() is string text)
{
    var syntax = SyntaxTree.Parse(text);
    syntax.Root.WriteTo(Console.Out);

    foreach (var diag in syntax.Diagnostics)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{diag.Message} ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(syntax.Text.Slice(diag.Location.Start, diag.Location.End).ToString());
        Console.ResetColor();

        var location = diag.Location;
        var reader = new SequenceReader<char>(new ReadOnlySequence<char>(text.AsMemory()));

        var line = reader.Sequence;
        var startPos = reader.Position;
        while (reader.TryReadTo(sequence: out var sequence, '\n'))
        {
            if (reader.Consumed >= text.Length)
            {
                line = sequence;
                break;
            }

            startPos = reader.Position;
        }

        Console.WriteLine(line.ToString());

        var underlineLength = syntax.Text.Slice(location.Start, location.End).Length;
        var padLength = syntax.Text.Slice(startPos, location.Start).Length + underlineLength;
        Console.WriteLine($"{new string('~', (int)underlineLength).PadLeft((int)padLength)}");
    }
}