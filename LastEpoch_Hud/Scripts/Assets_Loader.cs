using Il2Cpp;
using Il2CppLE.AssetBundles;
using UnityEngine;
using UnityEngine.UI;

namespace LastEpoch_Hud.Scripts
{
    //LE 1.4 moved ability icons, ability prefabs and item sprites behind addressable
    //soft refs. The mod reads them from Harmony patches and Hud code that cannot
    //await, so a lookup blocks for the load. The load ref is kept afterwards, both so
    //the asset is not unloaded under us and so the next lookup costs nothing.
    public class Assets_Loader
    {
        private static readonly System.Collections.Generic.Dictionary<string, LoadRef<Sprite>> sprite_refs = new System.Collections.Generic.Dictionary<string, LoadRef<Sprite>>();
        private static readonly System.Collections.Generic.Dictionary<string, LoadRef<GameObject>> prefab_refs = new System.Collections.Generic.Dictionary<string, LoadRef<GameObject>>();

        private static string Key(SoftRef soft_ref) { return soft_ref.Guid.ToString() + "|" + soft_ref.SubPath; }

        public static Sprite LoadSprite(SoftRef<Sprite> soft_ref)
        {
            if (soft_ref == null) { return null; }
            try
            {
                string key = Key(soft_ref);
                LoadRef<Sprite> load_ref = null;
                if (!sprite_refs.TryGetValue(key, out load_ref))
                {
                    load_ref = SoftRefExtensions.CreateLoadRef<Sprite>(soft_ref, "Assets_Loader", 0);
                    if (load_ref == null) { return null; }
                    load_ref.BlockForLoad();
                    sprite_refs[key] = load_ref;
                }
                return load_ref.AssetOrNull;
            }
            catch { Main.logger_instance?.Error("Assets_Loader : sprite load failed"); return null; }
        }

        public static GameObject LoadPrefab(SoftRef<GameObject> soft_ref)
        {
            if (soft_ref == null) { return null; }
            try
            {
                string key = Key(soft_ref);
                LoadRef<GameObject> load_ref = null;
                if (!prefab_refs.TryGetValue(key, out load_ref))
                {
                    load_ref = SoftRefExtensions.CreateLoadRef<GameObject>(soft_ref, "Assets_Loader", 0);
                    if (load_ref == null) { return null; }
                    load_ref.BlockForLoad();
                    prefab_refs[key] = load_ref;
                }
                return load_ref.AssetOrNull;
            }
            catch { Main.logger_instance?.Error("Assets_Loader : prefab load failed"); return null; }
        }

        //The item tooltip builds the sprite soft ref, and it is an instance method now.
        public static Sprite LoadItemSprite(ItemData item, ItemUIContext context)
        {
            if (UITooltipItem.instance.IsNullOrDestroyed()) { return null; }
            return LoadSprite(UITooltipItem.instance.GetItemSprite(item, context));
        }

        //The mod's custom uniques used to return their own icon out of GetItemSprite.
        //That hands back a soft ref now, and a sprite from the mod bundle has no entry
        //in the addressable catalogue to point one at. The icon is painted over the
        //image the tooltip just filled instead - the tooltip keeps one image per item
        //shape and only the matching one is active.
        public static void OverrideItemImage(UITooltipItem tooltip, bool for_comparison, Sprite sprite)
        {
            if (tooltip.IsNullOrDestroyed() || sprite.IsNullOrDestroyed()) { return; }

            Image[] images = for_comparison
                ? new Image[] { tooltip.compareItemImage, tooltip.compareSmallItemImage, tooltip.compareMediumItemImage, tooltip.compareTallMediumItemImage, tooltip.compareLargeItemImage, tooltip.compareSpearItemImage, tooltip.compareWideItemImage }
                : new Image[] { tooltip.itemImage, tooltip.smallItemImage, tooltip.mediumItemImage, tooltip.tallMediumItemImage, tooltip.largeItemImage, tooltip.spearItemImage, tooltip.wideItemImage };

            foreach (Image image in images)
            {
                if (image.IsNullOrDestroyed()) { continue; }
                if (!image.gameObject.active) { continue; }
                image.sprite = sprite;
            }
        }
    }
}
