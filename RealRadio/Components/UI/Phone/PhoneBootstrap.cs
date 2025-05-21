using System;
using ScheduleOne.UI.Phone;

namespace RealRadio.Components.UI.Phone;

internal static class PhoneBootstrap
{
    public static void CreateApp(AppsCanvas parent)
    {
        if (Plugin.Assets == null)
            throw new InvalidOperationException("Assets not loaded");

        var app = UnityEngine.Object.Instantiate(Plugin.Assets.Singletons.RadioApp, parent: parent.transform, worldPositionStays: false);
    }
}
