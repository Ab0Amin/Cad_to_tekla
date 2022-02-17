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
using System.Windows.Forms;
using System.Data;
using System.Collections;

namespace Cad_to_tekla
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ArrayList arrayList = new ArrayList();
       private readonly ViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel {

                dataGridItems = new List<DataGridItems>()
                {
                 
                }

            };
            this.DataContext = this.viewModel;
           
        }

        private void tb_addRow_Click(object sender, RoutedEventArgs e)
        {
           

        }

        private void dataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SelcetedRadioButtom_Checked(object sender, RoutedEventArgs e)
        {
            IEnumerable<DataGridItems> selected_items = viewModel.dataGridItems;



        }

      
        private void dt_data_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            IEnumerable<DataGridItems> selected_items = viewModel.dataGridItems;
           object d = dt_data.SelectedItem;

        }
    }
}
