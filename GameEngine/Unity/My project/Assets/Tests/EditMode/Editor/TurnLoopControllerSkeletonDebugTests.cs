using System.Reflection;
using IT4s.Orchestration;
using IT4s.Rhythm.Generation.Skeleton;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning.Models;
using NUnit.Framework;
using UnityEngine;

namespace IT4s.Orchestration.Tests
{
    public sealed class TurnLoopControllerSkeletonDebugTests
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private GameObject controllerObject;

        [TearDown]
        public void TearDown()
        {
            if (controllerObject != null)
            {
                Object.DestroyImmediate(controllerObject);
                controllerObject = null;
            }
        }

        [Test]
        public void LastSkeletonDebugSnapshot_IsSafeBeforeSkeletonExists()
        {
            TurnLoopController controller = CreateController();

            Assert.That(controller.HasLastSkeletonDebugSnapshot, Is.False);
            Assert.That(controller.LastSkeletonDebugSnapshot, Is.Null);
        }

        [Test]
        public void StoreSkeletonDebugSnapshot_UpdatesReadOnlyControllerState()
        {
            TurnLoopController controller = CreateController();
            ResponsePlan plan = new ResponsePlan(
                ResponseType.Mirror,
                targetDensity: 0.5f,
                complementarityBias: 0.2f,
                preserveAnchors: true,
                mirrorEnding: true,
                turnLengthSteps: 4);
            SkeletonPattern skeleton = CreateSkeletonPattern();

            InvokePrivateMethod(controller, "StoreResponsePlan", plan);
            InvokePrivateMethod(controller, "StoreSkeletonDebugSnapshot", skeleton);

            SkeletonDebugSnapshot snapshot = controller.LastSkeletonDebugSnapshot;

            Assert.That(controller.HasLastSkeletonDebugSnapshot, Is.True);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.TurnLengthSteps, Is.EqualTo(4));
            Assert.That(snapshot.SelectedStepCount, Is.EqualTo(2));
            Assert.That(snapshot.TargetDensity, Is.EqualTo(0.5f));
            Assert.That(snapshot.PreserveAnchors, Is.True);
            Assert.That(snapshot.RequireStrongEnding, Is.True);
            Assert.That(snapshot.IsActiveStep(0), Is.True);
            Assert.That(snapshot.IsActiveStep(1), Is.False);
            Assert.That(snapshot.GetAnnotation(0).IsAnchor, Is.True);
            Assert.That(snapshot.GetAnnotation(3).InEndingRegion, Is.True);
        }

        [Test]
        public void StoreSkeletonDebugSnapshot_DoesNotMutateGenerationOutput()
        {
            TurnLoopController controller = CreateController();
            SkeletonPattern skeleton = CreateSkeletonPattern();
            bool[] originalActive = (bool[])skeleton.ActiveSteps.Clone();
            int[] originalSelected = (int[])skeleton.SelectedStepIndices.Clone();
            bool originalAnchorFlag = skeleton.StepMeta[0].SourceAnchor;

            InvokePrivateMethod(controller, "StoreSkeletonDebugSnapshot", skeleton);

            CollectionAssert.AreEqual(originalActive, skeleton.ActiveSteps);
            CollectionAssert.AreEqual(originalSelected, skeleton.SelectedStepIndices);
            Assert.That(skeleton.StepMeta[0].SourceAnchor, Is.EqualTo(originalAnchorFlag));
        }

        [Test]
        public void SkeletonDebugSnapshot_CopiesGenerationArrays()
        {
            TurnLoopController controller = CreateController();
            SkeletonPattern skeleton = CreateSkeletonPattern();

            InvokePrivateMethod(controller, "StoreSkeletonDebugSnapshot", skeleton);
            SkeletonDebugSnapshot snapshot = controller.LastSkeletonDebugSnapshot;

            skeleton.ActiveSteps[0] = false;
            skeleton.SelectedStepIndices[0] = 99;
            skeleton.StepMeta[0].SourceAnchor = false;

            Assert.That(snapshot.IsActiveStep(0), Is.True);
            Assert.That(snapshot.SelectedStepIndices[0], Is.EqualTo(0));
            Assert.That(snapshot.GetAnnotation(0).IsAnchor, Is.True);
        }

        private TurnLoopController CreateController()
        {
            controllerObject = new GameObject("TurnLoopController");
            return controllerObject.AddComponent<TurnLoopController>();
        }

        private static SkeletonPattern CreateSkeletonPattern()
        {
            var stepMeta = new[]
            {
                new SkeletonStepMeta
                {
                    StepIndex = 0,
                    IsStrongBeat = true,
                    Selected = true,
                    SourceAnchor = true,
                    IsExplicitAnchor = true,
                    Protected = true,
                    ReasonFlags = SkeletonReasonFlags.MetricStrong | SkeletonReasonFlags.ProtectedAnchor
                },
                new SkeletonStepMeta
                {
                    StepIndex = 1,
                    ReasonFlags = SkeletonReasonFlags.MetricWeak
                },
                new SkeletonStepMeta
                {
                    StepIndex = 2,
                    Selected = true
                },
                new SkeletonStepMeta
                {
                    StepIndex = 3,
                    InEndingRegion = true
                }
            };

            return new SkeletonPattern(
                turnLengthSteps: 4,
                activeSteps: new[] { true, false, true, false },
                selectionScores: new[] { 1f, 0.1f, 0.8f, 0.2f },
                stepMeta: stepMeta,
                selectedStepIndices: new[] { 0, 2 },
                summary: new SkeletonPatternSummary(
                    activeCount: 2,
                    achievedDensity: 0.5f,
                    sourceOverlapCount: 1,
                    anchorAlignedCount: 1,
                    densityTargetMet: true,
                    usedStochasticTieBreak: false));
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, InstanceFlags);
            Assert.That(method, Is.Not.Null, $"Expected private method '{methodName}' to exist.");
            method.Invoke(target, arguments);
        }
    }
}
