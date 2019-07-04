using System;

namespace CannedBytes.Midi.Message
{
    /// <summary>
    /// Represents a midi real-time (short) message.
    /// </summary>
    public class MidiSysRealTimeMessage : MidiShortMessage
    {
        /// <summary>
        /// Constructs a new instance on the specified message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Only the least significant byte is set.</param>
        public MidiSysRealTimeMessage(int data)
        {
            Data = MidiData.GetData8(data);
            ByteLength = 1;

            if (Enum.IsDefined(typeof(MidiSysRealTimeType), Data))
            {
                RealTimeType = (MidiSysRealTimeType)Enum.ToObject(typeof(MidiSysRealTimeType), Data);
            }
            else
            {
                RealTimeType = MidiSysRealTimeType.Invalid;
            }
        }

        /// <summary>
        /// The type of real-time midi message.
        /// </summary>
        public MidiSysRealTimeType RealTimeType { get; set; }
    }
}

