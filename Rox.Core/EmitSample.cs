using System;
using System.Reflection;
using System.Reflection.Emit;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace Rox.Core
{
    public class EmitSample
    {

        public interface ISayHello
        {
            void SayHello();
        }

        public void Execute()
        {
            //
            // 0.これから作成する型を格納するアセンブリ名作成.
            //
            AssemblyName asmName = new AssemblyName { Name = "DynamicTypes" };
            //
            // 1.AssemlbyBuilderの生成
            //
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            //
            // 2.ModuleBuilderの生成.
            //
            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule("HelloWorld");
            //
            // 3.TypeBuilderの生成.
            //
            TypeBuilder typeBuilder = modBuilder.DefineType("SayHelloImpl", TypeAttributes.Public, typeof(object), new Type[] { typeof(ISayHello) });
            //
            // 4.MethodBuilderの生成
            //
            MethodAttributes methodAttr = (MethodAttributes.Public | MethodAttributes.Virtual);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("SayHello", methodAttr, typeof(void), new Type[] { });
            typeBuilder.DefineMethodOverride(methodBuilder, typeof(ISayHello).GetMethod("SayHello"));
            //
            // 5.ILGeneratorを生成し、ILコードを設定.
            //
            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldstr, "Hello World");
            il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            il.Emit(OpCodes.Ret);
            //
            // 6.作成した型を取得.
            //
            Type type = typeBuilder.CreateType();
            //
            // 7.型を具現化.
            //
            ISayHello hello = (ISayHello)Activator.CreateInstance(type);
            //
            // 8.実行.
            //
            hello.SayHello();
        }
    }




    public static class CodeGenerationHelper
    {
        public static void AddMethodDynamically(TypeBuilder myTypeBld,
                                    string mthdName,
                                    Type[] mthdParams,
                                    Type returnType,
                                    string mthdAction)
        {
            var myMthdBld = myTypeBld.DefineMethod(
                                                    mthdName,
                                                    MethodAttributes.Public |
                                                    MethodAttributes.Static,
                                                    returnType,
                                                    mthdParams);
            ILGenerator ILout = myMthdBld.GetILGenerator();
            int numParams = mthdParams.Length;
            for (byte x = 0; x < numParams; x++)
            {
                ILout.Emit(OpCodes.Ldarg_S, x);
            }
            if (numParams > 1)
            {
                for (int y = 0; y < (numParams - 1); y++)
                {
                    switch (mthdAction)
                    {
                        case "A":
                            ILout.Emit(OpCodes.Add);
                            break;
                        case "M":
                            ILout.Emit(OpCodes.Mul);
                            break;
                        default:
                            ILout.Emit(OpCodes.Add);
                            break;
                    }
                }
            }
            ILout.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates a dynamic class in memory from a hashtable. Each entry in the hashtable will become a field
        /// with a corresponding property. The class will implement INotifyPropertyChanged for each of the
        /// properties.
        /// </summary>
        /// <param name="namespaceName">The name of the namespace that will contain the dynamic class.</param>
        /// <param name="className">The name of the dynamic class.</param>
        /// <param name="source">The source hashtable that the members of the dynamic class will based off of.</param>
        /// <param name="useKeysForNaming">If set to True, the keys of the hashtable will be converted to strings
        /// and the field names and property names of the dynamic class will be based off of those strings. If
        /// set to false, the fields and properties will be named in sequential order.</param>
        /// <returns>Returns the C# code as a string.</returns>
        //public static string CreateClassFromHashtable(string namespaceName, string className, Hashtable source, bool useKeysForNaming)
        //{
        //    // Create compile unit.
        //    var compileUnit = new CodeCompileUnit();

        //    // Create namespace.
        //    var dynamicNamespace = new CodeNamespace(namespaceName);
        //    dynamicNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));

        //    // Create class.
        //    var dynamicClass = new CodeTypeDeclaration(className)
        //    {
        //        IsClass = true,
        //    };
        //    //dynamicClass.BaseTypes.Add(new CodeTypeReference("System.ComponentModel.INotifyPropertyChanged"));

        //    // Create PropertyChanged event; implement INotifyPropertyChanged.
        //    //var propertyChangedEvent = new CodeMemberEvent()
        //    //{
        //    //    Name = "PropertyChanged",
        //    //    Type = new CodeTypeReference("System.ComponentModel.PropertyChangedEventHandler"),
        //    //    Attributes = MemberAttributes.Public,
        //    //};
        //    //dynamicClass.Members.Add(propertyChangedEvent);

        //    foreach (object key in source.Keys)
        //    {
        //        // Construct field and property names.
        //        var fieldName = string.Format("_{0}", key.ToString());
        //        var propertyName = key.ToString();

        //        // Create field.
        //        var dynamicField = new CodeMemberField(source[key].GetType(), fieldName)
        //        {
        //            InitExpression = new CodePrimitiveExpression(source[key])
        //        };
        //        dynamicClass.Members.Add(dynamicField);

        //        var setPropMthdBldr = tb.DefineMethod("set_" + propertyName,
        //          MethodAttributes.Public |
        //          MethodAttributes.SpecialName |
        //          MethodAttributes.HideBySig,
        //          null, new[] { propertyType });

        //        // Create property.
        //        var dynamicProperty = new CodeMemberProperty
        //        {
        //            Name = key.ToString(),
        //            Type = new CodeTypeReference(source[key].GetType())
        //        };

        //        // Create property - get statements.
        //        dynamicProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)));

        //        // Create property - set statements.
        //        // Assign value to field.
        //        dynamicProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodePropertySetValueReferenceExpression()));

        //        // Call PropertyChanged event.
        //        // Create target object reference.
        //        //var propertyChangedTargetObject = new CodeEventReferenceExpression(new CodeThisReferenceExpression(), "PropertyChanged");

        //        // Create parameters array.
        //        //var propertyChangedParameters = new CodeExpression[]
        //        //{
        //        //    new CodeThisReferenceExpression(),
        //        //    new CodeObjectCreateExpression("System.ComponentModel.PropertyChangedEventArgs",
        //        //    new CodeExpression[] { new CodePrimitiveExpression(propertyName) })
        //        //};

        //        // Create delegate invoke expression and add it to the property's set statements; call PropertyChanged.
        //        //var invokePropertyChanged = new CodeDelegateInvokeExpression(propertyChangedTargetObject, propertyChangedParameters);
        //        //dynamicProperty.SetStatements.Add(invokePropertyChanged);

        //        // Add property to class.
        //        dynamicClass.Members.Add(dynamicProperty);
        //    }

        //    // Add class to namespace.
        //    dynamicNamespace.Types.Add(dynamicClass);

        //    // Add namespace to compile unit.
        //    compileUnit.Namespaces.Add(dynamicNamespace);

        //    // Generate CSharp code from compile unit.
        //    var stringWriter = new StringWriter();
        //    var provider = CodeDomProvider.CreateProvider("CSharp");
        //    provider.GenerateCodeFromCompileUnit(compileUnit, stringWriter, new CodeGeneratorOptions());
        //    stringWriter.Close();
        //    return stringWriter.ToString();
        //}

        public static object InstantiateClassFromCodeString(string codeString, string fullyQualifiedTypeName)
        {
            var compiler = new CSharpCodeProvider();
            var compilerParams = new CompilerParameters(new string[] { "System.dll" });
            var results = compiler.CompileAssemblyFromSource(compilerParams, new string[] { codeString });
            return results.CompiledAssembly.CreateInstance(fullyQualifiedTypeName);
        }
    }

    //public static class MyTypeBuilder
    //{
    //    public static void CreateNewObject()
    //    {
    //        var myType = CompileResultType();
    //        var myObject = Activator.CreateInstance(myType);
    //    }
    //    public static Type CompileResultType()
    //    {
    //        TypeBuilder tb = GetTypeBuilder();
    //        ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

    //        // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
    //        foreach (var field in yourListOfFields)
    //            CreateProperty(tb, field.FieldName, field.FieldType);

    //        Type objectType = tb.CreateType();
    //        return objectType;
    //    }

    //    private static TypeBuilder GetTypeBuilder()
    //    {
    //        var typeSignature = "MyDynamicType";
    //        var an = new AssemblyName(typeSignature);
    //        AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
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
    //                MethodAttributes.Public |
    //                MethodAttributes.SpecialName |
    //                MethodAttributes.HideBySig,
    //                null, new[] { propertyType });

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
}
