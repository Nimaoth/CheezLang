﻿using Cheez.Ast.Expressions.Types;
using Cheez.Ast.Statements;
using System.Collections.Generic;
using System.Linq;

namespace Cheez.Types.Abstract
{
    public abstract class AbstractType : CheezType {
        public override bool IsErrorType => false;
    }

    public class VarDeclType : AbstractType
    {
        public override bool IsPolyType => false;
        public AstSingleVariableDecl Declaration { get; }

        public VarDeclType(AstSingleVariableDecl decl)
        {
            Declaration = decl;
        }

        public override string ToString() => $"<var decl> {Declaration.Name.Name}";
    }

    public class CombiType : AbstractType
    {
        public override bool IsPolyType => false;
        public List<AbstractType> SubTypes { get; }

        public CombiType(List<AbstractType> decls)
        {
            SubTypes = decls;
        }

        public override string ToString() => $"<decls> ({string.Join(", ", SubTypes)})";
    }

    public class AliasType : AbstractType
    {
        public override bool IsPolyType => false;
        public AstTypeAliasDecl Declaration { get; }

        public AliasType(AstTypeAliasDecl decl)
        {
            Declaration = decl;
        }

        public override string ToString() => $"<type alias> {Declaration.Name.Name}";
    }

    public class GenericFunctionType : CheezType
    {
        public AstFunctionDecl Declaration { get; }
        public override bool IsPolyType => false;
        public override bool IsErrorType => false;

        public GenericFunctionType(AstFunctionDecl decl)
        {
            Declaration = decl;
        }
    }

    public class GenericStructType : CheezType
    {
        public AstStructDecl Declaration { get; }
        public override bool IsPolyType => false;
        public override bool IsErrorType => false;

        public GenericStructType(AstStructDecl decl)
        {
            Declaration = decl;
        }
    }

    public class GenericTraitType : CheezType
    {
        public AstTraitDeclaration Declaration { get; }
        public override bool IsPolyType => false;
        public override bool IsErrorType => false;

        public GenericTraitType(AstTraitDeclaration decl)
        {
            Declaration = decl;
        }
    }

    public class ErrorType : CheezType
    {
        public static ErrorType Instance { get; } = new ErrorType { Size = 0 };
        public override bool IsPolyType => false;
        public override string ToString() => "<Error Type>";
        public override bool IsErrorType => true;
    }

    public class PolyType : CheezType
    {
        public string Name { get; }
        public override bool IsPolyType => true;
        public override bool IsErrorType => false;

        /// <summary>
        /// Wether or not the symbol with this type has declared this poly type with $ or not
        /// </summary>
        public bool IsDeclaring = false;

        public PolyType(string name, bool is_declaring)
        {
            this.Name = name;
            IsDeclaring = is_declaring;
        }

        public override string ToString() => IsDeclaring ? "$" + Name : Name;
    }
}
