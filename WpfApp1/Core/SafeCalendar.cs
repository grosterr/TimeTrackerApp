using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace WpfApp1.Controls
{
    public class SafeCalendar : Calendar
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new FrameworkElementAutomationPeer(this);
        }
    }
}