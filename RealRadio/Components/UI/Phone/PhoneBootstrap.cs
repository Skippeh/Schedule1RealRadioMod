using System;
using RealRadio.Assets;
using ScheduleOne.UI.Phone;

namespace RealRadio.Components.UI.Phone;

internal static class PhoneBootstrap
{
    public static void CreateApp(AppsCanvas parent)
    {
        if (AssetRegistry.Instance == null)
            throw new InvalidOperationException("Assets not loaded");

        UnityEngine.Object.Instantiate(AssetRegistry.Instance.Singletons.RadioApp, parent: parent.transform, worldPositionStays: false);
    }
}
