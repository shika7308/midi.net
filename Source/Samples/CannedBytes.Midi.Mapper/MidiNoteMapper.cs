using System;
using System.Collections.Generic;
using System.Diagnostics;
using CannedBytes.Midi.Components;

namespace CannedBytes.Midi.Mapper
{
    class MidiNoteMapper : IMidiDataReceiver, IDisposable
    {
        private readonly byte NoteOn = 0x90;
        private readonly byte NoteOff = 0x80;

        private MidiInPort _inPort = new MidiInPort();
        private MidiOutPort _outPort = new MidiOutPort();

        private MidiInPortChainManager _inChainMgr;
        private MidiDiagnosticReceiver _diagnosticsReceiver;

        private MidiNoteMapIndex _index;

        private byte _inNoteOn;
        private byte _inNoteOff;
        private byte _outNoteOn;
        private byte _outNoteOff;

        public MidiNoteMapper()
        {
            _inChainMgr = new MidiInPortChainManager(_inPort);

            _diagnosticsReceiver = new MidiDiagnosticReceiver();
            _inChainMgr.Add(_diagnosticsReceiver);

            _inChainMgr.Add(this);

            _inChainMgr.Initialize(0, 0);
        }

        private byte _inChannel;

        public byte InChannel
        {
            get { return _inChannel; }
            set { _inChannel = value; }
        }

        private byte _outChannel;

        public byte OutChannel
        {
            get { return _outChannel; }
            set { _outChannel = value; }
        }

        private byte _velocityOffset;

        public byte VelocityOffset
        {
            get { return _velocityOffset; }
            set { _velocityOffset = value; }
        }

        private bool _midiThru;

        public bool MidiThru
        {
            get { return _midiThru; }
            set { _midiThru = value; }
        }

        public void Start(int inPortId, int outPortId, MidiNoteMapIndex index)
        {
            _index = index;

            try
            {
                byte channel = (byte)((InChannel - 1) & 0x0F);
                _inNoteOn = (byte)(NoteOn | channel);
                _inNoteOff = (byte)(NoteOff | channel);

                channel = (byte)((OutChannel - 1) & 0x0F);
                _outNoteOn = (byte)(NoteOn | channel);
                _outNoteOff = (byte)(NoteOff | channel);

                _diagnosticsReceiver.Reset();

                _outPort.Open(outPortId);

                _inPort.Open(inPortId);
                _inPort.Start();
            }
            catch
            {
                if (_outPort.HasStatus(MidiPortStatus.Open))
                {
                    _outPort.Close();
                }

                if (_inPort.HasStatus(MidiPortStatus.Open))
                {
                    _inPort.Close();
                }

                throw;
            }
            sendTest();
        }

        private void sendTest()
        {
            var channel = (byte)(0 | 1 | 2);
            var noteOn = (byte)(NoteOn | channel);

            var eventData = new MidiData(noteOn);
            eventData.Parameter1 = 127 / 2;
            eventData.Parameter2 = 127;

            var note1 = eventData.Data;
            _outPort.ShortData(eventData.Data);
        }

        public void Stop()
        {
            _inPort.Stop();
            _inPort.Close();
            _outPort.Close();

            Debug.WriteLine(_diagnosticsReceiver.ToString());
        }

        private void MapData(int data)
        {
            MidiData eventData = new MidiData(data);
            byte status = eventData.Status;
            List<MidiNoteMapItem> items = null;

#if DEBUG
            if (status == 0xFE)
            {
                return;
            }
#endif

            // Note On
            if (status == _inNoteOn)
            {
                if (OutChannel > 0)
                {
                    eventData.Status = _outNoteOn;
                }

                items = _index.Find(eventData.Parameter1);

                if (items != null && eventData.Parameter2 > 0)
                {
                    if (eventData.Parameter2 + VelocityOffset <= 127)
                    {
                        eventData.Parameter2 += VelocityOffset;
                    }
                    else
                    {
                        eventData.Parameter2 = 127;
                    }
                }
            }

            // Note Off
            if (status == _inNoteOff)
            {
                if (OutChannel > 0)
                {
                    eventData.Status = _outNoteOff;
                }

                items = _index.Find(eventData.Parameter1);
            }

            if (items != null)
            {
                foreach (MidiNoteMapItem item in items)
                {
                    eventData.Parameter1 = item.NoteOutNumber;

                    // output mapped event
                    _outPort.ShortData(eventData.Data);
                }
            }
            else if (MidiThru) // output unmapped event
            {
                _outPort.ShortData(eventData.Data);
            }
        }

        #region IMidiReceiver Members

        public void ShortData(int data, long timeIndex)
        {
            MapData(data);
        }

        public void LongData(MidiBufferStream buffer, long timeIndex)
        {
            if (MidiThru)
            {
                _outPort.LongData(buffer);
            }
        }

        #endregion IMidiReceiver Members

        #region IDisposable Members

        public void Dispose()
        {
            _inPort.Dispose();
            _outPort.Dispose();
        }

        #endregion IDisposable Members
    }
}