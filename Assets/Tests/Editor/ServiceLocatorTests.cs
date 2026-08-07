using NUnit.Framework;
using SteelTempest.Core.Di;

namespace SteelTempest.Tests.Editor
{
    public class ServiceLocatorTests
    {
        public interface IFoo { int Value { get; } }
        public class Foo : IFoo { public int Value => 42; }

        [Test]
        public void ResolveFactory_CachesSingleton()
        {
            var locator = new ServiceLocator();
            var callCount = 0;
            locator.Register<IFoo>(() =>
            {
                callCount++;
                return new Foo();
            });

            var a = locator.Resolve<IFoo>();
            var b = locator.Resolve<IFoo>();

            Assert.AreEqual(1, callCount);
            Assert.AreSame(a, b);
        }

        [Test]
        public void ResolveUnknown_Throws()
        {
            var locator = new ServiceLocator();
            Assert.Throws<System.InvalidOperationException>(() => locator.Resolve<IFoo>());
        }

        [Test]
        public void IsRegisteredForInstance_True()
        {
            var locator = new ServiceLocator();
            locator.RegisterInstance<IFoo>(new Foo());
            Assert.IsTrue(locator.IsRegistered<IFoo>());
        }
    }
}