using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace building_bms
{
    /// <summary>
    /// Interaction logic for extratime.xaml
    /// </summary>
    public partial class extratime : Window
    {
        public extratime()
        {
            InitializeComponent();
            etFrom.Text = Properties.Settings.Default.extraStartH.ToString();
            etTo.Text = Properties.Settings.Default.extraStopH.ToString();
        }

        private void extratime_save_Click(object sender, RoutedEventArgs e)
        {
            if (etFrom.Text != "" & etTo.Text != "" && Convert.ToInt16(etFrom.Text) <= Convert.ToInt16(etTo.Text))
            {
                Properties.Settings.Default.extraStartH = Convert.ToInt16(etFrom.Text);
                Properties.Settings.Default.extraStopH = Convert.ToInt16(etTo.Text);
                Properties.Settings.Default.extraTime = true;
                Properties.Settings.Default.extraTimeReset = false;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
                this.Close();
            } else
            {
                MessageBox.Show("بازه زمانی معتبر نیست!");
            }
        }
        public void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

    }
}
