using System;
using System.Linq;
using System.Reflection.Emit;
using YamlDotNet.Serialization;

namespace Celeste.Mod.SoundRandomizer;

public class SoundRandomizerSettings : EverestModuleSettings
{
    public enum RandomizationMode
    {
        Random,
        GroupByEventCategory,
    }

    [YamlIgnore]
    public bool Enabled { get; set; }
    public bool DeterministicRandomness { get; set; } = true;
    public RandomizationMode Randomization { get; set; } = RandomizationMode.GroupByEventCategory;

    #region Menu items

    // ReSharper disable NotAccessedField.Local
    private TextMenu.SubHeader WarningHeader;
    private TextMenu.OnOff EnabledToggle;
    private TextMenu.OnOff DeterministicRandomnessToggle;
    private TextMenu.Option<RandomizationMode> RandomizationSlider;
    private TextMenu.Button ResetCacheButton;
    // ReSharper restore NotAccessedField.Local

    #endregion

    internal void CreateSettingsMenu(TextMenu menu, bool inGame)
    {
        menu.Add(WarningHeader = new NarrowSubHeader(Localize("Warning")));

        menu.Add(EnabledToggle = new TextMenu.OnOff(Localize("Enabled"), Enabled));
        EnabledToggle.Change(SetEnabled);

        menu.Add(DeterministicRandomnessToggle = new TextMenu.OnOff(
            Localize("DeterministicRandomness"), DeterministicRandomness));
        DeterministicRandomnessToggle.Change(SetDeterministicRandomness);

        menu.Add(RandomizationSlider = new TextMenu.Option<RandomizationMode>(Localize("Randomization")));
        RandomizationSlider.Add(
            Localize(nameof(RandomizationMode.Random)),
            RandomizationMode.Random,
            Randomization == RandomizationMode.Random);
        RandomizationSlider.Add(
            Localize(nameof(RandomizationMode.GroupByEventCategory)),
            RandomizationMode.GroupByEventCategory,
            Randomization == RandomizationMode.GroupByEventCategory);
        RandomizationSlider.Change(SetRandomization);

        menu.Add(ResetCacheButton = new TextMenu.Button(Localize("ResetCache")));
        ResetCacheButton.Pressed(SoundRandomizerModule.ClearCache);
    }

    private static void SetEnabled(bool newValue)
        => SoundRandomizerModule.Settings.Enabled = newValue;

    private static void SetDeterministicRandomness(bool newValue)
        => SoundRandomizerModule.Settings.DeterministicRandomness = newValue;

    private static void SetRandomization(RandomizationMode newValue)
        => SoundRandomizerModule.Settings.Randomization = newValue;


    internal const string DialogPrefix = "SnipUndercover_SoundRandomizer_";
    internal static string Localize(string id)
    {
        string dialogId = DialogPrefix + id;
        return Dialog.Has(dialogId)
            ? Dialog.Clean(dialogId)
            : id.SpacedPascalCase();
    }

    private class NarrowSubHeader(string label) : TextMenu.SubHeader(label, topPadding: false)
    {
        public override float LeftWidth() => 0;
    }
}
