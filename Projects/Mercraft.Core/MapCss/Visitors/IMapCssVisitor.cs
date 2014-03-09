﻿using Antlr.Runtime.Tree;
using Mercraft.Core.MapCss.Domain;
using Mercraft.Core.MapCss.Domain.Selectors;

namespace Mercraft.Core.MapCss.Visitors
{
    public interface IMapCssVisitor
    {
        Stylesheet Visit(CommonTree tree);

        Rule VisitRule(CommonTree ruleTree);

        Selector VisitSelector(CommonTree selectorTree);

        Declaration VisitDeclaration(CommonTree declarationTree);
    }
}
