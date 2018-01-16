using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using System.Management;
using System.IO;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;
using HidUtilityNuget;


namespace RotaryTable
{
    /*
     *  The Model 
     */
    public class Communicator
    {
        // Instance variables
        public HidUtility HidUtil { get; set; }
        private ushort _Vid;
        private ushort _Pid;
        private byte _DisplayBrightness;
        private byte _DisplayContrast;
        public byte DisplaySavedBrightness { get; private set; }
        public byte DisplaySavedContrast { get; private set; }
        public List<byte> PacketsToRequest { get; set; }
        private List<UsbCommand> PendingCommands;
        public uint TxCount { get; private set; }
        public uint TxFailedCount { get; private set; }
        public uint RxCount { get; private set; }
        public uint RxFailedCount { get; private set; }
        public bool WaitingForDevice { get; private set; }
        //Information obtained from the device
        public Int16 CurrentMeasurementAdc { get; private set; }
        public Int32 CurrentMeasurementAdcSum { get; private set; }
        public Int16 CurrentMeasurement { get; private set; }
        public Int32[] CalibrationValues { get; private set; } = new Int32[14];

        public string DebugString { get; private set; }



        public class UsbCommand
        {
            public byte command { get; set; }
            public List<byte> data { get; set; }

            public UsbCommand(byte command)
            {
                this.command = command;
                this.data = new List<byte>();
            }

            public UsbCommand(byte command, byte value)
            {
                this.command = command;
                this.data = new List<byte>();
                this.data.Add(value);
            }

            public UsbCommand(byte command, byte index, Int32 value)
            {
                this.command = command;
                this.data = new List<byte>();
                this.data.Add(index);
                byte[] val_bytes = BitConverter.GetBytes(value);
                this.data.Add(val_bytes[2]);
                this.data.Add(val_bytes[1]);
                this.data.Add(val_bytes[0]);
            }

            public List<byte> GetByteList()
            {
                List<byte> ByteList = new List<byte>();
                ByteList.Add(command);
                foreach (byte b in data)
                {
                    ByteList.Add(b);
                }
                return ByteList;
            }
        } // End of UsbCommand

        //Others
        private bool _NewDataAvailable;

        public Communicator()
        {
            // Initialize variables
            TxCount = 0;
            TxFailedCount = 0;
            RxCount = 0;
            RxFailedCount = 0;
            PendingCommands = new List<UsbCommand>();
            PacketsToRequest = new List<byte>();
            PacketsToRequest.Add(0x10);
            WaitingForDevice = false;
            _NewDataAvailable = false;

            // Obtain and initialize an instance of HidUtility
            HidUtil = new HidUtility();

            // Subscribe to HidUtility events
            HidUtil.RaiseConnectionStatusChangedEvent += ConnectionStatusChangedHandler;
            HidUtil.RaiseSendPacketEvent += SendPacketHandler;
            HidUtil.RaisePacketSentEvent += PacketSentHandler;
            HidUtil.RaiseReceivePacketEvent += ReceivePacketHandler;
            HidUtil.RaisePacketReceivedEvent += PacketReceivedHandler;
        }

        //Convert binary coded decimal byte to integer
        private uint BcdToUint(byte bcd)
        {
            uint lower = (uint)(bcd & 0x0F);
            uint upper = (uint)(bcd >> 4);
            return (10 * upper) + lower;
        }

        //Convert integer to binary encoded decimal byte
        private byte UintToBcd(uint val)
        {
            uint lower = val % 10;
            uint upper = val / 10;
            byte retval = (byte)upper;
            retval <<= 4;
            retval |= (byte)lower;
            return retval;
        }

        //Accessors for the UI to call
        public bool NewDataAvailable
        {
            get
            {
                if (_NewDataAvailable)
                {
                    _NewDataAvailable = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private Int32 IntFromBytes(byte b1, byte b2, byte b3)
        {
            //b3 is the least significant byte
            //the most significant byte b0 is assumed to be zero
            byte[] byte_array = {b3, b2, b1, 0x00};
            //byte[] byte_array = { 0x01, 0x23, 0x45, 0x67 };
            return BitConverter.ToInt32(byte_array, 0);
        }


        //Function to parse packet received over USB
        private void ParseData(ref UsbBuffer InBuffer)
        {
            //Input values are often encoded as Int16
            CurrentMeasurementAdc = (Int16)((InBuffer.buffer[3] << 8) + InBuffer.buffer[2]);
            CurrentMeasurementAdcSum = (Int32)((InBuffer.buffer[6] << 16) + (InBuffer.buffer[5] << 8) + InBuffer.buffer[4]);
            CurrentMeasurement = (Int16)((InBuffer.buffer[9] << 8) + InBuffer.buffer[8]);
            _DisplayBrightness = (byte) InBuffer.buffer[12];
            _DisplayContrast = (byte) InBuffer.buffer[13];
            DisplaySavedBrightness = (byte)InBuffer.buffer[14];
            DisplaySavedContrast = (byte)InBuffer.buffer[15];
            //Calibration
            for(int i=0; i<CalibrationValues.Length; ++i)
            {
                CalibrationValues[i] = IntFromBytes(InBuffer.buffer[3 * i + 24], InBuffer.buffer[3 * i + 23], InBuffer.buffer[3 * i + 22]);
            }
            
            _NewDataAvailable = true;
        }

        // Accessor for _Vid
        // Only update selected device if the value has actually changed
        public ushort Vid
        {
            get
            {
                return _Vid;
            }
            set
            {
                if (value != _Vid)
                {
                    _Vid = value;
                    HidUtil.SelectDevice(new Device(_Vid, _Pid));
                }
            }
        }

        // Accessor for _Pid
        // Only update selected device if the value has actually changed
        public ushort Pid
        {
            get
            {
                return _Pid;
            }
            set
            {
                if (value != _Pid)
                {
                    _Pid = value;
                    HidUtil.SelectDevice(new Device(_Vid, _Pid));
                }
            }
        }

        // Accessor for _DisplayBrightness
        public byte DisplayBrightness
        {
            get { return _DisplayBrightness; }
            set
            {
                ScheduleCommand(new Communicator.UsbCommand(0x40, value));
                _DisplayBrightness = value;
            }

        }

        // Accessor for _ContrastValue
        public byte DisplayContrast
        {
            get { return _DisplayContrast; }
            set
            {
                ScheduleCommand(new Communicator.UsbCommand(0x41, value));
                _DisplayContrast = value;
            }
        }

        public void SetCalibration(byte index, Int32 newValue)
        {
            UsbCommand cmd = new UsbCommand(0x60, index, newValue);
            ScheduleCommand(cmd);
        }

        /*
         * HidUtility callback functions
         */

        public void ConnectionStatusChangedHandler(object sender, HidUtility.ConnectionStatusEventArgs e)
        {
            if (e.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
            {
                // Reset variables
                _NewDataAvailable = false;
                TxCount = 0;
                TxFailedCount = 0;
                RxCount = 0;
                RxFailedCount = 0;
                PendingCommands = new List<UsbCommand>();
                PacketsToRequest = new List<byte>();
                PacketsToRequest.Add(0x10);
                WaitingForDevice = false;
            }
        }

        // HidUtility asks if a packet should be sent to the device
        // Prepare the buffer and request a transfer
        public void SendPacketHandler(object sender, UsbBuffer OutBuffer)
        {
            DebugString = "Start SendPacketHandler";
            // Fill entire buffer with 0xFF
            OutBuffer.clear();

            // The first byte is the "Report ID" and does not get sent over the USB bus.  Always set = 0.
            OutBuffer.buffer[0] = 0x00;

            //Prepare data to send
            byte NextPacket;
            if (PacketsToRequest.Count >= 1)
            {
                NextPacket = PacketsToRequest[0];
                PacketsToRequest.RemoveAt(0);
            }
            else
            {
                NextPacket = 0x10;
            }
            OutBuffer.buffer[1] = NextPacket;
            PacketsToRequest.Add(NextPacket);

            int position = 2;
            while ((position <= 64) && (PendingCommands.Count > 0))
            {
                List<byte> CommandBytes = PendingCommands[0].GetByteList();

                //Check if entire command fits into current buffer
                if ((64 - position) >= CommandBytes.Count)
                {
                    foreach (byte b in CommandBytes)
                    {
                        OutBuffer.buffer[position] = b;
                        ++position;
                    }
                    PendingCommands.RemoveAt(0);
                }
                else
                {
                    position += CommandBytes.Count;
                    break;
                }
            }

            //Request the packet to be sent over the bus
            OutBuffer.RequestTransfer = true;
            DebugString = "End SendPacketHandler";
        }

        // HidUtility informs us if the requested transfer was successful
        // Schedule to request a packet if the transfer was successful
        public void PacketSentHandler(object sender, UsbBuffer OutBuffer)
        {
            DebugString = "Start PacketSentHandler";
            WaitingForDevice = OutBuffer.TransferSuccessful;
            if (OutBuffer.TransferSuccessful)
            {
                ++TxCount;
            }
            else
            {
                ++TxFailedCount;
            }
            DebugString = "End PacketSentHandler";
        }

        // HidUtility asks if a packet should be requested from the device
        // Request a packet if a packet has been successfully sent to the device before
        public void ReceivePacketHandler(object sender, UsbBuffer InBuffer)
        {
            DebugString = "Start ReceivePacketHandler";
            WaitingForDevice = true;
            InBuffer.RequestTransfer = WaitingForDevice;
            DebugString = "End ReceivePacketHandler";
        }

        // HidUtility informs us if the requested transfer was successful and provides us with the received packet
        public void PacketReceivedHandler(object sender, UsbBuffer InBuffer)
        {
            DebugString = "Start PacketReceivedHandler";
            WaitingForDevice = false;

            //Parse received data
            switch (InBuffer.buffer[1])
            {
                case 0x10:
                    ParseData(ref InBuffer);
                    break;
            };

            //Some statistics
            if (InBuffer.TransferSuccessful)
            {
                ++RxCount;
            }
            else
            {
                ++RxFailedCount;
            }
            DebugString = "End PacketReceivedHandler";
        }

        public bool RequestValid()
        {
            return true;
        }

        public void ScheduleCommand(UsbCommand cmd)
        {
            PendingCommands.Add(cmd);
        }

    } // Communicator

}
