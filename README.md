CouchDBHeartbeat 
----------------
CouchDB changes API for Silverlight (Chunked HTTP response parser in C#).  
Tested with Silverlight 4.0 in and out of browser

In-browser implementations will need to setup their policy files accordingly.

A word about ports
------------------
Silverlight is not allowed to connect to the default CouchDB port, it's out of the valid range. 
You will need to change your CouchDB default port in your config.  This example is looking at port 4530.

Configuration
-------------
Everything is configured in CouchDBHeartbeat.cs.  Check the top of the class for the configurable constants, port, server name, etc...