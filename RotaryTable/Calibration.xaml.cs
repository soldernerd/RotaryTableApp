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
    public partial class CalibrationWindow : Window
    {
        MainWindow parentWindow;

        public CalibrationWindow(MainWindow mainWin)
        {
            InitializeComponent();
            parentWindow = mainWin;
            this.MaxHeight = SystemParameters.VirtualScreenHeight-50;
        }

        private void CalibrationWindowClose(object sender, EventArgs e)
        {
            parentWindow.CalibrationWin = null;
        }

        private void OnFocusLost(object sender, EventArgs e)
        {
            TextBox tb = (TextBox) sender;
            tb.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

    }
}

