﻿using Cheez.Ast.Expressions;
using Cheez.Ast.Statements;
using Cheez.Extras;
using Cheez.Types;
using Cheez.Types.Complex;
using Cheez.Types.Primitive;
using Cheez.Util;
using LLVMSharp;
using System;
using System.Linq;

namespace Cheez.CodeGeneration.LLVMCodeGen
{
    public partial class LLVMCodeGenerator
    {

        private LLVMValueRef? GenerateExpression(LLVMValueRef ptr, AstExpression expr, LLVMValueRef? target, bool deref)
        {
            var v = GenerateExpression(expr, target, true);
            if (v != null)
                builder.CreateStore(v.Value, ptr);

            return target ?? v;
        }

        private LLVMValueRef? GenerateExpression(AstExpression expr, LLVMValueRef? target, bool deref)
        {
            switch (expr)
            {
                case AstArgument a: return GenerateExpression(a.Expr, target, deref);
                case AstNumberExpr n: return GenerateNumberExpr(n);
                case AstBoolExpr n: return GenerateBoolExpr(n);
                case AstIdExpr i: return GenerateIdExpr(i, target, deref);
                case AstCharLiteral ch: return GenerateCharLiteralExpr(ch);
                case AstStringLiteral ch: return GenerateStringLiteralExpr(ch);
                case AstTupleExpr t: return GenerateTupleExpr(t, target, deref);
                case AstDotExpr t: return GenerateDotExpr(t, target, deref);
                case AstArrayAccessExpr t: return GenerateIndexExpr(t, target, deref);
                case AstStructValueExpr t: return GenerateStructValueExpr(t, target, deref);
                case AstCallExpr c: return GenerateCallExpr(c, target, deref);
                case AstUnaryExpr u: return GenerateUnaryExpr(u, target, deref);
                case AstTempVarExpr t: return GenerateTempVarExpr(t, target, deref);
                case AstSymbolExpr s: return GenerateSymbolExpr(s, target, deref);
                case AstBlockExpr block: return GenerateBlock(block, target, deref);
            }
            throw new NotImplementedException();
        }

        private LLVMValueRef? GenerateBlock(AstBlockExpr block, LLVMValueRef? target, bool deref)
        {
            LLVMValueRef? result = null;

            int end = block.Statements.Count;
            if (block.Statements.LastOrDefault() is AstExprStmt) --end;

            for (int i = 0; i < end; i++)
            {
                GenerateStatement(block.Statements[i]);
            }

            if (block.Statements.LastOrDefault() is AstExprStmt expr)
            {
                result = GenerateExpression(expr.Expr, target, deref);
            }

            for (int i = block.DeferredStatements.Count - 1; i >= 0; i--)
            {
                GenerateStatement(block.DeferredStatements[i]);
            }

            return result;
        }

        private LLVMValueRef? GenerateSymbolExpr(AstSymbolExpr s, LLVMValueRef? target, bool deref)
        {
            var v = valueMap[s.Symbol];

            if (deref)
            {
                v = builder.CreateLoad(v, "");
            }

            if (target != null)
            {
                builder.CreateStore(v, target.Value);
                return null;
            }

            return v;
        }

        private LLVMValueRef? GenerateTempVarExpr(AstTempVarExpr t, LLVMValueRef? target, bool deref)
        {
            if (!valueMap.ContainsKey(t))
            {
                var x = CreateLocalVariable(t.Type);
                valueMap[t] = x;
                GenerateExpression(x, t.Expr, x, true);
            }

            var tmp = valueMap[t];

            if (deref)
            {
                tmp = builder.CreateLoad(tmp, "");
            }

            if (target != null)
            {
                builder.CreateStore(tmp, target.Value);
                return null;
            }

            return tmp;
        }

        private LLVMValueRef? GenerateUnaryExpr(AstUnaryExpr expr, LLVMValueRef? target, bool deref)
        {
            if (expr.Operator == "-")
            {
                var val = GenerateExpression(expr.SubExpr, null, true);
                if (val == null) throw new Exception("Bug! this should not be null");
                if (expr.Type is IntType)
                {
                    return builder.CreateNeg(val.Value, "");
                }
                else if (expr.Type is FloatType)
                {
                    return builder.CreateFNeg(val.Value, "");
                }
            }
            return null;
        }

        private LLVMValueRef? GenerateCallExpr(AstCallExpr c, LLVMValueRef? target, bool deref)
        {
            LLVMValueRef? func = null;
            if (c.Declaration != null)
            {
                func = valueMap[c.Declaration];
            }
            else
            {
                func = GenerateExpression(c.Function, null, false);
            }

            // arguments
            var args = c.Arguments.Select(a => GenerateExpression(a, null, true).Value).ToArray();

            return builder.CreateCall(func.Value, args, "");
        }

        private LLVMValueRef? GenerateIndexExpr(AstArrayAccessExpr expr, LLVMValueRef? target, bool deref)
        {
            switch (expr.SubExpression.Type)
            {
                case TupleType t:
                    {
                        var index = ((NumberData)expr.Indexer.Value).ToLong();
                        var left = GenerateExpression(expr.SubExpression, null, false);

                        LLVMValueRef? result;
                        if (!expr.SubExpression.GetFlag(ExprFlags.IsLValue))
                        {
                            result = builder.CreateExtractValue(left.Value, (uint)index, "");
                        }
                        else
                        {
                            result = builder.CreateStructGEP(left.Value, (uint)index, "");
                            if (deref)
                                result = builder.CreateLoad(result.Value, "");
                        }

                        if (target != null)
                        {
                            builder.CreateStore(result.Value, target.Value);
                            return null;
                        }

                        return result;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public LLVMValueRef? GenerateStructValueExpr(AstStructValueExpr expr, LLVMValueRef? target, bool deref)
        {
            if (target != null)
            {
                foreach (var mem in expr.MemberInitializers)
                {
                    var ptr = builder.CreateStructGEP(target.Value, (uint)mem.Index, "");
                    GenerateExpression(ptr, mem.Value, target, false);
                }

                return null;
            }

            throw new NotImplementedException();
        }

        public LLVMValueRef? GenerateDotExpr(AstDotExpr expr, LLVMValueRef? target, bool deref)
        {
            switch (expr.Left.Type)
            {
                case TupleType t:
                    {
                        var index = t.Members.IndexOf(m => m.name == expr.Right.Name);
                        var left = GenerateExpression(expr.Left, null, false);

                        LLVMValueRef? result;
                        if (!expr.Left.GetFlag(ExprFlags.IsLValue))
                        {
                            result = builder.CreateExtractValue(left.Value, (uint)index, "");
                        }
                        else
                        {
                            result = builder.CreateStructGEP(left.Value, (uint)index, "");
                            if (deref)
                                result = builder.CreateLoad(result.Value, "");
                        }

                        if (target != null)
                        {
                            builder.CreateStore(result.Value, target.Value);
                            return null;
                        }

                        return result;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public LLVMValueRef? GenerateTupleExpr(AstTupleExpr expr, LLVMValueRef? maybeTarget, bool deref)
        {
            if (maybeTarget != null)
            {
                var target = maybeTarget.Value;
                for (int i = 0; i < expr.Values.Count; i++)
                {
                    var memberPtr = builder.CreateStructGEP(target, (uint)i, "");
                    var v = GenerateExpression(expr.Values[i], memberPtr, true);
                    if (v != null)
                    {
                        builder.CreateStore(v.Value, memberPtr);
                    }
                }
                return null;
            }
            else
            {
                var tempVar = CreateLocalVariable(expr.Type);
                for (int i = 0; i < expr.Values.Count; i++)
                {
                    var memberPtr = builder.CreateStructGEP(tempVar, (uint)i, "");
                    var v = GenerateExpression(expr.Values[i], memberPtr, true);
                    if (v != null)
                    {
                        builder.CreateStore(v.Value, memberPtr);
                    }
                }

                if (deref)
                    tempVar = builder.CreateLoad(tempVar, "");
                return tempVar;
            }
        }

        public LLVMValueRef GenerateCharLiteralExpr(AstCharLiteral expr)
        {
            var ch = expr.CharValue;
            var val = LLVM.ConstInt(LLVMTypeRef.Int8Type(), ch, true);
            return val;
        }

        public LLVMValueRef? GenerateStringLiteralExpr(AstStringLiteral expr)
        {
            var ch = expr.StringValue;

            if (expr.Type == CheezType.CString)
            {
                return builder.CreateGlobalStringPtr(ch, "");
            }
            else if (expr.Type == CheezType.String)
            {
                var str = builder.CreateGlobalString(ch, "");
                return LLVM.ConstNamedStruct(CheezTypeToLLVMType(expr.Type), new LLVMValueRef[]
                {
                    LLVM.ConstInt(LLVM.Int32Type(), (ulong)ch.Length, true),
                    LLVM.ConstPointerCast(str, LLVM.PointerType(LLVM.Int8Type(), 0))
                });
            }
            else
            {
                throw new NotImplementedException();
            }
            
            return null;
        }

        public LLVMValueRef VisitStringLiteralExpr(AstStringLiteral expr)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef GenerateBoolExpr(AstBoolExpr expr)
        {
            var llvmType = CheezTypeToLLVMType(expr.Type);
            return LLVM.ConstInt(llvmType, expr.BoolValue ? 1u : 0u, false);
        }

        public LLVMValueRef GenerateNumberExpr(AstNumberExpr num)
        {
            var llvmType = CheezTypeToLLVMType(num.Type);
            if (num.Type is IntType)
            {
                var val = num.Data.ToUlong();
                return LLVM.ConstInt(llvmType, val, false);
            }
            else
            {
                var val = num.Data.ToDouble();
                var result = LLVM.ConstReal(llvmType, val);
                return result;
            }
        }

        public LLVMValueRef? GenerateIdExpr(AstIdExpr expr, LLVMValueRef? maybeTarget, bool deref)
        {
            LLVMValueRef v;
            if (expr.Symbol is AstDecl decl)
            {
                v = valueMap[decl];
            }
            else if (expr.Symbol is Using u)
            {
                v = GenerateExpression(u.Expr, null, false).Value;
            }
            else
            {
                v = valueMap[expr.Symbol];
            }

            if (maybeTarget != null)
            {
                if (!(expr.Symbol is ConstSymbol)) // :hack
                    v = builder.CreateLoad(v, "");
                builder.CreateStore(v, maybeTarget.Value);
                return null;
            }

            if (deref)
                return builder.CreateLoad(v, "");
            return v;
        }

        //public override LLVMValueRef VisitStructValueExpression(AstStructValueExpr str)
        //{
        //    var value = GetTempValue(str.Type);

        //    var llvmType = CheezTypeToLLVMType(str.Type);

        //    foreach (var m in str.MemberInitializers)
        //    {
        //        var v = m.Value.Accept(this, data);
        //        var memberPtr = builder.CreateStructGEP(value, (uint)m.Index, "");
        //        var s = builder.CreateStore(v, memberPtr);
        //    }

        //    return value;
        //}
    }
}
