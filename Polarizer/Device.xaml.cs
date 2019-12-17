using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RotaryTable
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class DeviceWindow : Window
    {
        MainWindow parentWindow;

        public DeviceWindow(MainWindow mainWin)
        {
            InitializeComponent();
            parentWindow = mainWin;
        }

        private void DeviceWindowClose(object sender, EventArgs e)
        {
            parentWindow.DeviceWin = null;
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
    }
}

