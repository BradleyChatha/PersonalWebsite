@date-created 31-10-2019@
@date-updated 07-02-2020@
@title Serialising structs@ 

The ability to serialiser basic types is fun and all, but it's not *useful*.
 
Naturally this means that the next step is to be able to serialise entire structs, which is actually somewhat useful.
While the end result of this serialiser is going to be relatively simple, this series
of posts should get you started with the knowledge you need to tailor it to your taste.

## static foreach
                    
Before we can start with structs there are a few features that need a dedicated explanation. One of these
features being `static foreach`.

A `static foreach` is a special compile-time-only version of a normal `foreach`
loop. Like all compile time constructs, it can only access data that exists at compile time.

A few things to note about `static foreach` are:

* It does **not** create a scope

* It is 'unrolled' at compile time

If you're confused about these points, please consider this code:

```
void main()
{
    import std.stdio : writeln;

    static foreach(number; [1, 2, 3, 4, 5])
    {
        writeln(number);
    }
}
```

When I say that a `static foreach` is 'unrolled' it means that the loop
is executed during compliation, and that the compiler will essentially copy-paste its body for each
iteration of the loop.

For example, the above example would unroll into the following code:

```
void main()
{
    import std.stdio : writeln;

    writeln(1);
    writeln(2);
    writeln(3);
    writeln(4);
    writeln(5);
}
```

Now, as I mentioned a `static foreach` won't create a scope, which can cause
issues once we start using variables:

```
void main()
{
    import std.stdio : writeln;

    static foreach(number; [1, 2, 3])
    {
        int veryComplexEquation = number + number;
        writeln(veryComplextEquation);
    }
}
```

Unrolls to:

```
void main()
{
    import std.stdio : writeln;

    int veryComplexEquation = 1 + 1;
    writeln(veryComplextEquation);

    // Error: Redefining variable 'veryComplexEquation'
    int veryComplexEquation = 2 + 2;
    writeln(veryComplextEquation);

    // Error: Redefining variable 'veryComplexEquation'
    int veryComplexEquation = 3 + 3;
    writeln(veryComplextEquation);
}
```

As you can see because we don't create a scope, and we use a variable inside
the `static foreach`, the variable we use is defined multiple times
which causes a compiler error.

While in this case we could just define our variable outside the loop, this isn't
always possible/clean to do so we will instead want to create a scope inside each
unrolling of the `static foreach`.

This is blindingly easy, instead of using singular curly braces ({ and }), double
them up instead ({{ and }}):

```
void main()
{
    import std.stdio : writeln;

    static foreach(number; [1, 2, 3])
    {{
        int veryComplexEquation = number + number;
        writeln(veryComplextEquation);
    }}
}
```

Unrolls to:

```
void main()
{
    import std.stdio : writeln;

    {
        int veryComplexEquation = 1 + 1;
        writeln(veryComplextEquation);
    }

    {
        int veryComplexEquation = 2 + 2;
        writeln(veryComplextEquation);
    }

    {
        int veryComplexEquation = 3 + 3;
        writeln(veryComplextEquation);
    }
}
```

And that's basically all you need to do to create a scope.
If it's not too clear how this is working, put the extra set of curly braces
on their own lines, and it might make more sense. It's worth noting this is also
how you can create a scope with `static if`.

## aliases

An `alias` is similar to a `typedef` from the C/C++ world but
instead of defining an entirely new type you, as the name suggests, create an alias to it instead.
Aliases can be used with any symbol (e.g. functions), not just types.

```
struct SomeStruct
{
    int a;
}

alias SStruct = SomeStruct;

void main()
{
    // Since it's just an alias, we can do things like "set alias to original type",
    // because they're literally the same thing.
    SStruct a = SomeStruct(200);
}
```

## Manifest constants and their ability to be templates

This often seems to confuse people at first, especially those coming from other languages.

A manifest constant can be seen as an immutable variable that exists only at compile time,
and that any uses of it inside of runtime portions of the code will cause the constant to duplicate
its value ('manifest') every time it is used.

Manifest constants are defined and used like a normal variable, except they are prefixed
by `enum`.

```
enum float PI = 3.14;

// Alternatively, let the compiler figure out the type by omitting the type completely.
enum AGE = 200;
```

They are a very important and useful in template code as they effectively act as
compile-time only variables, and are in fact one of the few ways to store values computed
at compile time while still allowing things like `static if` the ability to access them.

Be warned that there are [caveats](https://wiki.dlang.org/Declaring_constants#Caveats) 
to them when arrays are used, due to their nature of duplicating their value.

One very interesting feature of manifest constants is that they can actually be templates.

This allows for some interesting usages. For example, the templates we were using from
std.traits are all templated enums, which means we could even implement our own versions of templates like
std.traits#isBoolean  :

```
// I should note that std.traits.isBoolean does a bit more than this
// but that's besides the point.
enum myIsBoolean(T) = is(T == bool);

enum isStringABoolean  = myIsBoolean!string; // false
enum isBooleanABoolean = myIsBoolean!bool;   // true

static assert(!isStringABoolean);
static assert(isBooleanABoolean);
```

## Serialising a struct

Just as a reminder, here's our `serialise` function at the moment. Appreciate its
cuteness while it lasts:

```
JSONValue serialise(T)(T value)
{
    return JSONValue(value);
}
```

It's very simple and boring right now, effectively serving as a renamed constructor for `JSONValue`.
That's going to be changing of course.

The first thing we want to do is bring in a `static if` chain, so we
can show the user a custom message if they pass in something we can't handle yet.
Just like we do with the `deserialise` function.

### Serialisation - Type checking

At the moment let's just check if the value is a primitive type (bool, number, or string).

If we were to just stuff a single `static if` with all of these checks it'd be
a bit ugly to look at, not to mention annoying to maintain if we needed to reuse the checks
in another part of the code.

So instead, let's create a templated enum to make the code performing these
checks cleaner to use.

```
enum isPrimitiveType(T) = isNumeric!T || is(T == bool) || is(T == string);
```

Now, let's modify our `serialise` function to spit out an error
if this check fails:

```
enum isPrimitiveType(T) = isNumeric!T || is(T == bool) || is(T == string);

JSONValue serialise(T)(T value)
{
    static if(isPrimitiveType!T)
    {
        return JSONValue(value);
    }
    else
    {
        static assert(false, "Don't know how to serialise type: " ~ T.stringof);
    }
}
```

Much cleaner than stuffing the all of the type checks into a single `static if`.

### Serialisation - Iterating over a struct's members

To start we'll a `static if` for structs, and simply return an empty json value:

```
JSONValue serialise(T)(T value)
{
    static if(isPrimitiveType!T)
    { /**/ }
    else static if(is(T == struct))
    {
        JSONValue toReturn;

        return toReturn;
    }
    else
    { /**/ }
}
```

Next, we need a struct that we want to test with, so let's create one:

```
struct Person 
{
    string name;
    uint age;
}
```

The `serialise` function now requires the ability to inspect each member of the
struct, so it can determine what to do with them.

Structs and classes in D have a special `.tupleof` property, which returns
a special kind of compile time tuple (think of an immutable array that can contain
different types of data, including symbols) that contains all of the fields for the
struct/class.

So if we combine `.tupleof` and `static foreach` together then we can effectively create 
specialised code for each member field in the struct.

To make this more clear we can start off by creating an `alias` to the
member's type, then use `pragma(msg)` (akin to a compile-time std.stdio#writeln ) 
to print out the name of the type.

```
// https://run.dlang.io/is/NbfZ9i
JSONValue serialise(T)(T value)
{
    /* omitted for brevity */
    else static if(is(T == struct))
    {
        JSONValue toReturn;

        // Note that we're using double braces, so we can have a scope.
        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);
            pragma(msg, MemberType.stringof);
        }}

        return toReturn;
    }
    /**/
}
```

With the simple output of:

```
string
int
```

While we now have the type of the member, which we're not going to use until a bit later,
we also want to know the name of the member since otherwise we don't have a name to store its value with in JSON.

To do this, we can use one of the special
[__traits()](https://dlang.org/spec/traits.html)
which exposes many different characteristics about symbols.

For the case of getting the name of something, we can employ the use of
[__traits(identifier)](https://dlang.org/spec/traits.html#identifier), which when
given a symbol will return its name. This is different from `.stringof`
as that only works on types.

```
// https://run.dlang.io/is/ptqEnV
JSONValue serialise(T)(T value)
{
    /* omitted for brevity */
    else static if(is(T == struct))
    {
        JSONValue toReturn;

        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);

            // Much like `enum`, the compiler can figure out that the type is a string here
            const MemberName = __traits(identifier, member);

            pragma(msg, MemberName ~ " is a " ~ MemberType.stringof);
        }}

        return toReturn;
    }
    /**/
}
```

Which outputs:

```
name is a string
age is a int
```

### Serialisation - Serialising each member, and string mixins

Now that we have the name, type, and ability
to iterate each member of a struct, we finally have all the information needed
to be able to serialise each member into our `toReturn` value.

This is a fairly straight forward process: all we need to do is pass each member
to `serialise` as that already has all the logic in place,
and then place the return value into the `toReturn` value
with the member's name as the key and the return value as the value.

There's one thing you might be wondering however - how do I
actually *access* the member using the `value`
parameter that gets passed to the function.

The answer here, is to use a string [mixin](https://dlang.org/articles/mixin.html).

A string mixin is similar to the `#define` directive in C/C++ land
in that it can be used to turn a string into code,
except mixins are more limited in where you can place it yet more powerful by the fact
it uses strings directly.

So basically, we have our `value` parameter, and we have the `MemberName`
of each member of the value. By combining the two together in a mixin we can
get access to the member's value during runtime.

```
MemberType memberValue = mixin("value." ~ MemberName);
```

Which compiles into:

```
// For Person.name
string memberValue = value.name;

// For Person.age
int memberValue = value.age;
```

Relatively easy, right?

Anyway, all that's left to do is to serialise the `memberValue` and place it
into the `toReturn` value under the member's name.

```
JSONValue serialise(T)(T value)
{
    /* omitted for brevity */
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
    /**/
}
```

Now comes the time to test it!

```
// https://run.dlang.io/is/1WtNd0
void main()
{
    import std.stdio;
    
    // Instead of specifying the type ourselves as JSONValue, we can
    // instead use `auto`, which lets the compiler do it for us.
    auto json = serialise(Person("Bradley", 20));
    writeln(json);

    /*
    Output:
        {"age":20,"name":"Bradley"}
    */
}
```

Success!

## Deserialising a struct

For reference, this is what the `deserialise` function
looks like at the moment:

```
T deserialise(T)(JSONValue json)
{
    static if(is(T == string))
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
    else
    {
        static assert(false, "Don't know how to deserialise type: " ~ T.stringof);
    }
}
```

To be honest this is pretty much the exact same process, so
here's the code needed for our `deserialise` function
and I'll point out the differences afterwards.

```
T deserialise(T)(JSONValue json)
{
    /* omitted for brevity */
    else static if(is(T == struct))
    {
        T toReturn;

        static foreach(member; T.tupleof)
        {{
            alias MemberType = typeof(member);
            const MemberName = __traits(identifier, member);

            MemberType memberValue = deserialise!MemberType(json[MemberName]);

            // Since the mixin is the *entire* statement, we need to also include a semi-colon in the mixin.
            mixin("toReturn." ~ MemberName ~ " = memberValue;");
            // e.g
            // toReturn.name = memberValue;
            // toReturn.age = memberValue;
        }}

        return toReturn;
    }
    /**/
}
```

See how such (relatively) simple concepts (`static if/foreach`, `__traits`, `mixin`, etc.) can be
brought together to allow *easy yet powerful* code generation? This is in my opinion one of D's main selling points.

Anyway, here are the main differences:

* The type of `toReturn` is now `T`, the struct we're deserialising

* memberValue uses `json[MemberName]` to get the JSON version of the value,
  then calls `deserialise` to turn it into a `MemberType`
  
* We use a string mixin to generate the code to assign the value inside of `toReturn`
  to the deserialised `memberValue`
  
Let's give it a test:

```
// https://run.dlang.io/is/nCESzP
void main()
{
    import std.stdio : writeln;
    
    auto json = serialise(Person("Bradley", 20));
    writeln("As JSON: ", json);
    
    // writeln can pretty-print structs thanks to the same features we just used.
    auto person = deserialise!Person(json);
    writeln("As Person: ", person);

    /*
    Output:
        As JSON: {"age":20,"name":"Bradley"}
        As Person: Person("Bradley", 20)
    */
}
```

## Conclusion

We now have a slightly more useful serialiser due to the newly added
ability to serialise and deserialise structs.

If the ease and simplicity of what this code can accomplish doesn't open your eyes
to the capabilities of D's metaprogramming, then I advise you to go to a local [D
shelter](https://forum.dlang.org/) to pet and play with some lonely **D**evelopers ;).

## Excercises

### Excercise #1 - More validation

Fairly similar to the excercise from the previous post.

Basically, in the `deserialise` function ensure that the
`json` value passed to the function is an object, and not something
like a string or int.

Then, before you deserialise a member, check that it actually exists first inside
of the json value, and throw an exception otherwise.

You can use the `in` operator on `JSONValues`, which
returns a pointer, e.g. `JSONValue* ptr = ("name" in json);`

If this pointer is `null`, then the key doesn't exist. If it does exist,
then you can continue with the deserialisation.

Like before, while `JSONValue` will technically do this for you, it's a good way to
practice checking for and handling things like this.

Test case:

```
// https://run.dlang.io/is/HhxkdZ
void main()
{
    import std.exception : assertThrown, assertNotThrown;

    JSONValue json;
    json["age"] = JSONValue(200);

    assertThrown(json.deserialise!Person());
    assertThrown(JSONValue(null).deserialise!Person());

    json["name"] = JSONValue("Bradley");
    assertNotThrown(json.deserialise!Person());
}
```