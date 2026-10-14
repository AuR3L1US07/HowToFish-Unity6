using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using FishNet.Managing;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport.TLS;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace FishNet.Transporting.UTP {
[DisallowMultipleComponent]
[AddComponentMenu("FishNet/Transport/Unity Transport")]
public class UnityTransport : Transport, INetworkStreamDriverConstructor
{
	public enum ProtocolType
	{
		UnityTransport,
		RelayUnityTransport
	}

	private enum State
	{
		Disconnected,
		Listening,
		Connected
	}

	[Serializable]
	public struct ConnectionAddressData
	{
		[Tooltip("IP address of the server (address to which clients will connect to).")]
		[SerializeField]
		public string Address;

		[Tooltip("UDP port of the server.")]
		[SerializeField]
		public ushort Port;

		[Tooltip("IP address the server will listen on. If not provided, will use localhost.")]
		[SerializeField]
		public string ServerListenAddress;

		public NetworkEndpoint ServerEndPoint => ParseNetworkEndpoint(Address, Port);

		public NetworkEndpoint ListenEndPoint
		{
			get
			{
				if (string.IsNullOrEmpty(ServerListenAddress))
				{
					NetworkEndpoint networkEndpoint = NetworkEndpoint.LoopbackIpv4;
					if (!string.IsNullOrEmpty(Address) && ServerEndPoint.Family == NetworkFamily.Ipv6)
					{
						networkEndpoint = NetworkEndpoint.LoopbackIpv6;
					}
					return networkEndpoint.WithPort(Port);
				}
				return ParseNetworkEndpoint(ServerListenAddress, Port);
			}
		}

		public bool IsIpv6
		{
			get
			{
				if (!string.IsNullOrEmpty(Address))
				{
					return ParseNetworkEndpoint(Address, Port, silent: true).Family == NetworkFamily.Ipv6;
				}
				return false;
			}
		}

		private static NetworkEndpoint ParseNetworkEndpoint(string ip, ushort port, bool silent = false)
		{
			NetworkEndpoint endpoint = default(NetworkEndpoint);
			if (!NetworkEndpoint.TryParse(ip, port, out endpoint, NetworkFamily.Ipv4) && !NetworkEndpoint.TryParse(ip, port, out endpoint, NetworkFamily.Ipv6))
			{
				IPAddress[] hostAddresses = Dns.GetHostAddresses(ip);
				if (hostAddresses.Length != 0)
				{
					endpoint = ParseNetworkEndpoint(hostAddresses[0].ToString(), port, silent: true);
				}
			}
			if (endpoint == default(NetworkEndpoint) && !silent)
			{
				UnityEngine.Debug.LogError($"Invalid network endpoint: {ip}:{port}.");
			}
			return endpoint;
		}
	}

	[Serializable]
	public struct SimulatorParameters
	{
		[Tooltip("Delay to add to every send and received packet (in milliseconds). Only applies in the editor and in development builds. The value is ignored in production builds.")]
		[SerializeField]
		public int PacketDelayMS;

		[Tooltip("Jitter (random variation) to add/substract to the packet delay (in milliseconds). Only applies in the editor and in development builds. The value is ignored in production builds.")]
		[SerializeField]
		public int PacketJitterMS;

		[Tooltip("Percentage of sent and received packets to drop. Only applies in the editor and in the editor and in developments builds.")]
		[SerializeField]
		public int PacketDropRate;
	}

	[BurstCompile]
	private struct SendBatchedMessagesJob : IJob
	{
		public NetworkDriver.Concurrent Driver;

		public SendTarget Target;

		public BatchedSendQueue Queue;

		public NetworkPipeline ReliablePipeline;

		public void Execute()
		{
			ulong clientId = Target.ClientId;
			NetworkConnection connection = ParseClientId(clientId);
			NetworkPipeline networkPipeline = Target.NetworkPipeline;
			while (!Queue.IsEmpty)
			{
				int num = Driver.BeginSend(networkPipeline, connection, out var writer);
				if (num != 0)
				{
					UnityEngine.Debug.LogError($"Error sending message: {ErrorUtilities.ErrorToFixedString(num, clientId)}");
					break;
				}
				int num2 = ((networkPipeline == ReliablePipeline) ? Queue.FillWriterWithBytes(ref writer) : Queue.FillWriterWithMessages(ref writer));
				num = Driver.EndSend(writer);
				if (num == num2)
				{
					Queue.Consume(num2);
					continue;
				}
				if (num != -5)
				{
					UnityEngine.Debug.LogError($"Error sending the message: {ErrorUtilities.ErrorToFixedString(num, clientId)}");
					Queue.Consume(num2);
				}
				break;
			}
		}
	}

	private struct SendTarget : IEquatable<SendTarget>
	{
		public readonly ulong ClientId;

		public readonly NetworkPipeline NetworkPipeline;

		public SendTarget(ulong clientId, NetworkPipeline networkPipeline)
		{
			ClientId = clientId;
			NetworkPipeline = networkPipeline;
		}

		public bool Equals(SendTarget other)
		{
			if (ClientId == other.ClientId)
			{
				return NetworkPipeline.Equals(other.NetworkPipeline);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is SendTarget other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (ClientId.GetHashCode() * 397) ^ NetworkPipeline.GetHashCode();
		}
	}

	private readonly struct ClientHostSendData
	{
		public readonly Channel Channel;

		public readonly ArraySegment<byte> Segment;

		public ClientHostSendData(Channel channel, ArraySegment<byte> data)
		{
			if (data.Array == null)
			{
				throw new InvalidOperationException();
			}
			Channel = channel;
			byte[] array = new byte[data.Count];
			Buffer.BlockCopy(data.Array, data.Offset, array, 0, data.Count);
			Segment = new ArraySegment<byte>(array, 0, data.Count);
		}
	}

	public const int InitialMaxPacketQueueSize = 128;

	public const int InitialMaxPayloadSize = 6144;

	private const int k_MaxReliableThroughput = 5376;

	private static ConnectionAddressData s_DefaultConnectionAddressData = new ConnectionAddressData
	{
		Address = "127.0.0.1",
		Port = 7777,
		ServerListenAddress = string.Empty
	};

	private INetworkStreamDriverConstructor m_DriverConstructor;

	[Tooltip("Which protocol should be selected (Relay/Non-Relay).")]
	[SerializeField]
	private ProtocolType m_ProtocolType;

	[Tooltip("Per default the client/server will communicate over UDP. Set to true to communicate with WebSocket.")]
	[SerializeField]
	private bool m_UseWebSockets;

	[Tooltip("Per default the client/server communication will not be encrypted. Select true to enable DTLS for UDP and TLS for Websocket.")]
	[SerializeField]
	private bool m_UseEncryption;

	[Tooltip("The maximum amount of packets that can be in the internal send/receive queues. Basically this is how many packets can be sent/received in a single update/frame.")]
	[SerializeField]
	private int m_MaxPacketQueueSize = 128;

	[Tooltip("The maximum size of an unreliable payload that can be handled by the transport.")]
	[SerializeField]
	private int m_MaxPayloadSize = 6144;

	private int m_MaxSendQueueSize;

	[Tooltip("Timeout in milliseconds after which a heartbeat is sent if there is no activity.")]
	[SerializeField]
	private int m_HeartbeatTimeoutMS = 500;

	[Tooltip("Timeout in milliseconds indicating how long we will wait until we send a new connection attempt.")]
	[SerializeField]
	private int m_ConnectTimeoutMS = 1000;

	[Tooltip("The maximum amount of connection attempts we will try before disconnecting.")]
	[SerializeField]
	private int m_MaxConnectAttempts = 60;

	[Tooltip("Inactivity timeout after which a connection will be disconnected. The connection needs to receive data from the connected endpoint within this timeout. Note that with heartbeats enabled, simply not sending any data will not be enough to trigger this timeout (since heartbeats count as connection events).")]
	[SerializeField]
	private int m_DisconnectTimeoutMS = 30000;

	public ConnectionAddressData ConnectionData = s_DefaultConnectionAddressData;

	[Obsolete("DebugSimulator is no longer supported and has no effect. Use Network Simulator from the Multiplayer Tools package.", false)]
	[HideInInspector]
	public SimulatorParameters DebugSimulator = new SimulatorParameters
	{
		PacketDelayMS = 0,
		PacketJitterMS = 0,
		PacketDropRate = 0
	};

	protected NetworkDriver m_Driver;

	private State m_State;

	private NetworkSettings m_NetworkSettings;

	private ulong m_ServerClientId;

	private NetworkPipeline m_UnreliableFragmentedPipeline;

	private NetworkPipeline m_UnreliableSequencedFragmentedPipeline;

	private NetworkPipeline m_ReliableSequencedPipeline;

	private RelayServerData m_RelayServerData;

	private readonly Dictionary<SendTarget, BatchedSendQueue> m_SendQueue = new Dictionary<SendTarget, BatchedSendQueue>();

	private readonly Dictionary<ulong, BatchedReceiveQueue> m_ReliableReceiveQueues = new Dictionary<ulong, BatchedReceiveQueue>();

	private string m_ServerPrivateKey;

	private string m_ServerCertificate;

	private string m_ServerCommonName;

	private string m_ClientCaCertificate;

	[Range(1f, 4095f)]
	[SerializeField]
	private int m_MaximumClients = 4095;

	private LocalConnectionState m_ServerState = LocalConnectionState.Stopped;

	private int m_NextClientId = 1;

	private readonly Dictionary<int, ulong> m_TransportIdToClientIdMap = new Dictionary<int, ulong>();

	private readonly Dictionary<ulong, int> m_ClientIdToTransportIdMap = new Dictionary<ulong, int>();

	private LocalConnectionState m_ClientState = LocalConnectionState.Stopped;

	private const ulong k_ClientHostId = 0uL;

	private readonly Queue<ClientHostSendData> m_ClientHostSendQueue = new Queue<ClientHostSendData>();

	private readonly Queue<ClientHostSendData> m_ClientHostReceiveQueue = new Queue<ClientHostSendData>();

	public INetworkStreamDriverConstructor DriverConstructor
	{
		get
		{
			return m_DriverConstructor ?? this;
		}
		set
		{
			m_DriverConstructor = value;
		}
	}

	public bool UseWebSockets
	{
		get
		{
			return m_UseWebSockets;
		}
		set
		{
			m_UseWebSockets = value;
		}
	}

	public bool UseEncryption
	{
		get
		{
			return m_UseEncryption;
		}
		set
		{
			m_UseEncryption = value;
		}
	}

	public int MaxPacketQueueSize
	{
		get
		{
			return m_MaxPacketQueueSize;
		}
		set
		{
			m_MaxPacketQueueSize = value;
		}
	}

	public int MaxPayloadSize
	{
		get
		{
			return m_MaxPayloadSize;
		}
		set
		{
			m_MaxPayloadSize = value;
		}
	}

	public int MaxSendQueueSize
	{
		get
		{
			return m_MaxSendQueueSize;
		}
		set
		{
			m_MaxSendQueueSize = value;
		}
	}

	public int HeartbeatTimeoutMS
	{
		get
		{
			return m_HeartbeatTimeoutMS;
		}
		set
		{
			m_HeartbeatTimeoutMS = value;
		}
	}

	public int ConnectTimeoutMS
	{
		get
		{
			return m_ConnectTimeoutMS;
		}
		set
		{
			m_ConnectTimeoutMS = value;
		}
	}

	public int MaxConnectAttempts
	{
		get
		{
			return m_MaxConnectAttempts;
		}
		set
		{
			m_MaxConnectAttempts = value;
		}
	}

	public int DisconnectTimeoutMS
	{
		get
		{
			return m_DisconnectTimeoutMS;
		}
		set
		{
			m_DisconnectTimeoutMS = value;
		}
	}

	private ulong ServerClientId => m_ServerClientId;

	public ProtocolType Protocol => m_ProtocolType;

	public override event Action<ClientConnectionStateArgs> OnClientConnectionState;

	public override event Action<ServerConnectionStateArgs> OnServerConnectionState;

	public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;

	public override event Action<ClientReceivedDataArgs> OnClientReceivedData;

	public override event Action<ServerReceivedDataArgs> OnServerReceivedData;

	private void InitDriver()
	{
		DriverConstructor.CreateDriver(this, out m_Driver, out m_UnreliableFragmentedPipeline, out m_UnreliableSequencedFragmentedPipeline, out m_ReliableSequencedPipeline);
	}

	private void DisposeInternals()
	{
		if (m_Driver.IsCreated)
		{
			m_Driver.Dispose();
		}
		m_NetworkSettings.Dispose();
		foreach (BatchedSendQueue value in m_SendQueue.Values)
		{
			value.Dispose();
		}
		m_SendQueue.Clear();
		DisposeClientHost();
	}

	private NetworkPipeline SelectSendPipeline(Channel channel)
	{
		switch (channel)
		{
		case Channel.Unreliable:
			return m_UnreliableFragmentedPipeline;
		case Channel.Reliable:
			return m_ReliableSequencedPipeline;
		default:
			UnityEngine.Debug.LogError(string.Format("Unknown {0} value: {1}", "Channel", channel));
			return NetworkPipeline.Null;
		}
	}

	private bool ClientBindAndConnect()
	{
		NetworkEndpoint networkEndpoint = default(NetworkEndpoint);
		if (m_ProtocolType == ProtocolType.RelayUnityTransport)
		{
			if (m_RelayServerData.Equals(default(RelayServerData)))
			{
				UnityEngine.Debug.LogError("You must call SetRelayServerData() at least once before calling StartClient.");
				return false;
			}
			m_NetworkSettings.WithRelayParameters(ref m_RelayServerData, m_HeartbeatTimeoutMS);
			networkEndpoint = m_RelayServerData.Endpoint;
		}
		else
		{
			networkEndpoint = ConnectionData.ServerEndPoint;
		}
		if (networkEndpoint.Family == NetworkFamily.Invalid)
		{
			UnityEngine.Debug.LogError("Target server network address (" + ConnectionData.Address + ") is Invalid!");
			return false;
		}
		InitDriver();
		NetworkEndpoint endpoint = ((networkEndpoint.Family == NetworkFamily.Ipv6) ? NetworkEndpoint.AnyIpv6 : NetworkEndpoint.AnyIpv4);
		if (m_Driver.Bind(endpoint) != 0)
		{
			UnityEngine.Debug.LogError("Client failed to bind");
			return false;
		}
		NetworkConnection connection = m_Driver.Connect(networkEndpoint);
		m_ServerClientId = ParseClientId(connection);
		return true;
	}

	private bool ServerBindAndListen(NetworkEndpoint endPoint)
	{
		if (endPoint.Family == NetworkFamily.Invalid)
		{
			UnityEngine.Debug.LogError("Network listen address (" + ConnectionData.Address + ") is Invalid!");
			return false;
		}
		InitDriver();
		if (m_Driver.Bind(endPoint) != 0)
		{
			UnityEngine.Debug.LogError("Server failed to bind. This is usually caused by another process being bound to the same port.");
			return false;
		}
		if (m_Driver.Listen() != 0)
		{
			UnityEngine.Debug.LogError("Server failed to listen.");
			return false;
		}
		m_State = State.Listening;
		return true;
	}

	private void SetProtocol(ProtocolType inProtocol)
	{
		m_ProtocolType = inProtocol;
	}

	public void SetRelayServerData(string ipv4Address, ushort port, byte[] allocationIdBytes, byte[] keyBytes, byte[] connectionDataBytes, byte[] hostConnectionDataBytes = null, bool isSecure = false)
	{
		byte[] hostConnectionData = hostConnectionDataBytes ?? connectionDataBytes;
		m_RelayServerData = new RelayServerData(ipv4Address, port, allocationIdBytes, connectionDataBytes, hostConnectionData, keyBytes, isSecure);
		SetProtocol(ProtocolType.RelayUnityTransport);
	}

	public void SetRelayServerData(RelayServerData serverData)
	{
		if (m_ServerState.IsStartingOrStarted())
		{
			base.NetworkManager.LogWarning("It looks like you are trying to connect as a host to the Relay server. Since the local server is already running, calling SetRelayServerData() for the client is unnecessary. It doesn't cause errors, you can ignore it if you're sure you're doing it right.");
		}
		m_RelayServerData = serverData;
		SetProtocol(ProtocolType.RelayUnityTransport);
	}

	public void SetHostRelayData(string ipAddress, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, bool isSecure = false)
	{
		SetRelayServerData(ipAddress, port, allocationId, key, connectionData, null, isSecure);
	}

	public void SetClientRelayData(string ipAddress, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData, bool isSecure = false)
	{
		SetRelayServerData(ipAddress, port, allocationId, key, connectionData, hostConnectionData, isSecure);
	}

	public void SetConnectionData(string ipv4Address, ushort port, string listenAddress = null)
	{
		ConnectionData = new ConnectionAddressData
		{
			Address = ipv4Address,
			Port = port,
			ServerListenAddress = (listenAddress ?? ipv4Address)
		};
		SetProtocol(ProtocolType.UnityTransport);
	}

	public void SetConnectionData(NetworkEndpoint endPoint, NetworkEndpoint listenEndPoint = default(NetworkEndpoint))
	{
		string ipv4Address = endPoint.Address.Split(':')[0];
		string listenAddress = string.Empty;
		if (listenEndPoint != default(NetworkEndpoint))
		{
			listenAddress = listenEndPoint.Address.Split(':')[0];
			if (endPoint.Port != listenEndPoint.Port)
			{
				UnityEngine.Debug.LogError($"Port mismatch between server and listen endpoints ({endPoint.Port} vs {listenEndPoint.Port}).");
			}
		}
		SetConnectionData(ipv4Address, endPoint.Port, listenAddress);
	}

	[Obsolete("SetDebugSimulatorParameters is no longer supported and has no effect. Use Network Simulator from the Multiplayer Tools package.", false)]
	public void SetDebugSimulatorParameters(int packetDelay, int packetJitter, int dropRate)
	{
		if (m_Driver.IsCreated)
		{
			UnityEngine.Debug.LogError("SetDebugSimulatorParameters() must be called before StartClient() or StartServer().");
			return;
		}
		DebugSimulator = new SimulatorParameters
		{
			PacketDelayMS = packetDelay,
			PacketJitterMS = packetJitter,
			PacketDropRate = dropRate
		};
	}

	private bool StartRelayServer()
	{
		if (m_RelayServerData.Equals(default(RelayServerData)))
		{
			UnityEngine.Debug.LogError("You must call SetRelayServerData() at least once before calling StartServer.");
			return false;
		}
		m_NetworkSettings.WithRelayParameters(ref m_RelayServerData, m_HeartbeatTimeoutMS);
		return ServerBindAndListen(NetworkEndpoint.AnyIpv4);
	}

	private void SendBatchedMessages(SendTarget sendTarget, BatchedSendQueue queue)
	{
		if (m_Driver.IsCreated)
		{
			new SendBatchedMessagesJob
			{
				Driver = m_Driver.ToConcurrent(),
				Target = sendTarget,
				Queue = queue,
				ReliablePipeline = m_ReliableSequencedPipeline
			}.Run();
		}
	}

	private bool AcceptConnection()
	{
		NetworkConnection networkConnection = m_Driver.Accept();
		if (networkConnection == default(NetworkConnection))
		{
			return false;
		}
		HandleRemoteConnectionState(RemoteConnectionState.Started, ParseClientId(networkConnection));
		return true;
	}

	private void ReceiveMessages(ulong clientId, NetworkPipeline pipeline, DataStreamReader dataReader)
	{
		BatchedReceiveQueue value;
		if (pipeline == m_ReliableSequencedPipeline)
		{
			if (m_ReliableReceiveQueues.TryGetValue(clientId, out value))
			{
				value.PushReader(dataReader);
			}
			else
			{
				value = new BatchedReceiveQueue(dataReader);
				m_ReliableReceiveQueues[clientId] = value;
			}
		}
		else
		{
			value = new BatchedReceiveQueue(dataReader);
		}
		while (!value.IsEmpty)
		{
			ArraySegment<byte> arraySegment = value.PopMessage();
			if (!(arraySegment == default(ArraySegment<byte>)))
			{
				HandleTransportDataEvent(clientId, arraySegment, pipeline);
				continue;
			}
			break;
		}
	}

	private bool ProcessEvent()
	{
		NetworkConnection connection;
		DataStreamReader reader;
		NetworkPipeline pipe;
		NetworkEvent.Type type = m_Driver.PopEvent(out connection, out reader, out pipe);
		ulong num = ParseClientId(connection);
		switch (type)
		{
		case NetworkEvent.Type.Connect:
			HandleTransportConnectEvent(num);
			m_State = State.Connected;
			return true;
		case NetworkEvent.Type.Disconnect:
			if (m_State == State.Connected)
			{
				m_State = State.Disconnected;
				m_ServerClientId = 0uL;
			}
			else if (m_State == State.Disconnected)
			{
				UnityEngine.Debug.LogError("Failed to connect to server.");
				m_ServerClientId = 0uL;
			}
			m_ReliableReceiveQueues.Remove(num);
			ClearSendQueuesForClientId(num);
			HandleTransportDisconnectEvent(num);
			return true;
		case NetworkEvent.Type.Data:
			ReceiveMessages(num, pipe, reader);
			return true;
		default:
			return false;
		}
	}

	private void IterateIncoming()
	{
		if (!m_Driver.IsCreated)
		{
			return;
		}
		if (m_ProtocolType == ProtocolType.RelayUnityTransport && m_Driver.GetRelayConnectionStatus() == RelayConnectionStatus.AllocationInvalid)
		{
			UnityEngine.Debug.LogError("Transport failure! Relay allocation needs to be recreated, and NetworkManager restarted. Use NetworkManager.OnTransportFailure to be notified of such events programmatically.");
			HandleTransportFailureEvent();
			return;
		}
		m_Driver.ScheduleUpdate().Complete();
		while (AcceptConnection() && m_Driver.IsCreated)
		{
		}
		while (ProcessEvent() && m_Driver.IsCreated)
		{
		}
	}

	public void IterateOutgoing()
	{
		if (!m_Driver.IsCreated)
		{
			return;
		}
		foreach (KeyValuePair<SendTarget, BatchedSendQueue> item in m_SendQueue)
		{
			SendBatchedMessages(item.Key, item.Value);
		}
		m_Driver.ScheduleFlushSend().Complete();
	}

	private void OnDestroy()
	{
		Shutdown();
	}

	private unsafe int ExtractRtt(NetworkConnection networkConnection)
	{
		if (m_Driver.GetConnectionState(networkConnection) != NetworkConnection.State.Connected)
		{
			return 0;
		}
		m_Driver.GetPipelineBuffers(m_ReliableSequencedPipeline, NetworkPipelineStageId.Get<ReliableSequencedPipelineStage>(), networkConnection, out var _, out var _, out var sharedBuffer);
		ReliableUtility.SharedContext* unsafePtr = (ReliableUtility.SharedContext*)sharedBuffer.GetUnsafePtr();
		return unsafePtr->RttInfo.LastRtt;
	}

	private unsafe static ulong ParseClientId(NetworkConnection connection)
	{
		return *(ulong*)(&connection);
	}

	private unsafe static NetworkConnection ParseClientId(ulong clientId)
	{
		return *(NetworkConnection*)(&clientId);
	}

	private void ClearSendQueuesForClientId(ulong clientId)
	{
		using NativeList<SendTarget> nativeList = new NativeList<SendTarget>(16, Allocator.Temp);
		foreach (SendTarget key in m_SendQueue.Keys)
		{
			SendTarget value = key;
			if (value.ClientId == clientId)
			{
				nativeList.Add(in value);
			}
		}
		foreach (SendTarget item in nativeList)
		{
			m_SendQueue[item].Dispose();
			m_SendQueue.Remove(item);
		}
	}

	private void FlushSendQueuesForClientId(ulong clientId)
	{
		foreach (KeyValuePair<SendTarget, BatchedSendQueue> item in m_SendQueue)
		{
			if (item.Key.ClientId == clientId)
			{
				SendBatchedMessages(item.Key, item.Value);
			}
		}
	}

	private void DisconnectLocalClient()
	{
		SetClientConnectionState(LocalConnectionState.Stopping);
		if (m_State == State.Connected)
		{
			FlushSendQueuesForClientId(m_ServerClientId);
			if (m_Driver.Disconnect(ParseClientId(m_ServerClientId)) == 0)
			{
				m_State = State.Disconnected;
				m_ReliableReceiveQueues.Remove(m_ServerClientId);
				ClearSendQueuesForClientId(m_ServerClientId);
				ShutdownInternals();
			}
		}
		SetClientConnectionState(LocalConnectionState.Stopped);
	}

	private bool DisconnectRemoteClient(ulong clientId)
	{
		if (m_State == State.Listening)
		{
			FlushSendQueuesForClientId(clientId);
			m_ReliableReceiveQueues.Remove(clientId);
			ClearSendQueuesForClientId(clientId);
			NetworkConnection connection = ParseClientId(clientId);
			if (m_Driver.GetConnectionState(connection) != NetworkConnection.State.Disconnected)
			{
				m_Driver.Disconnect(connection);
				HandleTransportDisconnectEvent(clientId);
				return true;
			}
		}
		return false;
	}

	public ulong GetCurrentRtt(int clientId)
	{
		if (m_TransportIdToClientIdMap.TryGetValue(clientId, out var value))
		{
			return (ulong)ExtractRtt(ParseClientId(value));
		}
		base.NetworkManager.LogWarning($"Connection with id {clientId} is disconnected. Unable to get the current Rtt.");
		return 0uL;
	}

	private void InitializeNetworkSettings()
	{
		m_NetworkSettings = new NetworkSettings(Allocator.Persistent);
		int payloadCapacity = m_MaxPayloadSize + 4;
		m_NetworkSettings.WithFragmentationStageParameters(payloadCapacity);
		m_NetworkSettings.WithReliableStageParameters(64, 64, (m_ProtocolType == ProtocolType.RelayUnityTransport) ? 750 : 500);
	}

	private void Send(ulong clientId, ArraySegment<byte> payload, Channel channel)
	{
		NetworkPipeline networkPipeline = SelectSendPipeline(channel);
		if (networkPipeline != m_ReliableSequencedPipeline && payload.Count > m_MaxPayloadSize)
		{
			UnityEngine.Debug.LogError($"Unreliable payload of size {payload.Count} larger than configured 'Max Payload Size' ({m_MaxPayloadSize}).");
			return;
		}
		SendTarget sendTarget = new SendTarget(clientId, networkPipeline);
		if (!m_SendQueue.TryGetValue(sendTarget, out var value))
		{
			int val = ((m_MaxSendQueueSize > 0) ? m_MaxSendQueueSize : (m_DisconnectTimeoutMS * 5376));
			value = new BatchedSendQueue(Math.Max(val, m_MaxPayloadSize));
			m_SendQueue.Add(sendTarget, value);
		}
		if (value.PushMessage(payload))
		{
			return;
		}
		if (networkPipeline == m_ReliableSequencedPipeline)
		{
			UnityEngine.Debug.LogError($"Couldn't add payload of size {payload.Count} to reliable send queue. " + $"Closing connection {TransportIdToClientId(clientId)} as reliability guarantees can't be maintained.");
			if (clientId == m_ServerClientId)
			{
				DisconnectLocalClient();
			}
			else
			{
				DisconnectRemoteClient(clientId);
			}
		}
		else
		{
			m_Driver.ScheduleFlushSend().Complete();
			SendBatchedMessages(sendTarget, value);
			value.PushMessage(payload);
		}
	}

	private bool StartClient()
	{
		if (m_ClientState != LocalConnectionState.Stopped)
		{
			return false;
		}
		SetClientConnectionState(LocalConnectionState.Starting);
		if (m_ServerState == LocalConnectionState.Starting)
		{
			return true;
		}
		if (m_ServerState == LocalConnectionState.Started)
		{
			if (m_TransportIdToClientIdMap.Count >= GetMaximumClients())
			{
				SetClientConnectionState(LocalConnectionState.Stopping);
				base.NetworkManager.LogWarning("Connection limit reached. Server cannot accept new connections.");
				SetClientConnectionState(LocalConnectionState.Stopped);
				return false;
			}
			m_ServerClientId = 0uL;
			HandleRemoteConnectionState(RemoteConnectionState.Started, m_ServerClientId);
			SetClientConnectionState(LocalConnectionState.Started);
			return true;
		}
		if (m_Driver.IsCreated)
		{
			return false;
		}
		InitializeNetworkSettings();
		bool num = ClientBindAndConnect();
		if (!num)
		{
			SetClientConnectionState(LocalConnectionState.Stopping);
			ShutdownInternals();
			SetClientConnectionState(LocalConnectionState.Stopped);
		}
		return num;
	}

	private bool StartServer()
	{
		if (m_Driver.IsCreated)
		{
			return false;
		}
		SetServerConnectionState(LocalConnectionState.Starting);
		InitializeNetworkSettings();
		int num = Protocol switch
		{
			ProtocolType.UnityTransport => ServerBindAndListen(ConnectionData.ListenEndPoint) ? 1 : 0, 
			ProtocolType.RelayUnityTransport => StartRelayServer() ? 1 : 0, 
			_ => 0, 
		};
		if (num != 0)
		{
			SetServerConnectionState(LocalConnectionState.Started);
			if (m_ClientState == LocalConnectionState.Starting)
			{
				HandleRemoteConnectionState(RemoteConnectionState.Started, m_ServerClientId);
				SetClientConnectionState(LocalConnectionState.Started);
				return (byte)num != 0;
			}
		}
		else
		{
			SetServerConnectionState(LocalConnectionState.Stopping);
			StopClientHost();
			DisposeInternals();
			SetServerConnectionState(LocalConnectionState.Stopped);
		}
		return (byte)num != 0;
	}

	private void ShutdownInternals()
	{
		if (m_Driver.IsCreated)
		{
			while (ProcessEvent() && m_Driver.IsCreated)
			{
			}
			foreach (KeyValuePair<SendTarget, BatchedSendQueue> item in m_SendQueue)
			{
				SendBatchedMessages(item.Key, item.Value);
			}
			m_Driver.ScheduleUpdate().Complete();
		}
		DisposeInternals();
		m_ReliableReceiveQueues.Clear();
		m_State = State.Disconnected;
		m_ServerClientId = 0uL;
	}

	private void ConfigureSimulatorForUtp2()
	{
		m_NetworkSettings.WithSimulatorStageParameters(300, 1400, ApplyMode.AllPackets, 0, 0, 0, 0, 0, 0, 0, (uint)Stopwatch.GetTimestamp());
		m_NetworkSettings.WithNetworkSimulatorParameters();
	}

	public void SetServerSecrets(string serverCertificate, string serverPrivateKey)
	{
		m_ServerPrivateKey = serverPrivateKey;
		m_ServerCertificate = serverCertificate;
	}

	public void SetClientSecrets(string serverCommonName, string caCertificate = null)
	{
		m_ServerCommonName = serverCommonName;
		m_ClientCaCertificate = caCertificate;
	}

	public void CreateDriver(UnityTransport transport, out NetworkDriver driver, out NetworkPipeline unreliableFragmentedPipeline, out NetworkPipeline unreliableSequencedFragmentedPipeline, out NetworkPipeline reliableSequencedPipeline)
	{
		bool flag = m_ServerState == LocalConnectionState.Starting;
		ref NetworkSettings networkSettings = ref m_NetworkSettings;
		int maxConnectAttempts = transport.m_MaxConnectAttempts;
		CommonNetworkParametersExtensions.WithNetworkConfigParameters(connectTimeoutMS: transport.m_ConnectTimeoutMS, maxConnectAttempts: maxConnectAttempts, disconnectTimeoutMS: transport.m_DisconnectTimeoutMS, sendQueueCapacity: m_MaxPacketQueueSize, receiveQueueCapacity: m_MaxPacketQueueSize, settings: ref networkSettings, heartbeatTimeoutMS: transport.m_HeartbeatTimeoutMS);
		if (m_UseEncryption)
		{
			if (m_ProtocolType == ProtocolType.RelayUnityTransport)
			{
				if (m_RelayServerData.IsSecure == 0)
				{
					base.NetworkManager.LogError("Mismatched security configuration, between Relay and local UnityTransport settings");
				}
			}
			else if (flag)
			{
				if (string.IsNullOrEmpty(m_ServerCertificate) || string.IsNullOrEmpty(m_ServerPrivateKey))
				{
					throw new Exception("In order to use encrypted communications, when hosting, you must set the server certificate and key.");
				}
				m_NetworkSettings.WithSecureServerParameters(m_ServerCertificate, m_ServerPrivateKey);
			}
			else
			{
				if (string.IsNullOrEmpty(m_ServerCommonName))
				{
					throw new Exception("In order to use encrypted communications, clients must set the server common name.");
				}
				if (string.IsNullOrEmpty(m_ClientCaCertificate))
				{
					m_NetworkSettings.WithSecureClientParameters(m_ServerCommonName);
				}
				else
				{
					m_NetworkSettings.WithSecureClientParameters(m_ClientCaCertificate, m_ServerCommonName);
				}
			}
		}
		if (m_UseWebSockets)
		{
			driver = NetworkDriver.Create(default(WebSocketNetworkInterface), m_NetworkSettings);
		}
		else
		{
			driver = NetworkDriver.Create(default(UDPNetworkInterface), m_NetworkSettings);
		}
		SetupPipelinesForUtp2(driver, out unreliableFragmentedPipeline, out unreliableSequencedFragmentedPipeline, out reliableSequencedPipeline);
	}

	private void SetupPipelinesForUtp2(NetworkDriver driver, out NetworkPipeline unreliableFragmentedPipeline, out NetworkPipeline unreliableSequencedFragmentedPipeline, out NetworkPipeline reliableSequencedPipeline)
	{
		unreliableFragmentedPipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage));
		unreliableSequencedFragmentedPipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(UnreliableSequencedPipelineStage));
		reliableSequencedPipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
	}

	private void HandleTransportConnectEvent(ulong clientId)
	{
		if (m_ServerState == LocalConnectionState.Started)
		{
			HandleRemoteConnectionState(RemoteConnectionState.Started, clientId);
		}
		else
		{
			SetClientConnectionState(LocalConnectionState.Started);
		}
	}

	private void HandleTransportDisconnectEvent(ulong clientId)
	{
		if (m_ServerState == LocalConnectionState.Started)
		{
			HandleRemoteConnectionState(RemoteConnectionState.Stopped, clientId);
		}
		else if (m_ClientState == LocalConnectionState.Started)
		{
			SetClientConnectionState(LocalConnectionState.Stopping);
			ShutdownInternals();
			SetClientConnectionState(LocalConnectionState.Stopped);
		}
	}

	private void HandleTransportDataEvent(ulong clientId, ArraySegment<byte> data, NetworkPipeline pipeline)
	{
		Channel channel = SelectSendChannel(pipeline);
		switch (m_State)
		{
		case State.Listening:
		{
			int connectionId = TransportIdToClientId(clientId);
			HandleServerReceivedDataArgs(new ServerReceivedDataArgs(data, channel, connectionId, base.Index));
			break;
		}
		case State.Connected:
			HandleClientReceivedDataArgs(new ClientReceivedDataArgs(data, channel, base.Index));
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void HandleTransportFailureEvent()
	{
		string text = ((!m_ServerState.IsStartingOrStarted()) ? "Client" : (m_ClientState.IsStartingOrStarted() ? "Host" : "Server"));
		string text2 = ((m_ServerState == LocalConnectionState.Starting || m_ClientState == LocalConnectionState.Starting) ? "start failure" : "failure");
		base.NetworkManager.LogError(text + " is shutting down due to network transport " + text2 + " of UnityTransport!");
		Shutdown();
	}

	public override string GetConnectionAddress(int connectionId)
	{
		bool num = m_ServerState == LocalConnectionState.Started;
		bool flag = m_ClientState == LocalConnectionState.Started;
		if (num)
		{
			if (!m_TransportIdToClientIdMap.TryGetValue(connectionId, out var value))
			{
				base.NetworkManager.LogWarning($"Connection with id {connectionId} is disconnected. Unable to get connection address.");
				return string.Empty;
			}
			if (flag && value == 0L)
			{
				return GetLocalEndPoint().Address;
			}
			NetworkConnection connection = ParseClientId(value);
			if (connection.GetState(m_Driver) != NetworkConnection.State.Disconnected)
			{
				return m_Driver.GetRemoteEndpoint(connection).Address;
			}
			return string.Empty;
		}
		if (flag && base.NetworkManager.ClientManager.Connection.ClientId == connectionId)
		{
			return GetLocalEndPoint().Address;
		}
		return string.Empty;
		NetworkEndpoint GetLocalEndPoint()
		{
			return m_Driver.GetLocalEndpoint();
		}
	}

	public override LocalConnectionState GetConnectionState(bool server)
	{
		if (!server)
		{
			return m_ClientState;
		}
		return m_ServerState;
	}

	public override RemoteConnectionState GetConnectionState(int connectionId)
	{
		if (m_TransportIdToClientIdMap.TryGetValue(connectionId, out var value))
		{
			if (ParseClientId(value).GetState(m_Driver) != NetworkConnection.State.Connected)
			{
				return RemoteConnectionState.Stopped;
			}
			return RemoteConnectionState.Started;
		}
		return RemoteConnectionState.Stopped;
	}

	public override void HandleClientConnectionState(ClientConnectionStateArgs connectionStateArgs)
	{
		OnClientConnectionState?.Invoke(connectionStateArgs);
	}

	public override void HandleServerConnectionState(ServerConnectionStateArgs connectionStateArgs)
	{
		OnServerConnectionState?.Invoke(connectionStateArgs);
	}

	public override void HandleRemoteConnectionState(RemoteConnectionStateArgs connectionStateArgs)
	{
		OnRemoteConnectionState?.Invoke(connectionStateArgs);
	}

	public override void IterateIncoming(bool server)
	{
		if (m_ClientState == LocalConnectionState.Started && m_ServerState == LocalConnectionState.Started)
		{
			IterateClientHost(server);
		}
		IterateIncoming();
	}

	public override void IterateOutgoing(bool server)
	{
		if (server || m_ServerState != LocalConnectionState.Started)
		{
			IterateOutgoing();
		}
	}

	public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs receivedDataArgs)
	{
		OnClientReceivedData?.Invoke(receivedDataArgs);
	}

	public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs receivedDataArgs)
	{
		OnServerReceivedData?.Invoke(receivedDataArgs);
	}

	public override void SendToServer(byte channelId, ArraySegment<byte> segment)
	{
		if (m_ClientState == LocalConnectionState.Started)
		{
			if (m_ServerState == LocalConnectionState.Started)
			{
				ClientHostSendToServer(channelId, segment);
			}
			else
			{
				Send(m_ServerClientId, segment, (Channel)channelId);
			}
		}
	}

	public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
	{
		if (m_ServerState == LocalConnectionState.Started && m_TransportIdToClientIdMap.TryGetValue(connectionId, out var value))
		{
			if (m_ClientState == LocalConnectionState.Started && value == m_ServerClientId)
			{
				SendToClientHost(channelId, segment);
			}
			else
			{
				Send(value, segment, (Channel)channelId);
			}
		}
	}

	public override int GetMaximumClients()
	{
		return m_MaximumClients;
	}

	public override void SetMaximumClients(int value)
	{
		if (m_ServerState.IsStartingOrStarted())
		{
			base.NetworkManager.LogWarning("Cannot set maximum clients when server is running.");
		}
		else
		{
			m_MaximumClients = value;
		}
	}

	public override void SetClientAddress(string address)
	{
		ConnectionData.Address = address;
	}

	public override string GetClientAddress()
	{
		return ConnectionData.Address;
	}

	public override void SetPort(ushort port)
	{
		ConnectionData.Port = port;
	}

	public override ushort GetPort()
	{
		return ConnectionData.Port;
	}

	public override void SetServerBindAddress(string address, IPAddressType addressType)
	{
		ConnectionData.ServerListenAddress = address;
	}

	public override string GetServerBindAddress(IPAddressType addressType)
	{
		return ConnectionData.ServerListenAddress;
	}

	public override bool StartConnection(bool server)
	{
		if (!server)
		{
			return StartClient();
		}
		return StartServer();
	}

	public override bool StopConnection(bool server)
	{
		if (!server)
		{
			return StopClient();
		}
		return StopServer();
	}

	public override bool StopConnection(int connectionId, bool immediately)
	{
		if (!m_TransportIdToClientIdMap.TryGetValue(connectionId, out var value))
		{
			return false;
		}
		if (value != m_ServerClientId)
		{
			return DisconnectRemoteClient(value);
		}
		return ServerRequestedStopClientHost();
	}

	public override void Shutdown()
	{
		StopConnection(server: false);
		StopConnection(server: true);
	}

	public override int GetMTU(byte channelId)
	{
		return 1400;
	}

	private Channel SelectSendChannel(NetworkPipeline pipeline)
	{
		if (!(pipeline == m_ReliableSequencedPipeline))
		{
			return Channel.Unreliable;
		}
		return Channel.Reliable;
	}

	private int TransportIdToClientId(ulong connection)
	{
		return m_ClientIdToTransportIdMap[connection];
	}

	private ulong ClientIdToTransportId(int transportId)
	{
		return m_TransportIdToClientIdMap[transportId];
	}

	private void HandleRemoteConnectionState(RemoteConnectionState state, ulong clientId)
	{
		switch (state)
		{
		case RemoteConnectionState.Started:
			if (m_TransportIdToClientIdMap.Count >= GetMaximumClients())
			{
				UnityEngine.Debug.LogWarning("Connection limit reached. Server cannot accept new connections.");
				NetworkConnection connection = ParseClientId(clientId);
				if (m_Driver.GetConnectionState(connection) != NetworkConnection.State.Disconnected)
				{
					m_Driver.Disconnect(connection);
					DataStreamReader reader;
					while (m_Driver.PopEventForConnection(connection, out reader) != NetworkEvent.Type.Empty)
					{
					}
				}
			}
			else
			{
				int num = m_NextClientId++;
				m_TransportIdToClientIdMap[num] = clientId;
				m_ClientIdToTransportIdMap[clientId] = num;
				HandleRemoteConnectionState(new RemoteConnectionStateArgs(state, num, base.Index));
			}
			break;
		case RemoteConnectionState.Stopped:
		{
			int num = m_ClientIdToTransportIdMap[clientId];
			HandleRemoteConnectionState(new RemoteConnectionStateArgs(state, num, base.Index));
			m_TransportIdToClientIdMap.Remove(num);
			m_ClientIdToTransportIdMap.Remove(clientId);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	private void SetServerConnectionState(LocalConnectionState state)
	{
		if (m_ServerState != state)
		{
			m_ServerState = state;
			HandleServerConnectionState(new ServerConnectionStateArgs(state, base.Index));
		}
	}

	private bool StopServer()
	{
		if (m_ServerState.IsStoppingOrStopped())
		{
			return false;
		}
		if (m_ClientState.IsStartingOrStarted())
		{
			ServerRequestedStopClientHost();
		}
		ulong[] array = m_ClientIdToTransportIdMap.Keys.ToArray();
		foreach (ulong clientId in array)
		{
			DisconnectRemoteClient(clientId);
		}
		SetServerConnectionState(LocalConnectionState.Stopping);
		ShutdownInternals();
		m_NextClientId = 1;
		m_TransportIdToClientIdMap.Clear();
		m_ClientIdToTransportIdMap.Clear();
		SetServerConnectionState(LocalConnectionState.Stopped);
		return true;
	}

	private void SetClientConnectionState(LocalConnectionState state)
	{
		if (m_ClientState != state)
		{
			m_ClientState = state;
			HandleClientConnectionState(new ClientConnectionStateArgs(state, base.Index));
		}
	}

	private bool StopClient()
	{
		if (m_ClientState.IsStoppingOrStopped())
		{
			return false;
		}
		if (m_ServerState.IsStartingOrStarted())
		{
			return StopClientHost();
		}
		DisconnectLocalClient();
		return true;
	}

	private bool StopClientHost()
	{
		if (m_ClientState.IsStoppingOrStopped())
		{
			return false;
		}
		SetClientConnectionState(LocalConnectionState.Stopping);
		DisposeClientHost();
		SetClientConnectionState(LocalConnectionState.Stopped);
		if (m_ServerState == LocalConnectionState.Started)
		{
			HandleRemoteConnectionState(RemoteConnectionState.Stopped, m_ServerClientId);
		}
		return true;
	}

	private bool ServerRequestedStopClientHost()
	{
		if (m_ClientState.IsStoppingOrStopped())
		{
			return false;
		}
		if (m_ServerState == LocalConnectionState.Started)
		{
			HandleRemoteConnectionState(RemoteConnectionState.Stopped, m_ServerClientId);
			SetClientConnectionState(LocalConnectionState.Stopping);
			DisposeClientHost();
			SetClientConnectionState(LocalConnectionState.Stopped);
		}
		return true;
	}

	private void DisposeClientHost()
	{
		m_ServerClientId = 0uL;
		m_ClientHostSendQueue.Clear();
		m_ClientHostReceiveQueue.Clear();
	}

	private void IterateClientHost(bool asServer)
	{
		if (asServer)
		{
			while (m_ClientHostSendQueue != null && m_ClientHostSendQueue.Count > 0)
			{
				ClientHostSendData clientHostSendData = m_ClientHostSendQueue.Dequeue();
				int connectionId = TransportIdToClientId(0uL);
				HandleServerReceivedDataArgs(new ServerReceivedDataArgs(clientHostSendData.Segment, clientHostSendData.Channel, connectionId, base.Index));
			}
		}
		else
		{
			while (m_ClientHostReceiveQueue != null && m_ClientHostReceiveQueue.Count > 0)
			{
				ClientHostSendData clientHostSendData2 = m_ClientHostReceiveQueue.Dequeue();
				HandleClientReceivedDataArgs(new ClientReceivedDataArgs(clientHostSendData2.Segment, clientHostSendData2.Channel, base.Index));
			}
		}
	}

	private void SendToClientHost(int channelId, ArraySegment<byte> payload)
	{
		m_ClientHostReceiveQueue.Enqueue(new ClientHostSendData((Channel)channelId, payload));
	}

	private void ClientHostSendToServer(int channelId, ArraySegment<byte> payload)
	{
		m_ClientHostSendQueue.Enqueue(new ClientHostSendData((Channel)channelId, payload));
	}
}

}