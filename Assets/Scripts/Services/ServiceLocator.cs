using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator
{
    private ServiceLocator() { }

    /// <summary>
    /// currently registered services.
    /// </summary>
    private readonly Dictionary<Type, object> services = new();

    /// <summary>
    /// Gets the currently active service locator instance.
    /// </summary>
    public static ServiceLocator Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Initialization()
    {
        Instance = new ServiceLocator();
    }

    /// <summary>
    /// Gets the service instance of the given type.
    /// </summary>
    /// <typeparam name="T">The type of the service to lookup.</typeparam>
    /// <returns>The service instance.</returns>
    public static T Get<T>()
    {
        if (!Instance.services.ContainsKey(typeof(T)))
        {
            Debug.LogError($"{typeof(T).Name} not registered with ServiceLocator");
            throw new InvalidOperationException();
        }

        return (T)Instance.services[typeof(T)];
    }

    /// <summary>
    /// Registers the service with the current service locator.
    /// </summary>
    /// <typeparam name="T">Service type.</typeparam>
    /// <param name="service">Service instance.</param>
    public static void Register<T>(T service)
    {
        if (Instance.services.ContainsKey(typeof(T)))
        {
            Debug.LogError($"Attempted to register service of type {typeof(T).Name} which is already registered with the ServiceLocator.");
            return;
        }

        Instance.services.Add(typeof(T), service);
    }

    /// <summary>
    /// Unregisters the service from the current service locator.
    /// </summary>
    /// <typeparam name="T">Service type.</typeparam>
    public static void Unregister<T>()
    {
        if (!Instance.services.ContainsKey(typeof(T)))
        {
            Debug.LogError($"Attempted to unregister service of type {typeof(T).Name} which is not registered with the ServiceLocator.");
            return;
        }

        Instance.services.Remove(typeof(T));
    }
    public static void Unregister<T>(T service) => Unregister<T>();
}