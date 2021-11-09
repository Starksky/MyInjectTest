using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[AttributeUsage(validOn: AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute
{ }
[AttributeUsage(validOn: AttributeTargets.Field | AttributeTargets.Property)]
public class InjectNewAttribute : Attribute
{ }

public static class MemberInfoExtension
{
    public static object GetValue(this MemberInfo info, object obj)
    {
        if((info.MemberType & MemberTypes.Property) != 0)
            return ((PropertyInfo)info)?.GetValue(obj);
        if((info.MemberType & MemberTypes.Field) != 0)
            return ((FieldInfo)info)?.GetValue(obj);
        return default;
    }
    public static void SetValue(this MemberInfo info, object obj, object value)
    {
        if((info.MemberType & MemberTypes.Property) != 0)
            ((PropertyInfo)info)?.SetValue(obj, value);
        if((info.MemberType & MemberTypes.Field) != 0)
            ((FieldInfo)info)?.SetValue(obj, value);
    }
    public static Type GetMemberType(this MemberInfo info)
    {
        if ((info.MemberType & MemberTypes.Property) != 0)
            return ((PropertyInfo) info)?.PropertyType;
        if ((info.MemberType & MemberTypes.Field) != 0)
            return ((FieldInfo) info)?.FieldType;
        return default;
    }
}

public class Inject : MonoBehaviour
{
    [SerializeField] private List<MonoScript> classes;
    
    private Assembly _assembly;
    private Dictionary<string, object> _instances;
    private int _indexNew = 0;
    
    private object InstantiateMono(Type type, int index = 0)
    {
        object result = null;
        string typeString = $"{type}{index}";
        
        if (!_instances.ContainsKey(typeString))
        {
            result = FindObjectOfType(type);
            if (result == null)
            {
                var go = new GameObject($"[{type.Name}]");
                go.SetActive(false); // Stop MonoBehaviour
                result = go.AddComponent(type);
            }
            if(result != null)
                _instances.Add(typeString, result);
        }
        else
            result = _instances[typeString]; 
        
        return result;
    }
    
    private object InstantiateDefault(Type type, int index = 0)
    {
        object result = null;
        string typeString = $"{type}{index}";
        
        if (!_instances.ContainsKey(typeString))
        {
            result = _assembly.CreateInstance(type.FullName);
            if(result != null)
                _instances.Add(typeString, result);
        }
        else  
            result = _instances[typeString];
        
        return result;
    }
    
    private object Instantiate<TAttribute>(Type type) where TAttribute : Attribute
    {
        int index = IsNew<TAttribute>() ? _indexNew++ : 0;
        if (type.BaseType == typeof(MonoBehaviour))
            return InstantiateMono(type, index);
        return InstantiateDefault(type, index);
    }

    private bool IsNew<TAttribute>() where TAttribute : Attribute
    {
        if (typeof(TAttribute) == typeof(InjectAttribute)) return false;
        return true;
    }

    private void Injecting<TAttribute>(Type type) where TAttribute : Attribute
    {
        if  (classes.All(c => c.GetClass().Name != type.Name))
            return;

        object instance = null;
        if(type.GetCustomAttribute(typeof(TAttribute), true) == null)
            instance = Instantiate<InjectAttribute>(type);
        else instance = Instantiate<TAttribute>(type);

        foreach (var member in type.GetFields())
        {
            Injecting<InjectAttribute>(member, instance, type);
            Injecting<InjectNewAttribute>(member, instance, type);
        }
           
        foreach (var member in type.GetProperties())
        {
            Injecting<InjectAttribute>(member, instance, type);
            Injecting<InjectNewAttribute>(member, instance, type);
        }
    }
    
    private void Injecting<TAttribute>(MemberInfo member, object parent, Type typeParent) where TAttribute : Attribute
    {
        Type type = member.GetMemberType();
        
        if (type == typeParent) 
            throw(new Exception($"{type.Name} refers to itself"));
        
        if (member.GetCustomAttribute(typeof(TAttribute), true) == null) return;

        int idClass = classes.FindIndex(c => c.GetClass().GetInterface(type.FullName) != null);
        if (idClass < 0) return;
        
        Type typeInject = classes[idClass].GetClass();
        
        var instance = IsNew<TAttribute>() ? null : member.GetValue(parent);
        if(instance == null)
        {
            instance = Instantiate<TAttribute>(typeInject);
            member.SetValue(parent, instance);
        }

        foreach (var m in typeInject.GetFields())
        {
            Injecting<InjectAttribute>(m, instance, typeInject);
            Injecting<InjectNewAttribute>(m, instance, typeInject);
        }

        foreach (var m in typeInject.GetProperties())
        {
            Injecting<InjectAttribute>(m, instance, typeInject);
            Injecting<InjectNewAttribute>(m, instance, typeInject);
        }
            
    }
    
    private void OnEnable()
    {
        _instances = new Dictionary<string, object>();
        _assembly = GetType().Assembly;
        
        foreach (var type in _assembly.GetTypes())
        {
            if (!type.IsClass) continue;
            Injecting<InjectAttribute>(type);
            Injecting<InjectNewAttribute>(type);
        }

        //Launching the created MonoBehaviours
        foreach (var instance in _instances)
        {
            if (instance.Value.GetType().BaseType != typeof(MonoBehaviour)) continue;
            MonoBehaviour mb = instance.Value as MonoBehaviour;
            if(mb) mb.gameObject.SetActive(true);
        }
    }
}