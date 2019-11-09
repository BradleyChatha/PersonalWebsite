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

void main()
{
    import std.stdio : writeln;
    
    auto json = serialise(PersonType.Student);
    writeln(json);
    writeln(json.deserialise!PersonType());
    
    json = serialise(Person("Bradley", 20, PersonType.Student));
    writeln(json);
    writeln(json.deserialise!Person());
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
		return JSONValue(value.to!string());
	}
    else static if(is(T == struct))
    {
        JSONValue toReturn;

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
    else static if(is(T == struct))
    {
        T toReturn;

        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);
            const MemberName = __traits(identifier, member);

            MemberType memberValue = deserialise!MemberType(json[MemberName]);
            
           	mixin("toReturn." ~ MemberName ~ " = memberValue;");
        }}

        return toReturn;
    }
    else
    {
        static assert(false, "Don't know how to deserialise type: " ~ T.stringof);
    }
}