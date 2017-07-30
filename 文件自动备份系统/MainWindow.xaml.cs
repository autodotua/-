using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;

namespace 自动备份系统
{

    public class TaskInfo
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public string OriginalDirectories { get; internal set; }
        public string TargetDirectories { get; internal set; }
        public string Interval { get; internal set; }
        public string State { get; internal set; }
        public string LastResult { get; internal set; }
    }

    public class XMLLog
    {
        public string Time { get; internal set; }
        public string Event { get; internal set; }

    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 字段属性声明
        public MainWindow()
        {
            InitializeComponent();
            //TaskData = new ObservableCollection<TaskInfo>();//实例化数据
            //TaskData.Add(new { Name = "NextTime",NextTime = "fsdfa" });

            lvwTasks.DataContext = TaskData;//绑定任务列表数据
            lvwLog.DataContext = LogData;//绑定日志数据

        }
        public StringBuilder log = new StringBuilder();//“当前日志”中显示的内容，由备份线程控制
        Thread backupThread;//备份线程
        int currentTaskIndex;//正在进行哪一个任务的备份
        public ObservableCollection<TaskInfo> TaskData = new ObservableCollection<TaskInfo>();//需要绑定的数据
        public ObservableCollection<XMLLog> LogData = new ObservableCollection<XMLLog>();

        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);//配置项
        public List<string> itemsName = new List<string>();//任务名称列表
        public List<int> itemsLastTime = new List<int>();//任务距离下一次时间列表
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();//定时器
        public int CurrentBackupThreads = 0;//用于判断是否正在备份
        public string CurrentFileCount = "";//用于显示状态，由备份线程控制
        bool pauseTimer = false;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        BackupCore bc;

        #endregion 字段属性声明

        private void MainWindowLoadedEventHandler(object sender, RoutedEventArgs e)
        {
            string tempFileName = Path.GetTempFileName();
            FileStream fs = new FileStream(tempFileName, FileMode.Create);
            Properties.Resources.icon.Save(fs);
            fs.Close();

            //设置托盘的各个属性
            notifyIcon = new System.Windows.Forms.NotifyIcon()
            {
                BalloonTipText = "设置界面在托盘",
                Text = "文件自动备份系统",
                Icon = new System.Drawing.Icon(tempFileName),
                Visible = true
            };
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(NotifyIconClickEventHandler);

            System.Windows.Forms.MenuItem miExit = new System.Windows.Forms.MenuItem("退出");
            miExit.Click += new EventHandler(delegate (object sender3, EventArgs e3)
            {
                notifyIcon.Visible = false;
                Application.Current.Shutdown();
            });
            System.Windows.Forms.MenuItem miPause = new System.Windows.Forms.MenuItem("暂停计时");
            miPause.Click += new EventHandler(delegate (object sender2, EventArgs e2) { pauseTimer = !pauseTimer; });

            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { miPause, miExit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);
            this.Icon = new BitmapImage(new Uri(tempFileName));

            if (cfa.AppSettings.Settings["AutoMinimum"].Value == "true")
            {
                cbxMinimum.IsChecked = true;
            }

            if (System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\FileBackuper.lnk"))
            {
                cbxStartup.IsChecked = true;
            }


            LoadLog();
            RefreshLog();
            RefreshListView();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();

        }

        private void NotifyIconClickEventHandler(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (WindowState != WindowState.Minimized)
                {
                    //ShowInTaskbar = false;
                    WindowState = WindowState.Minimized;
                    Hide();
                }
                else
                {
                    //ShowInTaskbar = true;

                    //WindowState = WindowState.Maximized;
                    WindowState = WindowState.Normal;

                    //Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                    Show();
                    //Arrange(new Rect(DesiredSize));
                    Activate();
                    //不知道为什么，试了一个小时还是没法把窗口调出来，然后发现回车键可以，就模拟一下了
                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                }
            }
            // this.Margin = new Thickness(300);

            // }
            //if (this.Visibility == Visibility.Visible)
            //{
            //    this.WindowState = WindowState.Minimized;
            //    this.Visibility = Visibility.Hidden;
            //}
            //else
            //{
            //    this.WindowState = WindowState.Normal;
            //    this.Visibility = Visibility.Visible;
            //    this.Activate();
            //}
            //  }
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            if (CurrentBackupThreads == 0)
            {
                stopAll.IsEnabled = false;
            }
            for (int i = 0; i < itemsName.Count; i++)
            {

                if (itemsLastTime[i] == 0)//如果时间到了
                {
                    if (CurrentBackupThreads == 0)//如果没有正在备份
                    {
                        if (!Directory.Exists(TaskData[i].TargetDirectories))//如果不存在目标目录
                        {
                            try
                            {
                                //尝试去创建
                                Directory.CreateDirectory(TaskData[i].TargetDirectories);
                            }
                            catch
                            {

                                itemsLastTime[i/*itemsName.IndexOf(itemsName[i])*/] = int.Parse(cfa.AppSettings.Settings[itemsName[i] + "_Interval"].Value);
                                AppendLog(itemsName[i], "目标目录不存在且无法创建，将在下一个周期重试。");
                                txtLogPanel.Text = log.ToString();
                                lvwTasks.Items.Refresh();
                                continue;
                            }


                        }
                        else//如果目录存在
                        {
                            //如果校验失败
                            if (!System.IO.File.Exists(TaskData[i].TargetDirectories + "\\" + "FileBackuper_" + TaskData[i].Name))
                            {
                                MessageBoxResult mbrContinue = MessageBox.Show("没有找到备份文件记录，可能是分区发生了改变。" + Environment.NewLine + "继续备份可能造成损失。" + Environment.NewLine + "是否继续？", "校验失败", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                MessageBoxResult mbrCreatNew = MessageBox.Show("是否创建新的校验文件？" + Environment.NewLine + "请手动清理备份目录。", "校验失败", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (mbrCreatNew == MessageBoxResult.Yes)
                                {
                                    System.IO.File.Create(TaskData[i].TargetDirectories + "\\" + "FileBackuper_" + TaskData[i].Name);

                                }
                                if (mbrContinue == MessageBoxResult.No || mbrContinue == MessageBoxResult.None)

                                {
                                    itemsLastTime[itemsName.IndexOf(itemsName[i])] = -2;
                                    AppendLog(itemsName[i], "校验失败：不存在文件：" + TaskData[i].TargetDirectories + "\\" + "FileBackuper_" + TaskData[i].Name);
                                    txtLogPanel.Text = log.ToString();
                                    lvwTasks.Items.Refresh();
                                    continue;
                                }

                            }
                            else
                            {
                                itemsLastTime[i] = -1;//标记正在备份
                                CurrentBackupThreads++;//标记有备份线程运行中
                                 bc = new BackupCore(this);
                                backupThread = new Thread(new ParameterizedThreadStart(bc.Backup));
                                backupThread.Start(itemsName[i]);
                                currentTaskIndex = i;
                                TaskData[i].State = "正在准备";
                                stopAll.IsEnabled = true;
                            }
                        }


                    }
                    else//如果正在备份其他的东西
                    {
                        TaskData[i].State = "等待中";
                    }
                }
                else if (itemsLastTime[i] > 0)//如果还有时间
                {
                    if (!pauseTimer)
                    {
                        itemsLastTime[i]--;
                        TaskData[i].State = "剩余" + itemsLastTime[i].ToString() + "秒";
                    }

                }
                else if (itemsLastTime[i] == -1)//如果本项正在备份
                {
                    TaskData[i].State = CurrentFileCount;
                }
                else//=-2，暂停
                {
                    TaskData[i].State = "暂停";
                }
                //Debug.WriteLine(txtLogPanel.LineCount);
                //txtLogPanel.Text = log.ToString();
               
            }
            lvwTasks.Items.Refresh();


        }
        /// <summary>
        /// 刷新列表
        /// </summary>
        private void RefreshListView()
        {
            btnDeleteTask.IsEnabled = false;
            btnEditTask.IsEnabled = false;
            btnForceToExecute.IsEnabled = false;
            btnPauseCurrent.IsEnabled = false;
            btnOpenTargetDirectory.IsEnabled = false;
            if (cfa.AppSettings.Settings["Items"] != null)//如果不是第一次运行的话
            {
                foreach (var i in cfa.AppSettings.Settings["Items"].Value.Split(new string[] { "#Split#" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    itemsName.Add(i);

                }

                foreach (var i in itemsName)
                {
                    if (cfa.AppSettings.Settings[i + "_State"] == null)
                    {
                        cfa.AppSettings.Settings.Add(i + "_State", "true");
                    }
                    cfa.Save();
                    if (cfa.AppSettings.Settings[i + "_Black"].Value == "")
                    {
                        TaskData.Add(new TaskInfo
                        {
                            Id = TaskData.Count + 1,
                            Name = i,
                            OriginalDirectories =
(cfa.AppSettings.Settings[i + "_White"].Value.Remove(cfa.AppSettings.Settings[i + "_White"].Value.Length - 7, 7)).Replace("#Split#", "、"),
                            TargetDirectories = cfa.AppSettings.Settings[i + "_TargetDirectory"].Value,
                            Interval = new TimeSpan(0, 0, int.Parse(cfa.AppSettings.Settings[i + "_Interval"].Value)).ToString(),
                            State = cfa.AppSettings.Settings[i + "_State"].Value == "false" ? "就绪" : "暂停"
                        });
                    }
                    else
                    {
                        TaskData.Add(new TaskInfo
                        {
                            Id = TaskData.Count + 1,
                            Name = i,
                            OriginalDirectories =
         (cfa.AppSettings.Settings[i + "_White"].Value.Remove(cfa.AppSettings.Settings[i + "_White"].Value.Length - 7, 7)
        + "，除了" + cfa.AppSettings.Settings[i + "_Black"].Value.Remove(cfa.AppSettings.Settings[i + "_Black"].Value.Length - 7, 7))
        .Replace("#Split#", "、"),
                            TargetDirectories = cfa.AppSettings.Settings[i + "_TargetDirectory"].Value,
                            Interval = new TimeSpan(0, 0, int.Parse(cfa.AppSettings.Settings[i + "_Interval"].Value)).ToString(),
                            State = cfa.AppSettings.Settings[i + "_State"].Value == "false" ? "就绪" : "暂停"
                        });

                    }


                    if (cfa.AppSettings.Settings[i + "_State"].Value == "false")
                    {
                        itemsLastTime.Add(-2);
                    }
                    else
                    {
                        itemsLastTime.Add(int.Parse(cfa.AppSettings.Settings[i + "_Interval"].Value));

                    }

                }
            }
            else
            {
                cfa.AppSettings.Settings.Add("Items", "");
                cfa.Save();
            }
        }



        private void Reinitialize()
        {
            if (backupThread != null)
            {
                backupThread.Abort();
            }
            TaskData.Clear();//清除绑定的数据
            itemsName.Clear();//清除Task项目列表
            itemsLastTime.Clear();
            cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);//重新初始化config文件
        }

        #region 任务相关按钮等控件事件
        /// <summary>
        /// 单击新建按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            new TaskSettings("", false).ShowDialog();
            Reinitialize();
            RefreshListView();
            timer.Start();
        }
        /// <summary>
        /// 单击停止按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopThreadButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            bc.xml.Save("log.xml");
            backupThread.Abort();
            statusText.Text = "";
            CurrentBackupThreads = 0;
            itemsLastTime[currentTaskIndex] = int.Parse(cfa.AppSettings.Settings[TaskData[currentTaskIndex].Name + "_Interval"].Value);
        }
        /// <summary>
        /// 单击暂停时间按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseTimerButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            pauseTimer = !pauseTimer;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtLogPaneMouseLeaveEventHandler(object sender, MouseEventArgs e)
        {
            txtLogPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
        /// <summary>
        /// 单击强制执行按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ForceToExecuteButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            itemsLastTime[lvwTasks.SelectedIndex] = 0;
        }
        /// <summary>
        /// 单击删除按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            int index = lvwTasks.SelectedIndex;
            TaskData.RemoveAt(index);
            cfa.AppSettings.Settings["Items"].Value = cfa.AppSettings.Settings["Items"].Value.Replace(itemsName[index] + "#Split#", "");
            foreach (var i in new string[] { "White", "Black", "TargetDirectory", "Interval" })
            {

                cfa.AppSettings.Settings.Remove(itemsName[index] + "_" + i);
            }
            cfa.Save();
            Reinitialize();
            RefreshListView();
        }
        /// <summary>
        /// 单击完全退出按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        /// <summary>
        /// 单击编辑按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {

            timer.Stop();
            new TaskSettings(itemsName[lvwTasks.SelectedIndex], true).ShowDialog();
            Reinitialize();
            RefreshListView();
            timer.Start();


        }
        /// <summary>
        /// 双击列表项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvwTasksItemPreviewMouseDoubleClickEventHandler(object sender, MouseButtonEventArgs e)
        {
            btnForceToExecute.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        /// <summary>
        /// 在列表项上按下按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvwTaskItemPreviewKeyDownEventHandler(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
                case Key.Enter:
                    btnEditTask.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Delete:
                    btnDeleteTask.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Space:
                    btnForceToExecute.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
            }

        }

        private void LvwTaskPreviewMouseLeftButtonUpEventHandler(object sender, MouseButtonEventArgs e)
        {
            if (lvwTasks.SelectedIndex != -1)
            {
                btnDeleteTask.IsEnabled = true;
                btnEditTask.IsEnabled = true;
                btnForceToExecute.IsEnabled = true;
                btnPauseCurrent.IsEnabled = true;
                btnOpenTargetDirectory.IsEnabled = true;
            }
        }
        #endregion 任务相关按钮等控件事件

        #region 日志相关事件
        /// <summary>
        /// 单击日志列表项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbxItemsPreviewMouseLeftButtonUpEventHandler(object sender, MouseButtonEventArgs e)
        {
            LogData.Clear();
            foreach (XmlElement i in xml.DocumentElement[lbxLogList.SelectedItem.ToString()])
            {
                LogData.Add(new XMLLog() { Time = i.GetAttribute("Time"), Event = i.GetAttribute("Event") });
            }
            lvwLog.Items.Refresh();



        }
        /// <summary>
        /// 在日志区输入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtLogPanelPreviewKeyDownEventHandler(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }
        XmlDocument xml = new XmlDocument();
        private void LoadLog()
        {

            //XmlNode currentLog;
            if (!System.IO.File.Exists("log.xml"))
            {
                XmlDeclaration xdec = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
                xml.AppendChild(xdec);
                xml.AppendChild(xml.CreateElement("文件自动备份系统日志"));
            }
            else
            {
                xml.Load("log.xml");
            }

            XmlElement root = xml.DocumentElement;
            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                lbxLogList.Items.Add(root.ChildNodes[i].Name);
            }
            xml.Save("log.xml");

        }

        public void RefreshLog()
        {
            xml.Load("log.xml");
            XmlElement root = xml.DocumentElement;
            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                if (!lbxLogList.Items.Contains(root.ChildNodes[i].Name))
                {
                    lbxLogList.Dispatcher.Invoke(new Action(() => { lbxLogList.Items.Add(root.ChildNodes[i].Name); }));
                    //因为此时这个方法是由不同线程的BackupCore调用的，而lbxLogList在主线程上，所以需要Invoke。
                }

            }
        }

        private void AppendLog(string taskName, string value)
        {

            XmlElement xe = xml.CreateElement("log");
            xe.SetAttribute("Time", DateTime.Now.ToString() + "." + DateTime.Now.Millisecond);
            xe.SetAttribute("Event", value);
            XmlNode currentLog = xml.CreateElement(taskName + "--" + DateTime.Now.ToString().Replace("/", "-").Replace(" ", "_").Replace(":", "-"));
            currentLog.AppendChild(xe);
            xml.DocumentElement.AppendChild(currentLog);
            xml.Save("log.xml");
            RefreshLog();
            log.Append("[" + taskName + "]" + DateTime.Now.ToString() + "." + DateTime.Now.Millisecond + "                 " + value + System.Environment.NewLine + System.Environment.NewLine);
        }

        #endregion 日志相关事件

        #region 程序相关事件
        private void MainWindowClosingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            cfa.Save();
            this.Visibility = Visibility.Hidden;
        }

        #endregion 程序相关事件

        private void CbxStartupClickEventHandler(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked == true)
            {

                string Path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);// System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\新建文件夹 (3)"; //"%USERPROFILE%\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshShortcut sc = (IWshShortcut)shell.CreateShortcut(Path + "\\FileBackuper.lnk");
                sc.TargetPath = Process.GetCurrentProcess().MainModule.FileName;
                sc.WorkingDirectory = Environment.CurrentDirectory;
                sc.Save();
                //Debug.WriteLine(Path);
                if (System.IO.File.Exists(Path + "\\FileBackuper.lnk"))
                {
                    MessageBox.Show("成功", "结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("失败", "结果", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                string Path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);// System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\新建文件夹 (3)"; //"%USERPROFILE%\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

                System.IO.File.Delete(Path + "\\FileBackuper.lnk");
                if (System.IO.File.Exists(Path + "\\FileBackuper.lnk"))
                {
                    MessageBox.Show("失败", "结果", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("成功", "结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }

            }
        }

        private void cbxMinimumClickEventHandler(object sender, RoutedEventArgs e)
        {
            cfa.AppSettings.Settings["AutoMinimum"].Value = cbxMinimum.IsChecked == true ? "true" : "false";
            cfa.Save();
        }

        private void PauseCurrentTimerButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            //itemsLastTime[lvwTasks.SelectedIndex] =
            //    itemsLastTime[lvwTasks.SelectedIndex] == -2 ?
            //    int.Parse(cfa.AppSettings.Settings[itemsName[lvwTasks.SelectedIndex] + "_Interval"].Value) : -2;
            if (itemsLastTime[lvwTasks.SelectedIndex] == -2)
            {
                itemsLastTime[lvwTasks.SelectedIndex] = int.Parse(cfa.AppSettings.Settings[itemsName[lvwTasks.SelectedIndex] + "_Interval"].Value);
                cfa.AppSettings.Settings[itemsName[lvwTasks.SelectedIndex] + "_State"].Value = "true";
            }
            else
            {
                itemsLastTime[lvwTasks.SelectedIndex] = -2;
                cfa.AppSettings.Settings[itemsName[lvwTasks.SelectedIndex] + "_State"].Value = "false";

            }
            cfa.Save();
        }

        private void btnOpenTargetDirectoryClickEventHandler(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", TaskData[lvwTasks.SelectedIndex].TargetDirectories);
        }


    }
}


