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
using ConfigurationFile;
using HidUtilityNuget;


namespace RotaryTable
{
    /*
     *  The ViewModel 
     */
    public class CommunicatorViewModel : INotifyPropertyChanged
    {
        private Communicator communicator;
        private ConfigFile config;
        DispatcherTimer timer;
        private DateTime ConnectedTimestamp = DateTime.Now;
        public string ActivityLogTxt { get; private set; }
        private Int32[] _calibration = new Int32[14];
        private bool[] _calibration_pending = new bool[14];
        private ushort _Pid;
        private ushort _Vid;
        //private byte _DisplayBrightness = 0;
        //private byte _DisplayContrast = 0;
        private int _WindowPositionX;
        private int _WindowPositionY;
        public event PropertyChangedEventHandler PropertyChanged;

        public CommunicatorViewModel()
        {
            //Change this later to not include path
            //config = new ConfigFile("C:\\Users\\lfaes\\OneDrive\\Visual Studio 2017\\Projects\\RotaryTable\\config.xml");
            config = new ConfigFile("config.xml");
            _WindowPositionX = config.PositionX;
            _WindowPositionY = config.PositionY;

            communicator = new Communicator();
            communicator.HidUtil.RaiseDeviceAddedEvent += DeviceAddedEventHandler;
            communicator.HidUtil.RaiseDeviceRemovedEvent += DeviceRemovedEventHandler;
            communicator.HidUtil.RaiseConnectionStatusChangedEvent += ConnectionStatusChangedHandler;
            _Vid = config.VendorId;
            _Pid = config.ProductId;
            communicator.Vid = _Vid;
            communicator.Pid = _Pid;

            //Configure and start timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += TimerTickHandler;
            timer.Start();

            WriteLog("Program started", true);
        }

        //Destructor
        ~CommunicatorViewModel()
        {
            //Save data to config file
            config.PositionX = _WindowPositionX;
            config.PositionY = _WindowPositionY;
        }

        /*
         * Local function definitions
         */

        // Add a line to the activity log text box
        void WriteLog(string message, bool clear)
        {
            // Replace content
            if (clear)
            {
                ActivityLogTxt = string.Format("{0}: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
            }
            // Add new line
            else
            {
                ActivityLogTxt += Environment.NewLine + string.Format("{0}: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
            }
        }

        public void RequestLeftEncoderTurnLeft()
        {
            WriteLog("Left encoder turn left button clicked", false);
            communicator.RequestEncoder(Communicator.RotaryEncoder.LeftEncoder_TurnLeft);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void RequestLeftEncoderTurnRight()
        {
            WriteLog("Left encoder turn right button clicked", false);
            communicator.RequestEncoder(Communicator.RotaryEncoder.LeftEncoder_TurnRight);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void RequestLeftEncoderButtonPress()
        {
            WriteLog("Left encoder press button clicked", false);
            communicator.RequestEncoder(Communicator.RotaryEncoder.LeftEncoder_ButtonPress);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void RequestRightEncoderTurnLeft()
        {
            WriteLog("Right encoder turn left button clicked", false);
            communicator.RequestEncoder(Communicator.RotaryEncoder.RightEncoder_TurnLeft);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void RequestRightEncoderTurnRight()
        {
            WriteLog("Right encoder turn right button clicked", false);
            communicator.RequestEncoder(Communicator.RotaryEncoder.RightEncoder_TurnRight);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void RequestRightEncoderButtonPress()
        {
            WriteLog("Right encoder press button clicked", false);
            communicator.RequestEncoder(Communicator.RotaryEncoder.RightEncoder_ButtonPress);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void SavePidVid()
        {
            //communicator.ScheduleCommand(new Communicator.UsbCommand(0x30));
            //WriteLog("Brightness and contrast saved", false);

            if ((_Pid != communicator.Pid) || (_Vid != communicator.Vid))
            {
                config.ProductId = _Pid;
                config.VendorId = _Vid;
                communicator.Pid = _Pid;
                communicator.Vid = _Vid;
                string log = string.Format("New PID/VID saved and applied (VID=0x{0:X4} PID=0x{1:X4})", _Vid, _Pid);
                WriteLog(log, false);
            }
            PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
        }

        public void ResetPidVid()
        {
            //communicator.ScheduleCommand(new Communicator.UsbCommand(0x31));
            //DisplayBrightness = communicator.DisplaySavedBrightness;
            //DisplayContrast = communicator.DisplaySavedContrast;
            //PropertyChanged(this, new PropertyChangedEventArgs("DisplayBrightness"));
            //PropertyChanged(this, new PropertyChangedEventArgs("DisplayContrast"));
            //WriteLog("Brightness and contrast reset", false);

            if ((_Pid != communicator.Pid) || (_Vid != communicator.Vid))
            {
                _Pid = communicator.Pid;
                _Vid = communicator.Vid;
                PropertyChanged(this, new PropertyChangedEventArgs("VidTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("PidTxt"));
                WriteLog("PID/VID reset", false);
            }
            PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
        }

        public void ResetCalibration()
        {
            for (int i = 0; i < _calibration.Length; ++i)
            {
                _calibration[i] = communicator.CalibrationValues[i];
                _calibration_pending[i] = false;
            }
            WriteLog("Calibration resetted", false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration00Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration01Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration02Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration03Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration04Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration05Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration06Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration07Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration08Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration09Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration10Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration11Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration12Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("Calibration13Txt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void SaveCalibration()
        {
            for (byte i = 0; i < _calibration.Length; ++i)
            {
                if (_calibration_pending[i])
                {
                    communicator.SetCalibration(i, _calibration[i]);
                    _calibration_pending[i] = false;
                    WriteLog(string.Format("Calibration {0} set to {1}", i, _calibration[i]), false);
                }
            }
            //WriteLog("Calibration saved", false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void SimpleReqest(Communicator.SimpleRequest request)
        {
            WriteLog(String.Format("Request sent: {0}", request), false);
            communicator.SendSimpleRequest(request);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        private void JumpDegrees(double degrees)
        {
            WriteLog(String.Format("Jump counter-clockwise by {0:0.000} degrees", degrees), false);
            communicator.RequestJumpDegrees(degrees);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void Jump30CCW()
        {
            JumpDegrees(-30.0);
        }

        public void Jump30CW()
        {
            JumpDegrees(30.0);
        }

        public void Jump10CCW()
        {
            JumpDegrees(-10.0);
        }

        public void Jump10CW()
        {
            JumpDegrees(10.0);
        }

        public void GoToZero()
        {
            SimpleReqest(Communicator.SimpleRequest.GoToZero);
        }

        public void ContinuousLeft()
        {
            SimpleReqest(Communicator.SimpleRequest.StartTurnManualCCW);
        }

        public void ContinuousRight()
        {
            SimpleReqest(Communicator.SimpleRequest.StartTurnManualCW);
        }

        public void MotorStop()
        {
            SimpleReqest(Communicator.SimpleRequest.StopMotorManual);
        }

        public void Setup_SetZeroCCW()
        {
            WriteLog("Set current position as zero (CCW)", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SetZeroPositionCCW));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void Setup_SetZeroCW()
        {
            WriteLog("Set current position as zero (CW)", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SetZeroPositionCW));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void MainMenu()
        {
            WriteLog("Entering main menu", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SelectModeMainMenu));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void MainMenu_Setup()
        {
            WriteLog("Entering Setup menu", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SelectModeSetup));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void MainMenu_Divide()
        {
            WriteLog("Entering Divide menu", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SelectModeDivide));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void MainMenu_Arc()
        {
            WriteLog("Entering Arc menu", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SelectModeArc));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void MainMenu_Manual()
        {
            WriteLog("Entering Manual menu", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SelectModeManual));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void MainMenu_Go2Zero()
        {
            WriteLog("Entering Go2Zero menu", false);
            communicator.ScheduleCommand(new Communicator.UsbCommand((byte)Communicator.SimpleRequest.SelectModeGo2Zero));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }


        public ICommand LeftEncoder_TurnLeftClick
        {
            get
            {
                return new UiCommand(this.RequestLeftEncoderTurnLeft, communicator.RequestValid);
            }
        }

        public ICommand LeftEncoder_TurnRightClick
        {
            get
            {
                return new UiCommand(this.RequestLeftEncoderTurnRight, communicator.RequestValid);
            }
        }

        public ICommand LeftEncoder_ButtonPressClick
        {
            get
            {
                return new UiCommand(this.RequestLeftEncoderButtonPress, communicator.RequestValid);
            }
        }

        public ICommand RightEncoder_TurnLeftClick
        {
            get
            {
                return new UiCommand(this.RequestRightEncoderTurnLeft, communicator.RequestValid);
            }
        }

        public ICommand RightEncoder_TurnRightClick
        {
            get
            {
                return new UiCommand(this.RequestRightEncoderTurnRight, communicator.RequestValid);
            }
        }

        public ICommand RightEncoder_ButtonPressClick
        {
            get
            {
                return new UiCommand(this.RequestRightEncoderButtonPress, communicator.RequestValid);
            }
        }

        public ICommand SavePidVidClick
        {
            get
            {
                return new UiCommand(this.SavePidVid, communicator.RequestValid);
            }
        }

        public ICommand ResetPidVidClick
        {
            get
            {
                return new UiCommand(this.ResetPidVid, communicator.RequestValid);
            }
        }

        public ICommand ResetCalibrationClick
        {
            get
            {
                return new UiCommand(this.ResetCalibration, communicator.RequestValid);
            }
        }

        public ICommand SaveCalibrationClick
        {
            get
            {
                return new UiCommand(this.SaveCalibration, communicator.RequestValid);
            }
        }

        public ICommand Jump30CCWClick
        {
            get
            {
                return new UiCommand(this.Jump30CCW, communicator.RequestValid);
            }
        }

        public ICommand Jump30CWClick
        {
            get
            {
                return new UiCommand(this.Jump30CW, communicator.RequestValid);
            }
        }

        public ICommand Jump10CCWClick
        {
            get
            {
                return new UiCommand(this.Jump10CCW, communicator.RequestValid);
            }
        }

        public ICommand Jump10CWClick
        {
            get
            {
                return new UiCommand(this.Jump10CW, communicator.RequestValid);
            }
        }

        public ICommand GoToZeroClick
        {
            get
            {
                return new UiCommand(this.GoToZero, communicator.RequestValid);
            }
        }

        public ICommand ContinuousLeftClick
        {
            get
            {
                return new UiCommand(this.ContinuousLeft, communicator.RequestValid);
            }
        }

        public ICommand ContinuousRightClick
        {
            get
            {
                return new UiCommand(this.ContinuousRight, communicator.RequestValid);
            }
        }

        public ICommand ContinuousRightRelease
        {
            get
            {
                return new UiCommand(this.MotorStop, communicator.RequestValid);
            }
        }

        public ICommand Setup_SetZeroCCWClick
        {
            get
            {
                return new UiCommand(this.Setup_SetZeroCCW, communicator.RequestValid);
            }
        }

        public ICommand Setup_SetZeroCWClick
        {
            get
            {
                return new UiCommand(this.Setup_SetZeroCW, communicator.RequestValid);
            }
        }

        public ICommand MainMenu_SetupClick
        {
            get
            {
                return new UiCommand(this.MainMenu_Setup, communicator.RequestValid);
            }
        }

        public ICommand MainMenu_DivideClick
        {
            get
            {
                return new UiCommand(this.MainMenu_Divide, communicator.RequestValid);
            }
        }

        public ICommand MainMenu_ArcClick
        {
            get
            {
                return new UiCommand(this.MainMenu_Arc, communicator.RequestValid);
            }
        }

        public ICommand MainMenu_ManualClick
        {
            get
            {
                return new UiCommand(this.MainMenu_Manual, communicator.RequestValid);
            }
        }

        public ICommand MainMenu_Go2ZeroClick
        {
            get
            {
                return new UiCommand(this.MainMenu_Go2Zero, communicator.RequestValid);
            }
        }

        public string UserInterfaceEnabled
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return "True";
                else
                    return "False";
            }
        }

        public string MoveAllowed
        {
            get
            {
                if(communicator.DeviceStatus_Busy==0)
                    return "True";
                else
                    return "False";
            }
        }

        public string UserInterfaceColor
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return "Black";
                else
                    return "Gray";
            }
        }

        public void TimerTickHandler(object sender, EventArgs e)
        {
            if (communicator.NewDebugString)
            {
                WriteLog(communicator.DebugString, false);
                communicator.NewDebugString = false;
            }

            if (PropertyChanged != null)
            {
                //WriteLog(communicator.DebugString, false);

                if (communicator.NewDataAvailable)
                {
                    for(int i=0; i<_calibration.Length; ++i)
                    {
                        if (!_calibration_pending[i])
                        {
                            _calibration[i] = communicator.CalibrationValues[i];
                            //string s = string.Format("Calibration{{0:00}Txt", i);
                            //PropertyChanged(this, new PropertyChangedEventArgs(s));
                        }
                    }

                    PropertyChanged(this, new PropertyChangedEventArgs("DisplayTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("PositionTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("TemperatureInternalTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("TemperatureExternalTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("FanTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("BrakeTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("FirmwareVersionTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("BusyTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("DeviceConfigurationTxt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("MoveAllowed"));
                    PropertyChanged(this, new PropertyChangedEventArgs("ManualSpeed"));

                    /*
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration00Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration01Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration02Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration03Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration04Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration05Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration06Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration07Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration08Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration09Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration10Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration11Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration12Txt"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Calibration13Txt"));
                    */
                }

                //Update these in any case
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ConnectionStatusTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("UptimeTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TxSuccessfulTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TxFailedTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("RxSuccessfulTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("RxFailedTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TxSpeedTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("RxSpeedTxt"));
            }
        }

        public void DeviceAddedEventHandler(object sender, Device dev)
        {
            WriteLog("Device added: " + dev.ToString(), false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DeviceListTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public void DeviceRemovedEventHandler(object sender, Device dev)
        {
            WriteLog("Device removed: " + dev.ToString(), false);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("DeviceListTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }

        }

        public void ConnectionStatusChangedHandler(object sender, HidUtility.ConnectionStatusEventArgs e)
        {
            WriteLog("Connection status changed to: " + e.ToString(), false);
            switch (e.ConnectionStatus)
            {
                case HidUtility.UsbConnectionStatus.Connected:
                    ConnectedTimestamp = DateTime.Now;
                    break;
                case HidUtility.UsbConnectionStatus.Disconnected:
                    break;
                case HidUtility.UsbConnectionStatus.NotWorking:
                    break;
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ConnectionStatusTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("UptimeTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("UserInterfaceColor"));
                PropertyChanged(this, new PropertyChangedEventArgs("UserInterfaceEnabled"));
                PropertyChanged(this, new PropertyChangedEventArgs("DisplayTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("PositionTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TemperatureInternalTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("TemperatureExternalTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("FanTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("BrakeTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("FirmwareVersionTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("BusyTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("MoveAllowed"));
                PropertyChanged(this, new PropertyChangedEventArgs("DeviceConfigurationTxt"));
                PropertyChanged(this, new PropertyChangedEventArgs("ManualSpeed"));

            }
        }

        public int ManualSpeedMinimum
        {
            get
            {
                return communicator.DeviceConfig_MinimumSpeed;
            }
        }

        public int ManualSpeedMaximum
        {
            get
            {
                return communicator.DeviceConfig_MaximumSpeedManual;
            }
        }

        public int ManualSpeed
        {
            get
            {
                return communicator.ManualSpeed;
            }
            set
            {
                communicator.ManualSpeed = Convert.ToUInt16(value);
            }
        }

        public string FirmwareVersionTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "Not connected";
                else
                {
                    return ("Fimware " + communicator.FirmwareMajor).ToString() + "." + (communicator.FirmwareMinor).ToString() + "." + (communicator.FirmwareFix).ToString();
                }
            }
        }

        public string BusyTxt
        {
            get
            {
                if (communicator.DeviceStatus_Busy==0)
                    return "";
                else
                {
                    return "busy...";
                }
            }
        }

        public string PositionTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "- - -";
                else
                {
                    double position = 360.0 * (double)communicator.DeviceStatus_CurrentPositionInSteps / (double)communicator.DeviceConfig_FullCircleInSteps;
                    if(position>180.0)
                    {
                        position -= 360.0;
                    }
                    return string.Format("{0:0.000}°", position);
                }
            }   
        }

        public string TemperatureInternalTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "";
                else
                    return String.Format("On-board temperature: {0:0.0}°C", 0.01 * communicator.DeviceStatus_InternalTemperature);
            }
        }

        public string TemperatureExternalTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "";
                else
                    return String.Format("External temperature: {0:0.0}°C", 0.01 * communicator.DeviceStatus_ExternalTemperature);
            }
        }

        public string FanTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "";
                else
                { 
                    if (communicator.DeviceStatus_FanOn != 0)
                        return "Fan ON";
                    else
                        return "Fan OFF";
                }
            }
        }

        public string BrakeTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "";
                else
                {
                    if (communicator.DeviceStatus_BrakeOn != 0)
                        return "Brake ON";
                    else
                        return "Brake OFF";
                }
            }
        }

        public string DisplayTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "";
                else
                {
                    return communicator.DisplayContent[0]
                        + System.Environment.NewLine
                        + communicator.DisplayContent[1]
                        + System.Environment.NewLine
                        + communicator.DisplayContent[2]
                        + System.Environment.NewLine
                        + communicator.DisplayContent[3];
                }
            }
        }

        public string DeviceConfigurationTxt
        {

            get
            {
                string byte_to_bool(byte b)
                {
                    if (b == 0)
                        return "No";
                    else
                        return "Yes";
                }


                if (communicator.HidUtil.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
                    return "Not connected";
                else
                {

                    string s = "Full circle in steps: {0}" + System.Environment.NewLine
                        + "Inverse direction: {1}" + System.Environment.NewLine
                        + "Overshoot in steps: {2}" + System.Environment.NewLine
                        + "Overshoot cost in steps: {3}" + System.Environment.NewLine
                        + "Minimum speed: {4}" + System.Environment.NewLine
                        + "Maximum speed: {5}" + System.Environment.NewLine
                        + "Initial speed arc: {6}" + System.Environment.NewLine
                        + "Maximum speed arc: {7}" + System.Environment.NewLine
                        + "Initial speed manual: {8}" + System.Environment.NewLine
                        + "Maximum speed manual: {9}" + System.Environment.NewLine
                        + "Beep duration: {10}" + System.Environment.NewLine;
                    return String.Format(s,
                        communicator.DeviceConfig_FullCircleInSteps,
                        byte_to_bool(communicator.DeviceConfig_InverseDirection),
                        communicator.DeviceConfig_OvershootInSteps,
                        communicator.DeviceConfig_OvershootCostInSteps,
                        communicator.DeviceConfig_MinimumSpeed,
                        communicator.DeviceConfig_MaximumSpeed,
                        communicator.DeviceConfig_InitialSpeedArc,
                        communicator.DeviceConfig_MaximumSpeedArc,
                        communicator.DeviceConfig_InitialSpeedManual,
                        communicator.DeviceConfig_MaximumSpeedManual,
                        communicator.DeviceConfig_BeepDuration);
                }
            }
        }

        public string DeviceListTxt
        {
            get
            {
                string txt = "";
                foreach (Device dev in communicator.HidUtil.DeviceList)
                {
                    string devString = string.Format("VID=0x{0:X4} PID=0x{1:X4}: {2} ({3})", dev.Vid, dev.Pid, dev.Caption, dev.Manufacturer);
                    txt += devString + Environment.NewLine;
                }
                return txt.TrimEnd();
            }
        }

        // Try to convert a (hexadecimal) string to an unsigned 16-bit integer
        // Return 0 if the conversion fails
        // This function is used to parse the PID and VID text boxes
        private ushort ParseHex(string input)
        {
            input = input.ToLower();
            if (input.Length >= 2)
            {
                if (input.Substring(0, 2) == "0x")
                {
                    input = input.Substring(2);
                }
            }
            try
            {
                ushort value = ushort.Parse(input, System.Globalization.NumberStyles.HexNumber);
                return value;
            }
            catch
            {
                return 0;
            }
        }

        public string VidTxt
        {
            get
            {
                return string.Format("0x{0:X4}", _Vid);
            }
            set
            {
                _Vid = ParseHex(value);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public string PidTxt
        {
            get
            {
                return string.Format("0x{0:X4}", _Pid);
            }
            set
            {
                _Pid = ParseHex(value);
                PropertyChanged(this, new PropertyChangedEventArgs("ActivityLogTxt"));
            }
        }

        public string ConnectionStatusTxt
        {
            get
            {
                return string.Format("Connection Status: {0}", communicator.HidUtil.ConnectionStatus.ToString());
            }
        }

        public string UptimeTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                {
                    //Save time elapsed since the device was connected
                    TimeSpan uptime = DateTime.Now - ConnectedTimestamp;
                    //Return uptime as string
                    return string.Format("Uptime: {0}", uptime.ToString(@"hh\:mm\:ss\.f"));
                }
                else
                {
                    return "Uptime: -";
                }
            }
        }

        public string TxSuccessfulTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Sent: {0}", communicator.TxCount);
                else
                    return "Sent: -";
            }
        }

        public string TxFailedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Sending failed: {0}", communicator.TxFailedCount);
                else
                    return "Sending failed: -";
            }
        }

        public string RxSuccessfulTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Received: {0}", communicator.RxCount);
                else
                    return "Receied: -";
            }
        }

        public string RxFailedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                    return string.Format("Reception failed: {0}", communicator.RxFailedCount);
                else
                    return "Reception failed: -";
            }
        }

        public string TxSpeedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                {
                    if (communicator.TxCount != 0)
                    {
                        return string.Format("TX Speed: {0:0.00} packets per second", communicator.TxCount / (DateTime.Now - ConnectedTimestamp).TotalSeconds);
                    }
                }
                return "TX Speed: n/a";
            }
        }

        public string RxSpeedTxt
        {
            get
            {
                if (communicator.HidUtil.ConnectionStatus == HidUtility.UsbConnectionStatus.Connected)
                {
                    if (communicator.TxCount != 0)
                    {
                        return string.Format("RX Speed: {0:0.00} packets per second", communicator.TxCount / (DateTime.Now - ConnectedTimestamp).TotalSeconds);
                    }
                }
                return "RX Speed: n/a";
            }
        }

        public string StatusVisibility
        {
            get
            {
                if (config.StatusVisible)
                    return "Visible";
                else
                    return "Collapsed";
            }
            set
            {
                if (value == "Visible")
                    config.StatusVisible = true;
                else
                    config.StatusVisible = false;
            }
        }

        public string MainControlVisibility
        {
            get
            {
                if (config.MainControlVisible)
                    return "Visible";
                else
                    return "Collapsed";
            }
            set
            {
                if (value == "Visible")
                    config.MainControlVisible = true;
                else
                    config.MainControlVisible = false;
            }
        }

        public string ActivityLogVisibility
        {
            get
            {
                if (config.ActivityLogVisible)
                    return "Visible";
                else
                    return "Collapsed";
            }
            set
            {
                if (value == "Visible")
                    config.ActivityLogVisible = true;
                else
                    config.ActivityLogVisible = false;
            }
        }

        public string CommunicationVisibility
        {
            get
            {
                if (config.ConnectionDetailsVisible)
                    return "Visible";
                else
                    return "Collapsed";
            }
            set
            {
                if (value == "Visible")
                    config.ConnectionDetailsVisible = true;
                else
                    config.ConnectionDetailsVisible = false;
            }
        }

        public int WindowPositionX
        {
            get
            {
                return _WindowPositionX;
            }
            set
            {
                _WindowPositionX = value;
            }
        }

        public int WindowPositionY
        {
            get
            {
                return _WindowPositionY;
            }
            set
            {
                _WindowPositionY = value;
            }
        }

        public byte DisplayBrightness
        {
            get { return communicator.DisplayBrightness; }
            set { communicator.DisplayBrightness = value; }
        }

        public byte DisplayContrast
        {
            get { return communicator.DisplayContrast; }
            set { communicator.DisplayContrast = value; }
        }

        public string Calibration00Txt
        {
            get { return _calibration[0].ToString(); }
            set
            {
                _calibration_pending[0] = true;
                 _calibration[0] = Int32.Parse(value);
            }
        }

        public string Calibration01Txt
        {
            get { return _calibration[1].ToString(); }
            set
            {
                _calibration_pending[1] = true;
                _calibration[1] = Int32.Parse(value);
            }
        }

        public string Calibration02Txt
        {
            get { return _calibration[2].ToString(); }
            set
            {
                _calibration_pending[2] = true;
                _calibration[2] = Int32.Parse(value);
            }
        }

        public string Calibration03Txt
        {
            get { return _calibration[3].ToString(); }
            set
            {
                _calibration_pending[3] = true;
                _calibration[3] = Int32.Parse(value);
            }
        }

        public string Calibration04Txt
        {
            get { return _calibration[4].ToString(); }
            set
            {
                _calibration_pending[4] = true;
                _calibration[4] = Int32.Parse(value);
            }
        }

        public string Calibration05Txt
        {
            get { return _calibration[5].ToString(); }
            set
            {
                _calibration_pending[5] = true;
                _calibration[5] = Int32.Parse(value);
            }
        }

        public string Calibration06Txt
        {
            get { return _calibration[6].ToString(); }
            set
            {
                _calibration_pending[6] = true;
                _calibration[6] = Int32.Parse(value);
            }
        }

        public string Calibration07Txt
        {
            get { return _calibration[7].ToString(); }
            set
            {
                _calibration_pending[7] = true;
                _calibration[7] = Int32.Parse(value);
            }
        }

        public string Calibration08Txt
        {
            get { return _calibration[8].ToString(); }
            set
            {
                _calibration_pending[8] = true;
                _calibration[8] = Int32.Parse(value);
            }
        }

        public string Calibration09Txt
        {
            get { return _calibration[9].ToString(); }
            set
            {
                _calibration_pending[9] = true;
                _calibration[9] = Int32.Parse(value);
            }
        }

        public string Calibration10Txt
        {
            get { return _calibration[10].ToString(); }
            set
            {
                _calibration_pending[10] = true;
                _calibration[10] = Int32.Parse(value);
            }
        }

        public string Calibration11Txt
        {
            get { return _calibration[11].ToString(); }
            set
            {
                _calibration_pending[11] = true;
                _calibration[11] = Int32.Parse(value);
            }
        }

        public string Calibration12Txt
        {
            get { return _calibration[12].ToString(); }
            set
            {
                _calibration_pending[12] = true;
                _calibration[12] = Int32.Parse(value);
            }
        }

        public string Calibration13Txt
        {
            get { return _calibration[13].ToString(); }
            set
            {
                _calibration_pending[13] = true;
                _calibration[13] = Int32.Parse(value);
            }
        }
    }

}
