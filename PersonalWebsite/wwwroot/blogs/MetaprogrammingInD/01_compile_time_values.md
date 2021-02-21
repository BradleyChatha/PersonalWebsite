@date-created 02-11-2020@
@date-updated 02-11-2020@
@title Metaprograming in D - Compile Time Values@
@seo-tag d-metaprogramming-compile-time-values-enum-alias@
@seo-url /dlang-metaprogramming-compile-time-values@

Have you ever wondered about how serialisers (or other pieces of code) perform their 'magic'? Are you someone
who wants to learn how to perform such seemingly arcane feats yourself? Or are you simply someone who wants to learn more about D?

Regardless of your reason for being here, with the power of D I'll be taking you through the steps of creating a basic
JSON Serialiser that uses compile-time introspection/reflection to serialise any random object!

To achieve this, we'll be using one of D's most powerful features: [metaprogramming](https://tour.dlang.org/tour/en/gems/template-meta-programming)

This series is aimed towards people who are new/interested in D, while its purpose is to teach and 
show off different aspects of D's metaprogramming features, ending with the creation of a semi-useful serialiser/deserialiser for JSON.

This first post will cover the creation of two template functions, which, as the series progresses, will 
begin to use a broad range of D's meta programming features to determine how to serialise/deserialise most of D's primitive types.

Some code snippets may start with a comment containing a link to https://run.dlang.io/
which will take you to an online D environment containing a runnable version of that snippet.

--toc

## Requirements

* The ability to write and compile D code

* A basic understanding of what a template is (in any language)

* A ~~rubber ducky~~ [D man](https://dlang.org/images/dman-rain.jpg) close by.
    
## std.json
    
To make these examples as frictionless to compile as possible, the code will only use 
[Phobos](https://dlang.org/phobos/), D's standard library, which luckily for us 
includes a (rather outdated) JSON module called std.json
    
## What are the primitive types?
    
For the context of this blog series, a primitive type is type built into the language itself.

* Integers: `byte`, `short`, `int`, `long`, including their unsigned versions (ubyte, ushort, etc.)

* Floating point: `float`, `double`, and `real` (which we won't bother with)

* Booleans: `true` or `false`.

* Strings

* There are also chars, but in D they're UTF8 code units which are a bit weird to handle, so I'm just going to ignore them.

## Serialising primitive types

To start off let's begin with a simple D file that contains an empty main function, and imports std.json;:

```
import std.json;

void main()
{
}
```

Next we want to create a basic template function called `serialise`. 
This function will take a template parameter of any type (`T`), 
and returns a std.json#JSONValue which is the main struct that std.json uses to represent JSON values.

One of the interesting things about std.json#JSONValue is that its constructor is also a template, 
which conveniently supports being able to automatically wrap around any primitive type (and more) that D has.

This means that to serialise a primitive type all we must do is construct a std.json#JSONValue
and pass our primitive value directly to it. 
Don't think things are this convenient later down the road though.

```
JSONValue serialise(T)(T value)
{
    return JSONValue(value);
}
```

And it can be used like so:

```
// https://run.dlang.io/is/0pBFdu
void main()
{
    import std.stdio : writeln;

    JSONValue json;

    // Here we use `!int` to directly specify that `T` is of type `int`.
    json = serialise!int(420);
    writeln(json); // 420

    // However, the compiler can actually infer what `T` is based off of the parameters we pass through.
    // So here, `T` would be `string`.
    json = serialise("Hello world!");
    writeln(json); // "Hello world!", including the quotes.
}
```

Simple enough, however right now we're letting the user pass through any type they want into our `serialise` function, 
but we would like to distinguish between primitive types and any other future types we may want to handle such as structs, 
classes, and enums, all of which are unable to be serialised directly by std.json#JSONValue:JSONValue's constructor.

"How do we do this?" you may ask. The answer is `static if`.
    
## Static if

If you're familiar with C++'s `#if` directive
then `static if` should make you feel right at home (without all the downsides of `#if`).

`static if` is like a normal `if` statement, except: 

* It only runs at compile time, so its condition must also be able to be evaluated at compile time.

* It doesn't create a new scope.

* Any code that isn't inside a `static if`'s passing block (either the `if` or the `else`) will be ignored by the compiler (outside of syntax checks).

For example, say I wanted to have a compile time flag in my code that determined whether my program performs logging, 
`static if` could be used in this situation:

```
// https://run.dlang.io/is/yjxGYT
const bool SHOULD_LOG = true; // This value is readable at compile time, so can be used in static if.

void main()
{
    import std.stdio : writeln;

    static if(SHOULD_LOG)
    {
        writeln("This is a log!");
    }

    writeln("Done Task.");
}
```

If `SHOULD_LOG` is true then the line `writeln("This is a log!")` is compiled into the program, otherwise
everything inside of the `static if` is ignored by the compiler. 
    
## Deserialising primitive types
    
Now that we know how `static if` works we can move onto deserialising primitive types, 
as it's less straight forward than serialising them.

To start, we'll make a deserialise function that takes a std.json#JSONValue, a type parameter (`T`), and returns a `T`.

```
// The `T` can be passed by doing `deserialise!int(someJsonValue)`, where `T` would then be `int`.
T deserialise(T)(JSONValue json)
{
    assert(false, "Not implemented");
}
```

Now, converting a std.json#JSONValue back into primitive values isn't as convenient as the other way around 
(edit: this was written before std.json#JSONValue.get was added).

Instead, std.json#JSONValue has specific functions for converting back into different types:

* std.json#JSONValue.str - Convert to a string
* std.json#JSONValue.integer - Convert to a long
* std.json#JSONValue.uinteger - Convert to a ulong
* std.json#JSONValue.floating - Convert to a double
* std.json#JSONValue.boolean - Convert to a bool

This means we have to use static if to determine which of the correct functions to call. 
It should be noted that if for example, you tried to convert a std.json#JSONValue containing a string into a long then an error would be thrown, 
making it mandatory that the right function is called.

### Deserialisation - is() expression

Let's start off with strings. There is an expression in D called the `is()` expression, which has some very magical features 
but the most basic one is to compare one type to another.

I feel this is best shown by example, so let's use `static if` and `is()` together to determine if our type parameter (`T`) is a string, 
and then call std.json#JSONValue.str;.

```
T deserialise(T)(JSONValue json)
{
    // Yea, it's actually that easy.
    static if(is(T == string))
    {
        return json.str;
    }
}
```

One particular issue we have though is that with our current code things like `deserialise!string(JSONValue("Hello world!"))` would work, 
however if we were to do something such as `deserialise!int`, which we currently don't have code to handle, we'd get a compiler error 
complaining that there's no return value (since the static if doesn't compile a return statement in that case).

What if we could create our own error messages for a more user friendly experience? In comes `static assert`.

### Deserialisation - static assert

`static assert` is a compile time version of `assert` (requires a condition that must be true otherwise crash the program), 
that instead of crashing the program if its condition fails it will instead fail compliation, 
optionally displaying a user-defined message.

In the event that all of our `static if`s fail to have their conditions met, we can then fall back to a 
`static assert` that prints a user friendly message.

We can use the special `.stringof` property every type has to get a human readable string of whatever type `T` currently is.

```
// https://run.dlang.io/is/nd2LYl
T deserialise(T)(JSONValue json)
{
    static if(is(T == string))
    {
        return json.str;
    }
    else
    {
        static assert(false, "Don't know how to deserialise type: " ~ T.stringof);
    }
}
```

Here's an example of the output were we to do `deserialise!int(JSONValue(0))`:

```
.\test.d(17): Error: static assert:  "Don't know how to deserialise type: int"
.\test.d(5):         instantiated from here: `deserialise!int`
```

###  Deserialisation - cont.

Carrying on, with bools we pretty much do the same thing: 

```
T deserialise(T)(JSONValue json)
{
    static if(is(T == string))
    { /* ... */ }
    else static if(is(T == bool)) // Please note that it is "else STATIC if", not "else if"
    {
        return json.boolean;
    }
    else
    { /* ... */ }
}
```

For floating points while we could just check for both a float and a double in the same `static if`, 
we could instead start learning about what std.traits offers us, 
as it contains a plethora of templates that can determine certain things about a type (among other extremely useful things).

### Deserialisation - std.traits#isFloatingPoint;, and std.conv#to 

So let's start by importing std.traits at the top of the file: 

```
import std.json, std.traits;
```

Now we need to add another `else static if` statement into our deserialise function where we can use
the std.traits#isFloatingPoint template to check if `T` is a floating point type.

However, there is also one last issue we must address first. std.json#JSONValue.floating returns to us a `double`, 
but we want to support both `float` and `double` at the same time. While we could just cast the return value into a `float` 
this presents another issue of, what if the return value is larger than a `float` can hold? 
The cast in this case would then provide back a bad value.

So the solution is to use another incredibly helpful function called std.conv#to;, 
which is a template function that can convert between different types, and provides a few sanity checks including 
throwing an exception if we try to cast a `double` to a `float` where the `double` is too large to fit into a `float`.

So get an import for std.conv going somewhere, and let's improve our deserialiser.

```
// https://run.dlang.io/is/E7c9ZP
T deserialise(T)(JSONValue json)
{
    static if(is(T == string))
    { /* ... */ }
    else static if(is(T == bool))
    { /* ... */ }
    else static if(isFloatingPoint!T)
    {
        return json.floating.to!T();
    }
    else
    { /* ... */ }
}
```

Side note that we're using a feature called [UFCS](https://tour.dlang.org/tour/en/gems/uniform-function-call-syntax-ufcs) 
(Uniform function call syntax) to allow us to use std.conv#to as if it were a member function for a double.

### Deserialisation - isSigned, and isUnsigned

Finally, we're onto signed and unsigned integers.

The std.traits module provides us with the std.traits#isSigned and std.traits#isUnsigned templates. 
The std.traits#JSONValue.integer and the unsigned counterpart both return long/ulong, 
so we also want to use std.conv#to again for the sanity checks.

```
T deserialise(T)(JSONValue json)
{
    static if(is(T == string))
    { /* ... */ }
    else static if(is(T == bool))
    { /* ... */ }
    else static if(isFloatingPoint!T)
    { /* ... */ }
    else static if(isSigned!T)
    {
        return json.integer.to!T();
    }
    else static if(isUnsigned!T)
    {
        return json.uinteger.to!T();
    }
    else
    { /* ... */ }
}
```

It's actually really simple once you understand a bit more about D's metaprogramming power, right?

Anyway, let's do a quick test to see the results of our work.

```
// https://run.dlang.io/is/unkmfu
void main()
{
    import std.stdio;

    JSONValue foo;

    foo = serialise("Hello world!");
    writeln(foo); // "Hello world!"
    writeln(deserialise!string(foo)); // Hello world!

    foo = 500.serialise(); // Can also use UFCS for a cleaner syntax.
    writeln(foo); // 500
    writeln(foo.deserialise!short()); // 500
}
```


## Conclusion 

We now have a serialiser that can serialise and deserialise most of D's primitive types. 
While it is not too useful in its current state, the next post will talk about how to start (de)serialising structs, 
which will turn this tiny little serialiser into something infinitely more useful. 

## Excercises

There are various things I left out, either to reduce the length of this blog post, 
or to leave up to you, the reader, to implement for yourself as a challenge.

### Excercise #1 - Validation checks during deserialisation

While std.json#JSONValue itself does checks for things like "take this JSONValue containing a string and convert it into a long", 
adding these checks yourself can be good practice, and a great place to start getting into the habit of 
using the std.exception#enforce function.

You can use std.json#JSONValue.type to get the type of the std.json#JSONValue passed to the deserilise function, 
and please see std.json#JSONType to see all the different types.

Here is a test case. Just copy-paste this as your main function, and run the program.

```
void main()
{
    import std.stdio     : writeln;
    import std.exception : assertThrown, assertNotThrown;

    JSONValue json = serialise("Lalafell");

    assertThrown(json.deserialise!int());
    assertNotThrown(json.deserialise!string());

    writeln("Success");
}
```

### Excercise #2 - Allowing conversion between signed and unsigned integers.

Basically, you can store both signed and unsigned integers into a std.json#JSONValue;. 
But, for example, if you store a signed integer, you can only get it back as a signed integer, 
and trying to get it back as an unsigned integer will make std.json#JSONValue throw an error.

However, if you got a signed integer back as a signed integer, and then converted it to an unsigned integer, 
that'd work, although there's an argument about whether it's correct behaviour or not 
(but std.conv#to should catch most errornous conversions).

So you must modify the deserialise function to allow `deserialise!uint()` to work on JSONValues 
containing either signed or unsigned integers, and vice versa with `deserialise!int()`.

Test case:

```
void main()
{
    import std.stdio     : writeln;
    import std.exception : assertNotThrown;

    JSONValue json;

    // D has closures btw
    void test()
    {
        assertNotThrown(json.deserialise!int());
        assertNotThrown(json.deserialise!uint());
        assert(json.deserialise!int() == 400);
        assert(json.deserialise!uint() == 400);
    }

    json = serialise!uint(400u);
    test();

    json = serialise!int(400);
    test();

    writeln("Success");
}
```

### Excercise #3 - Write test cases using D's built-in unittesting.

D has built-in unittests, and this tiny project could be a good way to introduce yourself with them.

For example, the test cases I gave for the other excercises could be mo