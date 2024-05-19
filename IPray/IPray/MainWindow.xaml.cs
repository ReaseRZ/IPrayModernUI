using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IPray
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int themeDefault = 0;
        string City, Country;
        bool AdzanTime = false;
        BinaryWriter writeMemory;
        BinaryReader binaryReader;
        MediaPlayer soundPlayer = new MediaPlayer();
        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
        System.Timers.Timer timer = new System.Timers.Timer();
        GetApiPrayerTime prayerTime = new GetApiPrayerTime();
        Dictionary<string, bool> TimeCallerPrayer = new Dictionary<string, bool>();
        Dictionary<string,int>TimePrayerList = new Dictionary<string,int>();
        Dictionary<int,bool>ManagerRemainder = new Dictionary<int,bool>();
        
        Window window = null;
        TextBox countryTxt = null;
        TextBox cityTxt = null;

        readonly string[,] locationTag =
            {
            { "Tokyo", "Osaka", "Kyoto", "Sapporo" },
            { "Berlin", "Stuttgart","Hamburg","Dortmund" },
            {"Jakarta","Jayapura","Surabaya","Makassar" },
            {"Beijing", "Guangzhou","Shanghai","Hangzhou" },
            {"London", "Liverpool","Birmingham","Glasgow" },
            {"Makkah","Madinah","Riyadh","Jeddah" },
            {"Moskow","Kazan","Saint Petersburg","Omsk" },
            {"New York","Los Angeles","Chicago","San Francisco" }
            };
        private void SetUpTimerUpdate()
        {
            timer.Interval = 250;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(SetElementInterface);
        }
        private void SetUpTimerPrayer()
        {
            TimeCallerPrayer.Add("Fajr", false);
            TimeCallerPrayer.Add("Dhuhr", false);
            TimeCallerPrayer.Add("Asr", false);
            TimeCallerPrayer.Add("Maghrib", false);
            TimeCallerPrayer.Add("Isha'", false);

            TimePrayerList.Add("Fajr", 0);
            TimePrayerList.Add("Dhuhr", 0);
            TimePrayerList.Add("Asr", 0);
            TimePrayerList.Add("Maghrib", 0);
            TimePrayerList.Add("Isha'", 0);
            
            ManagerRemainder.Add(0, false);
            ManagerRemainder.Add(1, false);
        }

        private void InitBallonTipEvent()
        {
            notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
            notifyIcon.Visible = true;
            notifyIcon.Text = "IPray";
            notifyIcon.DoubleClick += (sender, e) =>
            {
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
                Duration duration = new Duration(TimeSpan.FromMilliseconds(500));
                DoubleAnimation doubleAnimation1 = new DoubleAnimation();
                doubleAnimation1.Duration = duration;
                Storyboard storyboard = new Storyboard();
                Storyboard.SetTarget(doubleAnimation1, WindowFrame);
                Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath("Height"));
                doubleAnimation1.From = 0;
                doubleAnimation1.To = 590;
                storyboard.Children.Add(doubleAnimation1);
                storyboard.Begin();
            };

        }

        private void SetUpStartUpTheme()
        {
            try
            {
                binaryReader = new BinaryReader(new FileStream("config", FileMode.Open));
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message + " can't open the file");
                return;
            }
            try
            {
                themeDefault = binaryReader.ReadInt32();
                Country = binaryReader.ReadString();
                City = binaryReader.ReadString();
                Console.WriteLine(Country);
                Console.WriteLine(City);
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message + " can't read the file");
                return;
            }
            binaryReader.Close();
            switch (themeDefault)
            {
                case 0:
                    ChangeThemeToBlue();
                    break;
                case 1:
                    ChangeColorToBrown();
                    break;
            }
            location.Text = City + ", " + Country;
        }

        private void ImportResource()
        {
            soundPlayer.Open(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sound/mecca_56_22.wav")));
        }

        public MainWindow()
        {
            InitBallonTipEvent();
            SetUpTimerPrayer();
            InitializeComponent();
            ImportResource();
            SetUpStartUpTheme();
            SetUpTimerUpdate();
        }

        private void QuitApp(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void WindowMove(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            Duration duration = new Duration(TimeSpan.FromMilliseconds(500));
            DoubleAnimation doubleAnimation1 = new DoubleAnimation();
            doubleAnimation1.Duration = duration;
            Storyboard storyboard = new Storyboard();
            Storyboard.SetTarget(doubleAnimation1, WindowFrame);
            Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath("Height"));
            doubleAnimation1.From = 590;
            doubleAnimation1.To = 0;
            storyboard.Children.Add(doubleAnimation1);
            storyboard.Completed += (ev, ex) =>
            {
                this.WindowState = WindowState.Minimized;
                notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                notifyIcon.ShowBalloonTip(5000, "IPray Is Running On The Background", "I will always remind you from here", System.Windows.Forms.ToolTipIcon.None);
                ManagerRemainder[0] = true;
                ShowInTaskbar = false;
            };
            storyboard.Begin();
        }

        private void StopAdzan(object sender, RoutedEventArgs e)
        {
            MainAdzanAlert.Visibility = Visibility.Hidden;
            soundPlayer.Stop();
            soundPlayer.Position = TimeSpan.Zero;
        }

        private void ChangeThemeToBlue(object sender, MouseButtonEventArgs e)
        {
            //Unique Theme Code
            themeDefault = 0;

            //Main element
            mainCanvas.Background = new SolidColorBrush(Color.FromRgb(58,0,166));
            location.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            dateHijri.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Fajr element
            cRectFajr.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            FajrText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            FajrTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Sunrises element
            cRectSunrise.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            SunriseText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            SunriseTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Dhuhr element
            cRectDhuhr.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            DhuhrText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            DhuhrTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Asr element
            cRectAsr.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            AsrText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            AsrTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Maghrib element
            cRectMaghrib.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            MaghribText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            MaghribTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Isha' element
            cRectIsha.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            IshaText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            IshaTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Top Slide
            TopMenu.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            BtnChangeLctn.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            BtnChangeLctn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            MinimizeBtn.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            MinimizeBtn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            QuitBtn.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            QuitBtn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Loading Screen
            loadingScreen.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            ScreenLoading.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            RectLoadingScreen.Fill = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            TextLoadingScreen.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Change Location Menu
            LocationWindow.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            SrcCity.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            SrcCountry.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            CountryComboBox.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            CountryComboBox.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            CountryComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            CityComboBox.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            CityComboBox.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            CityComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 0, 166));

            SyncBtn.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            SyncBtn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            SetDefault.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            SetDefault.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            Back.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            Back.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //ConfirmSet Default
            SubMainConfirmDefault.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            TextConfirmDefault.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            Confirm.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            Confirm.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Adzan Alert
            SubMainAdzanAlert.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            AdzanText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            Stop.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            Stop.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Creadit
            SubMainCredit.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            TitleText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            TitleText1.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            OK.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            OK.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
        }
        private void ChangeThemeToBlue()
        {
            //Main element
            mainCanvas.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            location.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            dateHijri.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Fajr element
            cRectFajr.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            FajrText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            FajrTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Sunrises element
            cRectSunrise.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            SunriseText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            SunriseTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Dhuhr element
            cRectDhuhr.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            DhuhrText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            DhuhrTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Asr element
            cRectAsr.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            AsrText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            AsrTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Maghrib element
            cRectMaghrib.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            MaghribText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            MaghribTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Isha' element
            cRectIsha.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            IshaText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            IshaTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Top Slide
            TopMenu.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            BtnChangeLctn.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            BtnChangeLctn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            MinimizeBtn.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            MinimizeBtn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            QuitBtn.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            QuitBtn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Loading Screen
            loadingScreen.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            ScreenLoading.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            RectLoadingScreen.Fill = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            TextLoadingScreen.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Change Location Menu
            LocationWindow.Background = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            SrcCity.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            SrcCountry.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            CountryComboBox.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            CountryComboBox.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            CountryComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 0, 166));
            CityComboBox.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            CityComboBox.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            CityComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 0, 166));

            SyncBtn.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            SyncBtn.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            SetDefault.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            SetDefault.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            Back.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            Back.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //ConfirmSet Default
            SubMainConfirmDefault.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            TextConfirmDefault.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            Confirm.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            Confirm.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Adzan Alert
            SubMainAdzanAlert.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            AdzanText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            Stop.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            Stop.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));

            //Credit
            SubMainCredit.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            TitleText.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            TitleText1.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
            OK.Background = new SolidColorBrush(Color.FromRgb(0, 0, 108));
            OK.Foreground = new SolidColorBrush(Color.FromRgb(0, 197, 255));
        }
        private void ChangeColorToBrown(object sender, MouseButtonEventArgs e)
        {
            //Unique Theme Code
            themeDefault = 1;

            //Main Window
            mainCanvas.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            location.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            dateHijri.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //Location window
            LocationWindow.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SrcCountry.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SrcCity.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SyncBtn.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SyncBtn.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SetDefault.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SetDefault.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Back.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            Back.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            CountryComboBox.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            CountryComboBox.Foreground = new SolidColorBrush(Color.FromRgb(0,0,0));
            CountryComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            CityComboBox.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            CityComboBox.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            CityComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //Fajr element
            cRectFajr.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            FajrText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            FajrTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Sunrises element
            cRectSunrise.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SunriseText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SunriseTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Dhuhr element
            cRectDhuhr.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            DhuhrText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            DhuhrTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Asr element
            cRectAsr.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            AsrText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            AsrTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Maghrib element
            cRectMaghrib.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            MaghribText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            MaghribTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Isha' element
            cRectIsha.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            IshaText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            IshaTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Top Slide
            TopMenu.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            BtnChangeLctn.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            BtnChangeLctn.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            MinimizeBtn.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            MinimizeBtn.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            QuitBtn.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            QuitBtn.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //Loading Screen
            ScreenLoading.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            RectLoadingScreen.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            loadingScreen.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            TextLoadingScreen.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //ConfirmSet Default
            SubMainConfirmDefault.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            TextConfirmDefault.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Confirm.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Confirm.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Adzan Alert
            SubMainAdzanAlert.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            AdzanText.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Stop.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Stop.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Credit
            SubMainCredit.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            TitleText.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            TitleText1.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            OK.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            OK.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
        }
        private void ChangeColorToBrown()
        {
            mainCanvas.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            location.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            dateHijri.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //Location window
            LocationWindow.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SrcCountry.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SrcCity.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SyncBtn.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SyncBtn.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SetDefault.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SetDefault.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Back.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            Back.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            CountryComboBox.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            CountryComboBox.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            CountryComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            CityComboBox.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            CityComboBox.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            CityComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //Fajr element
            cRectFajr.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            FajrText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            FajrTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Sunrises element
            cRectSunrise.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            SunriseText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            SunriseTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Dhuhr element
            cRectDhuhr.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            DhuhrText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            DhuhrTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Asr element
            cRectAsr.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            AsrText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            AsrTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Maghrib element
            cRectMaghrib.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            MaghribText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            MaghribTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Isha' element
            cRectIsha.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            IshaText.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            IshaTime.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Top Slide
            TopMenu.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            BtnChangeLctn.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            BtnChangeLctn.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            MinimizeBtn.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            MinimizeBtn.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            QuitBtn.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            QuitBtn.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //Loading Screen
            ScreenLoading.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            RectLoadingScreen.Fill = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            loadingScreen.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            TextLoadingScreen.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));

            //Adzan Alert
            SubMainAdzanAlert.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            AdzanText.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Stop.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            Stop.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

            //Credit
            SubMainCredit.Background = new SolidColorBrush(Color.FromRgb(165, 42, 42));
            TitleText.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            TitleText1.Foreground = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            OK.Background = new SolidColorBrush(Color.FromRgb(245, 222, 179));
            OK.Foreground = new SolidColorBrush(Color.FromRgb(165, 42, 42));

        }
        private void Credit(object sender, MouseButtonEventArgs e) 
        {
            MainCredit.Visibility = Visibility.Visible;
        }

        private void CloseCredit(object sender, RoutedEventArgs e)
        {
            MainCredit.Visibility = Visibility.Hidden;
        }

        private void CountryComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int indexOf = CountryComboBox.Items.IndexOf(CountryComboBox.SelectedItem);
            CityComboBox.IsEnabled = true;
            CityComboBox.Items.Clear();
            for (int i = 0; i < 4; i++)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = locationTag[indexOf,i];
                CityComboBox.Items.Add(textBlock);
            }
        }
        private void SyncPrayTime()
        {
            FajrTime.Text = prayerTime.GetTimePrayToday("Fajr");
            SunriseTime.Text = prayerTime.GetTimePrayToday("Sunrise");
            DhuhrTime.Text = prayerTime.GetTimePrayToday("Dhuhr");
            AsrTime.Text = prayerTime.GetTimePrayToday("Asr");
            MaghribTime.Text = prayerTime.GetTimePrayToday("Maghrib");
            IshaTime.Text = prayerTime.GetTimePrayToday("Isha");
        }
        private void SyncDateHijri()
        {
            dateHijri.Text = prayerTime.GetDateInHijri();
        }

        private void AdzanTimerPlay()
        {
            int TotalMinutesCurrent = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            if (!AdzanTime)
            {
                if (TimePrayerList["Fajr"] == TotalMinutesCurrent)
                {
                    MainAdzanAlert.Visibility = Visibility.Visible;
                    AdzanTime= true;
                    soundPlayer.Play();
                    notifyIcon.Visible = true;
                    notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                    notifyIcon.ShowBalloonTip(5000, "Time For Praying Fajr", "Let's Go To Pray And Reach Your Jannah ", System.Windows.Forms.ToolTipIcon.None);
                    soundPlayer.MediaEnded += (sender, e) =>
                    {
                        MainAdzanAlert.Visibility = Visibility.Hidden;
                        ManagerRemainder[0] = false;
                        ManagerRemainder[1] = false;
                        AdzanTime = false;
                        soundPlayer.Stop();
                        soundPlayer.Position = TimeSpan.Zero;
                    };
                }
                if (TimePrayerList["Dhuhr"] == TotalMinutesCurrent)
                {
                    MainAdzanAlert.Visibility = Visibility.Visible;
                    AdzanTime = true;
                    soundPlayer.Play();
                    notifyIcon.Visible = true;
                    notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                    notifyIcon.ShowBalloonTip(5000, "Time For Praying Dhuhr", "Let's Go To Pray And Reach Your Jannah ", System.Windows.Forms.ToolTipIcon.None);
                    soundPlayer.MediaEnded += (sender, e) =>
                    {
                        MainAdzanAlert.Visibility = Visibility.Hidden;
                        ManagerRemainder[0] = false;
                        ManagerRemainder[1] = false;
                        AdzanTime = false;
                        soundPlayer.Stop();
                        soundPlayer.Position = TimeSpan.Zero;
                    };
                }
                if (TimePrayerList["Asr"] == TotalMinutesCurrent)
                {
                    MainAdzanAlert.Visibility = Visibility.Visible;
                    AdzanTime = true;
                    soundPlayer.Play();
                    notifyIcon.Visible = true;
                    notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                    notifyIcon.ShowBalloonTip(5000, "Time For Praying Asr", "Let's Go To Pray And Reach Your Jannah ", System.Windows.Forms.ToolTipIcon.None);
                    soundPlayer.MediaEnded += (sender, e) =>
                    {
                        MainAdzanAlert.Visibility = Visibility.Hidden;
                        ManagerRemainder[0] = false;
                        ManagerRemainder[1] = false;
                        AdzanTime = false;
                        soundPlayer.Stop();
                        soundPlayer.Position = TimeSpan.Zero;
                    };
                }
                if (TimePrayerList["Maghrib"] == TotalMinutesCurrent)
                {
                    MainAdzanAlert.Visibility = Visibility.Visible;
                    AdzanTime = true;
                    soundPlayer.Play();
                    notifyIcon.Visible = true;
                    notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                    notifyIcon.ShowBalloonTip(5000, "Time For Praying Maghrib", "Let's Go To Pray And Reach Your Jannah ", System.Windows.Forms.ToolTipIcon.None);
                    soundPlayer.MediaEnded += (sender, e) =>
                    {
                        MainAdzanAlert.Visibility = Visibility.Hidden;
                        ManagerRemainder[0] = false;
                        ManagerRemainder[1] = false;
                        AdzanTime = false;
                        soundPlayer.Stop();
                        soundPlayer.Position = TimeSpan.Zero;
                    };
                }
                if (TimePrayerList["Isha'"] == TotalMinutesCurrent)
                {
                    MainAdzanAlert.Visibility = Visibility.Visible;
                    AdzanTime = true;
                    soundPlayer.Play();
                    notifyIcon.Visible = true;
                    notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                    notifyIcon.ShowBalloonTip(5000, "Time For Praying Isha'", "Let's Go To Pray And Reach Your Jannah ", System.Windows.Forms.ToolTipIcon.None);
                    soundPlayer.MediaEnded += (sender, e) =>
                    {
                        MainAdzanAlert.Visibility = Visibility.Hidden;
                        ManagerRemainder[0] = false;
                        ManagerRemainder[1] = false;
                        AdzanTime = false;
                        soundPlayer.Stop();
                        soundPlayer.Position = TimeSpan.Zero;
                    };
                }
            }
        }

        private void PopUpRemainder()
        {
            int TotalMinutesCurrent = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            if (!TimeCallerPrayer["Fajr"])
            {
                if (!ManagerRemainder[1])
                {
                    if (TimePrayerList["Fajr"] - TotalMinutesCurrent <= 15)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Fajr Is " + (TimePrayerList["Fajr"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                        ManagerRemainder[1] = true;
                    }
                }
                if (!ManagerRemainder[0])
                {
                    if (TimePrayerList["Fajr"] - TotalMinutesCurrent <= 60)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Fajr Is " + (TimePrayerList["Fajr"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                    }
                }
            }
            if (!TimeCallerPrayer["Dhuhr"])
            {
                if (!ManagerRemainder[1])
                {
                    if (TimePrayerList["Dhuhr"] - TotalMinutesCurrent <= 15)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Dhuhr Is " + (TimePrayerList["Dhuhr"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                        ManagerRemainder[1] = true;
                    }
                }
                if (!ManagerRemainder[0])
                {
                    if (TimePrayerList["Dhuhr"] - TotalMinutesCurrent <= 60)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Dhuhr Is " + (TimePrayerList["Dhuhr"]-TotalMinutesCurrent)+" Minute(s) More","Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                    }
                }
            }
            if (!TimeCallerPrayer["Asr"])
            {
                if (!ManagerRemainder[1])
                { 
                    if (TimePrayerList["Asr"] - TotalMinutesCurrent <= 15)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Asr Is " + (TimePrayerList["Asr"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                        ManagerRemainder[1] = true;
                    }
                }
                if (!ManagerRemainder[0])
                {
                    if (TimePrayerList["Asr"] - TotalMinutesCurrent <= 60)
                    {  
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Asr Is " + (TimePrayerList["Asr"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                    }
                }
            }
            if (!TimeCallerPrayer["Maghrib"])
            {
                if (!ManagerRemainder[1])
                {
                    if (TimePrayerList["Maghrib"] - TotalMinutesCurrent <= 15)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Maghrib Is " + (TimePrayerList["Maghrib"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                        ManagerRemainder[1] = true;
                    }
                }
                if (!ManagerRemainder[0])
                {
                    if (TimePrayerList["Maghrib"] - TotalMinutesCurrent <= 60)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Maghrib Is " + (TimePrayerList["Maghrib"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                    }
                }
            }
            if (!TimeCallerPrayer["Isha'"])
            {
                if (!ManagerRemainder[1])
                {
                    if (TimePrayerList["Isha'"] - TotalMinutesCurrent <= 15)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Isha' Is " + (TimePrayerList["Isha'"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                        ManagerRemainder[1] = true;
                    }
                }
                if (!ManagerRemainder[0])
                {
                    if (TimePrayerList["Isha'"] - TotalMinutesCurrent <= 60)
                    {
                        notifyIcon.Visible = true;
                        notifyIcon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moon.ico"));
                        notifyIcon.ShowBalloonTip(5000, "Time For Isha' Is " + (TimePrayerList["Isha'"] - TotalMinutesCurrent) + " Minute(s) More", "Don't Forget For Doing Your Duty As Muslim ^-^", System.Windows.Forms.ToolTipIcon.None);
                        ManagerRemainder[0] = true;
                    }
                }
            }
        }

        private void SetUpRemainderPrayer()
        {
            int HourConv = 0, MinuteConv = 0, TotalConv = 0;
            int TotalMinutesCurrent = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            

            //Fajr
            string[] Fajr = FajrTime.Text.Split(':');
            int.TryParse(Fajr[0], out HourConv);
            int.TryParse(Fajr[1].Substring(0, 2), out MinuteConv);
            TotalConv = HourConv * 60 + MinuteConv;
            TimePrayerList["Fajr"] = TotalConv;
            if (TotalMinutesCurrent > TotalConv)
            {
                TimeCallerPrayer["Fajr"] = true;
            }

            //Dhuhr
            string[] Dhuhr = DhuhrTime.Text.Split(':');
            int.TryParse(Dhuhr[0], out HourConv);
            int.TryParse(Dhuhr[1].Substring(0,2), out MinuteConv);
            TotalConv = HourConv * 60 + MinuteConv;
            TimePrayerList["Dhuhr"] = TotalConv;
            if (TotalMinutesCurrent > TotalConv)
            {
                TimeCallerPrayer["Dhuhr"] = true;
            }

            //Asr
            string[] Asr = AsrTime.Text.Split(':');
            int.TryParse(Asr[0], out HourConv);
            int.TryParse(Asr[1].Substring(0, 2), out MinuteConv);
            TotalConv = HourConv * 60 + MinuteConv;
            TimePrayerList["Asr"] = TotalConv;
            if (TotalMinutesCurrent > TotalConv)
            {
                TimeCallerPrayer["Asr"] = true;
            }

            //Maghrib
            string[] Maghrib = MaghribTime.Text.Split(':');
            int.TryParse(Maghrib[0], out HourConv);
            int.TryParse(Maghrib[1].Substring(0, 2), out MinuteConv);
            TotalConv = HourConv * 60 + MinuteConv;
            TimePrayerList["Maghrib"] = TotalConv;
            if (TotalMinutesCurrent > TotalConv)
            {
                TimeCallerPrayer["Maghrib"] = true;
            }

            //Isha'
            string[] Isha = IshaTime.Text.Split(':');
            int.TryParse(Isha[0], out HourConv);
            int.TryParse(Isha[1].Substring(0, 2), out MinuteConv);
            TotalConv = HourConv * 60 + MinuteConv;
            TimePrayerList["Isha'"] = TotalConv;
            if (TotalMinutesCurrent > TotalConv)
            {
                TimeCallerPrayer["Isha'"] = true;
            }
        }

        private void SetElementInterface(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (prayerTime.isDataReady())
                {
                    loadingScreen.Visibility = Visibility.Hidden;
                    SyncPrayTime();
                    SyncDateHijri();
                    SetUpRemainderPrayer();
                    PopUpRemainder();
                    AdzanTimerPlay();
                }
                else
                {
                    loadingScreen.Visibility = Visibility.Visible;
                }
            });
        }

        private void SetAsDefaultLocation(object sender, RoutedEventArgs e)
        {
            if (CountryComboBox.Text == string.Empty || CityComboBox.Text == string.Empty)
            {
                MessageBox.Show("Please fill all location requirement", "Warning !!!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                writeMemory = new BinaryWriter(new FileStream("config", FileMode.Create));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Can't create file");
                return;
            }
            try
            {
                writeMemory.Write(themeDefault);
                writeMemory.Write(CountryComboBox.Text);
                writeMemory.Write(CityComboBox.Text);
                Country = CountryComboBox.Text;
                City = CityComboBox.Text;
            }catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Can't write file");
                return;
            }
            writeMemory.Close();
            MainConfirmDefault.Visibility = Visibility.Visible;
        }

        private void ConfirmOKButton(object sender, RoutedEventArgs e)
        {
            MainConfirmDefault.Visibility = Visibility.Hidden;
        }

        private void BackFromChangeLocation(object sender,RoutedEventArgs e)
        {
            Duration duration = new Duration(TimeSpan.FromMilliseconds(250));
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            Storyboard storyboard = new Storyboard();
            Storyboard.SetTarget(doubleAnimation, LocationWindow);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Opacity"));
            doubleAnimation.From = 1;
            doubleAnimation.To = 0;
            storyboard.Children.Add(doubleAnimation);
            storyboard.Completed += (ev, ex) =>
            {
                ChooseLocationPanel.Visibility = Visibility.Hidden;
            };
            storyboard.Begin();
        }
        private void ChangeLocation(object sender, RoutedEventArgs e)
        {
            ChooseLocationPanel.Visibility = Visibility.Visible;
            Duration duration = new Duration(TimeSpan.FromMilliseconds(250));
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            Storyboard storyboard = new Storyboard();
            Storyboard.SetTarget(doubleAnimation, LocationWindow);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Opacity"));
            doubleAnimation.From = 0;
            doubleAnimation.To = 1;
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }

        private void SyncLocationPrayTime(object sender, RoutedEventArgs e)
        {
            if (CountryComboBox.Text == string.Empty || CityComboBox.Text == string.Empty)
            {
                MessageBox.Show("Please fill all location requirement", "Warning !!!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ManagerRemainder[0] = false;
            ManagerRemainder[1] = false;    
            ChooseLocationPanel.Visibility = Visibility.Hidden;
            loadingScreen.Visibility = Visibility.Visible;
            TextBlock CountryName = (TextBlock)CountryComboBox.SelectedItem;
            TextBlock CityName = (TextBlock)CityComboBox.SelectedItem;
            location.Text = CityName.Text + ", " + CountryName.Text;
            prayerTime.CallerSyncPrayerTime(CountryName.Text, CityName.Text, 5);
        }

        private void MouseCursorEnterWindow(object sender, MouseEventArgs e)
        {
            Duration duration = new Duration(TimeSpan.FromMilliseconds(250));
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            Storyboard storyboard = new Storyboard();
            Storyboard.SetTarget(doubleAnimation, TopMenu);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Height"));
            doubleAnimation.From = 0;
            doubleAnimation.To = 50;
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }

        private void MouseCursorLeaveWindow(object sender,MouseEventArgs e)
        {
            Duration duration = new Duration(TimeSpan.FromMilliseconds(250));
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            Storyboard storyboard = new Storyboard();
            Storyboard.SetTarget(doubleAnimation, TopMenu);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Height"));
            doubleAnimation.From = 50;
            doubleAnimation.To = 0;
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                writeMemory = new BinaryWriter(new FileStream("config", FileMode.Create));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Can't create file");
                return;
            }
            try
            {
                writeMemory.Write(themeDefault);
                writeMemory.Write(Country);
                writeMemory.Write(City);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Can't write file");
                return;
            }
            writeMemory.Close();
        }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            Duration duration = new Duration(TimeSpan.FromMilliseconds(2500));
            DoubleAnimation rotate = new DoubleAnimation();
            rotate.Duration = duration;
            rotate.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard storyboard = new Storyboard();
            Storyboard.SetTarget(rotate, (Rectangle)sender);
            Storyboard.SetTargetProperty(rotate, new PropertyPath("(Rectangle.RenderTransform).(RotateTransform.Angle)"));
            rotate.From = 0;
            rotate.To = 360;
            storyboard.Children.Add(rotate);
            storyboard.Begin();
        }
        
    }
}
