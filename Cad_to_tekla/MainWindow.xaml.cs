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
using System.Data;
using System.Collections;
using Tekla.Structures;
using Tekla.Structures.Geometry3d;
using t3d = Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Solid;
using System.IO;
using System.Threading;

namespace Cad_to_tekla
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Model TeklaModel = new Model();

        #region Parameters
        string profile, material, symbol;
        #endregion

       private readonly ViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            cm_beamAtt.ItemsSource = GetAttributeFiles("*.prt");
            viewModel = new ViewModel {

                dataGridItems = new List<DataGridItems>()
                {
                //new DataGridItems
                //{
                //    BeamAtt = GetAttributeFiles("*.prt")
                //}
                }

            };
            this.DataContext = this.viewModel;
           
        }

        private void tb_addRow_Click(object sender, RoutedEventArgs e)
        {
           

        }

     

        private void SelcetedRadioButtom_Checked(object sender, RoutedEventArgs e)
        {
            IEnumerable<DataGridItems> selected_items = viewModel.dataGridItems;
            for (int i = 0; i < selected_items.Count(); i++)
            {
                DataGridItems element = selected_items.ElementAt(i);
                if (element.IsDefault ==true)
                {
                    //MessageBox.Show(element.TeklaProfiles);
                }
            }
            
            
          

        }

        private List<string> GetAttributeFiles(string fileExtn)
        {
            List<string> files = new List<string>();
            string modelPath = System.IO.Path.Combine(TeklaModel.GetInfo().ModelPath, "attributes");
            List<string> localAttributes = Directory.GetFiles(modelPath, fileExtn).ToList();

            string firmPath = "";
            TeklaStructuresSettings.GetAdvancedOption("XS_FIRM", ref firmPath);
            List<string> firmAttributes = new List<string>();
            if (Directory.Exists(firmPath))
            {
                firmAttributes = Directory.GetFiles(firmPath, fileExtn).ToList();
            }

            string projPath = "";
            TeklaStructuresSettings.GetAdvancedOption("XS_PROJECT", ref projPath);
            List<string> projAttributes = new List<string>();
            if (Directory.Exists(projPath))
            {
                projAttributes = Directory.GetFiles(projPath, fileExtn).ToList();
            }

            string envPath = "";
            TeklaStructuresSettings.GetAdvancedOption("XS_SYSTEM", ref envPath);
            List<string> envAttributes = new List<string>();
            if (envPath != "")
            {
                string[] envList = envPath.Split(';');
                foreach (string env in envList)
                {
                    if (Directory.Exists(env))
                        envAttributes.AddRange(Directory.GetFiles(env, fileExtn).ToList());
                }
            }


            foreach (string file in localAttributes)
            {
                if (System.IO.Path.GetExtension(file) == System.IO.Path.GetExtension(fileExtn))
                {
                    files.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }

            }
            foreach (string file in projAttributes)
            {
                if (System.IO.Path.GetExtension(file) == System.IO.Path.GetExtension(fileExtn))
                {
                    if (!files.Contains(System.IO.Path.GetFileNameWithoutExtension(file)))
                        files.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }
            foreach (string file in firmAttributes)
            {
                if (System.IO.Path.GetExtension(file) == System.IO.Path.GetExtension(fileExtn))
                {
                    if (!files.Contains(System.IO.Path.GetFileNameWithoutExtension(file)))
                        files.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }
            foreach (string file in envAttributes)
            {
                if (System.IO.Path.GetExtension(file) == System.IO.Path.GetExtension(fileExtn))
                {
                    if (!files.Contains(System.IO.Path.GetFileNameWithoutExtension(file)))
                        files.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }

            files.Sort();
            return files;
        }
  


    }
}
