using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DDiary.ViewModels;
using Microsoft.UI.Xaml.Hosting;

namespace DDiary.Views
{
    public partial class MainWindow : Window
    {
        // ── WinUI 3 XAML Island ──────────────────────────────────────────────────

        /// <summary>COM interface necessaria per agganciare DesktopWindowXamlSource a una finestra Win32/WPF.</summary>
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3cbcf1bf-2f76-4e9c-96ab-e84b37972554")]
        private interface IDesktopWindowXamlSourceNative
        {
            void AttachToWindow([In] IntPtr parentWindowHandle);
            IntPtr WindowHandle { [return: MarshalAs(UnmanagedType.SysInt)] get; }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOZORDER   = 0x0004;
        private const int  SW_HIDE        = 0;
        private const int  SW_SHOW        = 5;

        private DesktopWindowXamlSource? _xamlSource;
        private Microsoft.UI.Xaml.Controls.TimePicker? _timePicker;
        private IntPtr _islandHwnd = IntPtr.Zero;

        /// <summary>Orario del pasto selezionato tramite il TimePicker nativo WinUI 3.</summary>
        public TimeSpan OraPasto { get; private set; }

        // ── Costruttore ──────────────────────────────────────────────────────────

        public MainWindow()
        {
            InitializeComponent();

            // Wire export element provider + VM init
            Loaded += async (_, _) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.DiaryElementProvider = () => DiaryViewControl;
                    await vm.InitializeAsync();

                    // Sottoscrivi ai cambi di IsInsertPanelOpen per mostrare/nascondere l'island
                    vm.PropertyChanged += OnMainViewModelPropertyChanged;
                }

                InitTimePickerIsland();
            };

            // Keep IsFullscreen in sync when the user restores via OS controls (only when not in true fullscreen)
            StateChanged += (_, _) =>
            {
                if (DataContext is MainViewModel vm && !vm.IsFullscreen)
                    vm.IsFullscreen = WindowState == System.Windows.WindowState.Maximized;
            };

            // Riposiziona l'island quando la finestra si sposta o cambia dimensione
            LocationChanged  += (_, _) => RepositionIsland();
            SizeChanged      += (_, _) => RepositionIsland();
        }

        // ── Inizializzazione XAML Island ─────────────────────────────────────────

        private void InitTimePickerIsland()
        {
            var parentHwnd = new WindowInteropHelper(this).Handle;
            if (parentHwnd == IntPtr.Zero) return;

            // Crea il DesktopWindowXamlSource e aggancialo alla finestra WPF padre
            _xamlSource = new DesktopWindowXamlSource();
            // Il cast (IDesktopWindowXamlSourceNative)(object) è necessario perché
            // DesktopWindowXamlSource è un oggetto WinRT: il CLR intercetta la QI COM
            // tramite il wrapper RCW solo se si passa prima per object.
            var native = (IDesktopWindowXamlSourceNative)(object)_xamlSource;
            native.AttachToWindow(parentHwnd);
            _islandHwnd = native.WindowHandle;

            // Crea il TimePicker nativo WinUI 3 in formato 24h
            _timePicker = new Microsoft.UI.Xaml.Controls.TimePicker
            {
                ClockIdentifier = "24HourClock"
            };
            _timePicker.TimeChanged += OnTimePickerTimeChanged;

            // Assegna il TimePicker come contenuto dell'island
            _xamlSource.Content = _timePicker;

            // L'island è inizialmente nascosta; sarà mostrata all'apertura del pannello
            ShowWindow(_islandHwnd, SW_HIDE);
        }

        // ── Sincronizzazione visibilità island con l'overlay ─────────────────────

        private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainViewModel.IsInsertPanelOpen)) return;

            if (sender is MainViewModel vm)
            {
                if (vm.IsInsertPanelOpen)
                {
                    // Il pannello si apre: sincronizza l'orario dal ViewModel e mostra l'island
                    SyncTimePickerFromViewModel(vm);
                    ShowIsland(true);
                }
                else
                {
                    ShowIsland(false);
                }
            }
        }

        private void SyncTimePickerFromViewModel(MainViewModel vm)
        {
            if (_timePicker == null) return;
            // Usa l'orario del ViewModel se disponibile, altrimenti l'ora corrente
            var mealTime = vm.FoodInsertVM?.MealTime ?? DateTime.Now.TimeOfDay;
            _timePicker.Time = mealTime;
        }

        private void ShowIsland(bool show)
        {
            if (_islandHwnd == IntPtr.Zero) return;
            if (show)
            {
                RepositionIsland();
                ShowWindow(_islandHwnd, SW_SHOW);
            }
            else
            {
                ShowWindow(_islandHwnd, SW_HIDE);
            }
        }

        // ── Posizionamento HWND island sopra TimePickerHost ──────────────────────

        private void RepositionIsland()
        {
            if (_islandHwnd == IntPtr.Zero || !IsLoaded || !TimePickerHost.IsLoaded) return;

            // Converti coordinate logiche WPF in pixel fisici (DPI-aware)
            var pos = TimePickerHost.TransformToAncestor(this)
                                    .Transform(new System.Windows.Point(0, 0));

            var presentationSource = PresentationSource.FromVisual(this);
            double dpiX = 1.0, dpiY = 1.0;
            if (presentationSource?.CompositionTarget != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }

            int x = (int)(pos.X * dpiX);
            int y = (int)(pos.Y * dpiY);
            int w = (int)(TimePickerHost.ActualWidth  * dpiX);
            int h = (int)(TimePickerHost.ActualHeight * dpiY);

            SetWindowPos(_islandHwnd, IntPtr.Zero, x, y, w, h,
                         SWP_NOACTIVATE | SWP_NOZORDER);
        }

        // ── Evento TimeChanged dal TimePicker nativo ─────────────────────────────

        private void OnTimePickerTimeChanged(object? sender,
            Microsoft.UI.Xaml.Controls.TimePickerValueChangedEventArgs args)
        {
            OraPasto = args.NewTime;

            // Aggiorna anche il ViewModel in modo che il salvataggio usi l'orario nativo
            if (DataContext is MainViewModel vm)
                vm.FoodInsertVM?.SetMealTime(OraPasto);
        }

        // ── Overlay mouse-down ────────────────────────────────────────────────────

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.CloseInsertPanelCommand.Execute(null);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────────

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.PropertyChanged -= OnMainViewModelPropertyChanged;

            _xamlSource?.Dispose();
            base.OnClosed(e);
        }
    }
}
