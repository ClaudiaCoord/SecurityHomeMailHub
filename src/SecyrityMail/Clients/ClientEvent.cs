
using MailKit;
using SecyrityMail.Data;

namespace SecyrityMail.Clients
{
    internal abstract class ClientEvent : MailEvent
    {
        protected string LogTag = string.Empty;

        protected void Client_MetadataChanged(object sender, MetadataChangedEventArgs e) =>
            Global.Instance.Log.Add(e.Metadata.Tag.Id, e.Metadata.Value);

        protected void Client_Disconnected(object sender, DisconnectedEventArgs e) =>
            Global.Instance.Log.Add(LogTag, $"Disconnected: {e.Host}:{e.Port}/{e.Options} = {e.IsRequested}");

        protected void Client_Connected(object sender, ConnectedEventArgs e) =>
            Global.Instance.Log.Add(LogTag, $"Connected: {e.Host}:{e.Port}/{e.Options}");

        protected void Client_Alert(object sender, AlertEventArgs e) =>
            Global.Instance.Log.Add(LogTag, $"Alert: {e.Message}");

        protected void Client_MessageSent(object sender, MessageSentEventArgs e) =>
            Global.Instance.Log.Add(LogTag, $"Sent: {e.Message}/{e.Response}");

        protected void Client_Authenticated(object sender, AuthenticatedEventArgs e) =>
            Global.Instance.Log.Add(LogTag, $"Authenticated: {e.Message}");
    }
}
