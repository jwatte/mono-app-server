
IDL Schema Format
=================

The IDL schema describes the contract for a particular interface. The contract consists 
of methods of access, parameters, return values, and necessary permissions.
The schema format is a well-formed XML document. You could write an XSD that validates 
the schema, but I have not done so.
IDL files can be compiled into server-side or client-side interface helpers. On the 
server side, it can compile to type/request checking code that takes care of checking 
input data formats and permissions and then delegates to a plain user-defined object. On 
the client side, it can compile to a native interface that provides an RPC-style API to 
the described interface at some given destination domain/URL.

The exact rules for how IDL maps to a particular language/environment are specific to 
that environment, but should be consistent within the environment, so that a programmer 
used to the language and environment in question will feel at home within the generated 
code.

XML Elements
----------

* Element: interface
* Description: The description of an interface, including documentation and all the 
methods, permissions, and return values used by the interface.
* Context: (root)
* Attributes:
    * name: idstring: name of the interface (used for language identifiers)
    * version: datetime: version of the interface (used for versioning)
* Children:
    * method

* Element: method
* Description: The description of a method within an interface, including the 
permissions, parameters, and return values.
* Context: interface
* Attributes:
    * name: idstring: the name of the method (used for language identifiers)
    * session: boolean: optional, default "true", whether this method requires an 
        established session (whatever that means for the binding)
    * formatter: idstring: optional, default "json", the type of the formatter used 
        for the return value/s of the method
    * type: idstring: optional, default "dict", the type of the data returned by the 
        method
* Children:
    * permission
    * parameter
    * return

* Element: permission
* Description: A permission that is required for using the method. Permissions are 
coupled to sessions, so it is not possible to satisfy a permission requirement if 
there is no session. Multiple permission elements means multiple permissions that 
each is required.
* Context: method
* Attributes:
    * name: varchar: the permission string
    * self: idstring: optional, default not present, defines the name of a parameter 
    that identifies a user; this permission is considered granted if the value of 
    the named parameter is the user name of the user owning the session.

* Element: parameter
* Description: A parameter for the method. Parameters may be identified by name or 
by order of argument in the generated language bindings. For example, a HTTP GET URL 
may use the name; a C++ method call may use order.
* Context: method
* Attributes:
    * name: idstring: the name of the parameter (used for language identifiers)
    * type: idstring: the type of the parameter

* Element: return
* Description: One return value from the method. These only make sense for methods of 
type "dict" (which is the default).
* Context: method[type='dict']
* Attributes:
    * name: idstring: The key within the dict for this return value.
    * type: idstring: The type of the value at this key.

Types
-----

* long: A 64-bit integer. JavaScript implementations may only support 53 bits of 
precision or may have to treat this as a string.
* bool: A boolean, value "true" or "false" (without quotes).
* idstring: string with max 32 characters, without whitespace or non-ASCII characters.
* password: string with max 64 characters, to be transferred using secure means only 
(HTTPS, etc)
* varchar: string with max 255 characters
* text: string with max 8191 characters
* email: string with max 64 characters, conforming to an email address regex
* list: a list of objects; mapped to JavaScript [] arrays
* dict: a key/value map from string-valued keys to objects; mapped to JavaScript objects

Todo
----

idstring is used both for language-style identifiers ([_a-z][_a-z0-9]*) and for general 
ID keys such as SHA hashes, base64-encoded integers, etc. These uses should be separated 
into two separate types.

User defined types.

Entity support for object/datastore mapping.


