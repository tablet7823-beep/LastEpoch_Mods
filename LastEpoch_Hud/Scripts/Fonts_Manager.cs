using HarmonyLib;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LastEpoch_Hud.Scripts
{
    //Korean is hard to read in the hud's bundled font and in the game's own, so every
    //TextMeshPro label is pointed at Pretendard instead.
    //
    //The font ships beside the mod rather than being installed into Windows, because
    //TMP can build a dynamic font asset straight from a file: the atlas is filled on
    //demand with whatever glyphs actually get drawn, so none of the 11,172 hangul
    //syllables are paid for up front. Deleting the ttf turns all of this off and the
    //original fonts come back.
    public class Fonts_Manager
    {
        public const string file_name = "Pretendard-Regular.ttf";

        private static TMP_FontAsset font = null;
        private static bool load_attempted = false;
        private static int next_sweep_frame = 0;
        private static int quiet_sweeps = 0;

        private static TMP_FontAsset Load()
        {
            if (!font.IsNullOrDestroyed()) { return font; }
            if (load_attempted) { return null; }
            load_attempted = true;

            string path = Application.dataPath + "/../Mods/" + Main.mod_name + "/Fonts/" + file_name;
            if (!System.IO.File.Exists(path))
            {
                Main.logger_instance?.Msg("Fonts : " + file_name + " is not installed, keeping the original fonts");
                return null;
            }

            try
            {
                //90pt sampling into a 1024 atlas keeps hangul readable at hud sizes without
                //paying for a 4k texture, and a second page is added if the first fills up.
                font = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA,
                                                     1024, 1024, AtlasPopulationMode.Dynamic, true);
            }
            catch (System.Exception ex)
            {
                Main.logger_instance?.Error("Fonts : Pretendard could not be read : " + ex);
                return null;
            }

            if (font.IsNullOrDestroyed())
            {
                Main.logger_instance?.Error("Fonts : Pretendard could not be read");
                return null;
            }

            font.name = "Pretendard-Regular SDF";
            Object.DontDestroyOnLoad(font);
            Main.logger_instance?.Msg("Fonts : Pretendard loaded from " + file_name);
            return font;
        }

        //Never loads. A label that enables before the sweep has run keeps its own font
        //until the sweep picks it up, which avoids building the atlas while the client
        //is still coming up.
        public static void Apply(TMP_Text text)
        {
            if (font.IsNullOrDestroyed()) { return; }
            if (text.IsNullOrDestroyed()) { return; }
            if (text.font == font) { return; }
            text.font = font;
        }

        //Catches the labels that already existed. A single pass is not enough: the hud
        //update starts running while the client is still loading, when there is no ui to
        //convert yet. So this keeps going until a few passes in a row find nothing, and
        //then stops - anything built after that is caught by OnEnable. Walking every
        //loaded object is expensive, hence twice a second at most.
        public static void Sweep()
        {
            if (quiet_sweeps >= 3) { return; }
            if (Time.frameCount < next_sweep_frame) { return; }
            next_sweep_frame = Time.frameCount + 120;

            if (Load().IsNullOrDestroyed()) { quiet_sweeps = 3; return; }

            int converted = 0;
            foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (text.IsNullOrDestroyed()) { continue; }
                if (text.font == font) { continue; }
                text.font = font;
                converted++;
            }

            if (converted == 0) { quiet_sweeps++; return; }
            quiet_sweeps = 0;
            Main.logger_instance?.Msg("Fonts : moved " + converted + " label(s) to Pretendard");
        }

        //Everything the game builds afterwards is caught as it comes up, so panels that
        //open later do not need another sweep.
        [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
        public class TextMeshProUGUI_OnEnable
        {
            [HarmonyPostfix]
            static void Postfix(TextMeshProUGUI __instance) { Apply(__instance); }
        }

        [HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
        public class TextMeshPro_OnEnable
        {
            [HarmonyPostfix]
            static void Postfix(TextMeshPro __instance) { Apply(__instance); }
        }
    }
}
