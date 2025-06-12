using FishNet.Object;

namespace RealRadio.Components.Building.Buildables;

public class AnalogRadio : Radio
{
    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();

        if (isGhost)
            return;

        //volumeEditSlider.Value = Volume * volumeEditSlider.MaxValue;
    }

    protected override void OnPlayerUserChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
        base.OnPlayerUserChanged(prev, next, asServer);

        if (!asServer)
        {
            //stationEditSlider.gameObject.SetActive(next == Player.Local.NetworkObject);
            //volumeEditSlider.gameObject.SetActive(next == Player.Local.NetworkObject);
        }
    }
}
