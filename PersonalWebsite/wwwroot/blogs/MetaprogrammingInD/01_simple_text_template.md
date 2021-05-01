@date-created 01-05-2021@
@date-updated 01-05-2021@
@title Text templates that can execute code at compile-time@
@seo-tag dlang-metaprogramming-text-template-compile-time@
@seo-url /dlang-compile-time-text-templates@
@seo-description This post covers how you can make a text template format that can even execute D code to generate its values. All at compile time. All in one language.@

In this post we'll be creating a text template format that is capable of running arbitrary D code, and this is all done at compile-time!

This post briefly explains D-specific concepts, so even those that don't know much about D can still read along. Those more familiar with D
may not find this post too interesting, but I'd like some feedback via a Github issue if there's anything that can be improved.

--toc

## Introduction

Some projects may find it useful to be able to define external template files for certain needs such as: project scaffolding, HTML templates,
or whatever you want really!

While we can (and will) create a simple "Replace $VAR with string" style of text template, we can actually take things one step further with D -
we can allow the template to execute arbitrary D code in order to determine some of its values.

Whether this is a good idea or not is completely up to debate, but this is the groundwork required to make things like ASP's Razor templates, 
vibe-d's diet templates, or even just a simple DSL.

## Basic format and initial code setup

First of all, let's run `dub init` to create a new project, and then let's also make a folder called "views". 

The project structure should look something like this:

![Project Structure](/img/blogs/meta/meta1_project_structure.webp)

Before we dive into allowing execution of D code, let's just get the basics done first: Allow the template to define variables, then allow the program
to fill in those variables with values.

To keep things simple, variables are just strings that we patch into the template's text wherever they're specified.

This will be the example file we're working with, and we'll store this in `views/template.txt`:

```
DECLARE
    $NAME
    $AGE
    $HOBBIES
START
My name is $NAME I am $AGE years old and my hobbies are: $HOBBIES
```

Now let's open up the `source/app.d` file, and create the initial structure for our program:

```d
import std;

struct Document
{
    string[] declaredVariables;
    string templateText;
}

string resolve(string templateName)(string[string] variables)
{
    Appender!(char[]) output;

    return output.data.assumeUnique;
}

Document parseDocument(string contents)
{
    Document doc;

    return doc;
}

void main()
{
    writeln(resolve!"template.txt"(
    [
        "$NAME":    "Bradley",
        "$AGE":     "22",
        "$HOBBIES": "programming, complaining, and long walks at night."
    ]));
}
```

We start off with `import std` so we have access to the entire standard library.

The `Document` struct is used to model our template files. For now we simply have the names of any declared variables, and the text that we need to resolve.

The `resolve` function will be performing the actual resolution. It takes `templateName` as a compile-time parameter, and `variables` as a runtime one.

The `parseDocument` function will parse the template into a `Document` struct for us.

And finally, the `main` function shows how we're going to be using our `resolve` function, and also prints its output for us.

You may have noticed we're using std.array#Appender:Appender, and std.exception#assumeUnique:assumeUnique. You can click on their links
if you're interested in reading about them as these are more an implementation detail rather than anything crucial.

## Implementing parseDocument

Since the focus of this post is more-so the metaprogramming stuff we'll be doing later, I'll show you the code for
`parseDocument`, talk about it a bit, and then we'll move on as to not spend too much time here.

```d
Document parseDocument(string contents)
{
    enum Mode
    {
        none,
        declare,
        start
    }

    Document doc;
    Mode mode;

    foreach(line; contents.lineSplitter())
    {
        switch(mode) with(Mode)
        {
            case none:
                enforce(line == "DECLARE", "Templates must start with 'DECLARE'");
                mode = declare;
                break;

            case declare:
                if(line == "START")
                {
                    mode = start;
                    continue;
                }

                doc.declaredVariables ~= line.strip(' ');
                break;

            case start:
                // This code here is bad, but I wanted to keep things simple.
                if(doc.templateText.length > 0)
                    doc.templateText ~= '\n';
                doc.templateText ~= line;
                break;

            default: assert(false);
        }
    }

    return doc;
}
```

I have decided to use a simple state machine to perform the parsing, whose state is determined by which `Mode` it's in. 

D allows functions to have their own classes, structs, enums, etc. which is very useful for cases like this.

The `with(Mode)` statement allows us to write `Mode.none, Mode.declare, etc.` as simply `none, declare, etc.`, which is very nice when paired with `switch` statements.

Also, here's the documentation for std.exception#enforce if you'd like to read more about it.

So briefly, foreach line within contents:

* If we are not in a mode, ensure that the first line is "DECLARE", then move into 'declare' mode.

* If we are in declare mode and the line is "START", move into 'start' mode.

* If we are in declare mode and the line isn't "START", trim the spaces from the left and right, and add it into `declaredVariables`.

* If we are in start mode then join up all the remaining lines into `templateText`.

## Implementing resolve

There's a few things to talk about with this function, but because we haven't implemented allowing templates to execute code yet, I'll do the same thing
as before:

```d
string resolve(string templateName)(string[string] variables)
{
    // The types for these two values were made explicit for the user's comfort.
    // In general, D can infer types automatically in cases like this, so explicit typing is not needed.
    const string TEMPLATE_CONTENTS = import(templateName);
    enum Document Doc = parseDocument(TEMPLATE_CONTENTS);
    enforce(
        Doc.declaredVariables.all!(varName => varName in variables), 
        "Not all declared variables were given a value in 'variables'"
    );

    Appender!(char[]) output;

    string text = Doc.templateText;
    while(text.length > 0)
    {
        const nextVarStart = text.indexOf('$');
        if(nextVarStart < 0)
        {
            output.put(text);
            break;
        }

        output.put(text[0..nextVarStart]);
        text = text[nextVarStart..$];

        const nextSpace = text.indexOfAny(" \r\n");
        const varName   = (nextSpace < 0) ? text : text[0..nextSpace];
        text            = (nextSpace < 0) ? null : text[nextSpace..$];
        output.put(variables[varName]);
    }

    return output.data.assumeUnique;
}
```

To start off, we have the very interesting line of `const string TEMPLATE_CONTENTS = import(templateName)`.

Remember how we called one of the folders "views"? This is actually a string import folder that dub automatically tells the compiler about.
Now remember that in our `main` function we pass in `template.txt` as the `templateName` compile-time parameter?

Well, the `import(string)` statement allows us to directly embed a file from the string import folder into our executable, and access it via
a variable, which is `TEMPLATE_CONTENTS` in this case. So `resolve!"template.txt"` is the user telling us to import the contents of `views/template.txt`
so we can process it at compile-time.

We then come to yet another very interesting line: `enum Document Doc = parseDocument(TEMPLATE_CONTENTS);`.

An `enum` value is called a Manifest Constant. It is a value that has no physical location within your executable, and exists purely at compile-time.
Any usage of an enum value results in it becoming duplicated wherever it is used, kind of like `#define` from C/C++.

The next thing to note is that we're actually setting the value of `Doc` to the return result of `parseDocument`. If you aren't familiar with D,
this is called CTFE - Compile Time Function Execution/Evaluation.

So in other words, we're importing the contents of `template.txt`, and then parsing it into a `Document` by executing normal D code, **all at compile time**.
This is important for when we let templates execute their own code, which we'll start doing very soon.

The rest of this function is basically just replacing all instances of `$NAME` with the value of `variables["$NAME"]`. You can read through it if you want,
as I'm not going to explain it line-by-line.

## Intermission

Let's have a quick `dub run` of our code, and then move onto the more interesting stuff:

```
$> dub run
My name is Bradley I am 22 years old and my hobbies are: programming, complaining, and long walks at night.
```

So with our current code we can import templates and do a basic search-and-replace for things like `$NAME` using user-provided values.

We will now add onto our two functions, and also our example file, the ability to compile and run arbitrary D code within our executable.

## Final format

The final format for our text templates will look like this. This is also the contents of `views/template.txt`:

```
DECLARE
    $NAME
    $AGE
    $HOBBIES
COMPUTE
    $HOBBIES_LOUD : variables["$HOBBIES"].splitter(',').map!(str => "!!!"~str.strip(' ').toUpper~"!!!").fold!((a,b) => a~", "~b)
START
My name is $NAME I am $AGE years old and my hobbies are: $HOBBIES

If you couldn't hear me loud enough, my hobbies are: $HOBBIES_LOUD
```

We have added a new section called "COMPUTE" which contains variables whose values are computed from the specified D code.

Our `$HOBBIES_LOUD` variable splits up the `$HOBBIES` variable by comma, adds "!!!" as a suffix and prefix to each word, then turns all words into upper-case
before joining them back together separated by a comma. It's not too important what's there, as long as it's D code, and as long as it evaluates to a `string`.

## Upgrading Document and parseDocument

We now need to upgrade `Document` and `parseDocument` to include our new computed variables. New code is covered in comment lines:

```d
struct Document
{
    string[] declaredVariables;
    string templateText;

    /////////////////////////////////
    // Key is var name, value is D code.
    string[string] computedVariables;
    /////////////////////////////////
}

Document parseDocument(string contents)
{
    enum Mode
    {
        none,
        declare,
        start,

        ///////
        compute
        ///////
    }

    Document doc;
    Mode mode;

    foreach(line; contents.lineSplitter())
    {
        switch(mode) with(Mode)
        {
            case none:
                enforce(line == "DECLARE", "Templates must start with 'DECLARE'");
                mode = declare;
                break;

            case declare:
                if(line == "START")
                {
                    mode = start;
                    continue;
                }
                //////////////////////////
                else if(line == "COMPUTE")
                {
                    mode = compute;
                    continue;
                }
                //////////////////////////

                doc.declaredVariables ~= line.strip(' ');
                break;

            ////////////////////////////////////////////////////
            case compute:
                if(line == "START")
                {
                    mode = start;
                    continue;
                }

                const colon   = line.indexOf(':');
                const varName = line[0..colon].strip(' ');
                const code    = line[colon+1..$].strip(' ');
                doc.computedVariables[varName] = code;
                break;
            ////////////////////////////////////////////////////

            case start:
                // This code here is bad, but I wanted to keep things simple.
                if(doc.templateText.length > 0)
                    doc.templateText ~= '\n';
                doc.templateText ~= line;
                break;

            default: assert(false);
        }
    }

    return doc;
}
```

In short: 

* We add `computedVariables` to `Document` which is a dictionary where the key is the variable's name, and the value is the D code.

* We then add a new 'compute' mode for our "COMPUTE" section, which can only be used after the "DECLARE" section.

* If we are in 'compute' mode, then split the line by the first colon - everything on the left is the variable name, everything on the right is the D code to execute.

## Upgrading resolve

The ultimate question now is: "How on earth do you let external string files execute D code?".

This is where the magic happens, and you'll hopefully be shocked (if you're new to D) at how simple this is. We are only adding two lines of code:

```d
string resolve(string templateName)(string[string] variables)
{
    const string TEMPLATE_CONTENTS = import(templateName);
    enum Document Doc = parseDocument(TEMPLATE_CONTENTS);
    enforce(
        Doc.declaredVariables.all!(varName => varName in variables), 
        "Not all declared variables were given a value in 'variables'"
    );

    ////////////////////////////////////////////////////
    static foreach(varName, code; Doc.computedVariables)
        variables[varName] = mixin(code);
    ////////////////////////////////////////////////////

    Appender!(char[]) output;

    string text = Doc.templateText;
    while(text.length > 0)
    {
        const nextVarStart = text.indexOf('$');
        if(nextVarStart < 0)
        {
            output.put(text);
            break;
        }

        output.put(text[0..nextVarStart]);
        text = text[nextVarStart..$];

        const nextSpace = text.indexOfAny(" \r\n");
        const varName   = (nextSpace < 0) ? text : text[0..nextSpace];
        text            = (nextSpace < 0) ? null : text[nextSpace..$];
        output.put(variables[varName]);
    }

    return output.data.assumeUnique;
}
```

The first part is that we're making use of [static foreach](https://dlang.org/spec/version.html#staticforeach). Without going into too much
detail, it is basically a compile-time `foreach` that duplicates its body into the final code for every value it iterates over. The more technical term is that
it "unrolls" itself.

The final part is that we're using the statement `mixin(code)`. D has a feature called [string mixins](https://tour.dlang.org/tour/en/gems/string-mixins) which will
insert a compile-time string into your code, which is then compiled as you'd expect.

Recall that our `enum Document Doc` variable is a compile-time-only value because it is marked as an `enum`. This means we can use the data within this
variable directly with compile-time-only constructs such as `static foreach` and `mixin`.

So what our two lines are doing is that they're literally just taking the code from our template file, mixing them into our `resolve` function, and then
the result of the code is stored under the variable's name, which the rest of the function will handle for us.

Let's have a run:

```
$> dub run
My name is Bradley I am 22 years old and my hobbies are: programming, complaining, and long walks at night.

If you couldn't hear me loud enough, my hobbies are: !!!PROGRAMMING!!!, !!!COMPLAINING!!!, !!!AND LONG WALKS AT NIGHT.!!!
```

And there you have it. D is amazing at writing code that generates code, and you can see for yourself just how simple things are yet how powerful they are
when you combine all of D's different features together. All without preprocessors or external tooling. All within the same language.

Now, if you think about it, you technically know enough to start making things like the [pegged](https://code.dlang.org/packages/pegged) library - translating
a DSL into fully compiled D code for a specific purpose (i.e. parsing from a grammar). All you need to do is translate something into some D code, then `mixin`
said code.

## Having some fun

Just for a bit of fun let's make our template resolve another template. Create another template in `views/other.txt` and give it these contents:

```
DECLARE
    $HOWDY
START
HOWDY $HOWDY
```

Now let's update our `views/template.txt`:

```
DECLARE
    $NAME
    $AGE
    $HOBBIES
COMPUTE
    $HOBBIES_LOUD : variables["$HOBBIES"].splitter(',').map!(str => "!!!"~str.strip(' ').toUpper~"!!!").fold!((a,b) => a~", "~b)
    $MYSELF       : readText("views/template.txt")
    $OTHER        : resolve!("other.txt")(["$HOWDY": "Y'ALL"])
START
My name is $NAME I am $AGE years old and my hobbies are: $HOBBIES

If you couldn't hear me loud enough, my hobbies are: $HOBBIES_LOUD

I can even print myself!
$MYSELF

And even... other templates:
$OTHER
```

And then this gives us the result of:

```
My name is Bradley I am 22 years old and my hobbies are: programming, complaining, and long walks at night.

If you couldn't hear me loud enough, my hobbies are: !!!PROGRAMMING!!!, !!!COMPLAINING!!!, !!!AND LONG WALKS AT NIGHT.!!!

I can even print myself!
[omitted. Contents of views/template.txt]

And even... other templates:
HOWDY Y'ALL
```

## Conclusion

To conclude, our program from a high-level point of view is:

* Import the contents of a file using D's `import(string)` statement.

* Parse the contents of the file into a struct, stored as a compile-time value, by using D's [CTFE](https://tour.dlang.org/tour/en/gems/compile-time-function-evaluation-ctfe).

* Generate code for each "compute variable" inside the template using D's `static foreach`.

* Mixin external/processed strings as code using D's `mixin(string)` statement.

* Do other faff at runtime that we only slightly care about compared to the above.

I hope this has been interesting for at least a few readers, and this particular blog series is about one-off posts like this that shows how D can be used to
make your life just a little bit easier/fancier.