//  
//  Copyright (C) 2020 John Tur
//  
//  This file is part of BrickLua, a high-performance Lua 5.4 implementation in C#.
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

namespace BrickLua.Syntax
{
    public sealed class NumericalForStatementSyntax : StatementSyntax
    {
        public NumericalForStatementSyntax(SyntaxToken initialValueIdentifier, ExpressionSyntax initialValue, ExpressionSyntax limit, ExpressionSyntax? step, BlockStatementSyntax body, in SequenceRange location) : base(location)
        {
            InitialValueIdentifier = initialValueIdentifier;
            InitialValue = initialValue;
            Limit = limit;
            Step = step;
            Body = body;
        }

        public SyntaxToken InitialValueIdentifier { get; }
        public ExpressionSyntax InitialValue { get; }
        public ExpressionSyntax Limit { get; }
        public ExpressionSyntax? Step { get; }
        public BlockStatementSyntax Body { get; }
        public override SyntaxKind Kind => SyntaxKind.NumericalFor;
    }
}