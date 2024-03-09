﻿using System.Linq.Expressions;
using System.Reflection;
using static EZCodeLanguage.Tokenizer;

namespace EZCodeLanguage
{
    public class Interpreter
    {
        public Tokenizer tokenizer { get; set; }
        public string WorkingFile { get; set; }
        public EZHelp EZHelp { get; private set; }
        public Interpreter(string file, Tokenizer tokenizer)
        {
            StackTrace = new Stack<string>();
            WorkingFile = file;
            this.tokenizer = tokenizer;
            EZHelp = new EZHelp(this);
            Methods = tokenizer.Methods.ToArray();

            for (int i = 0; i < Classes.Length; i++)
            {
                if (Classes[i].Properties == null) continue;
                for (int j = 0; j < Classes[i].Properties.Length; j++)
                {
                    Var var = Classes[i].Properties[j];
                    if (var.DataType.ObjectClass == null)
                    {
                        var.DataType = DataType.GetType(var.Line.Value.Split(" ")[0], Classes, Containers);
                        if (var.DataType.ObjectClass == null && var.DataType.ObjectContainer == null)
                        {
                            var.DataType.ObjectClass = Classes[i];
                        }
                    }
                }
            }
        }
        private bool LastIfWasTrue = true;
        Exception? LastTryNotFailed = null;
        private bool yielded = false;
        public Stack<string> StackTrace { get; private set; }
        public string[] Output { get; internal set; } = [];
        public Exception[] Errors { get; private set; } = [];
        public Var[] Vars { get; set; } = [];
        public Method[] Methods { get; set; } = [];
        public Class[] Classes { get => tokenizer.Classes.ToArray(); }
        public Container[] Containers { get => tokenizer.Containers.ToArray(); }
        public Line CurrentLine { get; private set; }
        public int Interperate() => Interperate(tokenizer.Tokens);
        public int Interperate(LineWithTokens[] LineTokens)
        {
            int endcode = 0;
            var temp_stack = new Stack<string>(StackTrace);

            foreach (LineWithTokens line in LineTokens)
            {
                try
                {
                    temp_stack = new Stack<string>(StackTrace);
                    SingleLine(line);
                }
                catch (Exception ex)
                {
                    string message = ex.Message + ", StackTrace: \n\t" + string.Join("\n\t", StackTrace.Reverse());
                    Console.WriteLine(message);
                    Errors = Errors.Append(ex).ToArray();
                    StackTrace = temp_stack;
                }
            }

            Console.WriteLine();
            Console.WriteLine(WorkingFile + ": Exited with code " + endcode + ".");

            return endcode;
        }
        internal int StackNumber = 0;
        private object? SingleLine(LineWithTokens line)
        {
            StackNumber++;
            try
            {
                string message = $"codeline: \"{line.Line.Value}\", file: \"{WorkingFile}\", line: {line.Line.CodeLine}";
                try { if (StackTrace.First() != message) StackTrace.Push(message); }
                catch { StackTrace.Push(message); }
                CurrentLine = line.Line;
                string deb = "";
                Token FirstToken = line.Tokens.FirstOrDefault(new Token(TokenType.None, "", ""));
                object? Return = null;
                switch (FirstToken.Type)
                {
                    case TokenType.Identifier:
                        try
                        {
                            switch (IsType(FirstToken.Value.ToString()!, out object? type))
                            {
                                case IdentType.Var:
                                    Var var = type as Var;
                                    if (line.Tokens.Length > 1)
                                    {
                                        if (line.Tokens[1].Type == TokenType.Arrow)
                                        {
                                            var tok = line.Tokens.Skip(2).ToArray();
                                            var.Value = SingleLine(new LineWithTokens(tok, line.Line));
                                            Return = var.Value;
                                        }
                                        else
                                        {
                                            if (var.Value is Class || (var.DataType.ObjectClass != null && var.DataType.Type != DataType.Types._null))
                                            {
                                                Class c = var.Value is Class ? var.Value as Class : var.DataType.ObjectClass;
                                                if (c.Methods.Any(x => x.Name == line.Tokens[1].Value.ToString()))
                                                {
                                                    for (int i = 2; i < line.Tokens.Length; i++)
                                                    {
                                                        if (line.Tokens[i].Type == TokenType.Variable)
                                                            line.Tokens[i].Value = GetValue(line.Tokens[i].Value);
                                                    }
                                                    Method[] backupMethods = Methods;
                                                    Methods = c.Methods;
                                                    Var[] backupVars = Vars;
                                                    Vars = c.Properties;
                                                    line.Tokens = line.Tokens.Skip(1).ToArray();
                                                    object value = SingleLine(line);
                                                    Methods = backupMethods;
                                                    Vars = backupVars;
                                                    Return = value;
                                                }
                                                else
                                                {
                                                    throw new Exception($"Variable is trying to call a method \"{line.Tokens[1].Value}\" that doesn't exists");
                                                }
                                            }
                                            else
                                            {
                                                throw new Exception($"Variable type is undefined and can not call the method \"{line.Tokens[1].Value}\"");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Return = var.Value;
                                    }
                                    break;

                                case IdentType.Class:
                                    Class cl = new Class(type as Class);
                                    if (line.Tokens[2].Type == TokenType.New)
                                    {
                                        string name = line.Tokens[1].Value.ToString();
                                        if (Vars.Any(x => x.Name == name))
                                        {
                                            var = Vars.FirstOrDefault(x=>x.Name == name);
                                            if (var.Value is Class)
                                            {
                                                if ((var.Value as Class).Name != cl.Name)
                                                    throw new Exception("Can not create a class instance with the same name as another class instance of another class");
                                                else cl = (Class)var.Value;
                                            }
                                            else throw new Exception("Can not create a class instance with the same name as another class instance of another class");
                                        }
                                        else if (Methods.Any(x => x.Name == name) || Classes.Any(x => x.Name == name))
                                            throw new Exception("Can not create a new class instance with the same name as a method or class");
                                        else if (name == "exeption")
                                            throw new Exception("The name \"exception\" is reserved for the try fail statement error variable");
                                        else
                                            var = new Var(name, cl, line.Line, stackNumber: StackNumber, type: new DataType(cl.TypeOf != null ? cl.TypeOf.Type : DataType.Types._object, cl));
                                        Vars = [.. Vars, var];

                                        if (line.Tokens.Length > 3)
                                        {
                                            if (line.Tokens[3].Type != TokenType.Colon)
                                                throw new Exception("Expected colon to set instance properties");
                                            ExplicitParams? p = new ExplicitParams(cl.Params.Pattern, cl.Params.Runs, cl.Params.Vars, cl.Params.IsOverride, cl.Params.All);
                                            if (p != null)
                                            {
                                                if (tokenizer.ParamIsFound(line.Tokens.Select(x => x.Value).ToArray(), 4, out _))
                                                {
                                                    if (p.IsOverride)
                                                    {
                                                        RunMethod run = p.Runs;
                                                        Method[] backupMethods = Methods;
                                                        Var[] backupVars = Vars;
                                                        Vars = cl.Properties;
                                                        Methods = cl.Methods;
                                                        MethodRun(run.Runs, run.Parameters);
                                                        Methods = backupMethods;
                                                        Vars = backupVars;
                                                    }
                                                    else if (p.All)
                                                    {
                                                        RunMethod run = new RunMethod(p.Runs.Runs, p.Runs.Parameters, p.Runs.ClassName, p.Runs.Tokens);
                                                        Method[] backupMethods = Methods;
                                                        Var[] backupVars = Vars, backupParams = p.Runs.Parameters.Select(x => new Var(x.Name, x.Value, x.Line, x.StackNumber, x.DataType, x.Required)).ToArray();
                                                        Vars = cl.Properties;
                                                        Methods = cl.Methods;
                                                        Var v = run.Parameters.FirstOrDefault(x => x.Name == "PARAMS");
                                                        if (v is not null)
                                                        {
                                                            if (v.Value is string) v.Value = string.Join(" ", line.Tokens.Select(x => x.Value).Skip(4));
                                                            else
                                                            {
                                                                v.Value = GetValue(new Token(TokenType.Identifier, line.Tokens.Skip(4).ToArray(), string.Join(" ", line.Tokens.Select(x => x.StringValue))));
                                                                v.DataType = DataType.TypeFromValue(v.Value.ToString(), Classes, Containers);
                                                            }
                                                        }
                                                        MethodRun(run.Runs, run.Parameters);
                                                        p.Runs.Parameters = backupParams; 
                                                        Methods = backupMethods;
                                                        Vars = backupVars;
                                                    }
                                                    /*else if (line.Tokens[4].Type == TokenType.Match)
                                                    {
                                                        RunMethod run = line.Tokens[4].Value as RunMethod;
                                                        Method[] backupMethods = Methods;
                                                        Var[] backupVars = Vars;

                                                        int i = 0;
                                                        while (run.ClassName != FirstToken.Value.ToString())
                                                        {
                                                            i++; if (i > 50) throw new Exception($"Infinite loop error in with declaring instance of class \"{cl.Name}\". Unkown cause, might be Interperater error or faulty code");
                                                            Class cc = Classes.FirstOrDefault(x => x.Name == run.ClassName);
                                                            if (cc == null) throw new Exception("Error with finding class from name");
                                                            Vars = cc.Properties;
                                                            Methods = cc.Methods;
                                                            object result = GetValue(run);
                                                            Methods = backupMethods;
                                                            Vars = backupVars;
                                                            bool f = tokenizer.ParamIsFound([result is Var v ? v.Value : result], 0, out var param);
                                                            if (f)
                                                            {
                                                                run = p.Runs;
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                throw new Exception($"The format pattern of the class, \"{p.Pattern}\" does not match this class instance");
                                                            }
                                                        }
                                                        Vars = cl.Properties;
                                                        Methods = cl.Methods;
                                                        MethodRun(run.Runs, run.Parameters);
                                                        Methods = backupMethods;
                                                        Vars = backupVars;
                                                    }*/
                                                    else
                                                    {

                                                    }
                                                }
                                                else
                                                {
                                                    throw new Exception($"The format pattern of the class, \"{p.Pattern}\" does not match this class instance");
                                                }
                                            }
                                            else
                                            {
                                                string all = string.Join(" ", line.Tokens.Skip(4).Select(x => x.Value));
                                                string[] vals = all.Split(',').Select(x => x.Trim()).ToArray();
                                                Var[] prop = [];
                                                if (vals.Length > 0)
                                                {
                                                    for (int i = 0; i < vals.Length; i++)
                                                    {
                                                        string[] whole = vals[i].Split(":");
                                                        if (whole.Length < 2) throw new Exception("Expected properties to be set by syntax \"PropertyName:Value\"");
                                                        string before = whole[0].Trim();
                                                        string after = whole[1].Trim();
                                                        if (cl.Properties.Any(x => x.Name == before))
                                                        {
                                                            Var v = cl.Properties.FirstOrDefault(x => x.Name == before);
                                                            after = GetValue(after).ToString();
                                                            prop = [.. prop, new Var(v.Name, after, line.Line, stackNumber: StackNumber, type:v.DataType)];
                                                        }
                                                        else
                                                        {
                                                            throw new Exception($"There is no \"{before}\" property in the \"{cl.Name}\" class");
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }
                                    else
                                    {
                                        throw new Exception("Expected \"new\" keyword to declare instance of class");
                                    }
                                    break;

                                case IdentType.Method:
                                    Method method = type as Method;

                                    Method.MethodSettings settings = method.Settings;
                                    bool nocol = (settings & Method.MethodSettings.NoCol) != 0;
                                    int next = nocol ? 0 : 1;
                                    if (!nocol && line.Tokens[1].Type != TokenType.Colon)
                                    {
                                        throw new Exception("Expected \":\" identifier to set method perameters");
                                    }
                                    Var[] vars = [];

                                    if (method.Params != null && method.Params.Length > 0)
                                    {
                                        string all = string.Join(" ", line.Tokens.Skip(next + 1).Select(x => x.StringValue));
                                        string[] vals = all.Split(',').Select(x => x.Trim()).Select(x => GetValue(x).ToString()).ToArray();
                                        if (vals.Length > 0)
                                        {
                                            for (int i = 0; i < method.Params.Length; i++)
                                            {
                                                vars = [.. vars, new Var(method.Params[i].Name, vals[i], line.Line, stackNumber: StackNumber, type:method.Params[i].DataType)];
                                            }
                                            if (vars.Length != method.Params.Select(x => x.Required).ToArray().Length)
                                            {
                                                throw new Exception("Not all parameters are set");
                                            }
                                        }
                                        else
                                        {
                                            if (line.Tokens.Length > 1)
                                            {
                                                throw new Exception("Method does not require any parameters");
                                            }
                                        }
                                    }

                                    Return = MethodRun(method, vars);
                                    break;

                                default:
                                case IdentType.Other:
                                    throw new Exception("Unexpected identifier \"" + FirstToken.Value.ToString() + "\". Expects methods, object delcaration, or existing variables");
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"{line.Line.Value}\", Error Message:\"{ex.Message}\"");
                        }
                        break;
                    case TokenType.RunExec:
                        try
                        {
                            CSharpMethod method = FirstToken.Value as CSharpMethod;
                            StackTrace.Push($"csharp-method: \"{(method.Path != "" ? method.Path : "Null")}\", file: \"{WorkingFile}\", line: {line.Line.CodeLine}");
                            object obj = Reflect(method!);
                            StackTrace.Pop();
                            Return = obj;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"runexec\" and with C# method. The reason may be because the method may not exist. Error Message:\"{ex.Message}\"");
                        }
                        break;
                    case TokenType.Undefined:
                        try
                        {
                            Token tokenName = line.Tokens[1];
                            object? value = null;
                            if (line.Tokens.Length > 2)
                            {
                                if (line.Tokens[2].Type == TokenType.Arrow)
                                {
                                    line.Tokens = line.Tokens.Skip(3).ToArray();
                                    value = SingleLine(line);
                                    if (value == null)
                                    {
                                        throw new Exception("Expected method that returns value");
                                    }
                                }
                                else
                                {
                                    throw new Exception("Expected arrow identifier \"=>\" to set value");
                                }
                            }
                            Vars = [.. Vars, new Var(tokenName.Value.ToString(), value, line.Line, stackNumber: StackNumber, type:null)];
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"undefined\", Error Message:\"{ex.Message}\"");
                        }
                        break;
                    case TokenType.Return:
                        try
                        {
                            line.Tokens = line.Tokens.Skip(1).ToArray();
                            Return = SingleLine(line);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"return\", Error Message:\"{ex.Message}\"");
                        }
                        break;
                    case TokenType.If:
                        try
                        {
                            Statement? statement = FirstToken.Value as Statement;
                            Argument[]? arguments = statement!.Argument!.Args() ?? throw new Exception($"Problem with argument \"{statement.Argument.Value}\"");
                            string[] parts = [];
                            for (int i = 0; i < arguments.Length; i++)
                            {
                                Argument arg = arguments[i];
                                bool? istrue = ArgumentIsTrue(arg);
                                parts = [.. parts, istrue.ToString()];
                                if (arg.ArgAdd != Argument.ArgAdds.None) parts = [.. parts, arg.ArgAdd.ToString()];
                            }
                            bool run = EZHelp.Evaluate(string.Join(" ", parts));
                            if (run)
                            {
                                LastIfWasTrue = true;
                                RunStatement(statement, out _);
                            }
                            else
                            {
                                LastIfWasTrue = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"if\", Error Message:\"{ex.Message}\"");
                        }
                        break;
                    case TokenType.Elif:
                        try
                        {
                            if (LastIfWasTrue) break;
                            Statement? statement = FirstToken.Value as Statement;
                            Argument[]? arguments = statement!.Argument!.Args() ?? throw new Exception($"Problem with argument \"{statement.Argument.Value}\"");
                            string[] parts = [];
                            for (int i = 0; i < arguments.Length; i++)
                            {
                                Argument arg = arguments[i];
                                bool? istrue = ArgumentIsTrue(arg);
                                parts = [.. parts, istrue.ToString()];
                                if (arg.ArgAdd != Argument.ArgAdds.None) parts = [.. parts, arg.ArgAdd.ToString()];
                            }
                            bool run = EZHelp.Evaluate(string.Join(" ", parts));
                            if (run)
                            {
                                LastIfWasTrue = true;
                                RunStatement(statement, out _);
                            }
                            else
                            {
                                LastIfWasTrue = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"elif\", Error Message:\"{ex.Message}\"");
                        }
                        break;
                    case TokenType.Else:
                        try
                        {
                            if (LastIfWasTrue) break;
                            Statement? statement = FirstToken.Value as Statement;
                            LastIfWasTrue = true;
                            RunStatement(statement, out _);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"else\", Error Message:\"{ex.Message}\"");
                        }
                        break;
                    case TokenType.Loop:
                        try
                        {
                            Statement? statement = FirstToken.Value as Statement;
                            Argument[]? arguments = statement!.Argument!.Args() ?? throw new Exception($"Problem with argument \"{statement.Argument.Value}\"");
                            if (statement.Argument.Tokens.Length == 1 && float.TryParse(statement.Argument.Tokens[0].Value.ToString(), out float floatParse))
                            {
                                if (int.TryParse(floatParse.ToString(), out int parse))
                                {
                                    for (int i = 0; i < parse; i++)
                                    {
                                        RunStatement(statement, out bool broke);
                                        if (broke) break;
                                    }
                                }
                                else throw new Exception("Expected integer for loop count");
                            }
                            else
                            {
                                string[] parts = [];
                                for (int i = 0; i < arguments.Length; i++)
                                {
                                    Argument arg = arguments[i];
                                    bool? istrue = ArgumentIsTrue(arg);
                                    parts = [.. parts, istrue.ToString()];
                                    if (arg.ArgAdd != Argument.ArgAdds.None) parts = [.. parts, arg.ArgAdd.ToString()];
                                }
                                bool run = EZHelp.Evaluate(string.Join(" ", parts));
                                while (run)
                                {
                                    RunStatement(statement, out bool broke);
                                    if(broke) break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"loop\", Error Message:\"{ex.Message}\"");
                        }
                        break;

                    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case TokenType.Try:
                        try
                        {
                            Statement? statement = FirstToken.Value as Statement;
                            LastTryNotFailed = null;
                            RunStatement(statement, out _);
                        }
                        catch (Exception ex)
                        {
                            LastTryNotFailed = ex;
                        }
                        break;
                    case TokenType.Fail:
                        try
                        {
                            if (LastTryNotFailed == null) break;
                            Var exception = new Var("exception", LastTryNotFailed.Message, CurrentLine, stackNumber: StackNumber, type:DataType.GetType("str", Classes, Containers));
                            Vars = [.. Vars, exception];
                            Statement? statement = FirstToken.Value as Statement;
                            LastTryNotFailed = null;
                            RunStatement(statement, out _);
                            Vars = Vars.Where(x => x.Name != "exception").ToArray();
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error with \"fail\", Error Message:\"{ex.Message}\"");
                        }
                        break;
                    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                }
                StackTrace.TryPop(out _);
                return Return;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }
        private object? RunStatement(Statement statement, out bool broke)
        {
            broke = false;
            object? result = null;
            foreach (LineWithTokens line in statement.InBrackets)
            {
                result = SingleLine(new LineWithTokens(line));  

                if (yielded)
                {
                    yielded = false;
                    broke = true;
                    break;
                }
                if (line.Tokens[0].Type == TokenType.Yield)
                {
                    yielded = true;
                    if (line.Tokens.Length > 1)
                    {
                        if (line.Tokens[1].Type == TokenType.Break)
                        {
                            broke = true;
                            break;
                        }
                        else throw new Exception("Expected the \"break\" keyword after yield");
                    }
                    else
                    {
                        throw new Exception("Expected token after yield. yield is a modifier and can not be by itself");
                    }
                }
                if (line.Tokens[0].Type == TokenType.Break)
                {
                    broke = true;
                    break;
                }
            }
            StackTrace.TryPop(out _);
            return result;
        }
        private bool? ArgumentIsTrue(Argument argument)
        {
            LineWithTokens line = new LineWithTokens(argument.Tokens, argument.Line);
            object? result = null;
            try { result = SingleLine(line); } catch { }
            if (result == null) result = argument.Value;
            bool? term = Argument.EvaluateTerm(result.ToString());
            if (term == null) throw new Exception($"Expected the argument section's method \"{argument.Value}\" to return boolean");
            StackTrace.TryPop(out _);
            return term;
        }
        private enum IdentType { Var, Class, Method, Other }
        private IdentType IsType(string token, out object? type)
        {
            if (Vars.Any(x => x.Name == token))
            {
                type = Vars.FirstOrDefault(x => x.Name == token)!;
                return IdentType.Var;
            }
            if (Methods.Any(x => x.Name == token))
            {
                type = Methods.FirstOrDefault(x => x.Name == token)!;
                return IdentType.Method;
            }
            if (Classes.Any(x => x.Name == token))
            {
                type = Classes.FirstOrDefault(x => x.Name == token)!;
                return IdentType.Class;
            }
            else if (Classes.Any(x => x.Aliases.Any(y => y == token)))
            {
                type = Classes.FirstOrDefault(x => x.Aliases.Any(y => y == token));
                return IdentType.Class;
            }
            type = null;
            return IdentType.Other;
        }
        public object? MethodRun(Method method, Var[]? parameters)
        {
            StackTrace.Push($"method: {method.Name}, file: {WorkingFile}, line: {method.Line.CodeLine}");
            parameters ??= [];
            LineWithTokens[] lines = method.Lines;
            Var[] vvars = method.Params ?? [];

            for (int i = 0; i < vvars.Length; i++)
            {
                if (vvars[i].DataType.Type != parameters[i].DataType.Type && vvars[i].DataType != DataType.UnSet)
                {
                    throw new Exception($"Method parameters do not match. Error in method: \"{method.Name}\" parameter: \"{vvars[i].Name}\" inputted value: \"{parameters[i].Value}\"");
                }
                parameters[i].Name = vvars[i].Name;
            }
            Var[] overlap = [];
            Var? v;
            do
            {
                v = Vars.FirstOrDefault(x => parameters.Any(y => y.Name == x.Name), null);
                if (v != null)
                {
                    overlap = [.. overlap, v];
                    Vars = Vars.Where(x => x != v).ToArray();
                }
            }
            while (v != null);
            Vars = [.. Vars, .. parameters];
            object? result = null;
            foreach (LineWithTokens line in lines)
            {
                result = SingleLine(new LineWithTokens(line));

                if (line.Tokens[0].Type == TokenType.Return)
                    break;
            }
            Vars = [.. Vars, .. overlap];
            Vars = Vars.Except(parameters).ToArray();

            StackTrace.TryPop(out _);
            return result;
        }
        public object GetValue(object obj, DataType? type = null)
        {
            if (obj.GetType().IsArray)
            {
                object[] a = (object[])obj;
                string all = "";
                for (int i = 0; i < a.Length; i++)
                {
                    all += GetValue(a[i], type) + " ";
                }
                return all.Trim();
            }
            else if (obj is Class)
            {
                if (type == null)
                    return GetValue((obj as Class).Name, type);
                Var var = new Var("Intermediate Var for getting value", obj as Class, CurrentLine, stackNumber: StackNumber, type:type);
                return GetValue(var, type);
            }
            else if (obj is string || obj is Var)
            {
                Var? var;
                if (obj is Var)  var = obj as Var;
                else var = Vars.FirstOrDefault(x => x.Name == obj.ToString());
                if (var != null)
                {
                    if (type != null && var.DataType != null)
                    {
                        if (var.DataType.Type == DataType.Types._null || var.DataType.ObjectClass == null)
                        {
                            return var.Value ?? "";
                        }
                        else if (var.DataType.ObjectClass == null && (var.DataType.Type == DataType.Types._null || var.DataType.Type == DataType.Types._object))
                        {
                            throw new Exception($"Error with DataType of class instance. variable \"{var.Name}\" has an explicit data type that is not connected to a class");
                        }
                        else if (var.DataType.ObjectClass != null && var.DataType.Type == DataType.Types._object)
                        {
                            GetValueMethod[] get = var.DataType.ObjectClass.GetTypes;
                            if (get != null && get.Length != 0)
                            {
                                int t = -1;
                                for (int i = 0; i < get.Length; i++)
                                {
                                    if (get[i].DataType == type) t = i;
                                }
                                if (t != -1)
                                {
                                    object? result = MethodRun(get[t].Method, null);
                                    if (result != null)
                                        return result;
                                    else throw new Exception("The \"get\" method for this class instance does not return a value");
                                }
                                else
                                {
                                    if (var.DataType == type)
                                    {
                                        return var.Value;
                                    }
                                    else
                                    {
                                        throw new Exception($"The Class of the instance does not contain a \"get\" method for the expected datatype \"{type.Type.ToString().Remove(0, 1)}\"");
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception($"The Class of the instance does not contain a \"get\" method for the expected datatype \"{type.Type.ToString().Remove(0, 1)}\"");
                            }
                        }
                        else if (var.DataType.ObjectClass != null && !(var.DataType.Type == DataType.Types._null || var.DataType.Type == DataType.Types._object))
                        {
                            obj = var.Value;
                            if (obj is Class)
                            {
                                obj = (var.Value as Class).Properties.FirstOrDefault(x => x.Name == "Value", (var.Value as Class).Properties.FirstOrDefault(x => x.Name == "value", null)).Value ?? obj;
                            }
                            switch (var.DataType.Type)
                            {
                                case DataType.Types._string: return obj.ToString();
                                case DataType.Types._int: return int.Parse(obj.ToString());
                                case DataType.Types._float: return float.Parse(obj.ToString());
                                case DataType.Types._bool: return bool.Parse(obj.ToString());
                                case DataType.Types._char: return char.Parse(obj.ToString());
                                case DataType.Types._double: return double.Parse(obj.ToString());
                                case DataType.Types._decimal: return decimal.Parse(obj.ToString());
                                case DataType.Types._long: return long.Parse(obj.ToString());
                                case DataType.Types._uint: return uint.Parse(obj.ToString());
                                case DataType.Types._ulong: return ulong.Parse(obj.ToString());
                                default: throw new Exception("Error finding datatype of class instance. This may be an interpreter error or the code for the class is faulty");
                            }
                        }
                        else
                        {
                            throw new Exception("Error finding datatype of class instance. This may be an interpreter error or the code for the class is faulty");
                        }
                    }
                    else
                    {
                        return GetValue(var.Value, var.DataType) ?? obj;
                    }
                }
            }
            else if (obj is RunMethod run)
            {
                if (run.ClassName != "" && run.ClassName != null)
                {
                    Class cl = Classes.FirstOrDefault(x => x.Name == run.ClassName);
                    Method[] backupMethods = Methods;
                    Var[] backupVars = Vars;

                    Vars = cl.Properties;
                    Methods = cl.Methods;
                    object o = MethodRun(run.Runs, run.Parameters);
                    Methods = backupMethods;
                    Vars = backupVars;
                    return o;
                }
                else
                {
                    return MethodRun(run.Runs, run.Parameters);
                }
            }
            else if (obj is Token t)
            {
                return GetValue(t.Value);
            }
            return obj;
        }
        public object Reflect(CSharpMethod method)
        {
            if (method.IsVar)
            {
                string value = Vars.FirstOrDefault(x => x.Name == method.Path).Value.ToString();
                Line[] l = [new Line(value, 0)];
                object[] o = tokenizer.SplitParts(ref l, 0, 0, out _, out _);
                if (o.Length > 1) throw new Exception("Error with reflection properties");
                method = o[0] as CSharpMethod;
            }

            return InvokeMethod(method.Path, method.Params.Select(x => x).ToArray(), EZHelp);
        }
        public static object? InvokeMethod(string methodPath, object[] parameters, EZHelp e)
        {
            // Split the method path into type and method name
            string[] pathParts = methodPath.Split('.');
            if (pathParts.Length < 2)
            {
                throw new ArgumentException("Invalid method path");
            }

            // Get the type from the full type name
            string typeName = string.Join(".", pathParts.Take(pathParts.Length - 1));
            // Include the assembly information for types in the System namespace
            if (typeName.StartsWith("System."))
            {
                typeName += ", mscorlib";
            }
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                throw new ArgumentException($"Type \"{typeName}\" not found");
            }

            // Get the method name from the path
            string methodName = pathParts.Last();

            // Find the method in the type
            MethodInfo methodInfo = type.GetMethod(methodName, parameters.Select(p => p.GetType()).ToArray());
            if (methodInfo == null)
            {
                throw new ArgumentException($"Method \"{methodName}\" not found in type \"{typeName}\"");
            }

            // If the method is static, no need to create an instance
            if (methodInfo.IsStatic)
            {
                // Invoke the method with the specified parameters
                object result = methodInfo.Invoke(null, parameters);
                return result;
            }
            else
            {
                // Create an instance of the type
                object instance = Activator.CreateInstance(type);
                if (typeName == "EZCodeLanguage.EZHelp") instance = e;

                // Invoke the non-static method with the specified parameters
                object result = methodInfo.Invoke(instance, parameters);
                return result;
            }
        }
    }
}
