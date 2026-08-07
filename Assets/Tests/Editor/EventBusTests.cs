using NUnit.Framework;
using SteelTempest.Core.Events;

namespace SteelTempest.Tests.Editor
{
    public class EventBusTests
    {
        [Test]
        public void Publish_CallsSubscriber()
        {
            var bus = new EventBus();
            var called = false;
            bus.Subscribe<TestNotification>(e => called = true);
            bus.Publish(new TestNotification("hi"));
            Assert.IsTrue(called);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            var bus = new EventBus();
            var calls = 0;
            void Handler(TestNotification e) => calls++;
            bus.Subscribe<TestNotification>(Handler);
            bus.Publish(new TestNotification("1"));
            bus.Unsubscribe<TestNotification>(Handler);
            bus.Publish(new TestNotification("2"));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Publish_NoSubscribers_IsSafe()
        {
            var bus = new EventBus();
            Assert.DoesNotThrow(() => bus.Publish(new TestNotification("x")));
        }

        private readonly struct TestNotification
        {
            public readonly string Text;
            public TestNotification(string text) => Text = text;
        }
    }
}