using Game.Players;
using Game.Players.Input;
using NUnit.Framework;
using UnityEngine;

namespace Core.Tests
{
    public class ServerInputPlayoutPolicyTests
    {
        [Test]
        public void RepeatedInputKeepsMovementAndClearsTransientActions()
        {
            PlayerInputData last_input = CreateActionInput();

            PlayerInputData result = ServerInputPlayoutPolicy.CreatePredictedInput(
                last_input,
                true,
                101,
                1,
                3,
                4,
                out ServerInputSource source);

            Assert.That(source, Is.EqualTo(ServerInputSource.Repeated));
            Assert.That(result.Tick, Is.EqualTo(101));
            Assert.That(result.Move, Is.EqualTo(last_input.Move));
            Assert.That(result.Look, Is.EqualTo(Vector2.zero));
            Assert.That(result.IsSprintHeld, Is.True);
            Assert.That(result.IsCrouchHeld, Is.True);
            AssertTransientActionsCleared(result);
        }

        [Test]
        public void MissingInputDecaysThenBecomesNeutral()
        {
            PlayerInputData last_input = CreateActionInput();

            PlayerInputData decayed = ServerInputPlayoutPolicy.CreatePredictedInput(
                last_input,
                true,
                105,
                5,
                3,
                4,
                out ServerInputSource decayed_source);
            PlayerInputData neutral = ServerInputPlayoutPolicy.CreatePredictedInput(
                last_input,
                true,
                108,
                8,
                3,
                4,
                out ServerInputSource neutral_source);

            Assert.That(decayed_source, Is.EqualTo(ServerInputSource.Decayed));
            Assert.That(decayed.Move, Is.EqualTo(last_input.Move * 0.5f));
            Assert.That(neutral_source, Is.EqualTo(ServerInputSource.Neutral));
            Assert.That(neutral.Move, Is.EqualTo(Vector2.zero));
            Assert.That(neutral.IsSprintHeld, Is.False);
            Assert.That(neutral.IsCrouchHeld, Is.False);
        }

        [Test]
        public void CatchUpNeverCoalescesOneShotActions()
        {
            PlayerInputData continuous = default;
            continuous.Move = Vector2.one;

            Assert.That(ServerInputPlayoutPolicy.CanCoalesce(continuous), Is.True);

            continuous.IsShootPressed = true;
            Assert.That(ServerInputPlayoutPolicy.CanCoalesce(continuous), Is.False);
            continuous.IsShootPressed = false;
            continuous.IsJumpPressed = true;
            Assert.That(ServerInputPlayoutPolicy.CanCoalesce(continuous), Is.False);
            continuous.IsJumpPressed = false;
            continuous.RequestedWeaponSlot = WeaponSlot.Pistol;
            Assert.That(ServerInputPlayoutPolicy.CanCoalesce(continuous), Is.False);
        }

        private static PlayerInputData CreateActionInput()
        {
            return new PlayerInputData
            {
                Tick = 100,
                ShotViewTick = 91,
                Move = new Vector2(0.5f, 1f),
                Look = new Vector2(2f, -1f),
                IsShootPressed = true,
                IsShootHeld = true,
                ShotSequence = 4,
                IsReloadPressed = true,
                RequestedWeaponSlot = WeaponSlot.Pistol,
                IsJumpPressed = true,
                IsJumpHeld = true,
                IsSprintHeld = true,
                IsCrouchHeld = true,
            };
        }

        private static void AssertTransientActionsCleared(PlayerInputData input)
        {
            Assert.That(input.IsShootPressed, Is.False);
            Assert.That(input.IsShootHeld, Is.False);
            Assert.That(input.ShotSequence, Is.Zero);
            Assert.That(input.ShotViewTick, Is.EqualTo(-1));
            Assert.That(input.IsReloadPressed, Is.False);
            Assert.That(input.RequestedWeaponSlot, Is.EqualTo(WeaponSlot.None));
            Assert.That(input.IsJumpPressed, Is.False);
            Assert.That(input.IsJumpHeld, Is.False);
        }
    }
}
