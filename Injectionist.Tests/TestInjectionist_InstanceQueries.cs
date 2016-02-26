using System;
using System.Linq;
using NUnit.Framework;

namespace Injectionist.Tests
{
    [TestFixture]
    public class TestInjectionist_InstanceQueries
    {
        [Test]
        public void CanGetResolvedInstancesOfSomeParticularType()
        {
            var injectionist = new Injection.Injectionist();

            injectionist.Decorate<IService>(c => new Decorator("2", c.Get<IService>()));
            injectionist.Decorate<IService>(c => new Decorator("3", c.Get<IService>()));
            injectionist.Decorate<IService>(c => new Decorator("4", c.Get<IService>()));

            injectionist.Register<IService>(c => new Primary("1"));

            var result = injectionist.Get<IService>();

            var instanceNames = result.TrackedInstances
                .OfType<INamed>()
                .Select(n => n.Name)
                .ToList();

            Console.WriteLine($@"Instance names:

{string.Join(Environment.NewLine, instanceNames.Select(name => $"    {name}"))}");

            Assert.That(instanceNames, Is.EqualTo(new[] {"1", "2", "3", "4"}));
        }

        interface IInitializable
        {
            void Initialize();
        }

        interface IService
        {
        }

        interface INamed
        {
            string Name { get; }
        }

        class Primary : IService, IInitializable, INamed
        {
            public string Name { get; }

            public Primary(string name)
            {
                Name = name;
            }

            public bool WasInitialized { get; set; }

            public void Initialize()
            {
                WasInitialized = true;
            }
        }

        class Decorator : IService, INamed
        {
            readonly IService _innerService;

            public string Name { get; }

            public Decorator(string name, IService innerService)
            {
                Name = name;
                _innerService = innerService;
            }
        }
    }
}