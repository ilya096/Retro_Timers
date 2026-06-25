using NUnit.Framework;
using RetroTimers.TimeRewind;

public sealed class TimeRewindRulesTests
{
    [Test]
    public void CalculatePlayerControlDelay_UsesCompletedRewindCount()
    {
        Assert.AreEqual(0f, TimeRewindRules.CalculatePlayerControlDelay(3f, 0));
        Assert.AreEqual(3f, TimeRewindRules.CalculatePlayerControlDelay(3f, 1));
        Assert.AreEqual(6f, TimeRewindRules.CalculatePlayerControlDelay(3f, 2));
    }

    [Test]
    public void HasEnoughActiveTime_AllowsThresholdExactly()
    {
        Assert.IsTrue(TimeRewindRules.HasEnoughActiveTime(10f, 9f, 1f));
        Assert.IsFalse(TimeRewindRules.HasEnoughActiveTime(10f, 9.1f, 1f));
    }

    [Test]
    public void CanRequestManualRewind_RequiresAliveActiveLevelBeforeDeathIsFixed()
    {
        Assert.IsTrue(TimeRewindRules.CanRequestManualRewind(levelEnded: false, controlledActorAlive: true, deathAlreadyFixed: false));
        Assert.IsFalse(TimeRewindRules.CanRequestManualRewind(levelEnded: true, controlledActorAlive: true, deathAlreadyFixed: false));
        Assert.IsFalse(TimeRewindRules.CanRequestManualRewind(levelEnded: false, controlledActorAlive: false, deathAlreadyFixed: false));
        Assert.IsFalse(TimeRewindRules.CanRequestManualRewind(levelEnded: false, controlledActorAlive: true, deathAlreadyFixed: true));
    }
}
