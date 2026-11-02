using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BonelabRP.Telephone;

public sealed class PhoneWindy
{
	public const int MaxPosts = 300;

	public const int MaxText = 280;

	public const int PageSize = 1;

	private readonly string file;

	private WindyDatabase db;

	public PhoneWindy(string file)
	{
		this.file = file;
		if (File.Exists(file) && new FileInfo(file).Length > 16777216)
		{
			throw new InvalidDataException("Base Windy trop volumineuse.");
		}
		db = (File.Exists(file) ? JsonSerializer.Deserialize<WindyDatabase>(File.ReadAllText(file)) : new WindyDatabase());
		Validate();
	}

	public static string Handle(string input)
	{
		string text = (input ?? "").Trim().ToLowerInvariant();
		if (text.Length < 3 || text.Length > 20 || text.Any((char c) => (c < 'a' || c > 'z') && (c < '0' || c > '9') && c != '_'))
		{
			throw new InvalidOperationException("Pseudo : 3 à 20 lettres, chiffres ou _.");
		}
		return text;
	}

	private void Validate()
	{
		if (db == null || db.Version != 1 || db.Accounts == null || db.Posts == null || db.Accounts.Count > 2048 || db.Posts.Count > 300)
		{
			throw new InvalidDataException("Base Windy invalide.");
		}
		if (db.Accounts.Any((WindyAccount a) => a == null) || db.Accounts.Select((WindyAccount a) => a.Player).Distinct().Count() != db.Accounts.Count || db.Accounts.Select((WindyAccount a) => a.Handle).Distinct().Count() != db.Accounts.Count)
		{
			throw new InvalidDataException("Comptes Windy dupliqués.");
		}
		foreach (WindyAccount account in db.Accounts)
		{
			if (account.Handle != Handle(account.Handle) || account.Nonces == null || account.Nonces.Count > 128 || account.Nonces.Any((string n) => !Guid.TryParseExact(n, "N", out var _)))
			{
				throw new InvalidDataException("Compte Windy invalide.");
			}
		}
		if (db.Posts.Any((WindyPost windyPost) => windyPost == null) || db.Posts.Select((WindyPost windyPost) => windyPost.Id).Distinct().Count() != db.Posts.Count)
		{
			throw new InvalidDataException("Publications Windy invalides.");
		}
		foreach (WindyPost p in db.Posts)
		{
			if (!Guid.TryParseExact(p.Id, "N", out var result) || !db.Accounts.Any((WindyAccount a) => a.Player == p.Author) || p.Text == null || p.Photo == null || p.Text.Length > 280 || p.Text.Any((char c) => char.IsControl(c) && c != '\n') || (p.Text.Trim() == "" && p.Photo == "") || (p.Photo != "" && !Guid.TryParseExact(p.Photo, "N", out result)))
			{
				throw new InvalidDataException("Publication Windy invalide.");
			}
		}
	}

	private void Commit(Action action)
	{
		byte[] array = JsonSerializer.SerializeToUtf8Bytes(db);
		try
		{
			action();
			Validate();
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			byte[] array2 = JsonSerializer.SerializeToUtf8Bytes(db);
			if (array2.Length > 16777216)
			{
				throw new InvalidOperationException("Stockage Windy plein.");
			}
			if (File.Exists(file))
			{
				File.Copy(file, file + ".bak", overwrite: true);
			}
			using (FileStream fileStream = new FileStream(file + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
			{
				fileStream.Write(array2);
				fileStream.Flush(flushToDisk: true);
			}
			File.Move(file + ".tmp", file, overwrite: true);
		}
		catch
		{
			db = JsonSerializer.Deserialize<WindyDatabase>(array);
			throw;
		}
		finally
		{
			if (File.Exists(file + ".tmp"))
			{
				File.Delete(file + ".tmp");
			}
		}
	}

	private static void Nonce(WindyAccount account, string nonce)
	{
		account.Nonces.Add(nonce);
		while (account.Nonces.Count > 128)
		{
			account.Nonces.RemoveAt(0);
		}
	}

	private static void CheckNonce(string nonce)
	{
		if (!Guid.TryParseExact(nonce, "N", out var _))
		{
			throw new InvalidOperationException("Commande Windy invalide.");
		}
	}

	public bool Register(ulong actor, string input, string nonce)
	{
		CheckNonce(nonce);
		string handle = Handle(input);
		WindyAccount windyAccount = db.Accounts.FirstOrDefault((WindyAccount a) => a.Player == actor);
		if (windyAccount != null)
		{
			if (windyAccount.Handle != handle)
			{
				throw new InvalidOperationException("Ton compte Windy existe déjà : @" + windyAccount.Handle);
			}
			return false;
		}
		if (db.Accounts.Any((WindyAccount a) => a.Handle == handle))
		{
			throw new InvalidOperationException("Ce pseudo Windy est déjà pris.");
		}
		if (db.Accounts.Count >= 2048)
		{
			throw new InvalidOperationException("Capacité des comptes Windy atteinte.");
		}
		Commit(delegate
		{
			db.Accounts.Add(new WindyAccount
			{
				Player = actor,
				Handle = handle,
				Nonces = new List<string> { nonce }
			});
		});
		return true;
	}

	public bool Post(ulong actor, string text, string photo, string nonce, DateTime utc)
	{
		CheckNonce(nonce);
		WindyAccount account = db.Accounts.FirstOrDefault((WindyAccount a) => a.Player == actor) ?? throw new InvalidOperationException("Créer ton compte Windy avant de publier.");
		if (account.Nonces.Contains(nonce))
		{
			return false;
		}
		text = (text ?? "").Trim();
		photo = photo ?? "";
		if (text.Length > 280 || text.Any((char c) => char.IsControl(c) && c != '\n') || (text == "" && photo == "") || (photo != "" && !Guid.TryParseExact(photo, "N", out var _)))
		{
			throw new InvalidOperationException("Publication : texte de 280 caractères maximum et/ou une photo.");
		}
		Commit(delegate
		{
			db.Posts.Add(new WindyPost
			{
				Id = Guid.NewGuid().ToString("N"),
				Author = actor,
				Text = text,
				Photo = photo,
				Utc = utc.ToUniversalTime()
			});
			while (db.Posts.Count > 300)
			{
				db.Posts.RemoveAt(0);
			}
			Nonce(account, nonce);
		});
		return true;
	}

	public bool PhotoReferenced(string id)
	{
		return !string.IsNullOrEmpty(id) && db.Posts.Any((WindyPost p) => p.Photo == id);
	}

	public WindyView View(ulong actor, int offset)
	{
		offset = Math.Max(0, Math.Min(Math.Max(0, db.Posts.Count - 1), offset));
		offset /= 1;
		return new WindyView
		{
			Player = actor,
			Owner = actor,
			Handle = (db.Accounts.FirstOrDefault((WindyAccount a) => a.Player == actor)?.Handle ?? ""),
			Offset = offset,
			Total = db.Posts.Count,
			Posts = (from p in db.Posts.AsEnumerable().Reverse().Skip(offset)
					.Take(1)
				select new WindyPostView
				{
					Id = p.Id,
					Handle = db.Accounts.First((WindyAccount a) => a.Player == p.Author).Handle,
					Text = p.Text,
					Photo = p.Photo,
					Utc = p.Utc,
					Mine = (p.Author == actor)
				}).ToList()
		};
	}
}
