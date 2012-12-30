DotQL
=====

DotQL is a *functional language* and *federated query processor* implemented in C# for .NET.  The intent is to provide a logical abstraction layer between applications and systems. 

Approximate Status:
* Front-end / SLA: 25% (only enforces time-based SLAs)
* Grammar: 100%
* Lexer: 99%
* Parser: 70%
* Planner: 1%
* Compiler: 3% (functioning for very basic expressions)
* Functions and types: 1%
* Pre-prepared query library: 0%
* Module library: 0%
* SLA authorization: 0%
* Storage abstraction: 5%
* Text/example stores: 0%
* Monitoring: 0%

Language
--------

DotQL is a full featured functional language, optimized around sets, named tuples, and lists.  Tables are simply sets of tuples, and relational queries may be easily formulated using "path" style dereferencing rather than explicit joins.  DotQL shares many similarities with [XQuery](http://en.wikipedia.org/wiki/XQuery), including FLWOR expressions; however, the language centers around relational data management concepts, not XML documents.

DotQL seeks to provide a simpler mechanism for querying a relational schema, especially for those used to programming language metaphors.  For instance, the following expression fetches all items that have been ordered by customers in a certain Zip Code:

	return Customer(ZipCode = '84604').Orders.Items

This is equivalent to the following SQL query:

	select I.ItemID, I.OrderID, I.PartID, I.Quantity 
		from Items as I
			join Orders as O on O.OrderID = I.OrderID
			join Customer as C on C.CustomerID = O.CustomerID
		where C.ZipCode = '84604'

For a by-example introduction to DotQL, read [DotQLByExample](DotQL/blob/master/Documents/DotQLByExample.dql).

Federated Query Processor
-------------------------

A query processor is a system that provides access to shared data (what we call a database) via a language (aka data model).  A federated query processor, allows queries and manipulation over multiple, even disparate, data sources.  Architecturally, a federated query processor provides a mechanism whereby an organization can insulate one set of systems, such as application servers, from another set of systems, such as database, indexing, and caching systems.  

For more, visit the [Overview](DotQL/wiki/Overview).
