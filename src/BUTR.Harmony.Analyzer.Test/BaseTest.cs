namespace BUTR.Harmony.Analyzer.Test
{
    public class BaseTest
    {
        protected static readonly string HarmonyBase = @"
namespace HarmonyLib
{
    using System;
    using System.Reflection;

    // API declared in Harmony/HarmonyX
	public enum MethodType { Normal, Getter, Setter, Constructor, StaticConstructor }

    // API declared in Harmony/HarmonyX
	public enum ArgumentType { Normal, Ref, Out, Pointer }

    // API declared in Harmony/HarmonyX
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Method, AllowMultiple = true)]
	public class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type declaringType) { }
        public HarmonyPatch(Type declaringType, Type[] argumentTypes) { }
        public HarmonyPatch(Type declaringType, string methodName) { }
        public HarmonyPatch(Type declaringType, string methodName, params Type[] argumentTypes) { }
        public HarmonyPatch(Type declaringType, string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations) { }
        public HarmonyPatch(Type declaringType, MethodType methodType) { }
        public HarmonyPatch(Type declaringType, MethodType methodType, params Type[] argumentTypes) { }
        public HarmonyPatch(Type declaringType, MethodType methodType, Type[] argumentTypes, ArgumentType[] argumentVariations) { }
        public HarmonyPatch(Type declaringType, string methodName, MethodType methodType) { }
        public HarmonyPatch(string methodName) { }
        public HarmonyPatch(string methodName, params Type[] argumentTypes) { }
        public HarmonyPatch(string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations) { }
        public HarmonyPatch(string methodName, MethodType methodType) { }
        public HarmonyPatch(MethodType methodType, params Type[] argumentTypes) { }
        public HarmonyPatch(MethodType methodType, Type[] argumentTypes, ArgumentType[] argumentVariations) { }
        public HarmonyPatch(Type[] argumentTypes) { }
        public HarmonyPatch(Type[] argumentTypes, ArgumentType[] argumentVariations) { }
    }

    // API declared in Harmony/HarmonyX and Hamony.Extensions
    public class AccessTools
    {
        public static object DeclaredConstructor(Type type, Type[]? parameters = null, bool searchForStatic = false) => null;
        public static object Constructor(Type type, Type[]? parameters = null, bool searchForStatic = false) => null;

        public static object DeclaredConstructor(string typeString, Type[]? parameters = null, bool searchForStatic = false) => null;
        public static object Constructor(string typeString, Type[]? parameters = null, bool searchForStatic = false) => null;


        public static object DeclaredField(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object Field(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;

        public static object DeclaredField(string typeColonFieldname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object Field(string typeColonFieldname, Type[]? parameters = null, Type[]? generics = null) => null;


        public static object DeclaredProperty(Type type, string name) => null;
        public static object Property(Type type, string name) => null;

        public static object DeclaredPropertyGetter(Type type, string name) => null;
        public static object DeclaredPropertySetter(Type type, string name) => null;
        public static object PropertyGetter(Type type, string name) => null;
        public static object PropertySetter(Type type, string name) => null;

        public static object DeclaredProperty(string typeColonPropertyName) => null;
        public static object Property(string typeColonPropertyName) => null;

        public static object DeclaredPropertySetter(string typeColonPropertyName) => null;
        public static object DeclaredPropertyGetter(string typeColonPropertyName) => null;
        public static object PropertyGetter(string typeColonPropertyName) => null;
        public static object PropertySetter(string typeColonPropertyName) => null;


        public static object DeclaredMethod(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object Method(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;

        public static object Method(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredMethod(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;


        public static object GetDeclaredConstructorDelegate<TDelegate>(Type type, Type[]? parameters = null) where TDelegate : Delegate => null;
        public static object GetConstructorDelegate<TDelegate>(Type type, Type[]? parameters = null) where TDelegate : Delegate => null;

        public static object GetDeclaredConstructorDelegate<TDelegate>(string typeString, Type[]? parameters = null) where TDelegate : Delegate => null;
        public static object GetConstructorDelegate<TDelegate>(string typeString, Type[]? parameters = null) where TDelegate : Delegate => null;


        public static object GetPropertyGetterDelegate<TDelegate>(PropertyInfo propertyInfo) where TDelegate : Delegate => null;
        public static object GetPropertySetterDelegate<TDelegate>(PropertyInfo propertyInfo) where TDelegate : Delegate => null;
        
        public static object GetPropertyGetterDelegate<TDelegate>(object? instance, PropertyInfo propertyInfo) where TDelegate : Delegate => null;
        public static object GetPropertySetterDelegate<TDelegate>(object? instance, PropertyInfo propertyInfo) where TDelegate : Delegate => null;

        public static object GetDeclaredPropertyGetterDelegate<TDelegate>(Type type, string name) where TDelegate : Delegate => null;
        public static object GetDeclaredPropertySetterDelegate<TDelegate>(Type type, string name) where TDelegate : Delegate => null;
        public static object GetPropertyGetterDelegate<TDelegate>(Type type, string name) where TDelegate : Delegate => null;
        public static object GetPropertySetterDelegate<TDelegate>(Type type, string name) where TDelegate : Delegate => null;

        public static object GetDeclaredPropertyGetterDelegate<TDelegate>(object? instance, Type type, string method) where TDelegate : Delegate => null;
        public static object GetDeclaredPropertySetterDelegate<TDelegate>(object? instance, Type type, string method) where TDelegate : Delegate => null;
        public static object GetPropertyGetterDelegate<TDelegate>(object? instance, Type type, string method) where TDelegate : Delegate => null;
        public static object GetPropertySetterDelegate<TDelegate>(object? instance, Type type, string method) where TDelegate : Delegate => null;
        
        public static object GetDeclaredPropertyGetterDelegate<TDelegate>(string typeColonPropertyName) where TDelegate : Delegate => null;
        public static object GetDeclaredPropertySetterDelegate<TDelegate>(string typeColonPropertyName) where TDelegate : Delegate => null;
        public static object GetPropertyGetterDelegate<TDelegate>(string typeColonPropertyName) where TDelegate : Delegate => null;
        public static object GetPropertySetterDelegate<TDelegate>(string typeColonPropertyName) where TDelegate : Delegate => null;

        public static object GetDeclaredPropertyGetterDelegate<TDelegate>(object? instance, string typeColonPropertyName) where TDelegate : Delegate => null;
        public static object GetDeclaredPropertySetterDelegate<TDelegate>(object? instance, string typeColonPropertyName) where TDelegate : Delegate => null;
        public static object GetPropertyGetterDelegate<TDelegate>(object? instance, string typeColonPropertyName) where TDelegate : Delegate => null;
        public static object GetPropertySetterDelegate<TDelegate>(object? instance, string typeColonPropertyName) where TDelegate : Delegate => null;


        public static object GetDelegate<TDelegate, TInstance>(TInstance instance, MethodInfo methodInfo) where TDelegate : Delegate => null;
        
        public static object GetDeclaredDelegateObjectInstance<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDelegateObjectInstance<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDeclaredDelegateObjectInstance<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDelegateObjectInstance<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;

        public static object GetDeclaredDelegate<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDelegate<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDeclaredDelegate<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDelegate<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        
        public static object GetDeclaredDelegate<TDelegate, TInstance>(TInstance instance, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDelegate<TDelegate, TInstance>(TInstance instance, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
   
        public static object GetDeclaredDelegate<TDelegate>(object? instance, Type type, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDelegate<TDelegate>(object? instance, Type type, string method, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
    
        public static object GetDeclaredDelegate<TDelegate>(object? instance, string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;
        public static object GetDelegate<TDelegate>(object? instance, string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null) where TDelegate : Delegate => null;


        public static object FieldRefAccess<T, F>(string fieldName) => null;
        public static object FieldRefAccess<F>(string typeColonFieldname) => null;

        public static object StaticFieldRefAccess<T, F>(string fieldName) => null;
        public static object StaticFieldRefAccess<F>(string typeColonFieldname) => null;

        public static object StructFieldRefAccess<T, F>(string fieldName) where T: struct => null;
        public static object StructFieldRefAccess<F>(string typeColonFieldname) => null;
    }
}
";
    }
}