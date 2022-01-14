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

    // TODO: Replace with https://github.com/dotnet/csharplang/issues/1881

    public static SyntaxKind GetIdentifierKind(ReadOnlySpan<char> input) => input.Length switch
    {
        2 => input[0] switch
        {
            'd' => input[1] switch
            {
                'o' => SyntaxKind.Do,
                _ => SyntaxKind.Name
            },
            'i' => input[1] switch
            {
                'f' => SyntaxKind.If,
                'n' => SyntaxKind.In,
                _ => SyntaxKind.Name
            },
            'o' => input[1] switch
            {
                'r' => SyntaxKind.Or,
                _ => SyntaxKind.Name
            },
            _ => SyntaxKind.Name,
        },
        3 => input[0] switch
        {
            'a' => input[1] switch
            {
                'n' => input[2] switch
                {
                    'd' => SyntaxKind.And,
                    _ => SyntaxKind.Name,
                },
                _ => SyntaxKind.Name,
            },
            'e' => input[1] switch
            {
                'n' => input[2] switch
                {
                    'd' => SyntaxKind.End,
                    _ => SyntaxKind.Name,
                },
                _ => SyntaxKind.Name,
            },
            'f' => input[1] switch
            {
                'o' => input[2] switch
                {
                    'r' => SyntaxKind.For,
                    _ => SyntaxKind.Name,
                },
                _ => SyntaxKind.Name,
            },
            'n' => input[1] switch
            {
                'i' => input[2] switch
                {
                    'l' => SyntaxKind.Nil,
                    _ => SyntaxKind.Name,
                },
                'o' => input[2] switch
                {
                    't' => SyntaxKind.Not,
                    _ => SyntaxKind.Name,
                },
                _ => SyntaxKind.Name,
            },
            _ => SyntaxKind.Name,
        },
        4 => input[0] switch
        {
            'e' => input[1] switch
            {
                'l' => input[2] switch
                {
                    's' => input[3] switch
                    {
                        'e' => SyntaxKind.Else,
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            'g' => input[1] switch
            {
                'o' => input[2] switch
                {
                    't' => input[3] switch
                    {
                        'o' => SyntaxKind.Goto,
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            't' => input[1] switch
            {
                'h' => input[2] switch
                {
                    'e' => input[3] switch
                    {
                        'n' => SyntaxKind.Then,
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                'r' => input[2] switch
                {
                    'u' => input[3] switch
                    {
                        'e' => SyntaxKind.True,
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            _ => SyntaxKind.Name
        },
        5 => input[0] switch
        {
            'b' => input[1] switch
            {
                'r' => input[2] switch
                {
                    'e' => input[3] switch
                    {
                        'a' => input[4] switch
                        {
                            'k' => SyntaxKind.Break,
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            'f' => input[1] switch
            {
                'a' => input[2] switch
                {
                    'l' => input[3] switch
                    {
                        's' => input[4] switch
                        {
                            'e' => SyntaxKind.False,
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            'l' => input[1] switch
            {
                'o' => input[2] switch
                {
                    'c' => input[3] switch
                    {
                        'a' => input[4] switch
                        {
                            'l' => SyntaxKind.Local,
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            'u' => input[1] switch
            {
                'n' => input[2] switch
                {
                    't' => input[3] switch
                    {
                        'i' => input[4] switch
                        {
                            'l' => SyntaxKind.Until,
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            'w' => input[1] switch
            {
                'h' => input[2] switch
                {
                    'i' => input[3] switch
                    {
                        'l' => input[4] switch
                        {
                            'e' => SyntaxKind.While,
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            _ => SyntaxKind.Name
        },
        6 => input[0] switch
        {
            'e' => input[1] switch
            {
                'l' => input[2] switch
                {
                    's' => input[3] switch
                    {
                        'e' => input[4] switch
                        {
                            'i' => input[5] switch
                            {
                                'f' => SyntaxKind.ElseIf,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            'r' => input[1] switch
            {
                'e' => input[2] switch
                {
                    'p' => input[3] switch
                    {
                        'e' => input[4] switch
                        {
                            'a' => input[5] switch
                            {
                                't' => SyntaxKind.Repeat,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    't' => input[3] switch
                    {
                        'u' => input[4] switch
                        {
                            'r' => input[5] switch
                            {
                                'n' => SyntaxKind.Return,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            _ => SyntaxKind.Name,
        },
        8 => input[0] switch
        {
            'f' => input[1] switch
            {
                'u' => input[2] switch
                {
                    'n' => input[3] switch
                    {
                        'c' => input[4] switch
                        {
                            't' => input[5] switch
                            {
                                'i' => input[6] switch
                                {
                                    'o' => input[7] switch
                                    {
                                        'n' => SyntaxKind.Function,
                                        _ => SyntaxKind.Name,
                                    },
                                    _ => SyntaxKind.Name,
                                },
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name,
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            _ => SyntaxKind.Name,
        },
        _ => SyntaxKind.Name
    };
}
