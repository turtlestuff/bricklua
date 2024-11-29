using System.Globalization;

namespace BrickLua.CodeAnalysis.Syntax;

public static class SyntaxFacts
{
    public static bool IsRightAssociative(SyntaxKind kind) => kind == SyntaxKind.Caret || kind == SyntaxKind.DotDot;

    public static int UnaryOperatorPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.Not or SyntaxKind.Hash or SyntaxKind.Minus or SyntaxKind.Tilde => 11,
            _ => 0,
        };
    }

    public static int BinaryOperatorPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.Caret => 12,
            SyntaxKind.Asterisk or SyntaxKind.Slash or SyntaxKind.SlashSlash or SyntaxKind.Percent => 10,
            SyntaxKind.Plus or SyntaxKind.Minus => 9,
            SyntaxKind.DotDot => 8,
            SyntaxKind.GreaterGreater or SyntaxKind.LessLess => 7,
            SyntaxKind.Ampersand => 6,
            SyntaxKind.Tilde => 5,
            SyntaxKind.Pipe => 4,
            SyntaxKind.Less or SyntaxKind.Greater or SyntaxKind.LessEquals or SyntaxKind.GreaterEquals or SyntaxKind.TildeEquals or SyntaxKind.EqualsEquals => 3,
            SyntaxKind.And => 2,
            SyntaxKind.Or => 1,
            _ => 0,
        };
    }

    public static string GetEofString(this char c) => c == default ? "<eof>" : c.ToString(CultureInfo.InvariantCulture);

    public static SyntaxKind GetIdentifierKind(ReadOnlySpan<char> input)
    {
        return input switch
        {
            "do" => SyntaxKind.Do,
            "if" => SyntaxKind.If,
            "in" => SyntaxKind.In,
            "or" => SyntaxKind.Or,
            "and" => SyntaxKind.And,
            "end" => SyntaxKind.End,
            "for" => SyntaxKind.For,
            "nil" => SyntaxKind.Nil,
            "not" => SyntaxKind.Not,
            "else" => SyntaxKind.Else,
            "goto" => SyntaxKind.Goto,
            "then" => SyntaxKind.Then,
            "true" => SyntaxKind.True,
            "break" => SyntaxKind.Break,
            "false" => SyntaxKind.False,
            "local" => SyntaxKind.Local,
            "until" => SyntaxKind.Until,
            "while" => SyntaxKind.While,
            "elseif" => SyntaxKind.ElseIf,
            "repeat" => SyntaxKind.Repeat,
            "return" => SyntaxKind.Return,
            "function" => SyntaxKind.Function,
            _ => SyntaxKind.Name
        };
    }
}
