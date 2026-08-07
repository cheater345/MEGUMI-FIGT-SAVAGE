using System;
using System.Collections.Generic;

namespace SteelTempest.Core.Di
{
    /// <summary>
    /// Lightweight dependency injection container.
    /// Supports singleton services, lazy factories, and manual resolution.
    /// </summary>
    public sealed class ServiceLocator
    {
        private static readonly Lazy<ServiceLocator> Lazy = new(() => new ServiceLocator());
        public static ServiceLocator Instance => Lazy.Value;

        private readonly Dictionary<Type, Func<object>> _factories = new();
        private readonly Dictionary<Type, object> _singletons = new();

        public ServiceLocator() { }

        /// <summary>Registers a factory used to create the service on first resolution.</summary>
        public ServiceLocator Register<TInterface>(Func<TInterface> factory)
            where TInterface : class
        {
            _factories[typeof(TInterface)] = () => factory();
            return this;
        }

        /// <summary>Registers a concrete singleton instance directly.</summary>
        public ServiceLocator RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class
        {
            _singletons[typeof(TInterface)] = instance;
            return this;
        }

        /// <summary>Resolves a service, creating it lazily if needed.</summary>
        public TInterface Resolve<TInterface>() where TInterface : class
        {
            var type = typeof(TInterface);
            if (_singletons.TryGetValue(type, out var singleton))
            {
                return (TInterface)singleton;
            }
            if (_factories.TryGetValue(type, out var factory))
            {
                var created = (TInterface)factory();
                _singletons[type] = created;
                return created;
            }
            throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
        }

        /// <summary>True when a factory or instance is registered for the type.</summary>
        public bool IsRegistered<TInterface>() where TInterface : class =>
            _singletons.ContainsKey(typeof(TInterface)) || _factories.ContainsKey(typeof(TInterface));
    }
}
