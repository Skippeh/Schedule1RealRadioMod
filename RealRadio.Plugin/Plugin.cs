using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using FishNet;
using HarmonyLib;
using RealRadio.Assets;
using RealRadio.Compnoents.ConsoleCommands;
using RealRadio.Components.UI.Phone;
using RealRadio.Patches;
using ScheduleOne;
using ScheduleOne.NPCs.CharacterClasses;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;

namespace RealRadio.Plugin;

public class RealRadioPlugin
{
    private static AssetRegistry? assets => AssetRegistry.Instance;
    private static AssetBundle assetBundle = null!;

    private bool visitedMenu;

    private Harmony? harmony;

    public RealRadioPlugin()
    {
        assetBundle = LoadAssetBundle();
        Logger.LogDebug($"Loaded asset bundle: {assetBundle.name}");

        foreach (var path in assetBundle.GetAllAssetNames())
        {
            Logger.LogDebug($"- Found asset: {path}");
        }

        harmony = new Harmony("com.skipcast.realradio");
        harmony.PatchAll();

        LoadManagerPatches.InitializeObjectLoaders += Persistence.Persistence.AddObjectInitializers;
        LoadManagerPatches.InitializeItemLoaders += Persistence.Persistence.AddItemInitializers;

        AppsCanvasPatches.CanvasCreated += OnAppsCanvasCreated;
        DanAwakePatch.OnDanAwake += OnDanAwake;
        LandVehicleStartPatch.OnVehicleSpawned += (vehicle) => GameEvents.VehicleSpawned?.Invoke(vehicle);
        MusicTrackPatches.MusicTrackPlay += (track) => GameEvents.MusicTrackPlay?.Invoke(track);
        MusicTrackPatches.MusicTrackToggled += (track, enabled) => GameEvents.MusicTrackToggled?.Invoke(track, enabled);
        RegistryAwakePatch.RegistryAwake += OnRegistryAwake;
        ConsoleAwakePatch.OnPostConsoleAwake += (console) => ConsoleCommandsManager.RegisterCommands();

        if (Registry.Instance != null)
            OnRegistryAwake(Registry.Instance);

        InitFishNet();

        Logger.LogInfo($"Plugin RealRadio is loaded!");

        SceneManager.activeSceneChanged += (oldScene, newScene) =>
        {
            if (newScene.name == "Menu")
            {
                OnEnterMainMenu();
            }

            if (newScene.name == "Main")
            {
                OnMainSceneLoadComplete();
            }
        };
    }

    private void OnRegistryAwake(Registry registry)
    {
        if (AssetRegistry.Instance == null)
            AssetRegistry.Instance = AssetRegistry.LoadFromAssetBundle(assetBundle);

        AssetRegistry.Register(registry, InstanceFinder.NetworkManager, assetBundle);
    }

    private void InitFishNet()
    {
        // FishNet normally uses RuntimeInitializeOnLoadMethod to invoke these methods, but those do not get invoked when using a mod loader.
        // So we invoke them manually here.
        var types = typeof(Logger).Assembly.GetExportedTypes().Where(t => t.Namespace == "FishNet.Serializing.Generated");

        foreach (var type in types)
        {
            MethodInfo method = type.GetMethod("InitializeOnce", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, []);
        }
    }

    private void OnAppsCanvasCreated(AppsCanvas canvas)
    {
        var player = canvas.GetComponentInParent<Player>();

        if (player == null || !player.IsLocalPlayer)
        {
            Logger.LogWarning($"Detected AppsCanvas creation from non-local player, ignoring...");
            return;
        }

        PhoneBootstrap.CreateApp(canvas);
    }

    private void OnDanAwake(Dan dan)
    {
        if (assets == null)
            throw new InvalidOperationException("Assets have not been set");

        foreach (var shopListing in assets.ShopListings.Listings)
        {
            dan.ShopInterface.CreateListingUI(shopListing);
        }
    }

    private AssetBundle LoadAssetBundle()
    {
        string path = GetAssetBundlePath();

        try
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);

            if (assetBundle == null)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Asset bundle file not found", path);
                }
                else
                {
                    throw new Exception("Failed to load asset bundle");
                }
            }

            return assetBundle;
        }
        catch (Exception)
        {
            Logger.LogError("Failed to load asset bundle!");
            throw;
        }
    }

    internal static string GetAssetBundlePath() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "assets");

    private void OnEnterMainMenu()
    {
        if (visitedMenu)
            return;

        visitedMenu = false;
        Logger.LogDebug("Menu scene loaded for the first time");

        visitedMenu = true;
        CreatePersistentSingletons();
    }

    private void OnMainSceneLoadComplete()
    {
        // Create a game object that will initialize the singletons after the network is initialized
        new GameObject("InitNetworkSingletons", typeof(InitNetworkSingletons));
    }

    class InitNetworkSingletons : MonoBehaviour
    {
        void Awake()
        {
            StartCoroutine(WaitForNetwork());
        }

        private IEnumerator WaitForNetwork()
        {
            yield return new WaitUntil(() => InstanceFinder.IsServer || InstanceFinder.IsClient);

            if (InstanceFinder.IsServer)
            {
                CreateMainSceneServerSingletons();
            }

            CreateMainSceneClientSingletons();

            // Destroy the game object after the singletons are created
            Destroy(gameObject);
        }

        private void CreateMainSceneServerSingletons()
        {
            if (assets == null)
                throw new InvalidOperationException("Assets have not been set");

            Logger.LogDebug("Creating main scene server singletons");
            InstanceFinder.ServerManager.Spawn(Instantiate(assets.Singletons.OffGridBuildManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(assets.Singletons.RadioSyncManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(assets.Singletons.VehicleRadioManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(assets.Singletons.BuildingRadioManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(assets.Singletons.UserStationsManager));
        }

        private void CreateMainSceneClientSingletons()
        {
            if (assets == null)
                throw new InvalidOperationException("Assets have not been set");

            Logger.LogDebug("Creating main scene client singletons");
            Instantiate(assets.Singletons.RadialMenu);
            Instantiate(assets.Singletons.Modal);
            Instantiate(assets.Singletons.GameMusicManager);
            Instantiate(assets.Singletons.SpeakerConnectionManager);
        }
    }

    private void CreatePersistentSingletons()
    {
        if (assets == null)
            throw new InvalidOperationException("Assets have not been set");

        Logger.LogDebug("Creating persistent singletons");
        Instantiate(assets.Singletons.RadioStationManager);
        Instantiate(assets.Singletons.RadioStationInfoManager);
        Instantiate(assets.Singletons.YtDlpManager);
        Instantiate(assets.Singletons.YtDlpUiManager);

        // This should be instantiated last
        Instantiate(assets.Singletons.APIManager);
    }
}
