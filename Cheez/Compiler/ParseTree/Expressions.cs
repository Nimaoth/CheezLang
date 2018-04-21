﻿using Cheez.Compiler.Ast;
using Cheez.Compiler.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cheez.Compiler.ParseTree
{
    public abstract class PTExpr : ILocation
    {
        public TokenLocation Beginning { get; set; }
        public TokenLocation End { get; set; }

        public PTExpr(TokenLocation beg, TokenLocation end)
        {
            this.Beginning = beg;
            this.End = end;
        }

        public abstract AstExpression CreateAst();

        //public T CreateAst<T>()
        //    where T : AstExpression
        //{
        //    return (T)CreateGenericAst();
        //}
    }

    public abstract class PTLiteral : PTExpr
    {
        public PTLiteral(TokenLocation beg, TokenLocation end) : base(beg, end)
        {
        }
    }

    public class PTStringLiteral : PTLiteral
    {
        public string Value { get; set; }

        public PTStringLiteral(TokenLocation beg, string value) : base(beg, beg)
        {
            this.Value = value;
        }

        public override AstExpression CreateAst()
        {
            return new AstStringLiteral(this, Value);
        }
    }

    public class PTNumberExpr : PTLiteral
    {
        private NumberData mData;
        public NumberData Data => mData;

        public PTNumberExpr(TokenLocation loc, NumberData data) : base(loc, loc)
        {
            mData = data;
        }

        public override AstExpression CreateAst()
        {
            return new AstNumberExpr(this, Data);
        }
    }

    public class PTDotExpr : PTExpr
    {
        public PTExpr Left { get; set; }
        public PTIdentifierExpr Right { get; set; }

        public PTDotExpr(TokenLocation beg, TokenLocation end, PTExpr left, PTIdentifierExpr right) : base(beg, end)
        {
            this.Left = left;
            this.Right = right;
        }

        public override AstExpression CreateAst()
        {
            return new AstDotExpr(this, Left.CreateAst(), Right.Name);
        }
    }

    public class PTCallExpr : PTExpr
    {
        public PTExpr Function { get; }
        public List<PTExpr> Arguments { get; set; }

        public PTCallExpr(TokenLocation beg, TokenLocation end, PTExpr func, List<PTExpr> args) : base(beg, end)
        {
            Function = func;
            Arguments = args;
        }

        public override AstExpression CreateAst()
        {
            var args = Arguments.Select(a => a.CreateAst()).ToList();
            return new AstCallExpr(this, Function.CreateAst(), args);
        }
    }

    public class PTBinaryExpr : PTExpr
    {
        public Operator Operator { get; set; }
        public PTExpr Left { get; set; }
        public PTExpr Right { get; set; }

        public PTBinaryExpr(TokenLocation beg, TokenLocation end, Operator op, PTExpr lhs, PTExpr rhs) : base(beg, end)
        {
            Operator = op;
            Left = lhs;
            Right = rhs;
        }

        public override AstExpression CreateAst()
        {
            return new AstBinaryExpr(this, Operator, Left.CreateAst(), Right.CreateAst());
        }
    }

    public class PTIdentifierExpr : PTExpr
    {
        public string Name { get; set; }

        public PTIdentifierExpr(TokenLocation beg, string name) : base(beg, beg)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override AstExpression CreateAst()
        {
            return new AstIdentifierExpr(this, Name);
        }
    }
}
