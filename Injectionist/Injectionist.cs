using System;
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
        class Handler
        {
            public Handler()
            {
                Decorators = new List<Resolver>();
            }

            public Resolver PrimaryResolver { get; set; }

            public List<Resolver> Decorators { get; private set; }
        }

        readonly Dictionary<Type, Handler> _resolvers = new Dictionary<Type, Handler>();

        /// <summary>
        /// Starts a new resolution context, resolving an instance of the given <typeparamref name="TService"/>
        /// </summary>
        public ResolutionResult<TService> Get<TService>()
        {
            var resolutionContext = new ResolutionContext(_resolvers);
            var instance = resolutionContext.Get<TService>();
            return new ResolutionResult<TService>(instance, resolutionContext.GetTrackedInstancesOf<object>());
        }

        /// <summary>
        /// Registers a factory method that can provide an instance of <typeparamref name="TService"/>
        /// </summary>
        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod)
        {
            Register(resolverMethod, isDecorator: false);
        }

        /// <summary>
        /// Registers a decorator factory method that can provide an instance of <typeparamref name="TService"/> 
        /// (i.e. the resolver is expected to call <see cref="IResolutionContext.Get{TService}"/> where TService
        /// is <typeparamref name="TService"/>
        /// </summary>
        public void Decorate<TService>(Func<IResolutionContext, TService> resolverMethod)
        {
            Register(resolverMethod, isDecorator: true);
        }

        /// <summary>
        /// Returns whether there exists a registration for the specified <typeparamref name="TService"/>.
        /// </summary>
        public bool Has<TService>(bool primary = true)
        {
            var key = typeof(TService);
            
            if (!_resolvers.ContainsKey(key) ) return false;

            var handler = _resolvers[key];

            if (handler.PrimaryResolver != null) return true;

            if (!primary && handler.Decorators.Any()) return true;

            return false;
        }

        void Register<TService>(Func<IResolutionContext, TService> resolverMethod, bool isDecorator)
        {
            var key = typeof(TService);
            if (!_resolvers.ContainsKey(key))
            {
                _resolvers.Add(key, new Handler());
            }

            var handler = _resolvers[key];

            if (!isDecorator)
            {
                if (handler.PrimaryResolver != null)
                {
                    throw new InvalidOperationException(string.Format("Attempted to register {0} as primary implementation of {1}, but a primary registration already exists: {2}",
                        resolverMethod, typeof(TService), handler.PrimaryResolver));
                }
            }

            var resolver = new Resolver<TService>(resolverMethod, isDecorator: isDecorator);

            if (!resolver.IsDecorator)
            {
                handler.PrimaryResolver = resolver;
            }
            else
            {
                handler.Decorators.Insert(0, resolver);
            }
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
                    typeof(TService));
            }
        }

        class ResolutionContext : IResolutionContext
        {
            readonly Dictionary<Type, int> _decoratorDepth = new Dictionary<Type, int>();
            readonly Dictionary<Type, Handler> _resolvers;
            readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
            readonly List<object> _resolvedInstances = new List<object>();

            public ResolutionContext(Dictionary<Type, Handler> resolvers)
            {
                _resolvers = resolvers;
            }

            public TService Get<TService>()
            {
                var serviceType = typeof(TService);

                if (_instances.ContainsKey(serviceType))
                {
                    return (TService)_instances[serviceType];
                }

                if (!_resolvers.ContainsKey(serviceType))
                {
                    throw new ResolutionException("Could not find resolver for {0}", serviceType);
                }

                if (!_decoratorDepth.ContainsKey(serviceType))
                {
                    _decoratorDepth[serviceType] = 0;
                }

                var handlerForThisType = _resolvers[serviceType];
                var depth = _decoratorDepth[serviceType]++;

                try
                {
                    var resolver = handlerForThisType
                        .Decorators
                        .Cast<Resolver<TService>>()
                        .Skip(depth)
                        .FirstOrDefault()
                        ?? (Resolver<TService>)handlerForThisType.PrimaryResolver;

                    var instance = resolver.InvokeResolver(this);

                    _instances[serviceType] = instance;
                    _resolvedInstances.Add(instance);

                    return instance;
                }
                catch (Exception exception)
                {
                    throw new ResolutionException(exception, "Could not resolve {0} with decorator depth {1} - registrations: {2}",
                        serviceType, depth, string.Join("; ", handlerForThisType));
                }
                finally
                {
                    _decoratorDepth[serviceType]--;
                }
            }

            public IEnumerable<T> GetTrackedInstancesOf<T>()
            {
                return _resolvedInstances.OfType<T>().ToList();
            }
        }
    }
}
