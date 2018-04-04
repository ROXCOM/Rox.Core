using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Threading;

namespace Rox.Core
{
    //public class FieldDescriptor
    //{
    //    public FieldDescriptor(string fieldName, Type fieldType)
    //    {
    //        FieldName = fieldName;
    //        FieldType = fieldType;
    //    }
    //    public string FieldName { get; }
    //    public Type FieldType { get; }
    //}

    //public static class MyTypeBuilder
    //{
    //    public static object CreateNewObject()
    //    {
    //        var myTypeInfo = CompileResultTypeInfo();
    //        var myType = myTypeInfo.AsType();
    //        var myObject = Activator.CreateInstance(myType);

    //        return myObject;
    //    }

    //    public static TypeInfo CompileResultTypeInfo()
    //    {
    //        TypeBuilder tb = GetTypeBuilder();
    //        ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

    //        var yourListOfFields = new List<FieldDescriptor>()
    //        {
    //            new FieldDescriptor("YourProp1", typeof(string)),
    //            new FieldDescriptor("YourProp2", typeof(int))
    //        };
    //        foreach (var field in yourListOfFields)
    //            CreateProperty(tb, field.FieldName, field.FieldType);

    //        TypeInfo objectTypeInfo = tb.CreateTypeInfo();
    //        return objectTypeInfo;
    //    }

    //    private static TypeBuilder GetTypeBuilder()
    //    {
    //        var typeSignature = "MyDynamicType";
    //        var an = new AssemblyName(typeSignature);
    //        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
    //        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
    //        TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
    //                TypeAttributes.Public |
    //                TypeAttributes.Class |
    //                TypeAttributes.AutoClass |
    //                TypeAttributes.AnsiClass |
    //                TypeAttributes.BeforeFieldInit |
    //                TypeAttributes.AutoLayout,
    //                null);
    //        return tb;
    //    }

    //    private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
    //    {
    //        FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

    //        PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
    //        MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
    //        ILGenerator getIl = getPropMthdBldr.GetILGenerator();

    //        getIl.Emit(OpCodes.Ldarg_0);
    //        getIl.Emit(OpCodes.Ldfld, fieldBuilder);
    //        getIl.Emit(OpCodes.Ret);

    //        MethodBuilder setPropMthdBldr =
    //            tb.DefineMethod("set_" + propertyName,
    //              MethodAttributes.Public |
    //              MethodAttributes.SpecialName |
    //              MethodAttributes.HideBySig,
    //              null, new[] { propertyType });

    //        ILGenerator setIl = setPropMthdBldr.GetILGenerator();
    //        Label modifyProperty = setIl.DefineLabel();
    //        Label exitSet = setIl.DefineLabel();

    //        setIl.MarkLabel(modifyProperty);
    //        setIl.Emit(OpCodes.Ldarg_0);
    //        setIl.Emit(OpCodes.Ldarg_1);
    //        setIl.Emit(OpCodes.Stfld, fieldBuilder);

    //        setIl.Emit(OpCodes.Nop);
    //        setIl.MarkLabel(exitSet);
    //        setIl.Emit(OpCodes.Ret);

    //        propertyBuilder.SetGetMethod(getPropMthdBldr);
    //        propertyBuilder.SetSetMethod(setPropMthdBldr);
    //    }
    //}

    //public delegate object FastInvokeHandler(object
    //            target, object[] paramters);


    //public class Class1
    //{
    //}

    //public class FastInvoke
    //{
    //    FastInvokeHandler MyDelegate;
    //    public MethodInfo MyMethodInfo;
    //    public ParameterInfo[] MyParameters;
    //    Object HostObject;
    //    public int NumberOfArguments;

    //    public FastInvoke(Object MyObject, String MyName)
    //    {
    //        HostObject = MyObject;
    //        Type t2 = MyObject.GetType();
    //        MethodInfo m2 = t2.GetMethod(MyName);
    //        MyDelegate = GetMethodInvoker(m2);
    //        NumberOfArguments = m2.GetParameters().Length;
    //        MyMethodInfo = m2;
    //        MyParameters = m2.GetParameters();
    //    }

    //    public object ExecuteDelegate(object[] FunctionParameters)
    //    {
    //        try
    //        {
    //            return (MyDelegate(HostObject, FunctionParameters));
    //        }
    //        catch (Exception e)
    //        {
    //            Object o = new Object();
    //            o = e.Message;
    //            return (o);

    //        }

    //    }

    //    private FastInvokeHandler GetMethodInvoker(MethodInfo methodInfo)
    //    {
    //        DynamicMethod dynamicMethod = new DynamicMethod(string.Empty,
    //                      typeof(object), new Type[] { typeof(object),
    //                      typeof(object[]) },
    //                      methodInfo.DeclaringType.Module);
    //        ILGenerator il = dynamicMethod.GetILGenerator();
    //        ParameterInfo[] ps = methodInfo.GetParameters();
    //        Type[] paramTypes = new Type[ps.Length];
    //        for (int i = 0; i < paramTypes.Length; i++)
    //        {
    //            if (ps[i].ParameterType.IsByRef)
    //                paramTypes[i] = ps[i].ParameterType.GetElementType();
    //            else
    //                paramTypes[i] = ps[i].ParameterType;
    //        }
    //        LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];

    //        for (int i = 0; i < paramTypes.Length; i++)
    //        {
    //            locals[i] = il.DeclareLocal(paramTypes[i], true);
    //        }
    //        for (int i = 0; i < paramTypes.Length; i++)
    //        {
    //            il.Emit(OpCodes.Ldarg_1);
    //            EmitFastInt(il, i);
    //            il.Emit(OpCodes.Ldelem_Ref);
    //            EmitCastToReference(il, paramTypes[i]);
    //            il.Emit(OpCodes.Stloc, locals[i]);
    //        }
    //        if (!methodInfo.IsStatic)
    //        {
    //            il.Emit(OpCodes.Ldarg_0);
    //        }
    //        for (int i = 0; i < paramTypes.Length; i++)
    //        {
    //            if (ps[i].ParameterType.IsByRef)
    //                il.Emit(OpCodes.Ldloca_S, locals[i]);
    //            else
    //                il.Emit(OpCodes.Ldloc, locals[i]);
    //        }
    //        if (methodInfo.IsStatic)
    //            il.EmitCall(OpCodes.Call, methodInfo, null);
    //        else
    //            il.EmitCall(OpCodes.Callvirt, methodInfo, null);
    //        if (methodInfo.ReturnType == typeof(void))
    //            il.Emit(OpCodes.Ldnull);
    //        else
    //            EmitBoxIfNeeded(il, methodInfo.ReturnType);

    //        for (int i = 0; i < paramTypes.Length; i++)
    //        {
    //            if (ps[i].ParameterType.IsByRef)
    //            {
    //                il.Emit(OpCodes.Ldarg_1);
    //                EmitFastInt(il, i);
    //                il.Emit(OpCodes.Ldloc, locals[i]);
    //                if (locals[i].LocalType.IsValueType)
    //                    il.Emit(OpCodes.Box, locals[i].LocalType);
    //                il.Emit(OpCodes.Stelem_Ref);
    //            }
    //        }

    //        il.Emit(OpCodes.Ret);
    //        FastInvokeHandler invoder = (FastInvokeHandler)
    //           dynamicMethod.CreateDelegate(typeof(FastInvokeHandler));
    //        return invoder;
    //    }

    //    private static void EmitCastToReference(ILGenerator il,
    //                                            System.Type type)
    //    {
    //        if (type.IsValueType)
    //        {
    //            il.Emit(OpCodes.Unbox_Any, type);
    //        }
    //        else
    //        {
    //            il.Emit(OpCodes.Castclass, type);
    //        }
    //    }

    //    private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
    //    {
    //        if (type.IsValueType)
    //        {
    //            il.Emit(OpCodes.Box, type);
    //        }
    //    }

    //    private static void EmitFastInt(ILGenerator il, int value)
    //    {
    //        switch (value)
    //        {
    //            case -1:
    //                il.Emit(OpCodes.Ldc_I4_M1);
    //                return;
    //            case 0:
    //                il.Emit(OpCodes.Ldc_I4_0);
    //                return;
    //            case 1:
    //                il.Emit(OpCodes.Ldc_I4_1);
    //                return;
    //            case 2:
    //                il.Emit(OpCodes.Ldc_I4_2);
    //                return;
    //            case 3:
    //                il.Emit(OpCodes.Ldc_I4_3);
    //                return;
    //            case 4:
    //                il.Emit(OpCodes.Ldc_I4_4);
    //                return;
    //            case 5:
    //                il.Emit(OpCodes.Ldc_I4_5);
    //                return;
    //            case 6:
    //                il.Emit(OpCodes.Ldc_I4_6);
    //                return;
    //            case 7:
    //                il.Emit(OpCodes.Ldc_I4_7);
    //                return;
    //            case 8:
    //                il.Emit(OpCodes.Ldc_I4_8);
    //                return;
    //        }

    //        if (value > -129 && value < 128)
    //        {
    //            il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
    //        }
    //        else
    //        {
    //            il.Emit(OpCodes.Ldc_I4, value);
    //        }
    //    }

    //    public object TypeConvert(object source, Type DestType)
    //    {

    //        object NewObject = System.Convert.ChangeType(source, DestType);

    //        return (NewObject);
    //    }

    //}

    /// <summary>
    /// Returns objects that implement an interface, without the need to manually create a type 
    /// that implements the interface
    /// </summary>
    public static class InterfaceObjectFactory
    {
        /// <summary>
        /// All of the types generated for each interface.  This dictionary is indexed by the 
        /// interface's type object
        /// </summary>
        private static Dictionary<Type, Type> InterfaceImplementations = new Dictionary<Type, Type>();

        /// <summary>
        /// Дефайнит AsemblyBuilder для указанного имени assembly
        /// </summary>
        /// <param name="name">Имя сборки (assembly)</param>
        /// <param name="builderAccess">билдер сборки (assembly)</param>
        /// <returns></returns>
        private static AssemblyBuilder GetAssemblyBuilder(string name, AssemblyBuilderAccess builderAccess = AssemblyBuilderAccess.Run, IEnumerable<CustomAttributeBuilder> assemblyAttributes = null)
        {
            if (assemblyAttributes is null)
                return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = name }, builderAccess);
            else
                return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = name }, builderAccess, assemblyAttributes);
        }

        /// <summary>
        /// Дефайнит ModuleBuilder для указанного имени Module
        /// </summary>
        /// <param name="builder">AssemblyBuilder</param>
        /// <param name="name">Имя для Module</param>
        /// <returns>ModuleBuilder</returns>
        private static ModuleBuilder GetModuleBuilder(this AssemblyBuilder builder, string name)
        {
            return builder.DefineDynamicModule(name);
        }

        /// <summary>
        /// Определяет TypeBuilder для указанного имени типа
        /// </summary>
        /// <param name="builder">Module</param>
        /// <param name="name">Имя типа (Type)</param>
        /// <param name="genericParameters">Указание шаблонных параметров для типа (Type)</param>
        /// <returns></returns>
        public static TypeBuilder GetTypeBuilder(this ModuleBuilder builder, string name, Type parent, params string[] genericParameters)
        {
            var typeBuilder = builder.DefineType(name, TypeAttributes.Class | TypeAttributes.Public, parent);

            if (genericParameters == null || genericParameters.Length == 0)
                return typeBuilder;

            var genBuilders = typeBuilder.DefineGenericParameters(genericParameters);

            foreach (var genBuilder in genBuilders) {
                genBuilder.SetGenericParameterAttributes(GenericParameterAttributes.ReferenceTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint);
            }

            return typeBuilder;
        }

        /// <summary>
        /// Возвращает MethodBuilder для указанного имени
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MethodBuilder GetMethodBuilder(this TypeBuilder builder, string name)
        {
            return builder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.HideBySig);
        }
        public static MethodBuilder GetMethodBuilder(this TypeBuilder builder, string name, Type returnType, params Type[] parameterTypes)
        {
            return builder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, returnType, parameterTypes);
        }
        public static MethodBuilder GetMethodBuilder(this TypeBuilder builder, string name, Type returnType, string[] genericParameters, params Type[] parameterTypes)
        {
            var methodBuilder = builder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, returnType, parameterTypes);

            var genBuilders = methodBuilder.DefineGenericParameters(genericParameters);

            foreach (var genBuilder in genBuilders)
            {
                genBuilder.SetGenericParameterAttributes(GenericParameterAttributes.ReferenceTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint);
            }
            return methodBuilder;
        }

        public static Type CreateType<T>(this Type baseType)
        {
            return CreateType(baseType, typeof(T));
        }

        private static void EmitCastToReference(ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        private static void EmitBoxIfNeeded(ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        private static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        private static void DelegateMethodBody(ILGenerator il, MethodInfo methodInfo, FieldInfo field)
        {
            var ps = methodInfo.GetParameters();
            var paramTypes = new Type[ps.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    paramTypes[i] = ps[i].ParameterType.GetElementType();
                else
                    paramTypes[i] = ps[i].ParameterType;
            }
            var locals = new LocalBuilder[paramTypes.Length];

            for (int i = 0; i < paramTypes.Length; i++)
            {
                locals[i] = il.DeclareLocal(paramTypes[i], true);
            }

            // call DelegateFunc or DelegateAction
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, methodInfo.Name);
            il.Emit(OpCodes.Call, methodInfo);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);

            var length = ps.Length - 1;
            if (length > 0)
            {
                // new array
                EmitFastInt(il, length);
                il.Emit(OpCodes.Newarr, typeof(object));

                for (var i = 0; i < length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    EmitFastInt(il, i);             // add index array
                    il.Emit(OpCodes.Ldarg, i + 1);  // add object to i argument
                    var type = paramTypes[i + 1];
                    EmitBoxIfNeeded(il, type);
                    il.Emit(OpCodes.Stelem_Ref); // replace array element
                }
            }

            //il.Emit(OpCodes.Callvirt, );

            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i++);
                EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }
            if (!methodInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca_S, locals[i]);
                else
                    il.Emit(OpCodes.Ldloc, locals[i]);
            }
            if (methodInfo.IsStatic)
                il.EmitCall(OpCodes.Call, methodInfo, null);
            else
                il.EmitCall(OpCodes.Callvirt, methodInfo, null);
            if (methodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                EmitBoxIfNeeded(il, methodInfo.ReturnType);

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    EmitFastInt(il, i);
                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        private static void WriteLine(this ILGenerator il, string text)
        {
            il.Emit(OpCodes.Ldstr, text);
            var info = typeof(Console).GetMethod("WriteLine", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
            il.Emit(OpCodes.Call, info);
        }

        /// <summary>
        /// Creates a method that will generate an object that implements the interface for the 
        /// given type.
        /// </summary>
        /// <param name="interfaceImplement"></param>
        public static Type CreateType(Type baseType, Type interfaceImplement)
        {
            // Error checking...
            // Make sure that the type is an interface

            if (!interfaceImplement.IsInterface)
                throw new ArgumentException($"type '{interfaceImplement.FullName}' not interface");

            var assemblyBuilder = GetAssemblyBuilder("InterfaceObjectFactoryAssembly", builderAccess: AssemblyBuilderAccess.Run);

            // This ModuleBuilder is used for all generated classes.  It's only constructed once, 
            //the first time that the InterfaceObjectFactory is used
            var ModuleBuilder = assemblyBuilder.GetModuleBuilder("InterfaceObjectFactoryModule");

            var typeBuilder = ModuleBuilder.GetTypeBuilder("ImplOf" + interfaceImplement.Name, baseType);

            // Add interface implementation
            typeBuilder.AddInterfaceImplementation(interfaceImplement);

            // Create Constructor
            var baseConstructorInfos = baseType.GetConstructors();
            var baseConstructorInfo = baseConstructorInfos.First();

            var ctorTypes = baseConstructorInfo.GetParameters().Select(p => p.ParameterType).ToArray();

            var constructorBuilder = typeBuilder.DefineConstructor(
                           MethodAttributes.Public,
                           CallingConventions.Standard | CallingConventions.HasThis,
                           ctorTypes);

            var il = constructorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, baseConstructorInfo);
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Nop); 
            il.Emit(OpCodes.Ret);

            // Get a list of all methods, including methods in inherited interfaces
            // The methods that aren't accessors and will need default implementations...  However,
            // a property's accessors are also methods!
            var methods = new List<MethodInfo>();
            AddMethodsToList(methods, interfaceImplement);

            // Get a list of all of the properties, including properties in inherited interfaces
            var properties = new List<PropertyInfo>();
            AddPropertiesToList(properties, interfaceImplement);

            // Create accessors for each property
            foreach (var pi in properties)
            {
                string piName = pi.Name;
                var propertyType = pi.PropertyType;

                // Create underlying field; all properties have a field of the same type
                var field = typeBuilder.DefineField(
                    "_" + piName, propertyType, FieldAttributes.Private);

                // If there is a getter in the interface, create a getter in the new type
                var getMethod = pi.GetGetMethod();
                if (null != getMethod)
                {
                    // This will prevent us from creating a default method for the property's 
                    // getter
                    methods.Remove(getMethod);

                    // Now we will generate the getter method
                    var methodBuilder = typeBuilder.DefineMethod(
                        getMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        propertyType,
                        Type.EmptyTypes);

                    // The ILGenerator class is used to put op-codes (similar to assembly) into the
                    // method
                    il = methodBuilder.GetILGenerator();

                    // These are the op-codes, (similar to assembly)
                    il.Emit(OpCodes.Ldarg_0);      // Load "this"
                    il.Emit(OpCodes.Ldfld, field); // Load the property's underlying field onto the stack
                    il.Emit(OpCodes.Ret);          // Return the value on the stack

                    // We need to associate our new type's method with the getter method in the 
                    // interface
                    typeBuilder.DefineMethodOverride(methodBuilder, getMethod);
                }

                // If there is a setter in the interface, create a setter in the new type
                var setMethod = pi.GetSetMethod();
                if (null != setMethod)
                {
                    // This will prevent us from creating a default method for the property's 
                    // setter
                    methods.Remove(setMethod);

                    // Now we will generate the setter method
                    var methodBuilder = typeBuilder.DefineMethod(
                        setMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        typeof(void),
                        new Type[] { pi.PropertyType });

                    // The ILGenerator class is used to put op-codes (similar to assembly) into the
                    // method
                    il = methodBuilder.GetILGenerator();

                    // These are the op-codes, (similar to assembly)
                    il.Emit(OpCodes.Ldarg_0);      // Load "this"
                    il.Emit(OpCodes.Ldarg_1);      // Load "value" onto the stack
                    il.Emit(OpCodes.Stfld, field); // Set the field equal to the "value" 
                                                            // on the stack
                    il.Emit(OpCodes.Ret);          // Return nothing

                    // We need to associate our new type's method with the setter method in the 
                    // interface
                    typeBuilder.DefineMethodOverride(methodBuilder, setMethod);
                }
            }

            // Create default methods.  These methods will essentially be no-ops; if there is a 
            // return value, they will either return a default value or null
            var objectField = baseType.GetTypeInfo().GetDeclaredField("instance");

            foreach (var methodInfo in methods)
            {
                // Get the return type and argument types

                var returnType = methodInfo.ReturnType;

                var argumentTypes = new List<Type>();
                foreach (var parameterInfo in methodInfo.GetParameters())
                    argumentTypes.Add(parameterInfo.ParameterType);

                // Define the method
                var methodBuilder = typeBuilder.DefineMethod(
                    methodInfo.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    returnType,
                    argumentTypes.ToArray());

                // call DelegateFunc or DelegateAction
                var delegateMethod = default(MethodInfo);
                if (methodInfo.ReturnType == typeof(void))
                {
                    delegateMethod = baseType.GetTypeInfo().GetDeclaredMethod("DelegateAction");
                }
                else
                {
                    delegateMethod = baseType.GetTypeInfo().GetDeclaredMethod("DelegateFunc");
                }

                // The ILGenerator class is used to put op-codes (similar to assembly) into the
                // method
                il = methodBuilder.GetILGenerator();

                var ps = methodInfo.GetParameters();
                var paramTypes = new Type[ps.Length];
                for (var i = 0; i < paramTypes.Length; i++)
                {
                    if (ps[i].ParameterType.IsByRef)
                        paramTypes[i] = ps[i].ParameterType.GetElementType();
                    else
                        paramTypes[i] = ps[i].ParameterType;
                }

                //var locals = new LocalBuilder[paramTypes.Length];
                //for (var i = 0; i < paramTypes.Length; i++)
                //{
                //    locals[i] = il.DeclareLocal(paramTypes[i], true);
                //}

                //for (var i = 0; i < paramTypes.Length; i++)
                //{
                //    il.Emit(OpCodes.Ldarg_1);
                //    EmitFastInt(il, i);
                //    il.Emit(OpCodes.Ldelem_Ref);
                //    EmitCastToReference(il, paramTypes[i]);
                //    il.Emit(OpCodes.Stloc, locals[i]);
                //}

                if (!methodInfo.IsStatic)
                {
                    il.Emit(OpCodes.Ldarg_0);
                }

                //for (int i = 0; i < paramTypes.Length; i++)
                //{
                //    if (ps[i].ParameterType.IsByRef)
                //        il.Emit(OpCodes.Ldloca_S, locals[i]);
                //    else
                //        il.Emit(OpCodes.Ldloc, locals[i]);
                //}
                //if (delegateMethod.IsStatic)
                //    il.EmitCall(OpCodes.Call, delegateMethod, null);
                //else
                //    il.EmitCall(OpCodes.Callvirt, delegateMethod, null);
                //if (delegateMethod.ReturnType == typeof(void))
                //    il.Emit(OpCodes.Ldnull);
                //else
                //    EmitBoxIfNeeded(il, delegateMethod.ReturnType);

                //for (int i = 0; i < paramTypes.Length; i++)
                //{
                //    if (ps[i].ParameterType.IsByRef)
                //    {
                //        il.Emit(OpCodes.Ldarg_1);
                //        EmitFastInt(il, i);
                //        il.Emit(OpCodes.Ldloc, locals[i]);
                //        if (locals[i].LocalType.IsValueType)
                //            il.Emit(OpCodes.Box, locals[i].LocalType);
                //        il.Emit(OpCodes.Stelem_Ref);
                //    }
                //}

                //il.Emit(OpCodes.Ret);

                il.WriteLine($"начало функции {methodInfo.Name}");

                //il.Emit(OpCodes.Ldstr, methodInfo.Name);
                //il.EmitCall(OpCodes.Call, delegateMethod, new[] { typeof(string) });
                //il.Emit(OpCodes.Ldarg_0);
                //il.Emit(OpCodes.Ldfld, objectField);

                il.WriteLine($"после вызова делегата {delegateMethod.Name}");

                /*
                var length = ps.Length;
                if (length > 0)
                {
                    // new array
                    EmitFastInt(il, length);
                    il.Emit(OpCodes.Newarr, typeof(object));

                    for (var i = 0; i < length; i++)
                    {
                        il.Emit(OpCodes.Dup);
                        EmitFastInt(il, i);             // add index array
                        il.Emit(OpCodes.Ldarg, i + 1);  // add object to i argument
                        var type = paramTypes[i];
                        EmitBoxIfNeeded(il, type);
                        il.Emit(OpCodes.Stelem_Ref);    // replace array element
                    }
                }

                var invoker = delegateMethod.ReturnType.GetTypeInfo().GetDeclaredMethod("Invoke");

                il.Emit(OpCodes.Callvirt, invoker);

                //// If there's a return type, create a default value or null to return
                if (invoker.ReturnType != typeof(void))
                {
                    LocalBuilder localBuilder = il.DeclareLocal(invoker.ReturnType);   // this declares the local object, 
                    il.Emit(OpCodes.Ldloc, localBuilder);           // load the value on the stack to 
                }
                */
                il.Emit(OpCodes.Ret);   // return

                //// We need to associate our new type's method with the method in the interface
                typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            }

            // Finally, after all the fields and methods are generated, create the type for use at
            // run-time
            return typeBuilder.CreateType();
            //InterfaceImplementations[interfaceType] = createdType;
        }

        /// <summary>
        /// Helper method to get all MethodInfo objects from an interface.  This recurses to all 
        /// sub-interfaces
        /// </summary>
        /// <param name="methods"></param>
        /// <param name="type"></param>
        private static void AddMethodsToList(List<MethodInfo> methods, Type type)
        {
            methods.AddRange(type.GetMethods());

            foreach (var subInterface in type.GetInterfaces())
                AddMethodsToList(methods, subInterface);
        }

        /// <summary>
        /// Helper method to get all PropertyInfo objects from an interface.  This recurses to all 
        /// sub-interfaces
        /// </summary>
        /// <param name="methods"></param>
        /// <param name="type"></param>
        private static void AddPropertiesToList(List<PropertyInfo> properties, Type type)
        {
            properties.AddRange(type.GetProperties());

            foreach (var subInterface in type.GetInterfaces())
                AddPropertiesToList(properties, subInterface);
        }

        /// <summary>
        /// Thrown when an attempt is made to create an object of a type that is not an interface
        /// </summary>
        public class TypeIsNotAnInterface : Exception
        {
            internal TypeIsNotAnInterface(Type type)
                : base("The InterfaceObjectFactory only works with interfaces.  "
                    + "An attempt was made to create an object for the following type, "
                    + "which is not an interface: " + type.FullName)
            { }
        }
    }
}
