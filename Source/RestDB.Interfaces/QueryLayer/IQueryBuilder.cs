using RestDB.Interfaces.TableLayer;
using System;
using System.Collections.Generic;

namespace RestDB.Interfaces.QueryLayer
{
    /// <summary>
    /// Defines a fluent syntax for creating queries. This builder
    /// is used by the various languages to separate the business of
    /// parsing syntax from the business of executing queries against
    /// a database.
    /// </summary>
    public interface IQueryBuilder : IStatementBuilder<IQueryBuilder>
    {
        IQuery Build();
    }

    public interface IStatementListBuilder<T> : IStatementBuilder<IStatementListBuilder<T>>
    {
        T End();
    }

    public interface IStatementBuilder<T>
    {
        IStatementListBuilder<T> Begin();
        ITransactionBuilder<T> BeginTransaction();
        IRowsetBuilder<IForBuilder<T>> BeginFor(string rowName);
        ISelectBuilder<T> BeginSelect();
        IIfExpressionBuilder<T> If();
        IWhileExpressionBuilder<T> BeginWhile();

        T Delete(string rowName);
        IExpressionBuilder<T> Assign(string name);
        T Break();
        T Continue();
    }

    public interface IForBuilder<T> : IStatementBuilder<IForBuilder<T>>
    {
        T EndFor();
    }

    public interface ITransactionBuilder<T> : IStatementBuilder<ITransactionBuilder<T>>
    {
        T Commit();
        T Rollback();
    }

    public interface ISelectBuilder<T> : IExpressionBuilder<ISelectBuilder<T>>
    {
        ISelectBuilder<T> Record(string name);
        ISelectBuilder<T> Alias(string name);
        T EndSelect();
    }

    public interface IBooleanExpressionBuilder<T>
    {
        IExpressionBuilder<IExpressionBuilder<T>> Compare(CompareOperation operation);
        IAndExpressionBuilder<T> BeginAnd();
        IOrExpressionBuilder<T> BeginOr();
    }

    public interface IExpressionBuilder<T> : IBooleanExpressionBuilder<T>
    {
        T Variable(string name);
        T Literal(object value);
        T Field(string name);
        T Field(string rowsetName, string name);
        T Aggregate(AggregationOperation operation, string fieldName);
        T Aggregate(AggregationOperation operation, string rowsetName, string fieldName);
        IExpressionBuilder<IExpressionBuilder<T>> Binary(BinaryOperator binaryOperator);
        IExpressionBuilder<T> Unary(UnaryOperator unaryOperator);
    }

    public interface IRowsetBuilder<T>
    {
        T Function(Func<IQueryContext, IEnumerable<IRow>> rowsetFunc);
        IRowsetBuilder<T> Limit(uint maxRecords);
        IRowsetBuilder<T> OrderBy(string fieldName);
        IRowsetBuilder<T> Ascending();
        IRowsetBuilder<T> Descending();
        T TableScan(string tableName);
        T IndexScan(string tableName, string indexName);
        T TableQuery(string tableName, Func<IQueryContext, IColumnQuery[]> columnValuesFunc);
        T IndexQuery(string tableName, string indexName, Func<IQueryContext, IColumnQuery[]> columnValuesFunc);
        IGroupBuilder<T> Group();
        IFilterTableBuilder<T> FromTable(string tableName);
    }

    public interface IFilterBuilder<T>
    {
        IExpressionBuilder<T> Column(string columnName, CompareOperation compare);
        IExpressionBuilder<T> Columns(params Tuple<string, CompareOperation, object>[] columns);
        IFilterTableBuilder<T> Table(string tableName);
        IAndWhereBuilder<T> BeginAnd();
        IOrWhereBuilder<T> BeginOr();
    }

    public interface IGroupBuilder<T>: IRowsetBuilder<IGroupBuilder<T>>
    {
        T By(params string[] columnNames);
    }

    public interface IFilterTableBuilder<T>
    {
        IExpressionBuilder<T> Column(string columnName, CompareOperation compare);
        IExpressionBuilder<T> Columns(params Tuple<string, CompareOperation, object>[] columns);
        IIndexTableBuilder<T> UsingIndex(string indexName);
    }

    public interface IIndexTableBuilder<T>
    {
        IExpressionBuilder<T> WhereColumn(string columnName, CompareOperation compare);
        IExpressionBuilder<T> WhereColumns(params Tuple<string, CompareOperation, object>[] columns);
        T Function(Func<IQueryContext, Tuple<IColumnDefinition, CompareOperation, object>[]> expressionFunction);
    }

    public interface IAndWhereBuilder<T> : IFilterBuilder<IAndWhereBuilder<T>>
    {
        T EndAnd();
    }

    public interface IOrWhereBuilder<T> : IFilterBuilder<IOrWhereBuilder<T>>
    {
        T EndOr();
    }

    public interface IIfExpressionBuilder<T> : IBooleanExpressionBuilder<IStatementBuilder<T>>
    {
    }

    public interface IWhileExpressionBuilder<T> : IBooleanExpressionBuilder<IStatementBuilder<T>>
    {
    }

    public interface IAndExpressionBuilder<T> : IBooleanExpressionBuilder<IAndExpressionBuilder<T>>
    {
        T EndAnd();
    }

    public interface IOrExpressionBuilder<T> : IBooleanExpressionBuilder<IOrExpressionBuilder<T>>
    {
        T EndOr();
    }
}

namespace RestDB.Examples
{
    using Interfaces;
    using Interfaces.QueryLayer;

    internal class QueryBuilder
    {
        private void Example1()
        {
            IQueryBuilder builder = null;

            var query = builder
                // Return the first 10 customers that have 'fred' as a rep using the 'customer_rep' index
                .Begin()
                    .Assign("count").Literal(0)
                    .BeginFor("customer")
                            .OrderBy("customerId").Ascending()
                            .FromTable("customers").UsingIndex("customer_rep")
                            .Function(q => new[] { new Tuple<IColumnDefinition, CompareOperation, object>(q.Table["customers"].Column["rep_name"], CompareOperation.Equal, "fred") })
                            //.WhereColumns(new Tuple<string, CompareOperation, object>("rep_name", CompareOperation.Equal, "fred"))
                            //.WhereColumn("rep_name", CompareOperation.Equal).Literal("fred")
                        .If().Compare(CompareOperation.Equal).Variable("count").Literal(10).Break()
                        .Assign("count").Unary(UnaryOperator.Increment).Variable("count")
                        .BeginSelect()
                            .Field("customerId").Alias("id")
                            .Field("name").Alias("customerName")
                        .EndSelect()
                    .EndFor()
                .End()

                // Build the query
                .Build();
        }

        private void Example2()
        {
            IColumnQueryFactory cq = null;
            IQueryBuilder builder = null;

            var query = builder
                // Delete users created in the last 7 days whose first name is 'martin' and who are under 18
                .BeginTransaction()
                    .BeginFor("user").Function(q =>
                            {
                                var users = q.Table["users"];
                                var isNewMatch = cq.Create(users.Column["created"], CompareOperation.Greater, DateTime.UtcNow.AddDays(-7));
                                return users.MatchingRows(q.Transaction, new[] { isNewMatch });
                            })
                        .If()
                            .BeginAnd()
                                .Compare(CompareOperation.Similar).Field("firstName").Literal("martin")
                                .Compare(CompareOperation.Less).Field("age").Literal(18)
                            .EndAnd()
                            .Delete("user")
                    .EndFor()
                .Commit()

                // Build the query
                .Build();
        }

        private void Example3()
        {
            IQueryBuilder builder = null;

            var query = builder
                // Return orders grouped by customer with additional customer info
                // where the total value of the customer's orders is more than 1000
                .BeginFor("customerAggregate").Group().TableScan("order").By("cutomerId")
                    .Assign("orderTotal").Aggregate(AggregationOperation.Sum, "orderValue")
                    .Assign("customerId").Field("customerId")
                    .If()
                        .Compare(CompareOperation.Greater).Variable("orderTotal").Literal(1000)
                        .BeginFor("customer")
                            .FromTable("customers")
                                .UsingIndex("ix_customer_id")
                                .WhereColumn("customerId", CompareOperation.Equal).Variable("customerId")
                            .BeginSelect()
                                .Variable("customerId").Alias("id")
                                .Field("customerName").Alias("name")
                                .Variable("orderTotal")
                            .EndSelect()
                        .EndFor()
                .EndFor()

                // Build the query
                .Build();
        }
    }
}