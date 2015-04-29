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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.Timers;
using System.Windows.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Drawing.Imaging;
using System.IO;

namespace VirtualFittingRoomTest2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum ButtonTypes { None = -1, Floral24 = 0, Floral27 = 1, Floral30 = 2, Floral33 = 3, Floral79 = 4, ClearBtn = 5, CaptureBtn = 6, SkeletonBtn = 7}
        public enum SoundTypes  { DressButtons = 0, ClearBtn = 1, CaptureBtn = 2, SkeletonBtn = 3 }
        private Dictionary<ButtonTypes, Grid> DressButtons;
        private Dictionary<SoundTypes, MediaElement> ButtonSounds;
        private KinectMethods KinectVFR;
        private DressCollection DressGroup1;
        public DressTypes CurrentDress;
        private const double ButtonsSize = 300;
        private ButtonTypes PrevButtonLeftState = ButtonTypes.None, PrevButtonRightState = ButtonTypes.None;
        private int CaptureCount = 2;
        private DispatcherTimer CaptureTimer = new DispatcherTimer();
        private DispatcherTimer LabelCaptureTimer = new DispatcherTimer();
        #region GUI Init
        public MainWindow()
        {
            InitializeComponent();
            this.SizeChanged += new SizeChangedEventHandler(Window1_SizeChanged);               
       
            DressButtons = new Dictionary<ButtonTypes, Grid>();
            ButtonSounds = new Dictionary<SoundTypes, MediaElement>();
            PopulateDressButtons();
            SetButtonEvents();

            NativeMethods.mciSendString("open \"" + ".\\Sounds\\bone.mp3" + "\" type mpegvideo alias BoneSound", null, 0, IntPtr.Zero);
            
            this.Loaded += (s, e) => { LoadProgram(); };
            this.Unloaded += (s, e) => { KinectVFR = null; };
            CaptureTimer.Tick += new EventHandler(CaptureTimer_Tick);
            CaptureTimer.Interval = new TimeSpan(0, 0, 0, 3, 200);
            LabelCaptureTimer.Tick += new EventHandler(LabelCaptureTimer_Tick);
            LabelCaptureTimer.Interval = new TimeSpan(0, 0, 1);
        }

        void LabelCaptureTimer_Tick(object sender, EventArgs e)
        {
            if (CaptureCount-- <= 0)
            {
                LabelCaptureTimer.Stop();
                lblCaptureCounter.Content = "";
                return;
            }
            lblCaptureCounter.Content = ((CaptureCount) + 1);
            LabelCaptureTimer.Start();
            CommandManager.InvalidateRequerySuggested();
        }

        void CaptureTimer_Tick(object sender, EventArgs e)
        {
            CaptureTimer.Stop();
            Bitmap bitmapImage = new Bitmap((int)this.ActualWidth, (int)this.ActualHeight);
            Graphics gr1 = Graphics.FromImage(bitmapImage);
            IntPtr dc1 = gr1.GetHdc();
            IntPtr dc2 = NativeMethods.GetWindowDC(NativeMethods.GetForegroundWindow());
            NativeMethods.BitBlt(dc1, (int)20, (int)20, (int)this.ActualWidth, (int)this.ActualHeight, dc2, 20, 20, 13369376);
            gr1.ReleaseHdc(dc1);
            Random rnd = new Random();
            bitmapImage.Save(string.Format(".\\Captures\\Capture{0}.jpg", rnd.Next().ToString()), ImageFormat.Jpeg);
            PlaySoundOnButton(ButtonTypes.CaptureBtn);
            CommandManager.InvalidateRequerySuggested();
        }
        internal class NativeMethods
        {
            [DllImport("user32.dll")]
            public extern static IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hwnd);
            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern IntPtr GetForegroundWindow();
            [DllImport("gdi32.dll")]
            public static extern UInt64 BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, System.Int32 dwRop);
            [DllImport("winmm.dll")]
            public static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);
        }
        private void LoadProgram()
        {
            CurrentDress = DressTypes.Floral30;
            
            KinectVFR = new KinectMethods(VideoStreamGrid.ActualWidth - 25, VideoStreamGrid.ActualHeight);
            this.VideoStreamImage.Source = KinectVFR.ColorImageKinect;
            KinectVFR.VFRWindow = this;
            KinectVFR.OnKinectJointEvent += new KinectMethods.KinectEventHandler(KinectVFR_OnKinectJointEvent);
            KinectVFR.OnKinectScaleEvent += new KinectMethods.KinectTransformHandler(KinectVFR_OnKinectScaleEvent);
            KinectVFR.OnKinectTranslateEvent += new KinectMethods.KinectTranslateHandler(KinectVFR_OnKinectTranslateEvent);
            KinectVFR.OnKinectRotateEvent += new KinectMethods.KinectTransformHandler(KinectVFR_OnKinectRotateEvent);
            DressGroup1 = new DressCollection(this);
            DressGroup1.CreateDressCollection();
            foreach (KeyValuePair<DressTypes, DressModel> aDress in DressGroup1.Dresses)
            {
                if (aDress.Key <= DressTypes.Naked) continue;
                VideoStreamGrid.Children.Add(aDress.Value);
                SetDressVisibility(aDress.Key, Visibility.Hidden);
            }
            SetDressVisibility(CurrentDress, Visibility.Visible);
        }
        private void Window1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double ratioX= e.NewSize.Height / this.Height;
            double ratioY= e.NewSize.Width / this.Width;
            double aSizeX = ratioX *ButtonsSize;
            double aSizeY = ratioY * ButtonsSize;

            ViewBox1.Width = e.NewSize.Width;
            ViewBox1.Height = e.NewSize.Height;
            foreach (KeyValuePair<ButtonTypes, Grid> aItem in DressButtons)
            {
                if (aItem.Key == ButtonTypes.None) continue;
                aItem.Value.Width = aSizeY;
                aItem.Value.Height = aSizeX;
            }
            ClearBtn.Width = aSizeY;
            ClearBtn.Height = aSizeX;
            SkeletonBtn.Width = aSizeY;
            SkeletonBtn.Height = aSizeX;
            CaptureBtn.Width = aSizeY;
            CaptureBtn.Height = aSizeX;
        }
        #endregion  
        #region Kinects
        private void KinectVFR_OnKinectRotateEvent(double aValue)
        {
            if (CurrentDress <= DressTypes.Naked) return;
            DressModel aDress = DressGroup1.Dresses[CurrentDress];
            aDress.SetCamera(aDress.DressCamera.Position, aDress.DressCamera.FieldOfView);
            aDress.Rotate(aValue);
        }

        private void KinectVFR_OnKinectTranslateEvent(Point3D NewPosition)
        {
            if (CurrentDress <= DressTypes.Naked) return;
            DressModel aDress = DressGroup1.Dresses[CurrentDress];
            aDress.Margin = new Thickness(NewPosition.X - aDress.Width / 2, NewPosition.Y,
                                          VideoStreamGrid.Width - NewPosition.X - 10 - aDress.Width / 2,
                                          VideoStreamGrid.Height - NewPosition.Y - aDress.Height);
        }
        private void SetDressVisibility(DressTypes aDress, Visibility aVisible)
        {
            if (aDress <= DressTypes.Naked) return;
            VideoStreamGrid.Children[VideoStreamGrid.Children.IndexOf(DressGroup1.Dresses[aDress])].Visibility = aVisible;
        }
        private void KinectVFR_OnKinectScaleEvent(double ShoulderDistance)
        {
            if (CurrentDress <= DressTypes.Naked) return;
            if (ShoulderDistance == 0) return;
            DressGroup1.Dresses[CurrentDress].Width = ShoulderDistance * DressGroup1.Dresses[CurrentDress].ScaleFactor;
            DressGroup1.Dresses[CurrentDress].Height = ShoulderDistance * DressGroup1.Dresses[CurrentDress].ScaleFactor * DressGroup1.Dresses[CurrentDress].DressSizeRatio;
        }
        private void KinectVFR_OnKinectJointEvent(JointType aJointType, System.Windows.Media.Media3D.Point3D Pos3D, Skeleton[] aFrame)
        {
            switch (aJointType)
            {
                case JointType.HandLeft:
                    Pos3D.X = Pos3D.X + ButtonsSize / 2;
                    //Pos3D.Y = Pos3D.Y - ButtonsSize / 2;
                    ButtonTypes aButton = FindHitButton(Pos3D);
                    if (aButton > ButtonTypes.None)
                    {
                        if (aButton != ButtonTypes.CaptureBtn && aButton != PrevButtonLeftState)
                            PlaySoundOnButton(aButton);
                        SelectDress(ButtonTypeToDressType(aButton));
                        if (aButton == ButtonTypes.SkeletonBtn && aButton != PrevButtonLeftState)
                            KinectVFR.EnableDrawSkeleton = !KinectVFR.EnableDrawSkeleton;
                        if (aButton == ButtonTypes.CaptureBtn && aButton != PrevButtonLeftState && !CaptureTimer.IsEnabled)
                        {
                            lblCaptureCounter.Content = 3;
                            CaptureCount = 2;
                            CaptureTimer.Start();
                            LabelCaptureTimer.Start();
                        }
                    }
                    PrevButtonLeftState = aButton;
                    break;
                case JointType.HandRight:
                    Pos3D.X = Pos3D.X - ButtonsSize / 2;
                    Pos3D.Y = Pos3D.Y + ButtonsSize / 2;
                    aButton = FindHitButton(Pos3D);
                    if (aButton > ButtonTypes.None)
                    {
                        if (aButton != PrevButtonRightState)
                        {
                            PlaySoundOnButton(aButton);
                            SelectDress(ButtonTypeToDressType(aButton));
                        }
                    }
                    PrevButtonRightState = aButton;
                    break;
                case JointType.Head:
                    KinectVFR.DrawSkeleton(VideoStreamGrid);
                    break;

            }
        }
        private void SelectDress(DressTypes aDressType)
        {
            if (aDressType == CurrentDress || aDressType == DressTypes.None) return;
            SetDressVisibility(CurrentDress, Visibility.Hidden);
            CurrentDress = aDressType;
            if (aDressType > DressTypes.Naked)
                SetDressVisibility(CurrentDress, Visibility.Visible);
        }
        #endregion
        #region Buttons
        private void PopulateDressButtons()
        {
            this.DressButtons.Add(ButtonTypes.Floral33,    this.FloralRed);
            this.DressButtons.Add(ButtonTypes.Floral79,    this.RedUndergarment);
            this.DressButtons.Add(ButtonTypes.Floral30,    this.FloralOrange);
            this.DressButtons.Add(ButtonTypes.Floral24,    this.FloralGrey);
            this.DressButtons.Add(ButtonTypes.Floral27,    this.KnitGrey);
            this.DressButtons.Add(ButtonTypes.ClearBtn,    this.ClearBtn);
            this.DressButtons.Add(ButtonTypes.CaptureBtn,  this.CaptureBtn);
            this.DressButtons.Add(ButtonTypes.SkeletonBtn, this.SkeletonBtn);
            this.ButtonSounds.Add(SoundTypes.DressButtons, CreateSoundElement("Dress.mp3"));
            this.ButtonSounds.Add(SoundTypes.ClearBtn    , CreateSoundElement("sweeper.mp3"));
            this.ButtonSounds.Add(SoundTypes.SkeletonBtn , CreateSoundElement("bone.mp3"));
            this.ButtonSounds.Add(SoundTypes.CaptureBtn  , CreateSoundElement("camera.mp3"));
        }
        private MediaElement CreateSoundElement(string aFile)
        {
            MediaElement SoundElement = new MediaElement();
            SoundElement.LoadedBehavior = MediaState.Manual;
            SoundElement.Source = new Uri(DressCollection.CurPath + "\\Sounds\\" + aFile, UriKind.RelativeOrAbsolute);
            ParentGrid.Children.Add(SoundElement);
            return SoundElement;
        }
        private ButtonTypes FindHitButton(Point3D Pos)
        {
            ButtonTypes Res = ButtonTypes.None;
            foreach (KeyValuePair<ButtonTypes, Grid> aItem in DressButtons)
            {
                if (Pos.X > Canvas.GetLeft(aItem.Value) && Pos.X < Canvas.GetLeft(aItem.Value) + aItem.Value.ActualWidth &&
                    Pos.Y > Canvas.GetTop(aItem.Value) && Pos.Y < Canvas.GetTop(aItem.Value) + aItem.Value.ActualHeight)
                {
                    Res = aItem.Key;
                    break;
                }
            }
            return Res;
        }
        private DressTypes ButtonTypeToDressType(ButtonTypes aButton)
        {
            DressTypes Res = DressTypes.None;
            foreach (DressTypes aDress in Enum.GetValues(typeof(DressTypes)))
                if (aButton.ToString().Equals(aDress.ToString()))
                {
                    Res = aDress;
                    break;
                }
            if (aButton == ButtonTypes.ClearBtn)
                Res = DressTypes.Naked;
            return Res;
        }
        private void PlaySoundOnButton(ButtonTypes aButton)
        {
            switch (aButton)
            {
                case ButtonTypes.Floral24:
                case ButtonTypes.Floral27:
                case ButtonTypes.Floral30:
                case ButtonTypes.Floral33:
                case ButtonTypes.Floral79:
                    PlaySound(SoundTypes.DressButtons);
                    break;
                case ButtonTypes.ClearBtn:
                    PlaySound(SoundTypes.ClearBtn);
                    break;
                case ButtonTypes.CaptureBtn:
                    PlaySound(SoundTypes.CaptureBtn);
                    break;
                case ButtonTypes.SkeletonBtn:
                    PlaySound(SoundTypes.SkeletonBtn);
                    break;
            }
        }
        private void PlaySound(SoundTypes aSound)
        {
            ButtonSounds[aSound].Stop();
            ButtonSounds[aSound].Position = TimeSpan.Zero;
            ButtonSounds[aSound].Play();
        }
        private void SetButtonEvents()
        {
            FloralGrey.MouseDown += (s, e) => { SelectDress(DressTypes.Floral24); };
            RedUndergarment.MouseDown += (s, e) => { SelectDress(DressTypes.Floral79); };
            FloralOrange.MouseDown += (s, e) =>
            {
                SelectDress(DressTypes.Floral30);
            };
            FloralRed.MouseDown += (s, e) =>
            {
                SelectDress(DressTypes.Floral33);
            };
            KnitGrey.MouseDown += (s, e) => { SelectDress(DressTypes.Floral27); };
            CaptureBtn.MouseDown += (s, e) => {
                lblCaptureCounter.Content = 3;
                CaptureCount = 2;
                CaptureTimer.Start();
                LabelCaptureTimer.Start();
            };
        }
        #endregion
    }
}

