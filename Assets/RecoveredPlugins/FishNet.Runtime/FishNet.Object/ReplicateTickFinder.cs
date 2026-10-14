using FishNet.Object.Prediction;
using GameKit.Dependencies.Utilities.Types;

namespace FishNet.Object {
internal static class ReplicateTickFinder
{
	public enum DataPlacementResult
	{
		Error,
		Exact,
		InsertBeginning,
		InsertMiddle,
		InsertEnd
	}

	public static int GetReplicateHistoryIndex<T>(uint tick, RingBuffer<ReplicateDataContainer<T>> replicatesHistory, out DataPlacementResult findResult) where T : IReplicateData, new()
	{
		int replicatesCount = replicatesHistory.Count;
		if (replicatesCount == 0)
		{
			findResult = DataPlacementResult.InsertBeginning;
			return 0;
		}
		ReplicateDataContainer<T> replicateDataContainer = replicatesHistory[0];
		uint firstTick = replicateDataContainer.Data.GetTick();
		int num = (int)(tick - firstTick);
		if (num >= replicatesCount)
		{
			return FindIndexBruteForce(out findResult);
		}
		if (num < 0)
		{
			findResult = DataPlacementResult.InsertBeginning;
			return 0;
		}
		replicateDataContainer = replicatesHistory[num];
		if (replicateDataContainer.Data.GetTick() != tick)
		{
			return FindIndexBruteForce(out findResult);
		}
		findResult = DataPlacementResult.Exact;
		return num;
		int FindIndexBruteForce(out DataPlacementResult result)
		{
			if (tick < firstTick)
			{
				result = DataPlacementResult.InsertBeginning;
				return 0;
			}
			uint num2 = tick;
			ReplicateDataContainer<T> replicateDataContainer2 = replicatesHistory[replicatesCount - 1];
			if (num2 > replicateDataContainer2.Data.GetTick())
			{
				result = DataPlacementResult.InsertEnd;
				return replicatesCount;
			}
			for (int i = 0; i < replicatesCount; i++)
			{
				replicateDataContainer2 = replicatesHistory[i];
				uint tick2 = replicateDataContainer2.Data.GetTick();
				if (tick2 == tick)
				{
					result = DataPlacementResult.Exact;
					return i;
				}
				if (tick2 > tick)
				{
					result = DataPlacementResult.InsertMiddle;
					return i;
				}
			}
			result = DataPlacementResult.Error;
			return -1;
		}
	}
}

}