/*
Covers:
	- static foreach and struct/class.tupleof to perform reflection
	- Using alias
	- String mixins
*/
import std.json   : JSONValue;
import std.traits : isNumeric;

enum PersonType
{
	Student,
	Staff
}

struct Person 
{
	string name;
	ubyte age;
	PersonType type;
	
	// Needed so I can show that .tupleof needs to filter out things like function symbols.
	bool isTeenager()
	{
		return this.age >= 13 && this.age <= 18;
	}
}

void main()
{
	import std.stdio : writeln;

	JSONValue json = Person("Bradley", 20, PersonType.Student).serialise();
	writeln(json);
	
	// Show off writeln being able to format structs.
	Person person = json.deserialise!Person();
	writeln(person);
}

enum isPrimitiveType(T) = (isNumeric!T || is(T == bool) || is(T == string)) && !is(T == enum);

JSONValue serialise(T)(T value)
{
	static if(isPrimitiveType!T)
	{
		return JSONValue(value);
	}
	else static if(is(T == enum))
	{
		import std.conv : to;
		return JSONValue(value.to!string());
	}
	else static if(is(T == struct) || is(T == class)) // Point out is() can also detect structs and classes. *START* with structs, add classes at the end.
	{
		static if(is(T == class))
		{
			if(value == null)
				return null;
		}
	
		JSONValue json;
		
		// Explain that static foreach doesn't create a scope.
		// Provide an example of it's output.
		// Show how you can create a scope if needed.
		// Also provide an example of that output.
		static foreach(member; T.tupleof) // Explain that .tupleof is essentially a tuple of the symbols of T's members.
		{{
			import std.traits : isSomeFunction;
			
			pragma(msg, __traits(identifier, member)); // Show this once as an example of using __traits(identifier) and pragma(msg), but delete after.

			// Show that you can create aliases inside of functions, and that they can essentially act as "variables" for symbols.
			alias MemberType = typeof(member);
			enum MemberName  = __traits(identifier, member); // *Should* use const here, but for simplicity's sake don't explain it yet.
			
			// NOTE: Make sure to show an example of what happens when you *don't* do this check.
			//       Also note that there are other things (like nested types) that can cause failures, but don't go over how to
			//       check for them yet, as that's for a more advanced tutorial.
			static if(!isSomeFunction!MemberType)
			{
				// Explain how string mixins work.
				json[MemberName] = serialise(mixin("value."~MemberName));
			}
		}}
		
		return json;
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
	else static if(is(T == struct) || is(T == class))
	{
		// Remind that static if does not create a scope.
		static if(is(T == struct))
			T toReturn;
		else
			T toReturn = new T();
		
		// Explain that they can design their code in different ways to reduce code replication,
		// but I choose this way just to keep the examples simple.
		static foreach(member; T.tupleof)
		{{
			import std.traits : isSomeFunction;
		
			alias MemberType = typeof(member);
			enum MemberName  = __traits(identifier, member);
			
			static if(!isSomeFunction!MemberType)
			{
				// Explain that this could all be done on a single line, but I've split them up for readability.
				auto value = deserialise!MemberType(json[MemberName]);
				mixin("toReturn."~MemberName~" = value;"); // Step 2, using mixin to set the value in toReturn. Also we have to mixin a semi-colon.
			}
		}}
		
		return toReturn;
	}
	else
	{
		enum NameOfType = __traits(identifier, T);
		static assert(false, "Don't know how to deserialise type: " ~ NameOfType);
	}
}