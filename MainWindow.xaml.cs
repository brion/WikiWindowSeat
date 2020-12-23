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

                IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
                Sim = new SimConnect("WikiWindowSeat", hWnd, WIN32_SIMCONNECT_EVENT, null, 0);
                StatusLabel.Content = "Connected!";

                Sim.AddToDataDefinition(DEFINE_ID.FETCH_LOC, "Plane Altitude", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, 0);
                Sim.AddToDataDefinition(DEFINE_ID.FETCH_LOC, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, 8);
                Sim.AddToDataDefinition(DEFINE_ID.FETCH_LOC, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, 16);
                Sim.RegisterDataDefineStruct<FetchLoc>(DEFINE_ID.FETCH_LOC);

                Sim.OnRecvEvent += this.Sim_RecvEvent;
                Sim.OnRecvSimobjectDataBytype += this.Sim_RecvSimobjectDataBytype;

                // SHIT YEAH IT WORKS
                Sim.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 1.0f, SIMCONNECT_EVENT_FLAG.DEFAULT, "welcome to fun town");

                Sim.SubscribeToSystemEvent(EVENT_ID.TIMER_1HZ, "1sec");
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

        private void Sim_RecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            if (data.uEventID == (uint)EVENT_ID.TIMER_1HZ)
            {
                // Ask for data about the user
                Sim.RequestDataOnSimObjectType(REQUEST_ID.FETCH_LOC, DEFINE_ID.FETCH_LOC, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                StatusLabel.Content = "Requesting data...";
            }
        }

        private void Sim_RecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            if (data.dwRequestID == (uint)REQUEST_ID.FETCH_LOC)
            {
                FetchLoc loc = (FetchLoc)data.dwData[0];
                StatusLabel.Content = String.Format("Got data: {0} {1} {2}", loc.alt, loc.lat, loc.lon);
            }
        }
    }
}
