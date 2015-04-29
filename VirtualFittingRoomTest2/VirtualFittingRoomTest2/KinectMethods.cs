using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;


namespace VirtualFittingRoomTest2
{
    class KinectMethods
    {
        #region Consts
        private const double RotationThreshold = 2.0f;
        #endregion

        #region Events
        public delegate void KinectEventHandler(JointType aJointType, Point3D Pos3D, Skeleton[] aFrame);
        public event KinectEventHandler OnKinectJointEvent;
        public delegate void KinectTransformHandler(double aValue);
        public event KinectTransformHandler OnKinectScaleEvent, OnKinectRotateEvent;
        public delegate void KinectTranslateHandler(Point3D NewPosition);
        public event KinectTranslateHandler OnKinectTranslateEvent;
        #endregion Events

        #region Member Variables
        private KinectSensor _KinectDevice;
        private Skeleton[] _FrameSkeletons;
        public bool IsEnabled = true, EnableDrawSkeleton = false;
        private double FrameWidth, FrameHeight;
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        public int _ColorImageStride;
        private readonly Brush[] _SkeletonBrushes;
        private double Prev_Shoulder_Length = -1, Prev_Shoulder_Angle = 0;
        public MainWindow VFRWindow;
        #endregion Member Variables
        
        #region Constructor
        public KinectMethods(double aFrameWidth, double aFrameHeight)
        {
            FrameWidth   = aFrameWidth;
            FrameHeight  = aFrameHeight;
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
            _SkeletonBrushes = new Brush[] { Brushes.Black, Brushes.Yellow};
        }
        #endregion Constructor

        #region Methods
        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectDevice = e.Sensor;                                        
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.    
                    //MessageBox.Show("Kinect is not connected!"); // ***************************** Uncomment this *************************************************************
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }
        private void KinectDevice_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null && this.IsEnabled)
                {
                    
                    frame.CopySkeletonDataTo(this._FrameSkeletons);
                    DoKinectEvent(JointType.HandLeft);
                    DoKinectEvent(JointType.HandRight);
                    Point3D ShoulderLeftPos   = DoKinectEvent(JointType.ShoulderLeft);
                    Point3D ShoulderCenterPos = DoKinectEvent(JointType.ShoulderCenter);
                    Point3D ShoulderRightPos  = DoKinectEvent(JointType.ShoulderRight);
                    double ShoulderLen = CalcShoulderLength(ShoulderLeftPos, ShoulderRightPos, ShoulderCenterPos.Z);
                    double ShoulderAngle = CalcRotationAngle(ShoulderLeftPos, ShoulderCenterPos);
                    if (Math.Abs(ShoulderAngle - Prev_Shoulder_Angle)  > RotationThreshold)
                    {
                        OnKinectRotateEvent(ShoulderAngle);
                        Prev_Shoulder_Angle = ShoulderAngle;
                    }
                    else if (ShoulderLen > 0)
                        OnKinectScaleEvent(ShoulderLen);
                    DrawCoord(VFRWindow.label1, string.Format("L= {0}, C= {1}, R = {2}, Angle = {3:F}, Distance = {4:F}", ShoulderLeftPos.Z, ShoulderCenterPos.Z, ShoulderRightPos.Z, ShoulderAngle, ShoulderLen)); 
                    OnKinectTranslateEvent(ShoulderCenterPos);
                    DoKinectEvent(JointType.ShoulderCenter);
                    OnKinectJointEvent(JointType.Head, new Point3D(0, 0, 0), _FrameSkeletons);
                }
            }
        }
        public void DrawCoord(Label lab, string coordstr)
        {
            lab.Content = coordstr; 
        }
        private Point3D DoKinectEvent(JointType aJoint)
        {
            Point3D pt = TrackJoint(aJoint);
            if (pt.X != -1 && pt.Y != -1 && pt.Z != -1)
                OnKinectJointEvent(aJoint, pt, _FrameSkeletons);
            return pt;
        }
        
        private double CalcRotationAngle(Point3D ptLeft, Point3D ptCenter)
        {
            double ResAngle = -1;
            if (ptLeft.X != -1 && ptLeft.Y != -1 && ptLeft.Z != -1 && ptCenter.X != -1 && ptCenter.Y != -1 && ptCenter.Z != -1)
            {
                double CurAngle = (Math.Atan2(ptLeft.Z - ptCenter.Z, ptLeft.X - ptCenter.X) * (180 / Math.PI));

                if (CurAngle < 0)
                    ResAngle = CurAngle + 180;
                else if (CurAngle > 0)
                    ResAngle = CurAngle - 180;
            }
            return ResAngle;
        }
        private static double CalcDistance(Point3D pt1, Point3D pt2, double ShoulderCenterDepth)
        {
           return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2) + (Math.Pow(pt1.Z - pt2.Z, 2) * ShoulderCenterDepth));
        }
        private double CalcShoulderLength(Point3D sLeft, Point3D sRight, double ShoulderCenterDepth)
        {
            double Res = -1;
            if (sLeft.X != -1 && sLeft.Y != -1 && sLeft.Z != -1 && sRight.X != -1 && sRight.Y != -1 && sRight.Z != -1)
            {
                double CurrentLength = CalcDistance(sLeft, sRight, 1);
                if (Prev_Shoulder_Length != CurrentLength)
                {
                    Prev_Shoulder_Length = CurrentLength;
                    Res = CurrentLength;
                }
            }
            return Res;
        }
        private Point3D GetJointPoint(Joint joint)
        {           
            DepthImagePoint point = this.KinectDevice.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, this.KinectDevice.DepthStream.Format);
            point.X = (int)((double)point.X * (FrameWidth / KinectDevice.DepthStream.FrameWidth));
            point.Y = (int)((double)point.Y * (FrameHeight / KinectDevice.DepthStream.FrameHeight));
            return new Point3D(point.X, point.Y, point.Depth);
        }
        private Point3D TrackJoint(JointType aJoint)
        {
            Skeleton skeleton;
            for (int i = 0; i < this._FrameSkeletons.Length; i++)
            {
                skeleton = this._FrameSkeletons[i];
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    Joint joint = skeleton.Joints[aJoint];
                    if (joint.TrackingState != JointTrackingState.NotTracked)
                    {
                        Point3D jointPoint = GetJointPoint(joint);
                        double z = joint.Position.Z;
                        return new Point3D(jointPoint.X, jointPoint.Y, jointPoint.Z);
                    }
                }
            }
            return new Point3D(-1, -1, -1);
        }
        public void DrawSkeleton(Grid DrawingFrame)
        {
            Polyline figure;
            Brush userBrush;
            Skeleton skeleton;
            int j = 0;
            while (j < DrawingFrame.Children.Count)
            {
                if (DrawingFrame.Children[j].GetType() == typeof(Polyline))
                {
                    DrawingFrame.Children.Remove(DrawingFrame.Children[j]);
                    j = 0;
                }
                else
                    j++;
            }
            if (!EnableDrawSkeleton) return;
            for (int i = 0; i < this._FrameSkeletons.Length; i++)
            {
                skeleton = this._FrameSkeletons[i];
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    userBrush = _SkeletonBrushes[1];
                    figure = CreateFigure(skeleton, userBrush, new[] { JointType.Head, JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.Spine,
                                                                       JointType.ShoulderRight, JointType.ShoulderCenter, JointType.HipCenter});
                    DrawingFrame.Children.Add(figure);
                    figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipLeft, JointType.HipRight });
                    DrawingFrame.Children.Add(figure);
                    //Draw left leg
                    figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft });
                    DrawingFrame.Children.Add(figure);
                    //Draw right leg
                    figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight });
                    DrawingFrame.Children.Add(figure);
                    //Draw left arm
                    figure = CreateFigure(skeleton, userBrush, new[] { JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft });
                    DrawingFrame.Children.Add(figure);
                    //Draw right arm
                    figure = CreateFigure(skeleton, userBrush, new[] { JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight });
                    DrawingFrame.Children.Add(figure);
                }
            }
        }
        private Polyline CreateFigure(Skeleton skeleton, Brush brush, JointType[] joints)
        {
            Polyline figure = new Polyline();
            figure.StrokeThickness = 8;
            figure.Stroke = brush;
            for (int i = 0; i < joints.Length; i++)
            {
                Point3D jpoint = GetJointPoint(skeleton.Joints[joints[i]]);
                figure.Points.Add(new Point(jpoint.X, jpoint.Y));
            }
            return figure;
        }
        private void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    this._ColorImageBitmap.WritePixels(this._ColorImageBitmapRect, pixelData, this._ColorImageStride, 0);
                }
            }
        }
        #endregion Methods

        #region Properties
        public WriteableBitmap ColorImageKinect
        {
            get { return _ColorImageBitmap;  }
        }
        public KinectSensor KinectDevice 
        {
            get { return this._KinectDevice; }
            set
            {
                if(this._KinectDevice != value)
                {
                    //Uninitialize
                    if(this._KinectDevice != null)
                    {
                        this._KinectDevice.Stop();
                        this._KinectDevice.SkeletonFrameReady -= KinectDevice_SkeletonFrameReady;
                        this._KinectDevice.ColorFrameReady -= Kinect_ColorFrameReady;
                        this._KinectDevice.SkeletonStream.Disable();
                        this._FrameSkeletons = null;
                    }
                    this._KinectDevice = value;
                    //Initialize
                    if (this._KinectDevice != null)
                    {
                        if (this._KinectDevice.Status == KinectStatus.Connected)
                        {
                            ColorImageStream colorStream = _KinectDevice.ColorStream;
                            colorStream.Enable();
                            this._ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                                96, 96, PixelFormats.Bgr32, null);
                            this._ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                            this._ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                            this._KinectDevice.ColorFrameReady += Kinect_ColorFrameReady;
                            this._KinectDevice.SkeletonStream.Enable(new TransformSmoothParameters()
                            {
                                Correction = 0.5f,
                                JitterRadius = 0.05f,
                                MaxDeviationRadius = 0.04f,
                                Smoothing = 0.5f
                            });
                            this._FrameSkeletons = new Skeleton[this._KinectDevice.SkeletonStream.FrameSkeletonArrayLength];
                            this._KinectDevice.SkeletonFrameReady += KinectDevice_SkeletonFrameReady;
                            this._KinectDevice.Start();                             
                        }
                    }                
                }
            }
        }        
        #endregion Properties
    }
}
