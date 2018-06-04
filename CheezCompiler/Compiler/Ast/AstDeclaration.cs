﻿using Cheez.Compiler.Visitor;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Cheez.Compiler.Ast
{
    public class AstParameter : ISymbol
    {
        public ParseTree.PTParameter ParseTreeNode { get; }

        public AstIdentifierExpr Name { get; }
        public CheezType Type { get; set; }
        public AstExpression TypeExpr { get; set; }
        public Scope Scope { get; set; }

        public object Value { get; set; }

        public bool IsConstant => true;

        public AstParameter(ParseTree.PTParameter node, AstIdentifierExpr name, AstExpression typeExpr)
        {
            ParseTreeNode = node;
            Name = name;
            this.TypeExpr = typeExpr;
        }

        public override string ToString()
        {
            return $"{Name}: {TypeExpr}";
        }

        public AstParameter Clone()
        {
            return new AstParameter(ParseTreeNode, Name?.Clone() as AstIdentifierExpr, TypeExpr.Clone())
            {
                Scope = this.Scope,
                Type = this.Type
            };
        }
    }

    #region Function Declaration

    public class AstFunctionParameter : ISymbol
    {
        public ParseTree.PTFunctionParam ParseTreeNode { get; }

        public AstIdentifierExpr Name { get; }
        public CheezType Type { get; set; }
        public AstExpression TypeExpr { get; set; }
        public Scope Scope { get; set; }

        public bool IsConstant => false;

        public AstFunctionParameter(ParseTree.PTFunctionParam node, AstIdentifierExpr name, AstExpression typeExpr)
        {
            ParseTreeNode = node;
            Name = name;
            this.TypeExpr = typeExpr;
        }

        public AstFunctionParameter(AstIdentifierExpr name, AstExpression typeExpr)
        {
            Name = name;
            this.TypeExpr = typeExpr;
        }

        public override string ToString()
        {
            return $"{Name}: {TypeExpr}";
        }

        public AstFunctionParameter Clone()
        {
            return new AstFunctionParameter(ParseTreeNode, Name.Clone() as AstIdentifierExpr, TypeExpr.Clone())
            {
                Scope = this.Scope,
                Type = this.Type
            };
        }
    }

    public interface ITempVariable
    {
        AstIdentifierExpr Name { get; }
        CheezType Type { get; }
    }

    public class AstFunctionDecl : AstStatement, ISymbol
    {
        public override ParseTree.PTStatement GenericParseTreeNode { get; }

        public Scope HeaderScope { get; set; }
        public Scope SubScope { get; set; }

        public AstIdentifierExpr Name { get; set; }
        public List<AstFunctionParameter> Parameters { get; }
        public AstExpression ReturnTypeExpr { get; set; }
        public CheezType ReturnType { get; set; }

        //public List<AstIdentifierExpr> Generics { get; }

        public CheezType Type { get; set; }

        public AstBlockStmt Body { get; private set; }

        public List<ITempVariable> LocalVariables { get; } = new List<ITempVariable>();

        public List<AstFunctionDecl> PolymorphicInstances { get; } = new List<AstFunctionDecl>();

        public bool RefSelf { get; }
        public bool IsGeneric { get; set; }

        public bool IsConstant => true;

        public bool IsPolyInstance { get; set; } = false;
        public Dictionary<string, AstExpression> PolymorphicTypeExprs { get; internal set; }
        public Dictionary<string, CheezType> PolymorphicTypes { get; internal set; }

        public CheezType ImplTarget { get; set; }

        public AstFunctionDecl(ParseTree.PTStatement node,
            AstIdentifierExpr name,
            List<AstIdentifierExpr> generics,
            List<AstFunctionParameter> parameters,
            AstExpression returnTypeExpr,
            AstBlockStmt body = null, 
            Dictionary<string, AstDirective> directives = null, 
            bool refSelf = false)
            : base(directives)
        {
            GenericParseTreeNode = node;
            this.Name = name;
            this.Parameters = parameters;
            this.ReturnTypeExpr = returnTypeExpr;
            this.Body = body;
            this.RefSelf = refSelf;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D))
        {
            return visitor.VisitFunctionDeclaration(this, data);
        }

        public override string ToString()
        {
            var p = string.Join(", ", Parameters.Select(x => x.ToString()));
            if (ReturnTypeExpr != null)
                return $"fn {Name}({p}) -> {ReturnTypeExpr}";
            return $"fn {Name}({p})";
        }

        public override AstStatement Clone()
        {
            return new AstFunctionDecl(GenericParseTreeNode,
                Name.Clone() as AstIdentifierExpr,
                null,
                Parameters.Select(p => p.Clone()).ToList(),
                ReturnTypeExpr?.Clone(),
                Body?.Clone() as AstBlockStmt,
                Directives) // @Tode: clone this too?
            {
                Scope = this.Scope,
                HeaderScope = null,
                SubScope = null,
                Directives = this.Directives,
                mFlags = this.mFlags,
                ReturnType = this.ReturnType
            };
        }
    }

    #endregion

    #region Type Declaration

    public class AstMemberDecl
    {
        public ParseTree.PTMemberDecl ParseTreeNode { get; set; }

        public AstIdentifierExpr Name { get; }
        public AstExpression TypeExpr { get; set; }
        public CheezType Type { get; set; }

        public AstMemberDecl(ParseTree.PTMemberDecl node, AstIdentifierExpr name, AstExpression typeExpr)
        {
            ParseTreeNode = node;
            this.Name = name;
            this.TypeExpr = typeExpr;
        }

        public AstMemberDecl Clone()
        {
            return new AstMemberDecl(ParseTreeNode, Name.Clone() as AstIdentifierExpr, TypeExpr.Clone())
            {
                Type = this.Type
            };
        }
    }

    public class AstStructDecl : AstStatement, ISymbol
    {
        public ParseTree.PTStructDecl ParseTreeNode { get; set; }
        public override ParseTree.PTStatement GenericParseTreeNode => ParseTreeNode;

        public AstIdentifierExpr Name { get; set; }
        public List<AstMemberDecl> Members { get; }
        public List<AstParameter> Parameters { get; set; }

        public CheezType Type { get; set; }

        public Scope SubScope { get; set; }

        public bool IsPolymorphic { get; set; }
        public bool IsPolyInstance { get; set; }

        public bool IsConstant => true;

        public List<AstStructDecl> PolymorphicInstances { get; } = new List<AstStructDecl>();

        public AstStructDecl(ParseTree.PTStructDecl node, AstIdentifierExpr name, List<AstParameter> param, List<AstMemberDecl> members, Dictionary<string, AstDirective> dirs) : base(dirs)
        {
            ParseTreeNode = node;
            this.Name = name;
            this.Parameters = param;
            this.Members = members;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitStructDeclaration(this, data);
        }

        public override AstStatement Clone()
        {
            return new AstStructDecl(ParseTreeNode,
                Name.Clone() as AstIdentifierExpr,
                Parameters?.Select(p => p.Clone()).ToList(),
                Members.Select(m => m.Clone()).ToList(),
                Directives) // @Tode: clone this?
            {
                Scope = this.Scope,
                Directives = this.Directives,
                mFlags = this.mFlags
            };
        }
    }

    public class AstImplBlock : AstStatement
    {
        public ParseTree.PTImplBlock ParseTreeNode { get; }
        public override ParseTree.PTStatement GenericParseTreeNode => ParseTreeNode;

        public CheezType TargetType { get; set; }
        public AstExpression TargetTypeExpr { get; set; }
        public string Trait { get; set; }

        public List<AstFunctionDecl> Functions { get; }

        public Scope SubScope { get; set; }

        public AstImplBlock(ParseTree.PTImplBlock node, AstExpression targetTypeExpr, List<AstFunctionDecl> functions) : base()
        {
            ParseTreeNode = node;
            this.TargetTypeExpr = targetTypeExpr;
            this.Functions = functions;
        }

        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitImplBlock(this, data);
        }

        public override AstStatement Clone()
        {
            return new AstImplBlock(ParseTreeNode, TargetTypeExpr.Clone(), Functions.Select(f => f.Clone() as AstFunctionDecl).ToList())
            {
                Scope = this.Scope,
                Directives = this.Directives,
                mFlags = this.mFlags
            };
        }
    }

    #endregion

    #region Variable Declarion

    public class AstVariableDecl : AstStatement, ISymbol, ITempVariable
    {
        public override ParseTree.PTStatement GenericParseTreeNode { get; }

        public AstIdentifierExpr Name { get; set; }
        public CheezType Type { get; set; }
        public AstExpression TypeExpr { get; set; }
        public AstExpression Initializer { get; set; }
        public Scope SubScope { get; set; }

        public bool IsConstant { get; set; } = false;

        public AstVariableDecl(ParseTree.PTStatement node, AstIdentifierExpr name, AstExpression typeExpr, AstExpression init, Dictionary<string, AstDirective> directives = null) : base(directives)
        {
            GenericParseTreeNode = node;
            this.Name = name;
            this.TypeExpr = typeExpr;
            this.Initializer = init;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D))
        {
            return visitor.VisitVariableDeclaration(this, data);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"let {Name}");

            if (TypeExpr != null)
                sb.Append($": {TypeExpr}");

            if (Initializer != null)
                sb.Append($" = {Initializer}");

            return sb.ToString();
        }

        public override AstStatement Clone()
        {
            return new AstVariableDecl(GenericParseTreeNode,
                Name.Clone() as AstIdentifierExpr,
                TypeExpr?.Clone(),
                Initializer?.Clone(),
                Directives) // @Tode: clone this?
            {
                Scope = this.Scope,
                Directives = this.Directives,
                mFlags = this.mFlags
            };
        }
    }

    #endregion

    #region Enum

    public class AstEnumMember
    {
        public ParseTree.PTEnumMember ParseTreeNode { get; set; }

        public string Name { get; }
        public AstExpression Value { get; }
        public CheezType Type { get; set; }

        public AstEnumMember(ParseTree.PTEnumMember node, string name, AstExpression value)
        {
            ParseTreeNode = node;
            this.Name = name;
            this.Value = value;
        }

        public AstEnumMember Clone()
        {
            return new AstEnumMember(ParseTreeNode, Name, Value?.Clone())
            {
                Type = this.Type
            };
        }
    }

    public class AstEnumDecl : AstStatement, INamed
    {
        public ParseTree.PTEnumDecl ParseTreeNode { get; set; }
        public override ParseTree.PTStatement GenericParseTreeNode => ParseTreeNode;

        public AstIdentifierExpr Name { get; }
        public List<AstEnumMember> Members { get; }

        public AstEnumDecl(ParseTree.PTEnumDecl node, AstIdentifierExpr name, List<AstEnumMember> members, Dictionary<string, AstDirective> dirs) : base(dirs)
        {
            ParseTreeNode = node;
            this.Name = name;
            this.Members = members;
        }

        [DebuggerStepThrough]
        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitEnumDeclaration(this, data);
        }

        public override AstStatement Clone()
        {
            return new AstEnumDecl(ParseTreeNode, 
                Name.Clone() as AstIdentifierExpr,
                Members.Select(m => m.Clone()).ToList(),
                Directives) // @Tode: clone this?
            {
                Scope = this.Scope,
                Directives = this.Directives,
                mFlags = this.mFlags
            };
        }
    }

    #endregion

    #region Type Alias

    public class AstTypeAliasDecl : AstStatement
    {
        public override ParseTree.PTStatement GenericParseTreeNode { get; }

        public AstIdentifierExpr Name { get; set; }
        public AstExpression TypeExpr { get; set; }
        public CheezType Type { get; set; }

        public AstTypeAliasDecl(ParseTree.PTStatement node, AstIdentifierExpr name, AstExpression typeExpr, Dictionary<string, AstDirective> dirs = null) : base(dirs)
        {
            this.GenericParseTreeNode = node;
            this.Name = name;
        }

        public override T Accept<T, D>(IVisitor<T, D> visitor, D data = default)
        {
            return visitor.VisitTypeAlias(this, data);
        }

        public override AstStatement Clone()
        {
            return new AstTypeAliasDecl(GenericParseTreeNode, 
                Name.Clone() as AstIdentifierExpr,
                TypeExpr.Clone(),
                Directives) // @Tode: clone this?
            {
                Scope = this.Scope,
                Directives = this.Directives,
                mFlags = this.mFlags
            };
        }
    }

    #endregion
}