using System.Reflection;

namespace DeviceFX.NfcApp.Helpers;

public static class DependencyInjectExtensions
{
    public static IServiceCollection AddTransientAssembly<TService>(this IServiceCollection services, params Assembly[] assemblies)
        => AddTransientAssembly(services,typeof(TService), assemblies);
    public static IServiceCollection AddTransientAssembly(this IServiceCollection services, Type serviceType, Assembly[] assemblies)
    {
        if(assemblies == null || assemblies.Length == 0)
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(App).GetTypeInfo().Assembly;
            assemblies = new []{ assembly };
            // assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }
        foreach (var implementationType in assemblies.SelectMany(assembly => assembly.GetTypes())
                     .Where(type => (serviceType.IsAssignableFrom(type) || type.IsAssignableToGenericType(serviceType)) && !type.GetTypeInfo().IsAbstract))
        {
            if(serviceType.IsGenericTypeDefinition)
            {
                // Note: Cannot register an open generic service type against an implimentation so instead just registering the implementation type
                // TODO: Add an option to scan all matching generic serviceTypes and register with the specific generic types
                services.AddTransient(implementationType);
            }
            else
            {
                services.AddTransient(serviceType, implementationType);
            }
        }
        return services;
    }
    public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
    {
        var interfaceTypes = givenType.GetInterfaces();

        foreach (var it in interfaceTypes)
        {
            if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                return true;
        }

        if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            return true;

        Type baseType = givenType.BaseType;
        if (baseType == null) return false;

        return IsAssignableToGenericType(baseType, genericType);
    }    
}