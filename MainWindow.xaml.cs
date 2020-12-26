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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;

namespace WikiWindowSeat
{

    enum REQUEST_ID
    {
        FETCH_LOC
    }

    enum DEFINE_ID
    {
        FETCH_LOC
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FetchLoc
    {
        public double alt;
        public double lat;
        public double lon;
    }

    enum EVENT_ID
    {
        TIMER_1HZ
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SimConnect Sim;
        private Object Marker;

        private const uint WIN32_SIMCONNECT_EVENT = 0x0402;

        public MainWindow()
        {
            InitializeComponent();

            // Intercept our custom Win32 event ID for SimConnect
            IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
            HwndSource router = HwndSource.FromHwnd(hWnd);
            router.AddHook(this.onWindowProc);
        }

        // Intercept our custom Win32 event ID and tell SimConnect to check for incoming messages
        private IntPtr onWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WIN32_SIMCONNECT_EVENT)
            {
                if (Sim != null)
                {
                    // This processes any incoming messages via SimConnect
                    // and dispatches to my callback delegates.
                    // In C++ you'd call SimConnect_Dispatch() in a loop here.
                    Sim.ReceiveMessage();
                }
                handled = true;
            }
            else
            {
                handled = false;
            }
            return IntPtr.Zero;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sim == null)
            {
                StatusLabel.Content = "Connecting...";

                // The connection needs our Win32 window handle to route messages...
                IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
                Sim = new SimConnect("WikiWindowSeat", hWnd, WIN32_SIMCONNECT_EVENT, null, 0);
                StatusLabel.Content = "Connected!";

                // Set up a data record with the user properties we want to fetch
                Sim.AddToDataDefinition(DEFINE_ID.FETCH_LOC, "Plane Altitude", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, 0);
                Sim.AddToDataDefinition(DEFINE_ID.FETCH_LOC, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, 8);
                Sim.AddToDataDefinition(DEFINE_ID.FETCH_LOC, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, 16);
                Sim.RegisterDataDefineStruct<FetchLoc>(DEFINE_ID.FETCH_LOC);

                // Request user position, to be updated every second until we cancel it.
                Sim.OnRecvSimobjectData += this.Sim_RecvSimobjectData;
                Sim.RequestDataOnSimObject(REQUEST_ID.FETCH_LOC, DEFINE_ID.FETCH_LOC, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, 0, 0, 0, 0);

                // Also make a visible connection
                Sim.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 1.0f, SIMCONNECT_EVENT_FLAG.DEFAULT, "Test app connected");
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sim != null)
            {
                StatusLabel.Content = "Disconnecting...";
                Sim.Dispose();
                Sim = null;
                StatusLabel.Content = "Disconnected.";
            }
        }

        private void Sim_RecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == (uint)REQUEST_ID.FETCH_LOC)
            {
                // Do something with the user position.
                // For now just display it in the app UI.
                FetchLoc loc = (FetchLoc)data.dwData[0];
                StatusLabel.Content = String.Format("Got data: {0} {1} {2}", loc.alt, loc.lat, loc.lon);
            }
        }
    }
}
