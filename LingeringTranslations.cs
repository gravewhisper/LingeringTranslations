using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;

namespace LingeringTranslations;

public class LingeringTranslations : ModBehaviour
{
    public static LingeringTranslations Instance;

    public void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        var harmony = new Harmony("MegaPiggy.LingeringTranslations");

        harmony.PatchAll(typeof(Patches));
        harmony.PatchAll(typeof(TranslationDiscoveryPatch));

        if (ModHelper.Interaction.ModExists("xen.NewHorizons"))
        {
            ModHelper.Console.WriteLine(
                "New Horizons detected, applying compatibility patches.",
                MessageType.Info
            );

            harmony.PatchAll(typeof(NHPatches));
            harmony.PatchAll(typeof(NHPatches.DetailBuilderPatches));
        }

        OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if (newScene != OWScene.SolarSystem && newScene != OWScene.EyeOfTheUniverse) return;
        ModHelper.Console.WriteLine($"Loaded into {newScene}!", MessageType.Success);

        LingeringTranslationsData.Load();
    }

    public override object GetApi()
    {
        return new API();
    }

    public class API : ILingeringTranslationsAPI
    {
        public void RegisterTranslationSource(
            NomaiText nomaiText,
            string modUniqueName,
            string sourceId) =>
            TranslationUtils.RegisterTranslationSource(
                nomaiText, modUniqueName, sourceId
            );
    }
}