DotQL
=====

DotQL is a functional language and a federated query processor implemented in C# for .NET.  The intent is to provide a logical abstraction layer between applications and systems. 

Language
--------

DotQL is a full featured functional language, optimized around sets, named tuples, and lists.  Tables are simply sets of tuples, and relational queries may be easily formulated using a "path" style dereferencing rather than manual joins.  DotQL shares many similarities with XQuery, including the FLWOR expressions; however, the data model is oriented toward relational concepts.

DotQL seeks to make querying a relational schema more natural, especially for those used to Object Oriented metaphors.  For instance, the following expression fetches all items that have been ordered by a certain customer:
	return Customer(ID = 123).Orders.Items
	// e.g. result: { { ItemID: 234 OrderID: 345 PartID: 135 Quantity: 5 } ... }

For a quick introduction to DotQL, read [DotQLByExample](blob/master/Documents/DotQLByExample.dql).

Federated Query Processor
-------------------------

The federated query processor allows queries and transactions to be performed in a highly logical language near the abstraction of the domain-model, yet be implemented using various different storage, indexing, and/or caching techniques.