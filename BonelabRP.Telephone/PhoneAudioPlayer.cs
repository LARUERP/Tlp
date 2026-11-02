using System;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LabFusion.Audio;
using LabFusion.Utilities;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneAudioPlayer : IDisposable
{
	private readonly PhonePcmBuffer buffer;

	private readonly PhoneVoiceFilter filter;

	private readonly AudioClip clip;

	private readonly PhoneVoiceContinuity continuity = new PhoneVoiceContinuity();

	private float lastVolume;

	private readonly Action<Il2CppStructArray<float>> reader;

	private readonly PCMReaderCallback callback;

	private float[] block = Array.Empty<float>();

	private bool disposed;

	public GameObject gameObject { get; }

	public Transform transform => gameObject.transform;

	public AudioSource AudioSource { get; }

	public PhoneAudioPlayer()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		int outputSampleRate = AudioInfo.OutputSampleRate;
		filter = new PhoneVoiceFilter(outputSampleRate, 18000.0);
		buffer = new PhonePcmBuffer(outputSampleRate);
		gameObject = new GameObject("RP_BufferedPhoneAudio");
		AudioSource = gameObject.AddComponent<AudioSource>();
		AudioSource.playOnAwake = false;
		AudioSource.loop = true;
		AudioSource.dopplerLevel = 0f;
		AudioSource.bypassReverbZones = true;
		AudioSource.reverbZoneMix = 0f;
		reader = Fill;
		callback = PCMReaderCallback.op_Implicit(reader);
		clip = AudioClip.Create("RP_PhoneStreamingVoice", outputSampleRate, 1, outputSampleRate, true, callback);
		AudioSource.clip = clip;
		AudioSource.Play();
	}

	private void Fill(Il2CppStructArray<float> data)
	{
		if (block.Length != ((Il2CppArrayBase<float>)(object)data).Length)
		{
			block = new float[((Il2CppArrayBase<float>)(object)data).Length];
		}
		buffer.Read(block);
		IntPtr pointer = ((Il2CppObjectBase)data).Pointer;
		for (int i = 0; i < block.Length; i++)
		{
			InteropUtilities.FloatArrayFastWrite(pointer, IntPtr.Size, i, block[i]);
		}
	}

	public void Push(short[] pcm, float volume)
	{
		if (pcm != null)
		{
			lastVolume = volume;
			pcm = continuity.Prepare(pcm);
			short[] array = new short[pcm.Length];
			for (int i = 0; i < pcm.Length; i++)
			{
				array[i] = (short)Math.Clamp(PhoneVoiceContinuity.Limit(filter.Process((float)pcm[i] / 32768f)) * 32767f, -32767f, 32767f);
			}
			buffer.Push(array, volume);
		}
	}

	public void Conceal(int frames)
	{
		short[] array = continuity.Fill(Math.Clamp(frames, 0, 20) * AudioInfo.OutputSampleRate / 100);
		short[] array2 = new short[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = (short)(PhoneVoiceContinuity.Limit(filter.Process((float)array[i] / 32768f)) * 32767f);
		}
		buffer.Push(array2, lastVolume);
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			buffer.Dispose();
			AudioSource.Stop();
			Object.Destroy((Object)(object)gameObject);
			Object.Destroy((Object)(object)clip);
		}
	}
}
