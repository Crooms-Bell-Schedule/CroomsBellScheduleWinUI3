using Android.App;
using CBSApp.Service;

namespace CBSApp.Android
{
    public class AndroidHelper : IAndroidHelper
    {
        private MainActivity _activity;
        public AndroidHelper(MainActivity activity)
        {
            _activity = activity;
        }

        public void ShowDialog(string title, string message)
        {
            AlertDialog.Builder dlg = new AlertDialog.Builder(_activity);
            dlg.SetTitle(title);
            dlg.SetMessage(message);
            dlg.SetPositiveButton("OK", (senderAlert, args) => { });

            _activity.RunOnUiThread(() => dlg.Create()?.Show());
        }
    }
}