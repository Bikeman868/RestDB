# RestDB
A more intuitive database with a non-proprietary http/restful interface

## Features
- Truly free and open source.
- Full ACID RDBMS with most of the features of SQL Server and MySQL.
- Designed for large scale applications and enterprise systems.
- Modern design that fits with modern software development practices.
- No legacy baggage, slim efficient and very easy to configure.
- Requests are sent over http(s) so you can use all of the tools and technologies sourounding http for routing, logging, security, proxy, load balancing, load testing etc.
- Uses https for login and authentication.
- Http Accept header defines output format including html, json, yaml, xml and binary formats.
- Customizable templates for html output. Template name supplied in the query string.
- Supports row ordered and column ordered tables, indexes and views.
- Supports transations, locks and isolation levels with full ACID.
- Supports stored procedures, functions and custom data types.
- Query plan optimization. Cache and reuse query plans.Statistical analysis of data for query plan optimization.
- Multiple language support (T-SQL, MySQL and Native).
- Granular permissions, identity groups and roles.
- Backup, restore, trasaction logs and replication.
- Use one file set or many sets and split each file set into as many files as you like to balance simplicity against performance.
- Map database objects (databases, tables, indexes, stored procedures etc) to any file set for greater flexibility.

## Project Status
Initial development phase. MVP not achieved yet.

## Language examples

```
LANGUAGE TSQL;

SELECT TOP(10)
	ROW_NUMBER() AS RowNumber,
	c.CustomerId, 
	c.OrderTotal 
FROM 
	Customers c 
ORDER BY c.OrderTotal DESC;
```

```
LANGUAGE MYSQL;

SET @RowNumber := 0;
SELECT
	@RowNumber := @RowNumber + 1 AS RowNumber
	c.CustomerId, 
	c.OrderTotal 
FROM 
	Customers c 
ORDER BY c.OrderTotal DESC
LIMIT 10;
```

```
LANGUAGE NSQL;

RowNumber = 1;
FOR c IN Customers ORDER BY c.OrderTotal DESC
{
	SELECT RowNumber, c.CustomerId, c.OrderTotal;
	IF (++RowNumber > 10) BREAK;
}
```
