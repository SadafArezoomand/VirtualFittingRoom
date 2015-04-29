using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Reflection;
using System.Windows.Media.Animation;

namespace VirtualFittingRoomTest2
{
    public enum DressTypes { None = -2, Naked = -1, Floral24 = 0, Floral27 = 1, Floral30 = 2, Floral33 = 3, Floral79 = 4 }
    public sealed class DressModel : Grid
    {
        public DressTypes DressType;
        public ResourceDictionary DressResource;
        public PerspectiveCamera DressCamera;
        private GeometryModel3D DressGeometry;
        private Model3DGroup DressTransform;
        private Model3DGroup DressScene;
        public ModelVisual3D DressVisual;
        public Viewport3D DressViewport;
        public double DressSizeRatio;
        public double ScaleFactor = 1.4f;

        public DressModel(string XAMLFile)
        {
            DressType = DressTypes.Floral24;
            DressResource = new ResourceDictionary();
            DressResource.Source = new Uri(XAMLFile, UriKind.RelativeOrAbsolute);
            DressGeometry = new GeometryModel3D();
            DressCamera = new PerspectiveCamera();
            DressTransform = new Model3DGroup();
            DressViewport = new Viewport3D();   
            DressScene = new Model3DGroup();
            DressVisual = new ModelVisual3D();
            this.Width = 151;
            this.Height = 270;
            this.DressSizeRatio = (this.Height / this.Width);
        }
        public void CreateModel(string GeometryID, string MaterialID, string TransformID, string SceneID)
        {
            DressGeometry.Geometry = (MeshGeometry3D)DressResource[GeometryID];
            DressGeometry.Material = (MaterialGroup)DressResource[MaterialID];
            DressGeometry.BackMaterial = (MaterialGroup)DressResource[MaterialID];
            DressTransform.Transform = (Transform3D)DressResource[TransformID];
            DressTransform.Children.Add(DressGeometry);
            DressScene.Transform = (Transform3D)DressResource[SceneID]; ;
            DressScene.Children.Add(new AmbientLight((Color)ColorConverter.ConvertFromString("#111111")));
            DressScene.Children.Add(new DirectionalLight((Color)ColorConverter.ConvertFromString("#A4A4A4"), new Vector3D(-0.612372, -0.5, -0.612372)));
            DressScene.Children.Add(new DirectionalLight((Color)ColorConverter.ConvertFromString("#A4A4A4"), new Vector3D(0.612372, -0.5, -0.612372)));
            DressScene.Children.Add(DressTransform);
            DressVisual.Content = DressScene;
            DressViewport.Camera = DressCamera;
            DressViewport.Children.Add(DressVisual);
            this.Children.Add(DressViewport);
        }
        public void SetCamera(Point3D Pos, double FOVangle)
        {
            DressCamera.LookDirection = new Vector3D(0, 0, -1);
            DressCamera.UpDirection = new Vector3D(0, 1, 0);
            DressCamera.NearPlaneDistance = 0;
            DressCamera.Position = Pos;
            DressCamera.FieldOfView = FOVangle;
            DressCamera.FarPlaneDistance = DressVisual.Content.Bounds.Z + Pos.Z + DressVisual.Content.Bounds.SizeZ / 2;  // FarPlane to clip the model into half
        }
        public void Rotate(double angle)
        {
            RotateTransform3D _rotateTransform = new RotateTransform3D();
            var _axisAngleRotation3D = new AxisAngleRotation3D { Axis = new Vector3D(0, 1, 0), Angle = 360 - angle };
            this.DressCamera.Transform = _rotateTransform;
            Rotation3DAnimation _rotateAnimation = new Rotation3DAnimation(_axisAngleRotation3D, TimeSpan.FromSeconds(0));
            _rotateTransform.BeginAnimation(RotateTransform3D.RotationProperty, _rotateAnimation);
        }
    }
    public class DressCollection
    {
        const string GeometryIDStr = "Object__0OR9GR10", MaterialIDStr = "clothMR1", TransformIDStr = "Object__0OR9TR8", SceneIDStr = "SceneTR7";
        public Dictionary<DressTypes, DressModel> Dresses;
        private MainWindow VFRWindow;
        public static string CurPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase); 

        public DressCollection(MainWindow ParentWindow)
        {
            Dresses = new Dictionary<DressTypes, DressModel>();
            VFRWindow = ParentWindow;
        }
        public DressModel CreateDress(DressTypes aDressType, string XAMLFile, string GeometryID = GeometryIDStr, string MaterialID = MaterialIDStr, string TransformID = TransformIDStr, string SceneID = SceneIDStr)
        {
            DressModel aDress = new DressModel(XAMLFile);
            aDress.DressType = aDressType;
            VFRWindow.Resources.MergedDictionaries.Add(aDress.DressResource);
            aDress.CreateModel(GeometryID, MaterialID, TransformID, SceneID);
            aDress.SetCamera(new Point3D(0, 3.1, 3.5), 25); 
            Dresses.Add(aDressType, aDress);
            return aDress;
        }
        public void CreateDressCollection()
        {
            DressModel aDress = CreateDress(DressTypes.Floral24, CurPath + "\\Dresses\\Floral24.xaml");
            aDress.Height = 326;
            aDress.DressSizeRatio = aDress.Height / aDress.Width;
            aDress.SetCamera(new Point3D(0, 2.77, 1.7), 39);
            
            aDress = CreateDress(DressTypes.Floral27, CurPath + "\\Dresses\\Floral27.xaml");
            aDress.SetCamera(new Point3D(0, 3.02, 1.6), 39);

            aDress = CreateDress(DressTypes.Floral30, CurPath + "\\Dresses\\Floral30.xaml");
            aDress.SetCamera(new Point3D(0, 3.09, 1.55), 39);

            aDress = CreateDress(DressTypes.Floral33, CurPath + "\\Dresses\\Floral33.xaml");
            aDress.SetCamera(new Point3D(0, 3.05, 1.54), 39);

            aDress = CreateDress(DressTypes.Floral79, CurPath + "\\Dresses\\Floral79.xaml");
            aDress.SetCamera(new Point3D(0, 3.33, 1.12), 39);
            aDress.ScaleFactor = 1.1f;

        }
    }
}
