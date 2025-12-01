using Microsoft.Azure.Cosmos;
using Microsoft.OData.UriParser;
using System;
using System.Text;
using Denomica.Cosmos;
using Denomica.OData;
using System.Linq;

namespace Denomica.Cosmos.Odata
{
    /// <summary>
    /// Provides extension methods for building Cosmos DB SQL queries using an <see cref="ODataUriParser"/>.
    /// </summary>
    /// <remarks>This static class contains methods that extend the functionality of <see
    /// cref="QueryDefinitionBuilder"/>      to support appending SELECT, WHERE, and ORDER BY clauses based on OData
    /// query options. It also includes      a method to create a complete <see cref="QueryDefinition"/> from an OData
    /// URI.</remarks>
    public static class CosmosExtensionMethods
    {
        /// <summary>
        /// Appends the SELECT and EXPAND clauses to the query definition based on the provided OData URI parser.
        /// </summary>
        /// <param name="builder">The <see cref="QueryDefinitionBuilder"/> to which the SELECT and EXPAND clauses will be appended.</param>
        /// <param name="uriParser">The <see cref="ODataUriParser"/> used to parse the OData query options.</param>
        /// <returns>The updated <see cref="QueryDefinitionBuilder"/> with the appended SELECT and EXPAND clauses.</returns>
        /// <exception cref="NotSupportedException">Thrown if the parsed OData query contains unsupported select items. Only path select items are supported.</exception>
        public static QueryDefinitionBuilder AppendSelectAndExpand(this QueryDefinitionBuilder builder, ODataUriParser uriParser)
        {
            var select = uriParser.ParseSelectAndExpand();
            if (null != select && !select.AllSelected && !select.SelectedItems.All(x => x is PathSelectItem))
            {
                throw new NotSupportedException("Only path select items are currently supported.");
            }

            builder
                .AppendQueryText("SELECT")
                .AppendQueryTextIf(" *", null == select || select.AllSelected)
                .AppendQueryTextIf($" c[\"{string.Join("\"],c[\"", select!.SelectedPathIdentifiers())}\"]", null != select && !select.AllSelected)
                .AppendQueryText(" FROM c")
                ;

            return builder;
        }

        /// <summary>
        /// Appends a filter condition to the query being built, based on the filter expression parsed from the
        /// specified OData URI.
        /// </summary>
        /// <remarks>If the <paramref name="uriParser"/> does not contain a valid filter expression, no
        /// changes are made to the query.</remarks>
        /// <param name="builder">The <see cref="QueryDefinitionBuilder"/> instance to which the filter condition will be appended.</param>
        /// <param name="uriParser">The <see cref="ODataUriParser"/> used to parse the filter expression from the OData URI.</param>
        /// <returns>The updated <see cref="QueryDefinitionBuilder"/> instance with the appended filter condition, if a valid
        /// filter expression is present; otherwise, the original builder.</returns>
        public static QueryDefinitionBuilder AppendFilter(this QueryDefinitionBuilder builder, ODataUriParser uriParser)
        {
            var filter = uriParser.ParseFilter();
            if (null != filter?.Expression)
            {
                builder
                    .AppendQueryText(" WHERE ")
                    .AppendFilterNode(filter.Expression)
                    ;
            }

            return builder;
        }

        /// <summary>
        /// Appends an <c>ORDER BY</c> clause to the query definition based on the specified OData URI parser.
        /// </summary>
        /// <remarks>This method parses the <c>$orderby</c> expressions from the provided OData URI and appends
        /// them to the query definition. If no <c>ORDER BY</c> expressions are present in the URI, the query definition
        /// remains unchanged.</remarks>
        /// <param name="builder">The <see cref="QueryDefinitionBuilder"/> to which the <c>ORDER BY</c> clause will be appended.</param>
        /// <param name="uriParser">The <see cref="ODataUriParser"/> used to parse the <c>$orderby</c> expressions from the OData query.</param>
        /// <returns>The updated <see cref="QueryDefinitionBuilder"/> with the appended "ORDER BY" clause, if applicable.</returns>
        public static QueryDefinitionBuilder AppendOrderBy(this QueryDefinitionBuilder builder, ODataUriParser uriParser)
        {
            var orderBy = uriParser.ParseOrderBy().ToList();
            if (orderBy.Count > 0)
            {
                builder.AppendQueryText(" ORDER BY");
                var orderByIndex = 0;
                foreach (var item in orderBy)
                {
                    builder
                        .AppendQueryTextIf(",", orderByIndex > 0)
                        .AppendQueryTextIf(" ", orderByIndex == 0)
                        .AppendQueryText("c[\"")
                        .AppendQueryText(item.Item1.Property.Name)
                        .AppendQueryText("\"]")
                        .AppendQueryTextIf(" desc", item.Item2 == OrderByDirection.Descending);

                    orderByIndex++;
                }
            }

            return builder;
        }

        /// <summary>
        /// Creates a <see cref="QueryDefinition"/> based on the parsed OData URI.
        /// </summary>
        /// <remarks>This method constructs the query definition by sequentially appending the Select,
        /// Expand, Filter, and OrderBy clauses parsed from the OData URI. The resulting <see cref="QueryDefinition"/>
        /// can be used to execute or analyze the query.</remarks>
        /// <param name="uriParser">The <see cref="ODataUriParser"/> instance used to parse the OData URI.</param>
        /// <returns>A <see cref="QueryDefinition"/> representing the query components derived from the OData URI.</returns>
        public static QueryDefinition CreateQueryDefinition(this ODataUriParser uriParser)
        {
            return new QueryDefinitionBuilder()
                .AppendSelectAndExpand(uriParser)
                .AppendFilter(uriParser)
                .AppendOrderBy(uriParser)
                .Build()
                ;
        }



        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, SingleValueNode node)
        {
            if (node is BinaryOperatorNode)
            {
                builder.AppendFilterNode((BinaryOperatorNode)node);
            }
            else if (node is ConvertNode)
            {
                builder.AppendFilterNode((ConvertNode)node);
            }
            else if (node is SingleValuePropertyAccessNode)
            {
                builder.AppendFilterNode((SingleValuePropertyAccessNode)node);
            }
            else if (node is ConstantNode)
            {
                builder.AppendFilterNode((ConstantNode)node);
            }
            else
            {
                throw new NotSupportedException($"Unsupported filter node type: {node.GetType().FullName}");
            }

            return builder;
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, BinaryOperatorNode node)
        {
            Func<SingleValueNode, bool> isBinaryOr = svNode =>
            {
                return svNode is BinaryOperatorNode && ((BinaryOperatorNode)svNode).OperatorKind == BinaryOperatorKind.Or;
            };

            switch (node.OperatorKind)
            {
                case BinaryOperatorKind.And:
                    builder
                        .AppendFilterNode(node.Left)
                        .AppendQueryText(" and ")
                        .OpenParenthesisIf(isBinaryOr(node.Right))
                        .AppendFilterNode(node.Right)
                        .CloseParenthesisIf(isBinaryOr(node.Right))
                        ;
                    break;

                case BinaryOperatorKind.Or:
                    builder
                        .AppendFilterNode(node.Left)
                        .AppendQueryText(" or ")
                        .AppendFilterNode(node.Right)
                        ;
                    break;

                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.NotEqual:
                    builder
                        .AppendFilterNode(node.Left)
                        .AppendQueryTextIf(" =", node.OperatorKind == BinaryOperatorKind.Equal)
                        .AppendQueryTextIf(" >", node.OperatorKind == BinaryOperatorKind.GreaterThan)
                        .AppendQueryTextIf(" >=", node.OperatorKind == BinaryOperatorKind.GreaterThanOrEqual)
                        .AppendQueryTextIf(" <", node.OperatorKind == BinaryOperatorKind.LessThan)
                        .AppendQueryTextIf(" <=", node.OperatorKind == BinaryOperatorKind.LessThanOrEqual)
                        .AppendQueryTextIf(" !=", node.OperatorKind == BinaryOperatorKind.NotEqual)
                        .AppendFilterNode(node.Right)
                        ;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported node operator kind: {node.OperatorKind}");
            }

            return builder;
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, ConvertNode node)
        {
            builder.AppendFilterNode(node.Source);
            return builder;
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, SingleValuePropertyAccessNode node)
        {
            return builder
                .AppendQueryText("c[\"")
                .AppendQueryText(node.Property.Name)
                .AppendQueryText("\"]")
                ;
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, ConstantNode node)
        {
            var name = $"@p{builder.Parameters.Count}";

            object value = node.Value;
            if(value is Microsoft.OData.Edm.Date)
            {
                // We need to convert an Edm.Date to a DateTime, because otherwise filtering on dates
                // will not work and produce the required result.
                var dt = (Microsoft.OData.Edm.Date)node.Value;
                value = new DateTime(dt.Year, dt.Month, dt.Day);
            }

            return builder
                .AppendQueryText(" ")
                .AppendQueryText(name)
                .WithParameter(name, value)
                ;
        }

        private static QueryDefinitionBuilder OpenParenthesis(this QueryDefinitionBuilder builder)
        {
            return builder.AppendQueryText("(");
        }

        private static QueryDefinitionBuilder OpenParenthesisIf(this QueryDefinitionBuilder builder, bool condition)
        {
            if (condition) builder.OpenParenthesis();
            return builder;
        }

        private static QueryDefinitionBuilder CloseParenthesis(this QueryDefinitionBuilder builder)
        {
            return builder.AppendQueryText(")");
        }

        private static QueryDefinitionBuilder CloseParenthesisIf(this QueryDefinitionBuilder builder, bool condition)
        {
            if (condition) builder.CloseParenthesis();
            return builder;
        }
    }
}
