﻿using System;
using System.Diagnostics;
using Cheez.Parsing;
using Cheez.Visitor;

namespace Cheez.Ast
{
    public class IdentifierExpression : Expression
    {
        public String Name { get; set; }

        public IdentifierExpression(TokenLocation beg, TokenLocation end, string name) : base(beg, end)
        {
            this.Name = name;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitIdentifierExpression(this, data);
        }

        [DebuggerStepThrough]
        public override void Accept<D>(IVoidVisitor<D> visitor, D data = default)
        {
            visitor.VisitIdentifierExpression(this, data);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
