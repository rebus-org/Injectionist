using NUnit.Framework;

namespace Injectionist.Tests
{
    [TestFixture]
    public class TestInjectionist_ServiceAndImplementation
    {
        Injectionist _injectionist;

        [SetUp]
        public void SetUp()
        {
            _injectionist = new Injectionist();
        }

        [Test]
        public void CanResolveViaRegisteredInterface()
        {
            _injectionist.Register<ISomeDependency>(c => new SomeDependency());
            _injectionist.Register<ISomeService>(c => new SomeImplementation(c.Get<ISomeDependency>()));

            var instance = _injectionist.Get<ISomeService>().Instance;

            Assert.That(instance, Is.TypeOf<SomeImplementation>());
            Assert.That(((SomeImplementation)instance).SomeDependency, Is.TypeOf<SomeDependency>());
        }

        interface ISomeService { }

        interface ISomeDependency { }

        class SomeImplementation : ISomeService
        {
            readonly ISomeDependency _someDependency;

            public SomeImplementation(ISomeDependency someDependency)
            {
                _someDependency = someDependency;
            }

            public ISomeDependency SomeDependency
            {
                get { return _someDependency; }
            }
        }

        class SomeDependency : ISomeDependency { }
    }

}