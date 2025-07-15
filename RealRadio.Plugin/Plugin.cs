using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using FishNet;
using HarmonyLib;
using RealRadio.Assets;
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
    public static AssetRegistry? Assets => AssetRegistry.Instance;
    public static AssetBundle AssetBundle { get; private set; } = null!;

    private bool visitedMenu;
    private static AssetRegistry? assets;

    private Harmony? harmony;

    public RealRadioPlugin()
    {
        AssetBundle = LoadAssetBundle();
        Logger.LogInfo($"Loaded asset bundle: {AssetBundle.name}");

        foreach (var path in AssetBundle.GetAllAssetNames())
        {
            Logger.LogInfo($"- Found asset: {path}");
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
            AssetRegistry.Instance = AssetRegistry.LoadFromAssetBundle(AssetBundle);

        AssetRegistry.Register(registry, InstanceFinder.NetworkManager, AssetBundle);
    }

    private void InitFishNet()
    {
        // FishNet normally uses RuntimeInitializeOnLoadMethod to invoke these methods, but those do not get invoked when using a mod loader.
        // So we invoke them manually here.
        var types = typeof(Logger).Assembly.GetExportedTypes().Where(t => t.Namespace == "FishNet.Serializing.Generated");

        Logger.LogInfo($"Found {types.Count()} types to initialize");

        foreach (var type in types)
        {
            MethodInfo method = type.GetMethod("InitializeOnce", BindingFlags.NonPublic | BindingFlags.Static);
            Logger.LogInfo($"Initializing FishNet type: {type.Name} by calling {method}");
            method.Invoke(null, []);
        }
    }

    private void OnAppsCanvasCreated(AppsCanvas canvas)
    {
        var player = canvas.GetComponentInParent<Player>();

        if (player == null || !player.IsLocalPlayer)
        {
            Logger.LogInfo($"Detected AppsCanvas creation from non-local player");
            return;
        }

        PhoneBootstrap.CreateApp(canvas);
    }

    private void OnDanAwake(Dan dan)
    {
        if (Assets == null)
            throw new InvalidOperationException("Assets have not been set");

        foreach (var shopListing in Assets.ShopListings.Listings)
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
            if (Assets == null)
                throw new InvalidOperationException("Assets have not been set");

            Logger.LogInfo("Creating main scene server singletons");
            InstanceFinder.ServerManager.Spawn(Instantiate(Assets.Singletons.OffGridBuildManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(Assets.Singletons.RadioSyncManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(Assets.Singletons.VehicleRadioManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(Assets.Singletons.BuildingRadioManager));
            InstanceFinder.ServerManager.Spawn(Instantiate(Assets.Singletons.UserStationsManager));
        }

        private void CreateMainSceneClientSingletons()
        {
            if (Assets == null)
                throw new InvalidOperationException("Assets have not been set");

            Logger.LogInfo("Creating main scene client singletons");
            Instantiate(Assets.Singletons.RadialMenu);
            Instantiate(Assets.Singletons.Modal);
            Instantiate(Assets.Singletons.GameMusicManager);
            Instantiate(Assets.Singletons.SpeakerConnectionManager);
        }
    }

    private void CreatePersistentSingletons()
    {
        if (Assets == null)
            throw new InvalidOperationException("Assets have not been set");

        Logger.LogInfo("Creating persistent singletons");
        Instantiate(Assets.Singletons.RadioStationManager);
        Instantiate(Assets.Singletons.RadioStationInfoManager);
        Instantiate(Assets.Singletons.YtDlpManager);
        Instantiate(Assets.Singletons.YtDlpUiManager);

        // This should be instantiated last
        Instantiate(Assets.Singletons.APIManager);
    }
}
