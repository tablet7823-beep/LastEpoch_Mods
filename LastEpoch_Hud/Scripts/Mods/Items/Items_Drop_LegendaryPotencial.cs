using HarmonyLib;
using UnityEngine;
using Il2Cpp;

namespace LastEpoch_Hud.Scripts.Mods.Items
{
    public class Items_Drop_LegendaryPotencial
    {
        public static bool CanRun()
        {
            if ((Hud_Manager.IsPauseOpen()) && (Hud_Manager.Content.OdlForceDrop.enable)) { return false; }
            else if ((Scenes.IsGameScene()) && (!Save_Manager.instance.IsNullOrDestroyed()))
            {
                if (!Save_Manager.instance.data.IsNullOrDestroyed())
                {
                    return Save_Manager.instance.data.Items.Drop.Enable_LegendaryPotencial;
                }
                else { return false; }
            }
            else { return false; }
        }

        [HarmonyPatch(typeof(ItemData), "RollLegendaryPotential")]
        public class rollLegendaryPotential
        {
            [HarmonyPrefix]
            static bool Prefix(ref int __result, ref bool __5)
            {
                if (CanRun())
                {
                    int roll = 0;
                    if (Save_Manager.instance.data.Items.Drop.LegendaryPotencial_Min == Save_Manager.instance.data.Items.Drop.LegendaryPotencial_Max) { roll = (int)Save_Manager.instance.data.Items.Drop.LegendaryPotencial_Max; }
                    else { roll = (int)Random.RandomRange(Save_Manager.instance.data.Items.Drop.LegendaryPotencial_Min, Save_Manager.instance.data.Items.Drop.LegendaryPotencial_Max); }
                    __result = roll;
                    __5 = false;
                    return false;
                }
                else { return true; };
            }
        }
    }
}
