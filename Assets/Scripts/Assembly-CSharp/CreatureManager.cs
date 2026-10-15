using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>在服务端检查鱼饵条件，按权重选择可钓对象并生成咬钩物品。</summary>
public class CreatureManager : NetworkBehaviour
{
    public static CreatureManager Instance;

    [SerializeField]
    private Fishable _defaultFishable;

    [SerializeField]
    private float _minBaitLevel = -2.5f;

    [SerializeField]
    private float _removeAliveFishLevel = -2.5f;

    private List<Creature> _aliveCreatures = new();

    private List<Creature> _aliveCreaturesToRemove = new();

    public const int DripCreatureChance = 10;

    private bool NetworkInitialize___EarlyCreatureManagerAssembly_002DCSharp_002Edll_Excuted;

    private bool NetworkInitialize___LateCreatureManagerAssembly_002DCSharp_002Edll_Excuted;

    public virtual void Awake()
    {
        NetworkInitialize___Early();
        Awake_UserLogic_CreatureManager_Assembly_002DCSharp_002Edll();
        NetworkInitialize___Late();
    }

    public override void OnStartServer()
    {
        base.TimeManager.OnPostTick += TickUpdate;
    }

    public override void OnStopServer()
    {
        base.TimeManager.OnPostTick -= TickUpdate;
    }

    public override void OnStopClient()
    {
        ClearFish();
    }

    public void AddAliveCreature(Creature creature)
    {
        _aliveCreatures.Add(creature);
    }

    private void TickUpdate()
    {
        FindFishForAllBaits();
    }

    private void FixedUpdate()
    {
        CheckUnderWaterCreatures();
    }

    private void CheckUnderWaterCreatures()
    {
        for (int i = 0; i < _aliveCreatures.Count; i++)
        {
            if (!_aliveCreatures[i] || _aliveCreatures[i].IsDead)
            {
                _aliveCreaturesToRemove.Add(_aliveCreatures[i]);
            }
            else if (_aliveCreatures[i].transform.position.y <= _removeAliveFishLevel && !_aliveCreatures[i].AttachedRod)
            {
                if (DazedUtils.TryMoveItemFromUnderLevel(_aliveCreatures[i]))
                {
                    Vector3 linearVelocity = _aliveCreatures[i].Rig.linearVelocity;
                    linearVelocity.y = 0f - linearVelocity.y;
                    _aliveCreatures[i].Rig.linearVelocity = linearVelocity;
                }
                else if (base.IsServerInitialized && _aliveCreatures[i].BossType == BossType.None && !_aliveCreatures[i].IgnoreDeathByWater && _aliveCreatures[i].IsServerInitialized && !_aliveCreatures[i].RigidbodySync.OnBoat)
                {
                    RemoveAliveCreature(_aliveCreatures[i]);
                }
            }
        }

        for (int j = 0; j < _aliveCreaturesToRemove.Count; j++)
        {
            _aliveCreatures.Remove(_aliveCreaturesToRemove[j]);
        }

        _aliveCreaturesToRemove.Clear();
    }

    private void RemoveAliveCreature(Creature creature)
    {
        _aliveCreaturesToRemove.Add(creature);
        creature.DestroyItem(1);
    }

    private void FindFishForAllBaits()
    {
        foreach (Bait item in Bait.BaitsUnderWater)
        {
            FindFishForBait(item);
        }
    }

    private void FindFishForBait(Bait bait)
    {
        if (bait.transform.position.y >= _minBaitLevel || (bool)bait.ServerItemOnBait || Time.time - bait.FishingRod.TimeOfEquip < 0.25f || bait.TimeUnderWater < bait.RandomizedCatchTime)
        {
            return;
        }

        bait.ResetTimeUnderWater();
        if (!FishableIsBoss(bait.Info.ItemWeights[0].Fishable) || !BossManager.Boss)
        {
            Fishable randomItem = GetRandomItem(bait.transform.position, bait.Info.ItemWeights);
            if (!randomItem)
            {
                MonoBehaviour.print("Found no fishable : (");
            }
            else if ((bool)randomItem.ItemToSpawn)
            {
                HookItem(randomItem.ItemToSpawn, bait);
            }
        }
    }

    private void HookItem(Item itemPrefab, Bait bait)
    {
        Item item = Object.Instantiate(itemPrefab, bait.transform.position, Quaternion.identity);
        Creature creature = item.Creature;
        if ((bool)creature && creature.BossType == BossType.None)
        {
            int shinyRoll = Random.Range(0, 100);
            int shinyChance = 10 + bait.FishingRod.Holder.Inventory.WishboneShinyChance;
            if (shinyRoll < shinyChance)
            {
                creature.SetDrip();
            }
        }

        item.SetAttachedRod(bait.FishingRod);
        bait.AddItemOnBait(item, asServer: true);
        item.RigidbodySync.ServerSetSyncedSimulator(bait.FishingRod.Holder.Owner);
        Spawn(item.gameObject);
    }

    public Fishable GetRandomItem(Vector3 position, List<ItemInfoWeight> weights)
    {
        if (weights.Count == 0)
        {
            Debug.LogWarning("weights.Count är 0, ger default fisk (current bait eller explosive är inte uppsatt)");
            return _defaultFishable;
        }

        float totalWeight = 0f;
        // Preserve the original terrain query; its hit result is not used by the selection rule.
        Physics.Raycast(position, Vector3.down, out _, 100f, GameInfo.LevelLayer);

        foreach (ItemInfoWeight weight in weights)
        {
            totalWeight += weight.Weight;
        }

        float randomWeight = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            if (randomWeight <= weights[i].Weight + cumulativeWeight)
            {
                if (FishableIsBoss(weights[i].Fishable) && (bool)BossManager.Boss)
                {
                    return _defaultFishable;
                }

                return weights[i].Fishable;
            }

            cumulativeWeight += weights[i].Weight;
        }

        Debug.LogWarning("Hittade ingen fisk, ger default fisk (detta borde inte hända)");
        return _defaultFishable;
    }

    private void ClearFish()
    {
        _aliveCreatures.Clear();
    }

    private bool FishableIsBoss(Fishable fishable)
    {
        if (!fishable || !fishable.ItemToSpawn)
        {
            return false;
        }

        Creature creature = fishable.ItemToSpawn as Creature;
        if ((bool)creature)
        {
            return creature.BossType != BossType.None;
        }

        return false;
    }

    public override void NetworkInitialize___Early()
    {
        if (!NetworkInitialize___EarlyCreatureManagerAssembly_002DCSharp_002Edll_Excuted)
        {
            NetworkInitialize___EarlyCreatureManagerAssembly_002DCSharp_002Edll_Excuted = true;
            base.NetworkInitialize___Early();
        }
    }

    public override void NetworkInitialize___Late()
    {
        if (!NetworkInitialize___LateCreatureManagerAssembly_002DCSharp_002Edll_Excuted)
        {
            NetworkInitialize___LateCreatureManagerAssembly_002DCSharp_002Edll_Excuted = true;
            base.NetworkInitialize___Late();
        }
    }

    public override void NetworkInitializeIfDisabled()
    {
        NetworkInitialize___Early();
        NetworkInitialize___Late();
    }

    private void Awake_UserLogic_CreatureManager_Assembly_002DCSharp_002Edll()
    {
        Setter.SetSingleInstance(ref Instance, this);
    }
}
