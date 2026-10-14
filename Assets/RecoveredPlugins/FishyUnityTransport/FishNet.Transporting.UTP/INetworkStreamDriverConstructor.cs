using Unity.Networking.Transport;

namespace FishNet.Transporting.UTP {
public interface INetworkStreamDriverConstructor
{
	void CreateDriver(UnityTransport transport, out NetworkDriver driver, out NetworkPipeline unreliableFragmentedPipeline, out NetworkPipeline unreliableSequencedFragmentedPipeline, out NetworkPipeline reliableSequencedPipeline);
}

}