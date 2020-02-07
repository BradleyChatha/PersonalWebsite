@date-created 31-10-2019@
@date-updated 07-02-2019@
@title Serialising arrays@ 

*Note that in the previous post, the `Person` struct was converted into a class*
*for testing purposes. From this point on, `Person` is a struct again unless specified otherwise.*

One of the final things missing before our serialiser can reach a "minimalistic but useable" state
is the ability to serialise both dynamic and associative arrays. Static arrays are saved as an excercise
for this post.

I've already covered all the main metaprogramming features that'll be used in this post, and arrays
are relatively simple to serialise, so this post will be on the shorter end.

## Serialising dynamic arrays

As usual, here's our current serialise function for reference:

```
JSONValue serialise(T)(T value)
{    
	static if(isPrimitiveType!T)
	{ /* omitted for brevity */ }
	else static if(is(T == enum))
	{ /**/ }
	else static if(is(T == struct) || is(T == class))
	{ /**/ }
	else
	{ /**/ }
}    
```

For dynamic arrays, all that needs to happen is to iterate over the array's elements, serialise
them, and then append the serialised value into a JSON array.

There's not too much to explain about to code, so I'll quickly highlight two things:

* We use <a href="https://dlang.org/phobos/std_traits.html#isDynamicArray">std.traits.isDynamicArray</a> 
  to check if `T` is a dynamic array.

* Creating a JSONValue that is also an array is a bit iffy, the `parseJSON("[]")` part is the best way I could find.

```
JSONValue serialise(T)(T value)
{    
	static if(isPrimitiveType!T)
	{ /* omitted for brevity */ }
	else static if(is(T == enum))
	{ /**/ }
	else static if(is(T == struct) || is(T == class))
	{ /**/ }
	else static if(isDynamicArray!T)
	{
		JSONValue toReturn = parseJSON("[]");
		
		foreach(element; value)
		{
			toReturn.array ~= serialise(element);
		}
		
		return toReturn;
	}
	else
	{ /**/ }
} 
```

Do note that `string` in D is actually just an `immutable(char)[]`, which
would satisfy the `isDynamicArray` template.

This isn't so much an issue in our case due to the fact we handle `string` higher
up in the if-else chain, but it's something worth keeping in mind.

Also, there are actually two other string types in D, `wstring` and `dstring`.
While up to this point I've pretended they don't exist, this series still won't be going over
using them due to various reasons (such as JSONValue not supporting them).

In regards to that issue, it may be wise to combine `isDynamicArray` with a check to make sure
the type isn't a `wstring` or `dstring`, depending on your circumstances.

Other than that, there's not really any other surprises in this code that haven't been seen before
- unless you're using a super large dataset that causes a bunch of recursive
calls that is... so onto deserialising!

## Deserialising dynamic arrays

Reference:

```
T deserialise(T)(JSONValue json)
{    
	static if(is(T == enum))
	{ /**/ }
	else static if(is(T == string))
	{ /**/ }
	else static if(is(T == bool))
	{ /**/ }
	else static if(isFloatingPoint!T)
	{ /**/ }    
	else static if(isSigned!T)
	{ /**/ }
	else static if(isUnsigned!T)
	{ /**/ }
	else static if(is(T == struct) || is(T == class))
	{ /**/ }
	else
	{ /**/ }
}    
```

Again, there's nothing overly new or complicated about the deserialisation code, except for one thing:
<a href="https://dlang.org/library/std/range/primitives/element_type.html">std.range.ElementType</a> 
is used to get the type of data stored in the array. Make sure to `import std.range;` somewhere.

```
T deserialise(T)(JSONValue json)
{    
	static if(is(T == enum))
	{ /**/ }
	else static if(is(T == string))
	{ /**/ }
	else static if(is(T == bool))
	{ /**/ }
	else static if(isFloatingPoint!T)
	{ /**/ }    
	else static if(isSigned!T)
	{ /**/ }
	else static if(isUnsigned!T)
	{ /**/ }
	else static if(is(T == struct) || is(T == class))
	{ /**/ }
	else static if(isDynamicArray!T)
	{
		T toReturn;
		
		alias ElementT = ElementType!T; // E.g. If `T` were `string[]`, then this would be `string`.
		
		foreach(element; json.array)
		{
			toReturn ~= deserialise!ElementT(element);
		}
		
		return toReturn;
	}
	else
	{ /**/ }
}
```

As another side note about strings, since they're kind of annoying, `ElementType`
will *always* return `dchar` if used on any kind of string. However, `ElementEncodingType`
will properly return `char`, `wchar`, and `dchar` for their respective
string types.

Finally, let's give it a test:

```
// <a href="https://godbolt.org/z/h7U38W">https://godbolt.org/z/h7U38W</a>
void main()
{
	import std.stdio : writeln;

	auto json = ["a", "b", "c"].serialise();
	writeln(json);                        // ["a","b","c"]
	writeln(json.deserialise!(string[])); // ["a", "b", "c"]
}
```

## Serialising and deserialising associative arrays (abbreviated as 'AA')

As a common theme with this post, there's nothing overly complicated to explain for AAs, so
here's a quick list of what's new/noteworthy that we're going to use:

* <a href="https://dlang.org/phobos/std_traits.html#isAssociativeArray">std.traits.isAssociativeArray</a> to check if a type is an AA.

* <a href="https://dlang.org/phobos/std_traits.html#KeyType">std.traits.KeyType</a>
and
<a href="https://dlang.org/phobos/std_traits.html#ValueType">std.traits.ValueType</a>
are used to get what type is used for the AA's key and value, respectively.

* We'll use `static assert` to enforce that the key type is a string.

### Serialising
```
JSONValue serialise(T)(T value)
{    
	static if(isPrimitiveType!T)
	{ /* omitted for brevity */ }
	else static if(is(T == enum))
	{ /**/ }
	else static if(is(T == struct) || is(T == class))
	{ /**/ }
	else static if(isDynamicArray!T)
	{ /**/ }
	else static if(isAssociativeArray!T)
	{
		JSONValue toReturn;
		
		alias KeyT = KeyType!T;		
		static assert(is(KeyT == string), "Only string keys are supported, not: " ~ KeyT.stringof);
		
		foreach(key, element; value)
		{
			toReturn[key] = serialise(element);
		}

		return toReturn;
	}
	else
	{ /**/ }
}
```

### Deserialising
```
T deserialise(T)(JSONValue json)
{    
	static if(is(T == enum))
	{ /**/ }
	else static if(is(T == string))
	{ /**/ }
	else static if(is(T == bool))
	{ /**/ }
	else static if(isFloatingPoint!T)
	{ /**/ }    
	else static if(isSigned!T)
	{ /**/ }
	else static if(isUnsigned!T)
	{ /**/ }
	else static if(is(T == struct) || is(T == class))
	{ /**/ }
	else static if(isDynamicArray!T)
	{ /**/ }
	else static if(isAssociativeArray!T)
	{
		T toReturn;
		
		alias KeyT   = KeyType!T;
		alias ValueT = ValueType!T;
		
		static assert(is(KeyT == string), "Only string keys are supported, not: " ~ KeyT.stringof);
		
		foreach(key, element; json.object)
		{
			toReturn[key] = deserialise!ValueT(element);
		}
			
		return toReturn;
	}
	else
	{ /**/ }
}
```

### Test
```
// <a href="https://godbolt.org/z/vjcQkc">https://godbolt.org/z/vjcQkc</a>
void main()
{
	import std.stdio : writeln;

	auto json = 
	[
		"bradley": Person("Bradley", 20, PersonType.Student), 
		"andy":    Person("Andy", 100, PersonType.Staff)
	].serialise();

	// {"andy":{"age":100,"name":"Andy","type":"Staff"},"bradley":{"age":20,"name":"Bradley","type":"Student"}}
	writeln(json);

	// ["andy":Person("Andy", 100, Staff), "bradley":Person("Bradley", 20, Student)]
	writeln(json.deserialise!(Person[string]));
}
```

## Conclusion

This post was a bit short and sweet. There weren't really any new features to explain, mostly just
new templates such as `isDynamicArray`.

While this post may have been a bit boring, hopefully the next post, where we explore the use
of <a href="https://dlang.org/spec/attribute.html#uda">UDAs</a> (User Defined Attributes) 
to allow deeper customisation of how things are serialised, will be more interesting.

## Excercises

### Add support for static arrays

The main two things you'll need for this are:

* <a href="https://dlang.org/phobos/std_traits.html#isStaticArray">std.traits.isStaticArray</a>
to determine whether a type is a static array or not.

* After you identify a type (T) as a static array, you can be assured that the type will
have a constant property called `T.length`, which of course is the length of the array.

Something to consider is, what if during deserialisation you find that the JSON data has
either too many or too little values for the array?

For the purposes of this excercise (you may want to do something different for real-world usage):


* If there are too **many** values, throw an exception.

* If there are **not enough** values, fill the empty spaces up with either
<a href="https://dlang.org/spec/property.html#init">T.init</a> (for value types), 
or `null` (for reference types).

And finally, here's a test case:

```
// <a href="https://godbolt.org/z/hjZdpN">https://godbolt.org/z/hjZdpN</a>
void main()
{
	import std.format    : format;
	import std.exception : assertThrown;

	string[2] people;
	JSONValue json;

	// Serialise
	people = ["Bradley", "Andy"];
	json = people.serialise();
	assert(json[0].str == "Bradley", "Got: " ~ json[0].str);
	assert(json[1].str == "Andy",    "Got: " ~ json[1].str);

	// Deserialise (exact amount of values wanted)
	people = json.deserialise!(string[2]);
	assert(people == ["Bradley", "Andy"], format("Got: %s", people));

	// Deserialise (too many values, so exception should be thrown)
	json = parseJSON(`["Bradley", "Andy", "Kaiya"]`);
	assertThrown(json.deserialise!(string[2]), "No exception was thrown");

	// Deserialise (not enough values, so empty spaces should be `string.init`)
	json = parseJSON(`["Bradley"]`);
	people = json.deserialise!(string[2]);
	assert(people == ["Bradley", string.init], format("Got: %s", people));
}
```