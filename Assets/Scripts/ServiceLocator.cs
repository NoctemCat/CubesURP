using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator
{
    private ServiceLocator() { }

    /// <summary>
    /// currently registered services.
    /// </summary>
    private readonly Dictionary<string, object> services = new();

    /// <summary>
    /// Gets the currently active service locator instance.
    /// </summary>
    public static ServiceLocator Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Initialization()
    {
        Instance = new ServiceLocator();
    }


    //public T Get<T>()
    //{
    //    string key = typeof(T).Name;
    //    if (!services.ContainsKey(key))
    //    {
    //        Debug.LogError($"{key} not registered with ServiceLocator");
    //        throw new InvalidOperationException();
    //    }

    //    return (T)services[key];
    //}

    /// <summary>
    /// Gets the service instance of the given type.
    /// </summary>
    /// <typeparam name="T">The type of the service to lookup.</typeparam>
    /// <returns>The service instance.</returns>
    public static T Get<T>()
    {
        //Debug.Log(Instance is null);
        string key = typeof(T).Name;
        if (!Instance.services.ContainsKey(key))
        {
            Debug.LogError($"{key} not registered with ServiceLocator");
            throw new InvalidOperationException();
        }

        return (T)Instance.services[key];
    }

    /// <summary>
    /// Registers the service with the current service locator.
    /// </summary>
    /// <typeparam name="T">Service type.</typeparam>
    /// <param name="service">Service instance.</param>
    public static void Register<T>(T service)
    {
        string key = typeof(T).Name;
        if (Instance.services.ContainsKey(key))
        {
            Debug.LogError($"Attempted to register service of type {key} which is already registered with the ServiceLocator.");
            return;
        }

        Instance.services.Add(key, service);
    }

    /// <summary>
    /// Unregisters the service from the current service locator.
    /// </summary>
    /// <typeparam name="T">Service type.</typeparam>
    public static void Unregister<T>()
    {
        string key = typeof(T).Name;
        if (!Instance.services.ContainsKey(key))
        {
            Debug.LogError($"Attempted to unregister service of type {key} which is not registered with the ServiceLocator.");
            return;
        }

        Instance.services.Remove(key);
    }
    public static void Unregister<T>(T service) => Unregister<T>();
}