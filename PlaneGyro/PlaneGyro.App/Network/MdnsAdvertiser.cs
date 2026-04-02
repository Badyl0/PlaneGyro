using Makaretu.Dns;

namespace PlaneGyroListner.Network;

internal sealed class MdnsAdvertiser : IDisposable
{
    private readonly ServiceDiscovery _serviceDiscovery;
    private readonly ServiceProfile _serviceProfile;

    public MdnsAdvertiser(string serviceInstanceName, int port)
    {
        _serviceDiscovery = new ServiceDiscovery();
        _serviceProfile = new ServiceProfile(serviceInstanceName, "_planegyro._tcp", (ushort)port);
    }

    public void Start()
    {
        _serviceDiscovery.Advertise(_serviceProfile);
    }

    public void Dispose()
    {
        _serviceDiscovery.Dispose();
    }
}
