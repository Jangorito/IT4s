using System.Collections.Generic;
using System.Reflection;
using IT4s.Data;
using IT4s.Input;
using IT4s.Orchestration;
using IT4s.Rhythm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IT4s.Orchestration.Tests
{
    public sealed class HumanTurnCaptureFlowTests
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TryStartCapture_UsesTimingToScheduleEndAndBeginsCapture()
        {
            FlowHarness harness = CreateHarness();
            var flow = new HumanTurnCaptureFlow(harness.TurnCaptureController, harness.HitReceiver);
            HitEvent triggerHit = new HitEvent(44100, 1, 100);
            MusicalTimingConfig timing = CreateTimingConfig();

            HumanTurnCaptureFlow.StartStatus status = flow.TryStartCapture(
                triggerHit,
                timing,
                out long turnDurationSamples,
                out long endSamples,
                out string errorReason);

            Assert.That(status, Is.EqualTo(HumanTurnCaptureFlow.StartStatus.Started));
            Assert.That(errorReason, Is.Null);
            Assert.That(turnDurationSamples, Is.EqualTo(timing.GetTurnDurationSamples()));
            Assert.That(endSamples, Is.EqualTo(triggerHit.tSamples + turnDurationSamples));
            Assert.That(harness.TurnCaptureController.IsCapturing, Is.True);
        }

        [Test]
        public void TickCapture_WaitsForClock_WhenReceiverHasNoClockYet()
        {
            FlowHarness harness = CreateHarness();
            var flow = new HumanTurnCaptureFlow(harness.TurnCaptureController, harness.HitReceiver);
            HitEvent triggerHit = new HitEvent(44100, 1, 100);

            Assert.That(
                flow.TryStartCapture(
                    triggerHit,
                    CreateTimingConfig(),
                    out _,
                    out long endSamples,
                    out _),
                Is.EqualTo(HumanTurnCaptureFlow.StartStatus.Started));

            HumanTurnCaptureFlow.TickStatus status = flow.TickCapture(
                triggerHit.tSamples,
                endSamples,
                out TurnWindow turnWindow,
                out string errorReason);

            Assert.That(status, Is.EqualTo(HumanTurnCaptureFlow.TickStatus.WaitingForClock));
            Assert.That(errorReason, Is.Null);
            Assert.That(turnWindow.HitCount, Is.EqualTo(0));
            Assert.That(harness.TurnCaptureController.IsCapturing, Is.True);
        }

        [Test]
        public void TickCapture_CompletesWhenClockReachesScheduledEnd()
        {
            FlowHarness harness = CreateHarness();
            var flow = new HumanTurnCaptureFlow(harness.TurnCaptureController, harness.HitReceiver);
            HitEvent triggerHit = new HitEvent(44100, 1, 100);
            MusicalTimingConfig timing = CreateTimingConfig();

            Assert.That(
                flow.TryStartCapture(
                    triggerHit,
                    timing,
                    out long turnDurationSamples,
                    out long endSamples,
                    out _),
                Is.EqualTo(HumanTurnCaptureFlow.StartStatus.Started));

            harness.HitBuffer.Add(new HitEvent(triggerHit.tSamples + 22050, 2, 80));
            SetReceiverClock(harness.HitReceiver, endSamples);

            HumanTurnCaptureFlow.TickStatus status = flow.TickCapture(
                triggerHit.tSamples,
                endSamples,
                out TurnWindow turnWindow,
                out string errorReason);

            Assert.That(status, Is.EqualTo(HumanTurnCaptureFlow.TickStatus.Completed));
            Assert.That(errorReason, Is.Null);
            Assert.That(turnWindow.startSamples, Is.EqualTo(triggerHit.tSamples));
            Assert.That(turnWindow.endSamples, Is.EqualTo(triggerHit.tSamples + turnDurationSamples));
            Assert.That(turnWindow.HitCount, Is.EqualTo(1));
            Assert.That(harness.TurnCaptureController.IsCapturing, Is.False);
        }

        private FlowHarness CreateHarness()
        {
            GameObject captureObject = CreateObject("TurnCaptureController");
            GameObject receiverObject = CreateObject("OscHitReceiver");

            var turnCaptureController = captureObject.AddComponent<TestTurnCaptureController>();
            var hitReceiver = receiverObject.AddComponent<TestOscHitReceiver>();
            HitBuffer hitBuffer = new HitBuffer();

            turnCaptureController.SetHitReceiver(hitReceiver);
            SetPrivateField(hitReceiver, "_buffer", hitBuffer);

            return new FlowHarness(turnCaptureController, hitReceiver, hitBuffer);
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static MusicalTimingConfig CreateTimingConfig()
        {
            return new MusicalTimingConfig(
                120f,
                4,
                2,
                4,
                44100);
        }

        private static void SetReceiverClock(OscHitReceiver receiver, long lastSamples)
        {
            SetPrivateField(receiver, "_lastSamples", lastSamples);
            SetPrivateField(receiver, "_hasLastSamples", true);
            SetPrivateField(receiver, "_lastSampleRealtimeSeconds", (double)Time.realtimeSinceStartup);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);

            if (field == null)
            {
                field = target.GetType().BaseType?.GetField(fieldName, InstanceFlags);
            }

            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private sealed class FlowHarness
        {
            public FlowHarness(
                TurnCaptureController turnCaptureController,
                OscHitReceiver hitReceiver,
                HitBuffer hitBuffer)
            {
                TurnCaptureController = turnCaptureController;
                HitReceiver = hitReceiver;
                HitBuffer = hitBuffer;
            }

            public TurnCaptureController TurnCaptureController { get; }
            public OscHitReceiver HitReceiver { get; }
            public HitBuffer HitBuffer { get; }
        }

        private sealed class TestTurnCaptureController : TurnCaptureController
        {
            private new void Awake()
            {
            }
        }

        private sealed class TestOscHitReceiver : OscHitReceiver
        {
            private new void Awake()
            {
            }
        }
    }
}
