using System;
using System.Collections.Generic;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneLive
{
	private sealed class Stream
	{
		public string Id;

		public string Handle;

		public ulong Host;

		public ulong Owner;

		public ushort Device;

		public bool Front;

		public double Until;

		public Dictionary<ulong, double> Viewers = new Dictionary<ulong, double>();

		public HashSet<ulong> Likes = new HashSet<ulong>();

		public List<WindyLiveComment> Comments = new List<WindyLiveComment>();

		public Dictionary<ulong, double> CommentTimes = new Dictionary<ulong, double>();
	}

	public const int MaxStreams = 4;

	public const int MaxViewers = 16;

	public const int MaxComment = 140;

	private readonly Dictionary<string, Stream> streams = new Dictionary<string, Stream>();

	private readonly Dictionary<ulong, string> watching = new Dictionary<ulong, string>();

	public void Clear()
	{
		streams.Clear();
		watching.Clear();
	}

	public string Casting(ulong actor)
	{
		return streams.Values.FirstOrDefault((Stream s) => s.Host == actor)?.Id ?? "";
	}

	public bool SourceValid(ulong actor, string id, Func<ulong, ulong, ushort, bool> available)
	{
		Stream value;
		return streams.TryGetValue(id ?? "", out value) && value.Host == actor && available(value.Host, value.Owner, value.Device);
	}

	public string Watching(ulong actor)
	{
		string value;
		return watching.TryGetValue(actor, out value) ? value : "";
	}

	public string Start(ulong actor, ulong owner, ushort device, string handle, bool front, double now)
	{
		handle = PhoneWindy.Handle(handle);
		if (Casting(actor) != "")
		{
			return Casting(actor);
		}
		if (streams.Count >= 4)
		{
			throw new InvalidOperationException("Quatre lives simultanés maximum.");
		}
		Leave(actor);
		Stream stream = new Stream
		{
			Id = Guid.NewGuid().ToString("N"),
			Host = actor,
			Owner = owner,
			Device = device,
			Handle = handle,
			Front = front,
			Until = now + 10.0
		};
		streams[stream.Id] = stream;
		return stream.Id;
	}

	public void Join(ulong actor, string id, double now)
	{
		if (Casting(actor) != "")
		{
			throw new InvalidOperationException("Arrêter ton live avant d'en rejoindre un autre.");
		}
		Stream stream = Find(id);
		if (stream.Host == actor)
		{
			throw new InvalidOperationException("Tu diffuses déjà ce live.");
		}
		if (!stream.Viewers.ContainsKey(actor) && stream.Viewers.Count >= 16)
		{
			throw new InvalidOperationException("Ce live est complet (16 spectateurs).");
		}
		Leave(actor);
		watching[actor] = id;
		stream.Viewers[actor] = now + 10.0;
	}

	public void Leave(ulong actor)
	{
		if (watching.Remove(actor, out var value) && streams.TryGetValue(value, out var value2))
		{
			value2.Viewers.Remove(actor);
		}
	}

	public void Stop(ulong actor, string id)
	{
		Stream stream = Find(id);
		if (stream.Host != actor)
		{
			throw new InvalidOperationException("Seul l'auteur peut arrêter ce live.");
		}
		ulong[] array = stream.Viewers.Keys.ToArray();
		foreach (ulong key in array)
		{
			watching.Remove(key);
		}
		streams.Remove(id);
	}

	public void Camera(ulong actor, string id, bool front)
	{
		Stream stream = Find(id);
		if (stream.Host != actor)
		{
			throw new InvalidOperationException("Caméra réservée à l'auteur.");
		}
		stream.Front = front;
	}

	public void Pulse(ulong actor, double now)
	{
		string value = Casting(actor);
		if (value != "")
		{
			streams[value].Until = now + 10.0;
		}
		if (watching.TryGetValue(actor, out value) && streams.TryGetValue(value, out var value2))
		{
			value2.Viewers[actor] = now + 10.0;
		}
	}

	public void Like(ulong actor, string id)
	{
		Stream stream = Participant(actor, id);
		stream.Likes.Add(actor);
	}

	public void Comment(ulong actor, string id, string handle, string text, double now)
	{
		Stream stream = Participant(actor, id);
		text = (text ?? "").Trim();
		if (text.Length < 1 || text.Length > 140 || text.Any(char.IsControl))
		{
			throw new InvalidOperationException("Commentaire : 1 à 140 caractères.");
		}
		if (stream.CommentTimes.TryGetValue(actor, out var value) && now - value < 1.0)
		{
			throw new InvalidOperationException("Attendre une seconde entre deux commentaires.");
		}
		handle = PhoneWindy.Handle(handle);
		stream.CommentTimes[actor] = now;
		stream.Comments.Add(new WindyLiveComment
		{
			Handle = handle,
			Text = text
		});
		while (stream.Comments.Count > 12)
		{
			stream.Comments.RemoveAt(0);
		}
	}

	private Stream Find(string id)
	{
		if (id == null || !streams.TryGetValue(id, out var value))
		{
			throw new InvalidOperationException("Ce live est terminé.");
		}
		return value;
	}

	private Stream Participant(ulong actor, string id)
	{
		Stream stream = Find(id);
		if (stream.Host != actor && Watching(actor) != id)
		{
			throw new InvalidOperationException("Rejoindre le live avant d'interagir.");
		}
		return stream;
	}

	public ulong[] Targets(ulong actor, string id, double now)
	{
		Stream value;
		return (streams.TryGetValue(id, out value) && value.Host == actor && now < value.Until) ? (from p in value.Viewers
			where now < p.Value
			select p.Key).ToArray() : Array.Empty<ulong>();
	}

	public bool Routes(string id, ulong target, double now)
	{
		Stream value;
		double value2;
		return streams.TryGetValue(id, out value) && now < value.Until && value.Viewers.TryGetValue(target, out value2) && now < value2;
	}

	public bool Tick(double now, Func<ulong, ulong, ushort, bool> available)
	{
		bool result = false;
		Stream[] array = streams.Values.ToArray();
		foreach (Stream stream in array)
		{
			if (now >= stream.Until || !available(stream.Host, stream.Owner, stream.Device))
			{
				Stop(stream.Host, stream.Id);
				result = true;
				continue;
			}
			ulong[] array2 = (from p in stream.Viewers
				where now >= p.Value
				select p.Key).ToArray();
			foreach (ulong actor in array2)
			{
				Leave(actor);
				result = true;
			}
		}
		return result;
	}

	public void Disconnect(ulong actor)
	{
		Leave(actor);
		string text = Casting(actor);
		if (text != "")
		{
			Stop(actor, text);
		}
	}

	private WindyLiveSummary Summary(Stream s)
	{
		return new WindyLiveSummary
		{
			Id = s.Id,
			Host = s.Host,
			Owner = s.Owner,
			Device = s.Device,
			Handle = s.Handle,
			Front = s.Front,
			Viewers = s.Viewers.Count,
			Likes = s.Likes.Count
		};
	}

	public WindyLiveView View(ulong actor)
	{
		WindyLiveView windyLiveView = new WindyLiveView
		{
			Player = actor,
			Available = streams.Values.Select(Summary).ToList()
		};
		string text = Casting(actor);
		windyLiveView.Casting = text != "";
		if (text == "")
		{
			text = Watching(actor);
		}
		if (text != "" && streams.TryGetValue(text, out var value))
		{
			windyLiveView.Active = Summary(value);
			windyLiveView.Liked = value.Likes.Contains(actor);
			windyLiveView.Comments = value.Comments.Select((WindyLiveComment c) => new WindyLiveComment
			{
				Handle = c.Handle,
				Text = c.Text
			}).ToList();
		}
		return windyLiveView;
	}
}
