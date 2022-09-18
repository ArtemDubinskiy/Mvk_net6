using MvkClient;
using MvkClient.Actions;
using MvkClient.Util;
using SharpGL;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MvkLauncher
{
    public partial class FormLauncher : Form
    {
        protected Client client = new Client();
        public FormLauncher()
        {
            client.Initialize();
            InitializeComponent();
            openGLControl1.MouseWheel += OpenGLControl1_MouseWheel;
            client.Draw += Client_Draw;
            client.Closeded += Client_Closeded;
            client.ThreadSend += Client_ThreadSend;
            client.CursorClipBounds += Client_CursorClipBounds;
        }



        private void Client_CursorClipBounds(object sender, CursorEventArgs e)
        {
            if (InvokeRequired) Invoke(new CursorEventHandler(Client_CursorClipBounds), sender, e);
            else Cursor.Clip = e.IsBounds ? Bounds : Rectangle.Empty;
        }
        private void Client_ThreadSend(object sender, ObjectKeyEventArgs e)
        {
            if (InvokeRequired) Invoke(new ObjectKeyEventHandler(Client_ThreadSend), sender, e);
            else client.ThreadReceive(e);
        }

        private void Client_Closeded(object sender, EventArgs e)
        {
            if (InvokeRequired) Invoke(new EventHandler(Client_Closeded), sender, e);
            else Close();
        }

        private void Client_Draw(object sender, EventArgs e)
        {
            if (InvokeRequired) Invoke(new EventHandler(Client_Draw), sender, e);
            else openGLControl1.DoRender();
        }

        #region Form
        /// <summary>
        /// ����������� ����
        /// </summary>
        private void FormLauncher_Deactivate(object sender, EventArgs e) => client.WindowDeactivate();

        /// <summary>
        /// ���� �������
        /// </summary>
        private void FormLauncher_FormClosed(object sender, FormClosedEventArgs e) => WinApi.TimeEndPeriod(1);
        private void openGLControl1_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// ������ �������� ����
        /// </summary>
        private void FormLauncher_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client.WindowClosing())
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// �������� �����
        /// </summary>
        private void FormLauncher_Load(object sender, EventArgs e)
        {
            WinApi.TimeBeginPeriod(1);
            client.WindowLoad();
        }

        #endregion

        private void OpenGLControl1_Enter(object sender, EventArgs e) => client.WindowGLEnter();


        /// <summary>
        /// ������ �������
        /// </summary>
        private void OpenGLControl1_KeyDown(object sender, KeyEventArgs e) { }//

        /// <summary>
        /// ������ ������� � char �������
        /// </summary>
        private void OpenGLControl1_KeyPress(object sender, KeyPressEventArgs e) => client.KeyPress(e.KeyChar);

        /// <summary>
        /// �������� �������
        /// </summary>
        private void OpenGLControl1_KeyUp(object sender, KeyEventArgs e) => client.KeyUp(e.Alt ? 18 : e.KeyValue);


        /// <summary>
        /// ������� ������� �����
        /// </summary>
        private void OpenGLControl1_MouseDown(object sender, MouseEventArgs e)
            => client.MouseDown(ConvertMouseButton(e.Button), e.X, e.Y);

        protected MouseButton ConvertMouseButton(MouseButtons button)
        {
            switch (button)
            {
                case MouseButtons.Left: return MouseButton.Left;
                case MouseButtons.Right: return MouseButton.Right;
                case MouseButtons.Middle: return MouseButton.Middle;
            }
            return MouseButton.None;
        }

        /// <summary>
        /// �������� �����
        /// </summary>
        private void OpenGLControl1_MouseMove(object sender, MouseEventArgs e)
        {
            // ���������� ������ �������
            Point point = new Point(Bounds.Width / 2 + Bounds.X, Bounds.Height / 2 + Bounds.Y);
            int deltaX = MousePosition.X - point.X;
            int deltaY = MousePosition.Y - point.Y;
            if (client.MouseMove(e.X, e.Y, deltaX, deltaY))
            {
                // ���������� ������ � �����
                Cursor.Position = point;
            }
        }

        /// <summary>
        /// �������� ������� �����
        /// </summary>
        private void OpenGLControl1_MouseUp(object sender, MouseEventArgs e)
            => client.MouseUp(ConvertMouseButton(e.Button), e.X, e.Y);

        // <summary>
        /// ���������� ������� �����
        /// </summary>
        private void OpenGLControl1_OpenGLDraw(object sender, RenderEventArgs args) => client.GLDraw();

        /// <summary>
        /// ����������������, ������ ������ OpenGL
        /// </summary>
        private void OpenGLControl1_OpenGLInitialized(object sender, EventArgs e) => client.GLInitialize(openGLControl1.OpenGL);

        /// <summary>
        /// ������ ����������� �������
        /// </summary>
        private void OpenGLControl1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) => client.KeyDown(e.Alt ? 18 : e.KeyValue);

        /// <summary>
        /// �������� �������
        /// </summary>
        private void OpenGLControl1_MouseWheel(object sender, MouseEventArgs e)
            => client.MouseWheel(e.Delta, e.X, e.Y);

        private void OpenGLControl1_Resized(object sender, EventArgs e)
           => client.GLResized(openGLControl1.Size.Width, openGLControl1.Size.Height);


    }
}