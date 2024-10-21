using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace BrickLua.CodeAnalysis.Syntax;

public ref struct Lexer
{
    private SequenceReader<char> reader;
    private SequencePosition tokenStart;

    public DiagnosticBag Diagnostics { get; }

    public Lexer(in SequenceReader<char> reader)
    {
        this.reader = reader;
        tokenStart = default;
        Diagnostics = new DiagnosticBag(reader.Sequence);

        if (NextIs('#'))
        {
            SkipComment();
        }
    }

    public SyntaxToken Lex()
    {
        if (!reader.TryPeek(out var ch))
        {
            return new SyntaxToken(SyntaxKind.EndOfFile, new SequenceRange(reader.Sequence.GetPosition(reader.Sequence.Length - 1), reader.Sequence.End));
        }

        tokenStart = reader.Position;

        switch (ch)
        {
            case '+':
                return LexSingleOperator(SyntaxKind.Plus);
            case '*':
                return LexSingleOperator(SyntaxKind.Asterisk);
            case '%':
                return LexSingleOperator(SyntaxKind.Asterisk);
            case '^':
                return LexSingleOperator(SyntaxKind.Caret);
            case '#':
                return LexSingleOperator(SyntaxKind.Hash);
            case '&':
                return LexSingleOperator(SyntaxKind.Ampersand);
            case '|':
                return LexSingleOperator(SyntaxKind.Pipe);
            case '(':
                return LexSingleOperator(SyntaxKind.OpenParenthesis);
            case ')':
                return LexSingleOperator(SyntaxKind.CloseParenthesis);
            case '{':
                return LexSingleOperator(SyntaxKind.OpenBrace);
            case '}':
                return LexSingleOperator(SyntaxKind.CloseBrace);
            case ']':
                return LexSingleOperator(SyntaxKind.CloseBracket);
            case ':':
                return LexDoubleOperator(':', SyntaxKind.Colon, SyntaxKind.ColonColon);
            case ';':
                return LexSingleOperator(SyntaxKind.Semicolon);
            case ',':
                return LexSingleOperator(SyntaxKind.Comma);
            case '/':
                return LexDoubleOperator('/', SyntaxKind.Slash, SyntaxKind.SlashSlash);
            case '=':
                return LexDoubleOperator('=', SyntaxKind.Equals, SyntaxKind.EqualsEquals);
            case '~':
                return LexDoubleOperator('=', SyntaxKind.Tilde, SyntaxKind.TildeEquals);
            case '<':
                return LexDoubleChoiceOperator('=', '<', SyntaxKind.Less, SyntaxKind.LessEquals, SyntaxKind.LessLess);
            case '>':
                return LexDoubleChoiceOperator('=', '>', SyntaxKind.Greater, SyntaxKind.GreaterEquals, SyntaxKind.GreaterGreater);

            case '\'':
            case '"':
                reader.Advance(1);
                var strStart = reader.Consumed;

                if (!reader.TryReadTo(sequence: out var seq, ch, '\\'))
                {
                    reader.AdvanceToEnd();
                    seq = reader.UnreadSequence;
                    Diagnostics.ReportUnterminatedString(new SequenceRange(tokenStart, reader.Sequence.End));
                }

                return new SyntaxToken(SyntaxKind.LiteralString, UnescapeStringLiteral(seq, false, strStart), Current);

            case '[':
                reader.Advance(1);

                var level = reader.AdvancePast('=');

                if (!NextIs('[') && level == 0)
                {
                    return NewToken(SyntaxKind.OpenBracket);
                }

                return LexLongLiteral(level);

            case '-':
                if (reader.TryPeek(out var next) && next is >= '0' and <= '9')
                    return LexNumeral();

                reader.Advance(1);

                if (NextIs('-'))
                {
                    SkipComment();
                    return Lex();
                }

                return NewToken(SyntaxKind.Minus);

            case '.':
                reader.Advance(1);

                if (NextIs('.'))
                {
                    if (NextIs('.'))
                    {
                        return NewToken(SyntaxKind.DotDotDot);
                    }

                    return NewToken(SyntaxKind.DotDot);
                }

                return NewToken(SyntaxKind.Dot);

            case >= '0' and <= '9':
                return LexNumeral();

            case '_':
            case >= 'A' and <= 'Z':
            case >= 'a' and <= 'z':
                return LexIdentifier();


            case ' ' or '\n' or '\r' or '\f' or '\t' or '\v':
                reader.Advance(1);
                return Lex();

            default:
                reader.TryRead(out var bad);
                Diagnostics.ReportBadCharacter(Current, bad);
                return NewToken(SyntaxKind.BadToken);
        }
    }

    void SkipComment()
    {
        if (NextIs('['))
        {
            var level = reader.AdvancePast('=');

            if (NextIs('['))
            {
                Span<char> delim = new char[level + 2];

                delim.Fill('=');
                delim[0] = ']';
                delim[^1] = ']';

                if (!reader.TryReadTo(sequence: out _, delim))
                {
                    reader.AdvanceToEnd();
                    Diagnostics.ReportUnterminatedLongComment(new SequenceRange(tokenStart, reader.Sequence.End));
                }
            }
        }

        if (!reader.TryAdvanceToAny("\r\n"))
            reader.AdvanceToEnd();
    }

    SyntaxToken LexLongLiteral(long level)
    {
        // TODO: This should be replaced with stackalloc once the language support makes it in.
        Span<char> endLongLiteral = new char[level + 2];

        endLongLiteral.Fill('=');
        endLongLiteral[0] = ']';
        endLongLiteral[^1] = ']';

        var startString = reader.Position;
        var startIndex = reader.Consumed;

        if (!reader.TryReadTo(sequence: out var literal, endLongLiteral))
        {
            reader.AdvanceToEnd();
            Diagnostics.ReportUnterminatedLongString(new SequenceRange(tokenStart, reader.Sequence.End));

            return new SyntaxToken(SyntaxKind.LiteralString,
                UnescapeStringLiteral(reader.Sequence.Slice(startString), true, startIndex),
                new SequenceRange(tokenStart, reader.Sequence.End));
        }

        return new SyntaxToken(SyntaxKind.LiteralString, UnescapeStringLiteral(literal, true, startIndex), Current);
    }

    SyntaxToken LexIdentifier()
    {
        reader.Advance(1);
        reader.AdvancePastAny("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_");
        var str = reader.Sequence.Slice(tokenStart, reader.Position);

        SyntaxKind type = SyntaxKind.Name;
        if (str.Length <= 8)
        {
            // Since this is tiny, it's probably OK to allocate (not likely to be broken across segments).
            var span = str.IsSingleSegment ? str.FirstSpan : str.ToArray();
            type = SyntaxFacts.GetIdentifierKind(span);
        }

        return new SyntaxToken(type, str.ToString(), Current);
    }

    // Hex float literals are blocked on https://github.com/dotnet/runtime/issues/1630
    SyntaxToken LexNumeral()
    {
        _ = NextIs('-');
        
        bool isHex = false;
        if (NextIs('0') && NextIs('X', 'x'))
        {
            reader.AdvancePastAny("1234567890ABCDEFabcdef");
            isHex = true;
        }
        else
        {
            reader.AdvancePastAny("1234567890");
            if (NextIs('.'))
            {
                reader.AdvancePastAny("1234567890");
            }

            if (NextIs('e', 'E'))
            {
                _ = NextIs('-');
                reader.AdvancePastAny("1234567890");
            }
        }

        var str = reader.Sequence.Slice(tokenStart, reader.Position);
        var span = str.IsSingleSegment ? str.FirstSpan : str.ToArray();
        var format = isHex ? NumberStyles.HexNumber : NumberStyles.Number;

        if (long.TryParse(span, format, CultureInfo.InvariantCulture, out var longResult))
        {
            return new SyntaxToken(longResult, Current);
        }
        else if (double.TryParse(span, format, CultureInfo.InvariantCulture, out var doubleResult))
        {
            return new SyntaxToken(doubleResult, Current);
        }
        else
        {
            throw new NotImplementedException("Unimplemented numeric literal.");
        }
    }

    SyntaxToken LexSingleOperator(SyntaxKind type)
    {
        reader.Advance(1);
        return NewToken(type);
    }

    SyntaxToken LexDoubleOperator(char next, SyntaxKind one, SyntaxKind two)
    {
        reader.Advance(1);
        SyntaxKind type = one;

        if (NextIs(next))
        {
            type = two;
        }

        return NewToken(type);
    }

    SyntaxToken LexDoubleChoiceOperator(char char1, char char2, SyntaxKind none, SyntaxKind type1, SyntaxKind type2)
    {
        reader.Advance(1);
        SyntaxKind type = none;

        if (NextIs(char1))
        {
            type = type1;
        }
        else if (NextIs(char2))
        {
            type = type2;
        }

        return NewToken(type);
    }


    static void UnescapeLongLiteral(ref SequenceReader<char> reader, ArrayBufferWriter<char> buffer)
    {
        // 3.1 Lexical Conventions
        // When the opening long bracket is immediately followed by a newline, the newline is not included in the string.
        for (int i = 0; i < 2; i++)
        {
            if (reader.TryPeek(out var start) && start is '\r' or '\n')
            {
                reader.Advance(1);
            }
            else
            {
                break;
            }
        }

        while (reader.TryReadToAny(sequence: out var sequence, "\r\n"))
        {
            foreach (var memory in sequence)
            {
                buffer.Write(memory.Span);
            }

            // 3.1 Lexical Conventions
            // Any kind of end-of-line sequence (carriage return, newline, carriage return followed by newline,
            // or newline followed by carriage return) is converted to a simple newline.
            char c = '\n';
            buffer.Write(MemoryMarshal.CreateSpan(ref c, 1));

            // TODO: This accepts sequences like \r\r and \n\n. These aren't accepted by the reference implementation.
            // This likely should be changed for better conformance.
            if (reader.TryPeek(out var next) && next is '\r' or '\n')
            {
                reader.Advance(1);
            }
        }

        foreach (var memory in reader.UnreadSequence)
        {
            buffer.Write(memory.Span);
        }
    }

    ReadOnlyMemory<char> UnescapeStringLiteral(in ReadOnlySequence<char> literal, bool multiLine, long startIndex)
    {
        var buffer = new ArrayBufferWriter<char>(checked((int) literal.Length));

        var reader = new SequenceReader<char>(literal);

        if (multiLine)
        {
            UnescapeLongLiteral(ref reader, buffer);
            return buffer.WrittenMemory;
        }

        while (reader.TryReadTo(sequence: out var sequence, '\\'))
        {
            foreach (var memory in sequence)
            {
                buffer.Write(memory.Span);
            }

            if (!reader.TryRead(out var escapeSequence))
            {
                break;
            }

            char escaped = escapeSequence switch
            {
                '"' => '"',
                'a' => '\a',
                'b' => '\b',
                'f' => '\f',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'v' => '\v',
                '\\' => '\\',
                '\'' => '\'',
                _ => default
            };

            // Some escape sequences simply map to one character. If this escape sequence does,
            // escaped will be that character. Otherwise, it will be default, indicating that
            // some more parsing is required.
            if (escaped != default)
            {
                buffer.Write(MemoryMarshal.CreateSpan(ref escaped, 1));
            }
            else
            {
                switch (escapeSequence)
                {
                    case 'x':
                        ReadHexEscapeSequence(ref reader, buffer, startIndex);
                        break;

                    case 'u':
                        ReadCodePointEscapeSequence(ref reader, buffer, startIndex);
                        break;

                    case >= '0' and <= '9':
                        ReadDecimalEscapeSequence(ref reader, buffer, startIndex, escapeSequence);
                        break;

                    // TODO: Implement `\z` and `\` followed by space

                    default:
                        var start = this.reader.Sequence.GetPosition(startIndex + reader.Consumed);
                        var end = this.reader.Sequence.GetPosition(startIndex + reader.Consumed + 1);

                        Diagnostics.ReportInvalidEscapeSequence(new SequenceRange(start, end), escapeSequence);
                        break;
                }
            }

        }

        foreach (var memory in reader.UnreadSequence)
        {
            buffer.Write(memory.Span);
        }

        return buffer.WrittenMemory;
    }

    // 3.1 Lexical Conventions
    // We can specify any byte in a short literal string, including embedded zeros, by its numeric value.
    // This can be done with the escape sequence \xXX, where XX is a sequence of exactly two hexadecimal digits,
    // or with the escape sequence \ddd, where ddd is a sequence of up to three decimal digits.

    readonly void ReadHexEscapeSequence(ref SequenceReader<char> reader, ArrayBufferWriter<char> buffer, long startIndex)
    {
        if (!reader.TryRead(out var ch1) || !reader.TryRead(out var ch2))
        {
            Diagnostics.ReportIncompleteEscapeSequence(
                new SequenceRange(
                    this.reader.Sequence.GetPosition(startIndex),
                    this.reader.Sequence.GetPosition(startIndex + reader.Consumed)));
            return;
        }

        var num = (char)byte.Parse([ch1, ch2], NumberStyles.AllowHexSpecifier);
        buffer.Write(MemoryMarshal.CreateSpan(ref num, 1));
    }

    void ReadDecimalEscapeSequence(ref SequenceReader<char> reader, ArrayBufferWriter<char> buffer, long startIndex, char d1)
    {
        Span<char> escape = stackalloc char[3];
        escape[0] = d1;

        if (reader.TryRead(out var d2) && d2 is >= '0' and <= '9')
        {
            escape[1] = d2;

            if (reader.TryRead(out var d3) && d3 is >= '0' and <= '9')
            {
                escape[2] = d3;
            }
            else
            {
                escape = escape[..2];
            }
        }
        else
        {
            escape = escape[..1];
        }

        var shortNum = (char) short.Parse(escape);
        buffer.Write(MemoryMarshal.CreateSpan(ref shortNum, 1));
    }


    void ReadCodePointEscapeSequence(ref SequenceReader<char> reader, ArrayBufferWriter<char> buffer, long startIndex)
    {
        // 3.1 Lexical Conventions  
        // The UTF-8 encoding of a Unicode character can be inserted in a literal string
        // with the escape sequence \u{XXX} (with mandatory enclosing braces), where XXX is a sequence of
        // one or more hexadecimal digits representing the character code point. 

        if (!reader.TryRead(out var openBrace) || openBrace != '{')
        {
            Diagnostics.ReportExpectedCharacter(
                new SequenceRange(
                    this.reader.Sequence.GetPosition(startIndex + reader.Consumed),
                    this.reader.Sequence.GetPosition(startIndex + reader.Consumed + 1)),
                openBrace,
                '{');

            return;
        }

        if (!reader.TryReadTo(span: out var span, '}'))
        {
            Diagnostics.ReportUnterminatedEscapeSequence(
                new SequenceRange(
                    this.reader.Sequence.GetPosition(startIndex),
                    this.reader.Sequence.GetPosition(startIndex + reader.Consumed)));

            return;
        }

        var codePoint = int.Parse(span, NumberStyles.AllowHexSpecifier);

        Span<char> chars = stackalloc char[2];

        // TODO: 3.1 Lexical Conventions says that the value is not restricted to valid Unicode code points,
        // and can be any integer less than 2^31. Rune will throw for any value which is not a valid
        // Unicode scalar. Investigate how better conformance can be achieved here.
        new Rune(codePoint).EncodeToUtf16(chars);
        buffer.Write(chars);
    }

    SequenceRange Current => new(tokenStart, reader.Position);

    bool NextIs(char c)
    {
        if (reader.TryPeek(out var next) && next == c)
        {
            reader.Advance(1);
            return true;
        }

        return false;
    }

    bool NextIs(char a, char b)
    {
        if (reader.TryPeek(out var next) && (next == a || next == b))
        {
            reader.Advance(1);
            return true;
        }

        return false;
    }


    SyntaxToken NewToken(SyntaxKind type) => new(type, Current);
}
