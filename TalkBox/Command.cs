using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace TalkBox
{
    // Most of the code here is stolen from "Yarn Spinner" (https://github.com/YarnSpinnerTool)
    // Specifically, these two directories: https://github.com/YarnSpinnerTool/YarnSpinner and https://github.com/YarnSpinnerTool/YarnSpinner-Unity
    // I did however change a few things to make it work outside of Unity, and I need to change a few more.
    // However, I have decided that it shall wait until it is needed.
    // TODO: Handle running IEnumerators. Be able to actually start a command. Clean it all up a whole lot.
    using Injector = Func<string, object?>;
    using Converter = Func<string, object?>;

    // I made this. It's for making the CreateConverter function able to search through a list of things with names to find a name that matches.
    interface ISearchable
    {
        public string Name { get; }

        public object This { get => this; }
    }

    class ActionA : Attribute
    {
        public string? Name { get; set; }

        public ActionA(string? name = null) => Name = name;
    }
    class CommandA : ActionA
    {
        public string? Injector { get; set; }

        public CommandA(string? name = null) => Name = name;
    }
    class FunctionA : ActionA
    {
        public string? Injector { get; set; }

        public FunctionA(string? name = null) => Name = name;
    }

    class StateInjectorA : Attribute
    {
        public string? Injector { get; set; }

        public StateInjectorA(string? injector = null) => Injector = injector;
    }

    class CommandManager
    {
        private const BindingFlags IgnoreVisiblity = BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllInstanceMembers = BindingFlags.Instance | IgnoreVisiblity;
        private const BindingFlags AllStaticMembers = BindingFlags.Static | IgnoreVisiblity;
        private const BindingFlags AllMembers = AllInstanceMembers | AllStaticMembers;

        public static bool IsInjector(Type injectorType, MethodInfo? injectorFunction, Type? destinationType = null)
        {
            destinationType ??= injectorType;

            if (injectorFunction == null
                || !injectorFunction.IsStatic
                || injectorFunction.ReturnType == typeof(void)
                || !destinationType.IsAssignableFrom(injectorFunction.ReturnType))
            { return false; }

            var parameters = injectorFunction.GetParameters();
            return parameters.Count(param => !param.IsOptional) == 1
                && parameters[0].ParameterType == typeof(string);
        }

        private static Injector? GetInjectorForMethod(Type injectorType, CommandA metadata)
        {
            var injectorFunction = metadata.Injector == null
                ? null
                : injectorType.GetMethod(metadata.Injector, AllStaticMembers);
            if (injectorFunction != null && IsInjector(injectorType, injectorFunction))
            {
                return (Injector)injectorFunction.CreateDelegate(typeof(Injector));
            }
            return null;
        }

        private static Injector? GetInjectorForType(Type injectorType, ref Dictionary<string, Injector?> injectorCache)
        {
            string? fullyQualifiedName = injectorType.AssemblyQualifiedName;

            if (fullyQualifiedName == null) throw new Exception("I don't quite understand what went wrong, but it went wrong here.");

            if (!injectorCache.ContainsKey(fullyQualifiedName))
            {
                string? injector = injectorType.GetCustomAttribute<StateInjectorA>()?.Injector;
                var injectorFunction = string.IsNullOrEmpty(injector)
                    ? null
                    : injectorType.GetMethod(injector, AllStaticMembers);

                if (injectorFunction != null && IsInjector(injectorType, injectorFunction))
                {
                    injectorCache.Add(fullyQualifiedName, (Injector)injectorFunction.CreateDelegate(typeof(Injector)));
                }
                else
                {
                    // default cache to null so that next time, we know we've looked at least
                    injectorCache.Add(fullyQualifiedName, null);
                }
            }

            return injectorCache[fullyQualifiedName];
        }

        private static Converter?[] CreateConverters(MethodInfo method)
        {
            return method.GetParameters().Select((param, i) => CreateConverter(method, param, i)).ToArray();
        }

        private static Converter? CreateConverter(MethodInfo method, ParameterInfo parameter, int index)
        {
            var targetType = parameter.ParameterType;

            // well, I mean...
            if (targetType == typeof(string)) { return null; }

            // find the GameObject.
            if (typeof(ISearchable).IsAssignableFrom(targetType))
            {
                return name =>
                {
                    if (searchableList == null)
                    {
                        return null;
                    }
                    foreach (ISearchable item in searchableList)
                    {
                        if (item.Name == name)
                        {
                            return item.This;
                        }
                    }
                    return null;
                };
            }

            // Currently I have nothing to convert this code into doing. Maybe in the future.

            // find components of the GameObject with the component, if available
            // if (typeof(Component).IsAssignableFrom(targetType))
            // {
            //     var paramMetadata = parameter.GetCustomAttribute<YarnParameterAttribute>();
            //     if (paramMetadata != null)
            //     {
            //         var methodType = method.DeclaringType;
            //         var injectorMeta = methodType.GetMethod(paramMetadata.Injector, AllStaticMembers);
            //         if (IsInjector(methodType, injectorMeta, targetType))
            //         {
            //             return (Injector)injectorMeta.CreateDelegate(typeof(Injector));
            //         }
            //     }
            //     return arg => GameObject.Find(arg)?.GetComponentInChildren(targetType);
            // }

            // Note: the below comment is from the code I stole this from. I have no idea why a bool can be the parameter name and why that makes it true.
            // bools can take "true" or "false", or the parameter name.
            if (typeof(bool).IsAssignableFrom(targetType))
            {
                return arg =>
                {
                    if (arg.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase)) { return true; }
                    if (bool.TryParse(arg, out bool res)) { return res; }
                    throw new ArgumentException(
                        $"Can't convert the given parameter at position {index + 1} (\"{arg}\") to parameter " +
                        $"{parameter.Name} of type {typeof(bool).FullName}.");
                };
            }

            // try converting using IConvertible.
            return arg =>
            {
                try
                {
                    return Convert.ChangeType(arg, targetType, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Can't convert the given parameter at position {index + 1} (\"{arg}\") to parameter " +
                        $"{parameter.Name} of type {targetType.FullName}: {e}");
                }
            };
        }

        private static DispatchCommand CreateCommandRunner(
            MethodInfo method, CommandA metadata, ref Dictionary<string, Injector?> injectorCache)
        {
            var methodType = method.DeclaringType;
            if (methodType == null) { throw new ArgumentException($"Method {method.Name} does not have a type...somehow."); }

            Injector? injector = null;
            if (!method.IsStatic)
            {
                injector = GetInjectorForMethod(methodType, metadata)
                    ?? GetInjectorForType(methodType, ref injectorCache)
                    ?? throw new Exception("Make the IDE happy.");
                // ?? GetDefaultMonoBehaviourInjector(methodType, metadata.Name);
            }

            Injector?[] converters = CreateConverters(method);

            return new DispatchCommand(method, injector, converters);
        }

        private static Type GetFuncType(int paramCount)
        {
            // this causes me physical pain - a comment by the code's original authors.
            return paramCount switch
            {
                0 => typeof(Func<>),
                1 => typeof(Func<,>),
                2 => typeof(Func<,,>),
                3 => typeof(Func<,,,>),
                4 => typeof(Func<,,,,>),
                5 => typeof(Func<,,,,,>),
                6 => typeof(Func<,,,,,,>),
                7 => typeof(Func<,,,,,,,>),
                8 => typeof(Func<,,,,,,,,>),
                9 => typeof(Func<,,,,,,,,,>),
                10 => typeof(Func<,,,,,,,,,,>),
                11 => typeof(Func<,,,,,,,,,,,>),
                12 => typeof(Func<,,,,,,,,,,,,>),
                13 => typeof(Func<,,,,,,,,,,,,,>),
                14 => typeof(Func<,,,,,,,,,,,,,,>),
                15 => typeof(Func<,,,,,,,,,,,,,,,>),
                16 => typeof(Func<,,,,,,,,,,,,,,,,>),
                _ => throw new ArgumentException("Delegates are limited to 16 parameters. Consider splitting up " +
                    "the implementation into multiple parts.")
            };
        }

        private static Delegate GetFunctionRunner(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var paramTypes = parameters.Select(param => param.ParameterType).Append(method.ReturnType).ToArray();
            return method.CreateDelegate(GetFuncType(parameters.Length).MakeGenericType(paramTypes));
        }

        private static string GetActionName(ActionA metadata, MethodInfo method)
        {
            return string.IsNullOrEmpty(metadata.Name) ? method.Name : metadata.Name;
        }

        private static void FindAllActions(IEnumerable<string> assemblyNames)
        {
            if (commands == null)
            {
                commands = new Dictionary<string, DispatchCommand>();
            }

            if (functions == null)
            {
                functions = new Dictionary<string, Delegate>();
            }

            if (searchedAssemblyNames == null)
            {
                searchedAssemblyNames = new HashSet<string>();
            }

            var assemblyNamesToLoad = assemblyNames.Except(searchedAssemblyNames);

            var injectorCache = new Dictionary<string, Injector?>();

            // Find the assemblies we're looking for
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Join(
                        assemblyNamesToLoad,
                        (assembly => assembly.GetName().Name),
                        (name => name),
                        (assembly, name) => assembly
                    ).ToList();

            // Record that we've searched these assemblies before, so we don't
            // try and do it again
            foreach (var assemblyName in assemblyNamesToLoad)
            {
                searchedAssemblyNames.Add(assemblyName);
            }

            // Search for all methods in these assemblies
            var allMethods = assemblies
                // It used to be GetLoadableTypes instead of GetTypes, but as far as I cam tell that function doesn't exist, so hopefully this will still work.
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(AllMembers).Select(method => (Type: type, Method: method)))
                .Where(m => m.Method.DeclaringType == m.Type); // don't register inherited methods

            foreach (var (_, method) in allMethods)
            {
                // We only care about methods with a YarnCommand or YarnFunction
                // attribute. Get the attributes for this method, and see if
                // it's one we should use.
                var attributes = method.GetCustomAttributes(false);

                foreach (var attribute in attributes)
                {
                    if (attribute is CommandA command)
                    {
                        // It's a command!
                        var name = GetActionName(command, method);
                        try
                        {
                            commands.Add(name, CreateCommandRunner(method, command, ref injectorCache));
                        }
                        catch (ArgumentException)
                        {
                            MethodInfo existingDefinition = commands[name].Method;
                            Console.WriteLine($"Can't add {method.DeclaringType?.FullName}.{method.Name} for command {name} " +
                                $"because it's already defined on {existingDefinition?.DeclaringType?.FullName}.{existingDefinition?.Name}");
                        }
                    }
                    else if (attribute is FunctionA function)
                    {
                        // It's a function!
                        var name = GetActionName(function, method);
                        try
                        {
                            functions.Add(name, GetFunctionRunner(method));
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine($"Can't add {method.DeclaringType?.FullName}.{method.Name} for command {name}: {e.Message}");
                        }
                    }
                }

            }
        }

        private static List<ISearchable>? searchableList = null;

        private static Dictionary<string, DispatchCommand> commands;

        private static Dictionary<string, Delegate> functions;

        private static HashSet<string> searchedAssemblyNames;

        private static List<IEnumerator> runningCommands;


        internal enum CommandDispatchResult
        {
            /// <summary>
            /// The command was located and successfully called.
            /// </summary>
            Success,

            /// <summary>
            /// The command was located, but failed to be called.
            /// </summary>
            Failed,

            /// <summary>
            /// The command could not be found.
            /// </summary>
            NotFound,
        }

        public static CommandDispatchResult TryExecuteCommand(string[] args, out object? returnValue)
        {
            if (!commands.TryGetValue(args[0], out var command))
            {
                returnValue = null;

                // We didn't find a command handler with this name. Stop here!
                return CommandDispatchResult.NotFound;
            }

            // Attempt to invoke the command handler we found, and return a
            // value indicating whether it succeeded or failed.
            var result = command.TryInvoke(args, out returnValue);
            if (result)
            {
                return CommandDispatchResult.Success;
            }
            else
            {
                return CommandDispatchResult.Failed;
            }
        }

        /// <summary>
        /// Attempt to parse the arguments to apply to the method.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object[] ParseArgs(MethodInfo method, string[] args)
        {
            return ParseArgs(method, CreateConverters(method), args, true);
        }

        /// <summary>
        /// Attempt to parse the arguments with cached converters.
        /// </summary>
        /// <param name="method">The method to parse args for.</param>
        /// <param name="converters">Converters to use. Will be assumed that
        /// the converters correctly correspond to <paramref name="method"/>.
        /// </param>
        /// <param name="args">The raw list of arguments, including command and
        /// instance name.</param>
        /// <param name="isStatic">Should we treat this function as static?
        /// </param>
        /// <returns>The parsed arguments.</returns>
        public static object[] ParseArgs(
            MethodInfo method, Converter?[] converters, string[] args, bool isStatic = false)
        {
            var parameters = method.GetParameters();
            int optional = parameters.Count(param => param.IsOptional);
            int required = parameters.Length - optional;
            int lead = isStatic ? 1 : 2;
            var count = args.Length - lead;

            if (optional > 0)
            {
                if (count < required || count > parameters.Length)
                {
                    throw new ArgumentException(
                        $"{method.Name} requires between {required} and {parameters.Length} parameters, but {count} " +
                        $"{(count == 1 ? "was" : "were")} provided.");
                }
            }
            else if (count != required)
            {
                var requiredParameterTypeNames = string.Join(", ", parameters.Where(p => !p.IsOptional).Select(p => p.ParameterType.ToString()));

                throw new ArgumentException($"{method.Name} requires {required} parameters ({requiredParameterTypeNames}), but {count} " +
                    $"{(count == 1 ? "was" : "were")} provided.");
            }

            var finalArgs = new object[parameters.Length];

            for (int i = 0; i < count; i++)
            {
                string arg = args[i + lead];
                finalArgs[i] = converters[i] == null ? arg : converters[i]?.Invoke(arg) ?? throw new Exception("Make the IDE happy.");
            }
            for (int i = count; i < finalArgs.Length; i++)
            {
                finalArgs[i] = Type.Missing;
            }
            return finalArgs;
        }

        /// <summary>
        /// Registers all functions known to this <see cref="ActionManager"/>
        /// into a <see cref="Library"/>.
        /// </summary>
        /// <remarks>
        /// Existing functions in the Library will not be modified.
        /// </remarks>
        /// <param name="library">Library instance to register functions
        /// into.</param>
        public static void RegisterFunctions(Library library)
        {
            foreach (var kv in functions)
            {
                if (library.FunctionExists(kv.Key) == false)
                {
                    library.RegisterFunction(kv.Key, kv.Value);
                }
            }
        }

        static CommandManager()
        {
            // We always want to get actions from the default Unity code
            // assembly, "Assembly-CSharp". Start by searching it.
            // AddActionsFromAssemblies(new[] {"Assembly-CSharp"});

            // Personally, I am not even slightly interested in the default Unity code, because I'm not using Unity.
            // However, I am interested in making the IDE stop complaining about possible null values all the time.
            commands = new Dictionary<string, DispatchCommand>();
            functions = new Dictionary<string, Delegate>();
            searchedAssemblyNames = new HashSet<string>();
            runningCommands = new List<IEnumerator>();
        }

        /// <summary>
        /// Searches all loaded assemblies whose names are equal to those found
        /// in <paramref name="assemblyNames"/>, and registers all methods that
        /// have the <see cref="YarnCommandAttribute"/> and <see
        /// cref="YarnFunctionAttribute"/> attributes.
        /// </summary>
        /// <param name="assemblyNames">The names of the assemblies to
        /// search.</param>
        public static void AddActionsFromAssemblies(IEnumerable<string> assemblyNames)
        {
            FindAllActions(assemblyNames);
        }

        /// <summary>
        /// Removes all registered commands and functions.
        /// </summary>
        public static void ClearAllActions()
        {
            commands.Clear();
            functions.Clear();
            searchedAssemblyNames.Clear();
        }

        public static List<MethodInfo> FunctionsInfo()
        {
            var funcs = new List<MethodInfo>();
            foreach (var func in functions.Values)
            {
                funcs.Add(func.Method);
            }
            return funcs;
        }
    }

    class DispatchCommand
    {
        public MethodInfo Method { get; set; }
        public Injector? Injector { get; set; }
        public Converter?[] Converters { get; set; }

        public DispatchCommand(MethodInfo method, Injector? injector, Converter?[] converters)
        {
            Method = method;
            Injector = injector;
            Converters = converters;
        }

        public bool TryInvoke(string[] args, out object? returnValue)
        {
            returnValue = null;

            // if the method isn't static, but doesn't have an object name,
            // then we can't proceed, but it might be caught by a manually
            // registered function.
            if (!Method.IsStatic && args.Length < 2) { return false; }

            try
            {
                var instance = Method.IsStatic ? null : Injector?.Invoke(args[1]);
                var finalArgs = CommandManager.ParseArgs(Method, Converters, args, Method.IsStatic);
                returnValue = Method.Invoke(instance, finalArgs);
                return true;
            }
            catch (Exception e) when (
                e is ArgumentException // when arguments are invalid
                || e is TargetException // when a method is not static, but the instance ended up null
            )
            {
                Console.WriteLine($"Can't run command {args[0]}: {e.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// A collection of functions that can be called from Yarn programs.
    /// </summary>
    /// <remarks>
    /// You do not create instances of this class yourself. The <see
    /// cref="Dialogue"/> class creates one of its own, which you can
    /// access via the <see cref="Dialogue.Library"/> property.
    /// </remarks>
    /// <seealso cref="Dialogue"/>
    public class Library
    {

        internal Dictionary<string, Delegate> Delegates = new Dictionary<string, Delegate>();

        /// <summary>
        /// Returns a <see cref="Delegate"/> with a given name.
        /// </summary>
        /// <param name="name">The name of the function to
        /// retrieve.</param>
        /// <returns>The <see cref="Delegate"/>.</returns>
        /// <throws cref="InvalidOperationException">Thrown when a function
        /// named <c>name</c> is not present in the library.</throws>
        public Delegate GetFunction(string name)
        {
            try
            {
                return Delegates[name];
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException($"Function {name} is not present in the library.");
            }

        }

        /// <summary>
        /// Loads functions from another <see cref="Library"/>.
        /// </summary>
        /// <param name="otherLibrary">The library to import functions from.</param>
        /// <remarks>
        /// If the other library contains a function with the same name as
        /// one in this library, the function in the other library takes
        /// precedence.
        /// </remarks>
        public void ImportLibrary(Library otherLibrary)
        {
            foreach (var entry in otherLibrary.Delegates)
            {
                Delegates[entry.Key] = entry.Value;
            }
        }

        /// <summary>
        /// Registers a new function that returns a value, which can be
        /// called from a Yarn program.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="implementation">The method to
        /// be invoked when the function is called.</param>
        /// <typeparam name="TResult">The return type of the
        /// function.</typeparam>
        /// <exception cref="ArgumentException">Thrown when a function
        /// named <paramref name="name"/> already exists in the <see
        /// cref="Library"/>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when name is
        /// null.</exception>
        public void RegisterFunction<TResult>(string name, Func<TResult> implementation)
        {
            RegisterFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="RegisterFunction{TResult}(string, Func{TResult})"/>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <typeparam name="T1">The type of the function's first argument.</typeparam>
        public void RegisterFunction<T1, TResult>(string name, Func<T1, TResult> implementation)
        {
            RegisterFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="RegisterFunction{TResult}(string, Func{TResult})"/>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <typeparam name="T1">The type of the function's first argument.</typeparam>
        /// <typeparam name="T2">The type of the function's second argument.</typeparam>
        public void RegisterFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> implementation)
        {
            RegisterFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="RegisterFunction{TResult}(string, Func{TResult})"/>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <typeparam name="T1">The type of the function's first argument.</typeparam>
        /// <typeparam name="T2">The type of the function's second argument.</typeparam>
        /// <typeparam name="T3">The type of the function's third argument.</typeparam>
        public void RegisterFunction<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> implementation)
        {
            RegisterFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="RegisterFunction{TResult}(string, Func{TResult})"/>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <typeparam name="T1">The type of the function's first argument.</typeparam>
        /// <typeparam name="T2">The type of the function's second argument.</typeparam>
        /// <typeparam name="T3">The type of the function's third argument.</typeparam>
        /// <typeparam name="T4">The type of the function's fourth argument.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> implementation)
        {
            RegisterFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="RegisterFunction{TResult}(string, Func{TResult})"/>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <typeparam name="T1">The type of the function's first argument.</typeparam>
        /// <typeparam name="T2">The type of the function's second argument.</typeparam>
        /// <typeparam name="T3">The type of the function's third argument.</typeparam>
        /// <typeparam name="T4">The type of the function's fourth argument.</typeparam>
        /// <typeparam name="T5">The type of the function's fifth argument.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, TResult>(string name, Func<T1, T2, T3, T4, T5, TResult> implementation)
        {
            RegisterFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="RegisterFunction{TResult}(string,
        /// Func{TResult})"/>
        public void RegisterFunction(string name, Delegate implementation)
        {
            Delegates.Add(name, implementation);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Library"/> contains a function named <c>name</c>.
        /// </summary>
        /// <param name="name">The name of the function to look for.</param>
        /// <returns><c>true</c> if a function exists in this Library; <c>false</c> otherwise.</returns>
        public bool FunctionExists(string name)
        {
            return Delegates.ContainsKey(name);
        }

        /// <summary>
        /// Removes a function from the Library.
        /// </summary>
        /// <param name="name">The name of the function to remove.</param>
        /// <remarks>
        /// If no function with the given name is present in the Library,
        /// this method does nothing.
        /// </remarks>
        public void DeregisterFunction(string name)
        {
            if (FunctionExists(name))
            {
                Delegates.Remove(name);
            }
        }

        // This code complains about missing IType and TypeUtil, but it's not needed so I don't care.
        /// <summary>
        /// Registers the methods found inside a type.
        /// </summary>
        /// <param name="type">The type to register methods from.</param>
        // protected void RegisterMethods(IType type)
        // {
        //     var methods = type.Methods;

        //     if (methods == null)
        //     {
        //         // this Type declares no methods; nothing to do
        //         return;
        //     }

        //     foreach (var methodDefinition in methods)
        //     {
        //         var methodName = methodDefinition.Key;
        //         var methodImplementation = methodDefinition.Value;

        //         var canonicalName = TypeUtil.GetCanonicalNameForMethod(type, methodName);

        //         this.RegisterFunction(canonicalName, methodImplementation);
        //     }
        // }

        /// <summary>
        /// Generates a unique tracking variable name.
        /// This is intended to be used to generate names for visting.
        /// Ideally these will very reproduceable and sensible.
        /// For now it will be something terrible and easy.
        /// </summary>
        /// <param name="nodeName">The name of the node that needs to
        /// have a tracking variable created.</param>
        /// <returns>The new variable name.</returns>
        public static string GenerateUniqueVisitedVariableForNode(string nodeName)
        {
            return $"$Yarn.Internal.Visiting.{nodeName}";
        }
    }
}