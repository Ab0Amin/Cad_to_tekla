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
using Tekla.Structures.Dialog.UIControls;
using Tekla.Structures.Catalogs;
using System.ComponentModel;

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
		delegate void updatePresetage(string pres);

		BackgroundWorker worker = new BackgroundWorker();

		#region Parameters
		string attributePath,modelPath, profile, material, symbol ,referencePath , selectedAttribure, selectedAttriburePanel;
		int referenceRotaion = 0, allModelBeamsSize;
		ReferenceModel referenceModel;
		Picker input;
		t3d.Point referenceOrgin;
		CoordinateSystem coordinateSystem_xz, coordinateSystem_xy;
		double refenceScale, pro_counter = 0;
		t3d.Vector global_X, global_Y, global_Z;
		byte referenceDir_selectedIndix;
		List<string> cachedData;
	  static  bool continueInsertingBeams;
		Tekla.Structures.Model.UI.ModelObjectSelector modelObjectSelector;
		ArrayList insertedBeamArrayList;
		MacroBuilder macroBuilder;
		List<Beam> modifiedBeams, modifiedColumns, modifiedHZ, modifiedVL;

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
			
				// get cashed data to table
				cachedData = new List<string>();
				modelPath = TeklaModel.GetInfo().ModelPath;
				getCachedData(modelPath + "//cachedData.ibim");
				
				
				// get saved attributes for beams and panels
				attributePath = System.IO.Path.Combine(TeklaModel.GetInfo().ModelPath, "attributes");
				cm_beamAtt.ItemsSource = GetAttributeFiles("*.prt");
				cm_panelAtt.ItemsSource = GetAttributeFiles("*.cpn");
			
				cb_vl_hz.SelectedIndex = 0;

				


				// run progressbar
				worker.RunWorkerCompleted += worker_runworkerComplete;
				worker.WorkerReportsProgress = true;
				worker.DoWork += worker_DoWork;
				worker.ProgressChanged += worker_ProgressChanged;

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
				modifiedBeams = new List<Beam>();
				modifiedColumns = new List<Beam>();
				modifiedHZ = new List<Beam>();
				modifiedVL   = new List<Beam>();
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



        #region reference model insert

        //opem dialog to get reference model location
        private void tb_browesRef_Click(object sender, RoutedEventArgs e)
        {
            getRefrenceattributePath();

        }

		// get path after selecting file
		private void getRefrenceattributePath()
		{
			OpenFileDialog DWg_fileDialog = new OpenFileDialog();
			//DWg_fileDialog.Filter = "*.DWg";
			DWg_fileDialog.ShowDialog();
			DWg_fileDialog.RestoreDirectory = true;
			tx_refPath.Text = DWg_fileDialog.FileName;
		}
		// insert reference
		private void tb_ref_Click(object sender, RoutedEventArgs e)
		{
			referencePath = tx_refPath.Text;
			refenceScale = double.Parse(tx_refScale.Text);
			referenceDir_selectedIndix = (byte)cb_vl_hz.SelectedIndex;
			insertRefrenceModel(referencePath, referenceOrgin, referenceRotaion, refenceScale);
			TeklaModel.CommitChanges();

		}

		// force scale text to accept only numbers
		private void tx_refScale_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}
	
		// insert reference using pont and scale
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

		#endregion

		#region cashing data 
	
		private void cacheData(string filep)
		{
			try
			{
				if (cachedData != null)
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



        #endregion

        #region modify model
        // run modify model to move all object in center line of each other
        private void tb_modifyModel_Click(object sender, RoutedEventArgs e)
        {
            pro_modify.Visibility = Visibility.Visible;
            tx_progressbarPres.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();
        }

		// put all object in sepretaed phases after finishing modifing it
		private void putModifiedObbjectsInPhase()
		{
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

			foreach (Beam item in modifiedColumns)
			{
				item.SetPhase(columnPhase);
				item.Modify();
			}
			foreach (Beam item in modifiedBeams)
			{
				item.SetPhase(beamsPhase);
				item.Modify();
			}
			foreach (Beam item in modifiedHZ)
			{
				item.SetPhase(hzPhase);
				item.Modify();
			}
			foreach (Beam item in modifiedVL)
			{
				item.SetPhase(vlPhase);
				item.Modify();
			}
		}


		private int modifyCurrentPart(Beam CurrentPart, double tolerance)
		{
			Beam currentMainBeam, currentMColumn, currentBracing;
			List<Beam> columns;
			List<Beam> beams;
			List<Beam> hzBracing;
			List<Beam> vlBracing;
			ModelObjectEnumerator me2;
			t3d.Point p1, p2;
			Solid partsolid;
			int done = 0;





			partsolid = CurrentPart.GetSolid();
			p1 = new t3d.Point(partsolid.MinimumPoint.X - tolerance, partsolid.MinimumPoint.Y - tolerance, partsolid.MinimumPoint.Z - tolerance);
			p2 = new t3d.Point(partsolid.MaximumPoint.X + tolerance, partsolid.MaximumPoint.Y + tolerance, partsolid.MaximumPoint.Z + tolerance);

			me2 = TeklaModel.GetModelObjectSelector().GetObjectsByBoundingBox(p1, p2);
			categorizeMocelObject(me2, out columns, out beams, out hzBracing, out vlBracing);

			if (is_beam(CurrentPart))
			{
				for (int i = 0; i < beams.Count; i++)
				{
					currentMainBeam = beams[i];
					done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 0) + done;

				}

				if (done == 0 || done == 1)
				{
					for (int i = 0; i < columns.Count; i++)
					{
						currentMColumn = columns[i];
						done = modifyBeamToColumn(CurrentPart, currentMColumn, tolerance) + done;

					}
				}
				if (done == 0)
				{
					for (int i = 0; i < beams.Count; i++)
					{
						currentMainBeam = beams[i];
						done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 1) + done;

					}
				}
				if (done > 0)
				{
					modifiedBeams.Add(CurrentPart);
				}
			}
			else if (is_hzBracing(CurrentPart))
			{
				for (int i = 0; i < beams.Count; i++)
				{
					currentMainBeam = beams[i];
					done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 0) + done;

				}

				if (done == 0 || done == 1)
				{
					for (int i = 0; i < columns.Count; i++)
					{
						currentMColumn = columns[i];
						done = modifyBeamToColumn(CurrentPart, currentMColumn, tolerance) + done;

					}
				}
				if (done == 0 || done == 1)
				{
					for (int i = 0; i < beams.Count; i++)
					{
						currentMainBeam = beams[i];
						done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 1) + done;

					}
				}
				if (done > 0)
				{
					modifiedHZ.Add(CurrentPart);
				}
			}
			else if (is_column(CurrentPart))
			{
				for (int i = 0; i < beams.Count; i++)
				{
					currentMainBeam = beams[i];
					done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 0) + done;

				}
				if (done == 0 || done == 1)
				{
					for (int i = 0; i < hzBracing.Count; i++)
					{
						currentBracing = hzBracing[i];
						done = modifyBeamToBeam(CurrentPart, currentBracing, tolerance, 0) + done;

					}
				}

				if (done == 0 || done == 1)
				{
					for (int i = 0; i < beams.Count; i++)
					{
						currentMainBeam = beams[i];
						done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 1) + done;

					}
				}
				if (done > 0)
				{
					modifiedColumns.Add(CurrentPart);
				}
			}
			else if (is_vlBracing(CurrentPart))
			{
				for (int i = 0; i < beams.Count; i++)
				{
					currentMainBeam = beams[i];
					done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 0) + done;

				}

				if (done == 0 || done == 1)
				{

					for (int i = 0; i < columns.Count; i++)
					{
						currentMColumn = columns[i];
						done = modifyBeamToBeam(CurrentPart, currentMColumn, tolerance, 0) + done;
					}
				}
				if (done == 0 || done == 1)
				{
					for (int i = 0; i < beams.Count; i++)
					{
						currentMainBeam = beams[i];
						done = modifyBeamToBeam(CurrentPart, currentMainBeam, tolerance, 1) + done;

					}
				}
				if (done > 0)
				{
					modifiedVL.Add(CurrentPart);
				}
			}
			return done;
		}

		private int modifyBeamToBeam(Beam ModifiedBeam, Beam mainBeam, double tolerance, int num)
		{
			int modified = 0;
			int pointsWithSoild = 1;
			bool startModify = false;
			CoordinateSystem currentSecBeam_coordinateSystem = ModifiedBeam.GetCoordinateSystem();
			t3d.Vector X_secBeam = currentSecBeam_coordinateSystem.AxisX;
			X_secBeam.Normalize();
			t3d.Point startPoint_SecBeam = ModifiedBeam.StartPoint;
			t3d.Point EndPoint_SecBeam = ModifiedBeam.EndPoint;




			Solid mainBeamSoild = mainBeam.GetSolid();

			CoordinateSystem MainBeam_coordinateSystem = mainBeam.GetCoordinateSystem();
			GeometricPlane geometricPlane_mainBeam_1 = new GeometricPlane(MainBeam_coordinateSystem.Origin, MainBeam_coordinateSystem.AxisX, MainBeam_coordinateSystem.AxisY);
			GeometricPlane geometricPlane_mainBeam_2 = new GeometricPlane(MainBeam_coordinateSystem.Origin, MainBeam_coordinateSystem.AxisX, MainBeam_coordinateSystem.AxisY.Cross(MainBeam_coordinateSystem.AxisX));

			t3d.LineSegment beamLine = new t3d.LineSegment(startPoint_SecBeam - tolerance * X_secBeam, EndPoint_SecBeam + tolerance * X_secBeam);

			t3d.Point intersectedStarttPoint_1 = Intersection.LineSegmentToPlane(beamLine, geometricPlane_mainBeam_1);
			t3d.Point intersectedStarttPoint_2 = Intersection.LineSegmentToPlane(beamLine, geometricPlane_mainBeam_2);
			if (num == 0)
			{
				pointsWithSoild = mainBeamSoild.Intersect(beamLine).Count;
				startModify = true;
			}
			else
			{
				double height = 0;
				mainBeam.GetReportProperty("HEIGHT", ref height);

				ArrayList mainBeamPoints = mainBeam.GetCenterLine(false);

				t3d.Point startPoint_mainBeamn = mainBeamPoints[0] as t3d.Point;
				t3d.Point EndPoint_mainBeam = mainBeamPoints[1] as t3d.Point;
				double dis = Math.Abs(startPoint_mainBeamn.Z - startPoint_SecBeam.Z);
				if (dis < height)
				{
					pointsWithSoild = 1;
					startModify = true;
				}
			}


			if (startModify)
			{
				if (intersectedStarttPoint_1 != null && pointsWithSoild > 0)
				{
					//fitBeam(ModifiedBeam, MainBeam_coordinateSystem.Origin , MainBeam_coordinateSystem.AxisX, MainBeam_coordinateSystem.AxisY);
					if (Distance.PointToPoint(intersectedStarttPoint_1, startPoint_SecBeam) < tolerance)
					{
						ModifiedBeam.StartPoint = intersectedStarttPoint_1;
						ModifiedBeam.Modify();
						modified += 1;
						colorhBeam(mainBeam);
					}
					else if (Distance.PointToPoint(intersectedStarttPoint_1, EndPoint_SecBeam) < tolerance)
					{
						ModifiedBeam.EndPoint = intersectedStarttPoint_1;
						ModifiedBeam.Modify();
						modified += 1;
						colorhBeam(mainBeam);

					}
				}
				if (intersectedStarttPoint_2 != null && pointsWithSoild > 0)
				{
					if (Distance.PointToPoint(intersectedStarttPoint_2, startPoint_SecBeam) < tolerance)
					{
						ModifiedBeam.StartPoint = intersectedStarttPoint_2;
						ModifiedBeam.Modify();
						modified += 1;
						colorhBeam(mainBeam);

					}
					else if (Distance.PointToPoint(intersectedStarttPoint_2, EndPoint_SecBeam) < tolerance)
					{
						ModifiedBeam.EndPoint = intersectedStarttPoint_2;
						ModifiedBeam.Modify();
						modified += 1;
						colorhBeam(mainBeam);

					}
				}
			}

			return modified;

		}
		private int modifyBeamToColumn(Beam ModifiedBeam, Beam column, double tolerance)
		{
			int done = 0;
			CoordinateSystem currentSecBeam_coordinateSystem = ModifiedBeam.GetCoordinateSystem();
			t3d.Vector X_secBeam = currentSecBeam_coordinateSystem.AxisX;
			X_secBeam.Normalize();


			t3d.Point startPoint_SecBeam = ModifiedBeam.StartPoint;
			t3d.Point EndPoint_SecBeam = ModifiedBeam.EndPoint;


			ArrayList columnPoints = column.GetCenterLine(false);

			t3d.Point startPoint_column = columnPoints[0] as t3d.Point;
			t3d.Point EndPoint_column = columnPoints[1] as t3d.Point;

			t3d.Point newStartPoint = new t3d.Point(startPoint_column.X, startPoint_column.Y, startPoint_SecBeam.Z);

			//Solid mainBeamSoild = column.GetSolid();
			//t3d.LineSegment beamLine = new t3d.LineSegment(startPoint_SecBeam - tolerance * X_secBeam, EndPoint_SecBeam + tolerance * X_secBeam);


			if (startPoint_SecBeam.X - startPoint_column.X < 1 || startPoint_SecBeam.Y - startPoint_column.Y < 1)
			{
				if (startPoint_SecBeam.Z > startPoint_column.Z && startPoint_SecBeam.Z < EndPoint_column.Z + tolerance / 2)
				{

					double ST = Distance.PointToPoint(startPoint_SecBeam, newStartPoint);
					double ED = Distance.PointToPoint(EndPoint_SecBeam, newStartPoint);



					if (ST < ED && ST < tolerance)
					{

						t3d.Vector resultVector = new t3d.Vector(startPoint_SecBeam - newStartPoint);
						resultVector.Normalize();
						if (resultVector == X_secBeam || resultVector * -1 == X_secBeam)
						{
							ModifiedBeam.StartPoint = newStartPoint;
							ModifiedBeam.Modify();
							done += 1;
						}

					}
					else if (ED < tolerance)
					{

						t3d.Vector resultVector = new t3d.Vector(EndPoint_SecBeam - newStartPoint);
						resultVector.Normalize();
						if (resultVector == X_secBeam || resultVector * -1 == X_secBeam)
						{
							ModifiedBeam.EndPoint = newStartPoint;
							ModifiedBeam.Modify();
							done += 1;
						}

					}
				}

			}




			return done;

		}

		//private static t3d.Point BeamPlanCheck(t3d.Vector X_secBeam, double tolerance, t3d.LineSegment line, GeometricPlane geometricPlane_1, GeometricPlane geometricPlane_2)
		//{
		//    //t3d.Point projectedStarttPoint_1 = Projection.PointToPlane(Point_SecBeam, geometricPlane_1);

		//    //t3d.Point projectedStarttPoint_2 = Projection.PointToPlane(Point_SecBeam, geometricPlane_2);

		//    t3d.Point intersectedStarttPoint_1 =     Intersection.LineSegmentToPlane(line, geometricPlane_1);
		//    t3d.Point intersectedStarttPoint_2 =     Intersection.LineSegmentToPlane(line, geometricPlane_2);

		//    if (Distance.PointToPoint(Point_SecBeam, projectedStarttPoint_1) < tolerance)
		//    {

		//        t3d.Vector resultVector = new t3d.Vector(Point_SecBeam - projectedStarttPoint_1);
		//        resultVector.Normalize();
		//        if (resultVector == X_secBeam || resultVector * -1 == X_secBeam)
		//        {
		//            return projectedStarttPoint_1;
		//        }
		//        else return Point_SecBeam;

		//    }
		//    else if (Distance.PointToPoint(Point_SecBeam, projectedStarttPoint_2) < tolerance)
		//    {
		//        t3d.Vector resultVector = new t3d.Vector(Point_SecBeam - projectedStarttPoint_2);
		//        resultVector.Normalize();
		//        if (resultVector == X_secBeam || resultVector * -1 == X_secBeam)
		//        {
		//            return projectedStarttPoint_2;
		//        }
		//        else return Point_SecBeam;

		//    }
		//    else return Point_SecBeam;
		//}


		private static void categorizeMocelObject(ModelObjectEnumerator ModelBeams, out List<Beam> columns, out List<Beam> beams, out List<Beam> hzBracing, out List<Beam> vlBracing)
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

		private static bool is_column(Beam beam)
		{

			if (!beam.Profile.ProfileString.Contains("PL") && !beam.Profile.ProfileString.Contains("GRT"))
			{
				ArrayList centerPoints = beam.GetCenterLine(false);
				t3d.Point c1 = centerPoints[0] as t3d.Point;
				t3d.Point c2 = centerPoints[1] as t3d.Point;
				if (((int)c1.X == (int)c2.X) && ((int)c1.Y == (int)c2.Y) && (int)c1.Z != (int)c2.Z)
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
			if (!beam.Profile.ProfileString.Contains("PL") && !beam.Profile.ProfileString.Contains("GRT"))
			{
				ArrayList centerPoints = beam.GetCenterLine(false);
				t3d.Point c1 = centerPoints[0] as t3d.Point;
				t3d.Point c2 = centerPoints[1] as t3d.Point;
				if ((((int)c1.X == (int)c2.X) && ((int)c1.Y != (int)c2.Y) && (int)c1.Z == (int)c2.Z) ||
					(((int)c1.X != (int)c2.X) && ((int)c1.Y == (int)c2.Y) && (int)c1.Z == (int)c2.Z))
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
			if (!beam.Profile.ProfileString.Contains("PL") && !beam.Profile.ProfileString.Contains("GRT"))
			{
				ArrayList centerPoints = beam.GetCenterLine(false);
				t3d.Point c1 = centerPoints[0] as t3d.Point;
				t3d.Point c2 = centerPoints[1] as t3d.Point;
				if (((int)c1.X != (int)c2.X) && ((int)c1.Y != (int)c2.Y) && (int)c1.Z == (int)c2.Z)
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

			if (!beam.Profile.ProfileString.Contains("PL") && !beam.Profile.ProfileString.Contains("GRT"))
			{
				ArrayList centerPoints = beam.GetCenterLine(false);
				t3d.Point c1 = centerPoints[0] as t3d.Point;
				t3d.Point c2 = centerPoints[1] as t3d.Point;
				if ((((int)c1.X == (int)c2.X) && ((int)c1.Y != (int)c2.Y) && (int)c1.Z != (int)c2.Z)
					|| (((int)c1.X != (int)c2.X) && ((int)c1.Y == (int)c2.Y) && (int)c1.Z != (int)c2.Z))
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



		private void update_progressbar_presentage(string presentage)
		{
			tx_progressbarPres.Content = presentage;
		}


		private void worker_runworkerComplete(object sender, RunWorkerCompletedEventArgs e)
		{
			pro_modify.Value = 0;
			pro_modify.Visibility = Visibility.Hidden;
			tx_progressbarPres.Visibility = Visibility.Hidden;
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			pro_counter = 0;
			int i = 0;
			TeklaModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
			Beam CurrentPart;


			modifiedHZ.Clear();
			modifiedVL.Clear();
			modifiedColumns.Clear();
			modifiedBeams.Clear();
			ModelObjectEnumerator allModelBeams = TeklaModel.GetModelObjectSelector().GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);

			allModelBeamsSize = allModelBeams.GetSize();




			while (allModelBeams.MoveNext())
			{
				CurrentPart = allModelBeams.Current as Beam;
				if (CurrentPart != null)
				{
					modifyCurrentPart(CurrentPart, 500);
					TeklaModel.CommitChanges();
				}
				pro_counter++;

				if ((allModelBeamsSize / 100) * (i + 1) < pro_counter && i < 100)
				{
					i++;
				}
				(sender as BackgroundWorker).ReportProgress(i);

				try
				{
					Dispatcher.BeginInvoke(new updatePresetage(update_progressbar_presentage), new object[] { i.ToString() + "% " });

				}
				catch (Exception)
				{

				}
			}


			putModifiedObbjectsInPhase();

			TeklaModel.CommitChanges();
			System.Windows.Forms.MessageBox.Show("Done");


		}

		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			pro_modify.Value = e.ProgressPercentage;
		}



		#endregion

		#region insert beam 
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
		private void insertBeam()
		{
			try
			{
				while (continueInsertingBeams)
				{
					if (profile == null)
					{
						System.Windows.Forms.MessageBox.Show("Please select a valid Profile first");
						Dispatcher.BeginInvoke(new EnableButtoms(Disable_Enable_bottoms), new object[] { true });

						break;
					}
					else
					{
						if (TeklaStructures.Connect())
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
		private void Disable_Enable_bottoms(bool enable)
		{
			tb_PickLines.IsEnabled = enable;
			tb_pickProfile.IsEnabled = enable;
			tb_modifyModel.IsEnabled = enable;
			tb_ref.IsEnabled = enable;
			tb_PickLines_Panel.IsEnabled = enable;
		}

		private void cm_beamAtt_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			selectedAttribure = cm_beamAtt.SelectedItem.ToString();

		}

		private void SelcetedRadioButtom_Checked(object sender, RoutedEventArgs e)
		{
			IEnumerable<DataGridItems> selected_items = (List<DataGridItems>)dt_data.ItemsSource;
			for (int i = 0; i < selected_items.Count(); i++)
			{
				DataGridItems element = selected_items.ElementAt(i);
				if (element.IsDefault == true)
				{
					profile = element.TeklaProfiles;
					material = element.Material;
					symbol = element.Symbol;
					lb_currentMaterial.Content = material;
					lb_cuurentTeklaProfile.Content = profile;
					//MessageBox.Show(element.TeklaProfiles);
				}
			}




		}

		#endregion

		#region insert panel
		void pickingLinesThreadPanel()
        {
            continueInsertingBeams = true;
            try
            {
                Dispatcher.BeginInvoke(new EnableButtoms(Disable_Enable_bottoms), new object[] { false });
                inserPanel();
            }
            catch (Exception)
            {

            }
        }

		private void inserPanel()
		{
			try
			{
				while (continueInsertingBeams)
				{
					if (profile == null)
					{
						System.Windows.Forms.MessageBox.Show("Please select a valid panel thickness from table first");
						Dispatcher.BeginInvoke(new EnableButtoms(Disable_Enable_bottoms), new object[] { true });

						break;
					}
					else
					{
						if (TeklaStructures.Connect())
						{
							ArrayList line1 = input.PickLine("Pick first line");
							ArrayList line2 = input.PickLine("Pick secend line");

							t3d.Point _1_line1 = line1[0] as t3d.Point;
							t3d.Point _2_line1 = line1[1] as t3d.Point;

							t3d.Point _1_line2 = line2[0] as t3d.Point;
							t3d.Point _2_line2 = line2[1] as t3d.Point;


							t3d.Point panelStartPoint = getmidpoint(_1_line1, _2_line1);
							t3d.Point panelEndPoint = getmidpoint(_1_line2, _2_line2);

							double length = Distance.PointToPoint(_1_line1, _2_line1);

							Beam panel = new Beam(Beam.BeamTypeEnum.PANEL);

							string panelProfile = profile + "*" + length;
							if (panelStartPoint.Z - panelEndPoint.Z < 1)
							{
								panelProfile = length + "*" + profile;
							}
							panel.Profile.ProfileString = panelProfile;



							panel.Material.MaterialString = material;



							panel.StartPoint = panelStartPoint;
							panel.EndPoint = panelEndPoint;
							panel.Insert();

							insertedBeamArrayList.Add(panel);
							modelObjectSelector.Select(insertedBeamArrayList);

							macroBuilder = new MacroBuilder();


							macroBuilder.Callback("acmd_display_selected_object_dialog", "", "View_03 window_1");
							macroBuilder.ValueChange("diaConcretePanel", "get_menu", selectedAttriburePanel);
							macroBuilder.PushButton("attrib_get", "diaConcretePanel");
							macroBuilder.ValueChange("diaConcretePanel", "profile", panelProfile);
							macroBuilder.ValueChange("diaConcretePanel", "material", material);
							macroBuilder.ValueChange("diaConcretePanel", "position_depth", "0");
							macroBuilder.PushButton("dia_pa_modify", "diaConcretePanel");
							macroBuilder.PushButton("dia_pa_ok", "diaConcretePanel");
						}


						macroBuilder.Run();
						macroBuilder = null;
						insertedBeamArrayList.Clear();
						TeklaModel.CommitChanges();
					}

				}


			}
			catch (Exception)
			{
				Dispatcher.BeginInvoke(new EnableButtoms(Disable_Enable_bottoms), new object[] { true });


			}
		}

		private void tb_PickLines_Panel_Click(object sender, RoutedEventArgs e)
		{
			TeklaModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
			thread = new Thread(pickingLinesThreadPanel);
			thread.IsBackground = true;
			thread.Start();
		}

		private void cm_panelAtt_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			selectedAttriburePanel = cm_panelAtt.SelectedItem.ToString();

		}

		public t3d.Point getmidpoint(t3d.Point p1, t3d.Point p2)
		{
			double dis = t3d.Distance.PointToPoint(p1, p2);
			t3d.Vector vec = new t3d.Vector(p2 - p1); vec.Normalize();
			return p1 + 0.5 * dis * vec;
		}
		#endregion

		#region main window actions

		// cashing data before exit the application
		private void Window_Closed(object sender, EventArgs e)
		{
			cacheData(modelPath + "//cachedData.ibim");

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

		private void midgelMouseClick(object sender, RoutedEventArgs e)
		{
			continueInsertingBeams = false;
			if (thread.IsAlive)
			{
				thread.Interrupt();

			}
		}


		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!TeklaModel.GetConnectionStatus())
			{
				System.Windows.Forms.MessageBox.Show("Please Open Tekla model first");
				w_mainWindow.Close();
			}
		}


		private void Window_MouseDown(object sender, System.Windows.Input.MouseEventArgs e)
		{
			continueInsertingBeams = false;
			thread.Abort();

		}

		#endregion







		private void Button_Click(object sender, RoutedEventArgs e)
        {

            TeklaModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
			Beam CurrentPart = input.PickObject(Picker.PickObjectEnum.PICK_ONE_PART) as Beam;


            modifiedHZ.Clear();
            modifiedVL.Clear();
            modifiedColumns.Clear();
            modifiedBeams.Clear();
            //ModelObjectEnumerator allModelBeams = TeklaModel.GetModelObjectSelector().GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);
            //while (allModelBeams.MoveNext())
            {
                //CurrentPart = allModelBeams.Current as Beam;
                if (CurrentPart != null)
                {
                    modifyCurrentPart(CurrentPart,700);
                    TeklaModel.CommitChanges();
                }
            }

           
            putModifiedObbjectsInPhase();
        
            TeklaModel.CommitChanges();
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

     
		private static void colorhBeam(Beam bb)
		{

            //bb.Class = "0";
            //bb.Modify();
        }
		private static void fitBeam(Beam bb, t3d.Point p,t3d.Vector x ,t3d.Vector y )
		{
			Fitting fitting = new Fitting();
			Plane plane = new Plane();
			plane.Origin = p;
			plane.AxisX = x;
			plane.AxisY = y;
			fitting.Plane = plane;
			fitting.Father =bb;
			fitting.Insert();
		}



  

		private void Button_Click1(object sender, RoutedEventArgs e)
        {

			ArrayList line1 = input.PickLine("Pick first line");
			ArrayList line2 = input.PickLine("Pick secend line");

			Beam panel = new Beam(Beam.BeamTypeEnum.PANEL);


			panel.Profile.ProfileString = "C200*200";
			panel.Material.MaterialString = material;



			panel.StartPoint = line1[0] as t3d.Point;
			panel.EndPoint = line2[1] as t3d.Point;
			panel.Insert();

			//for (int i = 0; i < 100; i++)
			//{
			//	pro_modify.Value++;
			//	Thread.Sleep(100);
			//}
			//allModelBeamsSize = 920;
   //         pro_counter = 0;
   //         BackgroundWorker worker = new BackgroundWorker();
   //         worker.RunWorkerCompleted += worker_runworkerComplete;
   //         worker.WorkerReportsProgress = true;
   //         //worker.DoWork += worker_DoWork;
   //         worker.ProgressChanged += worker_ProgressChanged;
   //         worker.RunWorkerAsync();

         
        }

    

      
		
	}
}
