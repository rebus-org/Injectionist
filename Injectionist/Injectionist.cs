﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Injectionist
{
    /// <summary>
    /// Dependency injectionist that can be used for configuring a system of injected service implementations, possibly with decorators,
    /// with caching of instances so that the same instance of each class is used throughout the tree. Should probably not be used for
    /// anything at runtime, is only meant to be used in configuration scenarios.
    /// </summary>
    public class Injectionist
    {
        readonly Dictionary<Type, List<Resolver>> _resolvers = new Dictionary<Type, List<Resolver>>();

        /// <summary>
        /// Starts a new resolution context, resolving an instance of the given <see cref="TService"/>
        /// </summary>
        public TService Get<TService>()
        {
            return new ResolutionContext(_resolvers).Get<TService>();
        }

        /// <summary>
        /// Registers a factory method that can provide an instance of <see cref="TService"/>. If <see cref="isDecorator"/> is set to true,
        /// the factory method will be called before the factory method where <see cref="isDecorator"/> is set to false (which is the default).
        /// There can be only one factory method with <see cref="isDecorator"/>=false for each type of <see cref="TService"/>.
        /// </summary>
        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod, bool isDecorator = false)
        {
            var key = typeof(TService);
            if (!_resolvers.ContainsKey(key))
            {
                _resolvers.Add(key, new List<Resolver>());
            }

            var resolverList = _resolvers[key];

            if (!isDecorator)
            {
                var existingPrimaryRegistration = resolverList.FirstOrDefault(r => !r.IsDecorator);

                if (existingPrimaryRegistration != null)
                {
                    throw new InvalidOperationException(string.Format("Attempted to register {0} as primary implementation of {1}, but a primary registration already exists: {2}",
                        resolverMethod, typeof(TService), existingPrimaryRegistration));
                }
            }

            var resolver = new Resolver<TService>(resolverMethod, isDecorator: isDecorator);

            if (!resolver.IsDecorator)
            {
                resolverList.Add(resolver);
            }
            else
            {
                resolverList.Insert(0, resolver);
            }
        }

        /// <summary>
        /// Returns whether there exists a registration for the specified <see cref="TService"/>.
        /// </summary>
        public bool Has<TService>(bool primary = true)
        {
            var key = typeof(TService);
            return _resolvers.ContainsKey(key) 
                && _resolvers[key].Count(r => !r.IsDecorator) == 1;
        }

        abstract class Resolver
        {
            readonly bool _isDecorator;

            protected Resolver(bool isDecorator)
            {
                _isDecorator = isDecorator;
            }

            public bool IsDecorator
            {
                get { return _isDecorator; }
            }
        }

        class Resolver<TService> : Resolver
        {
            readonly Func<IResolutionContext, TService> _resolver;

            public Resolver(Func<IResolutionContext, TService> resolver, bool isDecorator)
                : base(isDecorator)
            {
                _resolver = resolver;
            }

            public TService InvokeResolver(IResolutionContext context)
            {
                return _resolver(context);
            }

            public override string ToString()
            {
                return string.Format("{0} ({1} {2})",
                    _resolver,
                    IsDecorator ? "decorator ->" : "primary ->",
                    typeof (TService));
            }
        }

        class ResolutionContext : IResolutionContext
        {
            readonly Dictionary<Type, int> _decoratorDepth = new Dictionary<Type, int>();
            readonly Dictionary<Type, List<Resolver>> _resolvers;
            readonly Dictionary<Type, Tuple<object, int>> _instances = new Dictionary<Type, Tuple<object, int>>();
            int _resolutionOrderCounter;

            public ResolutionContext(Dictionary<Type, List<Resolver>> resolvers)
            {
                _resolvers = resolvers;
            }

            public TService Get<TService>()
            {
                var serviceType = typeof(TService);

                if (_instances.ContainsKey(serviceType))
                {
                    return (TService) _instances[serviceType].Item1;
                }

                if (!_resolvers.ContainsKey(serviceType))
                {
                    throw new ResolutionException("Could not find resolver for {0}", serviceType);
                }

                if (!_decoratorDepth.ContainsKey(serviceType))
                {
                    _decoratorDepth[serviceType] = 0;
                }

                var resolversForThisType = _resolvers[serviceType];
                var depth = _decoratorDepth[serviceType]++;

                try
                {
                    var instance = resolversForThisType
                        .Cast<Resolver<TService>>()
                        .Skip(depth)
                        .First()
                        .InvokeResolver(this);

                    _instances[serviceType] = new Tuple<object, int>(instance, _resolutionOrderCounter++);

                    return instance;
                }
                catch (Exception exception)
                {
                    throw new ResolutionException(exception, "Could not resolve {0} with decorator depth {1} - registrations: {2}",
                        serviceType, depth, string.Join("; ", resolversForThisType));
                }
                finally
                {
                    _decoratorDepth[serviceType]--;
                }
            }

            public IEnumerable<T> GetTrackedInstancesOf<T>()
            {
                return _instances.Values
                    .OrderBy(t => t.Item2)  //< order instances by when they were created
                    .Select(t => t.Item1)
                    .OfType<T>()
                    .ToList();
            }
        }
    }
}
