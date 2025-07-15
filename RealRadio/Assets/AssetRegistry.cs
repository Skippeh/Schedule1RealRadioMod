using System;
using System.Linq;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using RealRadio.Data;
using RealRadio.Helpers;
using ScheduleOne;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI.Shop;
using UnityEngine;

namespace RealRadio.Assets;

[Serializable]
[CreateAssetMenu(fileName = "AssetRegistry", menuName = "RealRadio/ScriptableObjects/Asset Registry/Root", order = 1)]
public class AssetRegistry : ScriptableObject
{
#nullable disable
    public ItemDefinitionAssets ItemDefinitions;
    public SingletonPrefabs Singletons;
    public Prefabs Prefabs;
    public ShopListings ShopListings;
    [NonSerialized] public RadioStation[] DefaultRadioStations;
#nullable enable

    /// <summary>
    /// Load an AssetRegistry from an AssetBundle. Throws an exception if any required assets are missing.
    /// </summary>
    /// <exception cref="AssetRegistryLoadException">Thrown if any required assets are missing.</exception>
    public static AssetRegistry LoadFromAssetBundle(AssetBundle assetBundle)
    {
        var result = assetBundle.LoadAsset<AssetRegistry>("Assets/RealRadio/AssetRegistry.asset");

        if (result == null)
            throw new AssetRegistryLoadException("Failed to find AssetRegistry in asset bundle");

        if (result.ItemDefinitions.RadioTier1 == null)
            throw new AssetRegistryLoadException("ItemDefinitions.RadioTier1 is null");

        if (result.Singletons.OffGridBuildManager == null)
            throw new AssetRegistryLoadException("Singletons.OffGridBuildManager is null");

        if (result.Singletons.RadioStationManager == null)
            throw new AssetRegistryLoadException("Singletons.RadioStationManager is null");

        if (result.Singletons.RadioStationInfoManager == null)
            throw new AssetRegistryLoadException("Singletons.RadioStationInfoManager is null");

        if (result.Singletons.RadioSyncManager == null)
            throw new AssetRegistryLoadException("Singletons.RadioSyncManager is null");

        if (result.Singletons.VehicleRadioManager == null)
            throw new AssetRegistryLoadException("Singletons.VehicleRadioManager is null");

        if (result.Singletons.BuildingRadioManager == null)
            throw new AssetRegistryLoadException("Singletons.BuildingRadioManager is null");

        if (result.Singletons.RadialMenu == null)
            throw new AssetRegistryLoadException("Singletons.RadialMenu is null");

        if (result.Singletons.Modal == null)
            throw new AssetRegistryLoadException("Singletons.Modal is null");

        if (result.Singletons.YtDlpManager == null)
            throw new AssetRegistryLoadException("Singletons.YtDlpManager is null");

        if (result.Singletons.YtDlpUiManager == null)
            throw new AssetRegistryLoadException("Singletons.YtDlpUiManager is null");

        if (result.Singletons.APIManager == null)
            throw new AssetRegistryLoadException("Singletons.APIManager is null");

        if (result.Singletons.GameMusicManager == null)
            throw new AssetRegistryLoadException("Singletons.GameMusicManager is null");

        if (result.Singletons.RadioApp == null)
            throw new AssetRegistryLoadException("Singletons.RadioApp is null");

        if (result.Singletons.UserStationsManager == null)
            throw new AssetRegistryLoadException("Singletons.UserStationsManager is null");

        if (result.Singletons.SpeakerConnectionManager == null)
            throw new AssetRegistryLoadException("Singletons.SpeakerConnectionManager is null");

        if (result.Prefabs.VehicleRadioProxy == null)
            throw new AssetRegistryLoadException("Prefabs.VehicleRadio is null");

        if (result.Prefabs.SelectionArrow == null)
            throw new AssetRegistryLoadException("Prefabs.SelectionArrow is null");

        result.DefaultRadioStations = assetBundle.LoadAllAssets<RadioStation>();

        return result;
    }

    /// <summary>
    /// Register all assets so they work with the game. NOTE: This method may be called multiple times! Each time a new Registry is instantiated.
    /// </summary>
    internal static void Register(Registry registry, NetworkManager networkManager, AssetBundle assetBundle)
    {
        ushort assetBundleHash = assetBundle.Get16BitHash();
        var netPrefabs = networkManager.GetPrefabObjects<SinglePrefabObjects>(assetBundleHash, createIfMissing: true);

        foreach (string assetName in assetBundle.GetAllAssetNames())
        {
            var itemDefinition = assetBundle.LoadAsset<ItemDefinition>(assetName);

            if (itemDefinition != null)
            {
                Plugin.Logger.LogInfo($"Registering ItemDefinition {itemDefinition.ID} from {assetName}");

                if (registry.ItemRegistry.Any(x => x.ID == itemDefinition.ID))
                {
                    Plugin.Logger.LogWarning($"ItemDefinition {itemDefinition.ID} is already registered");
                    continue;
                }

                registry.ItemRegistry.Add(new Registry.ItemRegister
                {
                    ID = itemDefinition.ID,
                    AssetPath = assetName,
                    Definition = itemDefinition
                });
                continue;
            }

            GameObject gameObject = assetBundle.LoadAsset<GameObject>(assetName);

            if (gameObject != null)
            {
                var networkObject = gameObject.GetComponent<NetworkObject>();

                if (networkObject != null)
                {
                    Plugin.Logger.LogInfo($"Registering NetworkObject {networkObject.name} from {assetName}");
                    netPrefabs.AddObject(networkObject, checkForDuplicates: true);
                }

                continue;
            }
        }
    }
}

[CreateAssetMenu(fileName = "Item Definitions", menuName = "RealRadio/ScriptableObjects/Asset Registry/Item Definitions", order = 1)]
public class ItemDefinitionAssets : ScriptableObject
{
#nullable disable
    public ItemDefinition RadioTier1;
#nullable enable
}

[CreateAssetMenu(fileName = "Singletons", menuName = "RealRadio/ScriptableObjects/Asset Registry/Singletons", order = 1)]
public class SingletonPrefabs : ScriptableObject
{
#nullable disable
    public GameObject OffGridBuildManager;
    public GameObject RadioStationManager;
    public GameObject RadioStationInfoManager;
    public GameObject RadioSyncManager;
    public GameObject VehicleRadioManager;
    public GameObject BuildingRadioManager;
    public GameObject RadialMenu;
    public GameObject Modal;
    public GameObject YtDlpManager;
    public GameObject YtDlpUiManager;
    public GameObject APIManager;
    public GameObject GameMusicManager;
    public GameObject RadioApp;
    public GameObject UserStationsManager;
    public GameObject SpeakerConnectionManager;
#nullable enable
}

public class AssetRegistryLoadException : Exception
{
    public AssetRegistryLoadException(string message) : base(message) { }
    public AssetRegistryLoadException(string message, Exception innerException) : base(message, innerException) { }
}

[CreateAssetMenu(fileName = "Shop Listings", menuName = "RealRadio/ScriptableObjects/Asset Registry/Shop Listings", order = 1)]
public class ShopListings : ScriptableObject
{
#nullable disable
    public ShopListing[] Listings;
#nullable enable
}

[CreateAssetMenu(fileName = "Prefabs", menuName = "RealRadio/ScriptableObjects/Asset Registry/Prefabs", order = 1)]
public class Prefabs : ScriptableObject
{
#nullable disable
    public GameObject VehicleRadioProxy;
    public GameObject SelectionArrow;
#nullable enable
}
