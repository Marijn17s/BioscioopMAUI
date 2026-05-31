using Android.App;
using Android.Content;
using Android.Content.PM;

namespace BioscoopMAUI;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = CallbackScheme)]
public class WebAuthenticatorActivity : WebAuthenticatorCallbackActivity
{
    private const string CallbackScheme = "bioscoopmaui";
}