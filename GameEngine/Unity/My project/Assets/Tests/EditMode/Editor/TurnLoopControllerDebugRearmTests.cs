using System.Collections.Generic;
using System.Reflection;
using IT4s.Data;
using IT4s.Orchestration;
using NUnit.Framework;
using UnityEngine;

namespace IT4s.Orchestration.Tests
{
    public sealed class TurnLoopControllerDebugRearmTests
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
        public void TryDebugRearmForHumanCapture_FromPlayingAiResponse_ReturnsToWaitingForHuman()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);

            bool rearmed = controller.TryDebugRearmForHumanCapture();

            Assert.That(rearmed, Is.True);
            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.WaitingForHuman));
        }

        [Test]
        public void TryDebugRearmForHumanCapture_FromCapturingHuman_IsIgnored()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.CapturingHuman);

            bool rearmed = controller.TryDebugRearmForHumanCapture();

            Assert.That(rearmed, Is.False);
            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.CapturingHuman));
        }

        [Test]
        public void TryDebugRearmForHumanCapture_ClearsCaptureRuntimeState()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);
            object runtimeState = GetPrivateField<object>(controller, "runtimeState");
            InvokeMethod(runtimeState, "RecordCaptureStart", new HitEvent(128, 1, 100), 512L);

            Assert.That(GetProperty<bool>(runtimeState, "HasLastTriggerHit"), Is.True);
            Assert.That(GetProperty<long>(runtimeState, "CaptureStartSamples"), Is.EqualTo(128));
            Assert.That(GetProperty<long>(runtimeState, "CaptureEndSamples"), Is.EqualTo(512));

            bool rearmed = controller.TryDebugRearmForHumanCapture();

            Assert.That(rearmed, Is.True);
            Assert.That(GetProperty<bool>(runtimeState, "HasLastTriggerHit"), Is.False);
            Assert.That(GetProperty<long>(runtimeState, "CaptureStartSamples"), Is.EqualTo(-1));
            Assert.That(GetProperty<long>(runtimeState, "CaptureEndSamples"), Is.EqualTo(-1));
        }

        [Test]
        public void TryDebugRearmForHumanCapture_RepeatedPressesDoNotClearLastDebugOutput()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);
            PatternTurn generatedPattern = CreatePatternTurn();
            InvokeMethod(controller, "StoreGeneratedAiPattern", generatedPattern);

            bool firstRearm = controller.TryDebugRearmForHumanCapture();
            bool secondRearm = controller.TryDebugRearmForHumanCapture();

            Assert.That(firstRearm, Is.True);
            Assert.That(secondRearm, Is.False);
            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.WaitingForHuman));
            Assert.That(controller.HasLastGeneratedAiPatternTurn, Is.True);
            Assert.That(controller.LastGeneratedAiPatternTurn, Is.SameAs(generatedPattern));
        }

        [Test]
        public void TryDebugRearmForHumanCapture_WhenHotkeyDisabled_IsInert()
        {
            TurnLoopController controller = CreateRunningController(TurnPhase.PlayingAiResponse);
            SetPrivateField(controller, "enableDebugRearmHotkey", false);

            bool rearmed = controller.TryDebugRearmForHumanCapture();

            Assert.That(rearmed, Is.False);
            Assert.That(controller.CurrentPhase, Is.EqualTo(TurnPhase.PlayingAiResponse));
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

        private static PatternTurn CreatePatternTurn()
        {
            return new PatternTurn
            {
                turnId = 42,
                bpm = 120f,
                stepsPerQuarter = 4,
                sampleRate = 44100,
                startSamples = 0,
                endSamples = 352800,
                velocity = new[] { 100, 0, 0, 0 },
                offsetSamples = new int[4]
            };
        }

        private static void InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, InstanceFlags);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' to exist.");
            method.Invoke(target, arguments);
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
