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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;

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
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TaskData = new ObservableCollection<TaskInfo>();//实例化数据
            //TaskData.Add(new { Name = "NextTime",NextTime = "fsdfa" });

            lvwTasks.DataContext = TaskData;//绑定数据

        }
        public StringBuilder log = new StringBuilder();//“当前日志”中显示的内容，由备份线程控制
        Thread backupThread;//备份线程
        int currentTaskIndex;//正在进行哪一个任务的备份
        public ObservableCollection<TaskInfo> TaskData;//需要绑定的数据
        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);//配置项
        public List<string> itemsName = new List<string>();//任务名称列表
        public List<int> itemsLastTime = new List<int>();//任务距离下一次时间列表
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();//定时器
        public int CurrentBackupThreads = 0;//用于判断是否正在备份
        public string CurrentFileCount = "";//用于显示状态，由备份线程控制
        bool pauseTimer = false;
        private void MainWindowLoadedEventHandler(object sender, RoutedEventArgs e)
        {

            RefreshListView();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            //TaskData[0].State = "就绪，剩余" + itemsLastTime[0].ToString() + "秒";

        }

        private void timer_Tick(object sender, EventArgs e)
        {

            for (int i = 0; i < itemsName.Count; i++)
            {

                if (itemsLastTime[i] == 0)//如果时间到了
                {
                    if (CurrentBackupThreads == 0)
                    {
                        // itemsLastTime[i] = int.Parse(TaskData[i].Interval);
                        itemsLastTime[i] = -1;//标记正在备份
                        CurrentBackupThreads++;//标记有备份线程运行中
                        BackupCore bc = new BackupCore(this);
                        backupThread = new Thread(new ParameterizedThreadStart(bc.Backup));
                        backupThread.Start(itemsName[i]);
                        currentTaskIndex = i;
                        TaskData[i].State = "正在准备";
                    }
                    else
                    {
                        TaskData[i].State = "等待中";

                    }
                }
                else if (itemsLastTime[i] > 0)
                {
                    if (!pauseTimer)
                    {
                        itemsLastTime[i]--;
                        TaskData[i].State = "剩余" + itemsLastTime[i].ToString() + "秒";
                    }

                }
                else
                {
                    TaskData[i].State = CurrentFileCount;
                    if (!txtLogPanel.IsFocused)
                    {
                        txtLogPanel.ScrollToEnd();
                    }
                }
                txtLogPanel.Text = log.ToString();
           
                lvwTasks.Items.Refresh();
            }



        }

        private void RefreshListView()
        {
            //reinitialize();
            if (cfa.AppSettings.Settings["Items"] != null)//如果不是第一次运行的话
            {
                foreach (var i in cfa.AppSettings.Settings["Items"].Value.Split(new string[] { "#Split#" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    itemsName.Add(i);

                }

                foreach (var i in itemsName)
                {
                    TaskData.Add(new TaskInfo
                    {
                        Id = TaskData.Count + 1,
                        Name = i,
                        OriginalDirectories = (cfa.AppSettings.Settings[i + "_White"].Value.Remove(cfa.AppSettings.Settings[i + "_White"].Value.Length - 7, 7) + "，除了" + cfa.AppSettings.Settings[i + "_Black"].Value).Replace("#Split#", "、"),
                        TargetDirectories = cfa.AppSettings.Settings[i + "_TargetDirectory"].Value,
                        Interval = cfa.AppSettings.Settings[i + "_Interval"].Value,
                        State = "就绪"
                    });



                    itemsLastTime.Add(int.Parse(cfa.AppSettings.Settings[i + "_Interval"].Value));

                }
            }
            else
            {
                cfa.AppSettings.Settings.Add("Items", "");
                cfa.Save();
            }
        }

        private void NewTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            new TaskSettings("").ShowDialog();
            reinitialize();
            RefreshListView();
            timer.Start();
        }

        private void reinitialize()
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

        private void StopThreadButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            backupThread.Abort();
            CurrentBackupThreads = 0;
            itemsLastTime[currentTaskIndex] = int.Parse(TaskData[currentTaskIndex].Interval);
        }

        private void PauseTimerButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            pauseTimer = !pauseTimer;
        }

        private void txtLogPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            txtLogPanel.MoveFocus( new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void ForceToExecuteButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            itemsLastTime[lvwTasks.SelectedIndex] = 0;
        }

        private void DeleteTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            int index = lvwTasks.SelectedIndex;
            TaskData.RemoveAt(index);
            cfa.AppSettings.Settings["Items"].Value = cfa.AppSettings.Settings["Items"].Value.Replace(itemsName[index] + "#Split#", "");
            foreach (var i in new string[] {"White","Black","TargetDirectory","Interval" })
            {
              
                cfa.AppSettings.Settings.Remove(itemsName[index] + "_" + i);
            }
            cfa.Save();
            reinitialize();
            RefreshListView();
        }
        //public void refreshLog(StringBuilder log)
        //{
        //    txtLogPanel.Text = log.ToString();
        //}
    }
}
