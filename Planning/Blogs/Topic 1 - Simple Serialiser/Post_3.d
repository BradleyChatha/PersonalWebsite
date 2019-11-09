/*
Covers:
	- Converting an enum to a string and back (via its name). a.k.a, std.conv.to is fun
	- Why is(T == enum) is needed *before* or alongside other type checks (where the enum check is negated).
	- Explain a struct's `.init`, why default ctors don't exist for structs, and that structs can typically be used without use of constructor, without much issue.
		- However, classes don't get these things, so we *need* a default ctor otherwise we can't create an instance of the class.
		- So we get some fun with __traits(compiles)
*/
import std.json, std.traits, std.conv;

enum PersonType
{
	Unknown,
	Student,
	Staff
}

struct Person
{
    string name;
    int age;
	PersonType type;
}

class PersonClass
{
	string name;
    int age;
	PersonType type;
	
	this(string name, int age, PersonType type)
	{
		this.name = name;
		this.age = age;
		this.type = type;
	}	
	
	override string toString()
	{
		import std.format : format;
		return format("%s, %s, %s", this.name, this.age, this.type);
	}
	
	static PersonClass deserialise(JSONValue value)
	{
		return new PersonClass(
			value["name"].deserialise!string(), 
			value["age"].deserialise!int(), 
			value["type"].deserialise!PersonType()
		);
	}
}

void main()
{
	import std.stdio;
	
	auto json = (new PersonClass("Bradley", 20, PersonType.Student)).serialise();
	writeln(json);
	writeln(json.deserialise!PersonClass());
	
	writeln(JSONValue(null).deserialise!PersonClass());
}

enum isPrimitiveType(T) = (isNumeric!T || is(T == bool) || is(T == string)) && !is(T == enum);

JSONValue serialise(T)(T value)
{
	// Here, we can do the enum check *after* the primitive type check, as the primitive check
	// specifically won't work on enums.
    static if(isPrimitiveType!T)
    {
        return JSONValue(value);
    }
	else static if(is(T == enum))
	{
		return JSONValue(value.to!string());
	}
    else static if(is(T == struct) || is(T == class))
    {
        JSONValue toReturn;
		
		// Classes can be null. JSON supports null. So we do the only logical thing here
		static if(is(T == class))
		{
			if(value is null)
			{
				return JSONValue(null);
			}
		}

        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);
            const MemberName = __traits(identifier, member);

            MemberType memberValue = mixin("value." ~ MemberName);

            // toReturn is a JSON object. Use MemberName as the key. Serialise memberValue as the value.
            toReturn[MemberName] = serialise(memberValue);
        }}

        return toReturn;
    }
    else
    {
        static assert(false, "Don't know how to serialise type: " ~ T.stringof);
    }
}

T deserialise(T)(JSONValue json)
{
	// However, here we're just going to put the enum check first, so it takes
	// priority over the conflicting checks below it.
	static if(is(T == enum))
	{
		return json.str.to!T();
	}
    else static if(is(T == string))
    {
        return json.str;
    }
    else static if(is(T == bool))
    {
        return json.boolean;
    }
    else static if(isFloatingPoint!T)
    {
        return json.floating.to!T();
    }    
    else static if(isSigned!T)
    {
        return json.integer.to!T();
    }
    else static if(isUnsigned!T)
    {
        return json.uinteger.to!T();
    }
    else static if(is(T == struct) || is(T == class))
    {
		enum HasDefaultCtor 	      = __traits(compiles, new T());
		enum HasStaticDeserialiseFunc = __traits(compiles, { T obj = T.deserialise(JSONValue()); });
	
		static if(!HasDefaultCtor && !HasStaticDeserialiseFunc)
		{
			static assert(false, 
				"class '"~T.stringof~"' must contain either a default ctor, or a static function matching: `"~T.stringof~" deserialise(JSONValue)`."
			);
		}
		
		static if(is(T == class))
		{
			if(json.type == JSONType.null_)
				return null;
		}
		
		static if(HasStaticDeserialiseFunc)
		{
			return T.deserialise(json);
		}
		else
		{
			static if(is(T == class))
			{			
				static if(HasDefaultCtor)
				{
					T toReturn = new T();
				}
				else
					static assert(false, "Internal error");
			}
			else
			{
				T toReturn;
			}
			
			static foreach(member; T.tupleof)
			{{
				alias MemberType = typeof(member);
				const MemberName = __traits(identifier, member);

				MemberType memberValue = deserialise!MemberType(json[MemberName]);
				
				mixin("toReturn." ~ MemberName ~ " = memberValue;");
			}}

			return toReturn;
		}
    }
    else
    {
        static assert(false, "Don't know how to deserialise type: " ~ T.stringof);
    }
}