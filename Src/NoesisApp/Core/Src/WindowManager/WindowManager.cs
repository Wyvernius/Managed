using Noesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace NoesisApp
{
    public enum WindowManagerLifyCycle
    {
        MainWindowClose,
        LastWindowClose
    }

    public class WindowManager
    {
        static List<Window> _windows = new List<Window>();
        static List<Display> _displays = new List<Display>();
        static List<RenderContext> _contexts = new List<RenderContext>();
        static List<DateTime> _startTime = new List<DateTime>();
        static WindowManagerLifyCycle _lifeCycle = WindowManagerLifyCycle.LastWindowClose;
        static WindowManager _instance;

        T AddWindow<T>(T window) where T : Window
        {
            _windows.Add(window);
            return window;
        }

        T AddContext<T>(T context) where T : RenderContext
        {
            _contexts.Add(context);
            return context;
        }

        T AddDisplay<T>(T display) where T : Display
        {
            _displays.Add(display);
            return display;
        }

        public static WindowManager Instance => _instance;

        public WindowManager(WindowManagerLifyCycle windowManagerLifyCycle = WindowManagerLifyCycle.LastWindowClose, bool vsync = true, bool ppaa = true)
        {
            _instance = this;
            _lifeCycle = windowManagerLifyCycle;
            _vsync = vsync;
            _ppaa = ppaa;
        }

        private bool _vsync = false;
        private bool _ppaa = false;

        public int WindowCount => _displays.Count;
        public Action<Display> WindowClosed;
        public Action<Display> AppClosed;
        public Action OnRender;

        public void EnterMessageLoop(bool RunInBackGround)
        {
            _displays[0].EnterMessageLoop(RunInBackGround);
        }

        public void CreateWindow(Window window, Display display, RenderContext context, ResizeMode resizeMode = ResizeMode.CanResize, WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen, WindowState windowState = WindowState.Normal, WindowStyle windowStyle = WindowStyle.ThreeDBorderWindow)
        {
            Window _window = AddWindow(window);

            Display _display = AddDisplay(display);
            _display.SetResizeMode(resizeMode);
            _display.SetWindowStartupLocation(startupLocation);
            _display.SetWindowState(windowState);
            _display.SetWindowStyle(windowStyle);
            _display.SetWindowType(WindowType.ChildWindow);

            RenderContext _context = AddContext(context);
            _context.Init(_display.NativeHandle, _display.NativeWindow, 1, _vsync, false);

            _window.Init(_display, _context, 1, _ppaa, false, false, 0, 0, 0, 0);

            _display.Render += Render;
            _display.Closed += OnClosed;
            _display.Show();
            _startTime.Add(DateTime.Now);
        }

        public void CreateMainWindow(Window window, Display display, RenderContext context, ResizeMode resizeMode = ResizeMode.CanResize, WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen, WindowState windowState = WindowState.Normal, WindowStyle windowStyle = WindowStyle.ThreeDBorderWindow)
        {
            Window _window = AddWindow(window);
            
            Display _display = AddDisplay(display);
            _display.SetResizeMode(resizeMode);
            _display.SetWindowStartupLocation(startupLocation);
            _display.SetWindowState(windowState);
            _display.SetWindowStyle(windowStyle);
            _display.SetWindowType(WindowType.MainWindow);

            RenderContext _context = AddContext(context);
            _context.Init(_display.NativeHandle, _display.NativeWindow, 1, _vsync, false);

            _window.Init(_display, _context, 1, _ppaa, false, false, 0, 0, 0, 0);

            _display.Render += Render;
            _display.Closed += OnClosed;
            _display.Show();
            _startTime.Add(DateTime.Now);
        }

        void Render(Display display)
        {
            for (int i = 0; i < _displays.Count; i++)
            {
                _windows[i].Render((DateTime.Now - _startTime[i]).TotalSeconds);
            }
            OnRender?.Invoke();
        }

        void OnClosed(Display display)
        {
            int index = _displays.IndexOf(display);
            WindowClosed?.Invoke(_displays[index]);
            bool lastWindowClosed = false;

            // about to close last window.
            if (_displays.Count - 1 == 0)
            {
                AppClosed?.Invoke(_displays[index]);
                lastWindowClosed = true;
            }

            _displays.RemoveAt(index);
            _windows.RemoveAt(index);
            _contexts.RemoveAt(index);
            _startTime.RemoveAt(index);

            if (_lifeCycle == WindowManagerLifyCycle.LastWindowClose && lastWindowClosed)
                return;

            if (_lifeCycle == WindowManagerLifyCycle.MainWindowClose)
            {
                bool MainAlive = false;
                foreach (var disp in _displays)
                {
                    if (disp.WindowType == WindowType.MainWindow)
                        MainAlive = true;
                }
                if (MainAlive)
                    EnterMessageLoop(true);
            }
        }
    }
}
