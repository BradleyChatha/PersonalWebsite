@date-created 31-10-2019@
@date-updated 07-02-2020@
@title Using UDAs for customisation@ 
@seo-tag uda-customise-at-sign@

At the moment our serialiser is still really basic and rigid, without really any way to customise
how it serialises/deserialises our data.

So the topic for this post is to explore using [UDAs](https://dlang.org/spec/attribute.html#uda) 
(User Defined Attributes) to allow a clean way of customising the functionality of our serialiser.

## Basic usage of UDAs

A UDA in D can be pretty much anything such as a struct, any primitive type, you can
even use functions that are executed at compile-time which return a value to be used as a UDA.

To attach a UDA onto something you use the form `@TypeName`, 
and use [__traits(getAttributes)](https://dlang.org/spec/traits.html#getAttributes)
to gain access to these UDAs, e.g:
```
// https://godbolt.org/z/69d7am
enum AnEnum{a}
struct AStruct{}
class AClass{}

int FunctionThatReturnsTheUDAValue(){ return 0; }

@AnEnum
@AStruct
@AClass
@FunctionThatReturnsTheUDAValue
struct Test{}

void main()
{
    import std.traits;

    // __traits(getAttributes) is another special __trait
    // which returns a tuple of all UDAs on a symbol.
    static foreach(uda; __traits(getAttributes, Test))
        pragma(msg, uda);

    /* 
        Output:
            AnEnum
            AStruct
            AClass
            0
    */
}
```

### How to access UDAs on a symbol

There are a few ways to do this. One way that the above example shows is to use
the [__traits(getAttributes)](https://dlang.org/spec/traits.html#getAttributes) function
to get a tuple of every UDA on a symbol.

While `__traits(getAttributes)` is the most flexible way to mess with UDAs, and
is how the next few templates below are able to function in the first place, it can be a bit
too much of a hassle to work with when all you want to do are simple checks such as "Is this struct marked
`@Special`", or "Get me ONLY the `@Special` UDA from this struct".

In comes std.traits#hasUDA and std.traits#getUDAs.
Special mention to std.traits#getSymbolsByUDA as I won't be using it in this post, but it's still very useful. 
I feel a quick example should be enough of a demonstration of their usage:

```
// https://godbolt.org/z/Tszj-k
struct UsefulUDA
{
    string some;
    int data;
}

struct NeverUsedUDA
{
}

struct MultiUDA
{
    string data;
}

@UsefulUDA("Foo", 21)
@MultiUDA("Use")
@MultiUDA("Me")
@(MultiUDA("Multiple"), MultiUDA("Times"))
struct MyStruct
{

}

void main()
{
    import std.traits : hasUDA, getUDAs;
    import std.stdio  : writeln, write;

    writeln("Does struct have @UsefulUDA: ", hasUDA!(MyStruct, UsefulUDA));
    writeln("What about @NeverUsedUDA:    ", hasUDA!(MyStruct, NeverUsedUDA));

    // Since UDAs can be used multiple times, getUDAs will return a tuple of ALL
    // UDAs that you ask it for.
    // So if you only want a single one, you'll have to get the [0]th one.
    const UsefulUDA useful = getUDAs!(MyStruct, UsefulUDA)[0];
    writeln(useful);

    // And of course, you can iterate over the results for UDAs that occur multiple times.
    static foreach(uda; getUDAs!(MyStruct, MultiUDA))
        write(uda.data, " ");

    /* 
        Output:  
            Does struct have @UsefulUDA: true
            What about @NeverUsedUDA:    false
            const(UsefulUDA)("Foo", 21)
            Use Me Multiple Times 
    */
}
```

Using this newly learned magic, we'll be upgrading our serialiser with the following three UDAs:

* `@Ignore` - Completely ignore a field.

* `@Name` - Set a custom name to serialise a field as.

* `@ByValue` - Serialise an enum by value, rather than by name.

## Creating the UDAs, and a struct to test with

All of our UDAs will be `structs` as that's just how I roll with UDAs.

We will also need a struct to test our UDAs with, so we'll create a copy of our
`Person` struct and give each field a UDA:

```
struct ByValue {}
struct Ignore {}

struct Name
{
    string name;
}

// Keep this version of `Person` around, as I'll use it to compare the output between
// using UDAs, and not using them.
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
```

## Implementing the `@Ignore` UDA

### Serialise support

Reference for the current (and relevent parts of) `serialise` function:

```
JSONValue serialise(T)(T value)
{    
    /* omitted for brevity */
    else static if(is(T == struct) || is(T == class))
    {
        JSONValue toReturn;

        static if(is(T == class))
        { /**/ }

        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);
            const MemberName = __traits(identifier, member);

            MemberType memberValue = mixin("value." ~ MemberName);
            toReturn[MemberName] = serialise(memberValue);
        }}

        return toReturn;
    }
    /**/
}    
```

Your first thought for implementing `@Ignore` might be to use
`continue` if the field has the UDA attached to it.

However, `static foreach` and `continue` don't exactly
work well together, or really at all (think about how `static foreach` works,
specifically that it unrolls itself to generate code).

I should mention that *sometimes* you can get rid of a the `static` part of the
`static foreach` while still being able to use and access compile-time features/data, as well as 
having `continue` work as expected.

This is because `static foreach` is a relatively new feature of D, so back in ye olde times you had
to use a special `static-but-not-static foreach` which would only work when using a `foreach` with a
compile-time tuple. It does have its own issues though, so I'd adivse to stick with normal `static foreach`.

Anyway, because of `continue` not working
this means we're going to have to be a bit more creative. Basically, we'll lock the last line of the `static foreach` 
(which performs the actual serialisation) behind a `static if` that checks whether the field is to be ignored or not:

```
JSONValue serialise(T)(T value)
{    
    /* omitted for brevity */
    else static if(is(T == struct) || is(T == class))
    {
        JSONValue toReturn;

        static if(is(T == class))
        { /**/ }

        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);
            const MemberName = __traits(identifier, member);

            MemberType memberValue = mixin("value." ~ MemberName);

            // An annoying thing to have to do, but worth the gains that static foreach brings us.
            static if(!hasUDA!(member, Ignore))
            {
                toReturn[MemberName] = serialise(memberValue);
            }
        }}

        return toReturn;
    }
    /**/
}
```

This is a bit of an iffy way to go about things compared to simply being able to `continue`
like in a normal loop, but it does the job.

### Deserialise support

Reference of the current `deserialise` function:

```
T deserialise(T)(JSONValue json)
{    
    /**/
    else static if(is(T == struct) || is(T == class))
    {
        static if(is(T == class))
        {
            /**/

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
        { /**/ }
        else
        {
            static foreach(member; T.tupleof)
            {{
                alias MemberType = typeof(member);
                const MemberName = __traits(identifier, member);

                MemberType memberValue = deserialise!MemberType(toReturn[MemberName]);

                mixin("toReturn." ~ MemberName ~ " = memberValue;");
            }}

            return toReturn;
        }
    }
    /**/
}
```

This is pretty much the same deal: lock the code that does the actual deserialisation behind
a `static if` that checks for the `@Ignore` UDA:

```
T deserialise(T)(JSONValue json)
{    
    /**/
    else static if(is(T == struct) || is(T == class))
    {
        /**/
        else
        {
            static foreach(member; T.tupleof)
            {{
                alias MemberType = typeof(member);
                const MemberName = __traits(identifier, member);

                static if(!hasUDA!(member, Ignore)) // This can definitely get annoying for larger blocks of code
                {
                    MemberType memberValue = deserialise!MemberType(json[MemberName]);

                    mixin("toReturn." ~ MemberName ~ " = memberValue;");
                }
            }}

        return toReturn;
        }
    }
    /**/
}
```

### Test

While testing the UDAs, the tests will show the output from both `Person` and
`PersonWithUDAs` to make the differences more obvious.

```
// https://godbolt.org/z/VYWCjw
void main()
{
    import std.stdio : writeln;

    auto person     = Person("Bradley", 20, PersonType.Student);
    auto personUDA  = PersonWithUDAs("Bradley", 20, PersonType.Student);
    writeln(person.serialise());
    writeln(personUDA.serialise());

    writeln(person.serialise().deserialise!Person());
    writeln(personUDA.serialise().deserialise!PersonWithUDAs());

    /* 
        Output:
            {"age":20,"name":"Bradley","type":"Student"}
            {"age":20,"type":"Student"}
            Person("Bradley", 20, Student)
            PersonWithUDAs("", 20, Student)
    */
}
```

As you can see in the JSON output for `PersonWithUDAs`, the "name"
field is completely missing, and when we serialise it back into a struct the "name"
field is left as `string.init` since we never give it a value.

## Implementing the `@Name` UDA

### Serialise support

Adding support in the `serialise` function isn't anything too
difficult. If the field has `@Name` attached to it then
we use the string given by that UDA as the value's key, instead of
using the field's name.

We will also store the serialised value in its own variable in preparation
for the next UDA:

```
JSONValue serialise(T)(T value)
{    
    /* omitted for brevity */
    else static if(is(T == struct) || is(T == class))
    {
        JSONValue toReturn;

        static if(is(T == class))
        { /**/ }

            static foreach(member; T.tupleof)
            {{
                alias MemberType = typeof(member);
                const MemberName = __traits(identifier, member);

                MemberType memberValue = mixin("value." ~ MemberName);

                static if(!hasUDA!(member, Ignore))
                {
                    JSONValue serialised = serialise(memberValue); // Store the value in a variable for future purposes

                    static if(hasUDA!(member, Name)) // Use a custom name if needed.
                    {
                        const SerialiseName = getUDAs!(member, Name)[0].name;
                        toReturn[SerialiseName] = serialised;
                    }
                    else // Otherwise just use the field's name.
                    {
                        toReturn[MemberName] = serialised;
                    }
                }
            }}

        return toReturn;
    }
    /**/
}
```

### Deserialise support

Again this isn't too difficult - we will store the value to deserialise in its own variable and use
`static if` to determine whether to use the field's actual name, or
whatever name was provided by `@Name`.

```
T deserialise(T)(JSONValue json)
{    
    /**/
    else static if(is(T == struct) || is(T == class))
    {
        /**/
        else
        {
            static foreach(member; T.tupleof)
            {{
                alias MemberType = typeof(member);
                const MemberName = __traits(identifier, member);

                static if(!hasUDA!(member, Ignore))
                {
                    static if(hasUDA!(member, Name))
                    {
                        const SerialiseName = getUDAs!(member, Name)[0].name; 
                        JSONValue value = json[SerialiseName];
                    }
                    else
                    {
                        JSONValue value = json[MemberName];
                    }

                    MemberType memberValue = deserialise!MemberType(value);

                    mixin("toReturn." ~ MemberName ~ " = memberValue;");
                }
            }}

            return toReturn;
        }
    }
    /**/
}    
```

### Test

The testing code is the same as before, but now that we've implemented support for
`@Name`, the output is a bit different:

```
// https://godbolt.org/z/_n6fbB
void main()
{
    import std.stdio : writeln;

    auto person     = Person("Bradley", 20, PersonType.Student);
    auto personUDA  = PersonWithUDAs("Bradley", 20, PersonType.Student);
    writeln(person.serialise());
    writeln(personUDA.serialise());

    writeln(person.serialise().deserialise!Person());
    writeln(personUDA.serialise().deserialise!PersonWithUDAs());

    /* 
        Output:
            {"age":20,"name":"Bradley","type":"Student"}
            {"type":"Student","yearsOld":20}
            Person("Bradley", 20, Student)
            PersonWithUDAs("", 20, Student)
    */
}
```

Just as we had hoped, the JSON output for `PersonWithUDAs` now uses
"yearsOld" instead of "age" to store the person's age.

## Implementing the `@ByValue` UDA
### Serialise support

For serialisation we need to add a `static if` that checks for the
`@ByValue` UDA (and for good measure, making sure it's an enum), and
then pass the value directly to the constructor of `JSONValue` to serialise its value:

```
JSONValue serialise(T)(T value)
{    
    /* omitted for brevity */
    else static if(is(T == struct) || is(T == class))
    {
        /**/
        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);
            const MemberName = __traits(identifier, member);

            MemberType memberValue = mixin("value." ~ MemberName);

            static if(!hasUDA!(member, Ignore))
            {
                // This is why we started to store the value into its own variable.
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
        /**/
    }
    /**/
}  
```

### Deserialise support

To finish off with our last UDA, we need to, surprise surprise, use `static if`
yet again to check for `@ByValue` and if the field has the UDA
then instead of recursively calling the `deserialise` function we will
instead directly convert the `JSONValue` into the field's type via the wonderful
std.conv#to function.

Not only can it convert by name, it can also convert by value!

I'd like to note that in D an enum's base type isn't limited to just numeric types
such as `int` or `uint`, but to keep things simple we'll just assume that the enum's
base type is `int`.

```
T deserialise(T)(JSONValue json)
{    
    /**/
    else static if(is(T == struct) || is(T == class))
    {
        /**/
        else
        {
            static foreach(member; T.tupleof)
            {{
                alias MemberType = typeof(member);
                const MemberName = __traits(identifier, member);

                static if(!hasUDA!(member, Ignore))
                {
                    static if(hasUDA!(member, Name))
                    {
                        const SerialiseName = getUDAs!(member, Name)[0].name; 
                        JSONValue value = json[SerialiseName];
                    }
                    else
                    {
                        JSONValue value = json[MemberName];
                    }

                    // We can't use `deserialise` again, as that assumes enums are stored by name, as strings.
                    // So we have to go this route.
                    static if(hasUDA!(member, ByValue))
                    {
                        MemberType memberValue = value.integer.to!MemberType();
                    }
                    else
                    {
                        MemberType memberValue = deserialise!MemberType(value);
                    }

                    mixin("toReturn." ~ MemberName ~ " = memberValue;");
                }
            }}

            return toReturn;
        }
    }
    /**/
}    
```

### Test

Just like with `@Name` we'll use the same testing code as before, but
now that we've implemented `@ByValue` we should get different results:

```
// https://godbolt.org/z/TXLkMC
void main()
{
    import std.stdio : writeln;

    auto person     = Person("Bradley", 20, PersonType.Student);
    auto personUDA  = PersonWithUDAs("Bradley", 20, PersonType.Student);
    writeln(person.serialise());
    writeln(personUDA.serialise());

    writeln(person.serialise().deserialise!Person());
    writeln(personUDA.serialise().deserialise!PersonWithUDAs());

    /* 
        Output:
            {"age":20,"name":"Bradley","type":"Student"}
            {"type":1,"yearsOld":20}
            Person("Bradley", 20, Student)
            PersonWithUDAs("", 20, Student)
    */
}
```

And voila: the enum is now stored by its value instead of its name.

## Conclusion

Via the power of UDAs we can now customise to a basic extent the way our serialiser works.

One of the downsides of course is that our single-function `serialise` and
`deserialise` functions are becoming very unweidly and will continue to do so
as more and more UDAs are added (e.g. via the excercises below), so I encourage you to
experiment with [a different way](/Blog/JsonSerialiser/1)
of organising these two functions, or just a better way to organise the entire project as a whole.

There will likely be a large gap in time between this post and the next (which may or may not be the last of
this series) as I will be improving certain aspects of the website now that I have a few posts to design around,
putting more time into other projects, etc.

In the meantime I hope this series of posts have provided a decent enough introduction into the
myriad of metaprogramming features that D can provide you, to the extent that you're able to recognise the sheer
power, productivity benefits, and also possibly the downsides that these features can gift you.

"With great power comes great responsibilty."

## Excercises

### Excercise #1 - Implement more UDAs

There won't be a test case for this one, as it completely depends on what UDAs you decide
to implement.

Here are a few ideas:

* `@MaxLength and @MinLength` - Arrays must have at least/at most a certain length.

* `@MatchesRegex` - Strings must match the provided regex. (See also std.regex )

* `@InRange` - Numeric types must be within/outside/whatever a certain range.

* `@Description` - Serialisation will output a description for a field. A bit iffy when using JSON, but possible.

* `@Names` - A list of possible names that the value could be serialised/deserialised as.

* `@CaseInsensitive` - A value could be serialised as "KEY" or "kEy" and both would work.
