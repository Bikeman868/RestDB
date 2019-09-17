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
        IExpressionListBuilder<T> BeginSelect();
        IIfExpressionBuilder<T> BeginIf();
        IWhileExpressionBuilder<T> BeginWhile();

        T Delete(string rowName);
        T Assign(string variableName, Func<IQueryContext, object> value);
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

    public interface IExpressionListBuilder<T> : IExpressionBuilder<IExpressionListBuilder<T>>
    {
        IExpressionListBuilder<T> Record(string name);
        IExpressionListBuilder<T> Alias(string name);
        T EndSelect();
    }

    public interface IBooleanExpressionBuilder<T>
    {
        T Compare(Func<IQueryContext, object> leftSide, CompareOperation operation,
            Func<IQueryContext, object> rightSide);

        IAndExpressionBuilder<T> BeginAnd();
        IOrExpressionBuilder<T> BeginOr();
    }

    public interface IExpressionBuilder<T> : IBooleanExpressionBuilder<T>
    {
        T Variable(string name);
        T Field(string name);
        T Field(string rowsetName, string name);
    }

    public interface IRowsetBuilder<T>
    {
        IRowsetBuilder<T> Limit(uint maxRecords);
        IRowsetBuilder<T> OrderBy(string fieldName);
        IRowsetBuilder<T> Ascending();
        IRowsetBuilder<T> Descending();
        T Function(Func<IQueryContext, IEnumerable<IRow>> rowsetFunc);
        T TableScan(string tableName);
        T IndexScan(string tableName, string indexName);
        T TableQuery(string tableName, Func<IQueryContext, IColumnQuery[]> columnValuesFunc);
        T IndexQuery(string tableName, string indexName, Func<IQueryContext, IColumnQuery[]> columnValuesFunc);
        IFilterBuilder<T> TableWhere(string tableName);
        IFilterBuilder<T> IndexWhere(string tableName, string indexName);
    }

    public interface IFilterBuilder<T>
    {
        IFilterBuilder<T> Column(Func<IQueryContext, IColumnQuery> columnQueryFunc);
        T EndWhere();
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
            IColumnQueryFactory cq = null;
            IQueryBuilder builder = null;

            var query = builder
                // Return the first 10 customers that have 'fred' as a rep using the 'customer_rep' index
                .Begin()
                    .Assign("count", q => 0)
                    .BeginFor("customer")
                        .OrderBy("customerId").Ascending()
                        .Limit(10)
                        .IndexWhere("customers", "customer_rep")
                            .Column(q => cq.Create(q.Table["customers"].Index["customer_rep"].Definition.Columns[0].Column, CompareOperation.Equal, "fred"))
                        .EndWhere()
                        .BeginIf().Compare(q => q.Variable<int>("count"), CompareOperation.Equal, q => 10).Break()
                        .Assign("count", q => q.Variable<int>("count") + 1)
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
                    .BeginFor("user")
                        .Function(q =>
                        {
                            var users = q.Table["users"];
                            var isNewMatch = cq.Create(users.Column["created"], CompareOperation.Greater, DateTime.UtcNow.AddDays(-7));
                            return users.MatchingRows(q.Transaction, new[] { isNewMatch });
                        })
                        .BeginIf()
                            .BeginAnd()
                                .Compare(q => q.FieldValue<string>("firstName"), CompareOperation.Similar, q => "martin")
                                .Compare(q => q.FieldValue<int>("age"), CompareOperation.Less, q => 18)
                            .EndAnd()
                            .Delete("user")
                    .EndFor()
                .Commit()

                // Build the query
                .Build();
        }
    }
}