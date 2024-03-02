﻿using System.Text;
using System.Text.RegularExpressions;

namespace EZCodeLanguage
{
    public class Tokenizer
    {
        public class Token
        {
            public TokenType Type { get; set; }
            public object Value { get; set; }
            public Token(TokenType type, object value)
            {
                Type = type;
                Value = value; 
            }
        }
        public class Line
        {
            public string Value { get; set; }
            public int CodeLine { get; set; }
            public Line(string code, int line)
            {
                this.Value = code;
                this.CodeLine = line;
            }
        }
        public class Argument
        {
            public enum ArgAdds { And, Or, None }
            public ArgAdds ArgAdd = ArgAdds.None;
            public Token[] Tokens { get; set; }
            public Line Line { get; set; }
            public string Value { get; set; }
            public Argument(Token[] tokens, Line line, string value, ArgAdds argAdds = ArgAdds.None)
            {
                Tokens = tokens;
                Line = line;
                Value = value;
                ArgAdd = argAdds;
            }
            public Argument[]? Args()
            {
                Argument[] arguments = [];
                Token[] tokens = [];
                for (int i = 0; i < Tokens.Length; i++)
                {
                    if (Tokens[i].Type == TokenType.And)
                    {
                        arguments = [.. arguments, new Argument(tokens, Line, string.Join(" ", tokens.Select(x=>x.Value)), ArgAdds.And)];
                        tokens = [];
                        continue;
                    }
                    else if (Tokens[i].Type == TokenType.Or)
                    {
                        arguments = [.. arguments, new Argument(tokens, Line, string.Join(" ", tokens.Select(x => x.Value)), ArgAdds.Or)];
                        tokens = [];
                        continue;
                    }
                    else
                    {
                        tokens = [.. tokens, Tokens[i]];
                    }
                }
                arguments = [.. arguments, new Argument(tokens, Line, string.Join(" ", tokens.Select(x => x.Value)))];

                return arguments;
            }
            public static bool? EvaluateTerm(string input)
            {
                switch (input.ToLower())
                {
                    case "true": case "y": case "yes": case "1": return true;
                    case "false": case "n": case "no": case "0": return false;
                    default: return null;
                }
            }
        }
        public class Statement
        {
            public Argument? Argument { get; set; }
            public Line Line { get; set; }
            public LineWithTokens[] InBrackets { get; set; }
            public static string[] Types = ["if", "elif", "else", "loop", "try", "fail"];
            public static string[] ConditionalTypes = ["if", "elif", "loop"];
            public static string[] NonConditionalTypes = ["else", "try", "fail"];
            public string Type { get; set; }
            public Statement(string type, Line line, LineWithTokens[] insides, Argument? argument = null)
            {
                Type = type;
                Line = line;
                InBrackets = insides;
                if (ConditionalTypes.Contains(type))
                {
                    Argument = argument;
                }
            }
            
        }
        public class DataType
        {
            public enum Types {
                NotSet,
                _object,
                _string,
                _int,
                _float,
                _bool,
                _char,
                _double,
                _decimal,
                _long,
                _uint,
                _ulong,
            }
            public Types Type { get; set; }
            public Class? ObjectClass { get; set; }
            public Container? ObjectContainer { get; set; }
            public DataType(Types type, Class? _class, Container? container = null)
            {
                Type = type;
                ObjectClass = _class;
                ObjectContainer = container;
            }
            public DataType() { }
            public static DataType UnSet = new DataType() { Type = Types.NotSet };
            public static DataType GetType(string param, Class[] classes, Container[] containers)
            {
                Types types = new();
                Class _class = new();
                Container container = new();
                param = param.Replace("@", "");
                switch (param)
                {
                    case "string": case "str": types = Types._string; break;
                    case "int": types = Types._int; break;
                    case "float": types = Types._float; break;
                    case "bool": types = Types._bool; break;
                    default: types = Types._object; break;
                }
                _class = classes.FirstOrDefault(x => x.Name == param, null);
                if (_class == null) container = containers.FirstOrDefault(x => x.Name == param, null);

                return new(types, _class, container);
            }
        }
        public class ExplicitWatch
        {
            public string Pattern { get; set; }
            public Var[]? Vars { get; private set; }
            public RunMethod Runs { get; set; }
            public ExplicitWatch(string format, RunMethod run, Var[] vars)
            {
                Pattern = format.Replace("{", "(?<").Replace("}", ">\\w+)");
                Runs = run;
                Vars = vars;
            }
            public bool IsFound(string input, Class[] classes, Container[] containers)
            {
                Match match = Regex.Match(input, Pattern);
                if (match.Success)
                {
                    GroupCollection groups = match.Groups;
                    int groupCount = groups.Count;
                    string[] capturedValues = new string[groupCount - 2];
                    for (int i = 1; i < groupCount - 1; i++)
                    {
                        capturedValues[i - 1] = groups[i].Value;
                    }
                    Var[] vars = [];
                    for (int i = 0; i < capturedValues.Length; i++)
                    {
                        string name = capturedValues[i], type = "";
                        if (capturedValues[i].Contains(":"))
                        {
                            type = capturedValues[i].Split(":")[0];
                            name = capturedValues[i].Split(":")[1];
                        }
                        vars = vars.Append(new Var(Vars[i].Name, capturedValues[i], Vars[i].Line, type != "" ? DataType.GetType(type, classes, containers) : DataType.UnSet)).ToArray();
                    }
                    Runs.Parameters = vars;
                    return true;
                }
                return false;
            }
        }
        public class ExplicitParams
        {
            public string Pattern { get; set; }
            public Var[]? Vars { get; private set; }
            public RunMethod Runs { get; set; }
            public bool IsOverride { get; set; }
            public ExplicitParams(string format, RunMethod run, Var[] vars, bool overide)
            {
                Pattern = format;
                Runs = run;
                Vars = vars;
                IsOverride = overide;
            }
            public bool IsFound(string input, Class[] classes, Container[] containers)
            {
                ExplicitWatch watch = new ExplicitWatch(Pattern, Runs, Vars);
                return watch.IsFound(Regex.Escape(input), classes, containers);
            }
            public ExplicitParams(ExplicitWatch w)
            {
                Pattern = w.Pattern;
                Vars = w.Vars;
                Runs = w.Runs;
            }
        }
        public class RunMethod
        {
            public string? ClassName { get; set; }
            public Method Runs { get; set; }
            public Var[]? Parameters { get; set; }
            public Token[] Tokens { get; set; }
            public RunMethod(Method method, Var[] vars, string? classname, Token[] tokens)
            {
                Runs = method;
                Parameters = vars;
                ClassName = classname;
                Tokens = tokens;
            }
        }
        public class Method
        {
            public string Name { get; set; }
            public Line Line { get; set; }
            [Flags] public enum MethodSettings
            {
                None = 0,
                Static = 1,
                NoCol = 2,
            }
            public MethodSettings Settings { get; set; }
            public LineWithTokens[] Lines { get; set; }
            public DataType? Returns { get; set; } 
            public Var[]? Params { get; set; }
            public Method(string name, Line line, MethodSettings methodSettings, LineWithTokens[] lines, Var[]? param = null, DataType? returns = null)
            {
                Name = name;
                Line = line;
                Settings = methodSettings;
                Lines = lines;
                Params = param;
                Returns = returns;
            }
            public Method() { }
        }
        public class GetValueMethod
        {
            public DataType DataType { get; set; }
            public Method Method { get; set; }
            public GetValueMethod() { }
            public GetValueMethod(DataType dataType, Method method)
            {
                DataType = dataType; 
                Method = method;
            }
        }
        public class Class
        {
            public GetValueMethod[]? GetTypes { get; set; }
            public ExplicitWatch[] WatchFormat { get; set; }
            public DataType? TypeOf { get; set; }
            public ExplicitParams? Params { get; set; }
            public string Name { get; set; }
            public Line Line { get; set; }
            public Method[]? Methods { get; set; }
            public Var[]? Properties { get; set; }
            public Class[]? Classes { get; set; }
            public DataType[] InsideOf { get; set; }
            public int Length { get; set; }
            [Flags] public enum ClassSettings
            {
                None = 0,
                Static = 1,
                Semi = 2,
                Ontop = 4
            }
            public ClassSettings Settings { get; set; }
            public Class() { }
            public Class(string name, Line line, Method[]? methods = null, ClassSettings settings = ClassSettings.None, Var[]? properties = null, ExplicitWatch[]? watchForamt = null, 
                ExplicitParams? explicitParams = null, DataType? datatype = null, GetValueMethod[]? getValue = null, Class[]? classes = null, DataType[]? insideof = null, int length = 0)
            {
                Name = name;
                Line = line;
                Methods = methods ?? [];
                Settings = settings;
                Properties = properties ?? [];
                WatchFormat = watchForamt ?? [];
                Params = explicitParams;
                TypeOf = datatype;
                GetTypes = getValue ?? [];
                Classes = classes ?? [];
                InsideOf = insideof ?? [];
                Length = length;
            }
            public Class(Class cl)
            {
                Name = cl.Name;
                Line = cl.Line;
                Methods = cl.Methods ?? [];
                Settings = cl.Settings;
                Properties = cl.Properties ?? [];
                WatchFormat = cl.WatchFormat ?? [];
                Params = cl.Params;
                TypeOf = cl.TypeOf;
                GetTypes = cl.GetTypes ?? [];
                Classes = cl.Classes ?? [];
                InsideOf = cl.InsideOf ?? [];
                Length = cl.Length;
            }
        }
        public class Var
        {
            public string? Name { get; set; }
            public object? Value { get; set; }
            public DataType? DataType { get; set; }
            public Line Line { get; set; }
            public bool Required { get; set; }
            public Var() { }
            public Var(string? name, object? value, Line line, DataType? type = null, bool optional = true)
            {
                type ??= DataType.UnSet;
                Name = name;
                Value = value;
                Line = line;
                DataType = type;
                Required = optional;
            }
            public object? GetFromType(DataType data)
            {
                object? value = null;
                switch (data.Type)
                {
                    case DataType.Types._object:
                        break;
                    case DataType.Types._string:
                        break;
                    case DataType.Types._int:
                        break;
                    case DataType.Types._float:
                        break;
                    case DataType.Types._bool:
                        break;
                    case DataType.Types._char:
                        break;
                    case DataType.Types._double:
                        break;
                    case DataType.Types._decimal:
                        break;
                    case DataType.Types._long:
                        break;
                    case DataType.Types._uint:
                        break;
                    case DataType.Types._ulong:
                        break;
                }
                return value;
            }
        }
        public class Container
        {
            public Container() { }
            public Container(string name, Class[] classes, Line line)
            {
                Name = name;
                Classes = classes;
                Line = line;
            }
            public string Name { get; set; }
            public Class[] Classes { get; set; }
            public Line Line { get; set; }
        }
        public class LineWithTokens
        {
            public Line Line { get; set; }
            public Token[] Tokens { get; set; }
            public LineWithTokens() { }
            public LineWithTokens(Token[] tokens, Line line)
            {
                Line = line;
                Tokens = tokens;
            }
            public LineWithTokens(LineWithTokens line)
            {
                Line = line.Line;
                Tokens = line.Tokens;
            }
        }
        public class CSharpMethod(string path, string[]? @params, bool isVar)
        {
            public string Path { get; set; } = path;
            public string[]? Params { get; set; } = @params;
            public bool IsVar { get; set; } = isVar;
        }
        public class CSharpDataType(string path, string type)
        {
            public string Path { get; set; } = path;
            public string Type { get; set; } = type;
        }
        public enum TokenType
        {
            None,
            Null,
            Comment,
            Comma,
            QuestionMark,
            Colon,
            Arrow,
            DataType,
            OpenCurlyBracket,
            CloseCurlyBracket,
            New,
            If,
            Else,
            Elif,
            Loop,
            Try,
            Fail,
            Argument,
            Identifier,
            Undefined,
            Class,
            Static,
            Explicit,
            Watch,
            Params,
            TypeOf,
            InsideOf,
            Semi,
            Ontop,
            NoCol,
            Method,
            Match,
            Container,
            Return,
            Get,
            And,
            Not,
            Or,
            Make,
            Is,
            RunExec,
            EZCodeDataType,
            Include,
            Exclude,
            Override
        }
        public char[] Delimeters = [' ', '{', '}', '@', ':', ',', '?'];
        public string Code { get; set; }
        public List<Class> Classes = [];
        public List<Container> Containers = [];
        public List<Method> Methods = [];
        public LineWithTokens[] Tokens = Array.Empty<LineWithTokens>();
        public Tokenizer() { }
        public Tokenizer(string code)
        {
            Code = code;
        }
        public LineWithTokens[] Tokenize(string code)
        {
            return Tokens = TokenArray(code).Where(x => x.Line.Value.ToString() != "").ToArray();
        }
        private LineWithTokens[] TokenArray(string code, bool insideClass = false)
        {
            List<LineWithTokens> withTokens = new List<LineWithTokens>();
            Line[] Lines = SplitLine(code);

            for (int i = 0; i < Lines.Length; i++)
            {
                List<Token> tokens = new List<Token>();
                Line line = Lines[i];
                object[] parts = SplitParts(ref Lines, i, out int continues, insideClass);
                for (int j = 0; j < parts.Length; j++)
                {
                    Token token = SingleToken(parts, j, out bool stops);
                    if (token.Type != TokenType.None) tokens.Add(token);
                    if (stops) continue;
                }
                i += continues;
                line.CodeLine += 1;

                withTokens.Add(new(tokens.ToArray(), line));
            }
            return withTokens.ToArray();
        }
        private Token SingleToken(object[] parts, int partIndex) =>
            SingleToken(parts, partIndex, out bool stops);
        private Token SingleToken(object[] parts, int partIndex, out bool stops)
        {
            stops = false;
            TokenType tokenType = TokenType.None;
            if (parts[partIndex] is string)
            {
                string part = parts[partIndex] as string;
                switch (part)
                {
                    default: tokenType = TokenType.Identifier; break;
                    case "!": case "not": tokenType = TokenType.Not; break;
                    case "&": case "&&": case "and": tokenType = TokenType.And; break;
                    case "|": case "||": case "or": tokenType = TokenType.Or; break;
                    case "//": tokenType = TokenType.Comment; stops = true; break;
                    case "=>": tokenType = TokenType.Arrow; break;
                    case ":": tokenType = TokenType.Colon; break;
                    case "{": tokenType = TokenType.OpenCurlyBracket; break;
                    case "}": tokenType = TokenType.CloseCurlyBracket; break;
                    case "?": tokenType = TokenType.QuestionMark; break;
                    case ",": tokenType = TokenType.Comma; break;
                    case "undefined": tokenType = TokenType.Undefined; break;
                    case "static": tokenType = TokenType.Static; break;
                    case "explicit": tokenType = TokenType.Explicit; break;
                    case "watch": tokenType = TokenType.Watch; break;
                    case "override": tokenType = TokenType.Override; break;
                    case "params": tokenType = TokenType.Params; break;
                    case "insideof": tokenType = TokenType.InsideOf; break;
                    case "typeof": tokenType = TokenType.TypeOf; break;
                    case "semi": tokenType = TokenType.Semi; break;
                    case "ontop": tokenType = TokenType.Ontop; break;
                    case "nocol": tokenType = TokenType.NoCol; break;
                    case "method": tokenType = TokenType.Method; break;
                    case "return": tokenType = TokenType.Return; break;
                    case "is": tokenType = TokenType.Is; break;
                    case "get": tokenType = TokenType.Get; break;
                    case "new": tokenType = TokenType.New; break;
                    case "make": tokenType = TokenType.Make; break;
                    case "null": tokenType = TokenType.Null; parts[partIndex] = ""; break;
                }
                if (part.StartsWith("//")) tokenType = TokenType.Comment;
                if (part.StartsWith('@')) tokenType = TokenType.DataType;
            }
            else if (parts[partIndex] is Statement)
            {
                Statement part = (Statement)parts[partIndex];

                switch (part.Type)
                {
                    case "if": tokenType = TokenType.If; break;
                    case "elif": tokenType = TokenType.Elif; break;
                    case "else": tokenType = TokenType.Else; break;
                    case "loop": tokenType = TokenType.Loop; break;
                    case "try": tokenType = TokenType.Try; break;
                    case "fail": tokenType = TokenType.Fail; break;
                }
            }
            else if (parts[partIndex] is Class)
            {
                tokenType = TokenType.Class;
            }
            else if (parts[partIndex] is RunMethod)
            {
                tokenType = TokenType.Match;
            }
            else if (parts[partIndex] is Method)
            {
                tokenType = TokenType.Method;
            }
            else if (parts[partIndex] is CSharpMethod)
            {
                tokenType = TokenType.RunExec;
            }
            else if (parts[partIndex] is CSharpDataType)
            {
                tokenType = TokenType.EZCodeDataType;
            }
            else if (parts[partIndex] is Container)
            {
                tokenType = TokenType.Container;
            }

            return new Token(tokenType, parts[partIndex]);
        }
        public object[] SplitParts(ref Line[] lines, int lineIndex, out int continues, bool insideClass = false)
        {
            string line = lines[lineIndex].Value;
            object[] parts = SplitWithDelimiters(line, Delimeters).Where(x => x != "" && x != " ").Select(x => (object)x).ToArray();
            string[] partsSpaces = line.Split(" ").Where(x => x != "" && x != " ").ToArray();
            continues = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                try
                {
                    if (parts[i].ToString() == "->")
                    {
                        if (i == parts.Length - 1)
                        {
                            parts = parts.Append(lines[i + 1].Value).ToArray();
                            continues++;
                        }
                    }
                    else if (parts[i].ToString() == "@")
                    {
                        parts[i] = "@" + parts[i + 1];
                        parts = parts.ToList().Where((item, index) => index != i + 1).ToArray();
                    }
                    else if (parts[i].ToString().StartsWith("//"))
                    {
                        parts[i] = string.Join(" ", parts.Skip(i));
                        parts = parts.ToList().Where((item, index) => index <= i).ToArray();
                        break;
                    }
                    else if (Statement.Types.Contains(parts[i]))
                    {
                        Statement statement = SetStatement(ref lines, lineIndex, i);
                        parts = [statement];
                    }
                    else if (parts[i].ToString() == "class")
                    {
                        string name = parts[i + 1].ToString();

                        Class.ClassSettings settings =
                            (line.Contains("static ") ? Class.ClassSettings.Static : Class.ClassSettings.None) |
                            (line.Contains("ontop ") ? Class.ClassSettings.Ontop : Class.ClassSettings.None) |
                            (line.Contains("semi ") ? Class.ClassSettings.Semi : Class.ClassSettings.None);

                        Var[] properties = [];
                        Method[] methods = [];
                        GetValueMethod[] getValueMethods = [];
                        ExplicitWatch[]? explicitWatch = [];
                        ExplicitParams? explicitParams = null;
                        DataType? typeOf = null;
                        Line nextLine = lines[lineIndex + 1];
                        bool sameLineBracket = nextLine.Value.StartsWith('{');
                        List<Line> l = [.. lines];
                        int curleyBrackets = sameLineBracket ? 0 : 1;
                        string[] watchFormats = [], watchNames = [];
                        string paramFormat = "", paramName = "";
                        List<Var[]> watchVars = new List<Var[]>();
                        List<Token[]> propertyTokens = new List<Token[]>();
                        List<Line> propertyLine = new List<Line>();
                        Var[]? paramVars = null;
                        bool paramoveride = false;
                        Token[] paramTokens = [], watchTokens = [];
                        DataType[] insideof = [];
                        Class[] classes = [];
                        int length = 0;
                        for (int j = lineIndex + 1, skip = 0; j < lines.Length; j++, skip -= skip > 0 ? 1 : 0)
                        {
                            Line bracketLine = lines[j];
                            LineWithTokens bracketLineTokens = TokenArray(bracketLine.Value, true)[0];
                            if (bracketLineTokens.Tokens[0].Value.ToString() == "class")
                            {
                                bracketLineTokens = TokenArray(string.Join(Environment.NewLine, lines.Select(x=>x.Value).Skip(j)), true)[0];
                                skip += (bracketLineTokens.Tokens[0].Value as Class).Length + 1;
                            }
                            if (skip == 0)
                            {
                                bool ismethod = false, isproperty = false, isexplicit = false, iswatch = false, isparam = false,
                                    istypeof = false, isinsideof = false, isget = false, isclass = false, isoverride = false;
                                for (int k = 0; k < bracketLineTokens.Tokens.Length; k++)
                                {
                                    bracketLineTokens.Line.CodeLine = lines[lineIndex].CodeLine + k;
                                    if (bracketLineTokens.Tokens[k].Type == TokenType.New || bracketLineTokens.Tokens[k].Type == TokenType.Undefined) isproperty = true;
                                    if (bracketLineTokens.Tokens[k].Type == TokenType.Method) ismethod = true;
                                    if (bracketLineTokens.Tokens[k].Type == TokenType.Explicit) isexplicit = true;
                                    if (bracketLineTokens.Tokens[k].Type == TokenType.Get) isget = true;
                                    if (bracketLineTokens.Tokens[k].Type == TokenType.Class) isclass = true;
                                    if (bracketLineTokens.Tokens[k].Type == TokenType.Override) isoverride = true;
                                    if (isexplicit && bracketLineTokens.Tokens[k].Type == TokenType.Watch) iswatch = true;
                                    if (isexplicit && bracketLineTokens.Tokens[k].Type == TokenType.Params) isparam = true;
                                    if (isexplicit && bracketLineTokens.Tokens[k].Type == TokenType.TypeOf) istypeof = true;
                                    if (isexplicit && bracketLineTokens.Tokens[k].Type == TokenType.InsideOf) isinsideof = true;
                                }
                                if (isproperty)
                                {
                                    propertyTokens.Add(bracketLineTokens.Tokens);
                                    propertyLine.Add(lines[j]);
                                }
                                else if (ismethod)
                                {
                                    Method method = SetMethod(lines, j);
                                    methods = methods.Append(method).ToArray();
                                    skip += method.Lines.Length;
                                }
                                else if (iswatch)
                                {
                                    watchFormats = [.. watchFormats, string.Join("", bracketLineTokens.Tokens.Skip(2).TakeWhile(x => x.Type != TokenType.Arrow).Select(x => x.Value))];
                                    watchNames = [.. watchNames, bracketLineTokens.Tokens.SkipWhile(x => x.Type != TokenType.Arrow).TakeWhile(x => x.Type != TokenType.Colon).ToArray()[1].Value.ToString()];
                                    Token[] varTokens = bracketLineTokens.Tokens.SkipWhile(x => x.Type != TokenType.Arrow).Skip(1).ToArray();
                                    watchVars.Add(GetVarsFromParameter(varTokens, lines[j]));
                                    watchTokens = bracketLineTokens.Tokens;
                                }
                                else if (isparam)
                                {
                                    paramFormat = string.Join("", bracketLineTokens.Tokens.Skip(isoverride ? 3 : 2).TakeWhile(x => x.Type != TokenType.Arrow).Select(x => x.Value));
                                    paramName = bracketLineTokens.Tokens.SkipWhile(x => x.Type != TokenType.Arrow).TakeWhile(x => x.Type != TokenType.Colon).ToArray()[1].Value.ToString();
                                    Token[] varTokens = bracketLineTokens.Tokens.SkipWhile(x => x.Type != TokenType.Arrow).Skip(1).ToArray();
                                    paramVars = GetVarsFromParameter(varTokens, lines[j]);
                                    paramoveride = isoverride;
                                    paramTokens = bracketLineTokens.Tokens;
                                }
                                else if (istypeof)
                                {
                                    Token token = bracketLineTokens.Tokens.SkipWhile(x => x.Type != TokenType.Arrow).Skip(1).ToArray()[0];
                                    if (token.Type == TokenType.EZCodeDataType)
                                    {
                                        string[] exp = token.Value.ToString().Split(['(', ')', '.']).Where(x => x != "").ToArray();
                                        string type = "@" + exp[exp.Length - 1].Remove(0, 1);
                                        type = type.Remove(type.Length - 1, 1);
                                        typeOf = DataType.GetType(type, Classes.ToArray(), Containers.ToArray());
                                    }
                                }
                                else if (isinsideof)
                                {
                                    Token token = bracketLineTokens.Tokens.SkipWhile(x => x.Type != TokenType.Arrow).Skip(1).ToArray()[0];
                                    if (token.Type == TokenType.DataType)
                                    {
                                        insideof = [.. insideof, DataType.GetType(token.Value.ToString(), Classes.ToArray(), Containers.ToArray())];
                                    }
                                }
                                else if (isclass)
                                {
                                    classes = [.. classes, bracketLineTokens.Tokens[0].Value as Class];
                                }
                                else if (isget)
                                {
                                    GetValueMethod getVal = SetGetVal(lines, j);
                                    getValueMethods = [.. getValueMethods, getVal];
                                    skip += getVal.Method.Lines.Length;
                                }
                            }
                            if (bracketLine.Value.Contains('{'))
                                curleyBrackets++;
                            if (bracketLine.Value.Contains('}'))
                                curleyBrackets--;
                            string code = bracketLineTokens.Line.Value;
                            l.Remove(bracketLine);
                            length = j;
                            if (curleyBrackets == 0)
                                break;
                        }
                        lines = [.. l];
                        for (int j = 0; j < watchFormats.Length; j++)
                            explicitWatch = [.. explicitWatch, new ExplicitWatch(watchFormats[j], new(methods.FirstOrDefault(x => x.Name == watchNames[j], null), watchVars[j] != null ? watchVars[j] : null, name, watchTokens), watchVars[j])];
                        if (paramName != "")
                            explicitParams = new ExplicitParams(paramFormat, new RunMethod(methods.FirstOrDefault(x => x.Name == paramName, null), paramVars != null ? paramVars : null, name, paramTokens), paramVars, paramoveride);
                        for (int j = 0; j < propertyTokens.Count; j++)
                            properties = [.. properties, SetVar(propertyLine[j], propertyTokens[j])];

                        Class @class = new(name, lines[lineIndex], methods, settings, properties, explicitWatch, explicitParams, typeOf, getValueMethods, classes, insideof, length);
                        if (Classes.Any(x => x.Name == name) != false)
                        {
                            Class oc = Classes.FirstOrDefault(x => x.Name == name);
                            oc = @class;
                        }
                        else
                        {
                            Classes.Add(@class);
                        }
                        parts = [@class];
                    }
                    else if (WatchIsFound(parts, i, out ExplicitWatch? watch))
                    {
                        parts[i] = watch.Runs;
                    }
                    else if (!insideClass && parts[i].ToString() == "method")
                    {
                        Method method = SetMethod(ref lines, lineIndex);
                        parts = [method];
                        if (Methods.Any(x => x.Name == method.Name) != false)
                        {
                            Method om = Methods.FirstOrDefault(x => x.Name == method.Name);
                            om = method;
                        }
                        else
                        {
                            Methods.Add(method);
                        }
                    }
                    else if (parts[i].ToString() == "make")
                    {
                        string[] both = string.Join(" ", partsSpaces.Skip(1)).Split("=>").Select(x => x.Trim()).ToArray();
                        string take = both[0];
                        string replace = both.Length == (0 | 1) ? "" : both[1];
                        int next = lineIndex + 1;
                        string takeMulti = "", replaceMulti = "";
                        if (take.Trim() == "{")
                        {
                            // Handle multi-line matching
                            StringBuilder multiLineTake = new StringBuilder();
                            int braceCount = 1;

                            for (int j = next; j < lines.Length; j++)
                            {
                                braceCount -= lines[j].Value.Trim().StartsWith('}') ? 1 : 0;

                                if (braceCount == 0)
                                    break;

                                multiLineTake.AppendLine(lines[j].Value.ToString());

                                next++;
                            }
                            if (!lines[next].Value.Contains("=>"))
                            {
                                return null;
                            }
                            else
                            {
                                replace = lines[next].Value.Split("=>")[1];
                            }

                            next++;
                            take = multiLineTake.ToString();
                            takeMulti = take.Split(['\n', '\r']).Select(x=>x.Trim()).ToArray()[0];
                        }
                        if (replace.Trim() == "{")
                        {
                            // Handle multi-line matching
                            StringBuilder multiLineTake = new StringBuilder();
                            int braceCount = 1;

                            for (int j = next; j < lines.Length; j++)
                            {
                                braceCount -= lines[j].Value.Trim().StartsWith('}') ? 1 : 0;

                                if (braceCount == 0)
                                    break;

                                multiLineTake.AppendLine(lines[j].Value.ToString());
                                next++;
                            }

                            next++;
                            replace = multiLineTake.ToString();
                            replaceMulti = replace.Split('\n').Select(x => x.Trim()).ToArray()[0];
                        }
                        string[] takeLines = take.Split("\n").Select(x => x.Trim()).Where(y => y != "").ToArray();
                        int line_removes = 0, lr = -1;
                        var _LINES = lines.ToList();

                        for (int j = lineIndex + 1; j < lines.Length; j++)
                        {
                            if (next > lineIndex + 1)
                            {
                                lr = lr == -1 ? j : lr;
                                line_removes++;
                                next--;
                                continue;
                            }
                            string input = lines[j].Value.ToString();
                            if (takeMulti != "")
                            {
                                string rep = replace;
                                if (MakeMatch(input, takeLines[0], ref rep, out string output, false))
                                {
                                    bool match = false;
                                    Line[] takes = [new Line(output, j)];
                                    for (int k = 1; k < takeLines.Length; k++)
                                    {
                                        match = MakeMatch(lines[j + k].Value.ToString(), takeLines[k], ref rep, out string o, false);
                                        if (!match) goto OuterLoop; 
                                        takes = takes.Append(new Line(o, j + k)).ToArray();
                                    }
                                    _LINES.RemoveRange(takes[0].CodeLine, takes[takes.Length - 1].CodeLine - takes[0].CodeLine + 1);
                                    rep = replace;
                                    bool m = MakeMatchMulti(string.Join(Environment.NewLine, takes.Select(x => x.Value.ToString())), take, ref rep, out string val, true);
                                    var vals = val.Split(Environment.NewLine).Where(x=>x != "").ToArray();
                                    for (int k = vals.Length - 1; k >= 0; k--)
                                    {
                                        _LINES.Insert(takes[0].CodeLine, new Line(vals[k], takes[0].CodeLine + k)); 
                                    }
                                }
                            }
                            else
                            {
                                string _ref = replace;
                                if (MakeMatch(input, take, ref _ref, out string output, true))
                                {
                                    if (replaceMulti != "")
                                    {
                                        bool m = MakeMatchMulti(input, take, ref _ref, out string val, true);
                                        var vals = val.Split(Environment.NewLine).Where(x => x != "").ToArray();
                                        _LINES.RemoveAt(j);
                                        for (int k = vals.Length - 1; k >= 0; k--)
                                        {
                                            _LINES.Insert(j, new Line(vals[k], j + k));
                                        }
                                    }
                                    else
                                    {
                                        lines[j].Value = output;
                                    }
                                }
                            }

                        OuterLoop: 
                            continue;
                        }
                        for (int j = 0; j < line_removes; j++)
                            _LINES.RemoveAt(lr);
                        lines = _LINES.ToArray();
                        parts = ["make"];
                    }
                    else if (parts[i].ToString().StartsWith("EZCodeLanguage.EZCode.DataType("))
                    {
                        string part = parts[i].ToString();
                        int ch = Array.IndexOf(part.ToCharArray(), '(');
                        string path = part[..ch];
                        string type = part.Substring(ch + 1, part.Length - ch - 2).Replace("\"", "");

                        parts[i] = new CSharpDataType(path, type);
                    }
                    else if (parts[i].ToString() == "runexec")
                    {
                        string path = "";
                        string[]? vars = null;
                        int skip = 1;
                        if (parts[i + 1].ToString() == "=>")
                        {
                            skip++;
                            path = parts[i + 2].ToString();
                            if (parts.Length - 1 > 3 && parts[i + 3].ToString() == "~>")
                            {
                                skip++;
                                string[] str = parts.Select(x => x.ToString()).Skip(4 + i).ToArray();
                                skip += str.Length;
                                string all = string.Join(" ", str);
                                str = all.Split(",").Select(x=>x.Trim()).ToArray();
                                vars = [];
                                for (int j = 0; j < str.Length; j++)
                                {
                                    vars = [.. vars, str[j]];
                                }
                            }
                        }
                        for (int j = 0; j <= skip; j++)
                        {
                            parts = parts.ToList().Where((item, index) => index != i + 1).ToArray();
                        }
                        parts[i] =  new CSharpMethod(path, vars, path.Contains('\''));
                    }
                    else if (parts[i].ToString() == "container")
                    {
                        string name = parts[i + 1].ToString();
                        Class[] classes = [];
                        Line nextLine = lines[lineIndex + 1];
                        bool sameLineBracket = nextLine.Value.StartsWith('{');
                        List<Line> l = [.. lines];
                        int curleyBrackets = sameLineBracket ? 0 : 1;
                        for (int j = lineIndex + 1; j < lines.Length; j++)
                        {
                            Line bracketLine = lines[j];
                            if (bracketLine.Value.Contains('{'))
                                curleyBrackets++;
                            if (bracketLine.Value.Contains('}'))
                                curleyBrackets--;

                            classes = classes.Append(Classes.FirstOrDefault(x => x.Name == bracketLine.Value)).ToArray();

                            l.Remove(bracketLine);
                            if (curleyBrackets == 0)
                                break;
                        }
                        lines = [.. l];
                        parts = [new Container(name, classes, lines[lineIndex])];
                    }
                }
                catch
                {

                }
            }
            return parts;
        }
        private static bool MakeMatch(string input, string pattern, ref string replace, out string output, bool format)
        {
            string newPat = pattern.Replace("\\{", "\\<[[>").Replace("\\}", "\\<]]>").Replace("{", "(?<").Replace("}", ">\\S+)").Replace("\\<[[>", "\\{").Replace("\\<]]>", "\\}");
            Match match = Regex.Match(input, newPat);
            if (match.Success)
            {
                GroupCollection groups = match.Groups;
                for (int k = 1; k < groups.Count; k++)
                {
                    string placeholder = $"{{{groups[k].Name}}}";
                    string capturedValue = groups[k].Value;
                    replace = replace.Replace(placeholder, capturedValue);
                }
                var l = replace.Split(Environment.NewLine);
                for (int i = 0; i < l.Length; i++)
                    l[i] = string.Join(" ", l[i].Split(" ").Select(x => x.Replace("\\\\{", "\\<[[>").Replace("\\\\}", "\\<]]>").Replace("\\{", "{").Replace("\\}", "}").Replace("\\<[[>", "\\{").Replace("\\<]]>", "\\}")));
                replace = string.Join(Environment.NewLine, l);
                if (format) output = input.Substring(0, match.Index) + replace + input.Substring(match.Index + match.Length);
                else output = input;
                return true;
            }
            output = "";
            return false;
        }
        private static bool MakeMatchMulti(string input, string pattern, ref string replace, out string output, bool format)
        {
            string[] inputLines = input.Split('\n').Select(x => x.Trim()).ToArray();
            string[] patternLines = pattern.Split('\n').Select(x => x.Trim()).ToArray();
            output = "";

            for (int i = 0; i < inputLines.Length; i++)
            {
                string line = inputLines[i];
                string lineOutput;
                bool lineMatch = MakeMatch(line, patternLines[i], ref replace, out lineOutput, format);

                if (lineMatch)
                {
                    output = lineOutput;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        internal bool WatchIsFound(object[] parts, int index, out ExplicitWatch? watch)
        {
            watch = null;

            for (int i = 0; i < Classes.Count; i++)
            {
                for (int j = 0; j < Classes[i].WatchFormat.Length; j++)
                {
                    if (Classes[i].WatchFormat[j].IsFound(parts[index].ToString(), Classes.ToArray(), Containers.ToArray()))
                    {
                        watch = Classes[i].WatchFormat[j];
                        return true;
                    }
                } 
            }

            return false;
        }
        internal bool ParamIsFound(object[] parts, int index, out ExplicitParams? param)
        {
            param = null;

            for (int i = 0; i < Classes.Count; i++)
            {
                if (Classes[i].Params == null) continue;
                if (Classes[i].Params.IsFound(parts[index].ToString(), Classes.ToArray(), Containers.ToArray()))
                {
                    param = Classes[i].Params;
                    return true;
                }
            }

            return false;
        }
        private Var[] GetVarsFromParameter(Token[] tokens, Line line)
        {
            line.CodeLine += 1;
            if (tokens.Length > 1 && tokens.Select(x => x.Type).Contains(TokenType.Colon))
            {
                tokens = tokens.Skip(2).TakeWhile(x => x.Type != TokenType.Arrow).ToArray();
                string[] all = string.Join("", tokens.Select(x=>x.Value.ToString())).Split(",");
                Var[] vars = [];
                for (int i = 0; i < all.Length ; i++)
                {
                    DataType? type = null;
                    string name = all[i];
                    string[] sides = all[i].Split(":");
                    if(sides.Length > 1)
                    {
                        type = DataType.GetType(sides[0], Classes.ToArray(), Containers.ToArray());
                        name = sides[1];
                    }
                    vars = [.. vars, new Var(name, null, line, type)];
                }
                return vars;
            }
            else
            {
                return [];
            }
        }
        private Statement SetStatement(ref Line[] lines, int lineIndex, int partIndex) => SetStatement(ref lines, lineIndex, partIndex, out List<Line> removes);
        private Statement SetStatement(ref Line[] lines, int lineIndex, int partIndex, out List<Line> removes)
        {
            removes = [];
            string line = lines[lineIndex].Value;
            object[] parts = SplitWithDelimiters(line, Delimeters).Where(x => x != "" && x != " ").Select(x => (object)x).ToArray();
            string[] partsSpaces = line.Split(" ").Where(x => x != "" && x != " ").ToArray();
            LineWithTokens[] lineWithTokens = [];
            Argument? argument = null;
            bool sameLine = false, brackets = false;
            string val = string.Join(" ", partsSpaces.Skip(1).TakeWhile(x => x != ":" && x != "{"));
            Token[] argTokens = [];
            for (int j = 1; j < parts.Length; j++)
            {
                if (parts[j].ToString() == ":")
                {
                    sameLine = true;
                    break;
                }
                if (parts[j].ToString() == "{")
                {
                    brackets = true;
                    break;
                }

                argTokens = argTokens.Append(SingleToken(parts, j)).ToArray();
            }
            if (Statement.ConditionalTypes.Contains(parts[partIndex]))
            {
                argument = new Argument(argTokens, lines[lineIndex], val);
            }
            if (sameLine)
            {
                string v = string.Join(" ", partsSpaces.SkipWhile(x => x != ":").Skip(1));
                LineWithTokens inLineTokens = TokenArray(v)[0];
                string code = inLineTokens.Line.Value;
                Line endline = new(code, lines[lineIndex].CodeLine);
                lineWithTokens = [new LineWithTokens(inLineTokens.Tokens, endline)];
            }
            else
            {
                Line nextLine = lines[lineIndex + 1];
                bool sameLineBracket = nextLine.Value.StartsWith('{');
                if (!brackets && !sameLineBracket)
                {
                    LineWithTokens nextLineTokens = TokenArray(nextLine.Value)[0];
                    nextLineTokens.Line.CodeLine = lines[lineIndex].CodeLine + 1;
                    string code = nextLineTokens.Line.Value;
                    lineWithTokens = [new(nextLineTokens.Tokens, nextLine)];
                    List<Line> l = [.. lines];
                    l.Remove(nextLine);
                    removes.Add(nextLine);
                    lines = [.. l];
                }
                else
                {
                    List<Line> l = [.. lines];
                    int curleyBrackets = sameLineBracket ? 0 : 1;
                    string code = "";
                    for (int i = lineIndex + 1; i < lines.Length; i++)
                    {
                        Line bracketLine = lines[i];
                        LineWithTokens bracketLineTokens = TokenArray(bracketLine.Value)[0];
                        bracketLineTokens.Line.CodeLine = lines[lineIndex].CodeLine + i;
                        if (bracketLine.Value.Contains('{'))
                            curleyBrackets++;
                        if (bracketLine.Value.Contains('}'))
                            curleyBrackets--;
                        removes.Add(bracketLine);
                        l.Remove(bracketLine);
                        if (curleyBrackets == 0)
                            break;
                        code += bracketLine.Value + Environment.NewLine;
                    }
                    lineWithTokens = Tokenize(code);
                    if (l.Last().Value.ToString() == "}")
                    {
                        removes.Add(l[l.Count - 1]);
                        l.RemoveAt(l.Count - 1);
                        lineWithTokens = lineWithTokens.Where((x, y) => y != l.Count - 1).ToArray();
                    }
                    if (lineWithTokens[0].Line.Value == "{") lineWithTokens = lineWithTokens.Where((x, y) => y != 0).ToArray();
                    lines = [.. l];
                }
            }
            for (int i = 0; i < lineWithTokens.Length; i++)
                lineWithTokens[i].Line.CodeLine += 1;
            return new Statement(parts[partIndex].ToString(), lines[lineIndex], lineWithTokens, argument);
        }
        private Method SetMethod(Line[] lines, int index) => SetMethod(ref lines, index);
        private Method SetMethod(ref Line[] lines, int index)
        {
            Line line = lines[index];
            line.CodeLine += 1;

            Method.MethodSettings settings =
                (line.Value.Contains("static ") ? Method.MethodSettings.Static : Method.MethodSettings.None) |
                (line.Value.Contains("nocol ") ? Method.MethodSettings.NoCol : Method.MethodSettings.None);
            DataType? returns = null;
            Var[]? param = [];
            Token[] fistLineTokens = TokenArray(string.Join(" ", line.Value.Split(" ").SkipWhile(x => x != "method").Skip(1)))[0].Tokens;
            string name = fistLineTokens[0].Value.ToString()!;
            bool ret = false, req = true;
            for (int i = 1; i < fistLineTokens.Length; i++)
            {
                Token token = fistLineTokens[i];
                if (ret)
                {
                    if(token.Type == TokenType.DataType)
                    {
                        returns = DataType.GetType(token.Value.ToString()!, Classes.ToArray(), Containers.ToArray());
                        break;
                    }
                }
                if (token.Type == TokenType.Arrow)
                {
                    ret = true;
                    continue;
                }

                if (token.Type == TokenType.QuestionMark)
                {
                    req = false;
                    continue;
                }

                if ((token.Type == TokenType.Comma || i == 1) && token.Type != TokenType.Arrow && token.Type != TokenType.OpenCurlyBracket)
                {
                    bool pTypeDef = fistLineTokens[i + 1].Type == TokenType.DataType;
                    string pName = "";
                    DataType pType = DataType.UnSet;
                    if(pTypeDef)
                    {
                        pType = DataType.GetType(fistLineTokens[i + 1].Value.ToString()!, Classes.ToArray(), Containers.ToArray());
                        if (fistLineTokens[i + 2].Type == TokenType.Colon)
                        {
                            pName = fistLineTokens[i + 3].Value.ToString()!;
                        }
                    }
                    else
                    {
                        pName = fistLineTokens[i + 1].Value.ToString()!;
                    }
                    param = param.Append(new Var(pName, null, line, pType, req)).ToArray();
                }
            }

            LineWithTokens[] lineWithTokens = [];
            Line nextLine = lines[index + 1];
            bool sameLineBracket = nextLine.Value.StartsWith('{');
            List<Line> l = [.. lines];
            int curleyBrackets = sameLineBracket ? 0 : 1;
            for (int i = index + 1; i < lines.Length; i++)
            {
                Line bracketLine = lines[i];
                Token[] bracketLineTokens = TokenArray(bracketLine.Value)[0].Tokens;
                try
                {
                    if (Statement.Types.Contains(bracketLineTokens[0].Value))
                    {
                        lines = l.ToArray();
                        Statement statement = SetStatement(ref lines, Array.IndexOf(lines, bracketLine), 0, out List<Line> removes);
                        for (int j = 0; j < removes.Count; j++) l.Remove(removes[j]);
                        bracketLineTokens = [SingleToken([statement], 0)];
                    }
                } catch { }
                if (bracketLine.Value.Contains('{'))
                    curleyBrackets++;
                if (bracketLine.Value.Contains('}'))
                    curleyBrackets--;
                lineWithTokens = [.. lineWithTokens, new LineWithTokens(bracketLineTokens, bracketLine)];
                l.Remove(bracketLine);
                if (curleyBrackets == 0)
                    break;
            }
            if (l.Last().Value.ToString() == "}")
            {
                l.RemoveAt(l.Count - 1);
                lineWithTokens = lineWithTokens.Where((x, y) => y != l.Count - 1).ToArray();
            }
            if (lineWithTokens[0].Line.Value == "{") lineWithTokens = lineWithTokens.Where((x, y) => y != 0).ToArray();
            lines = [.. l];

            for (int i = 0; i < lineWithTokens.Length; i++)
                lineWithTokens[i].Line.CodeLine += 1;
            return new Method(name, line, settings, lineWithTokens, param, returns);
        }
        private GetValueMethod SetGetVal(Line[] lines, int index)
        {
            Line line = new Line(string.Join(" ", lines[index].Value.Split(" ").Prepend("name").Prepend("method")), index);
            line.CodeLine += 1;
            Line[] liness = lines.Select((x, y) => y == index ? line : x).ToArray();
            Method method = SetMethod(liness, index);
            method.Line = lines[index];
            method.Name = null;
            GetValueMethod getValue = new GetValueMethod(method.Returns, method);
            return getValue;
        }
        private Var? SetVar(Line line, Token[] tokens)
        {
            line.CodeLine += 1;
            Var? var = null;
            if (tokens[0].Type == TokenType.Identifier)
            {
                if (tokens[1].Type == TokenType.Identifier)
                {
                    if (tokens[2].Type == TokenType.New)
                    {
                        if (tokens.Length > 3)
                        {
                            if (tokens[3].Type == TokenType.Colon)
                            {
                                var = new(tokens[1].Value.ToString(), string.Join(" ", tokens.Skip(4).Select(x => x.Value)), line);
                            }
                        }
                        else
                        {
                            var = new(tokens[1].Value.ToString(), tokens[2], line);
                        }
                    }
                }
            }
            if (tokens[0].Type == TokenType.Undefined)
            {
                if (tokens[1].Type == TokenType.Identifier)
                {
                    var = new(tokens[1].Value.ToString(), null, line);
                }
            }
            return var;
        }
        private static string[] SplitWithDelimiters(string input, char[] delimiters)
        {
            string pattern = $"({string.Join("|", delimiters.Select(c => Regex.Escape(c.ToString())))})";
            return Regex.Split(input, pattern);
        }
        private static Line[] SplitLine(string code)
        {
            Line[] lines = Array.Empty<Line>();
            int i = 0;
            foreach (var item in code.Split(Environment.NewLine).Select(s => s.Trim()).ToArray())
            {
                lines = lines.Append(new Line(item, i)).ToArray();
                i++;
            };
            return lines;
        }
    }
}