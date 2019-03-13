﻿using Cheez.Ast.Statements;
using Cheez.Types;
using Cheez.Types.Primitive;
using System;

namespace Cheez
{
    public interface IOperator
    {
        CheezType LhsType { get; }
        CheezType RhsType { get; }
        CheezType ResultType { get; }
        string Name { get; }

        int Accepts(CheezType lhs, CheezType rhs);

        object Execute(object left, object right);
    }

    public interface IUnaryOperator
    {
        CheezType SubExprType { get; }
        CheezType ResultType { get; }
        string Name { get; }

        int Accepts(CheezType sub);

        object Execute(object value);
    }

    public class BuiltInPointerOperator : IOperator
    {
        public CheezType LhsType => throw new NotImplementedException();
        public CheezType RhsType => throw new NotImplementedException();
        public CheezType ResultType { get; private set; }

        public string Name { get; private set; }

        public BuiltInPointerOperator(string name)
        {
            this.Name = name;
            switch (name)
            {
                case "==": ResultType = CheezType.Bool; break;
                case "!=": ResultType = CheezType.Bool; break;

                default: ResultType = PointerType.GetPointerType(CheezType.Any); break;
            }
        }

        public int Accepts(CheezType lhs, CheezType rhs)
        {
            if (lhs is PointerType lt && rhs is PointerType rt && lt == rt)
                return 2;
            return -1;
        }

        public object Execute(object left, object right)
        {
            throw new NotImplementedException();
        }
    }

    public class BuiltInOperator : IOperator
    {
        public CheezType LhsType { get; private set; }
        public CheezType RhsType { get; private set; }
        public CheezType ResultType { get; private set; }

        public string Name { get; private set; }

        public delegate object ComptimeExecution(object left, object right);
        public ComptimeExecution Execution { get; set; }

        public BuiltInOperator(string name, CheezType resType, CheezType lhs, CheezType rhs, ComptimeExecution exe = null)
        {
            Name = name;
            ResultType = resType;
            LhsType = lhs;
            RhsType = rhs;
            Execution = exe;
        }

        public int Accepts(CheezType lhs, CheezType rhs)
        {
            var ml = LhsType.Match(lhs);
            var mr = RhsType.Match(rhs);
            if (ml == -1 || mr == -1)
                return -1;

            return ml + mr;
        }

        private bool CheckType(CheezType needed, CheezType got)
        {
            if (needed == got)
                return true;

            //if (got == IntType.LiteralType)
            //{
            //    return needed is IntType || needed is FloatType;
            //}
            //if (got == FloatType.LiteralType)
            //{
            //    return needed is FloatType;
            //}

            return false;
        }

        public override string ToString()
        {
            return $"({ResultType}) {LhsType} {Name} {RhsType}";
        }

        public object Execute(object left, object right)
        {
            return Execution?.Invoke(left, right);
        }
    }

    public class BuiltInUnaryOperator : IUnaryOperator
    {
        public CheezType SubExprType { get; private set; }
        public CheezType ResultType { get; private set; }

        public string Name { get; private set; }

        public delegate object ComptimeExecution(object value);
        public ComptimeExecution Execution { get; set; }

        public BuiltInUnaryOperator(string name, CheezType resType, CheezType sub, ComptimeExecution exe = null)
        {
            Name = name;
            ResultType = resType;
            SubExprType = sub;
            this.Execution = exe;
        }

        public override string ToString()
        {
            return $"({ResultType}) {Name} {SubExprType}";
        }

        public int Accepts(CheezType sub)
        {
            throw new System.NotImplementedException();
        }

        public object Execute(object value)
        {
            return Execution?.Invoke(value);
        }
    }

    public class UserDefinedBinaryOperator : IOperator
    {
        public CheezType LhsType { get; set; }

        public CheezType RhsType { get; set; }

        public CheezType ResultType { get; set; }

        public string Name { get; set; }

        public AstFunctionDecl Declaration { get; set; }

        public UserDefinedBinaryOperator(string name, AstFunctionDecl func)
        {
            this.Name = name;
            this.LhsType = func.Parameters[0].Type;
            this.RhsType = func.Parameters[1].Type;
            this.ResultType = func.ReturnValue?.Type;
            this.Declaration = func;
        }

        public int Accepts(CheezType lhs, CheezType rhs)
        {
            // TODO: poly functions
            var ml = LhsType.Match(lhs);
            var mr = RhsType.Match(rhs);
            if (ml == -1 || mr == -1)
                return -1;

            return ml + mr;
        }

        public object Execute(object left, object right)
        {
            throw new NotImplementedException();
        }
    }
}
