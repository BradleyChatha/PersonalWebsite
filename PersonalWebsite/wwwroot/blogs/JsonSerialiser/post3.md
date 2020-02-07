@date-created 31-10-2019@
@date-updated 07-02-2019@
@title Serialising enums and classes@

*Side note: In true D fashion, run.dlang.org's ability to create snippets has been
broken for a few weeks, with no sign of it being fixed, so I've swapped over to a
difference service for the snippet links.*

In this post, we'll be going over how to serialise classes and enums,
both of which have certain considerations to go over.

## Serialising enums

The first thing to consider with enums is whether to serialise them via their names
(e.g. `PersonType.Student` would become "Student"), or to serialise them
via their values.

There are pros and cons to both, mostly revolving around enums that are used as bit flags,
but for now we're going to serialise them by names and later on provide a way to use either
names or values.

We're going to need an enum to work with, so let's create a `PersonType` enum
for our `Person` struct from our <a href="https://run.dlang.io/is/nCESzP">previous code</a>:

```
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
```

### The issue with enums

There is one fairly important, but easy to solve issue with enums - they count as both an enum,
as well as their value type.

For example take our newly made `PersonType` enum. It is just a
bog standard enum where the value type is an `int` (by default).

The `PersonType` enum, due to having a value type of `int`, will actually
**pass** a check such as `is(PersonType == int)` and `isNumeric!PersonType`.
Needless to say this is a bit of a roadblock due to our decision to serialise by name instead of value.

Fortunately, this issue is actually super easy to work around. Via `is(T == enum)` we can create
code that works/doesn't work specifically for enums.

### Upgrading our serialise function

For reference, here's the shortened code for the serialise function:

```
enum isPrimitiveType(T) = isNumeric!T || is(T == bool) || is(T == string);

JSONValue serialise(T)(T value)
{    
	static if(isPrimitiveType!T)
	{ /* omitted for brevity */ }
	else static if(is(T == struct))
	{ /* omitted for brevity */ }
	else
	{ /* omitted for brevity */ }
}
```

There are two options for us to take here:

* Modify `isPrimitiveType` so that it ignore enums, then handle enums later on in the function.

* Handle enums inside the function **before** handling primitive types, so enums get a larger 'priority'.

We will in fact be doing both - option #1 for the `serialise` function, and then option #2 for the
`deserialise` function.

So first off, by using `!is(T == enum)` we can make `isPrimitiveType` ignore enums:

```
// <a href="https://godbolt.org/z/_edV5X">https://godbolt.org/z/_edV5X</a>
enum isPrimitiveType(T) = !is(T == enum) && (isNumeric!T || is(T == bool) || is(T == string));    
```

Next we need to add a `static if` so we can handle enums, and then turn the enum
value into its name with the help of the ever-so-useful `std.conv.to` function:

```
JSONValue serialise(T)(T value)
{    
	static if(isPrimitiveType!T)
	{ /* omitted for brevity */ }
	else static if(is(T == enum))
	{
		return JSONValue(value.to!string()); // PersonType.Student -> "Student", PersonType.Staff -> "Staff", etc.
	}
	else static if(is(T == struct))
	{ /* omitted for brevity */ }
	else
	{ /* omitted for brevity */ }
}    
```

Note that if you do something weird such as `cast(PersonType)400`, then
`std.conv.to` will actually return `"cast(PersonType)400"`, which
will cause errors down the line.

### Upgrading our deserialise function

For reference, here's the shortened code for the deserialise function:

```
T deserialise(T)(JSONValue json)
{
	static if(is(T == string))
	{ /* omitted for brevity */ }
	else static if(is(T == bool))
	{ /**/ }
	else static if(isFloatingPoint!T)
	{ /**/ }   
	else static if(isSigned!T)
	{ /**/ }
	else static if(isUnsigned!T)
	{ /**/ }
	else static if(is(T == struct))
	{ /**/ }
	else
	{ /**/ }
}
```

As I mentioned before, for the `deserialise` function we're going to handle
enums before any other type, so that the enum path takes priority over the others.

Yet again, the handy `std.conv.to` function comes to our rescue as it can convert a string into an enum,
as long as the string has the same name as one of the enum values:

```
T deserialise(T)(JSONValue json)
{
	static if(is(T == enum))
	{
		// "Student" -> PersonType.Student, etc.
		return json.str.to!T();
	}
	else static if(is(T == string))
	{ /* omitted for brevity */ }
	else static if(is(T == bool))
	{ /**/ }
	else static if(isFloatingPoint!T)
	{ /**/ }   
	else static if(isSigned!T)
	{ /**/ }
	else static if(isUnsigned!T)
	{ /**/ }
	else static if(is(T == struct))
	{ /**/ }
	else
	{ /**/ }
}     
```

Finally, as usual, we'll give things a quick test:

```
// <a href="https://godbolt.org/z/f4xTyB">https://godbolt.org/z/f4xTyB</a>
void main()
{
	import std.stdio : writeln;

	auto json = serialise(PersonType.Student);
	writeln(json);                          // "Student"
	writeln(json.deserialise!PersonType()); // Student

	json = serialise(Person("Bradley", 20, PersonType.Student));
	writeln(json);                      // {"age":20,"name":"Bradley","type":"Student"}
	writeln(json.deserialise!Person()); // Person("Bradley", 20, Student)
}
```

## Serialising classes

Serialising classes is where a lot of the more important decisions come into play, as
structs and classes are very different from one another.

For example, while we can just treat classes like they're structs, that will only support
a very tiny amount of classes, because:

* Classes can be null.

* Classes may not have a reliable way to construct them (more on that later in the post).

* Classes tend to not expose variables directly, but via getters, setters, and other functions.

* And many other reasons...

This post will provide a way to handle some of these issues, but in a real project you may
need to fine tune how the serialiser works for your own needs, as my solutions are going to be
relatively basic and therefore, not robust.

### Treating classes as structs

Before we can start taking on some of the other issues, we need to treat the most
basic case of simply being able to serialise and deserialise a class' public variables,
and then build off of that.

For now, we'll assume that all classes passed to the serialise function are not-null.
So for serialisation, we can very simply just modify the `static if` that checks
for a struct, and extend it to also check for a class:

```
JSONValue serialise(T)(T value)
{    
	static if(isPrimitiveType!T)
	{ /* omitted for brevity */ }
	else static if(is(T == enum))
	{ /**/ }
	else static if(is(T == struct) || is(T == class)) // &lt;-----
	{ /**/ }
	else
	{ /**/ }
}   
```

This will work for extremely simple classes, e.g. if you were to change `Person`
to a class, it should be able to serialise a non-null instance of it perfectly fine.

For the deserialise function, our biggest hurdle is constructing a new instance of the class
so we can populate its fields. For now, our code will just assume that there is a default constructor
(e.g. `new MyClass()` works). Also keep in mind that `static if` does not
create a scope:

```
T deserialise(T)(JSONValue json)
{
	static if(is(T == enum))
	{ /* omitted for brevity */ }
	/**/
	else static if(is(T == struct) || is(T == class)) // Remember to check for a class here as well.
	{
		static if(is(T == class))
		{
			T toReturn = new T();
		}
		else
		{
			T toReturn; // Classes default to `null`, so we can't just reuse this line with them.
		}

		static foreach(member; T.tupleof)
		{{
			/**/
		}}

		return toReturn;
	}
	else
	{ /**/ }
}
```

As a quick example, change/copy `Person` to a class, and then we can
test if it works:

```
// <a href="https://godbolt.org/z/EhHVdN">https://godbolt.org/z/EhHVdN</a>
void main()
{
	import std.stdio : writeln, writefln;

	// The compiler doesn't generate a helper constructor like with structs,
	// so we'll do things like this for now.
	auto p = new Person();
	p.name = "Bradley";
	p.age = 20;
	p.type = PersonType.Student;

	auto json = p.serialise();
	writeln(json); // {"age":20,"name":"Bradley","type":"Student"}

	// writeln can't automatically format a class like with structs.
	// So either override the `toString` function in a class, or just manually write out the fields.
	writefln("Person(%s, %s, %s)", p.name, p.age, p.type);

	// Output: Person(Bradley, 20, Student)
}
```

### Handling 'classes can be null'

Realistically, being able to handle null classes is a mandatory requirement, so
let's get that out of the way quickly.

For serialising, if the class is null, then we can return a `JSONValue(null)`
which works perfectly fine:

```
JSONValue serialise(T)(T value)
{    
	static if(isPrimitiveType!T)
	{ /* omitted for brevity */ }
	else static if(is(T == enum))
	{ /**/ }
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
			/**/
		}}

		return toReturn;
	}
	else
	{ /**/ }
}
```

Now if we were to pass a null class instance to the `serialise` function
it would output a `null` in JSON.

For our `deserialise` function it's a very similar process - check if
the given JSON value is null, and if it is then return null:

```
T deserialise(T)(JSONValue json)
{
	static if(is(T == enum))
	{ /* omitted for brevity */ }
	/**/
	else static if(is(T == struct) || is(T == class)) // Remember to check for a class here as well.
	{
		static if(is(T == class))
		{
			if(json.type == JSONType.null_)
				return null;

			T toReturn = new T();
		}
		else
		{
			T toReturn;
		}

		/**/

		return toReturn;
	}
	else
	{ /**/ }
}
```

Very simple, very easy to handle, unlike the can of worms we're about to open regarding
construction of a class.

But before we fall into despair over constructing classes, here's a quick test of the null
handling (change `Person` to a class):

```
// <a href="https://godbolt.org/z/Q8pU7v">https://godbolt.org/z/Q8pU7v</a>
void main()
{
	import std.stdio : writeln;

	Person p = null;
	auto json = p.serialise();

	writeln(json);                      // null
	writeln(json.deserialise!Person()); // null
}
```

### Handling 'classes may not have a reliable way to construct them' with __traits(compiles)

Currently, we use the code `new T()` to construct a new instance of a class.
This will only work if the class has either no constructors, or a default constructor.

Again, realistically, classes are very likely to have parameterised constructors and will
likely not support having a default constructor.

Just as a note, this only applies to deserialisation, as serialising does not require
the construction of objects (well, unless you want it to).

There are many ways you may want to handle this, such as using
<a href="">std.traits.Parameters</a>
to check and recognise constructors with certain parameter patterns, having
a pre-defined list of constructor parameters that you support, or just
not allowing classes that do not contain a default constructor.

In our case we're going to disallow the use of classes that do not contain a default
constructor. Fret not though, as we will be exploring one potential workaround soon.

But for now we're going to be creating ourselves a helper template called `HasDefaultCtor`,
and using that in our `deserialise` function if the check returns `false`.

One way we can do this is to check if the exact code of `new T()` can compile,
which can be achieved via another magical function of `__traits()` called
<a href="https://dlang.org/spec/traits.html#compiles">__traits(compiles)</a>.

By passing in some code as a parameter, the compiler will determine if the code will
compile or not, and return a `bool` as the result.

Please note that there is a certain annoyance to `__traits(compiles)` when the code
you're checking contains a template function, but that is beyond this quick explanation of it.

Anyway, by telling the compiler to check if `new T()` works for a given type,
we can effectively check if the type has a default constructor, then from there
we can use a `static assert` to let us display a message to the user:

```
enum HasDefaultCtor(T) = __traits(compiles, new T());

T deserialise(T)(JSONValue json)
{
	static if(is(T == enum))
	{ /* omitted for brevity */ }
	/**/
	else static if(is(T == struct) || is(T == class))
	{
		static if(is(T == class))
		{
			static assert(HasDefaultCtor!T, "The class `" ~ T.stringof ~ "` requires a default constructor.");

			/**/
		}
		else
		{
		/	**/
		}

		/**/

		return toReturn;
	}
	else
	{ /**/ }
}
```

Here is an example of the error message:

```
// <a href="https://godbolt.org/z/NSwveB">https://godbolt.org/z/NSwveB</a>

class NoDefaultCtor
{
	this(string str){}
}

void main()
{
	deserialise!NoDefaultCtor(JSONValue());

	// .\temp.d(27): Error: static assert:  "The class `NoDefaultCtor` requires a default constructor."
	// .\temp.d(10):        instantiated from here: `deserialise!(NoDefaultCtor)`
}
```

This isn't overly ideal for many reasons, but thankfully as we explore the workaround,
we'll implement something that handles this issue to a somewhat reasonable degree.

### Workaround for the constructor issue, and the fact classes tend to not expose variables directly

I'll be brief. I've already gone over the constructor issue; and
because classes usually don't expose variables directly, our current behaviour won't work for most
of them.

Regarding the latter issue, there are several ways to handle the issue ranging from directly
inspecting the names of functions for keywords (e.g. starting with "get" or "set"), looking for
getters/setters that use the `@property` attribute, and any other ways suitable
for your use cases.

The solution I will be going for is to add support for classes and structs to have a static
`deserialise` function. This will provide a workaround for the constructor issue, as this
is essentially just a special constructor, and will also outsource the task of handling deserialisation
to the class itself, which technically handles the issue of classes not generally exposing variables.

A downside of course is that there's more code to be written for classes to support
deserialisation, and therefore more technical debt over the long term.

While you could also just specifically check for a constructor that takes a `JSONValue`,
I prefer to create specific functions for things like this as I don't like
that it'll limit classes from providing a `this(JSONValue)` constructor for other
purposes.

The first thing we're going to do is make another helper template (similar to `HasDefaultCtor`)
that will check for the special `deserialise` function.

This template needs to check for:

* A static function called `deserialise`.

* The function's return value is the same/compatible type as the class.

* The function takes a `JSONValue` as the first parameter.

There are a few ways to do this. One of these ways is to use `__traits(compiles)`
again:

```
enum HasStaticDeserialiseFunc(T) = __traits(compiles, { T obj = T.deserialise(JSONValue()); });    
```

Notice that the code we're checking is now inside brackets, since we're doing more
than a basic function call.

We're using `T.deserialise` to specifically check for a static function
(or anything that has the same syntax as a static function); we assign the return value
to a `T obj` to check that the return value is compatible with whatever `T` is;
then finally we also check that a `JSONValue` can be passed as the first parameter.

Next, in the `deserialise` function, we want to update the `static assert`
that requires classes to have a default constructor to check if the class has either
a default constructor, or a static deserialise function.

We also need to hide the `T toReturn = new T()` line behind
the `HasDefaultCtor` check:

```
enum HasStaticDeserialiseFunc(T) = __traits(compiles, { T obj = T.deserialise(JSONValue()); });    

T deserialise(T)(JSONValue json)
{
	static if(is(T == enum))
	{ /* omitted for brevity */ }
	/**/
	else static if(is(T == struct) || is(T == class))
	{
		static if(is(T == class))
		{
			static assert(HasDefaultCtor!T || HasStaticDeserialiseFunc!T, 
			"The class `" ~ T.stringof ~ "` requires a default constructor or a function matching "
			~"`static " ~ T.stringof ~ " deserialise(JSONValue)`"
			); 
			// e.g. "The class Person requires a default constructor or a function matching `static Person deserialise(JSONValue)`"

			/**/

			static if(HasDefaultCtor!T)
			{
				T toReturn = new T();
			}
		}
		/**/

		return toReturn;
	}
	else
	{ /**/ }
}
```

Now, if the class has a static deserialise function then we want to use that function
for deserialisation instead of our other logic. We still want to handle null JSONValues,
and we want to disable our other logic as we won't need it, among another issue of having
code exist beyond a `return` statement.

To do this, we just need to employ `static if` tactically:

```
enum HasStaticDeserialiseFunc(T) = __traits(compiles, { T obj = T.deserialise(JSONValue()); });    

T deserialise(T)(JSONValue json)
{
	static if(is(T == enum))
	{ /* omitted for brevity */ }
	/**/
	else static if(is(T == struct) || is(T == class))
	{
		static if(is(T == class))
		{ /* null is still handled here */ }
		else
		{ /**/ }

		static if(HasStaticDeserialiseFunc!T)
		{
			return T.deserialise(json);
		}
		else // If we don't disable this other code, then we'll get a "statement not reachable" error.
		{
			static foreach(/**/)
			{{
				/**/
			}}
			return toReturn;
		}
	}
	else
	{ /**/ }
}
```

To finish off, let's give it a test:

```
// <a href="https://godbolt.org/z/Xbann8">https://godbolt.org/z/Xbann8</a>

class Person
{
	private
	{
		string name;
		int age;
		PersonType type;
	}

	// No default ctor.
	this(string name, int age, PersonType type)
	{
		this.name = name;
		this.age = age;
		this.type = type;
	}

	static Person deserialise(JSONValue value)
	{
		// Classes having to implement this logic themselves is a neccessary burden if you
		// were to go this route... or is it *wink* (this is a topic for a future post)
		return new Person(
			value["name"].deserialise!string(),
			value["age"].deserialise!int(),
			value["type"].deserialise!PersonType()
		);
	}

	// So that writeln can show us something useful.
	override string toString()
	{
		import std.format : format;

		return format("Person(%s, %s, %s)", this.name, this.age, this.type);
	}
}

void main()
{
	import std.stdio : writeln;

	auto person = new Person("Bradley", 20, PersonType.Student);
	auto json = person.serialise();

	writeln(json); // {"age":20,"name":"Bradley","type":"Student"}

	person = json.deserialise!Person();
	writeln(person); // Person(Bradley, 20, Student)
}
```

## Conclusion

Our serialiser can now serialise enums by name, and has some dodgey support for serialising
classes, meaning it's starting to shape up to be at least *kind of* useable. Maybe.
If you're desperate.

The next thing we'll look into is to serialise arrays and associative arrays.

## Excercises

### Add support for classes and structs to provide their own serialise function

This might be something I end up doing in a future post, but for now this is a
good excercise to practice using `__traits(compiles)`.

Basically, allow classes and structs to provide a custom `serialise` function.

For the test case to work, the serialise function must match the signature of: `JSONValue serialise()`

And here's the test case:

```
struct VerySimpleTest
{
	string lalafell;

	JSONValue serialise()
	{
		JSONValue json;

		json["lalafell"] = this.lalafell;
		json["hidden_secret"] = "Don't be a lalafell please.";

		return json;
	}
}

void main()
{
	auto json = serialise(VerySimpleTest("Zuzu"));
	assert(json["lalafell"].str == "Zuzu");
	assert(json["hidden_secret"].str == "Don't be a lalafell please.");
}
```
