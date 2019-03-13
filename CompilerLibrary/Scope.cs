﻿using Cheez.Ast;
using Cheez.Ast.Expressions;
using Cheez.Ast.Statements;
using Cheez.Extras;
using Cheez.Types;
using Cheez.Types.Abstract;
using Cheez.Types.Complex;
using Cheez.Types.Primitive;
using Cheez.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cheez
{
    public interface ISymbol : INamed
    {
        //CheezType Type { get; }
        ILocation Location { get; }
    }

    public interface ITypedSymbol : ISymbol
    {
        CheezType Type { get; }
    }

    public class ConstSymbol : ITypedSymbol
    {
        private static int _id_counter = 0;

        public ILocation Location => null;
        public AstIdExpr Name { get; private set; }

        public CheezType Type { get; private set; }
        public object Value { get; private set; }
        public readonly int Id = _id_counter++;

        public ConstSymbol(string name, CheezType type, object value)
        {
            this.Name = new AstIdExpr(name, false);
            this.Type = type;
            this.Value = value;
        }
    }

    public class TypeSymbol : ITypedSymbol
    {
        public ILocation Location => null;
        public AstIdExpr Name { get; private set; }

        public CheezType Type { get; private set; }

        public TypeSymbol(string name, CheezType type)
        {
            this.Name = new AstIdExpr(name, false);
            this.Type = type;
        }
    }

    public class Using : ITypedSymbol
    {
        public CheezType Type => Expr.Type;
        public AstIdExpr Name => throw new NotImplementedException();

        public AstExpression Expr { get; }
        public bool IsConstant => true;

        public ILocation Location { get; set; }


        [DebuggerStepThrough]
        public Using(AstExpression expr)
        {
            this.Location = expr.Location;
            this.Expr = expr;
        }

        [DebuggerStepThrough]
        public override string ToString()
        {
            return $"using {Expr}";
        }
    }

    public class FunctionList : ISymbol
    {
        public CheezType Type => throw new System.NotImplementedException();

        public bool IsConstant => throw new System.NotImplementedException();

        public AstIdExpr Name => throw new System.NotImplementedException();

        public ILocation Location => throw new NotImplementedException();
    }

    public class Scope
    {
        public string Name { get; set; }

        public Scope Parent { get; }

        public List<AstFunctionDecl> FunctionDeclarations { get; } = new List<AstFunctionDecl>();
        public List<AstVariableDecl> VariableDeclarations { get; } = new List<AstVariableDecl>();
        public List<AstStatement> TypeDeclarations { get; } = new List<AstStatement>();
        public List<AstImplBlock> ImplBlocks { get; } = new List<AstImplBlock>();

        public IEnumerable<ISymbol> InitializedSymbols => mInitializedSymbols.Keys;

        //private CTypeFactory types = new CTypeFactory();

        private Dictionary<string, ISymbol> mSymbolTable = new Dictionary<string, ISymbol>();
        private Dictionary<string, List<IOperator>> mOperatorTable = new Dictionary<string, List<IOperator>>();
        private Dictionary<string, List<IUnaryOperator>> mUnaryOperatorTable = new Dictionary<string, List<IUnaryOperator>>();
        private Dictionary<AstImplBlock, List<AstFunctionDecl>> mImplTable = new Dictionary<AstImplBlock, List<AstFunctionDecl>>();
        private Dictionary<ISymbol, int> mInitializedSymbols = new Dictionary<ISymbol, int>();

        public IEnumerable<KeyValuePair<string, ISymbol>> Symbols => mSymbolTable.AsEnumerable();

        public Scope(string name, Scope parent = null)
        {
            this.Name = name;
            this.Parent = parent;
        }

        public Scope Clone()
        {
            return new Scope(Name, Parent)
            {
                mSymbolTable = new Dictionary<string, ISymbol>(mSymbolTable),
                mOperatorTable = new Dictionary<string, List<IOperator>>(mOperatorTable),
                mUnaryOperatorTable = new Dictionary<string, List<IUnaryOperator>>(mUnaryOperatorTable)
                // TODO: mImplTable?, rest?
            };
        }

        //public CheezType GetCheezType(PTTypeExpr expr)
        //{
        //    return types.GetCheezType(expr) ?? Parent?.GetCheezType(expr);
        //}

        //public CheezType GetCheezType(string name)
        //{
        //    return types.GetCheezType(name) ?? Parent?.GetCheezType(name);
        //}

        public bool IsInitialized(ISymbol symbol)
        {
            return mInitializedSymbols.ContainsKey(symbol);
        }

        public void SetInitialized(ISymbol symbol, int location = -1)
        {
            mInitializedSymbols[symbol] = location;
        }

        public List<IOperator> GetOperators(string name, CheezType lhs, CheezType rhs)
        {
            var result = new List<IOperator>();
            int level = int.MaxValue;
            GetOperator(name, lhs, rhs, result, ref level);
            return result;
        }

        private void GetOperator(string name, CheezType lhs, CheezType rhs, List<IOperator> result, ref int level)
        {
            if (!mOperatorTable.ContainsKey(name))
            {
                Parent?.GetOperator(name, lhs, rhs, result, ref level);
                return;
            }

            var ops = mOperatorTable[name];

            foreach (var op in ops)
            {
                var l = op.Accepts(lhs, rhs);
                if (l == -1)
                    continue;

                if (l < level)
                {
                    level = l;
                    result.Clear();
                    result.Add(op);
                }
                else if (l == level)
                {
                    result.Add(op);
                }
            }

            Parent?.GetOperator(name, lhs, rhs, result, ref level);
        }

        public List<IUnaryOperator> GetOperators(string name, CheezType sub)
        {
            var result = new List<IUnaryOperator>();
            int level = 0;
            GetOperator(name, sub, result, ref level);
            return result;
        }

        private void GetOperator(string name, CheezType sub, List<IUnaryOperator> result, ref int level)
        {
            if (!mUnaryOperatorTable.ContainsKey(name))
            {
                Parent?.GetOperator(name, sub, result, ref level);
                return;
            }

            var ops = mUnaryOperatorTable[name];

            foreach (var op in ops)
            {
                var l = op.Accepts(sub);
                if (l == -1)
                    continue;

                if (l < level)
                {
                    level = l;
                    result.Clear();
                    result.Add(op);
                }
                else if (l == level)
                {
                    result.Add(op);
                }
            }

            Parent?.GetOperator(name, sub, result, ref level);
        }

        public void DefineBinaryOperator(string op, AstFunctionDecl func)
        {
            DefineOperator(new UserDefinedBinaryOperator(op, func));
        }

        internal void DefineBuiltInTypes()
        {
            DefineTypeSymbol("i8", IntType.GetIntType(1, true));
            DefineTypeSymbol("i16", IntType.GetIntType(2, true));
            DefineTypeSymbol("i32", IntType.GetIntType(4, true));
            DefineTypeSymbol("i64", IntType.GetIntType(8, true));

            DefineTypeSymbol("u8", IntType.GetIntType(1, false));
            DefineTypeSymbol("u16", IntType.GetIntType(2, false));
            DefineTypeSymbol("u32", IntType.GetIntType(4, false));
            DefineTypeSymbol("u64", IntType.GetIntType(8, false));

            DefineTypeSymbol("int", IntType.GetIntType(8, true));
            DefineTypeSymbol("uint", IntType.GetIntType(8, false));

            DefineTypeSymbol("f32", FloatType.GetFloatType(4));
            DefineTypeSymbol("f64", FloatType.GetFloatType(8));
            DefineTypeSymbol("float", FloatType.GetFloatType(4));
            DefineTypeSymbol("double", FloatType.GetFloatType(8));

            DefineTypeSymbol("char", CheezType.Char);
            DefineTypeSymbol("bool", CheezType.Bool);
            DefineTypeSymbol("c_string", CheezType.CString);
            DefineTypeSymbol("string", CheezType.String);
            DefineTypeSymbol("void", CheezType.Void);
            DefineTypeSymbol("any", CheezType.Any);
            DefineTypeSymbol("type", CheezType.Type);
        }

        internal void DefineBuiltInOperators()
        {
            CheezType[] intTypes = new CheezType[]
            {
                IntType.GetIntType(1, true),
                IntType.GetIntType(2, true),
                IntType.GetIntType(4, true),
                IntType.GetIntType(8, true),
                IntType.GetIntType(1, false),
                IntType.GetIntType(2, false),
                IntType.GetIntType(4, false),
                IntType.GetIntType(8, false),
                CheezType.Char
            };
            CheezType[] floatTypes = new CheezType[]
            {
                FloatType.GetFloatType(4),
                FloatType.GetFloatType(8)
            };

            DefineArithmeticOperators(intTypes, "+", "-", "*", "/", "%");
            DefineArithmeticOperators(floatTypes, "+", "-", "*", "/", "%");

            DefineLiteralOperators();

            // 
            DefineLogicOperators(intTypes, 
                (">", null), 
                (">=", null), 
                ("<", null), 
                ("<=", null), 
                ("==", null), 
                ("!=", null));
            DefineLogicOperators(floatTypes,
                (">", null),
                (">=", null),
                ("<", null),
                ("<=", null),
                ("==", null),
                ("!=", null));

            DefineLogicOperators(new CheezType[] { BoolType.Instance }, 
                ("and", (a, b) => (bool)a && (bool)b), 
                ("or", (a, b) => (bool)a || (bool)b),
                ("==", (a, b) => (bool)a == (bool)b),
                ("!=", (a, b) => (bool)a != (bool)b));

            //
            DefineArithmeticUnaryOperators(intTypes, "-", "+");
            DefineArithmeticUnaryOperators(floatTypes, "-", "+");

            //DefineBuiltIn
            DefinePointerOperators();
        }

        private void DefineUnaryOperator(string name, CheezType type, BuiltInUnaryOperator.ComptimeExecution exe)
        {
            List<IUnaryOperator> list = null;
            if (mUnaryOperatorTable.ContainsKey(name))
                list = mUnaryOperatorTable[name];
            else
            {
                list = new List<IUnaryOperator>();
                mUnaryOperatorTable[name] = list;
            }

            list.Add(new BuiltInUnaryOperator(name, type, type, exe));
        }

        private void DefineLogicOperators(CheezType[] types, params (string name, BuiltInOperator.ComptimeExecution exe)[] ops)
        {
            foreach (var op in ops)
            {
                List<IOperator> list = null;
                if (mOperatorTable.ContainsKey(op.name))
                    list = mOperatorTable[op.name];
                else
                {
                    list = new List<IOperator>();
                    mOperatorTable[op.name] = list;
                }

                foreach (var t in types)
                {
                    list.Add(new BuiltInOperator(op.name, BoolType.Instance, t, t, op.exe));
                }
            }
        }

        private void DefinePointerOperators()
        {
            foreach (var op in new string[] { "==", "!=" })
            {
                List<IOperator> list = null;
                if (mOperatorTable.ContainsKey(op))
                    list = mOperatorTable[op];
                else
                {
                    list = new List<IOperator>();
                    mOperatorTable[op] = list;
                }
                
                list.Add(new BuiltInPointerOperator(op));
            }
        }

        private void DefineLiteralOperators()
        {
            DefineUnaryOperator("!", CheezType.Bool, b => !(bool)b);
            DefineUnaryOperator("-", IntType.LiteralType, a => ((NumberData)a).Negate());
            DefineUnaryOperator("-", FloatType.LiteralType, a => ((NumberData)a).Negate());

            DefineOperator(new BuiltInOperator("+", IntType.LiteralType, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a + (NumberData)b));
            DefineOperator(new BuiltInOperator("-", IntType.LiteralType, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a - (NumberData)b));
            DefineOperator(new BuiltInOperator("*", IntType.LiteralType, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a * (NumberData)b));
            DefineOperator(new BuiltInOperator("/", IntType.LiteralType, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a / (NumberData)b));
            DefineOperator(new BuiltInOperator("%", IntType.LiteralType, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a % (NumberData)b));

            DefineOperator(new BuiltInOperator("==", CheezType.Bool, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a == (NumberData)b));
            DefineOperator(new BuiltInOperator("!=", CheezType.Bool, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a != (NumberData)b));
            DefineOperator(new BuiltInOperator("<", CheezType.Bool, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a < (NumberData)b));
            DefineOperator(new BuiltInOperator("<=", CheezType.Bool, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a <= (NumberData)b));
            DefineOperator(new BuiltInOperator(">", CheezType.Bool, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a > (NumberData)b));
            DefineOperator(new BuiltInOperator(">=", CheezType.Bool, IntType.LiteralType, IntType.LiteralType, (a, b) => (NumberData)a >= (NumberData)b));


            DefineOperator(new BuiltInOperator("+", FloatType.LiteralType, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a + (NumberData)b));
            DefineOperator(new BuiltInOperator("-", FloatType.LiteralType, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a - (NumberData)b));
            DefineOperator(new BuiltInOperator("*", FloatType.LiteralType, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a * (NumberData)b));
            DefineOperator(new BuiltInOperator("/", FloatType.LiteralType, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a / (NumberData)b));
            DefineOperator(new BuiltInOperator("%", FloatType.LiteralType, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a % (NumberData)b));

            DefineOperator(new BuiltInOperator("==", CheezType.Bool, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a == (NumberData)b));
            DefineOperator(new BuiltInOperator("!=", CheezType.Bool, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a != (NumberData)b));
            DefineOperator(new BuiltInOperator("<", CheezType.Bool, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a < (NumberData)b));
            DefineOperator(new BuiltInOperator("<=", CheezType.Bool, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a <= (NumberData)b));
            DefineOperator(new BuiltInOperator(">", CheezType.Bool, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a > (NumberData)b));
            DefineOperator(new BuiltInOperator(">=", CheezType.Bool, FloatType.LiteralType, FloatType.LiteralType, (a, b) => (NumberData)a >= (NumberData)b));
        }

        private void DefineArithmeticOperators(CheezType[] types, params string[] ops)
        {
            foreach (var name in ops)
            {
                List<IOperator> list = null;
                if (mOperatorTable.ContainsKey(name))
                    list = mOperatorTable[name];
                else
                {
                    list = new List<IOperator>();
                    mOperatorTable[name] = list;
                }

                foreach (var t in types)
                {
                    list.Add(new BuiltInOperator(name, t, t, t));
                }
            }
        }

        private void DefineArithmeticUnaryOperators(CheezType[] types, params string[] ops)
        {
            foreach (var name in ops)
            {
                List<IUnaryOperator> list = null;
                if (mUnaryOperatorTable.ContainsKey(name))
                    list = mUnaryOperatorTable[name];
                else
                {
                    list = new List<IUnaryOperator>();
                    mUnaryOperatorTable[name] = list;
                }

                foreach (var t in types)
                {
                    list.Add(new BuiltInUnaryOperator(name, t, t));
                }
            }
        }

        private void DefineOperator(IOperator op)
        {
            List<IOperator> list = null;
            if (mOperatorTable.ContainsKey(op.Name))
                list = mOperatorTable[op.Name];
            else
            {
                list = new List<IOperator>();
                mOperatorTable[op.Name] = list;
            }

            list.Add(op);
        }

        private bool CheckType(CheezType needed, CheezType got)
        {
            if (needed == got)
                return true;

            if (got == IntType.LiteralType)
            {
                return needed is IntType || needed is FloatType;
            }
            if (got == FloatType.LiteralType)
            {
                return needed is FloatType;
            }

            return false;
        }

        public (bool ok, ILocation other) DefineSymbol(ISymbol symbol, string name = null)
        {
            name = name ?? symbol.Name.Name;
            if (mSymbolTable.TryGetValue(name, out var other))
                return (false, other.Location);

            mSymbolTable[name] = symbol;
            return (true, null);
        }

        public (bool ok, ILocation other) DefineUse(string name, AstExpression expr, out Using use)
        {
            use = null;
            if (mSymbolTable.TryGetValue(name, out var other))
                return (false, other.Location);

            use = new Using(expr);
            mSymbolTable[name] = use;
            return (true, null);
        }

        public (bool ok, ILocation other) DefineConstant(string name, CheezType type, object value)
        {
            if (mSymbolTable.TryGetValue(name, out var other))
                return (false, other.Location);

            mSymbolTable[name] = new ConstSymbol(name, type, value);
            return (true, null);
        }

        public bool DefineTypeSymbol(string name, CheezType symbol)
        {
            if (mSymbolTable.ContainsKey(name))
                return false;

            mSymbolTable[name] = new TypeSymbol(name, symbol);
            return true;
        }

        public (bool ok, ILocation other) DefineDeclaration(AstDecl decl)
        {
            string name = decl.Name.Name;
            if (mSymbolTable.TryGetValue(name, out var other))
                return (false, other.Location);

            mSymbolTable[name] = decl;
            return (true, null);
        }

        public ISymbol GetSymbol(string name)
        {
            if (mSymbolTable.ContainsKey(name))
            {
                var v = mSymbolTable[name];
                return v;
            }
            return Parent?.GetSymbol(name);
        }

        public bool DefineImplFunction(AstFunctionDecl f)
        {
            if (!mImplTable.TryGetValue(f.ImplBlock, out var list))
            {
                list = new List<AstFunctionDecl>();
                mImplTable[f.ImplBlock] = list;
            }

            if (list.Any(ff => ff.Name == f.Name))
                return false;

            list.Add(f);

            return true;
        }

        private bool TypesMatch(CheezType a, CheezType b)
        {
            if (a == b)
                return true;

            if (Utilities.Xor(a is PolyType, b is PolyType))
                return true;

            if (a is StructType sa && b is StructType sb)
            {
                if (sa.Declaration.Name.Name != sb.Declaration.Name.Name)
                    return false;
                if (sa.Arguments.Length != sb.Arguments.Length)
                    return false;
                for (int i = 0; i < sa.Arguments.Length; i++)
                {
                    if (!TypesMatch(sa.Arguments[i], sb.Arguments[i]))
                        return false;
                }

                return true;
            }

            return false;
        }

        public AstFunctionDecl GetImplFunction(CheezType targetType, string name)
        {
            var impls = mImplTable.Where(kv =>
            {
                var implType = kv.Key.TargetType;
                if (TypesMatch(implType, targetType))
                    return true;
                return false;
            });

            var candidates = new List<AstFunctionDecl>();

            foreach (var impl in impls)
            {
                var list = impl.Value;
                
                var c = list?.FirstOrDefault(f => f.Name.Name == name);
                if (c != null)
                    candidates.Add(c);

            }

            if (candidates.Count == 0)
                return Parent?.GetImplFunction(targetType, name);

            return candidates[0];
        }

        public AstFunctionDecl GetImplFunctionWithDirective(CheezType targetType, string attribute)
        {
            var impls = mImplTable.Where(kv =>
            {
                var implType = kv.Key.TargetType;
                if (TypesMatch(implType, targetType))
                    return true;
                return false;
            });

            var candidates = new List<AstFunctionDecl>();

            foreach (var impl in impls)
            {
                var list = impl.Value;

                var c = list?.FirstOrDefault(f => f.HasDirective(attribute));
                if (c != null)
                    candidates.Add(c);

            }

            if (candidates.Count == 0)
                return Parent?.GetImplFunctionWithDirective(targetType, attribute);

            return candidates[0];
        }

        public override string ToString()
        {
            if (Parent != null)
                return $"{Parent}::{Name}";
            return $"::{Name}";
        }
    }
}
