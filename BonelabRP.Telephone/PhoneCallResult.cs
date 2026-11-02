using System;

namespace BonelabRP.Telephone;

public sealed class PhoneCallResult
{
	public string Id;

	public string Outcome;

	public ulong Caller;

	public ulong Callee;

	public bool Masked;

	public bool Answered;

	public int Seconds;

	public DateTime Utc;
}
