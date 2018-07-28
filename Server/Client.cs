using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Server
{
    public class Client
    {
        private static string response = string.Empty;

        private TextBox textbox { get; set; }
        private Panel panel { get; set; }
        private readonly string UserId;
        private Socket socketClient;
        private readonly string ip;
        private readonly int port;

        /// <summary>
        /// 连接成功回调
        /// </summary>
        //private delegate void ConnectBackCall(byte[] byteData);

        public Client(TextBox textbox, Panel panel,string userid,string ip,int port)
        {
            this.textbox = textbox;
            this.panel = panel;
            this.UserId = userid;
            this.ip = ip;
            this.port = port;
            //ConnectBackCall connectBackCall = new ConnectBackCall(SendFileBackCall);
        }

        public void StartClient()
        {
            try
            {
                IPHostEntry iPHost = Dns.Resolve(this.ip);
                IPAddress iPAddress = iPHost.AddressList[0];
                IPEndPoint iPEnd = new IPEndPoint(iPAddress, this.port);

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socketClient = client;
                client.BeginConnect(iPEnd, new AsyncCallback(ConnectCallBack), client);
            }
            catch (Exception e)
            {
                Label error = new Label();
                error.Text = "some error1:" + e;
                error.AutoSize = true;
                AddControlToPanel(error);
            }
        }

        private delegate void ConreolDelegete(Control control);

        /// <summary>
        /// 添加控件到Panel容器
        /// </summary>
        /// <param name="newcontrol"></param>
        public void AddControlToPanel(Control newcontrol)
        {
            if (panel.InvokeRequired)
            {
                ConreolDelegete objSet = new ConreolDelegete(AddControlToPanel);
                panel.Invoke(objSet, new object[] { newcontrol });
            }
            else
            {
                newcontrol.Location = CountPoint(newcontrol);
                panel.Controls.Add(newcontrol);
            }
        }

        /// <summary>
        /// 得到容器里控件的最新排列坐标
        /// </summary>
        /// <param name="editpointcontrol">要修改坐标的控件</param>
        /// <returns></returns>
        public Point CountPoint(Control editpointcontrol)
        {
            int count = panel.Controls.Count;
            Control control = new Control();
            if (count != 0)
            {
                control = panel.Controls[count - 1];//得到容器里的最后一个控件(因为所有控件都从上而下排列 所有最后的控件就是Height值最大的控件)
            }

            Point point = new Point(editpointcontrol.Location.X, control.Location.Y + editpointcontrol.Height);
            return point;
        }

        public void ConnectCallBack(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
            }
            catch (Exception e)
            {
                Label error = new Label();
                error.Text = "some error:2" + e;
                error.AutoSize = true;
                AddControlToPanel(error);
            }
        }

        public void Send(string text)
        {
            StartClient();
            DateTime time = DateTime.Now;
            string messageModel = JsonConvert.SerializeObject(new MessageModel { Message = text, UserId = this.UserId, CreateTime = time });
            byte[] byteData= Encoding.UTF8.GetBytes(messageModel);
            this.socketClient.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendBackCall), this.socketClient);

            Label sendmessage = new Label();
            sendmessage.Text = string.Format(text + "     :" + time);
            sendmessage.AutoSize = true;
            sendmessage.Location = new Point(panel.Width - sendmessage.Width * 2, sendmessage.Height);
            AddControlToPanel(sendmessage);
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="files"></param>
        public void SendFile(List<string> fileaddress, string message)
        {
            foreach (var address in fileaddress)
            {
                StartClient();
                try
                {
                    byte[] byteimg = ImgToBytes(address);

                    string jsondata = JsonConvert.SerializeObject(new MessageModel { UserId = this.UserId, Message = message, CreateTime = DateTime.Now, MessageFile = byteimg });
                    byte[] byteData = Encoding.UTF8.GetBytes(jsondata);
                    while (!this.socketClient.Connected) { }
                    SendFileBackCall(byteData);
                }
                catch (Exception e)
                {
                    MessageBox.Show("file error" + e);
                }

            }           
        }

        public void SendFileBackCall(byte[] byteData)
        {           
            this.socketClient.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendBackCall), this.socketClient);
        }

        public byte[] ImgToBytes(string address )
        {
            Image image = Image.FromFile(address);
            ImageFormat format = image.RawFormat;
            using (MemoryStream ms = new MemoryStream())
            {
                if (format.Equals(ImageFormat.Jpeg))
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                else if (format.Equals(ImageFormat.Png))
                {
                    image.Save(ms, ImageFormat.Png);
                }
                else if (format.Equals(ImageFormat.Bmp))
                {
                    image.Save(ms, ImageFormat.Bmp);
                }
                else if (format.Equals(ImageFormat.Gif))
                {
                    image.Save(ms, ImageFormat.Gif);
                }
                else if (format.Equals(ImageFormat.Icon))
                {
                    image.Save(ms, ImageFormat.Icon);
                }
                byte[] buffer = new byte[ms.Length];
                //Image.Save()会改变MemoryStream的Position，需要重新Seek到Begin
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        //public byte[] ImgToBytes(string path)
        //{
        //    FileStream fileStream = new FileStream(path,FileMode.Open);
        //    byte[] ret = new byte[fileStream.Length];
        //    fileStream.Read(ret, 0, ret.Length);

        //    return ret;
        //}

        public Image BytesToImage(byte[] bytes)
        {
            return Image.FromStream(new MemoryStream(bytes));
        }

        public void SendBackCall(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int bytesSent = client.EndSend(ar);

                Receive(client);//开始接收服务器返回的数据
            }
            catch (Exception e)
            {
                Label error = new Label();
                error.Text = "some error:3" + e;
                error.AutoSize = true;
                AddControlToPanel(error);
            }
        }

        public void Receive(Socket client)
        {
            try
            {
                StateObject stateObject = new StateObject();
                stateObject.workSocket = client;

                client.BeginReceive(stateObject.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReceiveBackCall), stateObject);
            }
            catch (Exception e)
            {
                Label error = new Label();
                error.Text = "some error:4" + e;
                error.AutoSize = true;
                AddControlToPanel(error);
            }
        }

        public void ReceiveBackCall(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));

                    client.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReceiveBackCall), state);//检查有没接收完
                }
                else
                {
                    if (state.sb.Length >= 1)
                    {
                        response = state.sb.ToString();//得到返回数据
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();                        
                    }
                }
            }
            catch (Exception e)
            {
                Label error = new Label();
                error.Text = "some error:5" + e;
                error.AutoSize = true;
                AddControlToPanel(error);
            }
        }
    }
}
