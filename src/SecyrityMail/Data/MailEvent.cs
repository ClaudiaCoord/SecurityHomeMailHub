/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Runtime.CompilerServices;
using SecyrityMail.Messages;

namespace SecyrityMail.Data
{
    public enum MailEventId :int
    {
        None = 0,
        DeliveryOutMessage,
        DeliveryInMessage,
        DeliverySendMessage,
        DeliveryLocalMessage,
        DeliveryErrorMessage,
        DeleteMessage,
        DateExpired,
        BeginInit,
        EndInit,
        BeginCall,
        EndCall,
        Cancelled,
        Started,
        NotFound,
        PropertyChanged,
        ProxyCheckStart,
        ProxyCheckEnd,
        UseAccount,
        UserAuth,
        StartTls,
        StartFetchMail,
        StopFetchMail,
    }

    public class EventActionArgs : EventArgs
    {
        public MailEventId Id { get; set; } = MailEventId.None;
        public MailMessage Message => (Obj is MailMessage msg) ? msg : default;
        public string Text { get; set; } = string.Empty;
        public object Sender { get; set; } = null;
        public object Obj { get; set; } = null;

        public EventActionArgs(MailEventId id, object sender, string src, object msg) {
            Id = id;
            Text = src;
            Sender = sender;
            Obj = msg;
        }

        public static EventActionArgs Create(MailEventId id, object sender, string path, object msg) =>
            new EventActionArgs(id, sender, path, msg);
        public static EventActionArgs Create(object sender, string name) =>
            new EventActionArgs(MailEventId.PropertyChanged, sender, name, default);
    }

    public abstract class MailEvent
    {
        public event EventHandler<EventActionArgs> EventCb = delegate { };
        protected void OnProxyEvent(EventActionArgs args) =>
            EventCb.Invoke(this, args);
        protected void OnProxyEvent(object sender, EventActionArgs args) =>
            EventCb.Invoke(sender, args);
        protected void OnProxyEvent(MailEventId id, object sender, string path, object obj) =>
            EventCb.Invoke(this, EventActionArgs.Create(id, sender, path, obj));
        protected void OnCallEvent(MailEventId id, object clz, string path, object obj) =>
            EventCb.Invoke(this, EventActionArgs.Create(id, clz, path, obj));
        protected void OnCallEvent(MailEventId id, string path, object obj) =>
            EventCb.Invoke(this, EventActionArgs.Create(id, this, path, obj));
        protected void OnCallEvent(MailEventId id, string path) =>
            EventCb.Invoke(this, EventActionArgs.Create(id, this, path, default));
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            EventCb.Invoke(this, EventActionArgs.Create(this, name));
        protected void OnPropertyChanged(params string[] names) {
            if (names != null)
                foreach (string name in names)
                    EventCb.Invoke(this, EventActionArgs.Create(this, name));
        }
        public void SubscribeProxyEvent(EventHandler<EventActionArgs> handler) => this.EventCb += handler;
        public void UnSubscribeProxyEvent(EventHandler<EventActionArgs> handler) => this.EventCb -= handler;
    }

    public static class MailEventExtension
    {
        public static bool IsPropertyChanged(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.PropertyChanged);
        public static bool IsDeliveryOutMessage(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.DeliveryOutMessage);
        public static bool IsDeliveryInMessage(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.DeliveryInMessage);
        public static bool IsDeliveryLocalMessage(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.DeliveryLocalMessage);
        public static bool IsDeleteMessage(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.DeleteMessage);
        public static bool IsProxyCheckStart(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.ProxyCheckStart);
        public static bool IsProxyCheckEnd(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.ProxyCheckEnd);
        public static bool IsStartFetchMail(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.StartFetchMail);
        public static bool IsStopFetchMail(this EventActionArgs a) =>
            (a != default) && (a.Id == MailEventId.StopFetchMail);
        public static bool IsTypeOf<T1>(this EventActionArgs a) =>
            (a != default) && (a.Sender is T1);
    }

    internal static class WeakEventHandler<TArgs>
    {
        public static EventHandler<TArgs> Create<THandler>(
            THandler handler, Action<THandler, object, TArgs> invoker)
            where THandler : class {

            WeakReference<THandler> weakEventHandler = new(handler);

            return (sender, args) => {
                THandler thandler;
                if (weakEventHandler.TryGetTarget(out thandler))
                    invoker(thandler, sender, args);
            };
        }
    }
}
