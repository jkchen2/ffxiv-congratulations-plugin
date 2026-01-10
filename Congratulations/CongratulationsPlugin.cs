using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Congratulations.Windows;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace Congratulations
{
    public sealed class CongratulationsPlugin : IDalamudPlugin
    {
        public string Name => "Congratulations!";
        private const string CommandName = "/congratsconfig";

        public Configuration Configuration { get; init; }

        public readonly WindowSystem WindowSystem = new("Congratulations");
        private readonly ConfigWindow configWindow;

        private int partySizeAtPop = 1;
        private int partySizeAtClear = 1;
        private short commendationCountAtClear;
        private bool announceOnNextZoneChange;

        public CongratulationsPlugin(IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            this.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(Service.PluginInterface);
            Configuration.Save();

            configWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(configWindow);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Opens the Congratulations configuration window"
            });
            Service.PluginInterface.UiBuilder.Draw += DrawUserInterface;
            Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigWindow;

            Service.ClientState.CfPop += OnCfPop;
            Service.DutyState.DutyCompleted += OnDutyCompleted;
            Service.ClientState.TerritoryChanged += OnTerritoryChange;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            Service.CommandManager.RemoveHandler(CommandName);

            Service.ClientState.CfPop -= OnCfPop;
            Service.DutyState.DutyCompleted -= OnDutyCompleted;
            Service.ClientState.TerritoryChanged -= OnTerritoryChange;
        }

        private void Reset()
        {
            partySizeAtPop = 1;
            partySizeAtClear = 1;
            commendationCountAtClear = 0;
            announceOnNextZoneChange = false;
        }

        private void OnCfPop(ContentFinderCondition obj)
        {
            partySizeAtPop = GetCurrentPartySize();
            Service.PluginLog.Debug($"Popped duty with party size: {partySizeAtPop}");
        }

        private void OnDutyCompleted(object? sender, ushort e)
        {
            partySizeAtClear = GetCurrentPartySize();
            commendationCountAtClear = GetCurrentCommendationCount();
            announceOnNextZoneChange = true;
            Service.PluginLog.Debug($"Completed duty with party size {partySizeAtClear} (partySizeAtPop={partySizeAtPop} commendationCountAtClear={commendationCountAtClear})");
        }

        private void OnTerritoryChange(ushort obj)
        {
            if (!announceOnNextZoneChange) return;

            var numberOfMatchMadePlayers = partySizeAtClear - partySizeAtPop;
            var commendsObtained = GetCurrentCommendationCount() - commendationCountAtClear;
            var normalizedCommends = (float)commendsObtained / numberOfMatchMadePlayers;

            Service.PluginLog.Debug($"Gained {commendsObtained} commendations from {numberOfMatchMadePlayers} match-made players (normalized: {normalizedCommends})");

            Reset();

            if (commendsObtained >= 7)
            {
                PlaySoundConfig(Configuration.AllSevenInAFullParty);
            }
            else
            {
                switch (normalizedCommends)
                {
                    case > 2 / 3f:
                        PlaySoundConfig(Configuration.ThreeThirds);
                        break;
                    case > 1 / 3f:
                        PlaySoundConfig(Configuration.TwoThirds);
                        break;
                    case > 0:
                        PlaySoundConfig(Configuration.OneThird);
                        break;
                }
            }
        }

        private static void PlaySoundConfig(Configuration.SubConfiguration config)
        {
            if (config.PlaySound)
            {
                Service.PluginLog.Debug($"Playing sound: {config.SectionTitle}");
                SoundEngine.PlaySound(config.GetFilePath(), config.ApplySfxVolume, config.Volume * 0.01f);
            }
        }

        private int GetCurrentPartySize()
        {
            // PartyList.Length returns 0 if the player is alone,
            // so we change it to 1 manually if that's the case.
            return Math.Max(Service.PartyList.Length, 1);
        }

        private static unsafe short GetCurrentCommendationCount()
        {
            return PlayerState.Instance()->PlayerCommendations;
        }

        private void OnConfigCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            configWindow.IsOpen = true;
        }

        private void DrawUserInterface()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigWindow()
        {
            configWindow!.IsOpen = true;
        }
    }
}
