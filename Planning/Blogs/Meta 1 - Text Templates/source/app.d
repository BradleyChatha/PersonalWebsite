import std;

struct Document
{
    string[] declaredVariables;
    string[string] computedVariables;
    string templateText;
}

string resolve(string templateName)(string[string] variables)
{
    enum Doc = parseDocument(import(templateName));
    enforce(
        Doc.declaredVariables.all!(varName => varName in variables), 
        "Not all declared variables were given a value in 'variables'"
    );

    static foreach(varName, code; Doc.computedVariables)
        variables[varName] = mixin(code);

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

Document parseDocument(string contents)
{
    enum Mode
    {
        none,
        declare,
        start,
        compute
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
                else if(line == "COMPUTE")
                {
                    mode = compute;
                    continue;
                }

                doc.declaredVariables ~= line.strip(' ');
                break;

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

void main()
{
    writeln(resolve!"template.txt"(
    [
        "$NAME":    "Bradley",
        "$AGE":     "22",
        "$HOBBIES": "programming, complaining, and long walks at night."
    ]));
}