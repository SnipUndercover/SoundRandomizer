using System;
using System.Collections.Generic;
using FMOD.Studio;
using Monocle;

namespace Celeste.Mod.SoundRandomizer;

public class SoundRandomizerModule : EverestModule
{
    private const string LogId = nameof(SoundRandomizer);

    public override Type SettingsType => typeof(SoundRandomizerSettings);

    public static SoundRandomizerModule Instance { get; private set; }
    public static SoundRandomizerSettings Settings => (SoundRandomizerSettings)Instance._Settings;

    private static readonly List<string> CachedEventPaths = [];
    private static readonly Dictionary<string, List<string>> EventNamesByCategory = [];
    private static readonly Dictionary<string, string> RandomEventMappings = [];

    private static Random Rng;

    public SoundRandomizerModule()
    {
        Instance = this;
    #if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(SoundRandomizer), LogLevel.Verbose);
    #else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(SoundRandomizer), LogLevel.Info);
    #endif
    }

    public override void Load()
    {
        On.Celeste.Audio.Init += Audio_Init;
        On.Celeste.Audio.GetEventDescription += Audio_GetEventDescription;

        Rng = new Random();
    }

    public override void Unload()
    {
        On.Celeste.Audio.Init -= Audio_Init;
        On.Celeste.Audio.GetEventDescription -= Audio_GetEventDescription;
        ClearCache();
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
    {
        base.CreateModMenuSectionHeader(menu, inGame, snapshot);
        Settings.CreateSettingsMenu(menu, inGame);
    }

    internal static void ClearCache()
    {
        RandomEventMappings.Clear();
    }

    private static void Audio_Init(On.Celeste.Audio.orig_Init orig)
    {
        orig();

        // TODO: i don't support event names that aren't there at load-time, and i don't feel like
        // making extra hooks just to support cache invalidation for a dumb mod like this
        CachedEventPaths.AddRange(Audio.cachedPaths.Values);

        foreach (string path in CachedEventPaths)
        {
            if (!path.StartsWith("event:/"))
                continue;

            string eventCategory = GetEventCategory(path);

            if (!EventNamesByCategory.TryGetValue(eventCategory, out List<string> events))
                events = EventNamesByCategory[eventCategory] = [];

            events.Add(path);
        }
    }

    private static EventDescription Audio_GetEventDescription(
        On.Celeste.Audio.orig_GetEventDescription orig,
        string path)
    {
        if (!Settings.Enabled)
            return orig(path);

        if (!path.StartsWith("event:/"))
            return orig(path);

        if (Settings.DeterministicRandomness && RandomEventMappings.TryGetValue(path, out string randomEvent))
            return orig(randomEvent);

        randomEvent = Settings.Randomization switch {
            SoundRandomizerSettings.RandomizationMode.Random =>
                Rng.Choose(CachedEventPaths),
            SoundRandomizerSettings.RandomizationMode.GroupByEventCategory =>
                EventNamesByCategory.TryGetValue(GetEventCategory(path), out List<string> events)
                    ? Rng.Choose(events)
                    : path,
            _ =>
                throw new NotImplementedException($"Randomization mode \"{Settings.Randomization}\" not implemented."),
        };

        if (Settings.DeterministicRandomness)
        {
            Logger.Verbose(LogId, $"Mapped \"{path}\" to \"{randomEvent}\", adding to cache.");
            RandomEventMappings[path] = randomEvent;
        }
        else
        {
            Logger.Verbose(LogId, $"Mapped \"{path}\" to \"{randomEvent}\".");
        }

        return orig(randomEvent);
    }

    private static string GetEventCategory(string eventPath)
    {
        int firstSlash = eventPath.IndexOf('/') + 1;
        if (firstSlash < 0)
            return "";

        int secondSlash = eventPath.IndexOf('/', firstSlash);
        if (secondSlash < 0)
            return "";

        return eventPath[firstSlash..secondSlash];
    }
}
