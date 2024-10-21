using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace BrickLua.CodeAnalysis.Syntax;

public ref struct Parser
{
    private SyntaxToken current;
    private SyntaxToken? peek;

    private Lexer lexer;

    public DiagnosticBag Diagnostics { get; }

    public Parser(in Lexer lexer)
    {
        this.lexer = lexer;
        current = this.lexer.Lex();
        peek = null;
        Diagnostics = lexer.Diagnostics;
    }

    SyntaxToken NextToken()
    {
        var current = this.current;
        this.current = peek ?? lexer.Lex();

        if (peek is not null)
            peek = null;

        return current;
    }

    SyntaxToken Peek()
    {
        peek ??= lexer.Lex();
        return peek;
    }

    SyntaxToken MatchToken(SyntaxKind kind)
    {
        if (current.Kind == kind)
            return NextToken();

        Diagnostics.ReportUnexpectedToken(current.Location, kind, current.Kind);
        return new SyntaxToken(kind, current.Location, true);
    }

    bool CurrentIs(SyntaxKind kind, [NotNullWhen(true)] out SyntaxToken? token)
    {
        if (current.Kind == kind)
        {
            token = NextToken();
            return true;
        }

        token = null;
        return false;
    }

    readonly bool IsNot(SyntaxKind kind) => current.Kind != kind && current.Kind != SyntaxKind.EndOfFile;

    static bool StartsExpression(SyntaxToken token)
    {
        switch (token.Kind)
        {
            case SyntaxKind.Name:
            case SyntaxKind.IntegerConstant:
            case SyntaxKind.FloatConstant:
            case SyntaxKind.LiteralString:
            case SyntaxKind.Nil:
            case SyntaxKind.True:
            case SyntaxKind.False:
            case SyntaxKind.Minus:
            case SyntaxKind.Not:
            case SyntaxKind.Hash:
            case SyntaxKind.Tilde:
            case SyntaxKind.Function:
            case SyntaxKind.OpenParenthesis:
            case SyntaxKind.OpenBrace:
            case SyntaxKind.DotDotDot:
                return true;
            default:
                return false;
        }
    }

    bool BlockEnded()
    {
        switch (current.Kind)
        {
            case SyntaxKind.Semicolon:
                // ParseStatement doesn't handle semicolon statements, so we skip over them here.
                NextToken();
                return BlockEnded();
            case SyntaxKind.Return:
            case SyntaxKind.EndOfFile:
            case SyntaxKind.End:
            case SyntaxKind.Until:
            case SyntaxKind.ElseIf:
            case SyntaxKind.Else:
                return true;
            default:
                return false;
        }
    }

    static SequenceRange From(SyntaxNode first, SyntaxNode last) => new(first.Location.Start, last.Location.End);

    static SyntaxNode GetLast<TNode>(ImmutableArray<TNode> node, SyntaxNode last) where TNode : SyntaxNode
        => node.IsDefaultOrEmpty ? last : node[^1];

    public ChunkSyntax ParseChunk()
    {
        var block = ParseBlock();
        return new ChunkSyntax(block, block.Location);
    }

    /// <summary>
    /// Parses a lua statement.
    /// <code>
    /// stat ::= ';' | 
    ///          varlist '=' explist | 
    ///          functioncall | 
    ///          label | 
    ///          break | 
    ///          goto Name | 
    ///          do block end | 
    ///          while exp do block end | 
    ///          repeat block until exp | 
    ///          if exp then block {elseif exp then block} [else block] end | 
    ///          for Name '=' exp ',' exp [',' exp] do block end | 
    ///          for namelist in explist do block end | 
    ///          function funcname funcbody | 
    ///          local function Name funcbody | 
    ///          local attnamelist ['=' explist] 
    /// </code>
    /// </summary>
    StatementSyntax ParseStatement() => current.Kind switch
    {
        SyntaxKind.Break => ParseBreakStatement(),
        SyntaxKind.Goto => ParseGotoStatement(),
        SyntaxKind.Do => ParseDoStatement(),
        SyntaxKind.While => ParseWhileStatement(),
        SyntaxKind.Repeat => ParseRepeatStatement(),
        SyntaxKind.If => ParseIfStatement(),
        SyntaxKind.For => ParseForStatement(),
        SyntaxKind.Function => ParseFunctionStatement(),
        SyntaxKind.Local => ParseLocalStatement(),
        SyntaxKind.ColonColon => ParseLabelStatement(),
        _ => ParseAssignmentOrCallStatement(),
    };

    /// <summary>
    /// Parses an assignment or call statement.
    /// </summary>
    StatementSyntax ParseAssignmentOrCallStatement()
    {
        var parserState = this;

        var prefixExpression = ParsePrefixExpression();
        switch (prefixExpression)
        {
            case CallExpressionSyntax call:
                return new ExpressionStatementSyntax(call);
            case VariableExpressionSyntax var:
                return ParseAssignment(var);
            default:
                // The thing we're about to parse isn't an assignment or call statement.
                // We need to return some kind of statement syntax, though.
                // Back up to the state from before we parsed anything, then treat the following code like a function call statement.
                // The errors will suggest adding invocation.
                this = parserState;
                return new ExpressionStatementSyntax(ParseFunctionCallExpression());
        };
    }


    /// <summary>
    /// Parses an assignment statement, of the form <c>varlist '=' explist</c>.
    /// </summary>
    /// <param name="first">
    /// The first item in <c>varlist</c>. This is neccessary as it was alredy parsed in <see cref="ParseAssignmentOrCallStatement"/>.
    /// </param>
    AssignmentStatementSyntax ParseAssignment(VariableExpressionSyntax first)
    {
        var vars = ImmutableArray.CreateBuilder<VariableExpressionSyntax>();
        vars.Add(first);

        while (CurrentIs(SyntaxKind.Comma, out _))
        {
            var expr = ParseVariableExpression();
            vars.Add(expr);
        }

        MatchToken(SyntaxKind.Equals);
        var exprs = ParseExpressionList();
        return new AssignmentStatementSyntax(Location: From(vars[0], exprs[^1]),
                                            Variables: vars.DrainToImmutable(), 
                                            Values: exprs);
    }

    /// <summary>
    /// Parses a function statement of the form <c>function funcname funcbody</c> where
    /// <code>
    /// funcname ::= Name {'.' Name} [':' Name]
    /// funcbody ::= '(' [parlist] ')' block end
    /// </code>
    /// </summary>
    FunctionStatementSyntax ParseFunctionStatement()
    {
        var function = MatchToken(SyntaxKind.Function);
        var name = ParseFunctionName();
        var body = ParseFunctionBody();
        return new FunctionStatementSyntax(name, body, From(function, body.Body));
    }

    /// <summary>
    /// Parses a function name of the form <c>funcname ::= Name {'.' Name} [':' Name]</c>
    /// </summary>
    /// <returns></returns>
    FunctionName ParseFunctionName()
    {
        var builder = ImmutableArray.CreateBuilder<SyntaxToken>();
        do
        {
            builder.Add(MatchToken(SyntaxKind.Name));
        } while (CurrentIs(SyntaxKind.Dot, out _));

        var fieldName = CurrentIs(SyntaxKind.Colon, out _) ? MatchToken(SyntaxKind.Name) : null;
        return new FunctionName(builder.DrainToImmutable(), fieldName);
    }

    /// <summary>
    /// Parses a statement beginning with local, which can be
    /// <code>
    /// local function Name funcbody |
    /// local attnamelist ['=' explist]
    /// </code> where
    /// <code>
    /// attnamelist ::= Name attrib {',' Name attrib}
    /// </code>
    /// </summary>
    StatementSyntax ParseLocalStatement()
    {
        var local = MatchToken(SyntaxKind.Local);
        if (CurrentIs(SyntaxKind.Function, out _))
        {
            var name = MatchToken(SyntaxKind.Name);
            var body = ParseFunctionBody();

            return new LocalFunctionStatementSyntax(name, body, From(local, body.Body));
        }
        else
        {
            var declarations = ParseNameAttributeList();
            var expressions = CurrentIs(SyntaxKind.Equals, out var eq) ? ParseExpressionList() : [];

            return new LocalDeclarationStatementSyntax(declarations, expressions, From(local, expressions.IsDefaultOrEmpty ? declarations[^1].Name : GetLast(expressions, eq!)));
        }
    }

    /// <summary>
    /// Parses a list of names and attributes of the form <c>attnamelist ::= Name attrib {',' Name attrib}</c> where <c>attrib ::= ['<' Name '>']</c> for <see cref="ParseLocalStatement"/>.
    /// </summary>
    ImmutableArray<LocalVariableDeclaration> ParseNameAttributeList()
    {
        var builder = ImmutableArray.CreateBuilder<LocalVariableDeclaration>();
        do
        {
            var name = MatchToken(SyntaxKind.Name);
            SyntaxToken? attrib = null;
            if (CurrentIs(SyntaxKind.Less, out _))
            {
                attrib = MatchToken(SyntaxKind.Name);
                MatchToken(SyntaxKind.Greater);
            }

            builder.Add(new LocalVariableDeclaration(name, attrib));

        } while (CurrentIs(SyntaxKind.Comma, out _));

        return builder.DrainToImmutable();
    }

    /// <summary>
    /// Parses a block of the form <c>block ::= {stat} [retstat]</c> where <c>retstat ::= return [explist] [';']</c>.
    /// </summary>
    BlockSyntax ParseBlock()
    {
        var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

        // Since Lua doesn't use curly braces or something similar, we need to scan to make sure
        // the current block shouldn't be "over." Only certain tokens demarcate the end of a block
        // in the Lua grammar. This code checks for any such token. This appears to pose a problem;
        // however, if someone writes code like:
        //     while <exp> do <block> until
        // the function that parses a while block will be expecting 'end' anyway. Because of that, doing
        // this is OK, and arguably gives better diagnostics in that scenario.
        while (!BlockEnded())
        {
            var startToken = current;
            
            statements.Add(ParseStatement());

            // This helps us get out of infinite loops if ParseStatement didn't consume any tokens.
            if (current == startToken)
                NextToken();
        }

        ReturnSyntax? @return = null;
        if (CurrentIs(SyntaxKind.Return, out var returnToken))
        {
            var returnValues = StartsExpression(current) ? ParseExpressionList() : [];

            CurrentIs(SyntaxKind.Semicolon, out var semi);
            @return = new ReturnSyntax(returnValues, From(returnToken, semi ?? GetLast(returnValues, returnToken)));
        }

        var statementArr = statements.DrainToImmutable();
        return new BlockSyntax(statementArr, @return, new SequenceRange(
            statementArr.IsDefaultOrEmpty ? @return?.Location.Start ?? default : statementArr[0].Location.Start,
            @return?.Location.End ?? (statementArr.IsDefaultOrEmpty ? default : statementArr[^1].Location.End)));
    }

    /// <summary>
    /// Parses a Lua expression.
    /// <code>
    /// exp ::= nil | false | true | Numeral | LiteralString | '...' | functiondef |
    ///         prefixexp | tableconstructor | exp binop exp | unop exp
    /// </code>
    /// </summary>
    /// <param name="parentPrecedence">The precedence of the enclosing expression.</param>
    /// <param name="rightAssociative">Whether the enclosing expression is right associative.</param>
    ExpressionSyntax ParseExpression(int parentPrecedence = 0, bool? rightAssociative = null)
    {
        // This function is 'always' trying to parse out a binary expression.
        // However, if it ends up we aren't actually parsing one out, it will
        // bail out accordingly.

        ExpressionSyntax left;

        // Check if we're parsing a unary operator. If we are, we want to consume it
        // and apply it to the next expression.
        var unaryOperatorPrecedence = SyntaxFacts.UnaryOperatorPrecedence(current.Kind);
        if (unaryOperatorPrecedence != 0)
        {
            var operatorToken = NextToken();
            var operand = ParseExpression(unaryOperatorPrecedence);
            left = new UnaryExpressionSyntax(operatorToken.Kind, operand, From(operatorToken, operand));
        }
        else
        {
            left = ParsePrimaryExpression();
        }

        while (true)
        {
            var precedence = SyntaxFacts.BinaryOperatorPrecedence(current.Kind);
            // We may have gotten here, where current isn't actually a binary operator (e.g. 'f(-5)').
            // In that case, we should bail, and simply return what we've got.

            // In the case that we are parsing another operator (e.g. '2 * 3 + 4'), we understandably must check for precedence.
            // We're parsing left to right, so if we are already in a nested expression, then we need to decide where this newly
            // parsed out term goes; to the parent expression (2 * 3) or this one (3 + 4).
            // So, if the parent precedence is equal or higher, we also bail and return the term we've got.
            // As a slight caveat, some operators are right-associative. In that case, if the precedences are equal, we do
            // still want to eagerly parse them out as we go.
            if (precedence == 0 || (rightAssociative == true ? precedence < parentPrecedence : precedence <= parentPrecedence))
                break;

            var operatorToken = NextToken();

            // If we're still going, then we have the higher precedence (or we're right associative), so we can parse
            // another expression out and form a binary expression from that. Since that makes us the "parent expression"
            // now, we need to pass in our precedence, and whether we're right associative.
            var right = ParseExpression(precedence, SyntaxFacts.IsRightAssociative(operatorToken.Kind));
            left = new BinaryExpressionSyntax(left, operatorToken.Kind, right, From(left, right));
        }

        return left;
    }

    /// <summary>
    /// Parses a "primary expression," which is simply any expression which can be made a part of a binary expression.
    /// </summary>
    ExpressionSyntax ParsePrimaryExpression() => current.Kind switch
    {
        SyntaxKind.Nil => new LiteralExpressionSyntax(current, MatchToken(SyntaxKind.Nil).Location),
        SyntaxKind.True => new LiteralExpressionSyntax(current, MatchToken(SyntaxKind.True).Location),
        SyntaxKind.False => new LiteralExpressionSyntax(current, MatchToken(SyntaxKind.False).Location),
        SyntaxKind.IntegerConstant => new LiteralExpressionSyntax(current, MatchToken(SyntaxKind.IntegerConstant).Location),
        SyntaxKind.FloatConstant => new LiteralExpressionSyntax(current, MatchToken(SyntaxKind.FloatConstant).Location),
        SyntaxKind.LiteralString => new LiteralExpressionSyntax(current, MatchToken(SyntaxKind.LiteralString).Location),
        SyntaxKind.DotDotDot => new VarargExpressionSyntax(MatchToken(SyntaxKind.DotDotDot).Location),
        SyntaxKind.OpenBrace => ParseTableConstructor(),
        SyntaxKind.Function => ParseFunctionExpression(),
        _ => ParsePrefixExpression()
    };


    /// <summary>
    /// Parses a prefix expression of the form <c>prefixexp ::= var | functioncall | '(' exp ')'</c>, where
    /// <code>
    /// var ::= Name | prefixexp '[' exp ']' | prefixexp '.' Name
    /// functioncall ::= prefixexp args | prefixexp ':' Name args
    /// args ::= '(' [explist] ')' | tableconstructor | LiteralString
    /// </code>
    /// </summary>
    /// <returns></returns>
    PrefixExpressionSyntax ParsePrefixExpression()
    {
        // The grammar given in the Lua manual is left-recursive, and unsuitable for use in our parser.
        // We rewrite this grammar in the following non left-recursive form to use in our parser:
        //
        // functioncall ::= var Apply+ | '(' exp ')' Apply+
        // var ::= name Access* (Apply* Access+)* | '(' exp ')' Access* (Apply* Access+)+
        //
        // where we define extra non-terminals:
        // Apply ::= args | ':' name args
        // Access ::= '[' exp ']' | '.' name

        // Prefix expressions start with either a Name token or parenthesized expression. Call, index, and dot expressions
        // are layered on top using the previously parsed prefix as the receiver. These repeated expressions are accumulated.
        PrefixExpressionSyntax prefix;
        if (current.Kind is SyntaxKind.OpenParenthesis)
        {
            prefix = ParseParenthesizedExpression();

            if (StartsAccess(current))
            {
                // '(' exp ')' Access forces the choice var 
                prefix = ParseVariableExpression(prefix);
            }
            else if (StartsApplication(current))
            {
                // '(' exp ')' Apply forces the choice functioncall 
                prefix = ParseFunctionCallExpression(prefix);
            }
            // Otherwise, this prefix expression is just a parenthesized expression.
        }
        else
        {
            // If it's not parenthesized, we're either parsing a var or a functioncall which starts with a var
            prefix = ParseVariableExpression();
            if (StartsApplication(current))
            {
                // var Apply forces the choice functioncall
                prefix = ParseFunctionCallExpression(prefix);
            }
        }

        return prefix;
    }

    /// <summary>
    /// Parses a variable expression of the form <c>var ::= Name | prefixexp ‘[’ exp ‘]’ | prefixexp ‘.’ Name</c>.
    /// </summary>
    /// <param name="receiver"></param>
    VariableExpressionSyntax ParseVariableExpression(PrefixExpressionSyntax? parenthesizedReceiver = null)
    {
        // We use the rewritten grammar to parse variables:
        // var ::= name Access* (Apply* Access+)* | '(' exp ')' Access* (Apply* Access+)+

        // Because the initial receiver may be a parenthesized expression, we need a separate variable to store
        // the current receiver and the variable expression which we are parsing.
        VariableExpressionSyntax? variable = null;
        PrefixExpressionSyntax receiver;

        // We need to remember whether the initial receiver was a parenthesized expression for later, since we 
        // have to parse a terminal access in that case.
        bool mustParseAccess = false;
        
        if (parenthesizedReceiver is not null)
        {
            receiver = parenthesizedReceiver;
            mustParseAccess = true;
        }
        else if (current.Kind is SyntaxKind.OpenParenthesis)
        {
            receiver = ParseParenthesizedExpression();
            mustParseAccess = true;
        }
        else
        {
            receiver = variable = ParseNameExpression();
        }
        
        // Parse Access*
        while (StartsAccess(current))
        {
            receiver = variable = ParseAccess(receiver);
        }

        // Parse (Apply* Access+)
        // Ensure we parse at least one if necessary
        while (mustParseAccess || StartsApplication(current) || StartsAccess(current))
        {
            var parserState = this;
            
            // Parse Apply*
            while (StartsApplication(current))
            {
                receiver = ParseApplication(receiver);
            }

            // The last part of the prefix chain determines whether we are parsing a variable expression or a call expression.
            // If this is a call expression, we need to parse up to but not including the last Apply expression, so that the
            // accumulated variable expression can be passed as the receiver to ParseFunctionCallExpression.
            // In that case, we need to rewind the parser to before the Apply expressions we parsed and break out.
            if (mustParseAccess || StartsAccess(current))
            {
                do
                {
                    receiver = variable = ParseAccess(receiver);
                } while (StartsAccess(current));

                mustParseAccess = false;
            }
            else
            {
                this = parserState;
                break;
            }
        }

        return variable!;
    }


    /// <summary>
    /// Parses a function call expression of the form <c>functioncall ::= prefixexp args | prefixexp ‘:’ Name args</c>.
    /// </summary>
    /// <param name="receiver">The expression representing the receiver of the function call.</param>
    CallExpressionSyntax ParseFunctionCallExpression(PrefixExpressionSyntax? receiver = null)
    {
        receiver ??=
            current.Kind is SyntaxKind.OpenParenthesis 
            ? ParseParenthesizedExpression()
            : ParseVariableExpression(); 

        CallExpressionSyntax call;
        do
        {
            receiver = call = ParseApplication(receiver);
        } while (StartsApplication(current));

        return call;
    }

    /// <summary>
    /// Parses a parenthesized expression of the form <c>'(' exp ')'</c>.
    /// </summary>
    ParenthesizedExpressionSyntax ParseParenthesizedExpression()
    {
        var open = MatchToken(SyntaxKind.OpenParenthesis);
        var expr = ParseExpression();
        var close = MatchToken(SyntaxKind.CloseParenthesis);
        
        return new ParenthesizedExpressionSyntax(expr, From(open, close));
    }

    /// <summary>
    /// Parses an access (index or dot) expression of the form <c>Access ::= '[' exp ']' | '.' Name</c>.
    /// </summary>
    /// <param name="receiver">The receiver of the acess expression, parsed previously.</param>
    VariableExpressionSyntax ParseAccess(PrefixExpressionSyntax receiver)
    {
        if (CurrentIs(SyntaxKind.OpenBracket, out _))
        {
            var expr = ParseExpression();
            var close = MatchToken(SyntaxKind.CloseBracket);
            return new IndexExpressionSyntax(receiver, expr, From(receiver, close));
        }
        else
        {
            MatchToken(SyntaxKind.Dot);
            var name = MatchToken(SyntaxKind.Name);
            return new DottedExpressionSyntax(receiver, name, From(receiver, name));
        }
    }

    /// <summary>
    /// Parses a function application of the form <c>Apply ::= args | ':' Name args</c>.
    /// </summary>
    /// <param name="receiver">The receiver of the function call, parsed previously</param>
    CallExpressionSyntax ParseApplication(PrefixExpressionSyntax receiver)
    {
        SyntaxToken? name = null;
        if (CurrentIs(SyntaxKind.Colon, out _))
        {
            name = MatchToken(SyntaxKind.Name);
        }

        var arguments = ParseCallArguments(out var end);
        return new CallExpressionSyntax(receiver, name, arguments, From(receiver, end));
    }

    static bool StartsApplication(SyntaxToken token)
        => token.Kind is SyntaxKind.OpenParenthesis or SyntaxKind.OpenBrace or SyntaxKind.LiteralString or SyntaxKind.Colon;

    static bool StartsAccess(SyntaxToken token)
        => token.Kind is SyntaxKind.Dot or SyntaxKind.OpenBracket;

    NameExpressionSyntax ParseNameExpression()
    {
        var name = MatchToken(SyntaxKind.Name);
        return new NameExpressionSyntax(name);
    }

    /// <summary>
    /// Parses function call arguments, of the form <c>'(' [explist] ')' | tableconstructor | LiteralString</c>
    /// </summary>
    /// <param name="end">The last token of the argument list.</param>
    ImmutableArray<ExpressionSyntax> ParseCallArguments(out SyntaxNode end)
    {
        switch (current.Kind)
        {
            default:
            case SyntaxKind.OpenParenthesis:
                NextToken();
                var args = current.Kind != SyntaxKind.CloseParenthesis ? ParseExpressionList() : [];
                end = MatchToken(SyntaxKind.CloseParenthesis);
                return args;

            case SyntaxKind.OpenBrace:
                var tableArg = ParseTableConstructor();
                end = tableArg;
                return [tableArg];

            case SyntaxKind.LiteralString:
                var stringArg = NextToken();
                end = stringArg;
                return [new LiteralExpressionSyntax(stringArg, stringArg.Location)];
        }
    }

    /// <summary>
    /// Parses a function expression of the form <c>function funcbody</c>, where <c>funcbody ::= '(' [parlist] ')' block end</c>.
    /// </summary>
    FunctionExpressionSyntax ParseFunctionExpression()
    {
        var function = MatchToken(SyntaxKind.Function);
        var body = ParseFunctionBody();
        return new FunctionExpressionSyntax(body, From(function, body.Body));
    }

    /// <summary>
    /// Parses a table constructor of the form <c>tableconstructor ::= '{' [fieldlist] '}'</c>, where
    /// <code>
    /// fieldlist ::= field {fieldsep field} [fieldsep]
    /// field ::= '[' exp ']' '=' exp | Name '=' exp | exp
    /// fieldsep ::= ',' | ';'
    /// </code>
    /// </summary>
    /// <returns></returns>
    TableConstructorExpressionSyntax ParseTableConstructor()
    {
        var statements = ImmutableArray.CreateBuilder<FieldAssignmentSyntax>();
        var start = MatchToken(SyntaxKind.OpenBrace);
        while (IsNot(SyntaxKind.CloseBrace))
        {
            SyntaxNode target;
            ExpressionSyntax? value = null;
            switch (current.Kind)
            {
                case SyntaxKind.OpenBracket:
                    MatchToken(SyntaxKind.OpenBracket);
                    target = ParseExpression();
                    MatchToken(SyntaxKind.CloseBracket);
                    MatchToken(SyntaxKind.Equals);
                    value = ParseExpression();
                    break;

                case SyntaxKind.Name when Peek().Kind == SyntaxKind.Equals:
                    target = MatchToken(SyntaxKind.Name);
                    MatchToken(SyntaxKind.Equals);
                    value = ParseExpression();
                    break;

                default:
                    target = ParseExpression();
                    break;
            }

            statements.Add(new FieldAssignmentSyntax(target, value, From(target, value ?? target)));

            if (current.Kind is SyntaxKind.Semicolon or SyntaxKind.Comma)
            {
                NextToken();
            }
        }

        var end = MatchToken(SyntaxKind.CloseBrace);

        return new(statements.DrainToImmutable(), From(start, end));
    }

    /// <summary>
    /// Parses a function body of the form <c>funcbody ::= '(' [parlist] ')' block end</c>. This is used for function statements and expressions.
    /// </summary>
    /// <returns></returns>
    FunctionBody ParseFunctionBody()
    {
        MatchToken(SyntaxKind.OpenParenthesis);

        ImmutableArray<SyntaxToken> names;
        bool isVararg;

        if (current.Kind is not SyntaxKind.CloseParenthesis)
        {
            names = ParseParameterList(out isVararg);
        }
        else
        {
            names = ImmutableArray<SyntaxToken>.Empty;
            isVararg = false;
        }

        MatchToken(SyntaxKind.CloseParenthesis);

        var body = ParseBlock();
        MatchToken(SyntaxKind.End);
        return new FunctionBody(names, isVararg, body);
    }

    /// <summary>
    /// Parses a parameter list of the form <c>parlist ::= namelist [',' '...'] | '...'</c>.
    /// </summary>
    /// <param name="isVarargs">Returns whether the parameter list includes a varargs expression.</param>
    ImmutableArray<SyntaxToken> ParseParameterList(out bool isVarargs)
    {
        if (current.Kind is SyntaxKind.DotDotDot)
        {
            NextToken();
            isVarargs = true;
            return [];
        }

        var list = ParseNameList();
        if (current.Kind is SyntaxKind.Comma && Peek().Kind is SyntaxKind.DotDotDot)
        {
            NextToken();
            NextToken();
            isVarargs = true;
        }
        else
        {
            isVarargs = false;
        }

        return list;
    }

    /// <summary>
    /// Parses a name list of the form <c>Name {',' Name}</c>.
    /// </summary>
    /// <returns>An array of names. This array is guaranteed to have at least one element.</returns>
    ImmutableArray<SyntaxToken> ParseNameList()
    {
        var statements = ImmutableArray.CreateBuilder<SyntaxToken>();
        statements.Add(MatchToken(SyntaxKind.Name));
        while (current.Kind is SyntaxKind.Comma && Peek().Kind is SyntaxKind.Name)
        {
            NextToken();
            statements.Add(NextToken());
        }

        return statements.DrainToImmutable();
    }

    /// <summary>
    /// Parses an expression list of the form <c>exp {',' exp}</c>.
    /// </summary>
    /// <returns>An array of expressions. This array is guaranteed to have at least one element.</returns>
    ImmutableArray<ExpressionSyntax> ParseExpressionList()
    {
        var statements = ImmutableArray.CreateBuilder<ExpressionSyntax>();

        do
        {
            statements.Add(ParseExpression());
        } while (CurrentIs(SyntaxKind.Comma, out _));

        return statements.DrainToImmutable();
    }

    /// <summary>
    /// Parses a break statement of the form <c>break</c>.
    /// </summary>
    BreakStatementSyntax ParseBreakStatement()
    {
        var @break = MatchToken(SyntaxKind.Break);
        return new BreakStatementSyntax(@break.Location);
    }

    /// <summary>
    /// Parses a goto statement of the form <c>goto Name</c>.
    /// </summary>
    GotoStatementSyntax ParseGotoStatement()
    {
        var @goto = MatchToken(SyntaxKind.Goto);
        var name = MatchToken(SyntaxKind.Name);

        return new GotoStatementSyntax(name, From(@goto, name));
    }

    /// <summary>
    /// Parses a do statement of the form <c>do block end</c>.
    /// </summary>
    DoStatementSyntax ParseDoStatement()
    {
        var @do = MatchToken(SyntaxKind.Do);
        var block = ParseBlock();
        var end = MatchToken(SyntaxKind.End);

        return new DoStatementSyntax(block, From(@do, end));
    }

    /// <summary>
    /// Parses a while statement of the form <c>while exp do block end</c>.
    /// </summary>
    WhileStatementExpression ParseWhileStatement()
    {
        var @while = MatchToken(SyntaxKind.While);
        var expression = ParseExpression();
        MatchToken(SyntaxKind.Do);
        var body = ParseBlock();
        var end = MatchToken(SyntaxKind.End);

        return new WhileStatementExpression(expression, body, From(@while, end));
    }


    /// <summary>
    /// Parses a repeat statement of the form <c>repeat block until exp</c>.
    /// </summary>
    RepeatStatementSyntax ParseRepeatStatement()
    {
        var repeat = MatchToken(SyntaxKind.Repeat);
        var body = ParseBlock();
        MatchToken(SyntaxKind.Until);
        var expr = ParseExpression();

        return new RepeatStatementSyntax(body, expr, From(repeat, expr));
    }

    /// <summary>
    /// Parses an if statement of the form <c>if exp then block {elseif exp then block} [else block] end</c>.
    /// </summary>
    IfStatementSyntax ParseIfStatement()
    {
        var @if = MatchToken(SyntaxKind.If);
        var expr = ParseExpression();
        var then = MatchToken(SyntaxKind.Then);
        var body = ParseBlock();

        var elseIfClauses = ImmutableArray.CreateBuilder<ElseIfClauseSyntax>();
        ElseClauseSyntax? elseClause = null;

        while (true)
        {
            switch (current.Kind)
            {
                case SyntaxKind.ElseIf:
                    var elif = MatchToken(SyntaxKind.ElseIf);
                    var elifExpr = ParseExpression();
                    var elifThen = MatchToken(SyntaxKind.Then);
                    var elifBody = ParseBlock();
                    elseIfClauses.Add(new ElseIfClauseSyntax(elifExpr, elifBody, From(elif, GetLast(elifBody.Body, elifThen))));
                    break;

                case SyntaxKind.Else:
                    var @else = MatchToken(SyntaxKind.If);
                    var elseBody = ParseBlock();
                    elseClause = new ElseClauseSyntax(elseBody, From(@else, GetLast(body.Body, @else)));
                    break;

                default:
                case SyntaxKind.End:
                    goto exit;
            }
        }

    exit:

        var clauses = elseIfClauses.DrainToImmutable();
        return new IfStatementSyntax(expr, body, clauses, elseClause,
            From(@if, elseClause ?? GetLast(clauses, GetLast(body.Body, then))));
    }

    /// <summary>
    /// Parses a for statement of the form
    /// <code>
    /// for Name '=' exp ',' exp [',' exp] do block end |
    /// for namelist in explist do block end
    /// </code>
    /// </summary>
    /// <returns></returns>
    StatementSyntax ParseForStatement()
    {
        var @for = MatchToken(SyntaxKind.For);
        switch (Peek().Kind)
        {
            case SyntaxKind.Equals:
                var name = MatchToken(SyntaxKind.Name);
                MatchToken(SyntaxKind.Equals);
                var initial = ParseExpression();
                MatchToken(SyntaxKind.Comma);
                var limit = ParseExpression();
                ExpressionSyntax? step = null;
                if (CurrentIs(SyntaxKind.Comma, out _))
                    step = ParseExpression();

                MatchToken(SyntaxKind.Do);
                var body = ParseBlock();
                var end = MatchToken(SyntaxKind.End);

                return new NumericalForStatementSyntax(name, initial, limit, step, body, From(@for, end));

            case SyntaxKind.Comma:
            case SyntaxKind.In:
            default:
                var list = ParseNameList();
                MatchToken(SyntaxKind.In);
                var exprs = ParseExpressionList();
                MatchToken(SyntaxKind.Do);
                var inBody = ParseBlock();
                var inEnd = MatchToken(SyntaxKind.End);

                return new ForStatementSyntax(list, exprs, inBody, From(@for, inEnd));
        }
    }

    /// <summary>
    /// Parses a label statement of the form <c>'::' Name '::'</c>.
    /// </summary>
    /// <returns></returns>
    LabelStatementSyntax ParseLabelStatement()
    {
        var first = MatchToken(SyntaxKind.ColonColon);
        var name = MatchToken(SyntaxKind.Name);
        var last = MatchToken(SyntaxKind.ColonColon);

        return new LabelStatementSyntax(name, From(first, last));
    }
}
