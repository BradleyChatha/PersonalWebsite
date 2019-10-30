/*
Covers:
	- JSONValue's ctor being a template, but limited to only built in stuff.
	- Using static if + std.traits + is()
	- Only do built in types, enums(only names, not values), and using static assert.
	- An enum "variable"
	- Pretend dstring and wstring don't exist.
Excercises:
	- Add type checking using JSONValue.type
	- Add serialisation and deserialisation for the char type, hinting that it's probably best to store it as a string in the JSONValue.
	- [Advanced] By checking the JSONValue's type, allow a JSONValue holding a signed integer to be converted into an unsigned integer, and vice versa.
	- Use this as a way to practice writing unittests, using D's built in stuff.
NOTES:
	- The blog post ended up being too long, so enums should be moved into their own post.
	  To keep interest flowing, structs should come after this, so do enums after structs.
*/
import std.json : JSONValue;
import std.traits : isNumeric;

enum PersonType
{
	Student,
	Staff
}

void main()
{
	import std.stdio : writeln;

	JSONValue json;
	
	// Show serialisation with basic types
	
	json = serialise!int(420); // Start off with explicitly stating the type.
	writeln(json);
	
	json = serialise("Hello world!"); // Then use this as a way to demonstrate the compiler inferring the type.
	writeln(json);
	
	// Show deserialisation with basic types
	
	json = serialise(false);
	bool foo = deserialise!bool(json); // No UFCS the first time.
	writeln(foo);
	
	json = serialise("UFCS rocks!");
	string bar = json.deserialise!string(); // UFCS the second time though, to give as an example.
	writeln(bar);
	
	// Show serialisation and deserialisation with enums
	
	json = PersonType.Staff.serialise(); // UFCS show off again.
	writeln(json);
	
	// Show deserialisation with enums
	
	bar = json.deserialise!string(); // Default to UFCS
	writeln(bar);
}

enum isPrimitiveType(T) = (isNumeric!T || is(T == bool) || is(T == string)) && !is(T == enum); // Only add the !is(enum) part after showing why it's needed.

JSONValue serialise(T)(T value)
{
	static if(isPrimitiveType!T)
	{
		return JSONValue(value);
	}
	else static if(is(T == enum)) // Point out that it's "else STATIC if", not "else if"
	{
		import std.conv : to;
		return JSONValue(value.to!string());
	}
	else
	{
		enum NameOfType = __traits(identifier, T);
		static assert(false, "Don't know how to serialise type: " ~ NameOfType);
	}
}

T deserialise(T)(JSONValue json)
{
	static if(isPrimitiveType!T)
	{
		import std.traits : isIntegral, isFloatingPoint, isUnsigned;
		import std.conv   : to;
		
		static if(isIntegral!T)
		{
			static if(isUnsigned!T)
			{
				return json.uinteger.to!T();
			}
			else
			{
				return json.integer.to!T();
			}
		}
		else static if(isFloatingPoint!T)
		{
			return json.floating.to!T();
		}
		else static if(is(T == bool))
		{
			return json.boolean;
		}
		else static if(is(T == string))
		{
			return json.str;
		}
		else
		{
			static assert(false, "Internal error");
		}
	}
	else static if(is(T == enum))
	{
		import std.conv : to;
		return json.str.to!T;
	}
	else
	{
		enum NameOfType = __traits(identifier, T);
		static assert(false, "Don't know how to deserialise type: " ~ NameOfType);
	}
}