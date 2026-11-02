using System;
using System.Collections.Generic;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneCalls
{
	private sealed class Call
	{
		public string Id = Guid.NewGuid().ToString("N");

		public string State = "Ringing";

		public ulong Caller;

		public ulong Callee;

		public bool CallerSpeaker;

		public bool CalleeSpeaker;

		public bool VideoRequested;

		public bool CallerVideo;

		public bool CalleeVideo;

		public bool Masked;

		public bool Answered;

		public double AnsweredAt;

		public DateTime Utc = DateTime.UtcNow;

		public string CallerToken = "";

		public string CalleeToken = "";

		public double Created;

		public double CallerSeen;

		public double CalleeSeen;
	}

	private readonly Dictionary<ulong, Call> byPlayer = new Dictionary<ulong, Call>();

	public Func<double> Clock { get; set; } = () => (double)Environment.TickCount64 / 1000.0;

	public event Action<PhoneCallResult> Finished;

	public bool Busy(ulong player)
	{
		return byPlayer.ContainsKey(player);
	}

	public ulong[] Start(ulong caller, ulong callee, double now, bool video = false, bool masked = false)
	{
		if (caller == callee)
		{
			throw new InvalidOperationException("Impossible de s'appeler soi-même.");
		}
		if (Busy(caller))
		{
			throw new InvalidOperationException("Tu es déjà en appel.");
		}
		if (Busy(callee))
		{
			throw new InvalidOperationException("Ce correspondant est occupé.");
		}
		Call value = new Call
		{
			Caller = caller,
			Callee = callee,
			Created = now,
			CallerSeen = now,
			CalleeSeen = now,
			VideoRequested = video,
			CallerVideo = video,
			Masked = masked
		};
		byPlayer.Add(caller, value);
		byPlayer.Add(callee, value);
		return new ulong[2] { caller, callee };
	}

	private Call Get(ulong actor, string token)
	{
		if (!byPlayer.TryGetValue(actor, out var value) || string.IsNullOrEmpty(token) || value.Id != token)
		{
			throw new InvalidOperationException("Cet appel est terminé.");
		}
		return value;
	}

	public ulong[] Answer(ulong actor, string token, double now, bool video = false)
	{
		Call call = Get(actor, token);
		if (actor != call.Callee || call.State != "Ringing")
		{
			throw new InvalidOperationException("Aucun appel entrant à accepter.");
		}
		if (now - call.Created >= 30.0)
		{
			throw new InvalidOperationException("Appel expiré.");
		}
		call.Answered = true;
		call.AnsweredAt = now;
		call.CalleeVideo = video;
		call.State = "Active";
		call.CallerSeen = (call.CalleeSeen = now);
		return new ulong[2] { call.Caller, call.Callee };
	}

	public ulong[] Reject(ulong actor, string token)
	{
		Call call = Get(actor, token);
		if (actor != call.Callee || call.State != "Ringing")
		{
			throw new InvalidOperationException("Aucun appel entrant à refuser.");
		}
		return End(call, "declined");
	}

	public ulong[] Hangup(ulong actor, string token)
	{
		Call call = Get(actor, token);
		return End(call, (call.State == "Active") ? "completed" : ((actor == call.Caller) ? "cancelled" : "declined"));
	}

	public void Heartbeat(ulong actor, string token, double now)
	{
		Call call = Get(actor, token);
		if (actor == call.Caller)
		{
			call.CallerSeen = now;
		}
		else
		{
			call.CalleeSeen = now;
		}
	}

	public bool TryRoute(ulong actor, string token, double now, out ulong recipient)
	{
		recipient = 0uL;
		if (!byPlayer.TryGetValue(actor, out var value) || value.Id != token || value.State != "Active" || now - value.CallerSeen >= 12.0 || now - value.CalleeSeen >= 12.0)
		{
			return false;
		}
		recipient = ((actor == value.Caller) ? value.Callee : value.Caller);
		return true;
	}

	public PhoneCallView View(ulong actor, Func<ulong, string> number)
	{
		if (!byPlayer.TryGetValue(actor, out var value))
		{
			return new PhoneCallView();
		}
		ulong num = ((actor == value.Caller) ? value.Callee : value.Caller);
		return new PhoneCallView
		{
			VideoRequested = value.VideoRequested,
			Video = ((actor == value.Caller) ? value.CallerVideo : value.CalleeVideo),
			PeerVideo = ((actor == value.Caller) ? value.CalleeVideo : value.CallerVideo),
			Id = value.Id,
			State = value.State,
			Peer = num,
			Masked = value.Masked,
			Number = ((value.Masked && actor == value.Callee) ? "" : number(num)),
			Speaker = ((actor == value.Caller) ? value.CallerSpeaker : value.CalleeSpeaker),
			PeerSpeaker = ((actor == value.Caller) ? value.CalleeSpeaker : value.CallerSpeaker),
			Incoming = (actor == value.Callee)
		};
	}

	public ulong[] SetVideo(ulong actor, string token, bool enabled)
	{
		Call call = Get(actor, token);
		if (call.State != "Active")
		{
			throw new InvalidOperationException("Accepter l'appel avant la caméra.");
		}
		if (actor == call.Caller)
		{
			call.CallerVideo = enabled;
		}
		else
		{
			call.CalleeVideo = enabled;
		}
		return new ulong[2] { call.Caller, call.Callee };
	}

	public bool VideoEnabled(ulong actor, string token)
	{
		Call value;
		return byPlayer.TryGetValue(actor, out value) && value.Id == token && value.State == "Active" && ((actor == value.Caller) ? value.CallerVideo : value.CalleeVideo);
	}

	public ulong[] SetSpeaker(ulong actor, string token, bool enabled)
	{
		Call call = Get(actor, token);
		if (call.State != "Active")
		{
			throw new InvalidOperationException("Accepter l'appel avant d'activer le haut-parleur.");
		}
		if (actor == call.Caller && call.CallerSpeaker != enabled)
		{
			call.CallerSpeaker = enabled;
			call.CallerToken = Guid.NewGuid().ToString("N");
		}
		if (actor == call.Callee && call.CalleeSpeaker != enabled)
		{
			call.CalleeSpeaker = enabled;
			call.CalleeToken = Guid.NewGuid().ToString("N");
		}
		return new ulong[2] { call.Caller, call.Callee };
	}

	public (ulong Owner, string Token)[] Speakers(ulong actor, string token)
	{
		if (!byPlayer.TryGetValue(actor, out var value) || value.Id != token || value.State != "Active")
		{
			return Array.Empty<(ulong, string)>();
		}
		List<(ulong, string)> list = new List<(ulong, string)>();
		if (value.CallerSpeaker)
		{
			list.Add((value.Caller, value.CallerToken));
		}
		if (value.CalleeSpeaker)
		{
			list.Add((value.Callee, value.CalleeToken));
		}
		return list.ToArray();
	}

	private ulong[] End(Call call, string outcome = null)
	{
		byPlayer.Remove(call.Caller);
		byPlayer.Remove(call.Callee);
		this.Finished?.Invoke(new PhoneCallResult
		{
			Id = call.Id,
			Caller = call.Caller,
			Callee = call.Callee,
			Masked = call.Masked,
			Answered = call.Answered,
			Utc = call.Utc,
			Outcome = (outcome ?? (call.Answered ? "completed" : "missed")),
			Seconds = (call.Answered ? ((int)Math.Min(86400.0, Math.Max(0.0, Clock() - call.AnsweredAt))) : 0)
		});
		return new ulong[2] { call.Caller, call.Callee };
	}

	public ulong[] Disconnect(ulong actor)
	{
		Call value;
		return byPlayer.TryGetValue(actor, out value) ? End(value) : Array.Empty<ulong>();
	}

	public ulong[] Tick(double now)
	{
		Call[] source = (from c in byPlayer.Values.Distinct()
			where (c.State == "Ringing") ? (now - c.Created >= 30.0) : (now - c.CallerSeen >= 12.0 || now - c.CalleeSeen >= 12.0)
			select c).ToArray();
		return source.SelectMany((Call c) => End(c)).Distinct().ToArray();
	}

	public ulong[] StopAll()
	{
		Call[] source = byPlayer.Values.Distinct().ToArray();
		return source.SelectMany((Call c) => End(c)).Distinct().ToArray();
	}
}
