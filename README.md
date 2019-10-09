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
## Limitations

For most applications these limitations can be ignored. The RDMS is not 
artificially limited for licencing purposes, these limitations arise
from technical tradeoffs in the architectural design of the RDMS.

### File set limitations
- Maximum data files per file set = 64
- Maximum log files per file set = unlimited
- Maximum number of database objects per file set = unlimited
- Maximum number of different types of object per file set = page size / 8
- Maximum number of commited transactions per file set = 2^64
- Maximum number of pages per file set = 2^64
- Minimum page size = 64 bytes
- Maximum page size = 2^31 bytes (2GB)
- Maximum data file size = 2^63 bytes (1024 PB)
- Maximum log file size = 2^63 bytes (1024 PB)

### Row order table limitations
- Maximum row size = page size
- Maximum number of rows per table = 2^64
- Maximum number of columns per row = 2^16
- Maximum size of index entry = 2^16 bytes (64KB)


### Column order table limitations

## Performance tuning advice

### Page Size
The most important consideration is the choice of page size. The architecture
of RestDB has an ACID page store at the bottom and tables, indexes, procedures
etc are all layered on top.

All data files in a file set must have the same page size and the page size can
ony be changed by rwriting the whole file set (ie full backup and restore). Data
is read/written to the file set in Page Size chunks. This means that if you only 
change a few bytes the whole page will be read, updated and written back. Having
said that there is a very sophisticated mechanism to cache pages and group updates
by page to minimize the amount of disk operations required.

Database objects like tables, indexes etc must be contained within a single file
set (which can span multiple files). You can create as many file sets as you need,
and you should do this if your tables need different page sizes.

When transactions modify data they receive a private copy of the page that they
modified so that we have transaction isolation. This means that larger pages make
transactions that make lots of updates use a lot of additional memory.

Each page has a version history with all reachable versions held in memory. This
provides repeatable reads but means that long running transactions will hold
this snapshot of the database state in memory for a long time adding a lot of
memory pressure. Using smaller pag sizes will help this situation.

On the whole smaller pages are better except for the following considerations:
1. Many small pages creates a lot of management overhead increasing the memory
   needed to manage the pages and also adds CPU cycles to traverse data structures
   to find the version of a page that is appropriate to a given transaction.
2. In row ordered tables a row must be contained within a single page. The page
   size defines the maximum row size but there is also a fill factor. Consider having
   a page size of 64 bytes and rows that are 34 bytes long. In this case we can
   only fit one row in each page and 50% of the page is wasted. In this case
   each row will occupy 64 bytes of disk space and 64 bytes of memory even though
   the rows are only actually 34 bytes each.

### File sets
If your application is not constrained by disk I/O throughput and all of your
tables can use the same page size then you can put all of your database objects
such as tables, indexes, stored procedures etc into a single file set and configure
it with a single data file and a single log file. This makes it very convenient
to manage the files.

If you want to use different page sizes on different tables within your database 
then you will need to configure multiple file sets because the page size is defined
at the file set level.

Some people find it convenient to put their stored procedures in a separate file set
so that these can be managed independantly from the data. This makes no difference
from a performance standpoint but if you do this you should use a larger page size
for these file sets.

To improve performance you can split the file set into multiple data files and/or
log files.This improves performance because disk I/O operations are serialized
and threads have to wait their turn to access the file. For many database operations
most of the data is in cache and does not require many disk IO operations to complete.
Updates are always applied to the log file first then applied to the data file
only after all log file updates are flushed to disk, so these don't block the
executing transaction.

For applications that make a lot of small updates it helps to have more log files
because the updates can be stored in the logs using one thread for each log file.
These updates will be copied into the datafile by a separate background thread pool.

For applications that have a working set that is much larger than the available
memory on the machine it helps to have more data files because the cache hit rate 
will be lower and threads will block on reads that miss the cache.

It doesn't make sense to have more log files or more data files than you have CPUs.
For optimal performance split each file set into a number of files eaqual to the
number of CPUs in the machine and store these files on different physical disk
volumes.
