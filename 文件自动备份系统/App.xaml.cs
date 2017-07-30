using System;
using System.Configuration;
using System.IO;
using System.Windows;

namespace 自动备份系统
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        Window winMain;
        protected override void OnStartup(StartupEventArgs e)
        {
            //base.OnStartup(e);
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);//配置项
            
            if (cfa.AppSettings.Settings["AutoMinimum"] == null)
            {
                cfa.AppSettings.Settings.Add("AutoMinimum", "false");
                cfa.Save();
                winMain = new MainWindow();
                winMain.Show();
            }
            else
            {
                if (cfa.AppSettings.Settings["AutoMinimum"].Value == "true")
                {
                    winMain = new MainWindow() { Visibility = Visibility.Hidden, WindowState = WindowState.Minimized };
                    winMain.Show();
                }
                else
                {
                    winMain = new MainWindow();
                    winMain.Show();
                }
            }
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            
            MessageBoxResult mbr = MessageBox.Show("抱歉！程序运行出现没有捕获的异常。错误代码：" + System.Environment.NewLine + e.Exception.ToString() + System.Environment.NewLine + "日志已经保存到\"ExceptionLog.log\"文件中" + System.Environment.NewLine + System.Environment.NewLine + "是否继续运行？继续运行可能会出现未知错误", "错误（全局）", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            File.Create("ExceptionLog.log");
            File.WriteAllText("ExceptionLog.log", e.Exception.ToString() + System.Environment.NewLine + DateTime.Now.ToString());
            if (mbr == MessageBoxResult.No)
            {
                Current.Shutdown();
            }
        }
    }
}
