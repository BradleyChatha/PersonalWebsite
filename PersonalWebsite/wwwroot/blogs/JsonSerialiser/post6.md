@date-created 07-05-2020@
@date-updated 07-05-2020@
@title Mixin template to automate static deserialise@
@seo-tag mixin-template-automate@

This post concludes this short tutorial series by exploring the options that [mixin templates](https://dlang.org/spec/template-mixin.html)
can provide us.

The code we create in this post won't be very practical (quite the opposite), however it will show you more magical non-sense
that can be achieved with D, and can serve as a basis for your future code. :)

## Why use and what is a `mixin template`?

A `mixin template` is a way to create a block of code that we can then "copy & paste" into
other pieces of code. It can be used as a way to automate the generation of boilerplate code, since the full
features of D (especially `static if` and string mixins) are available for it to use.

In the context of our serialiser, we could create a `mixin template` to automatically generate the static `deserialise` function
that our deserialiser has support for (created in [post 3](/BlogPost/JsonSerialiser/3-serialise-enum-class-dlang-tutorial-metaprogramming)).

## Creating a class to test with

For reasons I'll make clear shortly, the function we're generating will only support classes, so let's go ahead and make one for us to play with!

```
class PersonClass
{
    private
    {
        string name;
        int age;
        PersonType type;
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
```

## Creating and using a `mixin template`

The most basic form of a `mixin template` is really simple. Take note that this one is named `AutoStaticDeserialise`, as this is
essentially the purpose of this `mixin template`.

```
// You can define template parameters in the parenthesis, just like you would for a templated function.
mixin template AutoStaticDeserialise()
{
}
```

To make use of our mixin, we simply have to add the following line into our `PersonClass`:
```
class PersonClass
{
    mixin AutoStaticDeserialise;

    // omitted...
}
```

And now once we've added some code into the `mixin template`, it'll start having an effect on our `PersonClass`.

## An interesting property of `mixin template`

An interesting, and very useful property of `mixin template` is that, as stated before, the code inside it is functionally
"copy & pasted" into its location.

Let me show you how we can test this:

```
// https://godbolt.org/z/6gWC2B

mixin template AutoStaticDeserialise()
{
    private alias ThisType = typeof(this);
    
    pragma(msg, ThisType);
}

class PersonClass
{
    mixin AutoStaticDeserialise;

    private
    {
        string name;
        // omitted...
    }
    // omitted...
}

/*
    Output:  
        PersonClass
*/
```

To explain - since the code has been 'pasted' into `PersonClass`, the expression `typeof(this)` evaluates to the type
that has used our mixin, which is `PersonClass` in this case.

A more important point is, this means we can even access *private* members of the type the template is being mixed in to. Take notice
that `PersonClass` has some private variables, and I'm sure you can see where this is going.

## Adding some compile-time checks

The `deserialise` function we're generating isn't actually all that important for the purpose of this post. This post is simply to teach
about `mixin template`, so we're going to generate a rather useless `deserialise` function just so we have something there at all.

Therefor, our `deserialise` function will only work for classes, and will require a default constructor in order to function, so let's
add in a few `static asserts` to enforce this behaviour:

```
mixin template AutoStaticDeserialise()
{
    private alias ThisType = typeof(this);
    static assert(is(ThisType == class), "This mixin only works with classes.");
    static assert(HasDefaultCtor!ThisType, "This function relies on the class having a default constructor.");
}
```

## Creating the `deserialise` function

I'm not going to waste much time explaining this code, as it is quite literally a very gutted version of the
struct/class deserialisation branch that we've already created in our deserialiser.

The main thing to note is that we can directly modify the private variables of the class due to the "copy & paste" property
of mixin templates.

```
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
            alias MemberType = typeof(member);
            const MemberName = __traits(identifier, member);

            MemberType memberValue = json[MemberName].deserialise!MemberType();
            mixin("instance." ~ MemberName ~ " = memberValue;");
        }}

        return instance;
    }
}
```

## Testing things worked

If we now make use of our deserialiser, it should end up calling our automatically generated `deserialise` function:

```
// https://godbolt.org/z/kUXvzB
void main()
{
    import std.stdio : writeln;

    auto person = new PersonClass("Bradley", 21, PersonType.Student);
    writeln(deserialise!PersonClass(parseJSON(`{ "name": "Bradley", "age": 21, "type": "Student" }`)));
}

/*
    Output:
        I am Bradley, I am 21 years old, and I'm a Student.
*/
```

## Conclusion

Even if this post was quite short and basic, it covers one of the last core features of D's metaprogramming features. The only other core
feature I haven't covered are [eponymous templates](https://dlang.org/spec/template.html#implicit_template_properties), as I couldn't think
of a way to fit them into the context of this serialiser.

Anyway, I hope the knowledge (and awful writing style!) of this tutorial series will have helped bootstrap your knowledge of D's metaprogramming, aiding
you in any future endevors.

If you've enjoyed this series, please share it around to whomever you may think will be interested.

If you have any suggestions or improvements, feel free to either directly propose changes using the "Improve on Github" button below; open an issue on Github, or even just
email me. I'm open to changes and criticism.

Thank you.
