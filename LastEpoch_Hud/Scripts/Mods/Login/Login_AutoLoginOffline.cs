using HarmonyLib;
using Il2CppLE.UI.Login.UnityUI;
using UnityEngine.UI;

namespace LastEpoch_Hud.Scripts.Mods.Login
{
    public class Login_AutoLoginOffline
    {
        public static bool CanRun()
        {
            bool r = false;
            if (!Save_Manager.instance.IsNullOrDestroyed())
            {
                if (!Save_Manager.instance.data.IsNullOrDestroyed())
                {
                    r = Save_Manager.instance.data.Login.Enable_AutoLoginOffline;
                }
            }

            return r;
        }

        [HarmonyPatch(typeof(LandingZonePanel), "OnOnEnable")]
        public class LandingZonePanel_OnOnEnable
        {
            [HarmonyPostfix]
            static void Postfix(LandingZonePanel __instance)
            {
                if (!CanRun()) { return; }
                //Calling OnPlayOfflineClicked() straight from OnEnable leaves the panel
                //blank: ShowActivity() hides the buttons, and the advance it kicks off
                //dies because the panel is not wired up yet. Press the button the player
                //would press instead, once it is actually pressable.
                MelonLoader.MelonCoroutines.Start(ClickPlayOffline(__instance));
            }

            static System.Collections.IEnumerator ClickPlayOffline(LandingZonePanel panel)
            {
                for (int frame = 0; frame < 600; frame++)
                {
                    yield return null;

                    if (panel.IsNullOrDestroyed()) { yield break; }
                    if (!CanRun()) { yield break; }

                    Button button = panel.playOfflineButton;
                    if (button.IsNullOrDestroyed()) { continue; }
                    if (!button.gameObject.active) { continue; }
                    if (!button.interactable) { continue; }

                    button.onClick.Invoke();
                    yield break;
                }
                Main.logger_instance?.Warning("Fix : the offline play button never became pressable");
            }
        }

        [HarmonyPatch(typeof(LandingZonePanel), "OnPlayOnlineClicked")]
        public class LandingZonePanel_BlockOnline
        {
            [HarmonyPrefix]
            static bool Prefix() => !CanRun();
        }

        [HarmonyPatch(typeof(Il2Cpp.CharacterSelect), "SwitchOnlineOffline")]
        public class CharacterSelect_BlockSwitch
        {
            [HarmonyPrefix]
            static bool Prefix() => !CanRun();
        }
    }
}
