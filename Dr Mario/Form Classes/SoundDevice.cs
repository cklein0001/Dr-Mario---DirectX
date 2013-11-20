using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.DirectSound;
using SlimDX;
using SlimDX.Multimedia;
using System.IO;
using System.Threading;

namespace Dr_Mario.Form_Classes
{

    public class SoundManager : IDisposable
    {
        private static SoundManager staticDevice;

        public static void Initialize(SlimDX.Windows.RenderForm parent)
        {
            staticDevice = new SoundManager(parent, CooperativeLevel.Priority);
            staticDevice.SetPrimaryBuffer(2, 22050, 16);
           
        }

        public static void Cleanup() { try { staticDevice.Dispose(); } catch { } }

        public static void Play(string path)
        {
            try
            {
                staticDevice.PlaySound(path);
            }
            catch
            {
                Console.WriteLine("Sound output error.");
            }
        }

        private DirectSound device;

        public DirectSound Device
        {
            get { return device; }
        }

        public SoundManager(SlimDX.Windows.RenderForm parent, CooperativeLevel level)
        {
            device = new DirectSound();
            device.SetCooperativeLevel(parent.Handle, level);
        }

        public void SetPrimaryBuffer(short channels, short frequency, short bitRate)
        {
            SoundBufferDescription desc = new SoundBufferDescription();
            desc.Flags = BufferFlags.PrimaryBuffer;

            using (PrimarySoundBuffer primary = new PrimarySoundBuffer(device, desc))
            {
                WaveFormatExtensible format = new WaveFormatExtensible();
                format.FormatTag = WaveFormatTag.Pcm;
                format.Channels = channels;
                format.SamplesPerSecond = frequency;
                format.BitsPerSample = bitRate;
                format.BlockAlignment = (short)(bitRate / 8 * channels);
                format.AverageBytesPerSecond = frequency * format.BlockAlignment;

                primary.Format = format;
            }
        }

        public SoundListener3D Create3DListener()
        {
            SoundListener3D listener = null;
            SoundBufferDescription description = new SoundBufferDescription();
            description.Flags = BufferFlags.PrimaryBuffer | BufferFlags.Control3D;

            using (PrimarySoundBuffer buffer = new PrimarySoundBuffer(device, description))
            {
                listener = new SoundListener3D(buffer);
            }

            return listener;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (device != null)
                device.Dispose();

            device = null;
        }

        #endregion


        internal void PlaySound(string audioFile)
        {
            if (this.Device == null)
                throw new NullReferenceException("Sound Device not initialized.");

            using (WaveStream file = new WaveStream(audioFile))
            {
                SoundBufferDescription description = new SoundBufferDescription();
                description.Format = file.Format;
                int fileLength = Convert.ToInt32(file.Length);
                description.SizeInBytes = fileLength;

                SecondarySoundBuffer applicationBuffer = new SecondarySoundBuffer(this.Device, description);

                byte[] data = new byte[fileLength];
                file.Read(data, 0, fileLength);

                applicationBuffer.Write(data, 0, LockFlags.None);
                applicationBuffer.Play(0, PlayFlags.None);
                //effectsManager.Initialize(applicationBuffer);
            }
            return;
            WaveFormat format = new WaveFormat();
            format.BitsPerSample = 16;
            format.BlockAlignment = 4;
            format.Channels = 2;
            format.FormatTag = WaveFormatTag.MpegLayer3;
            format.SamplesPerSecond = 44100;
            format.AverageBytesPerSecond = format.SamplesPerSecond * format.BlockAlignment;

            SoundBufferDescription desc = new SoundBufferDescription();
            desc.Format = format;
            desc.Flags = BufferFlags.GlobalFocus;
            desc.SizeInBytes = 8 * format.AverageBytesPerSecond;

            SoundBufferDescription desc2 = new SoundBufferDescription();
            desc2.Format = format;
            desc2.Flags = BufferFlags.GlobalFocus | BufferFlags.ControlPositionNotify | BufferFlags.GetCurrentPosition2;
            desc2.SizeInBytes = 8 * format.AverageBytesPerSecond;

            SecondarySoundBuffer sBuffer1 = new SecondarySoundBuffer(this.Device, desc2);

            NotificationPosition[] notifications = new NotificationPosition[2];
            notifications[0].Offset = desc2.SizeInBytes / 2 + 1;
            notifications[1].Offset = desc2.SizeInBytes - 1; ;

            notifications[0].Event = new AutoResetEvent(false);
            notifications[1].Event = new AutoResetEvent(false);
            sBuffer1.SetNotificationPositions(notifications);

            byte[] bytes1 = new byte[desc2.SizeInBytes / 2];
            byte[] bytes2 = new byte[desc2.SizeInBytes];

            Stream stream = File.Open(audioFile, FileMode.Open);

            Thread fillBuffer = new Thread(() =>
            {
                int readNumber = 1;
                int bytesRead;

                bytesRead = stream.Read(bytes2, 0, desc2.SizeInBytes);
                sBuffer1.Write<byte>(bytes2, 0, LockFlags.None);
                sBuffer1.Play(0, PlayFlags.None);
                while (true)
                {
                    if (bytesRead == 0) { break; }
                    notifications[0].Event.WaitOne();
                    bytesRead = stream.Read(bytes1, 0, bytes1.Length);
                    sBuffer1.Write<byte>(bytes1, 0, LockFlags.None);

                    if (bytesRead == 0) { break; }
                    notifications[1].Event.WaitOne();
                    bytesRead = stream.Read(bytes1, 0, bytes1.Length);
                    sBuffer1.Write<byte>(bytes1, desc2.SizeInBytes / 2, LockFlags.None);
                }
                stream.Close();
                stream.Dispose();
            });
            fillBuffer.Start();
        }
    }
}