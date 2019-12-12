﻿using Cheez.Ast;
using Cheez.Ast.Expressions;
using Cheez.Ast.Statements;
using Cheez.Types;
using Cheez.Types.Complex;
using Cheez.Types.Primitive;
using System;

namespace Cheez
{
    public partial class Workspace
    {
        private bool Move(CheezType targetType, AstExpression expr, SymbolStatusTable symStatTable, ILocation location = null)
        {
            switch (expr)
            {
                //case AstTempVarExpr tmp:
                //    {
                //        return Move(targetType, tmp.Expr, symStatTable, location);
                //    }

                case AstDotExpr dot:
                    {
                        if (dot.Left.Type is EnumType)
                        {
                            Move(targetType, dot.Left, symStatTable, location);
                            return true;
                        }
                        else if (!dot.Type.IsCopy)
                        {
                            ReportError(location ?? expr, $"Can't move out of '{dot}' because type {dot.Type} is not copy");
                            return false;
                        }
                        return true;
                    }

                case AstIdExpr id:
                    {
                        if (id.Type is ReferenceType && !(targetType is ReferenceType))
                        {
                            ReportError(id, $"Can't move out of reference");
                            return false;
                        }

                        if (symStatTable.TryGetSymbolStatus(id.Symbol, out var status))
                        {
                            if (status.kind != SymbolStatus.Kind.initialized)
                            {
                                ReportError(location ?? expr, $"Can't move out of '{id.Name}' because it is {status.kind}",
                                    ("Moved here:", status.location));
                                return false;
                            }
                            else
                            {
                                // check if type is move or copy
                                var type = (id.Symbol as ITypedSymbol).Type;
                                if (!type.IsCopy)
                                {
                                    symStatTable.UpdateSymbolStatus(id.Symbol, SymbolStatus.Kind.moved, expr);
                                }
                            }
                        }
                        else if (id.Symbol is Using use)
                        {
                            return Move(targetType, use.Expr, symStatTable, id.Location);
                        }
                        return true;
                    }

                //case AstStructValueExpr _:
                //    // TODO: move arguments
                //    break;

                default:
                    return true;
            }
        }
    }
}
