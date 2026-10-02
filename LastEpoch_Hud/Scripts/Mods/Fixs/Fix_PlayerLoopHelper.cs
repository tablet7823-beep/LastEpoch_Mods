using HarmonyLib;

namespace LastEpoch_Hud.Scripts.Mods.Fixs
{
    public class Fix_PlayerLoopHelper
    {
        //This used to skip AddAction entirely until the hud was up, to dodge an
        //exception thrown when the player is not set yet.
        //
        //1.4 runs the client's own loading on UniTask, so AddAction arrives long before
        //any mod object exists and skipping it throws those registrations away. The
        //loading continuations never run again and the client sits in SystemLoading
        //forever - eight dropped registrations were enough to strand it.
        //
        //So the call goes through and the exception it was guarding against is swallowed
        //here instead, which keeps the original intent without eating the game's work.
        [HarmonyPatch(typeof(Il2CppCysharp.Threading.Tasks.PlayerLoopHelper), "AddAction")]
        public class Il2CppCysharp_Threading_Tasks_PlayerLoopHelper_AddAction
        {
            [HarmonyFinalizer]
            static System.Exception Finalizer(System.Exception __exception)
            {
                if (__exception != null)
                {
                    Main.logger_instance?.Warning("Fix : PlayerLoopHelper.AddAction() threw, ignored");
                }
                return null;
            }
        }
    }
}
