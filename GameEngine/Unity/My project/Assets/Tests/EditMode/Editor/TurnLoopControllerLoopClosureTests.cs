using System.Collections.Generic;
using System.Reflection;
using IT4s.Data;
using IT4s.Orchestration;
using NUnit.Framework;
using UnityEngine;

namespace IT4s.Orchestration.Tests
{
    public sealed class TurnLoopControllerLoopClosureTests
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
        }

        [Test]
        public void ConfigureReturnToWaitingAfterAiPlayback_StoresOptInSettings()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);

            controller.ConfigureReturnToWaitingAfterAiPlayback(true, 0.25f);

            Assert.That(controller.ReturnToWaitingAfterAiPlaybackEnabled, Is.True);
            Assert.That(controller.AiPlaybackCompletionPaddingSeconds, Is.EqualTo(0.25f));
        }

        [Test]
        public void TickPlayingAiResponse_WhenLoopClosureDisabled_StaysInPlaybackPhase()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);
            PatternTurn pattern = CreatePatternTurn(durationSamples: 200);
            InvokeMethod(controller, "StoreGeneratedAiPattern", pattern);
            controller.ConfigureReturnToWaitingAfterAiPlayback(false, 0f);

            InvokeMethod(controller, "TickPlayingAiResponse", 1000f);

            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.PlayingAiResponse));
        }

        [Test]
        public void TickPlayingAiResponse_BeforeEstimatedCompletion_StaysInPlaybackPhase()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);
            PatternTurn pattern = CreatePatternTurn(durationSamples: 200);
            InvokeMethod(controller, "StoreGeneratedAiPattern", pattern);
            controller.ConfigureReturnToWaitingAfterAiPlayback(true, 0.1f);

            InvokeMethod(controller, "ScheduleAiPlaybackLoopClosure", pattern, 10f);
            InvokeMethod(controller, "TickPlayingAiResponse", 12.09f);

            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.PlayingAiResponse));
        }

        [Test]
        public void TickPlayingAiResponse_AfterEstimatedCompletion_ReturnsToWaitingForHuman()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);
            PatternTurn pattern = CreatePatternTurn(durationSamples: 200);
            InvokeMethod(controller, "StoreGeneratedAiPattern", pattern);
            controller.ConfigureReturnToWaitingAfterAiPlayback(true, 0.1f);

            InvokeMethod(controller, "ScheduleAiPlaybackLoopClosure", pattern, 10f);
            InvokeMethod(controller, "TickPlayingAiResponse", 12.11f);

            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.WaitingForHuman));
        }

        [Test]
        public void TickPlayingAiResponse_WhenReturningToWaiting_ClearsCaptureRuntimeState()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);
            PatternTurn pattern = CreatePatternTurn(durationSamples: 200);
            object runtimeState = GetPrivateField<object>(controller, "runtimeState");
            InvokeMethod(runtimeState, "RecordCaptureStart", new HitEvent(128, 1, 100), 512L);
            InvokeMethod(controller, "StoreGeneratedAiPattern", pattern);
            controller.ConfigureReturnToWaitingAfterAiPlayback(true, 0f);

            InvokeMethod(controller, "ScheduleAiPlaybackLoopClosure", pattern, 10f);
            InvokeMethod(controller, "TickPlayingAiResponse", 12.01f);

            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.WaitingForHuman));
            Assert.That(GetProperty<bool>(runtimeState, "HasLastTriggerHit"), Is.False);
            Assert.That(GetProperty<long>(runtimeState, "CaptureStartSamples"), Is.EqualTo(-1));
            Assert.That(GetProperty<long>(runtimeState, "CaptureEndSamples"), Is.EqualTo(-1));
        }

        private TurnLoopController CreateRunningController(TurnPhase phase)
        {
            GameObject controllerObject = CreateObject("TurnLoopController");
            TurnLoopController controller = controllerObject.AddComponent<TurnLoopController>();
            SetPrivateField(controller, "isRunning", true);
            InvokeMethod(controller, "SetPhase", phase);
            return controller;
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static PatternTurn CreatePatternTurn(int durationSamples)
        {
            return new PatternTurn
            {
                turnId = 7,
                bpm = 120f,
                stepsPerQuarter = 4,
                sampleRate = 100,
                startSamples = 0,
                endSamples = durationSamples,
                velocity = new[] { 100, 0, 0, 0 },
                offsetSamples = new int[4]
            };
        }

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, InstanceFlags);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' to exist.");
            return method.Invoke(target, arguments);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, InstanceFlags);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' to exist.");
            return (T)property.GetValue(target);
        }
    }
}
