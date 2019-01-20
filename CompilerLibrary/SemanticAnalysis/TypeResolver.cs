﻿using Cheez.Ast.Expressions;
using Cheez.Ast.Expressions.Types;
using Cheez.Ast.Statements;
using Cheez.Extras;
using Cheez.Types;
using Cheez.Types.Abstract;
using Cheez.Types.Complex;
using Cheez.Types.Primitive;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cheez
{
    public partial class Workspace
    {
        private CheezType ResolveType(AstTypeExpr typeExpr, bool poly_from_scope = false)
        {
            List<AstStructDecl> newInstances = new List<AstStructDecl>();
            var t = ResolveTypeHelper(typeExpr, null, newInstances, poly_from_scope);
            ResolveStructs(newInstances);
            return t;
        }

        private CheezType ResolveTypeHelper(AstTypeExpr typeExpr, HashSet<AstDecl> deps = null, List<AstStructDecl> instances = null, bool poly_from_scope = false)
        {
            switch (typeExpr)
            {
                case AstErrorTypeExpr _:
                    return CheezType.Error;

                case AstIdTypeExpr i:
                    {
                        if (i.IsPolymorphic && !poly_from_scope)
                            return new PolyType(i.Name, true);

                        var sym = typeExpr.Scope.GetSymbol(i.Name);

                        if (sym == null)
                        {
                            ReportError(typeExpr, $"Unknown symbol");
                            return CheezType.Error;
                        }

                        if (sym is CompTimeVariable c && c.Type == CheezType.Type)
                        {
                            var type = c.Value as CheezType;
                            if (deps != null && c.Declaration != null && type is AbstractType)
                                deps.Add(c.Declaration);
                            return type;
                        }
                        else
                        {
                            ReportError(typeExpr, $"'{typeExpr}' is not a valid type");
                        }

                        break;
                    }

                case AstPointerTypeExpr p:
                    {
                        p.Target.Scope = typeExpr.Scope;
                        var subType = ResolveTypeHelper(p.Target, deps, instances);
                        return PointerType.GetPointerType(subType);
                    }

                case AstSliceTypeExpr a:
                    {
                        a.Target.Scope = typeExpr.Scope;
                        var subType = ResolveTypeHelper(a.Target, deps, instances);
                        return SliceType.GetSliceType(subType);
                    }

                case AstArrayTypeExpr arr:
                    {
                        arr.Target.Scope = typeExpr.Scope;
                        var subType = ResolveTypeHelper(arr.Target, deps, instances);

                        if (arr.SizeExpr is AstNumberExpr num && num.Data.Type == NumberData.NumberType.Int)
                        {
                            int v = (int)num.Data.IntValue;
                            // TODO: check size of num.Data.IntValue
                            return ArrayType.GetArrayType(subType, v);
                        }
                        ReportError(arr.SizeExpr, "Index must be a constant int");
                        return CheezType.Error;
                    }

                case AstFunctionTypeExpr func:
                    {
                        (string name, CheezType type)[] par = new (string, CheezType)[func.ParameterTypes.Count];
                        for (int i = 0; i < par.Length; i++) {
                            func.ParameterTypes[i].Scope = func.Scope;
                            par[i].type = ResolveTypeHelper(func.ParameterTypes[i], deps, instances);
                        }

                        CheezType ret = CheezType.Void;

                        if (func.ReturnType != null)
                        {
                            func.ReturnType.Scope = func.Scope;
                            ret = ResolveTypeHelper(func.ReturnType, deps, instances);
                        }

                        return new FunctionType(par, ret);
                    }

                case AstPolyStructTypeExpr @struct:
                    {
                        @struct.Struct.Scope = @struct.Scope;
                        @struct.Struct.Type = CheezType.Type;
                        @struct.Struct.Value = ResolveTypeHelper(@struct.Struct, deps, instances);

                        foreach (var arg in @struct.Arguments)
                        {
                            arg.Scope = @struct.Scope;
                            arg.Type = CheezType.Type;
                            arg.Value = ResolveTypeHelper(arg, deps, instances);
                        }

                        // instantiate struct
                        var instance = InstantiatePolyStruct(@struct, instances);
                        return instance?.Type ?? CheezType.Error;
                    }

                case AstTupleTypeExpr tuple:
                    {
                        var members = new(string name, CheezType type)[tuple.Members.Count];
                        for (int i = 0; i < members.Length; i++)
                        {
                            var m = tuple.Members[i];
                            m.Scope = tuple.Scope;
                            m.TypeExpr.Scope = tuple.Scope;
                            m.Type = ResolveTypeHelper(m.TypeExpr, deps, instances);

                            members[i] = (m.Name?.Name, m.Type);
                        }

                        return TupleType.GetTuple(members);
                    }
            }

            ReportError(typeExpr, $"Expected type");
            return CheezType.Error;
        }

        private void CollectPolyTypes(AstTypeExpr typeExpr, HashSet<string> types)
        {
            switch (typeExpr)
            {
                case AstIdTypeExpr i:
                    if (i.IsPolymorphic)
                        types.Add(i.Name);
                    break;

                case AstPointerTypeExpr p:
                    CollectPolyTypes(p.Target, types);
                    break;

                case AstSliceTypeExpr p:
                    CollectPolyTypes(p.Target, types);
                    break;

                case AstArrayTypeExpr p:
                    CollectPolyTypes(p.Target, types);
                    break;

                case AstFunctionTypeExpr func:
                    if (func.ReturnType != null) CollectPolyTypes(func.ReturnType, types);
                    foreach (var p in func.ParameterTypes) CollectPolyTypes(p, types);
                    break;

                case AstPolyStructTypeExpr @struct:
                    foreach (var p in @struct.Arguments) CollectPolyTypes(p, types);
                    break;
            }
        }

        // struct
        private AstStructDecl InstantiatePolyStruct(AstPolyStructTypeExpr expr, List<AstStructDecl> instances = null)
        {
            var @struct = expr.Struct.Value as GenericStructType;

            if (expr.Arguments.Count != @struct.Declaration.Parameters.Count)
            {
                ReportError(expr, "Polymorphic struct instantiation has wrong number of arguments.", ("Declaration here:", @struct.Declaration));
                return null;
            }

            AstStructDecl instance = null;

            // check if instance already exists
            foreach (var pi in @struct.Declaration.PolymorphicInstances)
            {
                Debug.Assert(pi.Parameters.Count == expr.Arguments.Count);

                bool eq = true;
                for (int i = 0; i < pi.Parameters.Count; i++)
                {
                    var param = pi.Parameters[i];
                    var arg = expr.Arguments[i];
                    if (param.Value != arg.Value)
                    {
                        eq = false;
                        break;
                    }
                }

                if (eq)
                {
                    instance = pi;
                    break;
                }
            }

            // instatiate type
            if (instance == null)
            {
                instance = @struct.Declaration.Clone() as AstStructDecl;
                instance.SubScope = new Scope($"struct {@struct.Declaration.Name.Name}<poly>", instance.Scope);
                instance.IsPolyInstance = true;
                instance.IsPolymorphic = false;
                @struct.Declaration.PolymorphicInstances.Add(instance);
                instance.Scope.TypeDeclarations.Add(instance);

                Debug.Assert(instance.Parameters.Count == expr.Arguments.Count);

                for (int i = 0; i < instance.Parameters.Count; i++)
                {
                    var param = instance.Parameters[i];
                    var arg = expr.Arguments[i];
                    param.Type = arg.Type;
                    param.Value = arg.Value;
                }

                instance.Type = new StructType(instance);

                if (instances != null)
                    instances.Add(instance);
            }

            return instance;
        }

        private void ResolveStruct(AstStructDecl @struct, List<AstStructDecl> instances = null)
        {
            // define parameter types
            foreach (var p in @struct.Parameters)
            {
                @struct.SubScope.DefineTypeSymbol(p.Name.Name, p.Value as CheezType);
            }

            // resolve member types
            foreach (var member in @struct.Members)
            {
                member.TypeExpr.Scope = @struct.SubScope;
                member.Type = ResolveTypeHelper(member.TypeExpr, instances: instances);
            }
        }

        private void ResolveStructs(List<AstStructDecl> newInstances)
        {
            var nextInstances = new List<AstStructDecl>();

            int i = 0;
            while (i < MaxPolyStructResolveStepCount && newInstances.Count != 0)
            {
                foreach (var instance in newInstances)
                {
                    ResolveStruct(instance, nextInstances);
                }
                newInstances.Clear();

                var t = newInstances;
                newInstances = nextInstances;
                nextInstances = t;

                i++;
            }

            if (i == MaxPolyStructResolveStepCount)
            {
                var details = newInstances.Select(str => ("Here:", str.Location)).ToList();
                ReportError($"Detected a potential infinite loop in polymorphic struct declarations after {MaxPolyStructResolveStepCount} steps", details);
            }
        }

        // impl
        //private AstStructDecl InstantiatePolyImpl(AstImplBlock impl, List<AstImplBlock> instances = null)
        //{
        //    var target = impl.TargetType;

        //    //if (expr.Arguments.Count != @struct.Declaration.Parameters.Count)
        //    //{
        //    //    ReportError(expr, "Polymorphic struct instantiation has wrong number of arguments.", ("Declaration here:", @struct.Declaration));
        //    //    return null;
        //    //}

        //    // check if instance already exists
        //    AstStructDecl instance = null;
        //    //foreach (var pi in @struct.Declaration.PolymorphicInstances)
        //    //{
        //    //    Debug.Assert(pi.Parameters.Count == expr.Arguments.Count);

        //    //    bool eq = true;
        //    //    for (int i = 0; i < pi.Parameters.Count; i++)
        //    //    {
        //    //        var param = pi.Parameters[i];
        //    //        var ptype = param.Type;
        //    //        var pvalue = param.Value;

        //    //        var arg = expr.Arguments[i];
        //    //        var atype = arg.Type;
        //    //        var avalue = arg.Value;

        //    //        if (pvalue != avalue)
        //    //        {
        //    //            eq = false;
        //    //            break;
        //    //        }
        //    //    }

        //    //    if (eq)
        //    //    {
        //    //        instance = pi;
        //    //        break;
        //    //    }
        //    //}

        //    // instatiate type
        //    if (instance == null)
        //    {
        //        instance = impl.Clone() as AstImplBlock;
        //        instance.SubScope = new Scope($"impl {impl.TargetTypeExpr}<poly>", instance.Scope);
        //        impl..PolymorphicInstances.Add(instance);

        //        Debug.Assert(instance.Parameters.Count == expr.Arguments.Count);

        //        for (int i = 0; i < instance.Parameters.Count; i++)
        //        {
        //            var param = instance.Parameters[i];
        //            var arg = expr.Arguments[i];
        //            param.Type = arg.Type;
        //            param.Value = arg.Value;
        //        }

        //        instance.Type = new StructType(instance);

        //        if (instances != null)
        //            instances.Add(instance);
        //    }

        //    return instance;
        //}

        // struct
        private AstFunctionDecl InstantiatePolyFunction(Dictionary<string, CheezType> polyTypes, GenericFunctionType func, List<AstFunctionDecl> instances = null)
        {
            AstFunctionDecl instance = null;

            // check if instance already exists
            // TODO:

            // instatiate type
            if (instance == null)
            {
                instance = func.Declaration.Clone() as AstFunctionDecl;
                instance.SubScope = new Scope($"func {func.Declaration.Name.Name}<poly>", instance.Scope);
                instance.IsPolyInstance = true;
                instance.IsGeneric = false;
                instance.PolymorphicTypes = polyTypes;
                func.Declaration.PolymorphicInstances.Add(instance);

                instance.Scope.FunctionDeclarations.Add(instance);

                foreach (var pt in polyTypes)
                {
                    instance.SubScope.DefineTypeSymbol(pt.Key, pt.Value);
                }

                // return types
                if (instance.ReturnValue != null)
                {
                    instance.ReturnValue.Scope = instance.SubScope;
                    instance.ReturnValue.TypeExpr.Scope = instance.SubScope;
                    instance.ReturnValue.Type = ResolveType(instance.ReturnValue.TypeExpr, true);
                }

                // parameter types
                foreach (var p in instance.Parameters)
                {
                    p.TypeExpr.Scope = instance.SubScope;
                    p.Type = ResolveType(p.TypeExpr, true);
                }

                instance.Type = new FunctionType(instance);

                if (instances != null)
                    instances.Add(instance);
            }

            return instance;
        }
    }
}
