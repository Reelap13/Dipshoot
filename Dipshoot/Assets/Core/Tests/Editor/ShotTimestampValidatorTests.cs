using Game.Players;
using NUnit.Framework;

namespace Core.Tests
{
    public class ShotTimestampValidatorTests
    {
        [Test]
        public void PreservesValidDisplayedTick()
        {
            ShotTimestampValidation result = ShotTimestampValidator.Validate(
                1000,
                991,
                1004,
                9,
                12,
                0,
                21);

            Assert.That(result.ValidatedViewTick, Is.EqualTo(991));
            Assert.That(result.QueryTick, Is.EqualTo(991));
            Assert.That(result.VisualBackTicks, Is.EqualTo(9));
            Assert.That(result.ServerRewindTicks, Is.EqualTo(13));
            Assert.That(result.WasClamped, Is.False);
        }

        [Test]
        public void ClampsExcessiveClientRewind()
        {
            ShotTimestampValidation result = ShotTimestampValidator.Validate(
                1000,
                980,
                1004,
                9,
                12,
                0,
                21);

            Assert.That(result.ValidatedViewTick, Is.EqualTo(988));
            Assert.That(result.QueryTick, Is.EqualTo(988));
            Assert.That(result.VisualBackTicks, Is.EqualTo(12));
            Assert.That(result.WasClamped, Is.True);
        }

        [Test]
        public void ClampsQueryToAvailableServerHistory()
        {
            ShotTimestampValidation result = ShotTimestampValidator.Validate(
                1285,
                1276,
                1300,
                9,
                12,
                0,
                21);

            Assert.That(result.ValidatedViewTick, Is.EqualTo(1276));
            Assert.That(result.QueryTick, Is.EqualTo(1279));
            Assert.That(result.ServerRewindTicks, Is.EqualTo(21));
            Assert.That(result.WasClamped, Is.True);
        }

        [Test]
        public void AppliesForwardHitRegistrationBias()
        {
            ShotTimestampValidation result = ShotTimestampValidator.Validate(
                1000,
                991,
                1004,
                9,
                12,
                2,
                21);

            Assert.That(result.ValidatedViewTick, Is.EqualTo(991));
            Assert.That(result.QueryTick, Is.EqualTo(993));
            Assert.That(result.BiasTicks, Is.EqualTo(2));
            Assert.That(result.ServerRewindTicks, Is.EqualTo(11));
            Assert.That(result.WasClamped, Is.False);
        }
    }
}
