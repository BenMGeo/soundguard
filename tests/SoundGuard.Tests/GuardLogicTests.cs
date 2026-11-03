using SoundGuard;
using Xunit;

namespace SoundGuard.Tests;

public class GuardLogicTests
{
    private sealed class Fakes : IAudioQuery, IDefaultDeviceSetter, ISpeakerMuter, IGuardStore
    {
        public string? Default;
        public HashSet<string> Active = new();
        public HashSet<string> Speakers = new() { "speakers" };
        public string? Stored;
        public bool IsMuted { get; private set; }
        public List<string> SetCalls = new();

        public string? GetDefaultRenderId() => Default;
        public bool IsActive(string id) => Active.Contains(id);
        public bool IsMuteCandidate(string id) => Speakers.Contains(id);
        public void SetDefault(string id) { SetCalls.Add(id); Default = id; }
        public void Mute() { IsMuted = true; }
        public void Restore() { IsMuted = false; }
        public string? Load() => Stored;
        public void Save(string id) => Stored = id;

        public void Plug(string id) => Active.Add(id);
        public void Unplug(string id, string fallbackDefault) { Active.Remove(id); Default = fallbackDefault; }
    }

    private static (GuardLogic, Fakes) Create(bool restoreDefault = true, string? stored = null)
    {
        var f = new Fakes { Stored = stored };
        f.Plug("speakers");
        return (new GuardLogic(f, f, f, f, () => restoreDefault), f);
    }

    [Fact]
    public void FirstRun_AdoptsDefaultAndPersistsIt()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        Assert.Equal("headsetA", f.Stored);
        Assert.False(f.IsMuted);
    }

    [Fact]
    public void GuardedLost_FallbackToSpeakers_Mutes()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        f.Unplug("headsetA", "speakers");
        logic.Evaluate();
        Assert.True(f.IsMuted);
        Assert.Equal("headsetA", f.Stored);
    }

    [Fact]
    public void GuardedLost_FallbackToOtherHeadset_NoMuteAndAdopts()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Plug("headsetB"); f.Default = "headsetA";
        logic.Evaluate();
        f.Unplug("headsetA", "headsetB");
        logic.Evaluate();
        Assert.False(f.IsMuted);
        Assert.Equal("headsetB", f.Stored);
    }

    [Fact]
    public void GuardedReturns_UnmutesAndSetsDefault()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        f.Unplug("headsetA", "speakers");
        logic.Evaluate();
        f.Plug("headsetA");
        logic.Evaluate();
        Assert.False(f.IsMuted);
        Assert.Equal(new[] { "headsetA" }, f.SetCalls);
        Assert.Equal("headsetA", f.Stored);
    }

    [Fact]
    public void GuardedReturns_RestoreDefaultDisabled_AdoptsCurrentDefault()
    {
        var (logic, f) = Create(restoreDefault: false);
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        f.Unplug("headsetA", "speakers");
        logic.Evaluate();
        f.Plug("headsetA");
        logic.Evaluate();
        Assert.False(f.IsMuted);
        Assert.Empty(f.SetCalls);
        Assert.Equal("speakers", f.Stored);
    }

    [Fact]
    public void DeliberateSwitchToSpeakers_AdoptsAndDoesNotMute()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        f.Default = "speakers"; // headsetA still active
        logic.Evaluate();
        Assert.False(f.IsMuted);
        Assert.Equal("speakers", f.Stored);
    }

    [Fact]
    public void ColdStart_GuardedAlreadyGone_Mutes()
    {
        var (logic, f) = Create(stored: "headsetA");
        f.Default = "speakers";
        logic.Evaluate();
        Assert.True(f.IsMuted);
    }

    [Fact]
    public void RepeatedEvents_AreIdempotent()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        f.Unplug("headsetA", "speakers");
        logic.Evaluate();
        logic.Evaluate();
        Assert.True(f.IsMuted);
        Assert.Empty(f.SetCalls);
    }

    [Fact]
    public void Pause_RestoresAndResumeMutesAgain()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        f.Unplug("headsetA", "speakers");
        logic.Evaluate();
        logic.SetPaused(true);
        Assert.False(f.IsMuted);
        logic.SetPaused(false);
        Assert.True(f.IsMuted);
    }

    [Fact]
    public void Shutdown_RestoresMute()
    {
        var (logic, f) = Create();
        f.Plug("headsetA"); f.Default = "headsetA";
        logic.Evaluate();
        f.Unplug("headsetA", "speakers");
        logic.Evaluate();
        logic.Shutdown();
        Assert.False(f.IsMuted);
    }
}
