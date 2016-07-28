#if !XB1
using SharpDX;
using SharpDX.XAudio2;
using System;
using System.Runtime.InteropServices;
using VRage.Native;
using System.Diagnostics;

namespace VRage.Audio
{
    /// <summary>
    /// Native wrapper for source voice.
    /// It's not ref counted, no need to call Release or Dispose.
    /// </summary>
    public struct NativeSourceVoice
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BufferWma
        {
            /// <include file=".\..\Documentation\CodeComments.xml" path="/comments/comment[@id='XAUDIO2_BUFFER_WMA::pDecodedPacketCumulativeBytes']/*"/><unmanaged>const unsigned int* pDecodedPacketCumulativeBytes</unmanaged><unmanaged-short>unsigned int pDecodedPacketCumulativeBytes</unmanaged-short>
            public IntPtr DecodedPacketCumulativeBytesPointer;

            /// <include file=".\..\Documentation\CodeComments.xml" path="/comments/comment[@id='XAUDIO2_BUFFER_WMA::PacketCount']/*"/><unmanaged>unsigned int PacketCount</unmanaged><unmanaged-short>unsigned int PacketCount</unmanaged-short>
            public int PacketCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct NativeAudioBuffer
        {
            public BufferFlags Flags;
            public int AudioBytes;
            public IntPtr AudioDataPointer;
            public int PlayBegin;
            public int PlayLength;
            public int LoopBegin;
            public int LoopLength;
            public int LoopCount;
            public IntPtr Context;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct VoiceSendDescriptors
        {
            public int SendCount;
            internal IntPtr SendPointer;
        }

        private static void Check(Result result)
        {
            result.CheckError();
        }

        public readonly IntPtr Pointer;

        public NativeSourceVoice(IntPtr sourceVoicePtr)
        {
            Pointer = sourceVoicePtr;
        }

        #region IXAudio2SourceVoice interface

        // Summary:
        //     Returns the frequency adjustment ratio of the voice.
        //
        // Remarks:
        //     GetFrequencyRatio always returns the voice's actual current frequency ratio.
        //     However, this may not match the ratio set by the most recent SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32)
        //     call: the actual ratio is only changed the next time the audio engine runs
        //     after the SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32)
        //     was called with a deferred operation ID). For information on frequency ratios,
        //     see SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32).
        public float FrequencyRatio
        {
            get
            {
                unsafe
                {
                    float result = 0;
#if UNSHARPER
                    Debug.Assert(false);
#else
                    NativeCall.Method(Pointer, 27, new IntPtr(&result));
#endif
                    return result;
                }
            }
        }

        //
        // Summary:
        //     Reconfigures the voice to consume source data at a different sample rate
        //     than the rate specified when the voice was created.
        //
        // Remarks:
        //     The SetSourceSampleRate method supports reuse of XAudio2 voices by allowing
        //     a voice to play sounds with a variety of sample rates. To use SetSourceSampleRate
        //     the voice must have been created without the SharpDX.XAudio2.VoiceFlags.NoPitch
        //     or SharpDX.XAudio2.VoiceFlags.NoSampleRateConversion flags and must not have
        //     any buffers currently queued. The typical use of SetSourceSampleRate is to
        //     support voice pooling. For example to support voice pooling an application
        //     would precreate all the voices it expects to use. Whenever a new sound will
        //     be played the application chooses an inactive voice or ,if all voices are
        //     busy, picks the least important voice and calls SetSourceSampleRate on the
        //     voice with the new sound's sample rate. After SetSourceSampleRate has been
        //     called on the voice, the application can immediately start submitting and
        //     playing buffers with the new sample rate. This allows the application to
        //     avoid the overhead of creating and destroying voices frequently during gameplay.
        public int SourceSampleRate
        {
            set
            {
#if UNSHARPER
                Debug.Assert(false);
#else
                ((Result)NativeCall<int>.Method(Pointer, 28, (uint)value)).CheckError();
#endif
            }
        }

        //
        // Summary:
        //     Returns the voice's current cursor position data.
        //
        // Remarks:
        //     If a client needs to obtain the correlated positions of several voices (i.e.
        //     to know exactly which sample of a given voice is playing when a given sample
        //     of another voice is playing) it must make GetState calls in an XAudio2 engine
        //     callback, to ensure that none of the voices advance while the calls are being
        //     made. See the XAudio2 Callbacks overview for information about using XAudio2
        //     callbacks.
        public VoiceState State
        {
            get
            {
                unsafe
                {
                    VoiceState result = new VoiceState();
#if UNSHARPER
                    Debug.Assert(false);
#else
                    NativeCall.Method(Pointer, 25, new IntPtr(&result));
#endif
                    return result;
                }
            }
        }

        // Summary:
        //     Notifies an XAudio2 voice that no more buffers are coming after the last
        //     one that is currently in its queue.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise.
        //
        // Remarks:
        //     Discontinuity suppresses the warnings that normally occur in the debug build
        //     of XAudio2 when a voice runs out of audio buffers to play. It is preferable
        //     to mark the final buffer of a stream by tagging it with the SharpDX.XAudio2.BufferFlags.EndOfStream
        //     flag, but in some cases the client may not know that a buffer is the end
        //     of a stream until after the buffer has been submitted. Because calling Discontinuity
        //     is equivalent to applying the SharpDX.XAudio2.BufferFlags.EndOfStream flag
        //     retroactively to the last buffer submitted, an OnStreamEnd callback will
        //     be made when this buffer completes. Note XAudio2 may consume its entire buffer
        //     queue and emit a warning before the Discontinuity call takes effect, so Discontinuity
        //     is not guaranteed to suppress the warnings.
        public void Discontinuity()
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            ((Result)NativeCall<int>.Method(Pointer, 23)).CheckError();
#endif
        }

        //
        // Summary:
        //     Stops looping the voice when it reaches the end of the current loop region.
        //
        // Parameters:
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of XAudio2 specific error codes.
        //
        // Remarks:
        //     If the cursor for the voice is not in a loop region, ExitLoop does nothing.
        public void ExitLoop(int operationSet = 0)
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            ((Result)NativeCall<int>.Method(Pointer, 24, (uint)operationSet)).CheckError();
#endif
        }

        //
        // Summary:
        //     Removes all pending audio buffers from the voice queue.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise.
        //
        // Remarks:
        //     If the voice is started, the buffer that is currently playing is not removed
        //     from the queue. FlushSourceBuffers can be called regardless of whether the
        //     voice is currently started or stopped. For every buffer removed, an OnBufferEnd
        //     callback will be made, but none of the other per-buffer callbacks (OnBufferStart,
        //     OnStreamEnd or OnLoopEnd) will be made. FlushSourceBuffers does not change
        //     a the voice's running state, so if the voice was playing a buffer prior to
        //     the call, it will continue to do so, and will deliver all the callbacks for
        //     the buffer normally. This means that the OnBufferEnd callback for this buffer
        //     will take place after the OnBufferEnd callbacks for the buffers that were
        //     removed. Thus, an XAudio2 client that calls FlushSourceBuffers cannot expect
        //     to receive OnBufferEnd callbacks in the order in which the buffers were submitted.
        //     No warnings for starvation of the buffer queue will be emitted when the currently
        //     playing buffer completes; it is assumed that the client has intentionally
        //     removed the buffers that followed it. However, there may be an audio pop
        //     if this buffer does not end at a zero crossing. If the application must ensure
        //     that the flush operation takes place while a specific buffer is playing?perhaps
        //     because the buffer ends with a zero crossing?it must call FlushSourceBuffers
        //     from a callback, so that it executes synchronously. Calling FlushSourceBuffers
        //     after a voice is stopped and then submitting new data to the voice resets
        //     all of the voice's internal counters. A voice's state is not considered reset
        //     after calling FlushSourceBuffers until the OnBufferEnd callback occurs (if
        //     a buffer was previously submitted) or SharpDX.XAudio2.SourceVoice.GetState(SharpDX.XAudio2.VoiceState@)
        //     returns with SharpDX.XAudio2.VoiceState.BuffersQueued == 0. For example,
        //     if you stop a voice and call FlushSourceBuffers, it's still not legal to
        //     immediately call SharpDX.XAudio2.SourceVoice.SetSourceSampleRate(System.Int32)
        //     (which requires the voice to not have any buffers currently queued), until
        //     either of the previously mentioned conditions are met.
        public void FlushSourceBuffers()
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            ((Result)NativeCall<int>.Method(Pointer, 22)).CheckError();
#endif
        }

        //
        // Summary:
        //     Sets the frequency adjustment ratio of the voice.
        //
        // Parameters:
        //   ratio:
        //     [in] Frequency adjustment ratio. This value must be between SharpDX.XAudio2.XAudio2.MinimumFrequencyRatio
        //     and the MaxFrequencyRatio parameter specified when the voice was created
        //     (see SharpDX.XAudio2.XAudio2.CreateSourceVoice_(SharpDX.XAudio2.SourceVoice,System.IntPtr,SharpDX.XAudio2.VoiceFlags,System.Single,System.IntPtr,System.Nullable<SharpDX.XAudio2.VoiceSendDescriptors>,System.Nullable<SharpDX.XAudio2.EffectChain>)).
        //     SharpDX.XAudio2.XAudio2.MinimumFrequencyRatio currently is 0.0005, which
        //     allows pitch to be lowered by up to 11 octaves.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of error codes.
        //
        // Remarks:
        //     Frequency adjustment is expressed as source frequency / target frequency.
        //     Changing the frequency ratio changes the rate audio is played on the voice.
        //     A ratio greater than 1.0 will cause the audio to play faster and a ratio
        //     less than 1.0 will cause the audio to play slower. Additionally, the frequency
        //     ratio affects the pitch of audio on the voice. As an example, a value of
        //     1.0 has no effect on the audio, whereas a value of 2.0 raises pitch by one
        //     octave and 0.5 lowers it by one octave. If SetFrequencyRatio is called specifying
        //     a Ratio value outside the valid range, the method will set the frequency
        //     ratio to the nearest valid value. A warning also will be generated for debug
        //     builds. Note SharpDX.XAudio2.SourceVoice.GetFrequencyRatio(System.Single@)
        //     always returns the voice's actual current frequency ratio. However, this
        //     may not match the ratio set by the most recent SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32)
        //     call: the actual ratio is only changed the next time the audio engine runs
        //     after the SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32)
        //     was called with a deferred operation ID).
        public void SetFrequencyRatio(float ratio, int operationSet = 0)
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            ((Result)NativeCall<int>.Method(Pointer, 26, ratio, (uint)operationSet)).CheckError();
#endif
        }

        //
        // Summary:
        //     Starts consumption and processing of audio by the voice. Delivers the result
        //     to any connected submix or mastering voices, or to the output device.
        //
        // Parameters:
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the {{XAudio2
        //     Operation Sets}} overview for more information.
        //
        // Returns:
        //     No documentation.
        public void Start(int operationSet = 0)
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            ((Result)NativeCall<int>.Method(Pointer, 19, (uint)0, (uint)operationSet)).CheckError();
#endif
        }

        //
        // Summary:
        //     Stops consumption of audio by the current voice.
        //
        // Parameters:
        //   flags:
        //     [in] Flags that control how the voice is stopped. Can be 0 or the following:
        //     ValueDescriptionSharpDX.XAudio2.PlayFlags.TailsContinue emitting effect output
        //     after the voice is stopped.?
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of XAudio2 specific error codes.
        //
        // Remarks:
        //     All source buffers that are queued on the voice and the current cursor position
        //     are preserved. This allows the voice to continue from where it left off,
        //     when it is restarted. The SharpDX.XAudio2.SourceVoice.FlushSourceBuffers()
        //     method can be used to flush queued source buffers. By default, any pending
        //     output from voice effects?for example, reverb tails?is not played. Instead,
        //     the voice is immediately rendered silent. The SharpDX.XAudio2.PlayFlags.Tails
        //     flag can be used to continue emitting effect output after the voice stops
        //     running. A voice stopped with the SharpDX.XAudio2.PlayFlags.Tails flag stops
        //     consuming source buffers, but continues to process its effects and send audio
        //     to its destination voices. A voice in this state can later be stopped completely
        //     by calling Stop again with the Flags argument set to 0. This enables stopping
        //     a voice with SharpDX.XAudio2.PlayFlags.Tails, waiting sufficient time for
        //     any audio being produced by its effects to finish, and then fully stopping
        //     the voice by calling Stop again without SharpDX.XAudio2.PlayFlags.Tails.
        //     This technique allows voices with effects to be stopped gracefully while
        //     ensuring idle voices will not continue to be processed after they have finished
        //     producing audio. Stop is always asynchronous, even if called within a callback.
        //     Note XAudio2 never calls any voice callbacks for a voice if the voice is
        //     stopped (even if it was stopped with SharpDX.XAudio2.PlayFlags.Tails).
        public void Stop(PlayFlags flags, int operationSet = 0)
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            ((Result)NativeCall<int>.Method(Pointer, 20, (uint)flags, (uint)operationSet)).CheckError();
#endif
        }

        //
        // Summary:
        //     No documentation.
        //
        // Parameters:
        //   bufferRef:
        //     No documentation.
        //
        //   decodedXMWAPacketInfo:
        //     No documentation.
        //
        // Returns:
        //     No documentation.
        public void SubmitSourceBuffer(AudioBuffer bufferRef, uint[] decodedXMWAPacketInfo)
        {
            if (decodedXMWAPacketInfo != null)
            {
                unsafe
                {
                    fixed (uint* numPtr = decodedXMWAPacketInfo)
                    {
                        BufferWma bufferWma;
                        bufferWma.PacketCount = decodedXMWAPacketInfo.Length;
                        bufferWma.DecodedPacketCumulativeBytesPointer = (IntPtr)((void*)numPtr);
                        this.SubmitSourceBuffer(bufferRef, new IntPtr((void*)&bufferWma));
                    }
                }
            }
            else
            {
                SubmitSourceBuffer(bufferRef, IntPtr.Zero);
            }
        }

        private void SubmitSourceBuffer(AudioBuffer bufferRef, IntPtr decodedXMWAPacketInfo)
        {
            unsafe
            {
                NativeAudioBuffer buf = new NativeAudioBuffer();
                buf.Flags = bufferRef.Flags;
                buf.AudioBytes = bufferRef.AudioBytes;
                buf.AudioDataPointer = bufferRef.AudioDataPointer;
                buf.PlayBegin = bufferRef.PlayBegin;
                buf.PlayLength = bufferRef.PlayLength;
                buf.LoopBegin = bufferRef.LoopBegin;
                buf.LoopLength = bufferRef.LoopLength;
                buf.LoopCount = bufferRef.LoopCount;
                buf.Context = bufferRef.Context;

#if UNSHARPER
                Debug.Assert(false);
#else
                ((Result)NativeCall<int>.Method<IntPtr, IntPtr>(Pointer, 21, new IntPtr(&buf), decodedXMWAPacketInfo)).CheckError();
#endif
            }
        }

        #endregion

        #region IXAudio2Voice interface


        // Summary:
        //     Gets the voice's filter parameters.
        //
        // Remarks:
        //     GetFilterParameters will fail if the voice was not created with the SharpDX.XAudio2.VoiceSendFlags.UseFilter
        //     flag. GetFilterParameters always returns this voice's actual current filter
        //     parameters. However, these may not match the parameters set by the most recent
        //     SharpDX.XAudio2.Voice.SetFilterParameters(SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call: the actual parameters are only changed the next time the audio engine
        //     runs after the SharpDX.XAudio2.Voice.SetFilterParameters(SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetFilterParameters(SharpDX.XAudio2.FilterParameters,System.Int32)
        //     was called with a deferred operation ID). Note GetFilterParameters is usable
        //     only on source and submix voices and has no effect on mastering voices.
        public FilterParameters FilterParameters { get { throw new NotImplementedException("Implement when needed"); } }

        //
        // Summary:
        //     Returns information about the creation flags, input channels, and sample
        //     rate of a voice.
        public VoiceDetails VoiceDetails
        {
            get { throw new NotImplementedException("Implement when needed"); }
        }

        //
        // Summary:
        //     Gets the current overall volume level of the voice.
        //
        // Remarks:
        //     Volume levels are expressed as floating-point amplitude multipliers between
        //     -224 to 224, with a maximum gain of 144.5 dB. A volume level of 1 means there
        //     is no attenuation or gain and 0 means silence. Negative levels can be used
        //     to invert the audio's phase. See XAudio2 Volume and Pitch Control for additional
        //     information on volume control. Note GetVolume always returns the volume most
        //     recently set by SharpDX.XAudio2.Voice.SetVolume(System.Single,System.Int32).
        //     However, it may not actually be in effect yet: it only takes effect the next
        //     time the audio engine runs after the SharpDX.XAudio2.Voice.SetVolume(System.Single,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetVolume(System.Single,System.Int32) was
        //     called with a deferred operation ID).
        public float Volume
        {
            get
            {
                throw new NotImplementedException("Implement when needed");
            }
        }

        // Summary:
        //     Destroys the voice. If necessary, stops the voice and removes it from the
        //     XAudio2 graph.
        //
        // Remarks:
        //     If any other voice is currently sending audio to this voice, the method fails.
        //     DestroyVoice waits for the audio processing thread to be idle, so it can
        //     take a little while (typically no more than a couple of milliseconds). This
        //     is necessary to guarantee that the voice will no longer make any callbacks
        //     or read any audio data, so the application can safely free up these resources
        //     as soon as the call returns. To avoid title thread interruptions from a blocking
        //     DestroyVoice call, the application can destroy voices on a separate non-critical
        //     thread, or the application can use voice pooling strategies to reuse voices
        //     rather than destroying them. Note that voices can only be reused with audio
        //     that has the same data format and the same number of channels the voice was
        //     created with. A voice can play audio data with different sample rates than
        //     that of the voice by calling SharpDX.XAudio2.SourceVoice.SetFrequencyRatio(System.Single,System.Int32)
        //     with an appropriate ratio parameter. It is illegal to call DestroyVoice from
        //     within a callback. If DestroyVoice is called within a callback XAUDIO2_E_INVALID_CALL
        //     will be returned.
        public void DestroyVoice()
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            NativeCall.Method(Pointer, 18);
#endif
        }

        //
        // Summary:
        //     Disables the effect at a given position in the effect chain of the voice.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect in the effect chain of the voice.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful; otherwise, an error code. See XAudio2
        //     Error Codes for descriptions of valid error codes.
        //
        // Remarks:
        //     The effects in a given XAudio2 voice's effect chain must consume and produce
        //     audio at that voice's processing sample rate. The only aspect of the audio
        //     format they can change is the channel count. For example a reverb effect
        //     can convert mono data to 5.1. The client can use the SharpDX.XAudio2.EffectDescriptor
        //     structure's OutputChannels field to specify the number of channels it wants
        //     each effect to produce. Each effect in an effect chain must produce a number
        //     of channels that the next effect can consume. Any calls to SharpDX.XAudio2.Voice.EnableEffect(System.Int32,System.Int32)
        //     or SharpDX.XAudio2.Voice.DisableEffect(System.Int32,System.Int32) that would
        //     make the effect chain stop fulfilling these requirements will fail. Disabling
        //     an effect immediately removes it from the processing graph. Any pending audio
        //     in the effect?such as a reverb tail?is not played. Be careful disabling an
        //     effect while the voice that hosts it is running. This can result in an audible
        //     artifact if the effect significantly changes the audio's pitch or volume.
        //     DisableEffect takes effect immediately when called from an XAudio2 callback
        //     with an OperationSet of SharpDX.XAudio2.XAudio2.CommitNow.
        public void DisableEffect(int effectIndex, int operationSet = 0)
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            Check(NativeCall<int>.Method(Pointer, 4, (uint)effectIndex, (uint)operationSet));
#endif
        }

        //
        // Summary:
        //     Enables the effect at a given position in the effect chain of the voice.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect in the effect chain of the voice.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful; otherwise, an error code. See XAudio2
        //     Error Codes for descriptions of error codes.
        //
        // Remarks:
        //     Be careful when you enable an effect while the voice that hosts it is running.
        //     Such an action can result in a problem if the effect significantly changes
        //     the audio's pitch or volume. The effects in a given XAudio2 voice's effect
        //     chain must consume and produce audio at that voice's processing sample rate.
        //     The only aspect of the audio format they can change is the channel count.
        //     For example a reverb effect can convert mono data to 5.1. The client can
        //     use the SharpDX.XAudio2.EffectDescriptor structure's OutputChannels field
        //     to specify the number of channels it wants each effect to produce. Each effect
        //     in an effect chain must produce a number of channels that the next effect
        //     can consume. Any calls to SharpDX.XAudio2.Voice.EnableEffect(System.Int32,System.Int32)
        //     or SharpDX.XAudio2.Voice.DisableEffect(System.Int32,System.Int32) that would
        //     make the effect chain stop fulfilling these requirements will fail. EnableEffect
        //     takes effect immediately when you call it from an XAudio2 callback with an
        //     OperationSet of SharpDX.XAudio2.XAudio2.CommitNow.
        public void EnableEffect(int effectIndex, int operationSet = 0)
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            Check(NativeCall<int>.Method(Pointer, 3, (uint)effectIndex, (uint)operationSet));
#endif
        }

        //
        // Summary:
        //     Returns the volume levels for the voice, per channel.
        //
        // Parameters:
        //   channels:
        //     [in] Confirms the channel count of the voice.
        //
        //   volumesRef:
        //     [out] Returns the current volume level of each channel in the voice. The
        //     array must have at least Channels elements. See Remarks for more information
        //     on volume levels.
        //
        // Remarks:
        //     These settings are applied after the effect chain is applied. This method
        //     is valid only for source and submix voices, because mastering voices do not
        //     specify volume per channel. Volume levels are expressed as floating-point
        //     amplitude multipliers between -224 to 224, with a maximum gain of 144.5 dB.
        //     A volume of 1 means there is no attenuation or gain, 0 means silence, and
        //     negative levels can be used to invert the audio's phase. See XAudio2 Volume
        //     and Pitch Control for additional information on volume control. Note GetChannelVolumes
        //     always returns the volume levels most recently set by SharpDX.XAudio2.Voice.SetChannelVolumes(System.Int32,System.Single[],System.Int32).
        //     However, those values may not actually be in effect yet: they only take effect
        //     the next time the audio engine runs after the SharpDX.XAudio2.Voice.SetChannelVolumes(System.Int32,System.Single[],System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetChannelVolumes(System.Int32,System.Single[],System.Int32)
        //     was called with a deferred operation ID).
        public void GetChannelVolumes(int channels, float[] volumesRef)
        {
            unsafe
            {
                fixed (float* numPtr = volumesRef)
                {
#if UNSHARPER
                    Debug.Assert(false);
#else
                    NativeCall.Method(Pointer, 15, (uint)channels, new IntPtr(numPtr));
#endif
                }
            }
        }

        //
        // Summary:
        //     Sets parameters for a given effect in the voice's effect chain.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect within the voice's effect chain.
        //
        // Returns:
        //     Returns the current values of the effect-specific parameters.
        public T GetEffectParameters<T>(int effectIndex) where T : struct
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Returns the current effect-specific parameters of a given effect in the voice's
        //     effect chain.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect within the voice's effect chain.
        //
        //   effectParameters:
        //     [out] Returns the current values of the effect-specific parameters.
        //
        // Returns:
        //     No documentation.
        public void GetEffectParameters(int effectIndex, byte[] effectParameters)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Returns the filter parameters from one of this voice's sends.
        //
        // Parameters:
        //   destinationVoiceRef:
        //     [in] SharpDX.XAudio2.Voice reference to the destination voice of the send
        //     whose filter parameters will be read.
        //
        //   parametersRef:
        //     [out] Pointer to an SharpDX.XAudio2.FilterParameters structure containing
        //     the filter information.
        //
        // Remarks:
        //     GetOutputFilterParameters will fail if the send was not created with the
        //     XAUDIO2_SEND_USEFILTER flag. This method is usable only on sends belonging
        //     to source and submix voices and has no effect on mastering voices? sends.
        //     Note SharpDX.XAudio2.Voice.GetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters@)
        //     always returns this send?s actual current filter parameters. However, these
        //     may not match the parameters set by the most recent SharpDX.XAudio2.Voice.SetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call: the actual parameters are only changed the next time the audio engine
        //     runs after the SharpDX.XAudio2.Voice.SetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters,System.Int32)
        //     was called with a deferred operation ID).
        public void GetOutputFilterParameters(Voice destinationVoiceRef, out FilterParameters parametersRef)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Gets the volume level of each channel of the final output for the voice.
        //     These channels are mapped to the input channels of a specified destination
        //     voice.
        //
        // Parameters:
        //   destinationVoiceRef:
        //     [in] Pointer specifying the destination SharpDX.XAudio2.Voice to retrieve
        //     the output matrix for. Note If the voice sends to a single target voice then
        //     specifying null will cause GetOutputMatrix to operate on that target voice.
        //
        //   sourceChannels:
        //     [in] Confirms the output channel count of the voice. This is the number of
        //     channels that are produced by the last effect in the chain.
        //
        //   destinationChannels:
        //     [in] Confirms the input channel count of the destination voice.
        //
        //   levelMatrixRef:
        //     [out] Array of [SourceChannels * DestinationChannels] volume levels sent
        //     to the destination voice. The level sent from source channel S to destination
        //     channel D is returned in the form pLevelMatrix[DestinationChannels ? S +
        //     D]. See Remarks for more information on volume levels.
        //
        // Remarks:
        //     This method applies only to source and submix voices, because mastering voices
        //     write directly to the device with no matrix mixing. Volume levels are expressed
        //     as floating-point amplitude multipliers between -224 to 224, with a maximum
        //     gain of 144.5 dB. A volume level of 1 means there is no attenuation or gain
        //     and 0 means silence. Negative levels can be used to invert the audio's phase.
        //     See XAudio2 Volume and Pitch Control for additional information on volume
        //     control. See SharpDX.Multimedia.WaveFormatExtensible for information on standard
        //     channel ordering. Note GetOutputMatrix always returns the levels most recently
        //     set by SharpDX.XAudio2.Voice.SetOutputMatrix(SharpDX.XAudio2.Voice,System.Int32,System.Int32,System.Single[],System.Int32).
        //     However, they may not actually be in effect yet: they only take effect the
        //     next time the audio engine runs after the SharpDX.XAudio2.Voice.SetOutputMatrix(SharpDX.XAudio2.Voice,System.Int32,System.Int32,System.Single[],System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetOutputMatrix(SharpDX.XAudio2.Voice,System.Int32,System.Int32,System.Single[],System.Int32)
        //     was called with a deferred operation ID).
        public void GetOutputMatrix(Voice destinationVoiceRef, int sourceChannels, int destinationChannels, float[] levelMatrixRef)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Returns the running state of the effect at a specified position in the effect
        //     chain of the voice.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect in the effect chain of the voice.
        //
        // Remarks:
        //     GetEffectState always returns the effect's actual current state. However,
        //     this may not be the state set by the most recent SharpDX.XAudio2.Voice.EnableEffect(System.Int32,System.Int32)
        //     or SharpDX.XAudio2.Voice.DisableEffect(System.Int32,System.Int32) call: the
        //     actual state is only changed the next time the audio engine runs after the
        //     SharpDX.XAudio2.Voice.EnableEffect(System.Int32,System.Int32) or SharpDX.XAudio2.Voice.DisableEffect(System.Int32,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if EnableEffect/DisableEffect was called with a deferred operation
        //     ID).
        public bool IsEffectEnabled(int effectIndex)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets the volume levels for the voice, per channel.
        //
        // Parameters:
        //   channels:
        //     [in] Number of channels in the voice.
        //
        //   volumesRef:
        //     [in] Array containing the new volumes of each channel in the voice. The array
        //     must have Channels elements. See Remarks for more information on volume levels.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of XAudio2 specific error codes.
        //
        // Remarks:
        //     SetChannelVolumes controls a voice's per-channel output levels and is applied
        //     just after the voice's final SRC and before its sends. This method is valid
        //     only for source and submix voices, because mastering voices do not specify
        //     volume per channel. Volume levels are expressed as floating-point amplitude
        //     multipliers between -SharpDX.XAudio2.XAudio2.MaximumVolumeLevel and SharpDX.XAudio2.XAudio2.MaximumVolumeLevel
        //     (-224 to 224), with a maximum gain of 144.5 dB. A volume of 1 means there
        //     is no attenuation or gain and 0 means silence. Negative levels can be used
        //     to invert the audio's phase. See XAudio2 Volume and Pitch Control for additional
        //     information on volume control. Note SharpDX.XAudio2.Voice.GetChannelVolumes(System.Int32,System.Single[])
        //     always returns the volume levels most recently set by SharpDX.XAudio2.Voice.SetChannelVolumes(System.Int32,System.Single[],System.Int32).
        //     However, those values may not actually be in effect yet: they only take effect
        //     the next time the audio engine runs after the SharpDX.XAudio2.Voice.SetChannelVolumes(System.Int32,System.Single[],System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetChannelVolumes(System.Int32,System.Single[],System.Int32)
        //     was called with a deferred operation ID).
        public void SetChannelVolumes(int channels, float[] volumesRef, int operationSet = 0)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Replaces the effect chain of the voice.
        //
        // Parameters:
        //   effectDescriptors:
        //     [in, optional] an array of SharpDX.XAudio2.EffectDescriptor structure that
        //     describes the new effect chain to use. If NULL is passed, the current effect
        //     chain is removed. If array is non null, its length must be at least of 1.
        //
        // Returns:
        //     No documentation.
        public void SetEffectChain(params EffectDescriptor[] effectDescriptors)
        {
            // NEED
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets parameters for a given effect in the voice's effect chain.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect within the voice's effect chain.
        //
        //   effectParameter:
        //     [in] Returns the current values of the effect-specific parameters.
        //
        // Returns:
        //     No documentation.
        public void SetEffectParameters(int effectIndex, byte[] effectParameter)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets parameters for a given effect in the voice's effect chain.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect within the voice's effect chain.
        //
        //   effectParameter:
        //     [in] Returns the current values of the effect-specific parameters.
        //
        // Returns:
        //     No documentation.
        public void SetEffectParameters<T>(int effectIndex, T effectParameter) where T : struct
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets parameters for a given effect in the voice's effect chain.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect within the voice's effect chain.
        //
        //   effectParameter:
        //     [in] Returns the current values of the effect-specific parameters.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the {{XAudio2
        //     Operation Sets}} overview for more information.
        //
        // Returns:
        //     No documentation.
        public void SetEffectParameters(int effectIndex, byte[] effectParameter, int operationSet)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets parameters for a given effect in the voice's effect chain.
        //
        // Parameters:
        //   effectIndex:
        //     [in] Zero-based index of an effect within the voice's effect chain.
        //
        //   effectParameter:
        //     [in] Returns the current values of the effect-specific parameters.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the {{XAudio2
        //     Operation Sets}} overview for more information.
        //
        // Returns:
        //     No documentation.
        public void SetEffectParameters<T>(int effectIndex, T effectParameter, int operationSet) where T : struct
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets the voice's filter parameters.
        //
        // Parameters:
        //   parametersRef:
        //     [in] Pointer to an SharpDX.XAudio2.FilterParameters structure containing
        //     the filter information.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of error codes.
        //
        // Remarks:
        //     SetFilterParameters will fail if the voice was not created with the SharpDX.XAudio2.VoiceSendFlags.UseFilter
        //     flag. This method is usable only on source and submix voices and has no effect
        //     on mastering voices. Note SharpDX.XAudio2.Voice.GetFilterParameters(SharpDX.XAudio2.FilterParameters@)
        //     always returns this voice's actual current filter parameters. However, these
        //     may not match the parameters set by the most recent SharpDX.XAudio2.Voice.SetFilterParameters(SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call: the actual parameters are only changed the next time the audio engine
        //     runs after the SharpDX.XAudio2.Voice.SetFilterParameters(SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetFilterParameters(SharpDX.XAudio2.FilterParameters,System.Int32)
        //     was called with a deferred operation ID).
        public void SetFilterParameters(FilterParameters parametersRef, int operationSet = 0)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets the filter parameters on one of this voice's sends.
        //
        // Parameters:
        //   destinationVoiceRef:
        //     [in] SharpDX.XAudio2.Voice reference to the destination voice of the send
        //     whose filter parameters will be set.
        //
        //   parametersRef:
        //     [in] Pointer to an SharpDX.XAudio2.FilterParameters structure containing
        //     the filter information.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of error codes.
        //
        // Remarks:
        //     SetOutputFilterParameters will fail if the send was not created with the
        //     XAUDIO2_SEND_USEFILTER flag. This method is usable only on sends belonging
        //     to source and submix voices and has no effect on a mastering voice's sends.
        //     Note SharpDX.XAudio2.Voice.GetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters@)
        //     always returns this send?s actual current filter parameters. However, these
        //     may not match the parameters set by the most recent SharpDX.XAudio2.Voice.SetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call: the actual parameters are only changed the next time the audio engine
        //     runs after the SharpDX.XAudio2.Voice.SetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetOutputFilterParameters(SharpDX.XAudio2.Voice,SharpDX.XAudio2.FilterParameters,System.Int32)
        //     was called with a deferred operation ID).
        public void SetOutputFilterParameters(Voice destinationVoiceRef, FilterParameters parametersRef, int operationSet = 0)
        {
            throw new NotImplementedException("Implement when needed");
        }

        //
        // Summary:
        //     Sets the volume level of each channel of the final output for the voice.
        //     These channels are mapped to the input channels of a specified destination
        //     voice.
        //
        // Parameters:
        //   sourceChannels:
        //     [in] Confirms the output channel count of the voice. This is the number of
        //     channels that are produced by the last effect in the chain.
        //
        //   destinationChannels:
        //     [in] Confirms the input channel count of the destination voice.
        //
        //   levelMatrixRef:
        //     [in] Array of [SourceChannels ? DestinationChannels] volume levels sent to
        //     the destination voice. The level sent from source channel S to destination
        //     channel D is specified in the form pLevelMatrix[SourceChannels ? D + S].
        //     For example, when rendering two-channel stereo input into 5.1 output that
        //     is weighted toward the front channels?but is absent from the center and low-frequency
        //     channels?the matrix might have the values shown in the following table. OutputLeft
        //     InputRight Input Left1.00.0 Right0.01.0 Front Center0.00.0 LFE0.00.0 Rear
        //     Left0.80.0 Rear Right0.00.8 Note that the left and right input are fully
        //     mapped to the output left and right channels; 80 percent of the left and
        //     right input is mapped to the rear left and right channels. See Remarks for
        //     more information on volume levels.
        //
        // Returns:
        //     No documentation.
        public void SetOutputMatrix(int sourceChannels, int destinationChannels, float[] levelMatrixRef, int operationSet = 0)
        {
            SetOutputMatrix(null, sourceChannels, destinationChannels, levelMatrixRef, operationSet);
        }

        //
        // Summary:
        //     Sets the volume level of each channel of the final output for the voice.
        //     These channels are mapped to the input channels of a specified destination
        //     voice.
        //
        // Parameters:
        //   destinationVoiceRef:
        //     [in] Pointer to a destination SharpDX.XAudio2.Voice for which to set volume
        //     levels. Note If the voice sends to a single target voice then specifying
        //     null will cause SetOutputMatrix to operate on that target voice.
        //
        //   sourceChannels:
        //     [in] Confirms the output channel count of the voice. This is the number of
        //     channels that are produced by the last effect in the chain.
        //
        //   destinationChannels:
        //     [in] Confirms the input channel count of the destination voice.
        //
        //   levelMatrixRef:
        //     [in] Array of [SourceChannels ? DestinationChannels] volume levels sent to
        //     the destination voice. The level sent from source channel S to destination
        //     channel D is specified in the form pLevelMatrix[SourceChannels ? D + S].
        //     For example, when rendering two-channel stereo input into 5.1 output that
        //     is weighted toward the front channels?but is absent from the center and low-frequency
        //     channels?the matrix might have the values shown in the following table. OutputLeft
        //     Input [Array Index]Right Input [Array Index] Left1.0 [0]0.0 [1] Right0.0
        //     [2]1.0 [3] Front Center0.0 [4]0.0 [5] LFE0.0 [6]0.0 [7] Rear Left0.8 [8]0.0
        //     [9] Rear Right0.0 [10]0.8 [11] Note that the left and right input are fully
        //     mapped to the output left and right channels; 80 percent of the left and
        //     right input is mapped to the rear left and right channels. See Remarks for
        //     more information on volume levels.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of error codes.
        //
        // Remarks:
        //     This method is valid only for source and submix voices, because mastering
        //     voices write directly to the device with no matrix mixing. Volume levels
        //     are expressed as floating-point amplitude multipliers between -SharpDX.XAudio2.XAudio2.MaximumVolumeLevel
        //     and SharpDX.XAudio2.XAudio2.MaximumVolumeLevel (-224 to 224), with a maximum
        //     gain of 144.5 dB. A volume level of 1.0 means there is no attenuation or
        //     gain and 0 means silence. Negative levels can be used to invert the audio's
        //     phase. See XAudio2 Volume and Pitch Control for additional information on
        //     volume control. The X3DAudio function SharpDX.X3DAudio.X3DAudio.X3DAudioCalculate(SharpDX.X3DAudio.X3DAudioHandle@,SharpDX.X3DAudio.Listener,SharpDX.X3DAudio.Emitter,SharpDX.X3DAudio.CalculateFlags,System.IntPtr)
        //     can produce an output matrix for use with SetOutputMatrix based on a sound's
        //     position and a listener's position. Note SharpDX.XAudio2.Voice.GetOutputMatrix(SharpDX.XAudio2.Voice,System.Int32,System.Int32,System.Single[])
        //     always returns the levels most recently set by SharpDX.XAudio2.Voice.SetOutputMatrix(SharpDX.XAudio2.Voice,System.Int32,System.Int32,System.Single[],System.Int32).
        //     However, they may not actually be in effect yet: they only take effect the
        //     next time the audio engine runs after the SharpDX.XAudio2.Voice.SetOutputMatrix(SharpDX.XAudio2.Voice,System.Int32,System.Int32,System.Single[],System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetOutputMatrix(SharpDX.XAudio2.Voice,System.Int32,System.Int32,System.Single[],System.Int32)
        //     was called with a deferred operation ID).
        public void SetOutputMatrix(Voice destinationVoiceRef, int sourceChannels, int destinationChannels, float[] levelMatrixRef, int operationSet = 0)
        {
            IntPtr destPtr = destinationVoiceRef != null ? destinationVoiceRef.NativePointer : IntPtr.Zero;

            unsafe
            {
                fixed (float* coefPtr = &levelMatrixRef[0])
                {
#if UNSHARPER
                    Debug.Assert(false);
#else
                    Check(NativeCall<int>.Method<IntPtr, uint, uint, IntPtr, uint>(Pointer, 16, destPtr, (uint)sourceChannels, (uint)destinationChannels, new IntPtr(coefPtr), (uint)operationSet));
#endif
                }
            }
        }

        //
        // Summary:
        //     Designates a new set of submix or mastering voices to receive the output
        //     of the voice.
        //
        // Parameters:
        //   outputVoices:
        //     [in] Array of SharpDX.XAudio2.VoiceSendDescriptor structure pointers to destination
        //     voices. If outputVoices is NULL, the voice will send its output to the current
        //     mastering voice. To set the voice to not send its output anywhere set an
        //     array of lenvth 0. All of the voices in a send list must have the same input
        //     sample rate, see {{XAudio2 Sample Rate Conversions}} for additional information.
        //
        // Returns:
        //     No documentation.
        public void SetOutputVoices(VoiceSendDescriptor[] outputVoices)
        {
            if (outputVoices != null)
            {
                unsafe
                {
                    fixed (VoiceSendDescriptor* voiceSendDescriptorPtr = &outputVoices[0])
                    {
                        VoiceSendDescriptors descriptors = new VoiceSendDescriptors();
                        descriptors.SendCount = outputVoices.Length;
                        descriptors.SendPointer = (IntPtr)((void*)voiceSendDescriptorPtr);
#if UNSHARPER
                        Debug.Assert(false);
#else
                        Check(NativeCall<int>.Method(Pointer, 1, new IntPtr(&descriptors)));
#endif
                    }
                }
            }
            else
            {
#if UNSHARPER
                Debug.Assert(false);
#else
                Check(NativeCall<int>.Method(Pointer, 1, IntPtr.Zero));
#endif
            }
        }

        //
        // Summary:
        //     Sets the overall volume level for the voice.
        //
        // Parameters:
        //   volume:
        //     [in] Overall volume level to use. See Remarks for more information on volume
        //     levels.
        //
        //   operationSet:
        //     [in] Identifies this call as part of a deferred batch. See the XAudio2 Operation
        //     Sets overview for more information.
        //
        // Returns:
        //     Returns SharpDX.Result.Ok if successful, an error code otherwise. See XAudio2
        //     Error Codes for descriptions of error codes.
        //
        // Remarks:
        //     SetVolume controls a voice's master input volume level. The master volume
        //     level is applied at different times depending on the type of voice. For submix
        //     and mastering voices the volume level is applied just before the voice's
        //     built in filter and effect chain is applied. For source voices the master
        //     volume level is applied after the voice's filter and effect chain is applied.
        //     Volume levels are expressed as floating-point amplitude multipliers between
        //     -SharpDX.XAudio2.XAudio2.MaximumVolumeLevel and SharpDX.XAudio2.XAudio2.MaximumVolumeLevel
        //     (-224 to 224), with a maximum gain of 144.5 dB. A volume level of 1.0 means
        //     there is no attenuation or gain and 0 means silence. Negative levels can
        //     be used to invert the audio's phase. See XAudio2 Volume and Pitch Control
        //     for additional information on volume control. Note SharpDX.XAudio2.Voice.GetVolume(System.Single@)
        //     always returns the volume most recently set by SharpDX.XAudio2.Voice.SetVolume(System.Single,System.Int32).
        //     However, it may not actually be in effect yet: it only takes effect the next
        //     time the audio engine runs after the SharpDX.XAudio2.Voice.SetVolume(System.Single,System.Int32)
        //     call (or after the corresponding SharpDX.XAudio2.XAudio2.CommitChanges(System.Int32)
        //     call, if SharpDX.XAudio2.Voice.SetVolume(System.Single,System.Int32) was
        //     called with a deferred operation ID).
        public void SetVolume(float volume, int operationSet = 0)
        {
#if UNSHARPER
            Debug.Assert(false);
#else
            Check(NativeCall<int>.Method(Pointer, 12, volume, (uint)operationSet));
#endif
        }

        #endregion
    }
}
#endif // !XB1
