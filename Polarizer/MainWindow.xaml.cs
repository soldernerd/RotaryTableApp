using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Management;
using System.Text.RegularExpressions;

namespace RotaryTable
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DeviceWindow DeviceWin;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowClose(object sender, EventArgs e)
        {
            if (DeviceWin != null)
            {
                DeviceWin.Close();
            }
        }

        /*
        private void FreezeWindowSize(object sender, EventArgs e)
        {
            this.MinWidth = this.ActualWidth;
            this.MinHeight = this.ActualHeight;
            this.MaxWidth = this.ActualWidth;
            this.MaxHeight = this.ActualHeight;
        }
        */

        private void ResizeToContent(object sender, EventArgs e)
        {
            /*
            this.MinWidth = 0;
            this.MinHeight = 0;
            this.MaxWidth = 10000;
            this.MaxHeight = 10000;
            */
            this.ResizeMode = ResizeMode.CanResize;
            this.SizeToContent = SizeToContent.Manual;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.CanMinimize;
        }

        private void menu_view_connection(object sender, EventArgs e)
        {
            if (Communication_GroupBox.Visibility == Visibility.Visible)
            {
                Communication_GroupBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                Communication_GroupBox.Visibility = Visibility.Visible;
            }
            ResizeToContent(sender, e);
        }

        private void ExpandCollapseGroupBox(GroupBox box)
        {
            if (box.Visibility == Visibility.Visible)
            {
                box.Visibility = Visibility.Collapsed;
            }
            else
            {
                box.Visibility = Visibility.Visible;
            }
        }

        private void ExpandCollapse(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            switch (item.Name)
            {
                case "MenuItem_Status":
                    ExpandCollapseGroupBox(this.Status_GroupBox);
                    ExpandCollapseGroupBox(this.UserInterface_GroupBox);
                    break;
                case "MenuItem_MainControl":
                    ExpandCollapseGroupBox(this.MainControl_GroupBox);
                    break;
                case "MenuItem_ActivityLog":
                    ExpandCollapseGroupBox(this.ActivityLogging_GroupBox);
                    break;
                case "MenuItem_ConnectionDetail":
                    ExpandCollapseGroupBox(this.Communication_GroupBox);
                    break;
                
            }
            //Resize window
            ResizeToContent(sender, e);
        }


        private void SetZero(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            MessageBoxResult messageBoxResult;

            switch (item.Name)
            {
                case "MenuItem_CW":
                    messageBoxResult = System.Windows.MessageBox.Show("Set current position as new zero (approach clockwise)?", "Confirm", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        CommunicatorViewModel viewModel = this.DataContext as CommunicatorViewModel;
                        if (viewModel != null)
                        {
                            viewModel.SimpleReqest(Communicator.SimpleRequest.SetZeroPositionCW);
                        }
                    }
                    break;
                case "MenuItem_CCW":
                    messageBoxResult = System.Windows.MessageBox.Show("Set current position as new zero (approach counter-clockwise)?", "Confirm", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        CommunicatorViewModel viewModel = this.DataContext as CommunicatorViewModel;
                        if (viewModel != null)
                        {
                            viewModel.SimpleReqest(Communicator.SimpleRequest.SetZeroPositionCCW);
                        }
                    }
                    break;
            }
        }

        private void Restart(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            MessageBoxResult messageBoxResult;

            switch (item.Name)
            {
                case "MenuItem_Restart":
                    messageBoxResult = System.Windows.MessageBox.Show("Restart device in normal mode?", "Confirm", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        CommunicatorViewModel viewModel = this.DataContext as CommunicatorViewModel;
                        if (viewModel != null)
                        {
                            viewModel.SimpleReqest(Communicator.SimpleRequest.RebootNormalMode);
                        }
                    }
                    break;
                case "MenuItem_RestartBootloader":
                    messageBoxResult = System.Windows.MessageBox.Show("Restart device in bootloader mode?", "Confirm", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        CommunicatorViewModel viewModel = this.DataContext as CommunicatorViewModel;
                        if (viewModel != null)
                        {
                            viewModel.SimpleReqest(Communicator.SimpleRequest.RebootBootloaderMode);
                        }
                    }
                    break;
            }
        }

        // Update when focus is lost
        public void FocusLostHandler(object sender, EventArgs e)
        {
            try
            {
                TextBox tb = (TextBox)sender;
                tb.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
            catch
            {
                //nothin to do
            }
        }

        // Update if ENTER key has been pressed
        public void KeyUpHander(object sender, KeyEventArgs e)
        {
            try
            {
                TextBox tb = (TextBox)sender;
                if (e.Key == Key.Enter)
                {
                    tb.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                }
            }
            catch
            {
                //nothin to do
            }
        }

        //Scroll to bottom when text is changed
        public void ActivityLogTextChangedHandler(object sender, EventArgs e)
        {
            ActivityLogging_TextBox.ScrollToEnd();
            //ActivityLogScrollViewer.ScrollToBottom();
        }

        private void menu_window_about(object sender, EventArgs e)
        {
            MessageBox.Show("Lukas Fässler, 2020\n\nlfaessler@gmx.net\nVisit soldernerd.com for more information", "About Polarizer Controller");
        }

        private void menu_window_device(object sender, EventArgs e)
        {
            if (DeviceWin == null)
            {
                DeviceWin = new DeviceWindow(this);
            }
            DeviceWin.Show();
            DeviceWin.WindowState = WindowState.Normal;
            DeviceWin.Focus();
        }

        private void mouse_down(object sender, EventArgs e)
        {
            CommunicatorViewModel viewModel = this.DataContext as CommunicatorViewModel;
            if (viewModel != null)
            {
                Button clickedButton = sender as Button;
                if(clickedButton.Name== "ContinuousLeft_Button")
                {
                    viewModel.ContinuousLeft();
                }
                if (clickedButton.Name == "ContinuousRight_Button")
                {
                    viewModel.ContinuousRight();
                }
            }
        }

        private void mouse_up(object sender, EventArgs e)
        {
            CommunicatorViewModel viewModel = this.DataContext as CommunicatorViewModel;
            if (viewModel != null)
            {
                viewModel.SimpleReqest(Communicator.SimpleRequest.StopMotorManual);
            }
        }

    }
}
