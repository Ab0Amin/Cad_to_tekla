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
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Runtime.Caching;

namespace Cad_to_tekla
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Model TeklaModel = new Model();

        #region Parameters
        string profile, material, symbol ,referencePath;
        int referenceRotaion = 0;
        ReferenceModel referenceModel;
        Picker input;
        t3d.Point referenceOrgin;
        CoordinateSystem coordinateSystem_xz, coordinateSystem_xy;
        double refenceScale;
        t3d.Vector global_X, global_Y, global_Z;
        byte referenceDir_selectedIndix;
        #endregion

        private readonly ViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
           
            
            cm_beamAtt.ItemsSource = GetAttributeFiles("*.prt");
            #region generate new objects


            input = new Picker();
            referenceModel = new ReferenceModel();
            coordinateSystem_xz = new CoordinateSystem();
            coordinateSystem_xy = new CoordinateSystem();
            referencePath = tx_refPath.Text;
            refenceScale = double.Parse(tx_refScale.Text);
            referenceDir_selectedIndix = (byte)cb_vl_hz.SelectedIndex;
            global_X =  new t3d.Vector(1, 0, 0);
            global_Y=  new t3d.Vector(0, 1, 0);
            global_Z=  new t3d.Vector(0, 0, 1);
            viewModel = new ViewModel {

                dataGridItems = new List<DataGridItems>()
                {
                //new DataGridItems
                //{
                //    BeamAtt = GetAttributeFiles("*.prt")
                //}
                }

            };
            #endregion
            this.DataContext = this.viewModel;
           
        }

        private void tb_browesRef_Click(object sender, RoutedEventArgs e)
        {
            getRefrenceModelPath();

        }

    

        private void tb_ref_Click(object sender, RoutedEventArgs e)
        {
            insertRefrenceModel( referencePath,  referenceOrgin, referenceRotaion, refenceScale);
            TeklaModel.CommitChanges();

        }

        private ReferenceModel insertRefrenceModel(string _referencePath,t3d.Point _referenceOrgin,  int _referenceRotaion , double _refenceScale)
        {
            _referenceRotaion = 0;
            _referenceOrgin = input.PickPoint();
            if (_referencePath != "")
            {
                referenceModel.Filename = _referencePath;
                referenceModel.Position = _referenceOrgin;
                referenceModel.Rotation = _referenceRotaion;
                referenceModel.Scale = _refenceScale;
                referenceModel.Insert();

                coordinateSystem_xy.AxisX = global_X;
                coordinateSystem_xy.AxisY = global_Y;


                if (referenceDir_selectedIndix != 0)
                {

                    coordinateSystem_xz.AxisX = global_X;
                    coordinateSystem_xz.AxisY = global_Z;
                    Operation.MoveObject(referenceModel, coordinateSystem_xy, coordinateSystem_xz);
                }
                if (referenceDir_selectedIndix == 2)
                {
                    _referenceRotaion = 90;
                }
                if (cb_flip.IsChecked == true)
                {
                    _referenceRotaion += 180;
                }
                referenceModel.Rotation = _referenceRotaion;
                referenceModel.Position = _referenceOrgin;
                referenceModel.Modify();


            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select a valid Path", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                getRefrenceModelPath();

            }
            return referenceModel;
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

        private void tx_refScale_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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

        private void getRefrenceModelPath()
        {
            OpenFileDialog DWg_fileDialog = new OpenFileDialog();
            //DWg_fileDialog.Filter = "*.DWg";
            DWg_fileDialog.ShowDialog();
            DWg_fileDialog.RestoreDirectory = true;
            tx_refPath.Text = DWg_fileDialog.FileName;
        }

    }
}
