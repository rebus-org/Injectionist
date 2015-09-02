using System;
using System.Collections;

namespace Injectionist
{
    /// <summary>
    /// Contains a built object instance along with all the objects that were used to build the instance
    /// </summary>
    public class ResolutionResult<TService>
    {
        internal ResolutionResult(TService instance, IEnumerable trackedInstances)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (trackedInstances == null) throw new ArgumentNullException("trackedInstances");
            Instance = instance;
            TrackedInstances = trackedInstances;
        }

        /// <summary>
        /// Gets the instance that was built
        /// </summary>
        public TService Instance { get; private set; }

        /// <summary>
        /// Gets all object instances that were used to build <see cref="Instance"/>, including the instance itself
        /// </summary>
        public IEnumerable TrackedInstances { get; private set; }
    }
}