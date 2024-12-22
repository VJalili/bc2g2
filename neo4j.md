## Neo4j tips

Make sure apoc and all the necessary plug-ins are installed on the given
Neo4j database at the time of initialization. Currently if any of the plug-ins 
are not installed it fails at the load time. 

1. Visualize schema: 

    ```
    CALL db.schema.visualization()
    ```

2. Delete all the relationships and then all the nodes: 

    ```
    match ()-[r]->() delete r; match (n) delete n;
    ```

    if you have many nodes or edges, the following could be a better query.

    ```
    CALL apoc.periodic.iterate(
    "MATCH (n) RETURN n",
    "DETACH DELETE n",
    {batchSize:10000})
    ```

3. Get a list of all the contraints: 

    ```
    SHOW CONSTRAINTS
    ```


4. Get a list of all the indexes:

    ```
    SHOW INDEXES
    ```

5. drop all the constraints:

    ```
    CALL apoc.schema.assert({}, {})
    ```

6. Get all edges: 

    ```
    MATCH (n)-[r]-(m) RETURN r;
    ```

7. Get schema

    ```
    CALL apoc.meta.schema() YIELD value as schemaMap
    ```

8. get stats:
    ```
    CALL apoc.meta.stats();
    ```

    
9. why not using neo4j graph data science ((gds))[https://neo4j.com/docs/graph-data-science/current/] for graph analytics? 
because most (if not all) of the algorithms in gds require to load 
data from neo4j graph database into an in-memory graph library, and 
use CPU. drawbacks: CPU is not the most optimal compared to GPU, and
it would have been ideal to operate on the graph database directly, 
if going to load into a different system, cugraph can be more 
optimal.


10. a quick way to delete data in a database: 

    ```
    cd "[INSTALL DIR]\neo4j\Neo4j Desktop\relate-data\dbmss\dbms-[UUID]"
    rm .\data\databases\neo4j\ 
    rm .\data\transactions\neo4j\ 
    ```


### Neo4j test data

```
merge (b1:Block {height:1}) 
merge (b2:Block {height:2}) 
merge (b3:Block {height:3})
merge (b4:Block {height:4})
merge (s1:Script {address:"A"})
merge (s2:Script {address:"B1"})
merge (s3:Script {address:"B2"})
merge (s4:Script {address:"C"})
merge (s5:Script {address:"D"})
merge (s1)-[r01:Creates]->(b1)
merge (s2)-[r02:Creates]->(b2)
merge (s3)-[r03:Creates]->(b2)
merge (s4)-[r04:Creates]->(b3)
merge (s5)-[r05:Creates]->(b4)
merge (b1)-[r06:Redeems]->(s2)
merge (b1)-[r07:Redeems]->(s3)
merge (b2)-[r08:Redeems]->(s4)
merge (b3)-[r09:Redeems]->(s5)
merge (s1)-[r10:Sends {value:5}]->(s2)
merge (s1)-[r11:Sends {value:5}]->(s3)
merge (s2)-[r12:Sends {value:5}]->(s4)
merge (s3)-[r13:Sends {value:5}]->(s4)
merge (s4)-[r14:Sends {value:10}]->(s5)
```

```
merge (s1:Script {Address:"A", ScriptType:"PubKey"})
merge (s2:Script {Address:"B1", ScriptType:"PubKey"})
merge (s3:Script {Address:"B2", ScriptType:"PubKey"})
merge (s4:Script {Address:"C", ScriptType:"PubKey"})
merge (s1)-[:Transfer {Value:10.1, Height:1}]->(s2)
merge (s1)-[:Transfer {Value:11.2, Height:2}]->(s3)
merge (s2)-[:Transfer {Value:12.3, Height:3}]->(s4)
```

get neighbors of node with `address=A` at 3 hop distance.
```
match (n:Script {address:"A"})
call apoc.neighbors.byhop(n, "Sends", 3) 
yield nodes 
return nodes
```


### Notes
- Neo4j does not allow fixing the seed, and since the process of sampling graphs 
leverages randomly selected root nodes, hence the sampling process is not 
reproducible. Resulting in different sets of graphs at every try.




- `CREATE` instead of `MERGE` in Neo4j queries. It is better to use 
`MERGE` in queries to match a node or relationship first, and create
if does not exist, or update if exists. However, matching a node or
relationship based on given properties becomes exponentially expensive 
at the LOAD time w.r.t the number of nodes or relationships in a 
Neo4j database. I tried indexing all the properties of a node or 
relationship that are used for matching. Indexing helped a bit, 
but still the import runtime grows exponentially. 
Therefore, all the queries are implemented using `CREATE` 
which skips the matching step. This solves the issue with 
runtime, but results in creating duplicates. In order to 
avoid duplicates, BC2G implements some features to avoid 
duplicates does not exist in the CSV files used for importing 
data in Neo4j. However, BC2G does not track **every** node or
relationship it writes to a CSV file, hence there is a rare possibility 
that a duplicate node or relationship might be added. 
However, duplicates will be created if a query on a given 
CSV file is executed more than once. I tried approaches for 
cleaning up a Neo4j graph after the import, by first 
finding duplicates, and then merge/delete them. This was a 
very slow and infeasable to run approach at this scale. 