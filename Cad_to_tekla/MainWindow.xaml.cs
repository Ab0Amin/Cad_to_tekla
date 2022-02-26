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
using td= Tekla.Structures.Drawing;
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
using Microsoft.Win32.SafeHandles;
using Render;
using System.Runtime.InteropServices;


namespace Cad_to_tekla
{
   

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        //public static extern IntPtr FindWindow(string lpClassName,
        //  string lpWindowName);

        //// Activate an application window.
        //[DllImport("USER32.DLL")]
        //public static extern bool SetForegroundWindow(IntPtr hWnd);

        Model TeklaModel = new Model();
        delegate void EnableButtoms(bool enable);

        #region Parameters
        string attributePath,modelPath, profile, material, symbol ,referencePath , selectedAttribure;
        int referenceRotaion = 0;
        ReferenceModel referenceModel;
        Picker input;
        t3d.Point referenceOrgin;
        CoordinateSystem coordinateSystem_xz, coordinateSystem_xy;
        double refenceScale;
        t3d.Vector global_X, global_Y, global_Z;
        byte referenceDir_selectedIndix;
        List<string> cachedData;
      static  bool continueInsertingBeams;
        Tekla.Structures.Model.UI.ModelObjectSelector modelObjectSelector;
        ArrayList insertedBeamArrayList;
        MacroBuilder macroBuilder;
       

        //public event EventHandler MiddleClick;
        Thread thread;
        #endregion

        private readonly ViewModel viewModel;

        public MainWindow()
        {
            if (!TeklaModel.GetConnectionStatus())
            { }
                  
            else
            {
                InitializeComponent();
                cachedData = new List<string>();
                attributePath = System.IO.Path.Combine(TeklaModel.GetInfo().ModelPath, "attributes");
                modelPath = TeklaModel.GetInfo().ModelPath;
                getCachedData(modelPath + "//cachedData.ibim");

                cm_beamAtt.ItemsSource = GetAttributeFiles("*.prt");
                cb_vl_hz.SelectedIndex = 0;

                #region generate new objects

                thread = new Thread(pickingLinesThread);

                input = new Picker();
                referenceModel = new ReferenceModel();
                coordinateSystem_xz = new CoordinateSystem();
                coordinateSystem_xy = new CoordinateSystem();

                global_X = new t3d.Vector(1, 0, 0);
                global_Y = new t3d.Vector(0, 1, 0);
                global_Z = new t3d.Vector(0, 0, 1);

                modelObjectSelector = new Tekla.Structures.Model.UI.ModelObjectSelector();
                insertedBeamArrayList = new ArrayList();

                viewModel = new ViewModel
                {

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
                IMainWindow window = TeklaStructures.MainWindow;

                Loaded += delegate { Mouse.AddMouseDownHandler(this, new System.Windows.Input.MouseButtonEventHandler(midgelMouseClick)); };

            }

        }
      

       

        private void tb_browesRef_Click(object sender, RoutedEventArgs e)
        {
            getRefrenceattributePath();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            cacheData(modelPath + "//cachedData.ibim");

        }

      

        private void tb_modifyModel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void tb_PickLines_Click(object sender, RoutedEventArgs e)
        {
            thread = new Thread(pickingLinesThread);
            thread.IsBackground = true;
            thread.Start();
            //try
            //{
            //    thread.Start();
            //}
            //catch (Exception)
            //{
               

            //}


        }
        void pickingLinesThread()
        {
            continueInsertingBeams = true;
            try
            {
                Dispatcher.BeginInvoke(new EnableButtoms(Disable_Enable_bottoms), new object[] { false });
                insertBeam();
            }
            catch (Exception)
            {

            }
        }
        private void Disable_Enable_bottoms(bool enable)
        {
            tb_PickLines.IsEnabled = enable;
            tb_pickProfile.IsEnabled = enable;
            tb_modifyModel.IsEnabled = enable;
            tb_ref.IsEnabled = enable;
        }
        private  void insertBeam()
        {
            try
            {
                while (continueInsertingBeams)
                {
                    if (profile == null )
                    {
                        System.Windows.Forms.MessageBox.Show("Please select a valid Profile first");
                        Dispatcher.BeginInvoke(new EnableButtoms(Disable_Enable_bottoms), new object[] { true });

                        break;
                    }
                    else
                    {
                        if ( TeklaStructures.Connect())
                        {
                            ArrayList line = input.PickLine();
                            Beam beam = new Beam(Beam.BeamTypeEnum.BEAM);
                        

                            beam.Profile.ProfileString = profile;
                            beam.Material.MaterialString = material;
                            beam.StartPoint = line[0] as t3d.Point;
                            beam.EndPoint = line[1] as t3d.Point;
                            beam.Insert();
                           
                            insertedBeamArrayList.Add(beam);
                            modelObjectSelector.Select(insertedBeamArrayList);

                            macroBuilder = new MacroBuilder();
                            macroBuilder.Callback("acmd_display_selected_object_dialog", "", "View_01 window_1");
                            macroBuilder.ValueChange("part_attrib", "get_menu", selectedAttribure);
                            macroBuilder.PushButton("attrib_get", "part_attrib");
                            macroBuilder.ValueChange("part_attrib", "profile", profile);
                            macroBuilder.ValueChange("part_attrib", "material", material);
                            macroBuilder.PushButton("dia_pa_modify", "part_attrib");
                            macroBuilder.PushButton("dia_pa_cancel", "part_attrib");
                            macroBuilder.Run();
                            macroBuilder = null;
                            insertedBeamArrayList.Clear();
                            TeklaModel.CommitChanges(); 
                        }
                        
                    }
                   
                }
            }
           catch (Exception)
            {
                Dispatcher.BeginInvoke(new EnableButtoms(Disable_Enable_bottoms), new object[] { true });


            }
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseEventArgs e)
        {
            continueInsertingBeams = false;
            thread.Abort();
            
        }

        private void tb_ref_Click(object sender, RoutedEventArgs e)
        {
            referencePath = tx_refPath.Text;
            refenceScale = double.Parse(tx_refScale.Text);
            referenceDir_selectedIndix = (byte)cb_vl_hz.SelectedIndex;
            insertRefrenceModel( referencePath,  referenceOrgin, referenceRotaion, refenceScale);
            TeklaModel.CommitChanges();

        }

     

        private void cm_beamAtt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedAttribure = cm_beamAtt.SelectedItem.ToString();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!TeklaModel.GetConnectionStatus())
            {
                System.Windows.Forms.MessageBox.Show("Please Open Tekla model first");
                w_mainWindow.Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            List<Beam> columns;
            List<Beam> beams;
            List<Beam> hzBracing; 
            List<Beam> vlBracing;



            ModelObjectEnumerator allModelBeams = TeklaModel.GetModelObjectSelector().GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);

            //categorizeMocelObject( allModelBeams,out columns, out beams, out hzBracing, out vlBracing);

            Part p = input.PickObject(Picker.PickObjectEnum.PICK_ONE_PART) as Part;
            //int x= p.Identifier.ID;

            double tolerance = 500;
            Solid partsolid = p.GetSolid();
            t3d.Point p1 = new t3d.Point(partsolid.MinimumPoint.X - tolerance, partsolid.MinimumPoint.Y - tolerance, partsolid.MinimumPoint.Z - tolerance);
            t3d.Point p2 = new t3d.Point(partsolid.MaximumPoint.X + tolerance, partsolid.MaximumPoint.Y + tolerance, partsolid.MaximumPoint.Z + tolerance);



            ModelObjectEnumerator me2 = TeklaModel.GetModelObjectSelector().GetObjectsByBoundingBox(p1, p2);
            categorizeMocelObject(me2, out columns, out beams, out hzBracing, out vlBracing);


            //while (me2.MoveNext())
            //{
            //    Beam b = me2.Current as Beam;
            //    if (b != null)
            //    {

            //        insertedBeamArrayList.Add(b);

            //    }


            //}
            //modelObjectSelector.Select(insertedBeamArrayList);
            //insertedBeamArrayList.Clear();
            Phase columnPhase = new Phase();
            columnPhase.PhaseName = "columns";
            columnPhase.PhaseNumber = 2000001;
            columnPhase.Insert();


            Phase beamsPhase = new Phase();
            beamsPhase.PhaseName = "beams";
            beamsPhase.PhaseNumber = 2000002;

            beamsPhase.Insert();

            Phase hzPhase = new Phase();
            hzPhase.PhaseName = "HZ";
            hzPhase.PhaseNumber = 2000003;

            hzPhase.Insert();


            Phase vlPhase = new Phase();
            vlPhase.PhaseName = "VL";
            vlPhase.PhaseNumber = 2000004;
            vlPhase.Insert();

            foreach (Beam item in columns)
            {
                item.SetPhase(columnPhase);
                item.Modify();

            }
            foreach (Beam item in beams)
            {
                item.SetPhase(beamsPhase);
                item.Modify();


            }
            foreach (Beam item in hzBracing)
            {
                item.SetPhase(hzPhase);
                item.Modify();


            }
            foreach (Beam item in vlBracing)
            {
                item.SetPhase(vlPhase);
                item.Modify();


            }
            TeklaModel.CommitChanges();
        }

        private static void categorizeMocelObject(ModelObjectEnumerator ModelBeams,out List<Beam> columns, out List<Beam> beams, out List<Beam> hzBracing, out List<Beam> vlBracing)
        {
         columns = new List<Beam>();
         beams = new List<Beam>();
            hzBracing = new List<Beam>();
           vlBracing = new List<Beam>();

            while (ModelBeams.MoveNext())
            {
                Beam current = ModelBeams.Current as Beam;
                if (current != null)
                {
                    if (is_column(current))
                    {
                        columns.Add(current);
                    }
                    else if (is_beam(current))
                    {
                        beams.Add(current);
                    }
                    else if (is_hzBracing(current))
                    {
                        hzBracing.Add(current);
                    }
                    else if (is_vlBracing(current))
                    {
                        vlBracing.Add(current);
                    }



                }
            }
            //columns1 = columns;
            //beams1 = beams;
            //hzBracing1 = hzBracing;
            //vlBracing1 = vlBracing;
        }

        private static bool is_column( Beam beam)
        {
            
            if (!beam.Profile.ProfileString.Contains("PL"))
            {
                ArrayList centerPoints = beam.GetCenterLine(false);
                t3d.Point c1 = centerPoints[0] as t3d.Point;
                t3d.Point c2 = centerPoints[1] as t3d.Point;
                if ((c1.X == c2.X) && (c1.Y == c2.Y) && c1.Z != c2.Z)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                centerPoints = null;
                c1 = null;
                c2 = null;
            }
            else
            {
                return false;
            }
           

        }


        private static bool is_beam(Beam beam)
        {

            if (!beam.Profile.ProfileString.Contains("PL"))
            {
                ArrayList centerPoints = beam.GetCenterLine(false);
                t3d.Point c1 = centerPoints[0] as t3d.Point;
                t3d.Point c2 = centerPoints[1] as t3d.Point;
                if (((c1.X == c2.X) && (c1.Y != c2.Y) && c1.Z == c2.Z)|| ((c1.X != c2.X) && (c1.Y == c2.Y) && c1.Z == c2.Z))
                {
                    return true;
                }
                else
                {
                    return false;
                }
                centerPoints = null;
                c1 = null;
                c2 = null;
            }
            else
            {
                return false;
            }
        }

        private static bool is_hzBracing(Beam beam)
        {

            if (!beam.Profile.ProfileString.Contains("PL"))
            {
                ArrayList centerPoints = beam.GetCenterLine(false);
                t3d.Point c1 = centerPoints[0] as t3d.Point;
                t3d.Point c2 = centerPoints[1] as t3d.Point;
                if ((c1.X != c2.X) && (c1.Y != c2.Y) && c1.Z == c2.Z) 
                {
                    return true;
                }
                else
                {
                    return false;
                }
                centerPoints = null;
                c1 = null;
                c2 = null;
            }
            else
            {
                return false;
            }
        }

        private static bool is_vlBracing(Beam beam)
        {

            if (!beam.Profile.ProfileString.Contains("PL"))
            {
                ArrayList centerPoints = beam.GetCenterLine(false);
                t3d.Point c1 = centerPoints[0] as t3d.Point;
                t3d.Point c2 = centerPoints[1] as t3d.Point;
                if (((c1.X == c2.X) && (c1.Y != c2.Y) && c1.Z != c2.Z) || ((c1.X != c2.X) && (c1.Y == c2.Y) && c1.Z != c2.Z))
                {
                    return true;
                }
                else
                {
                    return false;
                }
                centerPoints = null;
                c1 = null;
                c2 = null;
            }
            else
            {
                return false;
            }
        }

        private static void checkwithBeam(t3d.Point p1, t3d.Point p2)
        {
            Beam bb = new Beam();
            bb.Profile.ProfileString = "UB406*140*46";
            bb.Class = "3";
            bb.StartPoint = p1;
            bb.EndPoint = p2;
            bb.Insert();
        }

        private void SelcetedRadioButtom_Checked(object sender, RoutedEventArgs e)
        {
            IEnumerable<DataGridItems> selected_items = (List<DataGridItems>)dt_data.ItemsSource;
            for (int i = 0; i < selected_items.Count(); i++)
            {
                DataGridItems element = selected_items.ElementAt(i);
                if (element.IsDefault ==true)
                {
                    profile = element.TeklaProfiles;
                    material = element.Material;
                    symbol = element.Symbol;
                    //MessageBox.Show(element.TeklaProfiles);
                }
            }
            
            
          

        }

        private void tx_refScale_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private ReferenceModel insertRefrenceModel(string _referencePath, t3d.Point _referenceOrgin, int _referenceRotaion, double _refenceScale)
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
                getRefrenceattributePath();

            }
            return referenceModel;
        }

        private List<string> GetAttributeFiles(string fileExtn)
        {
            List<string> files = new List<string>();
            List<string> localAttributes = Directory.GetFiles(attributePath, fileExtn).ToList();

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

        private void getRefrenceattributePath()
        {
            OpenFileDialog DWg_fileDialog = new OpenFileDialog();
            //DWg_fileDialog.Filter = "*.DWg";
            DWg_fileDialog.ShowDialog();
            DWg_fileDialog.RestoreDirectory = true;
            tx_refPath.Text = DWg_fileDialog.FileName;
        }

        private void cacheData(string filep)
        {
            try
            {
                if (cachedData !=null)
                {
                    cachedData.Clear();
                }
                if (File.Exists(filep))
                {

                }
                else
                {
                    FileStream fileStream = File.Create(filep);
                    fileStream.Close();
                }
                List<DataGridItems> data = (List<DataGridItems>)dt_data.ItemsSource;
                for (int i = 0; i < data.Count(); i++)
                {
                    DataGridItems element = data.ElementAt(i);
                    cachedData.Add(element.Symbol + "," + element.TeklaProfiles + "," + element.Material);
                }
                File.WriteAllLines(filep, cachedData.ToArray());
            }
            catch (Exception)
            {

            }

        }
        private void getCachedData(string filep)
        {
            cachedData.Clear();
            if (File.Exists(filep))
            {
                string[] loadedData;
                List<DataGridItems> gridItems = new List<DataGridItems>();

                cachedData = File.ReadAllLines(filep).ToList<string>();
                for (int i = 0; i < cachedData.Count; i++)
                {
                    string current = cachedData[i];
                    loadedData = current.Split(',');

                    DataGridItems item = new DataGridItems
                    {
                        TeklaProfiles = loadedData[1],
                        Symbol = loadedData[0],
                        Material = loadedData[2]
                    };
                    gridItems.Add(item);

                }



                dt_data.ItemsSource = gridItems;

            }
        }


        private void midgelMouseClick(object sender, RoutedEventArgs e)
        {
            continueInsertingBeams = false;
            if (thread.IsAlive)
            {
                thread.Interrupt();

            }
        }
        
    }
}
