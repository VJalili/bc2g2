### Some blocks for debugging
- There are many transactions where they create many outputs 
with 0 value. For instance, the transaction with id 
`ceb1a7fb57ef8b75ac59b56dd859d5cb3ab5c31168aa55eb3819cd5ddbd3d806`
belonging to the block with height `123573`, contains `279` outputs
with value `0`. 

- Examples of some bad/strange transactions:
    - search for txes in this block: 71036
    - 268449
    - 565912
    - 706953
    - 774532
    - Not implemented: 710,061




### Making bitcoin-qt accessible by another computer in the LAN

read more at: https://github.com/bitcoin/bitcoin/blob/master/doc/JSON-RPC-interface.md#security

Set the `bitcoin.conf` file as the following

```
rpcbind=127.0.0.1
rpcbind=192.168.1.2
rpcallowip=192.168.1.2
rpcallowip=192.168.1.3
debug=http
server=1
rest=1
txindex=1
rpcworkqueue=100
```

where `192.168.1.2` is the IP address of the computer where Bitcoin-qt
is running, and `192.168.1.3` is the IP address of the computer where 
you want to query bitcoin-qt. Alternatively you can use `192.168.1.1/24`
to allow every computer in the subnet to query bitcoin-qt, at the cost 
of less restricted access.

To check if the node is accessible: 
- On the host machine you can run the following to check if bitcoin-qt is correctly listening on the given port:

```
$ netstat -aonq | findstr 8332
TCP    127.0.0.1:8332         0.0.0.0:0              LISTENING       64324
TCP    192.168.1.2:8332     0.0.0.0:0              LISTENING       64324
```

if the output of this command is at the following, it indicates that 
bitcoin-qt is listening on `localhost` and the port is not reachable 
from any other computer. In this case, make sure the above configuration
is set correctly, the `rpcbind` parameter specifically.

```
netstat -aonq | findstr 8332
TCP    127.0.0.1:8332         0.0.0.0:0              LISTENING       2416
TCP    [::1]:8332             [::]:0                 LISTENING       2416
```

- On the client machine you can run the following to check if the port is open and accessible:

```
$ nc -z 192.168.1.2 8332
Connection to 192.168.1.2 port 8332 (tcp/*) succeeded!
```

Read more about networking at: https://bitcoin.org/en/full-node#upgrading-bitcoin-core




### FAQ

- Can the output of a transaction can be referenced as input of another transaction in the same block? YES
For instance, at the block with hash `0000000000000000000cfa4e0939572c39cdaa8d58a275ae22e5877fc925b91a`, 
the transaction with ID `a68bb8474920375010f7941f5f0b7261194365fbfa916fb1cdc6d726accb9a81` is created
and its output is refereced in the same block as an input.


- P2PK & P2PKH:
    > These 2 types of payment are referred as P2PK (pay to public key) and P2PKH (pay to public key hash).
    Satoshi later decided to use P2PKH instead of P2PK for two reasons:
    Elliptic Curve Cryptography (the cryptography used by your public key and private key) is vulnerable to a modified Shor's algorithm for solving the discrete logarithm problem on elliptic curves. In plain English, it means that in the future a quantum computer might be able to retrieve a private key from a public key. By publishing the public key only when the coins are spent (and assuming that addresses are not reused), such an attack is rendered ineffective. 
    >
    > With the hash being smaller (20 bytes) it is easier to print and easier to embed into small storage mediums like QR codes.
    >
    > ref: https://programmingblockchain.gitbook.io/programmingblockchain/other_types_of_ownership/p2pk-h-_pay_to_public_key_-hash


### Design prenciples: 

- All the modules, including BitcoinAgent and all similar agents must implement 
  co-operative cancellation semantics:
  https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads?redirectedfrom=MSDN



### Bitcoin, bitcoin-qt commands, tips:
- Get the private key of Wallet using BitcoinQt
    - Open bitcoinQt node console window;
    - Run:

        ```
        dumpprivkey <address>
        ```

- Decode script

    ```json
    decodescript <HEX>
    ```

- Some blockchain explorers:

    - https://blockchair.com/bitcoin
    - https://live.blockcypher.com/btc/


### Tips on the PostgreSQL database 

- Run the following to create a migration script: 

```
Add-Migration Initial -OutputDir Infrastructure/Migrations
```

- Update the database with the migration script: 

```
Update-Database
```

Run these in the "Package Manager Console". If using console, make sure 
to first install `ef` into `dotnet`: 

```
$ dotnet tool install --global dotnet-ef
$ cd BC2G
$ 
```