using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Collections;
using System.Runtime.InteropServices;

using ExtendedWebBrowser2;

namespace Gabtastik
{
    [ComVisible(true)]
    public partial class Form1 : Form /*, COMInterfaces.IOleClientSite */
    {
        string service;
        const string serviceNone = "";
        const string serviceFacebook = "@facebook.com";
        const string serviceGoogle = "@google.com";
        const string serviceMeebo = "@meebo.com";
        
        ExtendedWebBrowser2.ExtendedWebBrowser webView = null;
        ExtendedWebBrowser2.ExtendedWebBrowser facebook = null;
        ExtendedWebBrowser2.ExtendedWebBrowser google = null;
        ExtendedWebBrowser2.ExtendedWebBrowser meebo = null;

        ExtendedWebBrowser2.ExtendedWebBrowser browser = null;

        ToolTip googleLoginHelp = null;
        ToolTip facebookLoginHelp = null;

        bool loginFacebook = false;
        bool loginGoogle = false;

        bool resetFacebook = false;
        bool resetGoogle = false;
        bool resetMeebo = false;

        string facebookTitle = null;
        string googleTitle = null;
        string meeboTitle = null;

        bool ignoreClose = false;
        bool ignoreClick = false;
        bool ignoreResize = false;
        bool redirectToBrowser = false;

        ArrayList facebookPings = new ArrayList();
        ArrayList googlePings = new ArrayList();

        int push = 66;

        Form2 options = new Form2();
        Form3 splash = new Form3();

        [Flags]
        internal enum WindowStyles : int
        {
            ExToolWindow = 0x00000080,
            ExAppWindow = 0x00040000
        };

        public enum INTERNETFEATURELIST
        {
            FEATURE_WEBLOC_POPUPMANAGEMENT = 5,
            FEATURE_LOCALMACHINE_LOCKDOWN = 8,
            FEATURE_DISABLE_NAVIGATION_SOUNDS = 21,
            FEATURE_XMLHTTP = 24,
            FEATURE_AJAX_CONNECTIONSERVICES = 31
            //FEATURE_BLOCK_LMZ_IMG = 0,
            //FEATURE_BLOCK_LMZ_OBJECT = 1,
            //FEATURE_BLOCK_LMZ_SCRIPT = 2
        }

        private const int SET_FEATURE_ON_THREAD = 0x00000001;
        private const int SET_FEATURE_ON_PROCESS = 0x00000002;
        private const int SET_FEATURE_IN_REGISTRY = 0x00000004;
        private const int SET_FEATURE_ON_THREAD_LOCALMACHINE = 0x00000008;
        private const int SET_FEATURE_ON_THREAD_INTRANET = 0x00000010;
        private const int SET_FEATURE_ON_THREAD_TRUSTED = 0x00000020;
        private const int SET_FEATURE_ON_THREAD_INTERNET = 0x00000040;
        private const int SET_FEATURE_ON_THREAD_RESTRICTED = 0x00000080;

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(
             INTERNETFEATURELIST FeatureEntry,
             [MarshalAs(UnmanagedType.U4)] int dwFlags,
             bool fEnable);
        
        public Form1()
        {
            if (Gabtastik.Settings1.Default.DisableScriptDebugging)
            {
                try
                {
                    Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Internet Explorer\\Main", true);
                    if (key != null)
                    {
                        key.SetValue("Disable Script Debugger", "yes", Microsoft.Win32.RegistryValueKind.String);
                        key.Close();
                    }

                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Internet Explorer\\Main", true);
                    if (key != null)
                    {
                        key.SetValue("Disable Script Debugger", "yes", Microsoft.Win32.RegistryValueKind.String);
                        key.Close();
                    }
                }

                catch
                {
                }
            }

            InitializeComponent();

            CoInternetSetFeatureEnabled(INTERNETFEATURELIST.FEATURE_XMLHTTP, SET_FEATURE_ON_PROCESS, true);
            CoInternetSetFeatureEnabled(INTERNETFEATURELIST.FEATURE_WEBLOC_POPUPMANAGEMENT, SET_FEATURE_ON_PROCESS, false);
            CoInternetSetFeatureEnabled(INTERNETFEATURELIST.FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);
            CoInternetSetFeatureEnabled(INTERNETFEATURELIST.FEATURE_AJAX_CONNECTIONSERVICES, SET_FEATURE_ON_PROCESS, true);
 
            Microsoft.Win32.SystemEvents.SessionEnding += new Microsoft.Win32.SessionEndingEventHandler(SystemEvents_SessionEnding);

            //COMInterfaces.IOleObject oleObject = (COMInterfaces.IOleObject)this.extendedWebBrowser1.ActiveXInstance;
            //oleObject.SetClientSite(this);

            notifyIcon1.BalloonTipClicked += new EventHandler(notifyIcon1_BalloonTipClicked);
            notifyIcon1.MouseDown += new MouseEventHandler(notifyIcon1_MouseDown);
            notifyIcon1.MouseUp += new MouseEventHandler(notifyIcon1_MouseUp);
            notifyIcon1.Click += new EventHandler(notifyIcon1_Click);
            label1.DoubleClick += new EventHandler(label1_DoubleClick);

            button1.MouseDown += new MouseEventHandler(button1_MouseDown);
            button1.MouseUp += new MouseEventHandler(button1_MouseUp);

            facebookcomToolStripMenuItem.Click += new EventHandler(ServiceToolStripMenuItem_Click);
            googlecomToolStripMenuItem.Click += new EventHandler(ServiceToolStripMenuItem_Click);
            meebocomToolStripMenuItem.Click += new EventHandler(ServiceToolStripMenuItem_Click);
            facebookcomToolStripMenuItem1.Click += new EventHandler(ServiceToolStripMenuItem_Click);
            googlecomToolStripMenuItem1.Click += new EventHandler(ServiceToolStripMenuItem_Click);
            meebocomToolStripMenuItem1.Click += new EventHandler(ServiceToolStripMenuItem_Click);

            gabToolStripMenuItem.DropDownOpening += new EventHandler(gabToolStripMenuItem_DropDownOpening);

            notifyIcon1.Visible = Gabtastik.Settings1.Default.SystemTrayIcon;
            this.MinimizeBox = (Gabtastik.Settings1.Default.SystemTrayIcon ? false : true);
            this.ShowInTaskbar = (Gabtastik.Settings1.Default.SystemTrayIcon ? false : true);

            int x = Gabtastik.Settings1.Default.MainWindowX;
            int y = Gabtastik.Settings1.Default.MainWindowY;
            int w = Gabtastik.Settings1.Default.MainWindowWidth;
            int h = Gabtastik.Settings1.Default.MainWindowHeight;

            if (w == -1 && h == -1)
            {
                w = 360 + 16;
                h = 403 + push;
            }

            this.Opacity = ((double) Gabtastik.Settings1.Default.MainWindowOpacity / 100.0);
            this.TopMost = Gabtastik.Settings1.Default.MainWindowTopmost;

            this.WindowState = FormWindowState.Normal;
            this.SetDesktopBounds(x, y, w, h);
 
            if (x == -1 && y == -1)
                this.CenterToScreen();

            if (Gabtastik.Settings1.Default.MainWindowMaximized)
                this.WindowState = FormWindowState.Maximized;
            else if (Gabtastik.Settings1.Default.MainWindowMinimized)
                this.WindowState = FormWindowState.Minimized;

            this.Activated += new EventHandler(Form1_Activated);
            this.Deactivate += new EventHandler(Form1_Deactivate);
            this.ResizeEnd += new EventHandler(Form1_ResizeEnd);
            this.LocationChanged += new EventHandler(Form1_LocationChanged);
            this.DoubleBuffered = true;

            ToolTip tips = new ToolTip();
            tips.SetToolTip(label1, Gabtastik.Properties.Resources.TipStatus);
            tips.SetToolTip(button1, Gabtastik.Properties.Resources.TipReload);

            Gabtastik.Settings1.Default.PropertyChanged += new PropertyChangedEventHandler(Default_PropertyChanged);
 
            service = serviceNone;
            extendedWebBrowser1.Name = service;

            webView = extendedWebBrowser1;
            SetupWebView();

            OpenService(Gabtastik.Settings1.Default.DefaultService, false);
         }

        void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            ignoreClose = true;
            this.Close();
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        }

        void gabToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (service.Equals(serviceFacebook))
                logoutToolStripMenuItem.Enabled = !loginFacebook;

            if (service.Equals(serviceGoogle))
                logoutToolStripMenuItem.Enabled = !loginGoogle;
        }

        void ServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem i = (ToolStripMenuItem)sender;
            OpenService(i.Text, false);

            if (i == facebookcomToolStripMenuItem1
                || i == googlecomToolStripMenuItem1
                || i == meebocomToolStripMenuItem1)
            {
                this.Show();
                this.Activate();
            }
        }

        void Form1_Activated(object sender, EventArgs e)
        {
            if (Gabtastik.Settings1.Default.MainWindowGoOpaque && Gabtastik.Settings1.Default.MainWindowOpacity != 100)
                this.Opacity = 1.0;
        }

        void Form1_Deactivate(object sender, EventArgs e)
        {
            if (Gabtastik.Settings1.Default.MainWindowGoOpaque && Gabtastik.Settings1.Default.MainWindowOpacity != 100)
                this.Opacity = ((double)Gabtastik.Settings1.Default.MainWindowOpacity / 100.0);
        }

        void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("MainWindowOpacity"))
                this.Opacity = ((double)Gabtastik.Settings1.Default.MainWindowOpacity / 100.0);

            if (e.PropertyName.Equals("MainWindowTopmost"))
                this.TopMost = Gabtastik.Settings1.Default.MainWindowTopmost;

            if (e.PropertyName.Equals("SystemTrayIcon"))
            {
                notifyIcon1.Visible = Gabtastik.Settings1.Default.SystemTrayIcon;
                this.MinimizeBox = (Gabtastik.Settings1.Default.SystemTrayIcon ? false : true);
                this.ShowInTaskbar = (Gabtastik.Settings1.Default.SystemTrayIcon ? false : true);
            }
        }

        void notifyIcon1_MouseUp(object sender, MouseEventArgs e)
        {
            if (ignoreClick == false)
            {
                this.Show();
                this.Activate();
            }

            ignoreClick = false;
        }

        void notifyIcon1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                ignoreClick = true;
        }

        void notifyIcon1_Click(object sender, EventArgs e)
        {
        }

        void button1_MouseUp(object sender, MouseEventArgs e)
        {
            button1.Image = Gabtastik.Resource1.Reload;
        }

        void button1_MouseDown(object sender, MouseEventArgs e)
        {
            button1.Image = Gabtastik.Resource1.ReloadDown;
        }

        void webView_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            progressBar1.PerformStep();
        }

        void webView_StartNavigate(object sender, BrowserExtendedNavigatingEventArgs e)
        {
            //MessageBox.Show(e.Url.ToString());

            bool serviceIsActive = (service.Equals(webView.Name));
            string url = e.Url.ToString();

            if (facebook != null && url.Contains("channel") && url.Contains("facebook.com"))
            {
                loginFacebook = false;

                if (facebookLoginHelp != null)
                {
                    if (serviceIsActive)
                    {
                        facebookLoginHelp.Hide(this);
                        facebookLoginHelp = null;
                    }
                }
            }
            else if (google != null && loginGoogle)
            {
                if(url.Equals("http://talkgadget.google.com/talkgadget/popout"))
                    return;

                if (url.Contains("google.com") && url.Contains("ifpc_relay"))
                {
                    loginGoogle = false;
  
                    if (googleLoginHelp != null)
                    {
                        googleLoginHelp.Hide(this);
                        googleLoginHelp = null;
                    }

                    return;
                }
                
                if (url.Contains("google.com") && url.Contains("ServiceLoginAuth?"))
                {
                    resetGoogle = true;
                    loginGoogle = false;

                    if (googleLoginHelp != null)
                    {
                        if (serviceIsActive)
                        {
                            googleLoginHelp.Hide(this);
                            googleLoginHelp = null;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.Verb = "open";
                    p.StartInfo.FileName = url;
                    p.Start();

                    e.Cancel = true;
                }
            }
        }

        void webView_StartNewWindow(object sender, BrowserExtendedNavigatingEventArgs e)
        {
            bool serviceIsActive = (service.Equals(webView.Name));
            string url = e.Url.ToString();

            e.Cancel = true;

            if (facebook != null && url.Contains("login.php") && url.Contains("facebook.com"))
            {
                webView.Url = e.Url;
                return;
            }

            if (google != null && url.Contains("ServiceLogin?") && url.Contains("google.com"))
            {
                webView.Url = e.Url;
                return;
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.Verb = "open";
            p.StartInfo.FileName = url;
            p.Start();
        }

        void webView_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //COMInterfaces.IOleControl oleControl = (COMInterfaces.IOleControl)this.extendedWebBrowser1.ActiveXInstance;
            //oleControl.OnAmbientPropertyChange(-5513);

            string url = e.Url.ToString();

            if (url.Equals("about:blank"))
                return;

            ExtendedWebBrowser2.ExtendedWebBrowser thisWebView = (ExtendedWebBrowser2.ExtendedWebBrowser)sender;
            if (thisWebView.Name.Equals(serviceNone))
                return;

            WebReady();
        }

        void webView_DocumentTitleChanged(object sender, EventArgs e)
        {
            ExtendedWebBrowser2.ExtendedWebBrowser thisWebView = (ExtendedWebBrowser2.ExtendedWebBrowser)sender;

            string title = thisWebView.DocumentTitle;
            if (title.Length == 0)
                return;

            bool serviceIsActive = (service.Equals(webView.Name));
            string fullTitle = null;

            if (facebook != null && thisWebView.Name.Equals(serviceFacebook))
            {
                fullTitle = String.Format("{0} > {1}", service, title);

                if (googleLoginHelp != null && serviceIsActive)
                    googleLoginHelp.Hide(this);

                if (title.StartsWith("Login"))
                {
                    fullTitle += " (https)";
                    loginFacebook = true;

                    if (facebookLoginHelp == null && serviceIsActive)
                    {
                        facebookLoginHelp = new ToolTip();
                        facebookLoginHelp.Show(Gabtastik.Properties.Resources.HelpLoginFacebook, this, 150, 30, 3000);
                      }
                }
                else if (title.StartsWith("New message from "))
                {
                    if (Gabtastik.Settings1.Default.MessageNotify)
                    {
                        if (facebookPings.Contains(title) == false)
                        {
                            facebookPings.Add(title);

                            if (notifyIcon1.Visible)
                                notifyIcon1.ShowBalloonTip(10000, serviceFacebook, title, ToolTipIcon.None);
                            else
                                FlashWindow(this.Handle, true);
                        }
                    }
                }

                facebookTitle = fullTitle;
            }
            else if (google != null && thisWebView.Name.Equals(serviceGoogle))
            {
                fullTitle = String.Format("{0} > {1}", service, title);

                if (facebookLoginHelp != null && serviceIsActive)
                    facebookLoginHelp.Hide(this);

                if(title.Equals("Google Accounts")) {
                    fullTitle += " (https)";
                    loginGoogle = true;

                    if(googleLoginHelp == null && serviceIsActive)
                    {
                        googleLoginHelp = new ToolTip();
                        googleLoginHelp.Show(Gabtastik.Properties.Resources.HelpLoginGoogle, this, 150, 30, 3000);
                    }
                }
                else if (title.EndsWith(" says..."))
                {
                    if (Gabtastik.Settings1.Default.MessageNotify)
                    {
                        if (googlePings.Contains(title) == false)
                        {
                            googlePings.Add(title);

                            if (notifyIcon1.Visible)
                                notifyIcon1.ShowBalloonTip(10000, serviceGoogle, title, ToolTipIcon.None);
                            else
                                FlashWindow(this.Handle, true);
                        }
                    }
                }

                googleTitle = fullTitle;
            }

            if (serviceIsActive && fullTitle != null)
                label1.Text = fullTitle;
        }

        void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (resetFacebook && service.Equals(serviceFacebook))
            {
                resetFacebook = false;

                string saveService = service;
                service = serviceNone;

                OpenService(saveService, true);

                browser.Url = new Uri("");
            }
        }

        void SetupWebView()
        {
            webView.StartNavigate += new EventHandler<BrowserExtendedNavigatingEventArgs>(webView_StartNavigate);
            webView.StartNewWindow += new EventHandler<BrowserExtendedNavigatingEventArgs>(webView_StartNewWindow);
            webView.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webView_DocumentCompleted);
            webView.DocumentTitleChanged += new EventHandler(webView_DocumentTitleChanged);
            webView.ProgressChanged += new WebBrowserProgressChangedEventHandler(webView_ProgressChanged);
            webView.GotFocus += new EventHandler(webView_GotFocus);

            webView.Focus();
        }

        void webView_GotFocus(object sender, EventArgs e)
        {
            if (service.Equals(serviceFacebook) && facebookPings.Count > 0)
                facebookPings.Clear();

            if (service.Equals(serviceGoogle) && googlePings.Count > 0)
                googlePings.Clear();

        }

        void OpenService(string serviceName, bool force)
        {
            this.Activate();

            if (serviceName.Equals(service) == false)
            {
                button1.Visible = false;
                progressBar1.Visible = true;
 
                label1.Text = serviceName;

                facebookcomToolStripMenuItem.Checked = facebookcomToolStripMenuItem.Text.Equals(serviceName);
                googlecomToolStripMenuItem.Checked = googlecomToolStripMenuItem.Text.Equals(serviceName);
                meebocomToolStripMenuItem.Checked = meebocomToolStripMenuItem.Text.Equals(serviceName);
                facebookcomToolStripMenuItem1.Checked = facebookcomToolStripMenuItem1.Text.Equals(serviceName);
                googlecomToolStripMenuItem1.Checked = googlecomToolStripMenuItem1.Text.Equals(serviceName);
                meebocomToolStripMenuItem1.Checked = meebocomToolStripMenuItem1.Text.Equals(serviceName);

                if (webView.Name.Equals(serviceNone) == false)
                {
                    bool preloaded = false;
                    ExtendedWebBrowser2.ExtendedWebBrowser newWebView = null;

                    webView.Visible = false;

                    if (serviceName.Equals(serviceFacebook) && facebook != null)
                    {
                        if (facebookTitle != null)
                            label1.Text = facebookTitle;

                        if (facebookLoginHelp != null)
                        {
                            facebookLoginHelp.Show(Gabtastik.Properties.Resources.HelpLoginFacebook, this, 150, 30, 3000);
                        }

                        if (googleLoginHelp != null)
                            googleLoginHelp.Hide(this);

                        newWebView = facebook;
                        preloaded = true;
                    }
                    else if (serviceName.Equals(serviceGoogle) && google != null)
                    {
                        if (googleTitle != null)
                            label1.Text = googleTitle;

                        if (googleLoginHelp != null)
                        {
                            googleLoginHelp.Show(Gabtastik.Properties.Resources.HelpLoginGoogle, this, 150, 30, 3000);
                        }

                        if (facebookLoginHelp != null)
                            facebookLoginHelp.Hide(this);

                        newWebView = google;
                        preloaded = true;
                    }
                    else
                    {
                        newWebView = new ExtendedWebBrowser2.ExtendedWebBrowser();
                        newWebView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                            | System.Windows.Forms.AnchorStyles.Left)
                            | System.Windows.Forms.AnchorStyles.Right)));
                        newWebView.IsWebBrowserContextMenuEnabled = false;
                        newWebView.Location = new System.Drawing.Point(0, 24);
                        newWebView.Margin = new System.Windows.Forms.Padding(0);
                        newWebView.MinimumSize = new System.Drawing.Size(20, 20);
                        newWebView.Name = serviceNone;
                        newWebView.ScriptErrorsSuppressed = true;
                        newWebView.Size = webView.Size;
                        newWebView.TabIndex = 1;
                        newWebView.Url = new System.Uri("", System.UriKind.Relative);

                        this.Controls.Add(newWebView);
                    }

                    newWebView.Visible = true;

                    webView = newWebView;

                    if (preloaded)
                    {
                        if (force == false)
                        {
                            service = serviceName;

                            if(service.Equals(serviceFacebook) && facebookPings.Count > 0)
                                facebookPings.Clear();

                            if(service.Equals(serviceGoogle) && googlePings.Count > 0)
                                googlePings.Clear();

                            WebReady();
                            return;
                        }
                    }
                    else
                        SetupWebView();
                }

                service = serviceName;
                LoadService();
            }
        }

        void LoadService()
        {
            redirectToBrowser = false;

            if (service.Equals(serviceFacebook))
            {
                if (facebook == null)
                    facebook = webView;

                webView.Name = service;
                webView.Url = new Uri("http://www.facebook.com/presence/popout.php");
            }
            else if (service.Equals(serviceGoogle))
            {
                if (google == null)
                    google = webView;

                webView.Name = service;
                webView.Url = new Uri("http://talkgadget.google.com/talkgadget/popout");
            }
        }

        void WebReady()
        {
            if (resetGoogle && service.Equals(serviceGoogle))
            {
                resetGoogle = false;

                string saveService = service;
                service = serviceNone;

                OpenService(saveService, true);
            }

            ignoreResize = true;

            if (service.Equals(serviceFacebook))
            {
                try
                {
                    if (facebook.Document.Body != null)
                    {
                        HtmlElementCollection c = facebook.Document.Body.GetElementsByTagName("div");
                        foreach (HtmlElement h in c)
                        {
                            string att = h.GetAttribute("id");
                            if (att != null && att.Equals("presence_popout_header"))
                            {
                                HtmlElementCollection a = h.GetElementsByTagName("a");
                                if (a.Count > 0)
                                    a[0].SetAttribute("onclick", "null");
                            }
                        }
                    }
                }

                catch
                {
                }

                int min_w = this.MinimumSize.Width - 16;
                int min_h = this.MinimumSize.Height - push;

                if (min_w != 400 || min_h != 403)
                    this.MinimumSize = new System.Drawing.Size(400 + 16, 403 + push);

                if (min_w != 1024 || min_h != 4096)
                    this.MaximumSize = new System.Drawing.Size(1024 + 16, 4096 + push);

                if (loginFacebook)
                    this.Size = new System.Drawing.Size(820 + 16, 403 + push);
                else
                {
                    int w = Gabtastik.Settings1.Default.WidthFacebook - 16;
                    int h = Gabtastik.Settings1.Default.HeightFacebook - push;

                    if (w < 400)
                        w = 400;

                    this.Size = new System.Drawing.Size(w + 16, h + push);
                }
            }
            else if (service.Equals(serviceGoogle))
            {
                int min_w = this.MinimumSize.Width - 16;
                int min_h = this.MinimumSize.Height - push;

                if (min_w != 300 || min_h != 492)
                    this.MinimumSize = new System.Drawing.Size(300 + 16, 492 + push);

                if (min_w != 1024 || min_h != 4096)
                    this.MaximumSize = new System.Drawing.Size(1024 + 16, 4096 + push);

                if (loginGoogle)
                    this.Size = new System.Drawing.Size(820 + 16, 572 + push);
                else
                {
                    int w = Gabtastik.Settings1.Default.WidthGoogle - 16;
                    int h = Gabtastik.Settings1.Default.HeightGoogle - push;

                    if (w < 300)
                        w = 300;

                    this.Size = new System.Drawing.Size(w + 16, h + push);
                }
            }

            ignoreResize = false;

            progressBar1.Visible = false;
            button1.Visible = true;

            Gabtastik.Settings1.Default.DefaultService = service;
            redirectToBrowser = true;
        }

        void SaveFrames()
        {
            if (service.Equals(serviceFacebook) && loginFacebook == false)
            {
                Gabtastik.Settings1.Default.WidthFacebook = this.Size.Width;
                Gabtastik.Settings1.Default.HeightFacebook = this.Size.Height;
            }
            else if (service.Equals(serviceGoogle) && loginGoogle == false)
            {
                Gabtastik.Settings1.Default.WidthGoogle = this.Size.Width;
                Gabtastik.Settings1.Default.HeightGoogle = this.Size.Height;
            }
        }

        void ApplicationExit()
        {
            webView.Stop();

            SaveFrames();

            Gabtastik.Settings1.Default.MainWindowMaximized = (this.WindowState == FormWindowState.Maximized ? true : false);
            Gabtastik.Settings1.Default.MainWindowMinimized = (this.WindowState == FormWindowState.Minimized ? true : false);

            if (this.WindowState == FormWindowState.Normal)
            {
                Gabtastik.Settings1.Default.MainWindowWidth = this.Size.Width;
                Gabtastik.Settings1.Default.MainWindowHeight = this.Size.Height;
            }

            Gabtastik.Settings1.Default.Save();
        }

        void Form1_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                Gabtastik.Settings1.Default.MainWindowX = this.Location.X;
                Gabtastik.Settings1.Default.MainWindowY = this.Location.Y;
            }
        }

        void Form1_ResizeEnd(object sender, EventArgs e)
        {
            if (ignoreResize)
                return;

            SaveFrames();
        }

        void label1_DoubleClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.Verb = "open";
            p.StartInfo.FileName = String.Format("http://www.{0}", service.Substring(1));
            p.Start();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            webView.Focus();
            
            button1.Visible = false;
            progressBar1.Visible = true;

            redirectToBrowser = false;

            if (service.Equals(serviceGoogle))
            {
                string saveService = service;
                service = serviceNone;

                OpenService(saveService, true);
            }
            else
                webView.Refresh();
         }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.Cancel == true)
                return;

            if (
                e.CloseReason == CloseReason.ApplicationExitCall ||
                e.CloseReason == CloseReason.TaskManagerClosing ||
                e.CloseReason == CloseReason.WindowsShutDown)
            {
                ApplicationExit();
                return;
            }
            
            if (ignoreClose == false && Gabtastik.Settings1.Default.SystemTrayIcon)
            {
                if(this.Visible)
                    this.Hide();

                e.Cancel = true;
            }
            else
                ApplicationExit();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int x = this.Location.X + this.Size.Width + 8;
            int y = this.Location.Y + 8;

            Screen thisScreen = Screen.FromControl(this);
            Rectangle screen = thisScreen.Bounds;
            if (x + options.Size.Width > screen.Location.X + screen.Size.Width)
            {
                x = this.Location.X - (options.Size.Width + 8);

                if (x < screen.Location.X)
                    x = screen.Location.X;
            }

            options.TopMost = this.TopMost;
            options.SetDesktopLocation(x, y);
            options.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ApplicationExit();
            ignoreClose = true;
            this.Close();
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ApplicationExit();
            ignoreClose = true;
            this.Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (service.Equals(serviceFacebook) && resetFacebook == false)
            {
                if (browser == null)
                {
                    browser = new ExtendedWebBrowser2.ExtendedWebBrowser();
                    browser.Location = new System.Drawing.Point(0, 0);
                    browser.Margin = new System.Windows.Forms.Padding(0);
                    browser.Name = serviceNone;
                    browser.ScriptErrorsSuppressed = true;
                    browser.Size = new System.Drawing.Size(0, 0);
                    browser.Visible = false;

                    browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(browser_DocumentCompleted);
                 }

                 resetFacebook = true;
                 browser.Url = new Uri("http://www.gabtastik.com/doc/logout_facebook.htm");
              }
              else if (service.Equals(serviceGoogle))
              {
                  resetGoogle = true;
                  webView.Url = new Uri("http://www.google.com/accounts/logout?continue=http://talkgadget.google.com/talkgadget/popout");
              }
          }

        private void progressBar1_Click(object sender, EventArgs e)
        {
        }

        private void aboutGabtastikToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splash.TopMost = this.TopMost;
            splash.Show();
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            webView.Print();
        }

        private void releaseNotesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.Verb = "open";
            p.StartInfo.FileName = "http://www.gabtastik.com/doc/releasenotes_win.htm";
            p.Start();
        }

        private void softwareLicenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.Verb = "open";
            p.StartInfo.FileName = "http://www.gabtastik.com/doc/license_win.htm";
            p.Start();

        }

        private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.Verb = "open";
            p.StartInfo.FileName = "http://www.gabtastik.com/doc/credits_win.htm";
            p.Start();
        }

        private void contactCustomerSupportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.Verb = "open";
            p.StartInfo.FileName = "mailto:support@mesadynamics.com?subject=Gabtastik%20Windows";
            p.Start();

        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://www.gabtastik.com/updates/gabtastik_win.xml");
            req.UserAgent = "Gabtastik/0.2 (Windows)";
            req.Timeout = 10000;

            System.Net.HttpWebResponse res = (System.Net.HttpWebResponse)req.GetResponse();
            System.IO.StreamReader stream = new System.IO.StreamReader(res.GetResponseStream(), Encoding.UTF8);
            string xml = stream.ReadToEnd();

            res.Close();
            stream.Close();

            Cursor = Cursors.Arrow;

            if (xml != null)
            {
                int start = xml.IndexOf("<version>");
                if (start > 0)
                {
                    start += 9;
                    int end = xml.IndexOf("</version>", start);
                    if (end > start)
                    {
                        string v = xml.Substring(start, end - start);
                        int version = 0;

                        try
                        {
                            version = Int32.Parse(v);
                            if (version > 20)
                            {
                                DialogResult dr = MessageBox.Show(Gabtastik.Properties.Resources.UpdateYes, "", MessageBoxButtons.YesNo);
                                if (dr == DialogResult.Yes)
                                {
                                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                                    p.StartInfo.Verb = "open";
                                    p.StartInfo.FileName = "http://www.gabtastik.com/updates/gabtastik_win.htm";
                                    p.Start();
                                }
                            }
                            else
                                MessageBox.Show(Gabtastik.Properties.Resources.UpdateNo);
                        }

                        catch
                        {
                        }
                    }
                }
            }
 
        }

        /*
        [DispId(-5513)]
        public virtual string IDispatch_Invoke_Handler()
        {
            return "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.1) Gecko/20061204 Firefox/2.0.0.1";
        }

        #region IOleClientSite Members

        public int SaveObject()
        {
            return 0;
        }

        public int GetMoniker(int dwAssign, int dwWhichMoniker, out object moniker)
        {
            moniker = this;
            return 0;
        }

        public int GetContainer(out object container)
        {
            container = this;
            return 0;
        }

        public int ShowObject()
        {
            return 0;
        }

        public int OnShowWindow(int fShow)
        {
            return 0;
        }

        public int RequestNewObjectLayout()
        {
            return 0;
        }

        #endregion
        */
    }
}