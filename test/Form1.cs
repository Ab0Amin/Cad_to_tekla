using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using ACadSharp.Entities;
//using ACadSharp;
//using ACadSharp.IO;
//using ACadSharp.IO.DWG;
//using ACadSharp.IO.DXF;
using System.Diagnostics;
using System.IO;
using Tekla.Structures;
using Tekla.Structures.Geometry3d;
using t3d = Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Collaboration;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Model.UI;
using m=Tekla.Structures.Model.UI;
using Tekla.Structures.Solid;
using d=Tekla.Structures.Drawing.UI;
using Tekla.Structures.Drawing.UI;
using Tekla.Structures.Drawing;
//using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

using IronOcr;

namespace test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
		Model model = new Model();

		//static string PathSamples = "../../../../samples/local";
		string PathSamples = "C:\\Users\\amin\\Downloads";

		private void button1_Click(object sender, EventArgs e)
        {
			//if (TeklaStructures.Connect())
			//{
			//	Render.Color color = new Render.Color(1, 1, 1, 1);
			//	Render.Text text = new Render.Text("ahmed", 500, 50000.0, new Render.Point(0, 0, 0));

			//}
				GraphicsDrawer drawer = new GraphicsDrawer();

				drawer.DrawText(new t3d.Point(0.0, 1000.0, 1000.0), "TEXT SAMPLE", new Tekla.Structures.Model.UI.Color(1.0, 0.5, 0.0));
				drawer.DrawLineSegment(new t3d.Point(0.0, 0.0, 0.0), new t3d.Point(1000.0, 1000.0, 1000.0), new Tekla.Structures.Model.UI.Color(1.0, 0.0, 0.0));

				Mesh mesh = new Mesh();
				mesh.AddPoint(new t3d.Point(0.0, 0.0, 0.0));
				mesh.AddPoint(new t3d.Point(1000.0, 0.0, 0.0));
				mesh.AddPoint(new t3d.Point(1000.0, 1000.0, 0.0));
				mesh.AddPoint(new t3d.Point(0.0, 1000.0, 0.0));
				mesh.AddTriangle(0, 1, 2);
				mesh.AddTriangle(0, 2, 3);
				mesh.AddLine(0, 1); mesh.AddLine(1, 2); mesh.AddLine(2, 3); mesh.AddLine(3, 1);

				drawer.DrawMeshSurface(mesh, new Tekla.Structures.Model.UI.Color(1.0, 0.0, 0.0, 0.5));
				drawer.DrawMeshLines(mesh, new Tekla.Structures.Model.UI.Color(0.0, 0.0, 1.0));

				//model.CommitChanges();
				////CadDocument r = ReadDxf();
				//Picker picker = new Picker();
				////object b = picker.PickFace();
				//ReferenceModelObject b1 = picker.PickObject(Picker.PickObjectEnum.PICK_ONE_OBJECT) as ReferenceModelObject;
				//ReferenceModelObjectAttribute b2 =(ReferenceModelObjectAttribute) picker.PickObject(Picker.PickObjectEnum.PICK_ONE_OBJECT) as ReferenceModelObjectAttribute;


				//ModelObjectEnumerator mo =	b1.GetChildren();

				//while (mo.MoveNext())
				//{
				//	object r = mo.Current;
				//}



				////ReferenceModelObject referenceModelObject = new ReferenceModelObject()
				//Tekla.Structures.Model.Collaboration.ReferenceModelObjectAttributeEnumerator re = new ReferenceModelObjectAttributeEnumerator(b1);
				//ReferenceModelObjectAttribute referenceModelObjectAttribute 

				//while (re.MoveNext())
				//         {
				//	object r = re.Current;
				//         }

			}

        private void button2_Click(object sender, EventArgs e)
        {
            m.Picker picker1 = new m.Picker();
            //object b = picker.PickFace();
           //object  b1 = picker1.PickObject(m.Picker.PickObjectEnum.PICK_ONE_OBJECT) ;
           // DBText f =(DBText) b1;
            //d.Picker picker = new d.Picker(b1);
            //object b = picker.PickObject("nb");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //string path = textBox1.Text;

            //loadDataFromImage(path);

            //using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            //{
            //    using (var img = Pix.LoadFromFile(path))
            //    {
            //        using (Tesseract.Page page = engine.Process(img))
            //        {
            //            ResultIterator res = page.GetIterator();

            //            res.Begin();

            //            while (res.Next(PageIteratorLevel.TextLine))
            //            {
            //                data_t.Add(res.GetText(PageIteratorLevel.TextLine));

            //            }

                        //var text = page.GetText();


                    }
            //    }
            //}
         




            //var Ocr = new IronTesseract();
            //using (var Input = new OcrInput(path))
            //{
            //    //Input.Deskew();
            //    //Input.DeNoise(); // only use if accuracy <97%
            //    var Result = Ocr.Read(Input);
            //    label1.Text = Result.Text;
            //    //OcrResult.Word [] data = Result.Words;
            //    //OcrResult.Line [] lines = Result.Lines;
               
            //    OcrResult.Block[] blocks = Result.Blocks;
            //    OcrResult.Paragraph[] paragraphs = Result.Paragraphs;
             
                
            //    int numberOfBlocks = blocks.Count();
            //    int numberOfParagraphs = paragraphs.Count();
            //    int numberOfTable_column=0;
            //    int numberOfTable_row=0;

            //    List<int> noOfLines = new List<int>();

            //    if (numberOfBlocks == numberOfParagraphs)
            //    {

            //        numberOfTable_column = numberOfBlocks;
            //        numberOfTable_row = blocks[0].Lines.Count();
            //    }
            //    else
            //    {
            //        for (int i = 0; i < blocks.Count(); i++)
            //        {
            //            noOfLines.Add(blocks[i].Lines.Count());
            //        }
            //        for (int i = 0; i < paragraphs.Count(); i++)
            //        {
            //            noOfLines.Add(paragraphs[i].Lines.Count());
            //        }
                
            //        var most = (from i in noOfLines
            //                    group i by i into grp
            //                    orderby grp.Count() descending
            //                    select grp.Key).First();

            //        numberOfTable_row = most;

            //    }




        //    }
        //}
        // CadDocument ReadDxf()
        //{
        //	string file = Path.Combine(PathSamples, "21-S-013-DWG-403-1.dxf");
        //	DxfReader reader = new DxfReader(file, onNotification);
        //	CadDocument doc = reader.Read();
        //	return doc;
        //}

        // void ReadDwg()
        //{
        //	//string file = Path.Combine(PathSamples, "dwg/cad_v2013.dwg");
        //	//using (DwgReader reader = new DwgReader(file, onNotification))
        //	//{
        //	//	CadDocument doc = reader.Read();
        //	//}

        //	string[] files = Directory.GetFiles(PathSamples + "/dwg/", "*.dwg");

        //	foreach (var f in files)
        //	{
        //		using (DwgReader reader = new DwgReader(f, onNotification))
        //		{
        //			//CadDocument 
        //				_ = reader.Read();
        //		}

        //		Console.WriteLine($"file read : {f}");
        //		Console.ReadLine();
        //	}
        //}

        //private static void onNotificationFail(object sender, NotificationEventArgs e)
        //{
        //	Debug.Fail(e.Message);
        //}

        //private static void onNotification(object sender, NotificationEventArgs e)
        //{
        //	Console.WriteLine(e.Message);
        //}
    }
}
