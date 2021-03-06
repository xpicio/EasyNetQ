﻿// ReSharper disable InconsistentNaming
using System;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.FluentConfiguration;
using Xunit;
using NSubstitute;
using System.Reflection;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    public class When_autosubscribing_with_subscription_configuration_action_and_attribute : IDisposable
    {
        private IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
       
        public When_autosubscribing_with_subscription_configuration_action_and_attribute()
        {
            bus = Substitute.For<IBus>();
           
            var autoSubscriber = new AutoSubscriber(bus, "my_app")
                {
                        ConfigureSubscriptionConfiguration =
                                c => c.WithAutoDelete(false)
                                    .WithCancelOnHaFailover(false)
                                    .WithExpires(11)
                                    .WithPrefetchCount(11)
                                    .WithPriority(11)
                };

            bus.When(x => x.Subscribe(
                    Arg.Is("MyActionAndAttributeTest"),
                    Arg.Any<Action<MessageA>>(),
                    Arg.Any<Action<ISubscriptionConfiguration>>()
                    ))
                    .Do(a =>
                    {
                        capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2];
                    });

            autoSubscriber.Subscribe(GetType().GetTypeInfo().Assembly);
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_have_called_subscribe()
        {
            bus.Received().Subscribe(Arg.Any<string>(),
                                     Arg.Any<Action<MessageA>>(),
                                     Arg.Any<Action<ISubscriptionConfiguration>>());
        }

        [Fact]
        public void Should_have_called_subscribe_with_attribute_values_notaction_values()
        {
            var subscriptionConfiguration = new SubscriptionConfiguration(1);
            
            capturedAction(subscriptionConfiguration);

            subscriptionConfiguration.AutoDelete.ShouldBeTrue();
            subscriptionConfiguration.CancelOnHaFailover.ShouldBeTrue();
            subscriptionConfiguration.Expires.ShouldEqual(10);
            subscriptionConfiguration.PrefetchCount.ShouldEqual((ushort)10);
            subscriptionConfiguration.Priority.ShouldEqual(10);

        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumerWithActionAndAttribute : IConsume<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyActionAndAttributeTest")]
            [SubscriptionConfiguration(AutoDelete = true, CancelOnHaFailover = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
            public void Consume(MessageA message)
            {
            }
        }

        private class MessageA
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming