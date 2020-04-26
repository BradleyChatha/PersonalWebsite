@date-created 31-10-2019@
@date-updated 07-02-2020@
@title Serialising basic D types - Alternative function layout@
@seo-title Template function constraints@
@seo-tag template-function-constraints@

*This post is an optional post, meaning you can skip this post without it being
detrimental for viewing future posts, and simply exists as an aside "FYI".*

For convenience here's a shortened version of the `deserialise`
function that was created in the first post of this series.

```
T deserialise(T)(JSONValue json)
{
    static if(is(T == string))
    { /**/ }
    else static if(is(T == bool))
    { /**/ }
    else static if(isFloatingPoint!T)
    { /**/ }
    else static if(isSigned!T)
    { /**/ }
    else static if(isUnsigned!T)
    { /**/ }
    else
    {
        static assert(false, "Don't know how to deserialise type: " ~ T.stringof);
    }
}
```

This kind of 'design' being used here is to have a singular template function
(`deserialise`) that takes any type as its input, and then leverage `static if` to determine
the actual functionality.

What if we could write this another way? First, we need to know about template constraints.

## Template constraints
                    
Instead of a function that can take any type, imagine that we instead of multiple seperate *templated* functions that
can only take certain types (e.g. only integers, but any kind of integer).
                    
While we could go the route of nesting a bunch of `static ifs` inside the function's body
followed by a `static assert` should all of these `ifs` fail, what if instead
we tell the compiler the *exact* conditions needed for it to even consider using the template function?

I feel an example will help clear things up:

```
void someFunc(T)(T value)
if(is(T == string) || is(T == long))
{
    // Do stuff.
}
```         
                    
So what's happening here is that we're creating a template function called `someFunc`
which takes a type parameter (`T`), and it does stuff, similar to the other template functions we've made so far.
                    
However, take note that we can actually attach an `if` statement *directly to the function's signature*.
This is known as a constraint which you can imagine as a `static if` that applies for the entire function as a whole.
                    
The constraint's condition must pass the given template parameters, otherwise the compiler will refuse to use it for
that specific permutation.
                    
For example:

```
// https://run.dlang.io/is/bYQUBq
void main()
{
    someFunc!long(200);              // Fine
    someFunc!string("Hello world!"); // Fine
    someFunc!bool(false);            // Error (see comment below)

    // The error given is:
    /*
    onlineapp.d(5): Error: template instance onlineapp.someFunc!bool does not match template declaration someFunc(T)(T value)
      with T = bool
      must satisfy one of the following constraints:
           is(T == string)
           is(T == long)
    */
}

void someFunc(T)(T value)
if(is(T == string) || is(T == long))
{
    import std.stdio : writeln;
    writeln(T.stringof);
}
```

So where am I going with this?

## Using constraints to create template overloads

By using constraints, we can essentially create overloads for template functions.

Take these two functions for example:

```
void someFunc(T)(T value)
if(is(T == string))
{
    // stuff with strings
}

void someFunc(T)(T value)
if(is(T == long))
{
    // stuff with longs
}
```
                    
They both have the same name, same template parameters, and same runtime parameters. The
only difference are their constraints.
                    
When calling a template function with overloads like this, the compiler will try to match
each overload with the parameters that you pass to it (which includes testing the constraints).
               
* If no overloads matches your parameters, the compile fails.

* If more than one overload matches your parameters, the compile fails due to ambiguity.

* If exactly one overload matches your parameters, that overload will be used.
                    
So it's pretty much the same as overloading a non-templated function, except we have more specific
control on when each overload can be used thanks to constraints.

What this means is that instead of a singular `deserialise` function with a bunch
of `static ifs`, you could also/instead use seperate overloads for each type
of data you want to deserialise:

```
// For strings
string deserialise(T)(JSONValue json)
if(is(T == string))
{ /*code here*/ }

// For floats and doubles
T deserialise(T)(JSONValue json)
if(isFloatingPoint!T)
{ /*code here*/ }

// Using both constraints, and static if chains
T deserialise(T)(JSONValue json)
if(isIntegral!T)
{
    static if(isSigned!T)
    { /**/ }
    else static if(isUnsigned!T)
    { /**/ }
}

// etc.
```

## Conclusion
                    
There are pros and cons to both designs, and there are of course more complicated ways/features
that can be used to design your code around templates that need to discriminate by things like types.

However, the main point of this one-off post was just to show and explain a different way
of doing things, so that it doesn't seem like a nested web of `static ifs` is the only
way to handle something like this.

D will let you mold your code to the exact vision (or close enough to it) of how you want your API to look
without compromising on maintainability.