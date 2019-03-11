﻿using Cheez.Ast.Expressions;
using Cheez.Visitors;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cheez.Ast.Statements
{
    public enum StmtFlags
    {
        GlobalScope,
        Returns,
        IsLastStatementInBlock
    }

    public interface IAstNode {
        IAstNode Parent { get; }
    }

    public abstract class AstStatement : IVisitorAcceptor, ILocation, IAstNode
    {
        protected int mFlags = 0;

        public ILocation Location { get; private set; }
        public TokenLocation Beginning => Location?.Beginning;
        public TokenLocation End => Location?.End;

        public PTFile SourceFile { get; set; }

        public Scope Scope { get; set; }
        public List<AstDirective> Directives { get; protected set; }

        public IAstNode Parent { get; set; }

        public AstStatement(List<AstDirective> dirs = null, ILocation Location = null)
        {
            this.Directives = dirs ?? new List<AstDirective>();
            this.Location = Location;
        }

        public void SetFlag(StmtFlags f)
        {
            mFlags |= 1 << (int)f;
        }
        
        public void ClearFlag(StmtFlags f)
        {
            var mask = ~(1 << (int)f);
            mFlags &= mask;
        }

        public bool GetFlag(StmtFlags f) => (mFlags & (1 << (int)f)) != 0;
        public bool HasDirective(string name) => Directives.Find(d => d.Name.Name == name) != null;

        public AstDirective GetDirective(string name)
        {
            return Directives.FirstOrDefault(d => d.Name.Name == name);
        }

        public bool TryGetDirective(string name, out AstDirective dir)
        {
            dir = Directives.FirstOrDefault(d => d.Name.Name == name);
            return dir != null;
        }

        [DebuggerStepThrough]
        public abstract T Accept<T, D>(IVisitor<T, D> visitor, D data = default);

        public abstract AstStatement Clone();

        protected T CopyValuesTo<T>(T to)
            where T : AstStatement
        {
            to.Location = this.Location;
            to.Parent = this.Parent;
            to.Scope = this.Scope;
            to.Directives = this.Directives;
            to.mFlags = this.mFlags;
            return to;
        }

        public override string ToString()
        {
            var sb = new StringWriter();
            new RawAstPrinter(sb).PrintStatement(this);
            return sb.GetStringBuilder().ToString();
        }
    }

    public class AstDirectiveStatement : AstStatement
    {
        public AstDirective Directive;

        public AstDirectiveStatement(AstDirective Directive, ILocation Location = null) : base(Location: Location)
        {
            this.Directive = Directive;
        }

        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitDirectiveStmt(this, data);

        public override AstStatement Clone() => CopyValuesTo(new AstDirectiveStatement(Directive));
    }

    public class AstEmptyStatement : AstStatement
    {
        public AstEmptyStatement(ILocation Location = null) : base(Location: Location) {}
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitEmptyStmt(this, data);
        public override AstStatement Clone() =>  CopyValuesTo(new AstEmptyStatement());
    }

    public class AstDeferStmt : AstStatement
    {
        public AstStatement Deferred { get; set; }

        public AstDeferStmt(AstStatement deferred, List<AstDirective> Directives = null, ILocation Location = null)
            : base(Directives, Location)
        {
            this.Deferred = deferred;
        }

        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitDeferStmt(this, data);

        public override AstStatement Clone() => CopyValuesTo(new AstDeferStmt(Deferred.Clone()));
    }

    public class AstWhileStmt : AstStatement
    {
        public AstExpression Condition { get; set; }
        public AstBlockExpr Body { get; set; }

        public AstVariableDecl PreAction { get; set; }
        public AstStatement PostAction { get; set; }

        public Scope SubScope { get; set; }

        public AstWhileStmt(AstExpression cond, AstBlockExpr body, AstVariableDecl pre, AstStatement post, ILocation Location = null)
            : base(Location: Location)
        {
            this.Condition = cond;
            this.Body = body;
            this.PreAction = pre;
            this.PostAction = post;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitWhileStmt(this, data);
        public override AstStatement Clone() 
            => CopyValuesTo(new AstWhileStmt(Condition.Clone(), Body.Clone() as AstBlockExpr, PreAction?.Clone() as AstVariableDecl, PostAction?.Clone()));
    }

    public class AstReturnStmt : AstStatement
    {
        public AstExpression ReturnValue { get; set; }
        public List<AstStatement> DeferredStatements { get; } = new List<AstStatement>();

        public AstReturnStmt(AstExpression values, ILocation Location = null)
            : base(Location: Location)
        {
            ReturnValue = values;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitReturnStmt(this, data);
        public override AstStatement Clone() => CopyValuesTo(new AstReturnStmt(ReturnValue.Clone()));
    }

    public class AstAssignment : AstStatement
    {
        public AstExpression Pattern { get; set; }
        public AstExpression Value { get; set; }
        public string Operator { get; set; }

        public List<AstAssignment> SubAssignments { get; set; }

        public AstAssignment(AstExpression target, AstExpression value, string op = null, ILocation Location = null)
            : base(Location: Location)
        {
            this.Pattern = target;
            this.Value = value;
            this.Operator = op;
        }

        public void AddSubAssignment(AstAssignment ass)
        {
            if (SubAssignments == null) SubAssignments = new List<AstAssignment>();
            SubAssignments.Add(ass);
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitAssignmentStmt(this, data);

        public override AstStatement Clone()
            => CopyValuesTo(new AstAssignment(Pattern.Clone(), Value.Clone(), Operator));
    }

    public class AstExprStmt : AstStatement
    {
        public AstExpression Expr { get; set; }

        [DebuggerStepThrough]
        public AstExprStmt(AstExpression expr, ILocation Location = null) : base(Location: Location)
        {
            this.Expr = expr;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitExpressionStmt(this, data);

        public override AstStatement Clone()
            => CopyValuesTo(new AstExprStmt(Expr.Clone()));
    }

    public class AstUsingStmt : AstStatement
    {
        public AstExpression Value { get; set; }

        [DebuggerStepThrough]
        public AstUsingStmt(AstExpression expr, List<AstDirective> Directives = null, ILocation Location = null)
            : base(Directives, Location)
        {
            Value = expr;
        }
        
        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitUsingStmt(this, data);

        public override AstStatement Clone()
            => CopyValuesTo(new AstUsingStmt(Value.Clone()));
    }

    public class AstMatchCase : ILocation
    {
        public ILocation Location { get; private set; }
        public TokenLocation Beginning => Location?.Beginning;
        public TokenLocation End => Location?.End;
        
        public AstExpression Value { get; set; }
        public AstStatement Body { get; set; }

        public AstMatchCase(AstExpression value, AstStatement body, ILocation Location = null)
        {
            this.Location = Location;
            this.Value = value;
            this.Body = body;
        }

        public AstMatchCase Clone() => new AstMatchCase(Value.Clone(), Body.Clone(), Location);
    }

    public class AstMatchStmt : AstStatement
    {
        public AstExpression Value { get; set; }
        public List<AstMatchCase> Cases { get; set; }

        public AstMatchStmt(AstExpression value, List<AstMatchCase> cases, List<AstDirective> Directives = null, ILocation Location = null)
            : base(Directives, Location)
        {
            this.Value = value;
            this.Cases = cases;
        }

        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitMatchStmt(this, data);

        public override AstStatement Clone()
            => CopyValuesTo(new AstMatchStmt(Value.Clone(), Cases.Select(c => c.Clone()).ToList()));
    }

    public class AstBreakStmt : AstStatement
    {
        public List<AstStatement> DeferredStatements { get; } = new List<AstStatement>();
        public AstWhileStmt Loop { get; set; }

        public AstBreakStmt(ILocation Location = null) : base(Location: Location)
        { }

        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitBreakStmt(this, data);

        public override AstStatement Clone()
            => CopyValuesTo(new AstBreakStmt());
    }

    public class AstContinueStmt : AstStatement
    {
        public List<AstStatement> DeferredStatements { get; } = new List<AstStatement>();
        public AstStatement Loop { get; set; }

        public AstContinueStmt(ILocation Location = null) : base(Location: Location)
        { }

        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default) => visitor.VisitContinueStmt(this, data);

        public override AstStatement Clone()
            => CopyValuesTo(new AstContinueStmt());
    }
}
