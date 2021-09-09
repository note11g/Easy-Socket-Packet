using System;
using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using SocketPacket.PacketSocket;
using SocketPacket.Network;
using Xamarin.Essentials;

namespace EasySocketPacket
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private static PacketSocket socket;

        private static string recentData = ""; //last send data

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // set view event
            FindViewById<Button>(Resource.Id.btn_main_send).Click += Send;
            FindViewById<EditText>(Resource.Id.edit_main).EditorAction += Send;

            socket = new PacketSocket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp
            );

            // set socket event
            socket.ConnectCompleted += new EventHandler<PacketSocketAsyncEventArgs>(EasySocketOnConneted);
            socket.ReceiveCompleted += new EventHandler<PacketSocketAsyncEventArgs>(EasySocketOnReceived);
            socket.DisconnectCompleted += new EventHandler<PacketSocketAsyncEventArgs>(EasySocketOnDisconnected);

            socket.ConnectTimeout(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("132.226.239.170"), 2000), 1000); // try connect
        }

        protected override void OnDestroy()
        {
            if(socket.Connected) socket.Disconnect(true);
            base.OnDestroy();
        }

        private void Send(object sender, EventArgs eventArgs)
        {
            recentData = GetAndClearEditText(); // get and clear editText
            string senderName = FindViewById<EditText>(Resource.Id.edit_main_name).Text;
            if (recentData == "" || senderName == "") // editText is empty
                Toast.MakeText(Application.Context, "send data or sender name is empty", ToastLength.Short).Show();
            else
                SocketPacketStart(recentData, senderName);
        }

        private void SocketPacketStart(string sendText, string senderName)
        {
            if (socket.Connected)
                socket.Send(new StringPacket(new string[] { sendText, senderName })); // send data
            else // socket isn't connected
                Toast.MakeText(Application.Context, "socket isn't connected!", ToastLength.Short).Show();
        }

        private void EasySocketOnConneted(object sender, PacketSocketAsyncEventArgs e)
        {
            // Called when the client contacts the server.
            // e.ConnectSocket <- Connected Socket
            MainThread.BeginInvokeOnMainThread(() => {
                Toast.MakeText(Application.Context, "socket connected!", ToastLength.Short).Show();
                SetConnectStatus(true);
            });
        }

        private void EasySocketOnReceived(object sender, PacketSocketAsyncEventArgs e)
        {
            // Called when a packet is received.
            // e.ReceivePacket <- Received Packet
            Packet packet = e.ReceiveSocket.Receive(); // received packet
            string[] receiveDatas = ((StringPacket)packet).data; // received datas
            string receiveData = receiveDatas[0];
            string receiveSender = receiveDatas[1];

            MainThread.BeginInvokeOnMainThread(() => {
                PutText(receiveSender, receiveData); // display receive data
            });
        }

        private void EasySocketOnDisconnected(object sender, PacketSocketAsyncEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                SetConnectStatus(false);
            });
        }

        private void PutText(string sender, string message)
        {
            TextView tv = FindViewById<TextView>(Resource.Id.txt_main);
            tv.Text += sender + ": " + message + "\n";

            FindViewById<ScrollView>(Resource.Id.scrollView)
                    .FullScroll(Android.Views.FocusSearchDirection.Down); // scroll to bottom
        }

        private string GetAndClearEditText()
        {
            EditText editText = FindViewById<EditText>(Resource.Id.edit_main);
            string returnText = editText.Text;

            editText.Text = ""; // clear editText

            return returnText;
        }

        private void SetConnectStatus(bool connectStatus) {
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            toolbar.Title = connectStatus ? "connected" : "disconnected";
        }
    }
}
