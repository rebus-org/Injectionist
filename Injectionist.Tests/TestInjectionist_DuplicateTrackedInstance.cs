using System.Linq;
using NUnit.Framework;

namespace Injectionist.Tests
{
    [TestFixture]
    [Description("When a decorator does not actually decorate in order to return a decorator, but does it in order to hook into the resolution of a specific service, a bug caused the instance to be double-tracked")]
    public class TestInjectionist_DuplicateTrackedInstance
    {
        [Test]
        public void DoesNotReturnSameInstanceTwice()
        {
            var injectionist = new Injection.Injectionist();

            injectionist.Register(c => new Something());

            injectionist.Decorate(c => c.Get<Something>());

            var result = injectionist.Get<Something>();

            Assert.That(result.TrackedInstances.OfType<Something>().Count(), Is.EqualTo(1));
        }

        class Something { }
    }
}