/*
	Covers:
		- Using KeyType, ValueType, and ElementType.
		- Using isDynamicArray, isAssociativeArray, and for the excercise: isStaticArray
		- Probably not much else, since we've been through a huge chunk of the features the rest of this code will use.
	
	Excercise:
		- Implement support for static arrays.
			- If the JSON has *more* values than we want, throw an exception.
			- If the JSON has *less* values than we want, then for value types use Type.init, for classes use null.
*/
import std.json, std.traits, std.conv, std.range;

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

void main()
{
	import std.stdio : writeln;
	
	auto json = ["a", "b", "c"].serialise();
	writeln(json);
	writeln(json.deserialise!(string[]));
	
	json = 
	[
		"bradley": Person("Bradley", 20, PersonType.Student), 
		"andy":    Person("Andy", 100, PersonType.Staff)
	].serialise();
	writeln(json);
	writeln(json.deserialise!(Person[string]));
}

enum HasStaticDeserialiseFunc(T) = __traits(compiles, { T obj = T.deserialise(JSONValue()); });  
enum HasDefaultCtor(T)           = __traits(compiles, new T());
enum isPrimitiveType(T)          = !is(T == enum) && (isNumeric!T || is(T == bool) || is(T == string));  

JSONValue serialise(T)(T value)
{    
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
            toReturn[MemberName] = serialise(memberValue);
        }}

        return toReturn;
    }
	else static if(isDynamicArray!T)
	{
		JSONValue toReturn = parseJSON("[]");
		
		foreach(element; value)
			toReturn.array ~= serialise(element);
		
		return toReturn;
	}
	else static if(isAssociativeArray!T)
	{
		JSONValue toReturn;
		
		alias KeyT = KeyType!T;		
		static assert(is(KeyT == string), "Only string keys are supported, not: " ~ KeyT.stringof);
		
		foreach(key, element; value)
			toReturn[key] = serialise(element);
			
		return toReturn;
	}
    else
    {
        static assert(false, "Don't know how to serialise type: " ~ T.stringof);
    }
}

T deserialise(T)(JSONValue json)
{    
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
        static if(is(T == class))
        {
            static assert(HasDefaultCtor!T || HasStaticDeserialiseFunc!T, 
                "The class `" ~ T.stringof ~ "` requires a default constructor or a function matching "
               ~"`static " ~ T.stringof ~ " deserialise(JSONValue)`"
            );

            if(json.type == JSONType.null_)
                return null;

            static if(HasDefaultCtor!T)
            {
                T toReturn = new T();
            }
        }
        else
        {
            T toReturn;
        }

        static if(HasStaticDeserialiseFunc!T)
        {
            return T.deserialise(json);
        }
        else
        {
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
	else static if(isDynamicArray!T)
	{
		T toReturn;
		
		alias ElementT = ElementType!T;
		
		foreach(element; json.array)
			toReturn ~= deserialise!ElementT(element);
		
		return toReturn;
	}
	else static if(isAssociativeArray!T)
	{
		T toReturn;
		
		alias KeyT   = KeyType!T;
		alias ValueT = ValueType!T;
		
		static assert(is(KeyT == string), "Only string keys are supported, not: " ~ KeyT.stringof);
		
		foreach(key, element; json.object)
			toReturn[key] = deserialise!ValueT(element);
			
		return toReturn;
	}
    else
    {
        static assert(false, "Don't know how to deserialise type: " ~ T.stringof);
    }
}