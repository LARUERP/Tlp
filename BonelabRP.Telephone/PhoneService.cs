using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace BonelabRP.Telephone;

public sealed class PhoneService
{
	public const int MaxContacts = 100;

	public const int MaxMessages = 100;

	public const int MaxText = 320;

	private const int MaxAccounts = 2048;

	private PhoneDatabase db;

	private Dictionary<ulong, PhoneAccount> accounts;

	private Dictionary<string, PhoneAccount> numbers;

	private readonly string file;

	public int Count => accounts.Count;

	public PhoneService(string file)
	{
		this.file = file;
		if (File.Exists(file) && new FileInfo(file).Length > 16777216)
		{
			throw new InvalidDataException("Base téléphone trop volumineuse.");
		}
		db = (File.Exists(file) ? JsonSerializer.Deserialize<PhoneDatabase>(File.ReadAllText(file)) : new PhoneDatabase());
		Validate();
		Index();
		if (!db.RandomNumbers || db.Accounts.Any((PhoneAccount a) => a.PinHash != "" && a.PinDigits != 4))
		{
			Migrate();
		}
	}

	private static string RandomNumber(HashSet<string> occupied)
	{
		string text;
		do
		{
			text = "06" + RandomNumberGenerator.GetInt32(10000000, 100000000).ToString("D8", CultureInfo.InvariantCulture);
		}
		while (occupied.Contains(text));
		occupied.Add(text);
		return text;
	}

	private void Migrate()
	{
		Commit(delegate
		{
			if (!db.RandomNumbers)
			{
				HashSet<string> occupied = new HashSet<string>(db.Accounts.Select((PhoneAccount a) => a.Number));
				Dictionary<string, string> dictionary = db.Accounts.ToDictionary((PhoneAccount a) => a.Number, (PhoneAccount a) => RandomNumber(occupied));
				foreach (PhoneAccount account in db.Accounts)
				{
					account.Number = dictionary[account.Number];
					foreach (PhoneCallEntry call in account.Calls)
					{
						if (dictionary.TryGetValue(call.Number, out var value))
						{
							call.Number = value;
						}
					}
					foreach (PhoneContact contact in account.Contacts)
					{
						if (dictionary.TryGetValue(contact.Number, out var value2))
						{
							contact.Number = value2;
						}
					}
					foreach (PhoneSms message in account.Messages)
					{
						if (dictionary.TryGetValue(message.From, out var value3))
						{
							message.From = value3;
						}
						if (dictionary.TryGetValue(message.To, out var value4))
						{
							message.To = value4;
						}
					}
					foreach (PhoneBankEntry transaction in account.Transactions)
					{
						foreach (KeyValuePair<string, string> item in dictionary)
						{
							if (transaction.Label == "De " + Display(item.Key))
							{
								transaction.Label = "De " + Display(item.Value);
							}
							else if (transaction.Label == "Vers " + Display(item.Key))
							{
								transaction.Label = "Vers " + Display(item.Value);
							}
						}
					}
				}
				db.RandomNumbers = true;
			}
			foreach (PhoneAccount account2 in db.Accounts)
			{
				if (account2.PinDigits != 4)
				{
					string pinSalt = (account2.PinHash = "");
					account2.PinSalt = pinSalt;
					account2.PinDigits = 4;
				}
			}
		});
	}

	public static string Normalize(string number)
	{
		if (number == null || number.Length > 40)
		{
			throw new InvalidOperationException("Numéro invalide.");
		}
		string text = string.Concat(number.Where((char c) => c != ' ' && c != '-' && c != '.'));
		if (text.Length != 10 || text.Any((char c) => c < '0' || c > '9'))
		{
			throw new InvalidOperationException("Saisir un numéro RP de 10 chiffres.");
		}
		return text;
	}

	public static string Display(string number)
	{
		return (number != null && number.Length == 10) ? string.Join(" ", from i in Enumerable.Range(0, 5)
			select number.Substring(i * 2, 2)) : (number ?? "");
	}

	private void Index()
	{
		accounts = db.Accounts.ToDictionary((PhoneAccount a) => a.Player);
		numbers = db.Accounts.ToDictionary<PhoneAccount, string>((PhoneAccount a) => a.Number, StringComparer.Ordinal);
	}

	private void Validate()
	{
		if (db == null || db.Version != 1 || db.NextNumber < 1 || db.NextNumber > 100000000 || db.Accounts == null || db.Accounts.Count > 2048 || db.Accounts.Any((PhoneAccount a) => a == null))
		{
			throw new InvalidDataException("Base téléphone invalide. Conserver le fichier et vérifier son backup.");
		}
		if (db.Accounts.Select((PhoneAccount a) => a.Player).Distinct().Count() != db.Accounts.Count || db.Accounts.Select((PhoneAccount a) => a.Number).Distinct().Count() != db.Accounts.Count)
		{
			throw new InvalidDataException("Identités ou numéros dupliqués.");
		}
		foreach (PhoneAccount account in db.Accounts)
		{
			if (Normalize(account.Number) != account.Number || !account.Number.StartsWith("06") || account.Contacts == null || account.Messages == null || account.RecentCommands == null || account.Contacts.Count > 100 || account.Messages.Count > 100 || account.RecentCommands.Count > 128)
			{
				throw new InvalidDataException("Compte téléphone invalide.");
			}
			if (account.Wallpaper == null || (account.Wallpaper != "" && !Guid.TryParseExact(account.Wallpaper, "N", out var result)))
			{
				throw new InvalidDataException("Fond d’écran invalide.");
			}
			Guid result2;
			if (account.Calls == null || account.Calls.Count > 50 || account.Calls.Any(delegate(PhoneCallEntry c)
			{
				int result3;
				if (c != null && Guid.TryParseExact(c.Id, "N", out result2) && c.Number != null)
				{
					bool num;
					if (!(c.Number == ""))
					{
						num = Normalize(c.Number) != c.Number;
					}
					else
					{
						if (!c.Incoming)
						{
							goto IL_00b4;
						}
						num = !c.Masked;
					}
					if (!num && c.Seconds >= 0 && c.Seconds <= 86400)
					{
						result3 = ((!new string[4] { "completed", "missed", "cancelled", "declined" }.Contains(c.Outcome)) ? 1 : 0);
						goto IL_00b5;
					}
				}
				goto IL_00b4;
				IL_00b5:
				return (byte)result3 != 0;
				IL_00b4:
				result3 = 1;
				goto IL_00b5;
			}))
			{
				throw new InvalidDataException("Historique appels invalide.");
			}
			if (account.Transactions == null || account.Transactions.Count > 20 || account.Transactions.Any((PhoneBankEntry t) => t == null || !Guid.TryParseExact(t.Id, "N", out result2) || t.Label == null || t.Label.Length > 80))
			{
				throw new InvalidDataException("Historique banque invalide.");
			}
			if (account.GalleryDeleted == null || account.GalleryDeleted.Count > 200 || account.GalleryDeleted.Any((string id) => !Guid.TryParseExact(id, "N", out result2)))
			{
				throw new InvalidDataException("Historique Galerie invalide.");
			}
			if (account.Gallery == null || account.Gallery.Count > 100 || account.Gallery.Any((string id) => !Guid.TryParseExact(id, "N", out result2)) || account.Gallery.Distinct().Count() != account.Gallery.Count)
			{
				throw new InvalidDataException("Galerie invalide.");
			}
			if (account.PinSalt == null || account.PinHash == null || account.PinSalt == "" != (account.PinHash == ""))
			{
				throw new InvalidDataException("Code téléphone invalide.");
			}
			foreach (PhoneContact contact in account.Contacts)
			{
				if (contact == null || contact.Photo == null || (contact.Photo != "" && !Guid.TryParseExact(contact.Photo, "N", out result)) || Normalize(contact.Number) != contact.Number || string.IsNullOrWhiteSpace(contact.Name) || contact.Name.Length > 32 || contact.Name.Any(char.IsControl))
				{
					throw new InvalidDataException("Contact invalide.");
				}
			}
			if (account.Contacts.Select((PhoneContact c) => c.Number).Distinct().Count() != account.Contacts.Count)
			{
				throw new InvalidDataException("Contacts dupliqués.");
			}
			foreach (PhoneSms message in account.Messages)
			{
				if (message == null || message.Voice == null || (message.Voice != "" && (!Guid.TryParseExact(message.Voice, "N", out result) || message.VoiceSeconds < 1 || message.VoiceSeconds > 15)) || !Guid.TryParseExact(message.Id, "N", out result) || Normalize(message.From) != message.From || Normalize(message.To) != message.To || (message.From != account.Number && message.To != account.Number) || (string.IsNullOrWhiteSpace(message.Text) && string.IsNullOrEmpty(message.Photo) && string.IsNullOrEmpty(message.Voice)) || message.Text.Length > 320 || (!string.IsNullOrEmpty(message.Photo) && !Guid.TryParseExact(message.Photo, "N", out result)))
				{
					throw new InvalidDataException("Message invalide.");
				}
			}
			if (account.Messages.Select((PhoneSms m) => m.Id).Distinct().Count() != account.Messages.Count)
			{
				throw new InvalidDataException("Messages dupliqués.");
			}
			if (account.RecentCommands.Any((string s) => !Guid.TryParseExact(s, "N", out result2)))
			{
				throw new InvalidDataException("Commande sauvegardée invalide.");
			}
		}
	}

	private void Save()
	{
		Validate();
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		byte[] array = JsonSerializer.SerializeToUtf8Bytes(db);
		if (array.Length > 16777216)
		{
			throw new InvalidOperationException("Base téléphone pleine.");
		}
		string text = file + ".tmp";
		try
		{
			if (File.Exists(file))
			{
				File.Copy(file, file + ".bak", overwrite: true);
			}
			using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				fileStream.Write(array);
				fileStream.Flush(flushToDisk: true);
			}
			File.Move(text, file, overwrite: true);
		}
		finally
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	private void Commit(Action action)
	{
		byte[] array = JsonSerializer.SerializeToUtf8Bytes(db);
		try
		{
			action();
			Save();
			Index();
		}
		catch
		{
			db = JsonSerializer.Deserialize<PhoneDatabase>(array);
			Index();
			throw;
		}
	}

	public PhoneAccount Ensure(ulong id)
	{
		if (accounts.TryGetValue(id, out var value))
		{
			return value;
		}
		if (accounts.Count >= 2048)
		{
			throw new InvalidOperationException("Capacité des comptes téléphone atteinte.");
		}
		Commit(delegate
		{
			string number = RandomNumber(new HashSet<string>(numbers.Keys));
			db.NextNumber++;
			db.Accounts.Add(new PhoneAccount
			{
				Player = id,
				Number = number,
				PinDigits = 4
			});
		});
		return accounts[id];
	}

	public PhoneChange Apply(ulong id, PhoneCommand command, DateTime utc)
	{
		Ensure(id);
		if (command == null || !Guid.TryParseExact(command.Nonce, "N", out var _))
		{
			throw new InvalidOperationException("Identifiant de commande invalide.");
		}
		PhoneAccount account = accounts[id];
		if (account.RecentCommands.Contains(command.Nonce))
		{
			return new PhoneChange
			{
				Duplicate = true,
				Notice = "Action déjà enregistrée."
			};
		}
		PhoneChange change = new PhoneChange();
		string number = Normalize(command.Number);
		Commit(delegate
		{
			Guid result2;
			switch (command.Operation)
			{
			case "contact":
			{
				string text2 = (command.Name ?? "").Trim();
				if (text2.Length < 1 || text2.Length > 32 || text2.Any(char.IsControl))
				{
					throw new InvalidOperationException("Nom de contact : 1 à 32 caractères.");
				}
				if (number == account.Number)
				{
					throw new InvalidOperationException("C'est ton propre numéro.");
				}
				PhoneContact phoneContact2 = account.Contacts.FirstOrDefault((PhoneContact c) => c.Number == number);
				if (phoneContact2 == null)
				{
					if (account.Contacts.Count >= 100)
					{
						throw new InvalidOperationException("100 contacts maximum.");
					}
					account.Contacts.Add(new PhoneContact
					{
						Number = number,
						Name = text2
					});
				}
				else
				{
					phoneContact2.Name = text2;
				}
				change.Notice = "Contact enregistré.";
				break;
			}
			case "contactPhoto":
			{
				PhoneContact phoneContact = account.Contacts.FirstOrDefault((PhoneContact c) => c.Number == number);
				if (phoneContact == null)
				{
					throw new InvalidOperationException("Enregistrer le contact avant de choisir sa photo.");
				}
				string text = command.Photo ?? "";
				if (text != "" && (!Guid.TryParseExact(text, "N", out result2) || !account.Gallery.Contains(text)))
				{
					throw new InvalidOperationException("Choisir une photo de la Galerie de ce téléphone.");
				}
				phoneContact.Photo = text;
				change.Notice = ((text == "") ? "Photo du contact retirée." : "Photo du contact enregistrée.");
				break;
			}
			case "deleteContact":
				account.Contacts.RemoveAll((PhoneContact c) => c.Number == number);
				change.Notice = "Contact supprimé.";
				break;
			case "sms":
			{
				string text3 = (command.Text ?? "").Trim();
				if ((text3.Length < 1 && string.IsNullOrEmpty(command.Photo) && string.IsNullOrEmpty(command.Voice)) || text3.Length > 320 || text3.Any((char c) => char.IsControl(c) && c != '\n'))
				{
					throw new InvalidOperationException("SMS : 1 à 320 caractères.");
				}
				if (number == account.Number)
				{
					throw new InvalidOperationException("Impossible d'envoyer un SMS à soi-même.");
				}
				if (!numbers.TryGetValue(number, out var value))
				{
					throw new InvalidOperationException("Ce numéro n'est pas attribué sur ce serveur.");
				}
				if (!string.IsNullOrEmpty(command.Photo) && !Guid.TryParseExact(command.Photo, "N", out result2))
				{
					throw new InvalidOperationException("Pièce jointe invalide.");
				}
				if (!string.IsNullOrEmpty(command.Voice) && !string.IsNullOrEmpty(command.Photo))
				{
					throw new InvalidOperationException("Choisir une photo ou un vocal.");
				}
				if (!string.IsNullOrEmpty(command.Voice) && (!Guid.TryParseExact(command.Voice, "N", out result2) || command.VoiceSeconds < 1 || command.VoiceSeconds > 15))
				{
					throw new InvalidOperationException("Vocal invalide.");
				}
				string id2 = Guid.NewGuid().ToString("N");
				AddMessage(account, new PhoneSms
				{
					Id = id2,
					From = account.Number,
					To = number,
					Text = text3,
					Photo = (command.Photo ?? ""),
					Voice = (command.Voice ?? ""),
					VoiceSeconds = command.VoiceSeconds,
					Utc = utc.ToUniversalTime(),
					Read = true
				});
				AddMessage(value, new PhoneSms
				{
					Id = id2,
					From = account.Number,
					To = number,
					Text = text3,
					Photo = (command.Photo ?? ""),
					Voice = (command.Voice ?? ""),
					VoiceSeconds = command.VoiceSeconds,
					Utc = utc.ToUniversalTime()
				});
				change.Recipient = value.Player;
				change.Notice = "SMS enregistré et envoyé.";
				break;
			}
			case "read":
				foreach (PhoneSms item in account.Messages.Where((PhoneSms m) => m.To == account.Number && m.From == number))
				{
					item.Read = true;
				}
				change.Notice = "";
				break;
			default:
				throw new InvalidOperationException("Action téléphone inconnue.");
			}
			account.RecentCommands.Add(command.Nonce);
			if (account.RecentCommands.Count > 128)
			{
				account.RecentCommands.RemoveAt(0);
			}
		});
		return change;
	}

	private static void AddMessage(PhoneAccount account, PhoneSms message)
	{
		account.Messages.Add(message);
		while (account.Messages.Count > 100)
		{
			account.Messages.RemoveAt(0);
		}
	}

	public bool ReserveExternal(ulong actor, string nonce)
	{
		PhoneAccount account = Ensure(actor);
		if (!Guid.TryParseExact(nonce, "N", out var _))
		{
			throw new InvalidOperationException("Commande invalide.");
		}
		if (account.RecentCommands.Contains(nonce))
		{
			return false;
		}
		Commit(delegate
		{
			account.RecentCommands.Add(nonce);
			while (account.RecentCommands.Count > 128)
			{
				account.RecentCommands.RemoveAt(0);
			}
		});
		return true;
	}

	public void BankMovement(ulong from, ulong to, long amount, string nonce, DateTime utc)
	{
		PhoneAccount a = Ensure(from);
		PhoneAccount b = Ensure(to);
		if (a.Transactions.Any((PhoneBankEntry t) => t.Id == nonce))
		{
			return;
		}
		Commit(delegate
		{
			a.Transactions.Insert(0, new PhoneBankEntry
			{
				Id = nonce,
				Amount = -amount,
				Label = "Vers " + Display(b.Number),
				Utc = utc
			});
			b.Transactions.Insert(0, new PhoneBankEntry
			{
				Id = nonce,
				Amount = amount,
				Label = "De " + Display(a.Number),
				Utc = utc
			});
			while (a.Transactions.Count > 20)
			{
				a.Transactions.RemoveAt(20);
			}
			while (b.Transactions.Count > 20)
			{
				b.Transactions.RemoveAt(20);
			}
		});
	}

	public bool BankPinSet(ulong owner)
	{
		return Ensure(owner).BankPinHash != "";
	}

	public bool VerifyBankPin(ulong owner, string pin)
	{
		PhoneAccount phoneAccount = Ensure(owner);
		return PhonePin.Verify(pin, phoneAccount.BankPinSalt, phoneAccount.BankPinHash);
	}

	public void SetBankPin(ulong owner, string pin)
	{
		PhoneAccount a = Ensure(owner);
		string salt = PhonePin.Salt();
		string hash = PhonePin.Hash(pin, salt);
		Commit(delegate
		{
			a.BankPinSalt = salt;
			a.BankPinHash = hash;
		});
	}

	public bool PinSet(ulong owner)
	{
		return Ensure(owner).PinHash != "";
	}

	public bool VerifyPin(ulong owner, string pin)
	{
		PhoneAccount phoneAccount = Ensure(owner);
		return PhonePin.Verify(pin, phoneAccount.PinSalt, phoneAccount.PinHash);
	}

	public void SetPin(ulong owner, string pin)
	{
		PhoneAccount a = Ensure(owner);
		string salt = PhonePin.Salt();
		string hash = PhonePin.Hash(pin, salt);
		Commit(delegate
		{
			a.PinSalt = salt;
			a.PinHash = hash;
			a.PinDigits = 4;
		});
	}

	public List<string> DeletedPhotos(ulong owner)
	{
		return Ensure(owner).GalleryDeleted.ToList();
	}

	public List<string> Gallery(ulong owner)
	{
		return Ensure(owner).Gallery.ToList();
	}

	public void AddPhoto(ulong owner, string id)
	{
		if (!Guid.TryParseExact(id, "N", out var _))
		{
			throw new InvalidOperationException("Photo invalide.");
		}
		PhoneAccount a = Ensure(owner);
		if (!a.GalleryDeleted.Contains(id) && !a.Gallery.Contains(id))
		{
			if (a.Gallery.Count >= 100)
			{
				throw new InvalidOperationException("Galerie pleine (100 photos).");
			}
			Commit(delegate
			{
				a.Gallery.Insert(0, id);
			});
		}
	}

	public bool PhotoDeleted(ulong owner, string id)
	{
		return Ensure(owner).GalleryDeleted.Contains(id);
	}

	public void DeletePhoto(ulong owner, string id)
	{
		if (!Guid.TryParseExact(id, "N", out var _))
		{
			throw new InvalidOperationException("Photo invalide.");
		}
		PhoneAccount a = Ensure(owner);
		if (a.GalleryDeleted.Contains(id))
		{
			return;
		}
		Commit(delegate
		{
			a.Gallery.Remove(id);
			a.GalleryDeleted.Add(id);
			while (a.GalleryDeleted.Count > 200)
			{
				a.GalleryDeleted.RemoveAt(0);
			}
		});
	}

	public string Wallpaper(ulong owner)
	{
		return Ensure(owner).Wallpaper;
	}

	public bool IsWallpaper(ulong owner, string id)
	{
		return !string.IsNullOrEmpty(id) && Wallpaper(owner) == id;
	}

	public void SetWallpaper(ulong owner, string id)
	{
		id = id ?? "";
		PhoneAccount a = Ensure(owner);
		if (id != "" && (!Guid.TryParseExact(id, "N", out var _) || !a.Gallery.Contains(id)))
		{
			throw new InvalidOperationException("Choisir une photo de la Galerie de ce téléphone.");
		}
		Commit(delegate
		{
			a.Wallpaper = id;
		});
	}

	public bool CanReadVoice(ulong owner, string id)
	{
		return Ensure(owner).Messages.Any((PhoneSms m) => m.Voice == id);
	}

	public bool PhotoReferenced(string photo)
	{
		return db.Accounts.Any((PhoneAccount a) => a.Wallpaper == photo || a.Gallery.Contains(photo) || a.Contacts.Any((PhoneContact c) => c.Photo == photo) || a.Messages.Any((PhoneSms m) => m.Photo == photo));
	}

	public bool CanReadPhoto(ulong player, string photo)
	{
		return Ensure(player).Wallpaper == photo || Ensure(player).Gallery.Contains(photo) || Ensure(player).Contacts.Any((PhoneContact c) => c.Photo == photo) || Ensure(player).Messages.Any((PhoneSms m) => m.Photo == photo);
	}

	public ulong? FindPlayer(string input)
	{
		string key = Normalize(input);
		PhoneAccount value;
		return numbers.TryGetValue(key, out value) ? new ulong?(value.Player) : ((ulong?)null);
	}

	public void RecordCall(ulong caller, ulong callee, PhoneCallResult result)
	{
		PhoneAccount a = Ensure(caller);
		PhoneAccount b = Ensure(callee);
		if (a.Calls.Any((PhoneCallEntry c) => c.Id == result.Id) || b.Calls.Any((PhoneCallEntry c) => c.Id == result.Id))
		{
			return;
		}
		Commit(delegate
		{
			a.Calls.Insert(0, new PhoneCallEntry
			{
				Id = result.Id,
				Number = b.Number,
				Outcome = result.Outcome,
				Incoming = false,
				Masked = result.Masked,
				Seconds = result.Seconds,
				Utc = result.Utc
			});
			b.Calls.Insert(0, new PhoneCallEntry
			{
				Id = result.Id,
				Number = (result.Masked ? "" : a.Number),
				Outcome = result.Outcome,
				Incoming = true,
				Masked = result.Masked,
				Seconds = result.Seconds,
				Utc = result.Utc
			});
			while (a.Calls.Count > 50)
			{
				a.Calls.RemoveAt(50);
			}
			while (b.Calls.Count > 50)
			{
				b.Calls.RemoveAt(50);
			}
		});
	}

	public PhoneView PrivateView(ulong id)
	{
		PhoneAccount phoneAccount = Ensure(id);
		return new PhoneView
		{
			Player = id,
			Number = phoneAccount.Number,
			Calls = phoneAccount.Calls.Select((PhoneCallEntry c) => new PhoneCallEntry
			{
				Id = c.Id,
				Number = c.Number,
				Outcome = c.Outcome,
				Incoming = c.Incoming,
				Masked = c.Masked,
				Seconds = c.Seconds,
				Utc = c.Utc
			}).ToList(),
			Transactions = phoneAccount.Transactions.Select((PhoneBankEntry t) => new PhoneBankEntry
			{
				Id = t.Id,
				Amount = t.Amount,
				Label = t.Label,
				Utc = t.Utc
			}).ToList(),
			Contacts = phoneAccount.Contacts.Select((PhoneContact c) => new PhoneContact
			{
				Number = c.Number,
				Name = c.Name,
				Photo = c.Photo
			}).ToList(),
			Messages = phoneAccount.Messages.Select((PhoneSms m) => new PhoneSms
			{
				Id = m.Id,
				From = m.From,
				To = m.To,
				Text = m.Text,
				Photo = m.Photo,
				Voice = m.Voice,
				VoiceSeconds = m.VoiceSeconds,
				Utc = m.Utc,
				Read = m.Read
			}).ToList()
		};
	}
}
