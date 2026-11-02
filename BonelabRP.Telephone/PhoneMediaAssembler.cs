using System;
using System.Collections.Generic;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneMediaAssembler
{
	private sealed class Pending
	{
		public PhoneMediaChunk First;

		public byte[][] Parts;

		public int Received;

		public double Started;
	}

	private readonly Dictionary<string, Pending> pending = new Dictionary<string, Pending>();

	public void Clear()
	{
		pending.Clear();
	}

	public void Expire(double now)
	{
		string[] array = (from p in pending
			where now - p.Value.Started > (double)(PhoneMediaRules.Stream(p.Value.First.Kind) ? 2 : 15)
			select p.Key).ToArray();
		foreach (string key in array)
		{
			pending.Remove(key);
		}
	}

	public byte[] Add(ulong actor, PhoneMediaChunk c, double now)
	{
		Expire(now);
		if (!PhoneMediaRules.ValidChunk(c))
		{
			return null;
		}
		string prefix = actor + "/";
		string key = prefix + c.Session + "/" + c.Kind + "/" + c.Id;
		if (!this.pending.TryGetValue(key, out var value))
		{
			if (PhoneMediaRules.Stream(c.Kind))
			{
				KeyValuePair<string, Pending>[] array = (from x in this.pending
					where x.Key.StartsWith(prefix, StringComparison.Ordinal) && PhoneMediaRules.Stream(x.Value.First.Kind) && x.Value.First.Call == c.Call
					orderby x.Value.First.Frame
					select x).ToArray();
				if (array.Length >= 2)
				{
					if (c.Frame <= array[0].Value.First.Frame)
					{
						return null;
					}
					this.pending.Remove(array[0].Key);
				}
			}
			if (this.pending.Count >= 32 || this.pending.Keys.Count((string k) => k.StartsWith(prefix, StringComparison.Ordinal)) >= 3)
			{
				return null;
			}
			value = (this.pending[key] = new Pending
			{
				First = c,
				Parts = new byte[c.Count][],
				Started = now
			});
		}
		PhoneMediaChunk first = value.First;
		if (c.Count != first.Count || c.Total != first.Total || c.Call != first.Call || c.Frame != first.Frame || c.Owner != first.Owner || c.Device != first.Device || c.Access != first.Access)
		{
			this.pending.Remove(key);
			return null;
		}
		if (value.Parts[c.Index] != null)
		{
			if (!value.Parts[c.Index].SequenceEqual(c.Bytes))
			{
				this.pending.Remove(key);
			}
			return null;
		}
		value.Parts[c.Index] = (byte[])c.Bytes.Clone();
		value.Received++;
		if (value.Received != c.Count)
		{
			return null;
		}
		byte[] array2 = new byte[c.Total];
		for (int num = 0; num < c.Count; num++)
		{
			value.Parts[num].CopyTo(array2, num * 600);
		}
		this.pending.Remove(key);
		return array2;
	}
}
