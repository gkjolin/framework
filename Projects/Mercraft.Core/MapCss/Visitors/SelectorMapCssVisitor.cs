﻿using System;
using Antlr.Runtime.Tree;
using Mercraft.Core.MapCss.Domain;

namespace Mercraft.Core.MapCss.Visitors
{
    public class SelectorMapCssVisitor : MapCssVisitorBase
    {
        public override Selector VisitSelector(CommonTree selectorTree, string selectorType)
        {
            Selector selector = null;

            switch (selectorType)
            {
                case "node":
                    selector = new NodeSelector();
                    break;
                case "way":
                     selector = new WaySelector();
                    break;
                case "area":
                    selector = new AreaSelector();
                    break;
                case "canvas":
                    selector = new CanvasSelector();
                    break;
                default:
                    throw new MapCssFormatException(selectorTree, 
                        String.Format("Unknown selector type: {0}", selectorType));
            }
           
            var operation = selectorTree.Children[0].Text;

            ParseOperation(selectorTree, selector, operation);

            selector.Operation = operation;

            return selector;
        }

        /// <summary>
        /// Processes selector definition
        /// </summary>
        private void ParseOperation(CommonTree selectorTree, Selector selector, string operation)
        {
            // special pseudo selector class like area[building]:closed
            if (selectorTree.Text == "PSEUDO_CLASS_SELECTOR")
            {
                var pseudoClass = selectorTree.Children[1].Text;
                if (pseudoClass == "closed")
                {
                    selector.IsClosed = true;
                }
            }
            // existing selector case
            else if (operation == MapCssStrings.OperationExist)
            {
                if (selectorTree.ChildCount != 2)
                {
                    throw new MapCssFormatException(selectorTree, "Wrong 'exist' selector operation");
                }
                selector.Tag = selectorTree.Children[1].Text;
            }
            // Various selector operation like equals
            else
            {
                if (selectorTree.ChildCount != 3)
                {
                    throw new MapCssFormatException(selectorTree,
                        String.Format("Wrong '{0}' selector operation", operation));
                }

                switch (operation)
                {
                    case "=":
                        break;
                    // TODO add != operation as well
                    default:
                        throw new MapCssFormatException(selectorTree,
                            String.Format("Not supported selector operation: {0}", operation));
                }

                selector.Tag = selectorTree.Children[1].Text;
                selector.Value = selectorTree.Children[2].Text;
            }
        }

    }
}