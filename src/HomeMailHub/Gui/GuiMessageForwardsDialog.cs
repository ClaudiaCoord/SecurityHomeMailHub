/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MimeKit;
using SecyrityMail;
using SecyrityMail.Data;
using SecyrityMail.MailAccounts;
using SecyrityMail.Servers;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    internal class GuiMessageForwardsDialog : Dialog, IDisposable
    {
        private Label toLabel { get; set; } = default;
        private ComboBox toBox { get; set; } = default;
        private Button buttonSend { get; set; } = default;
        private Button buttonNo { get; set; } = default;

        private string msgPath = string.Empty;

        public GuiMessageForwardsDialog() : base() { }
        ~GuiMessageForwardsDialog() => Dispose();

        public new void Dispose() {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Load
        public GuiMessageForwardsDialog Load(string msgpath)
        {
            if (string.IsNullOrWhiteSpace(msgpath))
                throw new ArgumentNullException(nameof(msgPath));

            msgPath = msgpath;

            Width = 80;
            Height = 8;
            Title = RES.MENU_MSGFORWARDS.ClearText();
            Add(toLabel = new Label(RES.TAG_TO) {
                X = 4,
                Y = 1,
                AutoSize = true
            });
            Add(toBox = new ComboBox()
            {
                X = 11,
                Y = 1,
                Width = 60,
                Height = 4,
                ReadOnly = false,
                ColorScheme = GuiApp.ColorField
            });

            buttonSend = new Button(RES.TAG_SEND, true);
            buttonSend.Clicked += async () => {
                string to = toBox.Text.ToString();
                if (!string.IsNullOrWhiteSpace(to))
                    await Send_(to);
                Application.RequestStop();
            };
            buttonNo = new Button(RES.TAG_NO, false);
            buttonNo.Clicked += () => Application.RequestStop();
            AddButton(buttonSend);
            AddButton(buttonNo);

            List<string> addresesList = new();
            for (int i = 0; i < Global.Instance.Accounts.Count; i++)
                addresesList.Add(@$"""{Global.Instance.Accounts[i].Name}"" <{Global.Instance.Accounts[i].Email}>");
            for (int i = 0; i < Global.Instance.EmailAddresses.Count; i++)
                addresesList.Add(@$"""{Global.Instance.EmailAddresses[i].Name}"" <{Global.Instance.EmailAddresses[i].Email}>");

            toBox.SetSource(addresesList.Distinct().ToList());
            return this;
        }
        #endregion

        #region Send
        private async Task Send_(string to) =>
            await Task.Run(async () => {
                MimeMessage mmsg = default(MimeMessage),
                            fmsg = default(MimeMessage);
                try {
                    ReadMessageData rdata = await msgPath.ReadMessage().ConfigureAwait(false);
                    if (rdata.IsEmpty)
                        return;

                    mmsg = rdata.Message;
                    if (mmsg == null)
                        return;

                    UserAccount acc = Global.Instance.FindAccount(((MailboxAddress)mmsg.From[0]).Address);
                    if (acc == default)
                        return;

                    InternetAddressList toList = to.EmailParse();
                    if (toList.Count == 0)
                        return;

                    fmsg = new MimeMessage();
                    fmsg.From.Add(mmsg.From[0]);
                    fmsg.To.AddRange(toList);
                    fmsg.Subject = mmsg.Subject.StartsWith("FW:", StringComparison.OrdinalIgnoreCase) ?
                                    mmsg.Subject : $"Fw: {mmsg.Subject}";

                    MessagePart rfc822 = new() { Message = mmsg };
                    TextPart text = new("plain") {
                        Text = string.Format(RES.TAG_FMT_BODYFORWARD, mmsg.From[0].ToString(), mmsg.Subject)
                    };
                    Multipart mp = new ("mixed");
                    mp.Add(text);
                    mp.Add(rfc822);
                    fmsg.Body = mp;

                    CredentialsRoute route = new(acc);
                    MessageStoreReturn msr = await route.MessageStore(fmsg, Global.Instance.ToMainEvent)
                                                        .ConfigureAwait(false);
                    if (msr == MessageStoreReturn.MessageDelivered)
                        return;
                    Global.Instance.ToMainEvent(
                        MailEventId.DeliverySendMessage, msr.ToString(), null);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally {
                    if (mmsg != null)
                        mmsg.Dispose();
                    if (fmsg != null)
                        fmsg.Dispose();
                }
            });
        #endregion
    }
}
