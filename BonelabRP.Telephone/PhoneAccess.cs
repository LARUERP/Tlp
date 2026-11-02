using System;
using System.Collections.Generic;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneAccess
{
	public sealed class Lease
	{
		public ulong Owner;

		public ushort Device;

		public string Token;

		public double Until;

		public bool BankAuthorized;
	}

	private readonly Dictionary<ulong, Lease> leases = new Dictionary<ulong, Lease>();

	private readonly Dictionary<(ulong, ulong), (int Count, double Until)> failures = new Dictionary<(ulong, ulong), (int, double)>();

	public Lease Get(ulong actor)
	{
		Lease value;
		return leases.TryGetValue(actor, out value) ? value : null;
	}

	public void Clear()
	{
		leases.Clear();
		failures.Clear();
	}

	public void Lock(ulong actor)
	{
		leases.Remove(actor);
	}

	public string Unlock(ulong actor, ulong owner, ushort device, string pin, bool held, Func<string, bool> verify, double now)
	{
		if (!held)
		{
			throw new InvalidOperationException("Tenir ce téléphone pour le déverrouiller.");
		}
		(ulong, ulong) key = (actor, owner);
		failures.TryGetValue(key, out (int, double) value);
		if (now < value.Item2)
		{
			throw new InvalidOperationException("Trop de tentatives. Attendre trente secondes.");
		}
		if (!verify(pin))
		{
			value.Item1++;
			if (value.Item1 >= 5)
			{
				value.Item1 = 0;
				value.Item2 = now + 30.0;
			}
			failures[key] = value;
			throw new InvalidOperationException("Code incorrect.");
		}
		failures.Remove(key);
		ulong[] array = (from p in leases
			where p.Value.Device == device
			select p.Key).ToArray();
		foreach (ulong key2 in array)
		{
			leases.Remove(key2);
		}
		string text = Guid.NewGuid().ToString("N");
		leases[actor] = new Lease
		{
			Owner = owner,
			Device = device,
			Token = text,
			Until = now + 8.0
		};
		return text;
	}

	public bool Valid(ulong actor, ulong owner, ushort device, string token, bool held, double now)
	{
		Lease value;
		return held && leases.TryGetValue(actor, out value) && value.Owner == owner && value.Device == device && value.Token == token && now < value.Until;
	}

	public bool Renew(ulong actor, ulong owner, ushort device, string token, bool held, double now)
	{
		if (!Valid(actor, owner, device, token, held, now))
		{
			Lock(actor);
			return false;
		}
		leases[actor].Until = now + 8.0;
		return true;
	}

	public ulong[] Expire(double now, Func<ulong, Lease, bool> held)
	{
		ulong[] array = (from p in leases
			where now >= p.Value.Until || !held(p.Key, p.Value)
			select p.Key).ToArray();
		ulong[] array2 = array;
		foreach (ulong actor in array2)
		{
			Lock(actor);
		}
		return array;
	}

	public ulong[] Readers(ulong owner)
	{
		return (from p in leases
			where p.Value.Owner == owner
			select p.Key).ToArray();
	}
}
