using Game.TickSystem;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace Core.Tests
{
    public class RemoteTimelineTests
    {
        [Test]
        public void BufferUnderrunIsAppliedOnceAtFrameBoundary()
        {
            GameObject game_object = new("TickManagerTest");
            TickManager tick_manager = game_object.AddComponent<TickManager>();
            typeof(TickManager)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(tick_manager, null);

            try
            {
                Assert.That(tick_manager.RemoteInterpolationBackTicks, Is.EqualTo(9));

                tick_manager.ReportRemoteBufferUnderrun();
                tick_manager.ReportRemoteBufferUnderrun();

                Assert.That(tick_manager.RemoteInterpolationBackTicks, Is.EqualTo(9));

                typeof(TickManager)
                    .GetMethod(
                        "UpdateRemoteInterpolationDelay",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(tick_manager, null);

                Assert.That(tick_manager.RemoteInterpolationBackTicks, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(game_object);
            }
        }
    }
}
