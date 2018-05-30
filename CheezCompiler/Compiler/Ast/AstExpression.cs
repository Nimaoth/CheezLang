﻿using Cheez.Compiler.Parsing;
using Cheez.Compiler.Visitor;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cheez.Compiler.Ast
{
    public enum ExprFlags
    {
        IsLValue = 0
    }

    public abstract class AstExpression : IVisitorAcceptor
    {
        //public int Id { get; }

        public abstract ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public CheezType Type { get; set; }
        public Scope Scope { get; set; }
        private int mFlags = 0;

        [DebuggerStepThrough]
        public AstExpression()
        {
        }

        [DebuggerStepThrough]
        public void SetFlag(ExprFlags f)
        {
            mFlags |= 1 << (int)f;
        }

        [DebuggerStepThrough]
        public bool GetFlag(ExprFlags f)
        {
            return (mFlags & (1 << (int)f)) != 0;
        }
        
        [DebuggerStepThrough]
        public abstract T Accept<T, D>(IVisitor<T, D> visitor, D data = default);

        [DebuggerStepThrough]
        public abstract AstExpression Clone();
    }

    public class AstEmptyExpr : AstExpression
    {
        //public ParseTree.PTStringLiteral ParseTreeNode => GenericParseTreeNode as ParseTree.PTStringLiteral;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        [DebuggerStepThrough]
        public AstEmptyExpr(ParseTree.PTExpr node) : base()
        {
            GenericParseTreeNode = node;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D))
        {
            return visitor.VisitEmptyExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstEmptyExpr(GenericParseTreeNode)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return "()";
        }
    }

    public abstract class AstLiteral : AstExpression
    {
        public AstLiteral() : base()
        {
        }
    }

    public class AstStringLiteral : AstLiteral
    {
        //public ParseTree.PTStringLiteral ParseTreeNode => GenericParseTreeNode as ParseTree.PTStringLiteral;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public string Value { get; set; }
        
        [DebuggerStepThrough]
        public AstStringLiteral(ParseTree.PTExpr node, string value) : base()
        {
            GenericParseTreeNode = node;
            this.Value = value;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D))
        {
            return visitor.VisitStringLiteral(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstStringLiteral(GenericParseTreeNode, Value)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return "string-lit";
        }
    }

    public class AstDotExpr : AstExpression
    {
        //public ParseTree.PTDotExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTDotExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public AstExpression Left { get; set; }
        public string Right { get; set; }
        public bool IsDoubleColon { get; set; }

        [DebuggerStepThrough]
        public AstDotExpr(ParseTree.PTExpr node, AstExpression left, string right, bool isDC) : base()
        {
            GenericParseTreeNode = node;
            this.Left = left;
            this.Right = right;
            IsDoubleColon = isDC;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitDotExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstDotExpr(GenericParseTreeNode, Left.Clone(), Right, IsDoubleColon)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return $"{Left}.{Right}";
        }
    }

    public class AstCallExpr : AstExpression
    {
        //public ParseTree.PTCallExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTCallExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public AstExpression Function { get; set; }
        public List<AstExpression> Arguments { get; set; }

        [DebuggerStepThrough]
        public AstCallExpr(ParseTree.PTExpr node, AstExpression func, List<AstExpression> args) : base()
        {
            GenericParseTreeNode = node;
            Function = func;
            Arguments = args;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitCallExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstCallExpr(GenericParseTreeNode, Function.Clone(), Arguments.Select(a => a.Clone()).ToList())
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return $"{Function}(...)";
        }
    }

    public class AstBinaryExpr : AstExpression, ITempVariable
    {
        //public ParseTree.PTBinaryExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTBinaryExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public string Operator { get; set; }
        public AstExpression Left { get; set; }
        public AstExpression Right { get; set; }

        public string Name => "";

        [DebuggerStepThrough]
        public AstBinaryExpr(ParseTree.PTExpr node, string op, AstExpression lhs, AstExpression rhs) : base()
        {
            GenericParseTreeNode = node;
            Operator = op;
            Left = lhs;
            Right = rhs;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitBinaryExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstBinaryExpr(GenericParseTreeNode, Operator, Left.Clone(), Right.Clone())
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return $"{Left} {Operator} {Right}";
        }
    }

    public class AstUnaryExpr : AstExpression
    {
        //public ParseTree.PTBinaryExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTBinaryExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public string Operator { get; set; }
        public AstExpression SubExpr { get; set; }

        [DebuggerStepThrough]
        public AstUnaryExpr(ParseTree.PTExpr node, string op, AstExpression sub) : base()
        {
            GenericParseTreeNode = node;
            Operator = op;
            SubExpr = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitUnaryExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstUnaryExpr(GenericParseTreeNode, Operator, SubExpr.Clone())
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }
    }

    public class AstBoolExpr : AstExpression
    {
        //public ParseTree.PTBoolExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTBoolExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public bool Value { get; }

        [DebuggerStepThrough]
        public AstBoolExpr(ParseTree.PTExpr node, bool value)
        {
            GenericParseTreeNode = node;
            this.Value = value;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitBoolExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstBoolExpr(GenericParseTreeNode, Value)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }
    }

    public class AstAddressOfExpr : AstExpression
    {
        //public ParseTree.PTAddressOfExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTAddressOfExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public AstExpression SubExpression { get; set; }

        [DebuggerStepThrough]
        public AstAddressOfExpr(ParseTree.PTExpr node, AstExpression sub)
        {
            GenericParseTreeNode = node;
            SubExpression = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitAddressOfExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstAddressOfExpr(GenericParseTreeNode, SubExpression.Clone())
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }
    }

    public class AstDereferenceExpr : AstExpression
    {
        //public ParseTree.PTExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public AstExpression SubExpression { get; set; }

        [DebuggerStepThrough]
        public AstDereferenceExpr(ParseTree.PTExpr node, AstExpression sub)
        {
            GenericParseTreeNode = node;
            SubExpression = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitDereferenceExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstDereferenceExpr(GenericParseTreeNode, SubExpression.Clone())
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return $"*{SubExpression}";
        }
    }

    public class AstCastExpr : AstExpression
    {
        public ParseTree.PTCastExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTCastExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public AstExpression SubExpression { get; set; }

        [DebuggerStepThrough]
        public AstCastExpr(ParseTree.PTExpr node, AstExpression sub)
        {
            GenericParseTreeNode = node;
            SubExpression = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitCastExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstCastExpr(GenericParseTreeNode, SubExpression.Clone())
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return $"<{Type}>({SubExpression})";
        }
    }
    
    public class AstArrayAccessExpr : AstExpression
    {
        //public ParseTree.PTArrayAccessExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTArrayAccessExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public AstExpression SubExpression { get; set; }
        public AstExpression Indexer { get; set; }

        [DebuggerStepThrough]
        public AstArrayAccessExpr(ParseTree.PTExpr node, AstExpression sub, AstExpression index)
        {
            GenericParseTreeNode = node;
            SubExpression = sub;
            Indexer = index;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitArrayAccessExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstArrayAccessExpr(GenericParseTreeNode, SubExpression.Clone(), Indexer.Clone())
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }
    }

    public class AstNumberExpr : AstExpression
    {
        //public ParseTree.PTNumberExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTNumberExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        private NumberData mData;
        public NumberData Data => mData;

        [DebuggerStepThrough]
        public AstNumberExpr(ParseTree.PTExpr node, NumberData data) : base()
        {
            GenericParseTreeNode = node;
            mData = data;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitNumberExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstNumberExpr(GenericParseTreeNode, Data)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return mData.StringValue;
        }
    }

    public class AstIdentifierExpr : AstExpression
    {
        //public ParseTree.PTIdentifierExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTIdentifierExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        public string Name { get; set; }
        public ISymbol Symbol { get; set; }

        [DebuggerStepThrough]
        public AstIdentifierExpr(ParseTree.PTExpr node, string name) : base()
        {
            GenericParseTreeNode = node;
            this.Name = name;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitIdentifierExpression(this, data);
        }

        public override string ToString()
        {
            return Name;
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstIdentifierExpr(GenericParseTreeNode, Name)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }
    }

    public class AstTypeExpr : AstExpression
    {
        public ParseTree.PTTypeExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTTypeExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }

        [DebuggerStepThrough]
        public AstTypeExpr(ParseTree.PTExpr node) : base()
        {
            GenericParseTreeNode = node;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitTypeExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstTypeExpr(GenericParseTreeNode)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            return GenericParseTreeNode?.ToString() ?? Type?.ToString() ?? base.ToString();
        }
    }


    public class AstStructMemberInitialization
    {
        public ParseTree.PTStructMemberInitialization GenericParseTreeNode { get; set; }

        public string Name { get; set; }
        public AstExpression Value { get; set; }

        public AstStructMemberInitialization(ParseTree.PTStructMemberInitialization node, string name, AstExpression expr)
        {
            this.GenericParseTreeNode = node;
            this.Name = name;
            this.Value = expr;
        }
    }


    public class AstStructValueExpr : AstExpression
    {
        public ParseTree.PTStructValueExpr ParseTreeNode => GenericParseTreeNode as ParseTree.PTStructValueExpr;
        public override ParseTree.PTExpr GenericParseTreeNode { get; set; }
        public string Name { get; }
        public AstStructMemberInitialization[] MemberInitializers { get; }

        [DebuggerStepThrough]
        public AstStructValueExpr(ParseTree.PTExpr node, string name, AstStructMemberInitialization[] inits) : base()
        {
            GenericParseTreeNode = node;
            this.Name = name;
            this.MemberInitializers = inits;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitStructValueExpression(this, data);
        }

        [DebuggerStepThrough]
        public override AstExpression Clone()
        {
            return new AstStructValueExpr(GenericParseTreeNode, Name, MemberInitializers)
            {
                Type = this.Type,
                Scope = this.Scope
            };
        }

        public override string ToString()
        {
            var i = string.Join(", ", MemberInitializers.Select(m => m.Name != null ? $"{m.Name} = {m.Value}" : m.Value.ToString()));
            return $"{Name} {{ {i} }}";
        }
    }
}
