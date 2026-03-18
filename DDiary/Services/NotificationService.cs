using System;
using System.Threading;
using System.Threading.Tasks;

namespace DDiary.Services
{
    /// <summary>
    /// Servizio per la gestione delle notifiche desktop Windows.
    /// </summary>
    public interface INotificationService
    {
        void ShowTodayReminder(string profileName);
        void ScheduleDailyReminder(TimeSpan reminderTime, string profileName, CancellationToken cancellationToken);
        void ShowStartupReminder(string profileName);
    }

    public class NotificationService : INotificationService, IDisposable
    {
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        public NotificationService()
        {
            InitNotifyIcon();
        }

        private void InitNotifyIcon()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Text = "DDiary",
                    Icon = System.Drawing.SystemIcons.Information,
                    Visible = false
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotifyIcon init error: {ex.Message}");
            }
        }

        public void ShowTodayReminder(string profileName)
        {
            ShowBalloon("DDiary – Promemoria",
                $"Ciao {profileName}! Compila il tuo diario alimentare.");
        }

        public void ShowStartupReminder(string profileName)
        {
            ShowBalloon("DDiary – Benvenuto!",
                $"Ciao {profileName}! Ricordati del tuo diario.");
        }

        public void ScheduleDailyReminder(TimeSpan reminderTime, string profileName, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    var target = DateTime.Today.Add(reminderTime);
                    if (target < now)
                        target = target.AddDays(1);

                    var delay = target - now;
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                        if (!cancellationToken.IsCancellationRequested)
                            ShowTodayReminder(profileName);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, cancellationToken);
        }

        private void ShowBalloon(string title, string body)
        {
            try
            {
                if (_notifyIcon == null) InitNotifyIcon();
                if (_notifyIcon == null) return;

                _notifyIcon.Visible = true;
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = body;
                _notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                _notifyIcon.ShowBalloonTip(5000);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationService error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}
