using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.RPC;
using LabFusion.SDK.Metadata;
using LabFusion.Scene;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneDevices
{
	private sealed class Device
	{
		public NetworkEntity Entity;

		public GameObject Object;

		public bool Pending;

		public float Started;

		public int Token;

		public string Error = "";

		public string Barcode = "";

		public PhoneDeviceInfo Placement;

		public float Recalled;
	}

	private readonly Dictionary<ulong, Device> devices = new Dictionary<ulong, Device>();

	public Action<PhoneDeviceInfo, GameObject> Parked;

	private int generation;

	private long placements;

	private string cachedBarcode = "";

	private Spawnable cachedSpawnable;

	public ushort? Id(ulong player)
	{
		Device value;
		return (devices.TryGetValue(player, out value) && (Object)(object)value.Object != (Object)null && value.Object.activeInHierarchy && value.Entity != null && value.Entity.IsRegistered) ? new ushort?(value.Entity.ID) : ((ushort?)null);
	}

	public PhoneDeviceInfo[] Catalog(Func<ulong, bool> pin)
	{
		return (from p in devices.Keys
			where Id(p).HasValue
			select Info(p, Id(p).Value, pin(p))).ToArray();
	}

	private PhoneDeviceInfo Info(ulong owner, ushort id, bool pin)
	{
		PhoneDeviceInfo placement = devices[owner].Placement;
		return new PhoneDeviceInfo
		{
			Owner = owner,
			Device = id,
			PinSet = pin,
			Parked = (placement?.Parked ?? false),
			Placement = (placement?.Placement ?? 0),
			X = (placement?.X ?? 0f),
			Y = (placement?.Y ?? 0f),
			Z = (placement?.Z ?? 0f),
			Yaw = (placement?.Yaw ?? 0f)
		};
	}

	public void Tick(Func<ulong, ushort, bool> held)
	{
		foreach (KeyValuePair<ulong, Device> device in devices)
		{
			Device value = device.Value;
			ushort? num = Id(device.Key);
			if (num.HasValue)
			{
				bool flag = held(device.Key, num.Value);
				if ((value.Placement?.Parked ?? false) && flag)
				{
					value.Placement.Parked = false;
				}
			}
		}
	}

	private void Park(Device d, Vector3 position, float yaw, ulong owner, bool preserveRotation = false)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		d.Placement = new PhoneDeviceInfo
		{
			Owner = owner,
			Device = d.Entity.ID,
			Parked = true,
			Placement = ++placements,
			X = position.x,
			Y = position.y,
			Z = position.z,
			Yaw = yaw
		};
		d.Object.transform.position = position;
		if (!preserveRotation)
		{
			d.Object.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
		}
		foreach (Rigidbody componentsInChild in d.Object.GetComponentsInChildren<Rigidbody>())
		{
			componentsInChild.velocity = Vector3.zero;
			componentsInChild.angularVelocity = Vector3.zero;
		}
		Parked?.Invoke(d.Placement, d.Object);
	}

	public string Status(ulong player)
	{
		if (!devices.TryGetValue(player, out var value))
		{
			return "En attente du téléphone.";
		}
		if (value.Error != "")
		{
			return value.Error;
		}
		if (value.Pending)
		{
			return "Téléphone en préparation…";
		}
		object result;
		if (!Id(player).HasValue)
		{
			result = "Recréer le téléphone depuis BoneMenu.";
		}
		else
		{
			PhoneDeviceInfo placement = value.Placement;
			result = ((placement != null && placement.Parked) ? "Téléphone en attente de prise en main." : "Téléphone disponible.");
		}
		return (string)result;
	}

	public void Remove(ulong player)
	{
		if (devices.TryGetValue(player, out var value))
		{
			value.Token++;
			Despawn(value.Entity);
			devices.Remove(player);
		}
	}

	public void Reset()
	{
		generation++;
		cachedSpawnable = null;
		cachedBarcode = "";
		foreach (Device value in devices.Values)
		{
			value.Token++;
			Despawn(value.Entity);
		}
		devices.Clear();
	}

	public void Ensure(PlayerID player, PhoneConfiguration config, bool replace = false, Func<ulong, ushort, bool> held = null)
	{
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_0472: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Expected O, but got Unknown
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Expected O, but got Unknown
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		if (!NetworkInfo.IsHost || player == null || !player.IsValid || !NetworkSceneManager.InCurrentLevel(player))
		{
			return;
		}
		if (replace && devices.TryGetValue(player.PlatformID, out var value))
		{
			if (value.Pending)
			{
				throw new InvalidOperationException("Téléphone déjà en préparation.");
			}
			if (Id(player.PlatformID).HasValue && value.Barcode == config.Barcode)
			{
				if (held != null && held(player.PlatformID, Id(player.PlatformID).Value))
				{
					throw new InvalidOperationException("Relâcher le téléphone avant de le rappeler.");
				}
				if (Time.realtimeSinceStartup - value.Recalled < 2f)
				{
					throw new InvalidOperationException("Attendre avant de rappeler le téléphone.");
				}
				NetworkPlayer val = default(NetworkPlayer);
				if (NetworkPlayerManager.TryGetPlayer(player.SmallID, ref val) && val.HasRig && !((Object)(object)val.RigRefs.Head == (Object)null))
				{
					NetworkEntityManager.TakeOwnership(value.Entity);
					Transform head = val.RigRefs.Head;
					Park(value, head.position + head.forward * 0.45f + Vector3.down * 0.35f, head.eulerAngles.y, player.PlatformID);
					value.Recalled = Time.realtimeSinceStartup;
					value.Error = "";
				}
				return;
			}
			Remove(player.PlatformID);
		}
		if (devices.TryGetValue(player.PlatformID, out var value2))
		{
			if (value2.Pending && Time.realtimeSinceStartup - value2.Started > 30f)
			{
				value2.Token++;
				value2.Pending = false;
				value2.Error = "Téléphone indisponible : vérifier le pallet puis recréer.";
			}
		}
		else
		{
			NetworkPlayer val2 = default(NetworkPlayer);
			if (((MetadataVariableT<bool>)(object)player.Metadata.Loading).GetValue() || !NetworkPlayerManager.TryGetPlayer(player.SmallID, ref val2) || !val2.HasRig || (Object)(object)val2.RigRefs.Head == (Object)null)
			{
				return;
			}
			AssetWarehouse instance = AssetWarehouse.Instance;
			if (instance == null)
			{
				return;
			}
			if (cachedSpawnable == null || cachedBarcode != config.Barcode)
			{
				SpawnableCrateReference val3 = new SpawnableCrateReference(config.Barcode);
				SpawnableCrate val4 = default(SpawnableCrate);
				if (!((CrateReferenceT<SpawnableCrate>)(object)val3).TryGetCrate(ref val4))
				{
					devices[player.PlatformID] = new Device
					{
						Error = "Pallet Phone absent : l'installer puis recréer le téléphone."
					};
					return;
				}
				cachedBarcode = config.Barcode;
				cachedSpawnable = new Spawnable
				{
					crateRef = val3
				};
			}
			Device d = new Device
			{
				Pending = true,
				Started = Time.realtimeSinceStartup,
				Barcode = config.Barcode
			};
			devices[player.PlatformID] = d;
			int epoch = generation;
			int token = d.Token;
			Transform head2 = val2.RigRefs.Head;
			Vector3 spawnPosition = head2.position + head2.forward * 0.45f + Vector3.down * 0.35f;
			float spawnYaw = head2.eulerAngles.y;
			NetworkAssetSpawner.Spawn(new SpawnRequestInfo
			{
				Spawnable = cachedSpawnable,
				Position = spawnPosition,
				Rotation = Quaternion.Euler(0f, spawnYaw, 0f),
				SpawnEffect = false,
				SpawnSource = (EntitySource)1,
				SpawnCallback = delegate(SpawnCallbackInfo info)
				{
					//IL_007d: Unknown result type (might be due to invalid IL or missing references)
					//IL_008e: Unknown result type (might be due to invalid IL or missing references)
					//IL_0099: Unknown result type (might be due to invalid IL or missing references)
					//IL_005a: Unknown result type (might be due to invalid IL or missing references)
					//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
					//IL_00de: Unknown result type (might be due to invalid IL or missing references)
					//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
					//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
					if (generation != epoch || token != d.Token || !devices.TryGetValue(player.PlatformID, out var value3) || value3 != d)
					{
						Despawn(info.Entity);
					}
					else
					{
						d.Pending = false;
						d.Entity = info.Entity;
						d.Object = info.Spawned;
						if (info.Entity != null && (Object)(object)info.Spawned != (Object)null)
						{
							Park(d, spawnPosition, spawnYaw, player.PlatformID);
						}
						if (info.Entity == null || (Object)(object)info.Spawned == (Object)null)
						{
							d.Error = "Échec du spawn : recréer le téléphone.";
						}
					}
				}
			});
		}
	}

	public static GameObject Resolve(ushort id)
	{
		NetworkEntity entity = NetworkEntityManager.IDManager.RegisteredEntities.GetEntity(id);
		object result;
		if (entity == null)
		{
			result = null;
		}
		else
		{
			IMarrowEntityExtender extender = entity.GetExtender<IMarrowEntityExtender>();
			if (extender == null)
			{
				result = null;
			}
			else
			{
				MarrowEntity marrowEntity = extender.MarrowEntity;
				result = ((marrowEntity != null) ? ((Component)marrowEntity).gameObject : null);
			}
		}
		return (GameObject)result;
	}

	private static void Despawn(NetworkEntity entity)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (entity != null && entity.IsRegistered && NetworkInfo.HasServer && NetworkInfo.IsHost)
		{
			NetworkAssetSpawner.Despawn(new DespawnRequestInfo
			{
				EntityID = entity.ID,
				DespawnEffect = false
			});
		}
	}
}
