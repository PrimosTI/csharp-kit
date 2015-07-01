using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace CSharpKit
{
    //todo: controle de memória para moduleBuilder
    public static class DynamicFactory
    {
        public interface IDynamic { }

        private static ModuleBuilder moduleBuilder = Thread.GetDomain().DefineDynamicAssembly(
            new AssemblyName("DynamicAssembly"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("DynamicModule");

        private static long classCounter = 0;

        private static string UniqueClassName()
        {
            return "DynamicClass_" + (classCounter++).ToString();
        }

        private static object[] FromArray(object[] data)
        {
            Type dictionaryType = typeof(IDictionary<string, object>);
            Type arrayType = typeof(object[]);

            object[] ret = new object[data.Length];

            for (int i = 0, c = data.Length; i < c; i++)
            {
                object value = data[i];

                if (dictionaryType.IsInstanceOfType(value))
                    value = FromDictionary((IDictionary<string, object>)value);

                else if (arrayType.IsInstanceOfType(value))
                    value = FromArray((object[])value);

                ret[i] = value;
            }

            return ret;
        }

        private static object FromDictionary(IDictionary<string, object> data)
        {
            Type dictionaryType = typeof(IDictionary<string, object>);
            Type arrayType = typeof(object[]);

            List<string> keys = new List<string>(data.Keys);
            Dictionary<string, object> dicSubobjects = new Dictionary<string, object>();

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                    UniqueClassName(),
                    TypeAttributes.Public | TypeAttributes.Class,
                    (Type)null, new Type[] { typeof(IDynamic) });

            foreach (KeyValuePair<string, object> dataPair in data)
            {
                string fieldName = dataPair.Key;
                Type dataType = dataPair.Value == null ? typeof(object) : dataPair.Value.GetType();

                if (dictionaryType.IsInstanceOfType(dataPair.Value))
                {
                    object dynamicObj = FromDictionary((IDictionary<string, object>)dataPair.Value);
                    dicSubobjects[dataPair.Key] = dynamicObj;
                    dataType = dynamicObj.GetType();
                }
                else if (arrayType.IsInstanceOfType(dataPair.Value))
                {
                    dicSubobjects[dataPair.Key] = FromArray((object[])dataPair.Value);
                    dataType = arrayType;
                }

                FieldBuilder fieldBuilder = typeBuilder.DefineField(
                    fieldName, dataType, FieldAttributes.Private);

                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                    dataPair.Key, PropertyAttributes.None, dataType, new Type[] { dataType });

                MethodBuilder propertyGetMethodBuilder = typeBuilder.DefineMethod(
                    "get_" + fieldName, MethodAttributes.Public | MethodAttributes.HideBySig, dataType, Type.EmptyTypes);

                ILGenerator ilGetMethod = propertyGetMethodBuilder.GetILGenerator();
                ilGetMethod.Emit(OpCodes.Ldarg_0);
                ilGetMethod.Emit(OpCodes.Ldfld, fieldBuilder);
                ilGetMethod.Emit(OpCodes.Ret);

                MethodBuilder propertySetMethodBuilder = typeBuilder.DefineMethod(
                    "set_" + fieldName, MethodAttributes.Public | MethodAttributes.HideBySig, null, new Type[] { dataType });

                ILGenerator ilSetMethod = propertySetMethodBuilder.GetILGenerator();
                ilSetMethod.Emit(OpCodes.Ldarg_0);
                ilSetMethod.Emit(OpCodes.Ldarg_1);
                ilSetMethod.Emit(OpCodes.Stfld, fieldBuilder);
                ilSetMethod.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(propertyGetMethodBuilder);
                propertyBuilder.SetSetMethod(propertySetMethodBuilder);
            }

            Type type = typeBuilder.CreateType();
            object instance = type.GetConstructor(Type.EmptyTypes).Invoke(null);

            foreach (PropertyInfo property in type.GetProperties())
            {
                object value;
                if (!dicSubobjects.TryGetValue(property.Name, out value))
                    value = data[property.Name];

                property.SetValue(instance, value);
            }

            return instance;
        }

        public static object FromJSON(string json)
        {
            System.Web.Script.Serialization.JavaScriptSerializer serializer
                = new System.Web.Script.Serialization.JavaScriptSerializer();

            object jsonObj = serializer.DeserializeObject(json);

            if (jsonObj == null)
                return null;

            if (typeof(IDictionary<string, object>).IsInstanceOfType(jsonObj))
                return FromDictionary((IDictionary<string, object>)jsonObj);

            else if (typeof(object[]).IsInstanceOfType(jsonObj))
                return FromArray((object[])jsonObj);

            return jsonObj;
        }
    }
}
