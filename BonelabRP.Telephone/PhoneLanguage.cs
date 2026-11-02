using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BonelabRP.Telephone;

public static class PhoneLanguage
{
	private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		[" contacts   •   Carnet personnel"] = " contacts   •   Personal address book",
		[" i/s"] = " fps",
		[" images/s"] = " fps",
		[" notification(s)"] = " notification(s)",
		[" photos   •   Photos de ce téléphone"] = " photos   •   This phone's photos",
		[" spectateurs   •   "] = " viewers   •   ",
		["+ Ajouter"] = "+ Add",
		["0 contact"] = "0 contacts",
		["APPEL TÉLÉPHONIQUE"] = "PHONE CALL",
		["APPEL VIDÉO"] = "VIDEO CALL",
		["Accueil"] = "Home",
		["Actualiser"] = "Refresh",
		["Aimé"] = "Liked",
		["Ajouter une photo"] = "Add a photo",
		["Annuler"] = "Cancel",
		["Aperçu photo : "] = "Photo preview: ",
		["Appel en cours…"] = "Calling…",
		["Appel entrant"] = "Incoming call",
		["Appel terminé"] = "Call ended",
		["Appeler en visio"] = "Video call",
		["Appeler"] = "Call",
		["Appels : "] = "Calls: ",
		["Appuyer sur B de la main porteuse"] = "Press B on the hand holding the phone",
		["Appuyer sur Y de la main porteuse"] = "Press Y on the hand holding the phone",
		["Attendre le chargement de la photo."] = "Wait for the photo to load.",
		["Aucun contact trouvé"] = "No contacts found",
		["Aucun live en cours"] = "No live streams right now",
		["Aucun message"] = "No messages",
		["Aucun virement enregistré"] = "No transfers yet",
		["Avec caméra"] = "With camera",
		["Banque"] = "Bank",
		["BÉNÉFICIAIRE"] = "RECIPIENT",
		["COMPTE COURANT"] = "CURRENT ACCOUNT",
		["Cadence cible : le PC et le réseau déterminent la fluidité réelle."] = "Target frame rate: actual smoothness depends on your PC and network.",
		["Caméra arrière"] = "Rear camera",
		["Caméra avant"] = "Front camera",
		["Caméra du correspondant coupée"] = "The other person's camera is off",
		["Caméra"] = "Camera",
		["Changer la photo jointe"] = "Change attachment",
		["Changer la photo"] = "Change photo",
		["Changer le code"] = "Change PIN",
		["Chargement…"] = "Loading…",
		["Choisir le destinataire"] = "Choose recipient",
		["Choisir un numéro"] = "Choose a number",
		["Choisir un pseudo"] = "Choose a username",
		["Commencer le live"] = "Start live stream",
		["Commenter"] = "Comment",
		["Confirme le nouveau code"] = "Confirm the new PIN",
		["Confirme ton code"] = "Confirm your PIN",
		["Confirmer la suppression"] = "Confirm deletion",
		["Confirmer le virement"] = "Confirm transfer",
		["Confirmer"] = "Confirm",
		["Connexion au direct…"] = "Connecting to live stream…",
		["Connexion vidéo…"] = "Connecting video…",
		["Continuer"] = "Continue",
		["Correspondant sur haut-parleur"] = "The other person is on speakerphone",
		["Crée ton code à quatre chiffres"] = "Create your four-digit PIN",
		["Créer mon compte"] = "Create account",
		["DERNIÈRES OPÉRATIONS"] = "RECENT TRANSACTIONS",
		["Effacer"] = "Delete",
		["Enregistrer"] = "Save",
		["Envoi de la photo…"] = "Sending photo…",
		["Envoi…"] = "Sending…",
		["Envoyer cette photo"] = "Send this photo",
		["Envoyer le commentaire"] = "Send comment",
		["Envoyer le message"] = "Send message",
		["Espace"] = "Space",
		["Faire un virement"] = "Make a transfer",
		["Fluidité caméra / visio"] = "Camera / video frame rate",
		["Galerie"] = "Gallery",
		["Haut-parleur"] = "Speaker",
		["Historique des virements"] = "Transfer history",
		["Indisponible"] = "Unavailable",
		["Joindre une photo"] = "Attach a photo",
		["J’aime"] = "Like",
		["La caméra et le micro sont diffusés sur Windy"] = "Your camera and microphone are broadcast on Windy",
		["Le code est réservé au propriétaire."] = "Only the owner can change the PIN.",
		["Le fil est calme.\nPublie le premier message !"] = "The feed is quiet.\nWrite the first post!",
		["Les codes ne correspondent pas."] = "The PINs do not match.",
		["Lives • "] = "Live streams • ",
		["MONTANT"] = "AMOUNT",
		["Maj"] = "Shift",
		["Mes comptes   /   Banque personnelle"] = "My accounts   /   Personal banking",
		["Messages : "] = "Messages: ",
		["Micro coupé"] = "Muted",
		["Micro"] = "Microphone",
		["Modifier le contact"] = "Edit contact",
		["Modifier"] = "Edit",
		["NOM"] = "NAME",
		["NUMÉRO"] = "NUMBER",
		["Nom et numéro de téléphone"] = "Name and phone number",
		["Nouveau code à quatre chiffres"] = "New four-digit PIN",
		["Nouveau contact"] = "New contact",
		["Nouveau message"] = "New message",
		["Nouveau"] = "New",
		["Nouvelle publication"] = "New post",
		["Numéro : "] = "Number: ",
		["Opérations"] = "Transactions",
		["Partager mon numéro à 3 m"] = "Share my number within 3 m",
		["Passer en live"] = "Go live",
		["Photo du contact"] = "Contact photo",
		["Photo en cours de synchronisation. Réessayer dans quelques secondes."] = "Photo is syncing. Try again in a few seconds.",
		["Plus ancien"] = "Older",
		["Plus récent"] = "Newer",
		["Pour vous   /   @"] = "For you   /   @",
		["Préparation de la caméra…"] = "Preparing camera…",
		["Publier"] = "Post",
		["Quitter"] = "Leave",
		["Quoi de neuf ?"] = "What's happening?",
		["Raccrocher"] = "End call",
		["Recherche : "] = "Search: ",
		["Rechercher"] = "Search",
		["Refuser"] = "Decline",
		["Regarde, commente et partage le moment"] = "Watch, comment and share the moment",
		["Rejoins la conversation"] = "Join the conversation",
		["Retirer ce contact de ton carnet ?\nLes conversations resteront disponibles."] = "Remove this contact?\nYour conversations will remain available.",
		["Retirer la photo"] = "Remove photo",
		["Retirer"] = "Remove",
		["Retour au contact"] = "Back to contact",
		["Retour au fil"] = "Back to feed",
		["Retour aux comptes"] = "Back to accounts",
		["Retour aux contacts"] = "Back to contacts",
		["Retour"] = "Back",
		["Reçu"] = "Received",
		["Réglages"] = "Settings",
		["Répondre en audio"] = "Answer with audio",
		["Répondre"] = "Answer",
		["Saisir le nom"] = "Enter name",
		["Saisir le numéro"] = "Enter number",
		["Saisir un montant entier positif."] = "Enter a positive whole amount.",
		["Saisir un montant"] = "Enter amount",
		["Saisis le code du téléphone"] = "Enter the phone PIN",
		["Saisis les quatre chiffres du code."] = "Enter the four-digit PIN.",
		["Solde disponible : "] = "Available balance: ",
		["Supprimer cette photo de Galerie ?\nLes copies déjà envoyées restent chez leurs destinataires."] = "Delete this photo from Gallery?\nCopies already sent stay with their recipients.",
		["Supprimer de Galerie"] = "Delete from Gallery",
		["Supprimer le contact"] = "Delete contact",
		["Supprimer"] = "Delete",
		["Ta Galerie est vide.\nPrends ta première photo dans Photo."] = "Your Gallery is empty.\nTake your first photo in Camera.",
		["Terminer"] = "End",
		["Terminé"] = "Done",
		["Tiens le téléphone pour rester en direct."] = "Hold the phone to stay live.",
		["Ton carnet est vide.\nAjoute ton premier contact."] = "Your address book is empty.\nAdd your first contact.",
		["Ton live"] = "Your live stream",
		["Tout l’historique"] = "Full history",
		["Tu pourras choisir une photo après\nl’enregistrement du contact."] = "You can choose a photo after\nsaving the contact.",
		["Téléphone verrouillé\nLe propriétaire doit créer son code."] = "Phone locked\nThe owner must create a PIN.",
		["Téléphone"] = "Phone",
		["Un transfert entre comptes LabRP"] = "A transfer between LabRP accounts",
		["Valider"] = "Confirm",
		["Verrouiller"] = "Lock",
		["Vers "] = "To ",
		["Virement"] = "Transfer",
		["Visio : "] = "Video call: ",
		["Visio"] = "Video",
		["Voir la photo"] = "View photo",
		["Volume des sonneries : "] = "Ringtone volume: ",
		["Vous"] = "You",
		["Vérifie le bénéficiaire et le montant"] = "Check the recipient and amount",
		["Écrire / modifier"] = "Write / edit",
		["Écrire le texte"] = "Write text",
		["Écrire un message ou joindre une photo."] = "Write a message or attach a photo.",
		["Écris ton message…"] = "Write your message…",
		["Classique"] = "Classic",
		["Cristal"] = "Crystal",
		["Carillon"] = "Chime",
		["Silencieux"] = "Silent",
		["Langue"] = "Language",
		["Français"] = "French",
		["Anglais"] = "English",
		["Contact enregistré."] = "Contact saved.",
		["Contact supprimé."] = "Contact deleted.",
		["Photo du contact retirée."] = "Contact photo removed.",
		["Photo du contact enregistrée."] = "Contact photo saved.",
		["Nouveau SMS."] = "New message.",
		["Message envoyé."] = "Message sent.",
		["Action déjà enregistrée."] = "Action already saved.",
		["Action trop rapide, réessayer."] = "Too fast. Please try again.",
		["Session actualisée, réessayer."] = "Session updated. Please try again.",
		["Numéro invalide."] = "Invalid number.",
		["Saisir un numéro RP de 10 chiffres."] = "Enter a 10-digit RP phone number.",
		["C'est ton propre numéro."] = "This is your own number.",
		["Impossible d'envoyer un SMS à soi-même."] = "You cannot message yourself.",
		["Ce numéro n'est pas attribué."] = "This number is not assigned.",
		["Ce correspondant est hors ligne."] = "This person is offline.",
		["Appel connecté."] = "Call connected.",
		["Appel refusé."] = "Call declined.",
		["Téléphone absent."] = "Phone not available.",
		["Choisir une photo de la Galerie de ce téléphone."] = "Choose a photo from this phone's Gallery.",
		["Enregistrer le contact avant de choisir sa photo."] = "Save the contact before choosing a photo.",
		["Photo invalide."] = "Invalid photo.",
		["Déverrouiller le téléphone."] = "Unlock the phone.",
		["Une photo est déjà en cours d'envoi."] = "A photo is already being sent.",
		["Photo illisible."] = "Photo could not be read.",
		["Photo reçue invalide."] = "Invalid received photo.",
		["Photo non disponible pour cet expéditeur."] = "Photo not available for this sender.",
		["Photo de contact indisponible pour ce téléphone."] = "Contact photo not available for this phone.",
		["Galerie pleine (100 photos). Supprimer une photo."] = "Gallery full (100 photos). Delete a photo.",
		["Prise de photo…"] = "Taking photo…",
		["Tenir le téléphone et attendre l'écran."] = "Hold the phone and wait for the screen.",
		["Code incorrect."] = "Incorrect PIN.",
		["Le code doit contenir quatre chiffres."] = "The PIN must contain four digits.",
		["Nom de contact : 1 à 32 caractères."] = "Contact name: 1 to 32 characters.",
		["SMS : 1 à 320 caractères."] = "Message: 1 to 320 characters.",
		["Quitter le live avant l’appel."] = "Leave the live stream before calling.",
		["Haut-parleur activé : conversation audible à 2 m."] = "Speakerphone on: conversation audible within 2 m.",
		["Haut-parleur désactivé."] = "Speakerphone off.",
		["Caméra de visio activée."] = "Video camera on.",
		["Caméra de visio coupée."] = "Video camera off.",
		["Photo"] = "Camera",
		["SMS enregistré et envoyé."] = "Message saved and sent.",
		["Transfert photo expiré, réessayer."] = "Photo transfer timed out. Try again.",
		["Photo prise • enregistrement dans la Galerie…"] = "Photo taken • saving to Gallery…",
		["Photo conservée localement ; synchronisation au prochain déverrouillage."] = "Photo saved locally; it will sync when you next unlock.",
		["Tenir le téléphone pour prendre la photo."] = "Hold the phone to take a photo.",
		["Compte Windy prêt."] = "Windy account ready.",
		["Publication Windy envoyée."] = "Windy post sent.",
		["Commande déjà traitée."] = "Command already processed.",
		["Aucun joueur compatible à moins de 3 m."] = "No compatible player within 3 m.",
		["Virement refusé par LabRP. Attendre puis réessayer."] = "Transfer declined by LabRP. Wait and try again.",
		["Téléphone reverrouillé : appel terminé."] = "Phone locked again: call ended.",
		["Appel terminé : changement de map."] = "Call ended: map changed.",
		["Correspondant déconnecté."] = "The other person disconnected.",
		["Appel terminé : aucune réponse ou liaison interrompue."] = "Call ended: no answer or connection lost.",
		["Voix Fusion désactivée : appel terminé."] = "Fusion voice disabled: call ended.",
		["Téléphone déverrouillé."] = "Phone unlocked.",
		["Téléphone verrouillé."] = "Phone locked.",
		["Code enregistré."] = "PIN saved.",
		["Clavier"] = "Keypad",
		["Récents"] = "Recents",
		["Historique des appels"] = "Call history",
		["Ajoute # avant le numéro pour appeler en masqué"] = "Add # before the number to hide caller ID",
		["Appel masqué"] = "Private call",
		["Masqué"] = "Private",
		["Numéro masqué"] = "Private number",
		["Impossible de rappeler un numéro masqué."] = "You cannot call back a private number.",
		["Manqué"] = "Missed",
		["Entrant"] = "Incoming",
		["Sortant"] = "Outgoing",
		["Refusé"] = "Declined",
		["Annulé"] = "Cancelled",
		["Aucun appel récent"] = "No recent calls",
		["Apparence"] = "Appearance",
		["Thème"] = "Theme",
		["Clair"] = "Light",
		["Sombre"] = "Dark",
		["Fond d’écran"] = "Wallpaper",
		["Choisir dans la Galerie"] = "Choose from Gallery",
		["Même image sur le verrouillage et l’accueil."] = "Same image on the lock screen and home screen.",
		["Retirer le fond d’écran"] = "Remove wallpaper",
		["Fond d’écran indisponible."] = "Wallpaper unavailable.",
		["Tes conversations"] = "Your conversations",
		["Message"] = "Message",
		["Conversation"] = "Conversation",
		["Message vocal"] = "Voice message",
		["Vocal"] = "Voice note",
		["Lu"] = "Read",
		["Envoyé"] = "Sent",
		["Commence la conversation"] = "Start the conversation",
		["Écrire"] = "Write",
		["Enregistrement…"] = "Recording…",
		["Vocal prêt"] = "Voice note ready",
		["Enregistre ta voix"] = "Record your voice",
		["Arrêter"] = "Stop",
		["Réenregistrer"] = "Record again",
		["Démarrage du micro…"] = "Starting microphone…",
		["Micro actif"] = "Microphone active",
		["Fusion n’a pas ouvert le micro. Vérifier le périphérique sélectionné."] = "Fusion has not opened the microphone. Check the selected device.",
		["Le micro Fusion n’est pas en cours de capture."] = "The Fusion microphone is not recording.",
		["Le curseur du micro reste immobile. Vérifier le périphérique Fusion."] = "The microphone cursor is not moving. Check the Fusion input device.",
		["Chargement du vocal…"] = "Loading voice message…",
		["Téléchargement du vocal expiré. Réessayer."] = "Voice message download timed out. Try again.",
		["vient de lancer un live"] = "just went live",
		["a publié sur Windy"] = "posted on Windy",
		["Niveau micro"] = "Microphone level",
		["Aucune voix capturée. Vérifier le micro Fusion puis réenregistrer."] = "No voice captured. Check the Fusion microphone and record again.",
		["Activer le micro Fusion avant d’enregistrer."] = "Enable the Fusion microphone before recording.",
		["Lecture du micro impossible."] = "Unable to read the microphone.",
		["Écouter"] = "Listen",
		["Réécouter"] = "Listen again",
		["Envoyer"] = "Send",
		["Micro Fusion • 15 secondes maximum"] = "Fusion microphone • 15 seconds maximum",
		["Partager ton numéro à proximité"] = "Share your number nearby",
		["Aucun joueur à moins de 3 m"] = "No player within 3 m",
		["Numéros reçus"] = "Received numbers",
		["Aucun numéro reçu"] = "No numbers received",
		["Partager à tous • 3 m"] = "Share with all • 3 m",
		["Activer le micro Fusion et déverrouiller le téléphone."] = "Enable the Fusion microphone and unlock the phone.",
		["Quitter l’appel ou le live avant d’enregistrer."] = "Leave the call or live stream before recording.",
		["Enregistrer un vocal avant de l’envoyer."] = "Record a voice note before sending.",
		["Vocal : 0,5 à 15 secondes."] = "Voice note: 0.5 to 15 seconds.",
		["Vocal invalide."] = "Invalid voice note.",
		["Vocal indisponible."] = "Voice note unavailable.",
		["Vocal indisponible pour cet expéditeur."] = "Voice note not available for this sender.",
		["Envoi du vocal expiré."] = "Voice note upload timed out.",
		["Stockage des vocaux plein."] = "Voice note storage full.",
		["Accès à ce vocal refusé."] = "Access to this voice note denied.",
		["Attendre le chargement du vocal."] = "Wait for the voice note to load.",
		["Choisir une photo ou un vocal."] = "Choose a photo or a voice note."
	};

	public static bool Valid(string language)
	{
		return language == "fr" || language == "en";
	}

	public static string Text(string language, string text)
	{
		string value;
		return (language == "en" && text != null && English.TryGetValue(text, out value)) ? value : (text ?? "");
	}

	public static string Status(string language, string text)
	{
		if (language != "en")
		{
			return text ?? "";
		}
		string text2 = Text(language, text);
		if (text2 != text)
		{
			return text2;
		}
		if (text == null)
		{
			return "";
		}
		Match match = Regex.Match(text, "^Numéro partagé avec ([0-9]+) joueur\\(s\\)\\.$");
		if (match.Success)
		{
			return "Number shared with " + match.Groups[1].Value + " player(s).";
		}
		Match match2 = Regex.Match(text, "^Virement effectué : ([0-9]+) \\$\\.$");
		if (match2.Success)
		{
			return "Transfer completed: " + match2.Groups[1].Value + " $.";
		}
		Match match3 = Regex.Match(text, "^Virement reçu : ([0-9]+) \\$ de ([0-9 ]+)$");
		if (match3.Success)
		{
			return "Transfer received: " + match3.Groups[1].Value + " $ from " + match3.Groups[2].Value;
		}
		Match match4 = Regex.Match(text, "^(De|Vers) ([0-9 ]+)$");
		if (match4.Success)
		{
			return ((match4.Groups[1].Value == "De") ? "From " : "To ") + match4.Groups[2].Value;
		}
		return text;
	}

	public static string Date(string language, DateTime date)
	{
		if (language == "en")
		{
			return date.ToString("dddd d MMMM", CultureInfo.InvariantCulture);
		}
		string[] array = new string[7] { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };
		string[] array2 = new string[12]
		{
			"janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre",
			"novembre", "décembre"
		};
		return array[(int)date.DayOfWeek] + " " + date.Day.ToString(CultureInfo.InvariantCulture) + " " + array2[date.Month - 1];
	}
}
