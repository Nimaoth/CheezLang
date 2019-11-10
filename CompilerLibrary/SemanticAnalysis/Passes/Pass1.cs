﻿using System;
using System.Collections.Generic;
using Cheez.Ast.Statements;

namespace Cheez
{
    public partial class Workspace
    {
        // for semantic analysis
        private List<AstConstantDeclaration> mAllGlobalConstants = new List<AstConstantDeclaration>();
        private List<AstTraitTypeExpr> mAllTraits = new List<AstTraitTypeExpr>();
        private List<AstStructTypeExpr> mAllStructs = new List<AstStructTypeExpr>();
        private List<AstEnumTypeExpr> mAllEnums = new List<AstEnumTypeExpr>();
        private List<AstFuncExpr> mAllFunctions = new List<AstFuncExpr>();
        private List<AstVariableDecl> mAllGlobalVariables = new List<AstVariableDecl>();
        private List<AstUsingStmt> mAllGlobalUses = new List<AstUsingStmt>();
        private List<AstImplBlock> mAllImpls = new List<AstImplBlock>();

        private Queue<AstImplBlock> mUnresolvedImpls = new Queue<AstImplBlock>();
        private Queue<AstStructTypeExpr> mUnresolvedStructs = new Queue<AstStructTypeExpr>();
        private Queue<AstEnumTypeExpr> mUnresolvedEnums = new Queue<AstEnumTypeExpr>();
        private Queue<AstTraitTypeExpr> mUnresolvedTraits = new Queue<AstTraitTypeExpr>();
        private Queue<AstFuncExpr> mUnresolvedFunctions = new Queue<AstFuncExpr>();


        public IEnumerable<AstFuncExpr> Functions => mAllFunctions;
        public IEnumerable<AstVariableDecl> Variables => mAllGlobalVariables;
        public IEnumerable<AstTraitTypeExpr> Traits => mAllTraits;
        //

        private void AddTrait(AstTraitTypeExpr trait)
        {
            mAllTraits.Add(trait);
            mUnresolvedTraits.Enqueue(trait);
        }

        private void AddEnum(AstEnumTypeExpr en)
        {
            mAllEnums.Add(en);
            mUnresolvedEnums.Enqueue(en);
        }

        private void AddStruct(AstStructTypeExpr str)
        {
            mAllStructs.Add(str);
            mUnresolvedStructs.Enqueue(str);
        }

        private void AddFunction(AstFuncExpr func)
        {
            mAllFunctions.Add(func);
            mUnresolvedFunctions.Enqueue(func);
        }
    }
}
