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
        public string[] DisplayContent { get; private set; } = new string[4];
        public List<byte> PacketsToRequest { get; set; }
        private List<UsbCommand> PendingCommands;
        public uint TxCount { get; private set; }
        public uint TxFailedCount { get; private set; }
        public uint RxCount { get; private set; }
        public uint RxFailedCount { get; private set; }
        public bool WaitingForDevice { get; private set; }
        //Device information 
        public byte FirmwareMajor { get; private set; }
        public byte FirmwareMinor { get; private set; }
        public byte FirmwareFix { get; private set; }

        public byte DeviceStatus_SubTimeSlot { get; private set; }
        public byte DeviceStatus_TimeSlot { get; private set; }
        public byte DeviceStatus_Done { get; private set; }
        public sbyte DeviceStatus_LeftEncoderCount { get; private set; }
        public sbyte DeviceStatus_LeftEncoderButton { get; private set; }
        public sbyte DeviceStatus_RightEncoderCount { get; private set; }
        public sbyte DeviceStatus_RightEncoderButton { get; private set; }
        public UInt32 DeviceStatus_CurrentPositionInSteps { get; private set; }
        public Int32 DeviceStatus_CurrentPositionInDegrees { get; private set; }
        public byte DeviceStatus_DisplayState { get; private set; }
        public byte DeviceStatus_BeepCount { get; private set; }
        public Int16 DeviceStatus_InternalTemperature { get; private set; }
        public Int16 DeviceStatus_ExternalTemperature { get; private set; }
        public byte DeviceStatus_FanOn { get; private set; }
        public byte DeviceStatus_BrakeOn { get; private set; }
        public byte DeviceStatus_Busy { get; private set; }

        public UInt32 DeviceConfig_FullCircleInSteps { get; private set; }
        public byte DeviceConfig_InverseDirection { get; private set; }
        public UInt16 DeviceConfig_OvershootInSteps { get; private set; }
        public UInt16 DeviceConfig_MinimumSpeed { get; private set; }
        public UInt16 DeviceConfig_MaximumSpeed { get; private set; }
        public UInt16 DeviceConfig_InitialSpeedArc { get; private set; }
        public UInt16 DeviceConfig_MaximumSpeedArc { get; private set; }
        public UInt16 DeviceConfig_InitialSpeedManual { get; private set; }
        public UInt16 DeviceConfig_MaximumSpeedManual { get; private set; }
        public byte DeviceConfig_BeepDuration { get; private set; }
       

        //Information obtained from the device
        public Int16 CurrentMeasurementAdc { get; private set; }
        public Int32 CurrentMeasurementAdcSum { get; private set; }
        public Int16 CurrentMeasurement { get; private set; }
        public Int32[] CalibrationValues { get; private set; } = new Int32[14];

        public string DebugString { get; private set; }
        public bool NewDebugString { get; set; }



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
            PacketsToRequest.Add(0x11);
            PacketsToRequest.Add(0x12);
            PacketsToRequest.Add(0x13);
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


        //Function to parse status packet received over USB
        private void ParseStatus(ref UsbBuffer InBuffer)
        {
            FirmwareMajor = InBuffer.buffer[4];
            FirmwareMinor = InBuffer.buffer[5];
            FirmwareFix = InBuffer.buffer[6];

            DeviceStatus_SubTimeSlot = InBuffer.buffer[7];
            DeviceStatus_TimeSlot = InBuffer.buffer[8];
            DeviceStatus_Done = InBuffer.buffer[9];
            DeviceStatus_LeftEncoderCount = (sbyte) InBuffer.buffer[10];
            DeviceStatus_LeftEncoderButton = (sbyte)InBuffer.buffer[11];
            DeviceStatus_RightEncoderCount = (sbyte)InBuffer.buffer[12];
            DeviceStatus_RightEncoderButton = (sbyte)InBuffer.buffer[13];
            DeviceStatus_CurrentPositionInSteps = BitConverter.ToUInt32(InBuffer.buffer, 14);
            DeviceStatus_CurrentPositionInDegrees = BitConverter.ToUInt16(InBuffer.buffer, 18);
            DeviceStatus_DisplayState = InBuffer.buffer[22];
            DeviceStatus_BeepCount = InBuffer.buffer[23];
            DeviceStatus_InternalTemperature = BitConverter.ToInt16(InBuffer.buffer, 24);
            DeviceStatus_ExternalTemperature = BitConverter.ToInt16(InBuffer.buffer, 26);
            DeviceStatus_FanOn = InBuffer.buffer[28];
            DeviceStatus_BrakeOn = InBuffer.buffer[29];
            DeviceStatus_Busy = InBuffer.buffer[30];

            //44-47:    uint32_t full_circle_in_steps
            //48:       uint8_t inverse_direction
            //49-50:    uint16_t overshoot_in_steps
            //51-52:    uint16_t minimum_speed
            //53-54:    uint16_t maximum_speed
            //55-56:    uint16_t initial_speed_arc
            //57-58:    uint16_t maximum_speed_arc
            //59-60:    uint16_t initial_speed_manual
            //61-62:    uint16_t maximum_speed_manual
            //63:       uint8_t beep_duration
            DeviceConfig_FullCircleInSteps = BitConverter.ToUInt32(InBuffer.buffer, 45);
            DeviceConfig_InverseDirection = InBuffer.buffer[49];
            DeviceConfig_OvershootInSteps = BitConverter.ToUInt16(InBuffer.buffer, 50);
            DeviceConfig_MinimumSpeed = BitConverter.ToUInt16(InBuffer.buffer, 52);
            DeviceConfig_MaximumSpeed = BitConverter.ToUInt16(InBuffer.buffer, 54);
            DeviceConfig_InitialSpeedArc = BitConverter.ToUInt16(InBuffer.buffer, 56);
            DeviceConfig_MaximumSpeedArc = BitConverter.ToUInt16(InBuffer.buffer, 58);
            DeviceConfig_InitialSpeedManual = BitConverter.ToUInt16(InBuffer.buffer, 60);
            DeviceConfig_MaximumSpeedManual = BitConverter.ToUInt16(InBuffer.buffer, 62);
            DeviceConfig_BeepDuration = InBuffer.buffer[49];
        }

        private void ParseDisplay(ref UsbBuffer InBuffer)
        {
            string Replace(byte b)
            {
                switch(b)
                {
                    case 0x00:
                        return "|";

                    case 0x01:
                        return "°";

                    case 0x02:
                        return "ä";

                    default:
                        return "" + ((char)b);
                }
            }

            int first_line = 0;
            if (InBuffer.buffer[1] == 0x12)
                first_line = 2;

            DisplayContent[first_line] = "";
            DisplayContent[first_line+1] = "";

            for (int i = 0; i < 20; ++i)
            {
                DisplayContent[first_line] += Replace(InBuffer.buffer[4 + i]);
                DisplayContent[first_line+1] += Replace(InBuffer.buffer[24 + i]);
            }
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
                PacketsToRequest.Add(0x11);
                PacketsToRequest.Add(0x12);
                PacketsToRequest.Add(0x13);
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
            DebugString = String.Format("Start PacketReceivedHandler ({0:X})", InBuffer.buffer[1]);
            WaitingForDevice = false;

            //Parse received data
            switch (InBuffer.buffer[1])
            {
                case 0x10:
                    ParseStatus(ref InBuffer);
                    break;

                case 0x11:
                case 0x12:
                    ParseDisplay(ref InBuffer);
                    break;

                case 0x13:
                    //mode-specific details

                    //This is the last package. We now have a consistent snapshot
                    _NewDataAvailable = true;
                    break;

                default:
                    //We have received a packet of unknown content
                    DebugString = String.Format("Unknown packet received: {0:X}", InBuffer.buffer[1]);
                    NewDebugString = true;
                    break;

            }

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

        public enum RotaryEncoder : byte
        {
            LeftEncoder_TurnLeft = 0x30,
            LeftEncoder_TurnRight = 0x31,
            LeftEncoder_ButtonPress = 0x32,
            RightEncoder_TurnLeft = 0x33,
            RightEncoder_TurnRight = 0x34,
            RightEncoder_ButtonPress = 0x35
        }

        public void RequestEncoder(RotaryEncoder action)
        {
            PendingCommands.Add(new UsbCommand((byte)action));
        }

        public void ScheduleCommand(UsbCommand cmd)
        {
            PendingCommands.Add(cmd);
        }

    } // Communicator

}
