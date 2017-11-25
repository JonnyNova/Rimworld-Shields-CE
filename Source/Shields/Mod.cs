using System.Reflection;
using Harmony;
using Verse;

namespace FrontierDevelopments.Shields.CE
{
    public class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            var harmony = HarmonyInstance.Create("frontierdevelopments.shields.ce");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("FronterDevelopments Shields CE :: Loaded");
        }
    }
}