SquirrelDb
==========
All C# NoSql key value NoSql database.

##Building
Ensure that NuGet package restore is enabled to build.


##Usage

#####Server Config
In the moc server console application please check app config to set database directory.


`<appSettings>`<br/>
    `<add key="DatabaseLocation" value="C:\Database\"/>`<br/>
    `<add key="ApiHostUrl" value="http://localhost:9000"/>`<br/>
`</appSettings>`


#####Create New Bucket
Buckets can be created with the C# client (wraps web api) or by using the console "cbucket" command.<br><br>
The following line will create a bucket named "test-bucket" that has a maximum document size of 300 bytes allowing 10000 entries per backing file before a new file is created.

<b>Example</b>: <code>cbucket test-bucket|300|10000</code>

