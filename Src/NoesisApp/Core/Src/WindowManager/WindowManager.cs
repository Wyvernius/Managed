using Noesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        private List<Window> _windows = new List<Window>();
        private List<Display> _displays = new List<Display>();
        private List<RenderContext> _contexts = new List<RenderContext>();
        private List<DateTime> _startTime = new List<DateTime>();
        private WindowManagerLifyCycle _lifeCycle = WindowManagerLifyCycle.LastWindowClose;
        private IPlatformDisplayProvider _displayProvider;
        private IPlatformRenderContextProvider _contextProvider;
        static  WindowManager _instance;

        private bool _lastWindowClosed = true;
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

        public WindowManager(IPlatformDisplayProvider displayProvider, IPlatformRenderContextProvider contextProvider, WindowManagerLifyCycle windowManagerLifyCycle = WindowManagerLifyCycle.LastWindowClose, bool vsync = true, bool ppaa = true)
        {
            _instance = this;
            _lifeCycle = windowManagerLifyCycle;
            _vsync = vsync;
            _ppaa = ppaa;
            _displayProvider = displayProvider;
            _contextProvider = contextProvider;
        }

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

        /// <summary>
        /// Kick start Application
        /// </summary>
        /// <param name="RunInBackGround"></param>
        public void EnterMessageLoop()
        {
            while (WindowCount > 0)
            {
                if (_lifeCycle == WindowManagerLifyCycle.MainWindowClose)
                {
                    bool hasMinwindow = false;
                    for (int i = 0; i < _displays.Count; i++)
                    {
                        if (_displays[i].WindowType == WindowType.ChildWindow)
                            continue;

                        hasMinwindow = true;
                        EnterMessageLoop(i, _displays[i].WindowType == WindowType.MainWindow ? false : true);
                        break; // return to while loop.
                    }
                    if (!hasMinwindow) // no mainwindows anymore cleanup all other windows.
                    {
                        for (int i = WindowCount - 1; i > -1; i--)
                        {
                            OnClosed(_displays[i]);
                        }
                    }
                }
                else if (_lifeCycle == WindowManagerLifyCycle.LastWindowClose)
                {
                    EnterMessageLoop(0, _displays[0].WindowType == WindowType.MainWindow ? false : true);
                    continue; // return to while loop.
                }
            }
        }

        private void EnterMessageLoop(int index, bool RunInBackGround)
        {
            _displays[index].EnterMessageLoop(RunInBackGround);
        }

        private bool HasProvider()
        {
            return _displayProvider == null || _contextProvider == null;
        }

        public void CreateWindow(Window window, ResizeMode resizeMode = ResizeMode.CanResize, WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen, WindowState windowState = WindowState.Normal, WindowStyle windowStyle = WindowStyle.ThreeDBorderWindow)
        {
            if (!HasProvider())
                throw new Exception("No providers provided");

            CreateWindowInternal(window, _displayProvider.GetDisplay(), _contextProvider.GetRenderContext(), WindowType.ChildWindow, resizeMode, startupLocation, windowState, windowStyle);
        }

        public void CreateMainWindow(Window window, ResizeMode resizeMode = ResizeMode.CanResize, WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen, WindowState windowState = WindowState.Normal, WindowStyle windowStyle = WindowStyle.ThreeDBorderWindow)
        {
            if (!HasProvider())
                throw new Exception("No providers provided");

            CreateWindowInternal(window, _displayProvider.GetDisplay(), _contextProvider.GetRenderContext(), WindowType.MainWindow, resizeMode, startupLocation, windowState, windowStyle);
        }

        private void CreateWindowInternal(Window window, Display display, RenderContext context, WindowType type, ResizeMode resizeMode = ResizeMode.CanResize, WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen, WindowState windowState = WindowState.Normal, WindowStyle windowStyle = WindowStyle.ThreeDBorderWindow)
        {
            _startTime.Add(DateTime.Now);
            Window _window = AddWindow(window);
            Display _display = AddDisplay(display);
            RenderContext _context = AddContext(context);

            _context.Init(_display.NativeHandle, _display.NativeWindow, 1, _vsync, false);
            _window.Init(_display, _context, 1, _ppaa, false, false, 0, 0, 0, 0);

            _display.SetWindowType(type);
            _display.SetResizeMode(resizeMode);
            _display.SetWindowStartupLocation(startupLocation);
            _display.SetWindowStyle(windowStyle);

            _display.Render += Render;
            _display.Closed += OnClosed;
            _display.Show();

            _display.SetWindowState(windowState);
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

            // about to close last window.
            if (_displays.Count - 1 == 0)
            {
                AppClosed?.Invoke(_displays[index]);
            }

            _displays.RemoveAt(index);
            _windows.RemoveAt(index);
            _contexts.RemoveAt(index);
            _startTime.RemoveAt(index);
        }
    }
}
