﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cheez.Ast.Statements;

namespace Cheez
{
    public partial class Workspace
    {
        private void AnalyzeFunctions(List<AstFunctionDecl> newInstances)
        {
            var nextInstances = new List<AstFunctionDecl>();

            int i = 0;
            while (i < MaxPolyFuncResolveStepCount && newInstances.Count != 0)
            {
                foreach (var instance in newInstances)
                {
                    AnalyzeFunction(instance, nextInstances);
                }
                newInstances.Clear();

                var t = newInstances;
                newInstances = nextInstances;
                nextInstances = t;

                i++;
            }

            if (i == MaxPolyFuncResolveStepCount)
            {
                var details = newInstances.Select(str => ("Here:", str.Location)).ToList();
                ReportError($"Detected a potential infinite loop in polymorphic function declarations after {MaxPolyFuncResolveStepCount} steps", details);
            }
        }

        private void AnalyzeFunction(AstFunctionDecl func, List<AstFunctionDecl> instances = null)
        {
            if (func.Body != null)
            {
                func.Body.Scope = func.SubScope;
                AnalyzeStatement(func.Body);
            }
        }

        private void AnalyzeStatement(AstStatement stmt)
        {
            switch (stmt)
            {
                case AstBlockStmt block: AnalyzeBlockStatement(block); break;
                case AstReturnStmt ret: AnalyzeReturnStatement(ret); break;
            }
        }

        private void AnalyzeReturnStatement(AstReturnStmt ret)
        {
            if (ret.ReturnValue != null)
            {
                ret.ReturnValue.Scope = ret.Scope;
                InferTypes(ret.ReturnValue, null);

                ConvertLiteralTypeToDefaultType(ret.ReturnValue);
            }
        }

        private void AnalyzeBlockStatement(AstBlockStmt block)
        {
            foreach (var stmt in block.Statements)
            {
                stmt.Scope = block.Scope;
                AnalyzeStatement(stmt);
            }

            if (block.Statements.LastOrDefault() is AstExprStmt expr)
            {
                // TODO
            }
        }
    }
}