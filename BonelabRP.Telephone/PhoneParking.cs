using System.Collections.Generic;
using System.Linq;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Marrow;
using LabFusion.Entities;
using LabFusion.Player;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneParking
{
	private sealed class State
	{
		public GameObject Body;

		public Rigidbody[] Bodies;

		public bool[] OriginalGravity;

		public long Revision;

		public bool Suspended;

		public bool Released;
	}

	private readonly Dictionary<ushort, State> states = new Dictionary<ushort, State>();

	private static bool Holds(Hand hand, GameObject body)
	{
		GameObject val = ((hand != null) ? hand.m_CurrentAttachedGO : null);
		return (Object)(object)val != (Object)null && ((Object)(object)val == (Object)(object)body || val.transform.IsChildOf(body.transform) || body.transform.IsChildOf(val.transform));
	}

	private static bool AnyoneHolds(GameObject body)
	{
		if (Holds(Player.LeftHand, body) || Holds(Player.RightHand, body))
		{
			return true;
		}
		NetworkPlayer val = default(NetworkPlayer);
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (playerID != null && playerID.IsValid && NetworkPlayerManager.TryGetPlayer(playerID.SmallID, ref val) && val.HasRig && (Holds(val.RigRefs.LeftHand, body) || Holds(val.RigRefs.RightHand, body)))
			{
				return true;
			}
		}
		return false;
	}

	private static void Release(State s)
	{
		if (!s.Suspended)
		{
			return;
		}
		for (int i = 0; i < s.Bodies.Length; i++)
		{
			if ((Object)(object)s.Bodies[i] != (Object)null)
			{
				s.Bodies[i].useGravity = s.OriginalGravity[i];
			}
		}
		s.Suspended = false;
	}

	public void Hold(PhoneDeviceInfo d, GameObject supplied = null)
	{
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		if (!states.TryGetValue(d.Device, out var value))
		{
			if (!d.Parked)
			{
				return;
			}
			GameObject val = supplied ?? PhoneDevices.Resolve(d.Device);
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			Il2CppArrayBase<Rigidbody> componentsInChildren = val.GetComponentsInChildren<Rigidbody>();
			value = (states[d.Device] = new State
			{
				Body = val,
				Bodies = Il2CppArrayBase<Rigidbody>.op_Implicit(componentsInChildren),
				OriginalGravity = ((IEnumerable<Rigidbody>)componentsInChildren).Select((Rigidbody rb) => rb.useGravity).ToArray(),
				Revision = d.Placement
			});
		}
		if ((Object)(object)value.Body == (Object)null)
		{
			states.Remove(d.Device);
			return;
		}
		if (value.Revision != d.Placement)
		{
			Release(value);
			value.Revision = d.Placement;
			value.Released = false;
			value.OriginalGravity = value.Bodies.Select((Rigidbody rb) => (Object)(object)rb != (Object)null && rb.useGravity).ToArray();
		}
		if (!d.Parked || AnyoneHolds(value.Body))
		{
			Release(value);
			value.Released = true;
		}
		else
		{
			if (value.Released)
			{
				return;
			}
			if (value.Suspended)
			{
				Rigidbody[] bodies = value.Bodies;
				foreach (Rigidbody val2 in bodies)
				{
					if ((Object)(object)val2 != (Object)null)
					{
						val2.velocity = Vector3.zero;
						val2.angularVelocity = Vector3.zero;
					}
				}
				return;
			}
			Rigidbody[] bodies2 = value.Bodies;
			foreach (Rigidbody val3 in bodies2)
			{
				if ((Object)(object)val3 != (Object)null)
				{
					val3.velocity = Vector3.zero;
					val3.angularVelocity = Vector3.zero;
					val3.useGravity = false;
				}
			}
			value.Suspended = true;
		}
	}

	public void Tick(IEnumerable<PhoneDeviceInfo> catalog)
	{
		PhoneDeviceInfo[] array = catalog.ToArray();
		HashSet<ushort> active = array.Select((PhoneDeviceInfo phoneDeviceInfo) => phoneDeviceInfo.Device).ToHashSet();
		ushort[] array2 = states.Keys.Where((ushort id) => !active.Contains(id)).ToArray();
		foreach (ushort key in array2)
		{
			Release(states[key]);
			states.Remove(key);
		}
		PhoneDeviceInfo[] array3 = array;
		foreach (PhoneDeviceInfo d in array3)
		{
			Hold(d);
		}
	}

	public void Clear()
	{
		foreach (State value in states.Values)
		{
			Release(value);
		}
		states.Clear();
	}
}
