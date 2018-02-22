using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


namespace TCB.UBank.Core.Helpers
{
    public class ReflectionAnalyser
    {
        public static Dictionary<string, object> GetPropertiesDictionary(object ob)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            Type type = ob.GetType();
            PropertyInfo[] properties = RuntimeReflectionExtensions.GetRuntimeProperties(type).ToArray();
            for (int i = 0; i < properties.Length; i++)
            {
                result.Add(properties[i].Name, properties[i].GetValue(ob));
            }
            return result;
        }

        public static Dictionary<string, object> GetDictionaryFields(object ob)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            Type type = ob.GetType();
            FieldInfo[] fields = RuntimeReflectionExtensions.GetRuntimeFields(type).ToArray();
            for (int i = 0; i < fields.Length; i++)
            {
                result.Add(fields[i].Name, fields[i].GetValue(ob));
            }
            return result;
        }

        public static object GetDictionaryFieldsAndProperties(object ob, int deep = 0, List<string> ignoreElements = null)
        {
            Type[] baseTypes = { typeof(string), typeof(byte), typeof(int), typeof(double), typeof(long), typeof(decimal), typeof(bool), typeof(char), typeof(float), typeof(short) };
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (ob == null) return ob;
            Type type = ob.GetType();
            if (baseTypes.Contains(type) || ob is ICollection) return ob;
            FieldInfo[] fields = RuntimeReflectionExtensions.GetRuntimeFields(type).ToArray();
            PropertyInfo[] properties = RuntimeReflectionExtensions.GetRuntimeProperties(type).ToArray();
            int errInd = 0;
            bool isField = true;
            try
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (ignoreElements != null && ignoreElements.Contains(fields[i].Name)) continue;
                    errInd = i;
                    if (fields[i] != null)
                        if (deep == 0)
                            result.Add(fields[i].Name, fields[i].GetValue(ob));
                        else if (deep > 0)
                            result.Add(fields[i].Name, GetDictionaryFieldsAndProperties(fields[i].GetValue(ob), deep - 1));
                }

                isField = false;
                for (int i = 0; i < properties.Length; i++)
                {
                    if (ignoreElements != null && ignoreElements.Contains(properties[i].Name)) continue;
                    errInd = i;
                    if (properties[i] != null && properties[i].Name != "Item")
                        if (deep == 0)
                            result.Add(properties[i].Name, properties[i].GetValue(ob));

                        else if (deep > 0)
                            result.Add(properties[i].Name, GetDictionaryFieldsAndProperties(properties[i].GetValue(ob), deep - 1));
                }
            }
            catch (Exception e)
            {
                string key = isField ? "field" : "property";
                e.Data.Add(key, errInd);
                if (properties[errInd].Name == "Item") result.Add(properties[errInd].Name, null);
                else throw e;
            }
            return result;
        }
        public static object GetDictionaryFieldsAndProperties(object ob, List<Type> ignoreTypes, int deep = 0)
        {
            Type[] baseTypes = { typeof(string), typeof(byte), typeof(int), typeof(double), typeof(long), typeof(decimal), typeof(bool), typeof(char), typeof(float), typeof(short) };
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (ob == null) return ob;
            Type type = ob.GetType();
            if (baseTypes.Contains(type) || ob is ICollection) return ob;
            List<FieldInfo> fields = RuntimeReflectionExtensions.GetRuntimeFields(type).ToList();
            List<PropertyInfo> properties = RuntimeReflectionExtensions.GetRuntimeProperties(type).ToList();
            int errInd = 0;
            bool isField = true;
            try
            {
                if (ignoreTypes == null) ignoreTypes = new List<Type>();
                fields.RemoveAll(x =>
                {
                    return (ignoreTypes.Contains(x.FieldType));
                });
                foreach (var field in fields)
                {
                    errInd++;
                    if (field != null)
                        if (deep == 0)
                            result.Add(field.Name, field.GetValue(ob));
                        else if (deep > 0)
                            result.Add(field.Name, GetDictionaryFieldsAndProperties(field.GetValue(ob), ignoreTypes, deep - 1));

                }

                isField = false;
                errInd = 0;
                properties.RemoveAll(x => { return (ignoreTypes.Contains(x.PropertyType)); });
                foreach (var property in properties)
                {
                    errInd++;
                    if (property != null && property.Name != "Item")
                        if (deep == 0)
                            result.Add(property.Name, property.GetValue(ob));

                        else if (deep > 0)
                            result.Add(property.Name, GetDictionaryFieldsAndProperties(property.GetValue(ob), ignoreTypes, deep - 1));
                }
            }
            catch (Exception e)
            {
                string key;
                MemberInfo info;
                if (isField)
                {
                    key = "field";
                    info = fields[errInd];
                }
                else
                {
                    key = "property";
                    info = properties[errInd];
                }

                e.Data.Add(key, errInd);
                if (properties[errInd].Name == "Item") result.Add(properties[errInd].Name, null);
                else throw new Exception($"{key} {info.Name} on {deep} level", e);
            }
            return result;
        }
        public static T ReadFromJsonString<T>(string jsonString)
        {
            T result = JsonConvert.DeserializeObject<T>(jsonString);
            return result;
        }

        public static string DictionaryToJsonString(IDictionary<string, object> value)
        {
            string result =
                JsonConvert.SerializeObject(value, new JsonSerializerSettings()
                {
                    Error = (serializer, err) => { err.ErrorContext.Handled = true; }
                });
            return result;
        }

        public static string ToJsonString(object ob)
        {
            string result;
            try
            {
                result = JsonConvert.SerializeObject(ob);
            }
            catch (Exception e)
            {
                result = e.Message;
            }
             
            return result;
        }
    }
}