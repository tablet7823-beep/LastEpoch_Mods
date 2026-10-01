using HarmonyLib;
using Il2Cpp;
using Il2CppLE.AssetBundles;
using UnityEngine;

namespace LastEpoch_Hud.Scripts.Mods.Shrines
{
    public class Shrines_ClickUnlimited
    {
        public static bool CanRun()
        {
            if (!Save_Manager.instance.IsNullOrDestroyed())
            {
                if (!Save_Manager.instance.data.IsNullOrDestroyed()) { return Save_Manager.instance.data.modsNotInHud.Shrines_Unlimited; }
                else { return false; }
            }
            else { return false; }
        }

        static int FindShrineId(GameObject shrine)
        {
            LoadRefComponent load_ref_component = LoadRefComponent.FindInAncestors(shrine);
            if (load_ref_component.IsNullOrDestroyed()) { return -1; }

            LoadRef load_ref = load_ref_component.LoadRef;
            if (load_ref == null) { return -1; }

            ShrineList shrine_list = ShrineList.get();
            if (shrine_list.IsNullOrDestroyed()) { return -1; }

            for (int i = 0; i < shrine_list.entries.Count; i++)
            {
                if (shrine_list.entries[i].prefabSoftRef.Guid == load_ref.Guid) { return i; }
            }
            return -1;
        }

        [HarmonyPatch(typeof(WorldObjectClickListener), "ObjectClick")]
        public class WorldObjectClickListener_ObjectClick
        {
            [HarmonyPostfix]
            static void Postfix(ref WorldObjectClickListener __instance, UnityEngine.GameObject __0, bool __1)
            {
                if (CanRun())
                {
                    if ((__instance.gameObject.name.ToLower().Contains(" shrine")) && (__1 == true))
                    {
                        Vector3 position = __instance.gameObject.transform.position;
                        int id = FindShrineId(__instance.gameObject);
                        Object.Destroy(__instance.gameObject);

                        ShrinesManager shrines_manager = Object.FindObjectOfType<ShrinesManager>();
                        if (shrines_manager.IsNullOrDestroyed()) { Main.logger_instance?.Error("ShrinesManager not Found"); }
                        else if (id < 0) { Main.logger_instance?.Error("Shrine not found in ShrineList"); }
                        else { shrines_manager.PlaceNewShrine(id, position); }
                    }
                }
            }
        }
    }
}
