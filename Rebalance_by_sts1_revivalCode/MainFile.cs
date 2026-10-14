using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Rebalance_by_sts1_revival"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        
        Harmony harmony = new(ModId);

        harmony.PatchAll();

        // 羽化：把基础游戏的事件卡提升为无色稀有卡（稀有度由 MetamorphosisPatch 改）。
        // [Pool] 特性只能标在自己的卡类上，基础卡入池必须走这个 API，
        // 且必须在游戏初始化完成前调用，否则 ModHelper 会抛「太晚了」。
        ModHelper.AddModelToPool(typeof(ColorlessCardPool), typeof(Metamorphosis));

        // 强撑：机器人罕见 → 战士罕见。机器人侧的移除在 CardPoolPatches.Defect。
        ModHelper.AddModelToPool(typeof(IroncladCardPool), typeof(FightThrough));

        // A patch class naming a member its type doesn't *declare* throws "Undefined target method",
        // which aborts the rest of PatchAll — silently, since the loader still reports the mod as
        // initialized. Logging what actually bound is the cheapest way to catch that.
        foreach (var m in harmony.GetPatchedMethods())
            Logger.Info($"patched: {m.DeclaringType?.Name}.{m.Name}");
    }
}
