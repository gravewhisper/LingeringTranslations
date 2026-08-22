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
        OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if (newScene != OWScene.SolarSystem && newScene != OWScene.EyeOfTheUniverse) return;
        ModHelper.Console.WriteLine($"Loaded into {newScene}!", MessageType.Success);

    }

    public override object GetApi()
    {
        return new API();
    }

    public class API : ILingeringTranslationsAPI
    {
    }
}