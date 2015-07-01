using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSharpKit
{
    public static class FilterExtension
    {
        internal delegate bool CompareValue<in T>(T item);
        internal delegate bool IterateAllTrue<in T>(T item);
        internal delegate bool IterateSomeoneTrue<in T>(T item);

        public static object Compare<T>(Func<T, bool> func)
        {
            return new CompareValue<T>(item =>
            {
                
                return func(item);
            });
        }

        public class Enumerable
        {
            public static object All<T>(Func<T, bool> func)
            {
                return new IterateAllTrue<T>(item =>
                {
                    return func(item);
                });
            }
            public static object All<T>(object paramsObject)
            {
                return new IterateAllTrue<T>(item =>
                {
                    return ValuesEquals(item, paramsObject);
                });
            }

            public static object Contains<T>(Func<T, bool> func)
            {
                return new IterateSomeoneTrue<T>(item =>
                {
                    return func(item);
                });
            }
            public static object Contains<T>(object paramsObject)
            {
                return new IterateSomeoneTrue<T>(item =>
                {
                    return ValuesEquals(item, paramsObject);
                });
            }
        }

        //todo: compor LINQ expressions 
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> data, object paramsObj)
        {
            if (paramsObj is Delegate)
            {
                Delegate function = (Delegate)paramsObj;
                Type functionType = function.GetType();

                if (functionType.IsGenericType)
                {
                    Type functionGenericType = functionType.GetGenericTypeDefinition();
                    Type functionParameterType = functionType.GetGenericArguments()[0];

                    // verifica a assinatura da função
                    if (functionGenericType == typeof(IterateAllTrue<>)
                        || functionGenericType == typeof(IterateSomeoneTrue<>))
                        if (functionParameterType.IsAssignableFrom(typeof(T)))
                            return data.Where(new Func<T, bool>(item =>
                            {
                                return (bool)function.Method.Invoke(null, new object[] { item });
                            }));
                        else
                            return System.Linq.Enumerable.Empty<T>();
                }
            }

            return data.Where(item =>
            {
                return ValuesEquals(item, paramsObj);
            });
        }

        private static bool ValuesEquals(object obj1, object obj2)
        {
            // testa por Compare<> 
            if (obj2 is Delegate)
            {
                Delegate function = (Delegate)obj2;
                Type functionType = function.GetType();

                if (functionType.IsGenericType)
                {
                    Type functionGenericType = functionType.GetGenericTypeDefinition();
                    Type functionParameterType = functionType.GetGenericArguments()[0];

                    // verifica a assinatura da função
                    if (functionGenericType == typeof(CompareValue<>))
                        if (functionParameterType.IsAssignableFrom(obj1.GetType()))
                            return (bool)function.Method.Invoke(null, new object[] { obj1 });
                        else
                            return false;
                }
            }

            // nulos
            if (obj1 == null)
                return obj2 == null;
            else if (obj2 == null)
                return false;

            Type obj1Type = obj1.GetType();
            Type obj2Type = obj2.GetType();

            if (obj1Type.IsEnum)
            {
                obj1 = System.Convert.ChangeType(obj1, Enum.GetUnderlyingType(obj1Type));
                obj1Type = Enum.GetUnderlyingType(obj1Type);
            }

            if (obj2Type.IsEnum)
            {
                obj2 = System.Convert.ChangeType(obj2, Enum.GetUnderlyingType(obj2Type));
                obj2Type = Enum.GetUnderlyingType(obj2Type);
            }

            // função Equals
            if (object.Equals(obj1, obj2))
                return true;

            if ((obj1Type.IsPrimitive))
            { // primitivos

                if (obj1 is bool || obj2 is bool
                    || obj1 is char || obj2 is char
                    || obj1 is IntPtr || obj2 is IntPtr
                    || obj1 is UIntPtr || obj2 is UIntPtr
                )
                    return false;

                if (obj1 is float || obj1 is double)
                    return ((bool)obj1Type.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { obj1Type }, null)
                        .Invoke(obj1, new object[] { obj2 }));

                if (obj2 is float || obj2 is double)
                    return ((bool)obj2Type.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { obj2Type }, null)
                        .Invoke(obj2, new object[] { obj1 }));

                try
                {
                    return obj1.Equals(System.Convert.ChangeType(obj2, obj1Type));
                }
                catch (OverflowException)
                {
                    try
                    {
                        return obj2.Equals(System.Convert.ChangeType(obj1, obj2Type));
                    }
                    catch { }
                }
            }
            else
            {
                // string
                if (obj1 is string || obj2 is string)
                    return false;

                //todo: decimal

                // IEnumerable
                if (obj1 is IEnumerable)
                    if (obj2 is System.Linq.Expressions.Expression)
                    {
                        // TODO
                    }
                    else if (obj2 is Delegate)
                    {
                        Type functionType = obj2Type;

                        if (functionType.IsGenericType)
                        {
                            Type functionGenericType = functionType.GetGenericTypeDefinition();
                            Type[] FunctionParameterTypes = functionType.GetGenericArguments();

                            if (FunctionParameterTypes.Length == 1)
                            {
                                Type functionParameterType = functionType.GetGenericArguments()[0];

                                // verifica a assinatura da função
                                if (functionGenericType == typeof(IterateAllTrue<>))
                                {
                                    foreach (object item in (IEnumerable)obj1)
                                    {
                                        if (!functionParameterType.IsAssignableFrom(item.GetType()) // verifica se a Delegate é compatível com o item
                                            || !(bool)((Delegate)obj2).DynamicInvoke(item))
                                            return false;
                                    }
                                    return true;
                                }
                                else if (functionGenericType == typeof(IterateSomeoneTrue<>))
                                {
                                    foreach (object item in (IEnumerable)obj1)
                                    {
                                        if (functionParameterType.IsAssignableFrom(item.GetType()) // verifica se a Delegate é compatível com o item
                                            && (bool)((Delegate)obj2).DynamicInvoke(item))
                                            return true;
                                    }
                                    return false;
                                }
                            }
                        }
                    }

                // objetos
                if (!obj2Type.IsPrimitive)
                    return ComplexEquals(obj1, obj2);
            }

            return false;
        }

        private static bool ComplexEquals(object obj1, object obj2)
        {
            System.Type obj1Type = obj1.GetType();

            foreach (System.Reflection.PropertyInfo obj2Property in obj2.GetType().GetProperties())
            {
                System.Reflection.PropertyInfo obj1Property = obj1Type.GetProperty(obj2Property.Name);

                if (obj1Property == null)
                    return false;

                object obj1Value = obj1Property.GetValue(obj1);
                object obj2Value = obj2Property.GetValue(obj2);

                if (!ValuesEquals(obj1Value, obj2Value))
                    return false;
            }

            return true;
        }
    }
}
