﻿using Cheez.Ast.Expressions.Types;
using Cheez.Ast.Statements;
using Cheez.Extras;
using Cheez.Types;
using Cheez.Visitors;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cheez.Ast.Expressions
{
    public enum ExprFlags
    {
        IsLValue = 0
    }

    public abstract class AstExpression : IVisitorAcceptor, ILocation
    {
        private int mFlags = 0;

        public ILocation Location { get; private set; }
        public TokenLocation Beginning => Location?.Beginning;
        public TokenLocation End => Location?.End;

        public CheezType Type { get; set; }
        public object Value { get; set; }
        public Scope Scope { get; set; }

        public abstract bool IsPolymorphic { get; }

        public bool IsCompTimeValue { get; set; } = false;

        [DebuggerStepThrough]
        public AstExpression(ILocation Location = null)
        {
            this.Location = Location;
        }

        [DebuggerStepThrough]
        public void SetFlag(ExprFlags f)
        {
            mFlags |= 1 << (int)f;
        }

        [DebuggerStepThrough]
        public bool GetFlag(ExprFlags f) => (mFlags & (1 << (int)f)) != 0;

        [DebuggerStepThrough]
        public abstract T Accept<T, D>(IVisitor<T, D> visitor, D data = default);

        [DebuggerStepThrough]
        public abstract AstExpression Clone();

        protected T CopyValuesTo<T>(T to)
            where T : AstExpression
        {
            to.Location = this.Location;
            to.Scope = this.Scope;
            to.mFlags = this.mFlags;
            to.Value = this.Value;
            return to;
        }

        public override string ToString()
        {
            var sb = new StringWriter();
            new RawAstPrinter(sb).PrintExpression(this);
            return sb.GetStringBuilder().ToString();
        }
    }

    public class AstEmptyExpr : AstExpression
    {
        public override bool IsPolymorphic => false;

        [DebuggerStepThrough]
        public AstEmptyExpr(ILocation Location = null) : base(Location)
        { }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D)) => visitor.VisitEmptyExpression(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstEmptyExpr());
    }

    public abstract class AstLiteral : AstExpression
    {
        public AstLiteral(ILocation Location = null) : base(Location)
        { }
    }

    public class AstCharLiteral : AstLiteral
    {
        public override bool IsPolymorphic => false;
        public char CharValue { get; set; }
        public string RawValue { get; set; }

        [DebuggerStepThrough]
        public AstCharLiteral(string rawValue, ILocation Location = null) : base(Location)
        {
            this.RawValue = rawValue;
            this.IsCompTimeValue = true;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D)) => visitor.VisitCharLiteralExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstCharLiteral(RawValue) { CharValue = this.CharValue });
    }

    public class AstStringLiteral : AstLiteral
    {
        public override bool IsPolymorphic => false;
        public string StringValue => (string)Value;

        [DebuggerStepThrough]
        public AstStringLiteral(string value, ILocation Location = null) : base(Location)
        {
            this.Value = value;
            this.IsCompTimeValue = true;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D)) => visitor.VisitStringLiteralExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstStringLiteral(Value as string));
    }

    public class AstDotExpr : AstExpression
    {
        public AstExpression Left { get; set; }
        public AstIdExpr Right { get; set; }
        public bool IsDoubleColon { get; set; }
        public override bool IsPolymorphic => Left.IsPolymorphic;

        [DebuggerStepThrough]
        public AstDotExpr(AstExpression left, AstIdExpr right, bool isDC, ILocation Location = null) : base(Location)
        {
            this.Left = left;
            this.Right = right;
            IsDoubleColon = isDC;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitDotExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstDotExpr(Left.Clone(), Right.Clone() as AstIdExpr, IsDoubleColon));
    }

    public class AstCallExpr : AstExpression
    {
        public AstExpression Function { get; set; }
        public List<AstExpression> Arguments { get; set; }
        public override bool IsPolymorphic => Function.IsPolymorphic || Arguments.Any(a => a.IsPolymorphic);

        [DebuggerStepThrough]
        public AstCallExpr(AstExpression func, List<AstExpression> args, ILocation Location = null) : base(Location)
        {
            Function = func;
            Arguments = args;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitCallExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstCallExpr(Function.Clone(),  Arguments.Select(a => a.Clone()).ToList()));
    }

    public class AstCompCallExpr : AstExpression
    {
        public AstIdExpr Name { get; set; }
        public List<AstExpression> Arguments { get; set; }

        public override bool IsPolymorphic => false;

        [DebuggerStepThrough]
        public AstCompCallExpr(AstIdExpr func, List<AstExpression> args, ILocation Location = null) : base(Location)
        {
            Name = func;
            Arguments = args;
            IsCompTimeValue = true;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitCompCallExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstCompCallExpr(Name.Clone() as AstIdExpr, Arguments.Select(a => a.Clone()).ToList()));
    }

    public class AstBinaryExpr : AstExpression, ITempVariable
    {
        public string Operator { get; set; }
        public AstExpression Left { get; set; }
        public AstExpression Right { get; set; }

        public AstIdExpr Name => null;
        public override bool IsPolymorphic => Left.IsPolymorphic || Right.IsPolymorphic;

        [DebuggerStepThrough]
        public AstBinaryExpr(string op, AstExpression lhs, AstExpression rhs, ILocation Location = null) : base(Location)
        {
            Operator = op;
            Left = lhs;
            Right = rhs;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitBinaryExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstBinaryExpr(Operator, Left.Clone(), Right.Clone()));
    }

    public class AstUnaryExpr : AstExpression
    {
        public string Operator { get; set; }
        public AstExpression SubExpr { get; set; }
        public override bool IsPolymorphic => SubExpr.IsPolymorphic;

        [DebuggerStepThrough]
        public AstUnaryExpr(string op, AstExpression sub, ILocation Location = null) : base(Location)
        {
            Operator = op;
            SubExpr = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitUnaryExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstUnaryExpr(Operator, SubExpr.Clone()));
    }

    public class AstBoolExpr : AstExpression
    {
        public override bool IsPolymorphic => false;

        public bool BoolValue => (bool)Value;

        [DebuggerStepThrough]
        public AstBoolExpr(bool value, ILocation Location = null) : base(Location)
        {
            this.Value = value;
            IsCompTimeValue = true;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitBoolExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstBoolExpr((bool)Value));
    }

    public class AstAddressOfExpr : AstExpression
    {
        public AstExpression SubExpression { get; set; }
        public override bool IsPolymorphic => SubExpression.IsPolymorphic;
        public bool IsReference = false;

        [DebuggerStepThrough]
        public AstAddressOfExpr(AstExpression sub, ILocation Location = null) : base(Location)
        {
            SubExpression = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitAddressOfExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstAddressOfExpr(SubExpression.Clone()) { IsReference = this.IsReference });
    }

    public class AstDereferenceExpr : AstExpression
    {
        public AstExpression SubExpression { get; set; }
        public override bool IsPolymorphic => SubExpression.IsPolymorphic;

        [DebuggerStepThrough]
        public AstDereferenceExpr(AstExpression sub, ILocation Location = null) : base(Location)
        {
            SubExpression = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitDerefExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone()
            => CopyValuesTo(new AstDereferenceExpr(SubExpression.Clone()));
    }

    public class AstCastExpr : AstExpression, ITempVariable
    {
        public AstExpression SubExpression { get; set; }
        public AstExpression TypeExpr { get; set; }
        public override bool IsPolymorphic => SubExpression.IsPolymorphic || Type.IsPolyType;

        public AstIdExpr Name => null;

        [DebuggerStepThrough]
        public AstCastExpr(AstExpression typeExpr, AstExpression sub, ILocation Location = null) : base(Location)
        {
            this.TypeExpr = typeExpr;
            SubExpression = sub;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitCastExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone() => CopyValuesTo(new AstCastExpr(TypeExpr.Clone(), SubExpression.Clone()));
    }

    public class AstArrayAccessExpr : AstExpression
    {
        public AstExpression SubExpression { get; set; }
        public AstExpression Indexer { get; set; }
        public override bool IsPolymorphic => SubExpression.IsPolymorphic || Indexer.IsPolymorphic;

        [DebuggerStepThrough]
        public AstArrayAccessExpr(AstExpression sub, AstExpression index, ILocation Location = null) : base(Location)
        {
            SubExpression = sub;
            Indexer = index;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitArrayAccessExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone() => CopyValuesTo(new AstArrayAccessExpr(SubExpression.Clone(), Indexer.Clone()));
    }

    public class AstNullExpr : AstExpression
    {
        public override bool IsPolymorphic => false;

        [DebuggerStepThrough]
        public AstNullExpr(ILocation Location = null) : base(Location)
        {
            IsCompTimeValue = true;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitNullExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone() => CopyValuesTo(new AstNullExpr());
    }

    public class AstNumberExpr : AstExpression
    {
        private NumberData mData;
        public NumberData Data => mData;
        public override bool IsPolymorphic => false;

        [DebuggerStepThrough]
        public AstNumberExpr(NumberData data, ILocation Location = null) : base(Location)
        {
            mData = data;
            IsCompTimeValue = true;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitNumberExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone() => CopyValuesTo(new AstNumberExpr(Data));
    }

    public class AstIdExpr : AstExpression
    {
        public string Name { get; set; }
        public ISymbol Symbol { get; set; }

        public override bool IsPolymorphic => _isPolymorphic;
        private bool _isPolymorphic;

        [DebuggerStepThrough]
        public AstIdExpr(string name, bool isPolyTypeExpr, ILocation Location = null) : base(Location)
        {
            this.Name = name;
            this._isPolymorphic = isPolyTypeExpr;
        }

        public void SetIsPolymorphic(bool b)
        {
            _isPolymorphic = b;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitIdExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone() => CopyValuesTo(new AstIdExpr(Name, IsPolymorphic));
    }

    public class AstStructMemberInitialization: ILocation
    {
        public ILocation Location { get; private set; }
        public TokenLocation Beginning => Location?.Beginning;
        public TokenLocation End => Location?.End;

        public AstIdExpr Name { get; set; }
        public AstExpression Value { get; set; }

        public int Index { get; set; }

        public AstStructMemberInitialization(AstIdExpr name, AstExpression expr, ILocation Location = null)
        {
            this.Location = Location;
            this.Name = name;
            this.Value = expr;
        }
    }

    public class AstStructValueExpr : AstExpression, ITempVariable
    {
        public AstTypeExpr TypeExpr { get; set; }
        public List<AstStructMemberInitialization> MemberInitializers { get; }
        public override bool IsPolymorphic => false;

        public AstIdExpr Name => null;

        [DebuggerStepThrough]
        public AstStructValueExpr(AstTypeExpr type, List<AstStructMemberInitialization> inits, ILocation Location = null) : base(Location)
        {
            this.TypeExpr = type;
            this.MemberInitializers = inits;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitStructValueExpr(this, data);

        [DebuggerStepThrough]
        public override AstExpression Clone() => CopyValuesTo(new AstStructValueExpr(TypeExpr, MemberInitializers));
    }

    public class AstArrayExpr : AstExpression, ITempVariable
    {
        public override bool IsPolymorphic => false;

        public List<AstExpression> Values { get; set; }

        public AstIdExpr Name => null;

        public AstArrayExpr(List<AstExpression> values, ILocation Location = null) : base(Location)
        {
            this.Values = values;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitArrayExpr(this, data);

        public override AstExpression Clone() => CopyValuesTo(new AstArrayExpr(Values.Select(v => v.Clone()).ToList()));
    }
}