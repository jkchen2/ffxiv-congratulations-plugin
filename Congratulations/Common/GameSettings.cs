using Dalamud.Game.Config;

namespace Congratulations.Common;

public static class GameSettings
{
    public static float GetEffectiveSfxVolume()
    {
        if (IsChannelMuted(SystemConfigOption.IsSndSe) || IsChannelMuted(SystemConfigOption.IsSndMaster))
        {
            return 0;
        }

        if (Service.GameConfig.TryGet(SystemConfigOption.SoundSe, out uint seVolume) &&
            Service.GameConfig.TryGet(SystemConfigOption.SoundMaster, out uint masterVolume))
        {
            return seVolume / 100f * (masterVolume / 100f);
        }

        return 0;
    }

    private static bool IsChannelMuted(SystemConfigOption option)
    {
        return Service.GameConfig.TryGet(option, out bool value) && value;
    }
}
