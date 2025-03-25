using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
    /// <summary>
    /// Analyzes service dependencies by examining service classes and their methods
    /// to determine which services depend on other services.
    /// </summary>
    public static class ServiceDependencyAnalyzer
    {
        // Cache of dependencies to avoid recalculating
        private static readonly Dictionary<Type, HashSet<Type>> DependenciesCache = new();
        private static readonly Dictionary<Type, HashSet<Type>> DependentsCache = new();
        
        /// <summary>
        /// Gets the types of services that a specific service depends on.
        /// </summary>
        /// <param name="serviceType">The service type to analyze.</param>
        /// <returns>A set of service types that this service depends on.</returns>
        public static HashSet<Type> GetServiceDependencies(Type serviceType)
        {
            if (DependenciesCache.TryGetValue(serviceType, out var cachedDependencies))
            {
                return cachedDependencies;
            }

            var dependencies = new HashSet<Type>();
            
            try
            {
                // Get the concrete implementation if this is an interface
                Type implementationType = FindImplementationType(serviceType);
                
                // Analyze fields for service interfaces
                AnalyzeFieldsForDependencies(implementationType, dependencies);
                
                // Analyze the Awake method for GetServiceAsync calls
                AnalyzeAwakeMethodForDependencies(implementationType, dependencies);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error analyzing dependencies for {serviceType.Name}: {e.Message}");
            }
            
            // Cache the result
            DependenciesCache[serviceType] = dependencies;
            return dependencies;
        }
        
        private static Type FindImplementationType(Type serviceType)
        {
            if (!serviceType.IsInterface)
                return serviceType;
                
            // Get all loaded assemblies that might contain the implementation
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    // Look for concrete implementations of this interface
                    var implementationTypes = assembly.GetTypes()
                        .Where(t => !t.IsInterface && !t.IsAbstract && serviceType.IsAssignableFrom(t))
                        .ToList();
                        
                    if (implementationTypes.Count > 0)
                    {
                        return implementationTypes[0]; // Take the first implementation
                    }
                }
                catch (Exception)
                {
                    // Some assemblies may throw exceptions when getting types, just continue
                    continue;
                }
            }
            
            // If no implementation found, return the original type
            return serviceType;
        }
        
        private static void AnalyzeFieldsForDependencies(Type type, HashSet<Type> dependencies)
        {
            // Get all instance fields
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                
                // Check if it's a service interface (starts with 'I' and is an interface)
                if (fieldType.IsInterface && fieldType.Name.StartsWith("I"))
                {
                    // This is likely a service dependency
                    dependencies.Add(fieldType);
                    Debug.Log($"Found field dependency in {type.Name}: {field.Name} of type {fieldType.Name}");
                }
            }
        }
        
        private static void AnalyzeAwakeMethodForDependencies(Type type, HashSet<Type> dependencies)
        {
            // Look for Awake method
            var awakeMethod = type.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (awakeMethod == null)
                return;
                
            // We can't easily analyze the method body with reflection alone,
            // but we can check for common patterns in service implementations
            
            // In your code pattern, you typically have fields that match the service interfaces
            // and you set them in Awake with GetServiceAsync, so we've already caught those in AnalyzeFieldsForDependencies
            
            // For tuple returns like (_service1, _service2) = await GetServiceAsync<IService1, IService2>()
            // we need to examine the method more carefully. Since we can't do proper code analysis,
            // we'll use a heuristic approach: check if the class derives from a base class with GetServiceAsync methods
            
            // Check if this type inherits from a class with GetServiceAsync methods
            bool inheritsFromServiceBase = DoesInheritFromServiceBase(type);
            
            if (inheritsFromServiceBase)
            {
                // Look for additional generic GetServiceAsync calls that might not be caught by field analysis
                
                // Since we can't directly analyze the IL code easily, we'll rely on field analysis 
                // and assume any interface fields are likely dependencies.
                // This is already done in AnalyzeFieldsForDependencies
            }
        }
        
        private static bool DoesInheritFromServiceBase(Type type)
        {
            // Check if inherits from a class that likely has GetServiceAsync methods
            var baseType = type.BaseType;
            
            while (baseType != null && baseType != typeof(object))
            {
                // Check if it's a service base class (contains "Service" in the name)
                if (baseType.Name.Contains("Service") || 
                    (baseType.Namespace != null && baseType.Namespace.Contains("Service")))
                {
                    return true;
                }
                
                baseType = baseType.BaseType;
            }
            
            return false;
        }

        /// <summary>
        /// Gets the types of services that depend on a specific service.
        /// </summary>
        /// <param name="serviceType">The service type to analyze.</param>
        /// <param name="allServices">All service types to check against.</param>
        /// <returns>A set of service types that depend on this service.</returns>
        public static HashSet<Type> GetServiceDependents(Type serviceType, IEnumerable<Type> allServices)
        {
            if (DependentsCache.TryGetValue(serviceType, out var cachedDependents))
            {
                return cachedDependents;
            }

            var dependents = new HashSet<Type>();
            
            foreach (var potentialDependent in allServices)
            {
                if (potentialDependent == serviceType)
                    continue;
                
                var dependencies = GetServiceDependencies(potentialDependent);
                
                // Check if this service type or any of its interfaces or base classes are dependencies
                if (dependencies.Contains(serviceType))
                {
                    dependents.Add(potentialDependent);
                    continue;
                }
                
                // Check interface implementation
                foreach (var dependency in dependencies)
                {
                    if (dependency.IsInterface && serviceType.GetInterfaces().Contains(dependency))
                    {
                        dependents.Add(potentialDependent);
                        break;
                    }
                    
                    // Check inheritance
                    if (dependency.IsAssignableFrom(serviceType) && dependency != typeof(object))
                    {
                        dependents.Add(potentialDependent);
                        break;
                    }
                }
            }
            
            // Cache the result
            DependentsCache[serviceType] = dependents;
            return dependents;
        }
        
        /// <summary>
        /// Clears the dependency analysis caches.
        /// </summary>
        public static void ClearCache()
        {
            DependenciesCache.Clear();
            DependentsCache.Clear();
        }
    }
}