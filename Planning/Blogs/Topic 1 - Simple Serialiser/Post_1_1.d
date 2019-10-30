/*
Covers:
	Alternate way of structuring the functions.
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
	
	json = serialise!int(420);
	writeln(json);
	
	json = serialise("Hello world!");
	writeln(json);
	
	json = serialise(false);
	bool foo = deserialise!bool(json);
	writeln(foo);
	
	json = serialise("UFCS rocks!");
	string bar = json.deserialise!string();
	writeln(bar);
	
	json = PersonType.Staff.serialise();
	writeln(json);
	
	bar = json.deserialise!string();
	writeln(bar);
}

enum isPrimitiveType(T) = (isNumeric!T || is(T == bool) || is(T == string)) && !is(T == enum);

JSONValue serialise(T)(T value)
if(isPrimitiveType!T)
{
	return JSONValue(value);
}

JSONValue serialise(T)(T value)
if(is(T == enum))
{
	import std.conv : to;
	return JSONValue(value.to!string());
}

// PrimitveType design #1, "isPrimitiveType" as the main grouping, splitting off into static ifs for sub grouping.
T deserialise(T)(JSONValue json)
if(isPrimitiveType!T && false) // && false is just so it won't be used when testing the example, don't include it in the main output.
{
	import std.traits : isIntegral, isFloatingPoint;
	import std.conv   : to;
	
	static if(isIntegral!T)
	{
		return json.integer.to!T();
	}
	else static if(isFloatingPoint!T)
	{
		return json.floating.to!T();
	}
	// Yada, yada
	else static assert(false);
}

// PrimitveType design #2, each sub group is done with its own function.
import std.traits : isIntegral, isFloatingPoint, isUnsigned;
import std.conv   : to; // Explain this is for readability, but I encourage imports to be scoped where possible.

T deserialise(T)(JSONValue json)
if(isPrimitiveType!T && isIntegral!T && !isUnsigned!T)
{
	return json.integer.to!T();
}

T deserialise(T)(JSONValue json)
if(isPrimitiveType!T && isIntegral!T && isUnsigned!T)
{
	return json.uinteger.to!T();
}

T deserialise(T)(JSONValue json)
if(isPrimitiveType!T && isFloatingPoint!T)
{
	return json.floating.to!T();
}

T deserialise(T)(JSONValue json)
if(isPrimitiveType!T && is(T == bool))
{
	return json.boolean;
}

T deserialise(T)(JSONValue json)
if(isPrimitiveType!T && is(T == string))
{
	return json.str;
}

T deserialise(T)(JSONValue json)
if(is(T == enum))
{
	return json.str.to!T();
}