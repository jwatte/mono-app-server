
mono-app-server
===============

A simple application server for Mono (and, presumably, Microsoft CLR).

Introduction 
------------

This server is intended solely for web services, and does not serve static content (files) at all!
It is currently in the status of "tech demo" and only useful for testing ideas out on.
You describe the API for services in XML, and generate glue code, which you build.
Then you write the actual service/es as a plain C# class.
Requests to example.com:8888/class/method?arg1=2&arg2=false will translate to method calls on the object.
A simple key/value persistent store (that does not shard or distribute) is included.

Quickstart
----------

1. Build the Webserver.sln solution in monodevelop
2. cd to Plugins and make to build the auth plugin
3. Make sure that MONO_PATH points at the location of idl.dll (typically, where you built the Runtime.exe)
4. Start the Runtime.exe application, passing it "codepath=...../Plugins/bin" as an argument
5. It now listens to port localhost:8888
6. You can log in by hitting http://localhost:8888/auth/signin?userid=jwatte&password=123456
7. You can verify that you are logged in by hitting http://localhost:8888/auth/verify
8. You can log out by hitting http://localhost:8888/auth/signout
9. The "verify" method should now return an error when called

IDL
---

The IDL format is XML, as found in auth.xml. The idea is to keep the text part of the XML 
documented available for documentation -- any text under a <method> tag would be documentation 
for that method.

A few data types are currently supported:

* idstring -- 32 chars max
* password -- 64 chars max
* varchar  -- 255 chars max
* text     -- 8191 chars max
* bool     -- true or false
* long     -- 64-bit integer (only 53 bits in JavaScript)
* email    -- email address, 64 chars max

Methods can be open to the world (session="false") or require a valid login (default, or 
explicit with session="true"). There is an affordance for a named-privilege markup as well 
but this has not been tested and there is no way to grant privileges to users (yet ?).

A user needs to have all permissions listed for a method to call that method. There is an 
optional attribute "self='paramname'" which says that this permission is not needed if the 
operation is targeting the user himself; specifically, the 'paramname' parameter has the 
value of the userid logged in.

Possible future improvements
----------------------------

* Make all requests return a byte array -- writing to the output stream should be bad.
Perhaps wrap the Http request/response in a layer that only exposes what is directly 
needed and that buffers any output using a MemoryStream?
* Fold in JavaScript client-side API wrapper generator.
* Write some useful plug-ins.
* Write a real user authentication plug-in.
* Management interface (another port? a plugin?)
* Runtime re-load when plug-ins change (possibly automatic).
* Lots of other stuff.
