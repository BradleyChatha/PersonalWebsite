/*
	Covers:
		- Using mixin templates
*/
import std.json, std.traits, std.conv, std.range, std.meta;

mixin template AutoStaticDeserialise()
{
    private alias ThisType = typeof(this);
    static assert(is(ThisType == class), "This mixin only works with classes.");
    static assert(HasDefaultCtor!ThisType, "This function relies on the class having a default constructor.");

    public static ThisType deserialise(JSONValue json)
    {
        if(json.type == JSONType.null_)
        {
            return null;
        }

        auto instance = new ThisType();

        static foreach(member; ThisType.tupleof)
        {{
            static if(hasUDA!(member, Allow))
            {
                alias MemberType = typeof(member);
                const MemberName = __traits(identifier, member);

                MemberType memberValue = json[MemberName].deserialise!MemberType();
                mixin("instance." ~ MemberName ~ " = memberValue;");
            }
        }}

        return instance;
    }
}

class PersonClass
{
    mixin AutoStaticDeserialise;

    private
    {
        @Allow
        string name;

        @Allow
        int age;

        @Allow
        PersonType type;

        bool superSecretField;
    }

    this()
    {
    }

    this(string name, int age, PersonType type)
    {
        this.name = name;
        this.age = age;
        this.type = type;
    }

    override string toString()
    {
        import std.format : format;
        return "I am %s, I am %s years old, and I'm a %s.".format(this.name, this.age, this.type);
    }
}

struct ByValue {}
struct Ignore {}
struct Allow {}

struct Name
{
	string name;
}

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

struct PersonWithUDAs
{
    @Ignore
	string name;

    @Name("yearsOld")
	int age;

    @ByValue
	PersonType type;
}

void main()
{
    import std.stdio : writeln;
    
    auto person = new PersonClass("Bradley", 21, PersonType.Student);
    writeln(deserialise!PersonClass(parseJSON(`{ "name": "Bradley", "age": 21, "type": "Student" }`)));
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

            static if(!hasUDA!(member, Ignore))
            {
                static if(hasUDA!(member, ByValue) && is(MemberType == enum))
                {
                    JSONValue serialised = JSONValue(memberValue);
                }
                else
                {
                    JSONValue serialised = serialise(memberValue);
                }

                static if(hasUDA!(member, Name))
                {
                    const SerialiseName = getUDAs!(member, Name)[0].name;
                    toReturn[SerialiseName] = serialised;
                }
                else
                {
                    toReturn[MemberName] = serialised;
                }
            }
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
            {
                return null;
            }

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

                static if(hasUDA!(member, Name))
                {
                    const SerialiseName = getUDAs!(member, Name)[0].name; 
                }
                else
                {
                    const SerialiseName = MemberName;
                }

                static if(!hasUDA!(member, Ignore))
                {
                    static if(hasUDA!(member, ByValue) && is(MemberType == enum))
                    {
                        MemberType memberValue = json[SerialiseName].integer.to!MemberType();
                    }
                    else
                    {
                        MemberType memberValue = deserialise!MemberType(json[SerialiseName]);
                    }

                    mixin("toReturn." ~ MemberName ~ " = memberValue;");
                }
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