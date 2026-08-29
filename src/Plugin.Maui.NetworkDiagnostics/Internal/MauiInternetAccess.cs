using Microsoft.Maui.Networking;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class MauiInternetAccess : IInternetAccess
{
    public InternetSnapshot GetSnapshot()
    {
        try
        {
            var access = Connectivity.Current.NetworkAccess;
            var profiles = string.Join(", ", Connectivity.Current.ConnectionProfiles);
            var hasInternet = access is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet;
            var hasLink = access is not NetworkAccess.None;
            return new InternetSnapshot(hasLink, hasInternet, access.ToString(), profiles);
        }
        catch (Exception ex)
        {
            return new InternetSnapshot(false, false, "Unavailable", ex.GetType().Name);
        }
    }
}
