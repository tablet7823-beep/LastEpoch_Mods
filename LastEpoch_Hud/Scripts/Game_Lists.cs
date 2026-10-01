using Il2Cpp;

namespace LastEpoch_Hud.Scripts
{
    //1.4 put an InitGuard in front of the scriptable object singletons. get()
    //throws while the game loader has not run yet, where the instance field it
    //replaced simply came back null. The mod polls for these from Update loops
    //that start long before the loader does, and an escaping exception there
    //aborts the rest of the poll, so every read goes through here and gets the
    //null back. Once a list answers it is kept, so the guard is paid once.
    public class Game_Lists
    {
        private static AffixList affix_list = null;
        private static ItemList item_list = null;
        private static CharacterClassList character_class_list = null;
        private static QuestList quest_list = null;
        private static ShrineList shrine_list = null;

        public static AffixList Affixes()
        {
            if (!affix_list.IsNullOrDestroyed()) { return affix_list; }
            try { affix_list = AffixList.get(); } catch { affix_list = null; }
            return affix_list;
        }

        public static ItemList Items()
        {
            if (!item_list.IsNullOrDestroyed()) { return item_list; }
            try { item_list = ItemList.get(); } catch { item_list = null; }
            return item_list;
        }

        public static CharacterClassList CharacterClasses()
        {
            if (!character_class_list.IsNullOrDestroyed()) { return character_class_list; }
            try { character_class_list = CharacterClassList.get(); } catch { character_class_list = null; }
            return character_class_list;
        }

        public static QuestList Quests()
        {
            if (!quest_list.IsNullOrDestroyed()) { return quest_list; }
            try { quest_list = QuestList.get(); } catch { quest_list = null; }
            return quest_list;
        }

        public static ShrineList Shrines()
        {
            if (!shrine_list.IsNullOrDestroyed()) { return shrine_list; }
            try { shrine_list = ShrineList.get(); } catch { shrine_list = null; }
            return shrine_list;
        }
    }
}
