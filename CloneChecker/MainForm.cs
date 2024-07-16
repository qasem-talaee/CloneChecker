using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CloneChecker
{
    public partial class MainForm : Form
    {
        string FirstDestinationPath = "";
        string SecondDestinationPath = "";
        List<string> FirstLocationDataSource = new List<string>();
        List<string> SecondLocationDataSource = new List<string>();
        public Thread th;
        public bool _finishFlag = false;

        // UI
        public string StatusBarItemProcess = "";
        public string StatusBarItemProgressPercent = "";
        public string LogFilePath = "";

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.txtInput_first_location.Text = "";
            this.txtInput_second_location.Text = "";
            this.lbl_status_bar.Items[0].Text = "";
            this.lbl_status_bar.Items[1].Text = "";

            this.list_view_first.DataSource = FirstLocationDataSource;
            this.list_view_second.DataSource = SecondLocationDataSource;

            this.lbl_count_first.Text = "";
            this.lbl_count_second.Text = "";

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Log")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Log"));
            }
        }

            
        private void btn_start_check_Click(object sender, EventArgs e)
        {
            FirstLocationDataSource.Clear();
            SecondLocationDataSource.Clear();
            this.th = new Thread(new ThreadStart(CheckMethod));
            this.th.Start();            
        }

        public void WriteLog(string message)
        {
            File.AppendAllText(this.LogFilePath, Environment.NewLine + message);
        }

        public long GetSizeOfFilesInFolder(string path)
        {
            long TotalSize = 0;
            List<string> Temp = new List<string>();
            try
            {
                Temp = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).ToList();

                List<FileInfo> files = new List<FileInfo>();
                Temp.ForEach(x=>files.Add(new FileInfo(x)));

                files = files.Where(x => x.Attributes != FileAttributes.System).ToList();
                TotalSize = files.Sum(x => x.Length);

            }
            catch(Exception)
            {
                this.WriteLog("ErrFile : " + path);
            }
            return TotalSize;
        }

        public List<string> GetAllDirectoryList(string path)
        {
            List<string> Result = new List<string>();
            List<string> Temp = new List<string>();

            try
            {
                Temp = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly).ToList();
                Result = Temp.ToList();

                while (Temp.Count() != 0)
                {
                    try
                    {
                        foreach (string dir in Temp.ToList())
                        {
                            try
                            {
                                Result.AddRange(Directory.GetDirectories(dir, "*.*", SearchOption.TopDirectoryOnly).ToList());
                                Temp.AddRange(Directory.GetDirectories(dir, "*.*", SearchOption.TopDirectoryOnly).ToList());
                                Temp.Remove(dir);
                            }
                            catch (Exception)
                            {
                                Temp.Remove(dir);
                                this.WriteLog("ErrDirectory : " + dir);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {

            }
            return Result;
        }

        private void CheckMethod()
        {
            this.FirstDestinationPath = this.txtInput_first_location.Text;
            this.SecondDestinationPath = this.txtInput_second_location.Text;
            this.lbl_status_bar.Items[1].Text = "";

            if (this.FirstDestinationPath == "" || this.SecondDestinationPath == "")
            {
                MessageBox.Show("Your Locations have not setted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                try
                {
                    this.StatusBarItemProcess = "The Process is running! Please wait...";
                    this.StatusBarItemProgressPercent = "";

                    if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Log", DateTime.Now.ToString("yyyyy-MM-dd-HH-mm-ss"))))
                    {
                        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Log", DateTime.Now.ToString("yyyyy-MM-dd-HH-mm-ss")));
                    }
                    this.LogFilePath = Path.Combine(Environment.CurrentDirectory, "Log", DateTime.Now.ToString("yyyyy-MM-dd-HH-mm-ss"), "Log.txt");
                    
                    List<string> FirstDirectoryList = this.GetAllDirectoryList(this.FirstDestinationPath);
                    for (int index = 0; index < FirstDirectoryList.Count; index++)
                    {
                        FirstDirectoryList[index] = FirstDirectoryList[index].Replace(this.FirstDestinationPath, "").Remove(0, 1);
                    }

                    List<string> SecondDirectoryList = this.GetAllDirectoryList(this.SecondDestinationPath);
                    for (int index = 0; index < SecondDirectoryList.Count; index++)
                    {
                        SecondDirectoryList[index] = SecondDirectoryList[index].Replace(this.SecondDestinationPath, "").Remove(0, 1);
                    }

                    decimal TotalProcess = FirstDirectoryList.Count() + SecondDirectoryList.Count();
                    decimal InProcess = 0;
                    //Check Fisrt Des
                    int i = 0;
                    while (i < FirstDirectoryList.Count)
                    {
                        if (!SecondDirectoryList.Contains(FirstDirectoryList[i]))
                        {
                            FirstLocationDataSource.Add(Path.Combine(this.FirstDestinationPath, FirstDirectoryList[i]));
                        }
                        else
                        {
                            long totalSizeFirst = this.GetSizeOfFilesInFolder(Path.Combine(this.FirstDestinationPath, FirstDirectoryList[i]));

                            string desPath = SecondDirectoryList.Find(x => x.Contains(FirstDirectoryList[i]));
                            long totalSizeSecond = this.GetSizeOfFilesInFolder(Path.Combine(this.SecondDestinationPath, desPath));

                            if (totalSizeFirst != totalSizeSecond)
                            {
                                FirstLocationDataSource.Add(Path.Combine(this.FirstDestinationPath, FirstDirectoryList[i]));
                            }
                        }
                        i = i + 1;
                        InProcess = InProcess + 1;
                        this.StatusBarItemProgressPercent = "Progress : " + ((InProcess / TotalProcess) * 100).ToString("F2") + "%";
                    }

                    //Check second Des
                    i = 0;
                    while (i < SecondDirectoryList.Count)
                    {
                        if (!FirstDirectoryList.Contains(SecondDirectoryList[i]))
                        {
                            SecondLocationDataSource.Add(Path.Combine(this.SecondDestinationPath, SecondDirectoryList[i]));
                        }
                        else
                        {
                            long totalSizeFirst = this.GetSizeOfFilesInFolder(Path.Combine(this.SecondDestinationPath, SecondDirectoryList[i]));

                            string desPath = FirstDirectoryList.Find(x => x.Contains(SecondDirectoryList[i]));
                            long totalSizeSecond = this.GetSizeOfFilesInFolder(Path.Combine(this.FirstDestinationPath, desPath));

                            if (totalSizeFirst != totalSizeSecond)
                            {
                                SecondLocationDataSource.Add(Path.Combine(this.SecondDestinationPath, SecondDirectoryList[i]));
                            }
                        }
                        i = i + 1;
                        InProcess = InProcess + 1;
                        this.StatusBarItemProgressPercent = "Progress : " + ((InProcess / TotalProcess) * 100).ToString("F2") + "%";
                        
                    }
                    this.StatusBarItemProcess = "The Process has finished!";
                    this._finishFlag = true;
                }
                catch(Exception ex)
                {
                    this.StatusBarItemProcess = "SomeThing Went Wrong!" + ex.ToString();
                    this._finishFlag = true;
                }
                
            }
        }
        private void btn_clear_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you want to Clear?", "Confirmation", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                this.StatusBarItemProcess = "";
                this.StatusBarItemProgressPercent = "";
                this.txtInput_first_location.Text = "";
                this.txtInput_second_location.Text = "";
                this.FirstDestinationPath = "";
                this.SecondDestinationPath = "";
                this.list_view_first.DataSource = "";
                this.list_view_second.DataSource = "";
                this.lbl_status_bar.Items[0].Text = "";
                this.lbl_status_bar.Items[1].Text = "";
                this.lbl_count_first.Text = "";
                this.lbl_count_second.Text = "";
            }        
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.lbl_status_bar.Items[0].Text = this.StatusBarItemProcess;
            this.lbl_status_bar.Items[1].Text = this.StatusBarItemProgressPercent;
            if (_finishFlag)
            { 
                this.list_view_first.DataSource = "";
                this.list_view_second.DataSource = "";
                this._finishFlag = false;
                this.list_view_first.DataSource = this.FirstLocationDataSource;
                this.list_view_second.DataSource = this.SecondLocationDataSource;
                this.lbl_count_first.Text = "Count is : " + this.FirstLocationDataSource.Count().ToString();
                this.lbl_count_second.Text = "Count is : " + this.SecondLocationDataSource.Count().ToString();
            }

            //if (th != null)
            //{
                //this.Text = th.ThreadState.ToString();
            //}    
        }
    }
}
