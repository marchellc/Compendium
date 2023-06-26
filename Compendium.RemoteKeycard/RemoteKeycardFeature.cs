using Compendium.Features;

using helpers.Configuration.Converters.Yaml;
using helpers.Configuration.Ini;
using helpers.Patching;

namespace Compendium.RemoteKeycard
{
    public class RemoteKeycardFeature : IFeature
    {
        private IniConfigHandler m_Config;

        public string Name => "Remote Keycard";

        public void Load()
        {
            Plugin.Info($"Loading configuration ..");

            new IniConfigBuilder()
                .WithConverter<YamlConfigConverter>()
                .WithGlobalPath($"{FeatureManager.DirectoryPath}/remote_keycard.ini")
                .WithType(typeof(RemoteKeycardLogic), null)
                .Register(ref m_Config);

            m_Config.Read();

            Plugin.Info($"Configuration loaded.");

            Plugin.Info("Patching ..");

            PatchManager.Patch(
                RemoteKeycardPatches.DoorInteractionPatch, 
                RemoteKeycardPatches.LockerInteractionPatch, 
                RemoteKeycardPatches.GeneratorInteractionPatch, 
                RemoteKeycardPatches.WarheadButtonPatch);
            
            Plugin.Info($"Patched!\n" +
                $"{RemoteKeycardPatches.DoorInteractionPatch.Name}\n" +
                $"{RemoteKeycardPatches.GeneratorInteractionPatch.Name}\n" +
                $"{RemoteKeycardPatches.WarheadButtonPatch.Name}\n" +
                $"{RemoteKeycardPatches.LockerInteractionPatch.Name}");
        }

        public void Reload()
        {
            Plugin.Info($"Reloading ..");
            m_Config?.Read();
            Plugin.Info("Reloaded!");
        }

        public void Unload()
        {
            m_Config?.Save();
            m_Config = null;

            PatchManager.Unpatch(
                RemoteKeycardPatches.DoorInteractionPatch, 
                RemoteKeycardPatches.LockerInteractionPatch, 
                RemoteKeycardPatches.GeneratorInteractionPatch, 
                RemoteKeycardPatches.WarheadButtonPatch);

            Plugin.Info("Unloaded!");
        }
    }
}