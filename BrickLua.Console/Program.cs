using System.Buffers;

using BrickLua.CodeAnalysis.Syntax;

while (true)
{
    var seq = new ReadOnlySequence<char>(Console.ReadLine().AsMemory());
    var parser = new Parser(new Lexer(new SequenceReader<char>(seq)));
    var chunk = parser.ParseChunk();
    chunk.WriteTo(Console.Out);

    foreach (var diag in parser.Diagnostics)
    {

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{diag.Message} ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(parser.Diagnostics.Text.Slice(diag.Location.Start, diag.Location.End).ToString());
        Console.ResetColor();

        var text = parser.Diagnostics.Text;
        var location = diag.Location;
        var index = text.Slice(0, diag.Location.Start).Length;
        var reader = new SequenceReader<char>(parser.Diagnostics.Text);

        var line = reader.Sequence;
        var startPos = text.Start;
        while (reader.TryReadTo(sequence: out var sequence, '\n'))
        {
            if (reader.Consumed - 1 > index)
            {
                line = sequence;
                break;
            }

            startPos = reader.Position;
        }

        Console.WriteLine(line.ToString());

        var underlineLength = text.Slice(location.Start, location.End).Length;
        var padLength = text.Slice(startPos, location.Start).Length + underlineLength;
        Console.WriteLine($"{new string('~', (int) underlineLength).PadLeft((int) padLength)}");
    }
}