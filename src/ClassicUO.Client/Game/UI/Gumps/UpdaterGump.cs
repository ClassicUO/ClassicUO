#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL2;
using System.ComponentModel;
using System.Data;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System;
using System.Collections;
using System.Net;
using System.Threading;
using System.Drawing;
using System.Security.Cryptography;
using System.Security.Policy;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class UpdaterGump : Gump
    {
        //private readonly ushort _buttonNormal;
        //private readonly ushort _buttonOver;
        //private readonly Button _nextArrow0;,

        private readonly Label _durumLabel;

        //private readonly HtmlControl _progressLabel;
       // private Label _progressLabel;

        private ResizePic _progressbar;

        //private static int _progressbarCurrentW = 0;
        private static int _progressbarMaxW = 300;
        private static int _progressbarMinW = 0;

        private float _time;

        private static BackgroundWorker workInBackground;
        public static UpdateStates _update_state = UpdateStates.None;


        public UpdaterGump(LoginScene scene) : base(0, 0)
        {
            CanCloseWithRightClick = false;
            AcceptKeyboardInput = false;
            //_buttonNormal = 0x15A4;
            //_buttonOver = 0x15A5;
            const ushort HUE = 0x0386;


            if (Client.Version >= ClientVersion.CV_500A)
            {
                Add(new GumpPic(0, 0, 0x2329, 0));
            }

            //UO Flag
            Add(new GumpPic(0, 4, 0x15A0, 0) { AcceptKeyboardInput = false });

            //Login Panel
            Add
            (
                new ResizePic(0x13BE)
                {
                    X = 128,
                    Y = 288,
                    Width = 451,
                    Height = 157
                }
            );

            //if (Client.Version < ClientVersion.CV_500A)
            //{
            //Add(new GumpPic(286, 45, 0x058A, 0));
            //}

            Add
            (
                _durumLabel = new Label
                (
                    "-",
                    true,
                    0x040,
                    400,
                    0,
                    FontStyle.BlackBorder,
                    TEXT_ALIGN_TYPE.TS_LEFT,
                    true
                )
                {
                    X = 255,
                    Y = 388
                }
            );

            Add
            (
                _progressbar = new ResizePic(0x0BB8)
                {
                    X = 253,
                    Y = 340,
                    Width = _progressbarMinW,
                    Height = 30
                }
            );

            /*
            Add
            (
                _progressLabel = new Label
                (
                    "-",
                    false,
                    0x0,
                    900,
                    0,
                    FontStyle.None,
                    TEXT_ALIGN_TYPE.TS_LEFT,
                    false
                )
                {
                    X = 260,
                    Y = 345
                }
            );
            */
            /*
            Add
            (
                _progressLabel = new HtmlControl(260, 345, _progressbarMaxW, 30, false, false, false, "-")
            );
            */

            workInBackground = new BackgroundWorker();
            workInBackground.WorkerReportsProgress = true;
            //Update_state = UpdateStates.Ready;
            workInBackground.DoWork += new DoWorkEventHandler(UpdateTheClient);
            workInBackground.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateTheClientCompleted);
            workInBackground.ProgressChanged += new ProgressChangedEventHandler(UpdateTheClientProgressChanged);
            _durumLabel.Text = "Güncelleme kontrol ediliyor...";

            if (workInBackground.IsBusy && Update_state != UpdateStates.Ready)
            {
                _durumLabel.Text = "Güncelleme iş parçacığı şu an meşgul!";
                return;
            }
            else
            {
                Update_state = UpdateStates.Updating;
                workInBackground.RunWorkerAsync();
            }


            Add
            (
                new Label("Dosya Guncelleme", false, HUE, font: 2)
                {
                    X = 253,
                    Y = 305
                }
            );

            Add
            (
                new Label("Dosya:", false, HUE, font: 2)
                {
                    X = 183,
                    Y = 345
                }
            );

            Add
            (
                new Label("Durum:", false, HUE, font: 2)
                {
                    X = 183,
                    Y = 385
                }
            );

        }

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Update();

            if (_time < Time.Ticks)
            {
                _time = (float)Time.Ticks + 500;

                if (_progressbar.Width >= 100) {
                    _progressbar.Width = _progressbarMaxW;
                }

                _progressbar.Width = _progressbarMaxW;
                //_nextArrow0.ButtonGraphicNormal = _nextArrow0.ButtonGraphicNormal == _buttonNormal ? _buttonOver : _buttonNormal;
            }

        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.NextArrow:
                    //if (!_textboxAccount.IsDisposed)
                    //{
                    //Client.Game.GetScene<LoginScene>().Connect(_textboxAccount.Text, _passwordFake.RealText);
                    //}

                    break;

            }
        }


        private enum Buttons
        {
            NextArrow
        }


        public enum Durum
        {
            Bilgi,
            Uyari,
            Hata
        }

        public enum UpdateStates
        {
            None,
            Ready,
            Updating,
            Downloading,
            FinishedDownloading,
            Error
        }


        public UpdateStates Update_state
        {
            get { return _update_state; }
            set
            {
                _update_state = value;

                switch (_update_state)
                {
                    case UpdateStates.Ready:
                        Console.WriteLine("Hazır.");
                        
                        LoginScene ls = Client.Game.GetScene<LoginScene>();
                        if (ls.CurrentLoginStep == LoginSteps.Updater)
                        {
                            ls.CurrentLoginStep = LoginSteps.Main;
                        }
                        
                        _durumLabel.Text = "Hazır.";
                        break;

                    case UpdateStates.Updating:
                        Console.WriteLine("Güncelleniyor...");
                        _durumLabel.Text = "Güncelleniyor...";
                        break;

                    case UpdateStates.Downloading:
                        Console.WriteLine("İndiriliyor...");
                        _durumLabel.Text = "İndiriliyor...";
                        break;

                    case UpdateStates.FinishedDownloading:
                        Console.WriteLine("İndirme tamamlandı.");
                        _durumLabel.Text = "İndirme tamamlandı.";
                        break;

                    case UpdateStates.Error:
                        Console.WriteLine("Güncelleme hatası.");
                        _durumLabel.Text = "Güncelleme hatası";
                        break;

                    case UpdateStates.None:
                        Console.WriteLine("Güncelleme yok.");
                        _durumLabel.Text = "Güncelleme yok";
                        break;

                    default:
                        break;
                }
            }
        }

        public class ReportStatus
        {
            public string Message;
            public Durum Durum;

            public ReportStatus(Durum durum, string message)
            {
                Durum = durum;
                Message = message;
            }
        }

        public void UpdateTheClient(object sender, DoWorkEventArgs e)
        {
            try
            {
                Console.WriteLine("Güncelleme bilgisi getiriliyor...");
                _durumLabel.Text = "Güncelleme bilgisi getiriliyor...";

               ((BackgroundWorker)sender).ReportProgress(1, new ReportStatus(Durum.Bilgi, "Güncelleme bilgisi getiriliyor"));
                string updatesListRaw = DownloadUpdatesListRaw();

                if (updatesListRaw == null)
                {
                    _durumLabel.Text = "Güncelleme bilgisi okunamadı";
                    ((BackgroundWorker)sender).ReportProgress(0, new ReportStatus(Durum.Hata, "Güncelleme bilgisi okunamadı"));
                    return;
                }

                foreach (var line in updatesListRaw.Split('\n'))
                {
                    if (line.Trim().Length == 0)
                        continue;

                    string[] fileInfoReceived = line.Trim().Split(',');
                    Dictionary<string, string> remoteFileInfo = new Dictionary<string, string>();

                    remoteFileInfo.Add("filename", fileInfoReceived[0]);
                    remoteFileInfo.Add("size", fileInfoReceived[1]);
                    remoteFileInfo.Add("crc32b", fileInfoReceived[2].ToLower(CultureInfo.InvariantCulture));
                    remoteFileInfo.Add("local_filename", GetLocalFileName(remoteFileInfo["filename"]));
                    ((BackgroundWorker)sender).ReportProgress(1, new ReportStatus(Durum.Bilgi, "Kontrol Edilen Dosya:" + Path.GetFileName(remoteFileInfo["local_filename"])));

                    if (IsLocalFileNeedsUpdating(remoteFileInfo))
                    {
                       _durumLabel.Text = remoteFileInfo["filename"] + " dosyasının güncellenmesi gerekiyor...";

                       Update_state = UpdateStates.Downloading;

                        ((BackgroundWorker)sender).ReportProgress(1, remoteFileInfo);
                        while (Update_state == UpdateStates.Downloading)
                        {
                            Thread.Sleep(150);
                            //Console.WriteLine("İndirmenin bitmesi bekleniyor...");
                        }

                        //Console.WriteLine("İndirilen dosya: " + remoteFileInfo["filename"]);
                        _durumLabel.Text = "İndirilen dosya: " + remoteFileInfo["filename"];

                        if (File.Exists(remoteFileInfo["local_filename"]))
                        {
                            _durumLabel.Text = "Eski dosya siliniyor: " + remoteFileInfo["local_filename"];
                            Console.WriteLine("Eski dosya siliniyor: " + remoteFileInfo["local_filename"]);
                            File.Delete(remoteFileInfo["local_filename"]);
                        }

                        //var local_downloaded_filename = "/";
                        var local_downloaded_filename = Path.Combine(CUOEnviroment.ExecutablePath, remoteFileInfo["filename"]);

                        //Console.WriteLine("renaming {0} -> {1}", local_downloaded_filename + ".part", local_downloaded_filename);
                        File.Move(local_downloaded_filename + ".part", local_downloaded_filename);

                        if (isZip(remoteFileInfo["filename"]))
                        {
                            Console.WriteLine("Dosya çıkartılıyor: " + remoteFileInfo["filename"]);
                            _durumLabel.Text = "Dosya çıkartılıyor: " + remoteFileInfo["filename"];
                            unzipFile(local_downloaded_filename);
                            File.Delete(local_downloaded_filename);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata Oluştu: " + ex.Message);
                _durumLabel.Text = "Hata Oluştu: " + ex.Message;

                //MessageBox.Show("Hata Oluştu: " + ex.Message, "HATA", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                ((BackgroundWorker)sender).ReportProgress(0, new ReportStatus(Durum.Hata, "Hata Oluştu: " + ex.Message));
            }
        }

        public void UpdateTheClientCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Update_state != UpdateStates.Error)
            {
                Update_state = UpdateStates.Ready;
            }
        }

        public void UpdateTheClientProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is ReportStatus durum)
            {
                switch (durum.Durum)
                {
                    case Durum.Bilgi:
                        break;
                    case Durum.Uyari:
                        break;
                    case Durum.Hata:
                        Update_state = UpdateStates.Error;
                        break;
                    default:
                        break;
                }
            }
            else if (e.UserState is IDictionary)
            {
                var remoteFileInfo = (Dictionary<string, string>)e.UserState;
                //Console.WriteLine("Downloading {0}", remoteFileInfo["filename"]);
                downloadFile(remoteFileInfo["filename"]);

            }
        }

        public string DownloadUpdatesListRaw()
        {
            var client = new WebClient();

            try
            {
                string ret = client.DownloadString(new Uri(Constants.WEB_UPDATER_PATH_URL, "list_updates.php"));
                return ret;
            }
            catch (WebException ex)
            {
                _durumLabel.Text = "Hata: " + ex.Message + "\n" + ex.Status.ToString();
                Console.WriteLine(ex.Message + "\n" + ex.Status.ToString());
            }
            return null;
        }

        public string GetLocalFileName(string remote_filename)
        {
            string local_filename = null;

            if (Path.GetExtension(remote_filename) == ".zip") // remove .zip extention
            {
                local_filename = remote_filename.Substring(0, remote_filename.Length - 4);
            }
            local_filename = Path.Combine(CUOEnviroment.ExecutablePath, local_filename);

            return local_filename;
        }

        public bool IsLocalFileNeedsUpdating(Dictionary<string, string> remoteFileInfo)
        {

            string local_filename = remoteFileInfo["local_filename"];
            _durumLabel.Text = "Dosya kontrolü: " + Path.GetFileName(local_filename).ToLower();

            if (!File.Exists(local_filename))
            {
                Console.WriteLine("Dosya bulunamadı: " + remoteFileInfo["filename"]);
                _durumLabel.Text = "Dosya bulunamadı: " + remoteFileInfo["filename"];
                return true;
            }

            var localfile_size = (new FileInfo(local_filename)).Length;

            if (localfile_size != int.Parse(remoteFileInfo["size"]))
            {
                Console.WriteLine("Dosya boyutu eşleşmedi: " + remoteFileInfo["size"]);
                _durumLabel.Text = "Dosya boyutu eşleşmedi: " + remoteFileInfo["size"];
                return true;
            }

            string local_crc32;
            try
            {
                local_crc32 = getFileCrc32(local_filename);
            }
            catch (IOException)
            {
                Console.WriteLine();
                _durumLabel.Text = string.Format("{0} başka bir uygulama tarafından kullanıldığı için güncellenemedi!", Path.GetFileName(local_filename));
                return false;
            }

            if ((remoteFileInfo["crc32b"] != local_crc32) && (remoteFileInfo["crc32b"] != local_crc32.Substring(1) || local_crc32[0] != '0'))
            {
                _durumLabel.Text = "Uyumsuz hash kodu!" + Path.GetFileName(local_filename);
                Console.WriteLine("Uyumsuz hash kodu!" + Path.GetFileName(local_filename));
                return true;
            }

            return false;
        }

        public void downloadFile(string remote_filename)
        {
            _durumLabel.Text = remote_filename + " dosyası indiriliyor...";
            string local_path = Path.Combine(CUOEnviroment.ExecutablePath, remote_filename) + ".part";
            var client = new WebClient();
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            client.DownloadFileCompleted += client_DownloadFileCompleted;

            client.DownloadFileAsync(new Uri(Constants.WEB_UPDATER_PATH_URL, remote_filename), remote_filename + ".part");
        }

        public  void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Update_state = UpdateStates.FinishedDownloading;
            return;
        }

        public  void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            int val = (int)((e.BytesReceived * 100) / e.TotalBytesToReceive);
            _progressbar.Width = val;

            //if (val > 0 && val <= 100)
            //{
                //_progressLabel.Text = "% " + val.ToString();
               // Console.WriteLine(val.ToString());
            //}

            //_progressbar.Width = val;

        }

        #region ZipHangling
        public static bool isZip(string filename)
        {
            if (Path.GetExtension(filename).ToLower() == ".zip")
            {
                return true;
            }
            return false;
        }

        public void unzipFile(string archive_name)
        {
            ZipStorer zip = ZipStorer.Open(archive_name, FileAccess.Read);
            string local_filename = null;
            if (Path.GetExtension(archive_name) == ".zip") // remove .zip extention
            {
                local_filename = Path.GetFileName(archive_name.Substring(0, archive_name.Length - 4));
            }
            List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

            foreach (ZipStorer.ZipFileEntry entry in dir)
            {
                string zippedFilename = Path.GetFileName(entry.FilenameInZip);
                //Console.WriteLine("Unzip zippedFilename: {0}", zippedFilename);
                //Console.WriteLine("Unzip local_file: {0}", local_filename);
                if (!zippedFilename.Equals(local_filename, StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Unzip skipping file: {0}", zippedFilename);
                    continue;
                }
                zip.ExtractFile(entry, Path.Combine(Path.GetDirectoryName(archive_name), zippedFilename));
            }
            zip.Dispose();
        }
        #endregion


        #region Hasing
        public static string getFileCrc32(string filename)
        {
            return getFileCrc32(filename, null);
        }

        public static string getFileCrc32(string filename, BackgroundWorker sender)
        {
            Crc32 crc32 = new Crc32();
            string hash = string.Empty;

            using (FileStream fs = File.Open(filename, FileMode.Open))
                foreach (byte b in crc32.ComputeHash(fs)) hash += b.ToString("x2").ToLower();
            return hash.ToLower(CultureInfo.InvariantCulture);
        }
        #endregion



    }
}