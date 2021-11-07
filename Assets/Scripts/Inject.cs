using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[AttributeUsage(validOn: AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute
{ }

public static class MemberInfoExtension
{
    public static Attribute GetCustomAttributeEx<T1, T2>(this MemberInfo info, bool inherit)
    {
        if(typeof(T1) == typeof(PropertyInfo))
            return ((PropertyInfo)info)?.GetCustomAttribute(typeof(T2), inherit);
        if(typeof(T1) == typeof(FieldInfo))
            return ((FieldInfo)info)?.GetCustomAttribute(typeof(T2), inherit);
        return default;
    }
    public static object GetValue<T>(this MemberInfo info, object obj)
    {
        if(typeof(T) == typeof(PropertyInfo))
            return ((PropertyInfo)info)?.GetValue(obj);
        if(typeof(T) == typeof(FieldInfo))
            return ((FieldInfo)info)?.GetValue(obj);
        return default;
    }
    public static void SetValue<T>(this MemberInfo info, object obj, object value)
    {
        if(typeof(T) == typeof(PropertyInfo))
            ((PropertyInfo)info)?.SetValue(obj, value);
        if(typeof(T) == typeof(FieldInfo))
            ((FieldInfo)info)?.SetValue(obj, value);
    }
    public static string ToStringType<T>(this MemberInfo info)
    {
        if(typeof(T) == typeof(PropertyInfo))
            return ((PropertyInfo)info)?.PropertyType.Name;
        if(typeof(T) == typeof(FieldInfo))
            return ((FieldInfo)info)?.FieldType.Name;
        return default;
    }
}

public class Inject : MonoBehaviour
{
    [SerializeField] private List<MonoScript> _classes;
    private Dictionary<Type, object> _instances;

    private object GetInstanceObject(Assembly assembly, Type type)
    {
        object result = null;

        if (type.BaseType == typeof(MonoBehaviour))
        {
            if (!_instances.ContainsKey(type))
            {
                result = FindObjectOfType(type);
                if(result == null)
                    result = new GameObject($"[{type.Name}]").AddComponent(type);
                _instances.Add(type, result);
            }
            else result = _instances[type];
        }
        else
        {
            if (!_instances.ContainsKey(type))
            {
                result = assembly.CreateInstance(type.FullName);
                _instances.Add(type, result);
            }
            else  result = _instances[type];
        }
        
        return result;
    }
    
    private void InjectMembers<T>(Assembly assembly, Type parent, MemberInfo[] fields)
    {
        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<InjectAttribute>(true) == null) continue;

            var obj = GetInstanceObject(assembly, parent);
            if (obj == null) continue;
            
            if (field.GetValue<T>(obj) != null) continue;
            
            foreach (var cl in _classes)
            {
                if (cl.GetClass().GetInterface(field.ToStringType<T>()) != null)
                {
                    if (!_instances.ContainsKey(cl.GetClass()))
                    {
                        var instance = assembly.CreateInstance(cl.GetClass().FullName);
                        _instances.Add(cl.GetClass(), instance);
                        field.SetValue<T>(obj, instance);
                    }
                    else field.SetValue<T>(obj, _instances[cl.GetClass()]);
                }
            }
        }
    }

    private void Awake()
    {
        _instances = new Dictionary<Type, object>();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        foreach (var attr in assembly.GetTypes())
        {
            InjectMembers<FieldInfo>(assembly, attr, attr.GetFields());
            InjectMembers<PropertyInfo>(assembly, attr, attr.GetProperties());
        }
    }
}