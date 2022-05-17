
using System;
using System.Reflection;
using HomeMailHub.Version;
using SecyrityMail;
using SecyrityMail.Data;
using SecyrityMail.MailAccounts;
using SecyrityMail.Proxy;
using SecyrityMail.Proxy.SshProxy;
using SecyrityMail.Vpn;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiRootMenuBar : MenuBar, IDisposable
    {
		private Action actionUpdateTitle = () => { };
		private Action actionClearLog = () => { };
        private string appVersion;
        private bool   appVersionBusy = false;

        private MenuBarItem[] rootItems;
        private MenuItem[]  userItemsMenu { get; set; } = default;
        private MenuItem[]  proxyItemsMenu { get; set; } = default;
        private MenuItem[]  vpnItemsMenu { get; set; } = default;
        private MenuItem[]  sshItemsMenu { get; set; } = default;
        private MenuItem[]  buildLogMenu { get; set; } = default;
        private MenuItem[]  buildTunnelMenu { get; set; } = default;
        private MenuItem    vpnSelectedMenu { get; set; } = default;
        private MenuItem    sshSelectedMenu { get; set; } = default;
        private MenuItem    proxySelectedMenu { get; set; } = default;
		private MenuBarItem proxyTypeMenu { get; set; } = default;
		private MenuBarItem vpnAccountsMenu { get; set; } = default;
        private MenuBarItem sshAccountsMenu { get; set; } = default;
        private MenuBarItem mailboxMenu { get; set; } = default;
		private MenuBarItem logMenu { get; set; } = default;
		private MenuBarItem tunnelMenu { get; set; } = default;

		private bool BackupAllow =>
			!Global.Instance.Config.IsCheckMailRun &&
            !Global.Instance.Config.IsVpnTunnelRunning &&
			!Global.Instance.Config.IsSshRunning &&
			!Global.Instance.Config.IsProxyCheckRun;

        private bool IsViewLog {
			get => GuiApp.IsViewLog;
			set { GuiApp.IsViewLog = value; actionUpdateTitle.Invoke(); }
		}
		private bool IsViewEvent {
			get => GuiApp.IsViewEvent;
			set { GuiApp.IsViewEvent = value; actionUpdateTitle.Invoke(); }
		}
		private bool IsWriteLog {
			get => GuiApp.IsWriteLog;
			set { GuiApp.IsWriteLog = value; actionUpdateTitle.Invoke(); }
		}

		public GuiRootMenuBar(Action aupdate, Action aclear) {
            actionUpdateTitle = aupdate;
            actionClearLog = aclear;
            appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Init();
        }

		public new void Dispose() {

			this.GetType().IDisposableObject(this);
			base.Dispose();
        }

        #region Init
        public void Init() {

			vpnAccountsMenu = new MenuBarItem(RES.MENU_VPNACCOUNT, new MenuItem[0]);
            sshAccountsMenu = new MenuBarItem(RES.MENU_SSHACCOUNT, new MenuItem[0]);
            proxyTypeMenu = new MenuBarItem(RES.MENU_PROXYTYPE, new MenuItem[0]);

            mailboxMenu = new MenuBarItem(RES.MENU_MAILBOX, new MenuItem[0]);
            tunnelMenu = new MenuBarItem("_VPN/Proxy", new MenuItem[0]);
			logMenu = new MenuBarItem(RES.MENU_LOG, new MenuItem[0]);

			vpnSelectedMenu = RES.MENU_SSHSELECT.CreateCheckedMenuItem((_) =>
				Global.Instance.Config.IsSshRunning, false);

            sshSelectedMenu = RES.MENU_VPNSELECT.CreateCheckedMenuItem((_) =>
                Global.Instance.Config.IsVpnSelected, false);
            
			proxySelectedMenu = RES.MENU_PROXYSELECT.CreateCheckedMenuItem((_) =>
				Global.Instance.Config.ProxyType != ProxyType.None, false);

			rootItems = new MenuBarItem[] {
				new MenuBarItem (RES.MENU_MAIL, new MenuItem [] {
					mailboxMenu,
					new MenuItem (RES.MENU_MAILNEW, "", () => {
						GuiApp.Get.LoadWindow(typeof(GuiMessageWriteWindow));
					}),
					new MenuItem (RES.MENU_MAILCHCEK, "", async () => {
						_ = await Global.Instance.Tasks.Run().ConfigureAwait(false);
					},
					() => !Global.Instance.Tasks.IsCheckMailRun),
					null,
					new MenuItem (RES.MENU_QUIT, "", () => {
						Application.RequestStop ();
					})
				}),
				logMenu,
				new MenuBarItem (RES.MENU_SERVICES, new MenuItem [] {
					new MenuBarItem ($"POP3 {RES.TAG_SERVICE}", new MenuItem [] {
						new MenuItem (RES.BTN_START, "", () => Global.Instance.StartPop3Service(), () => !Global.Instance.IsPop3Run),
						new MenuItem (RES.BTN_STOP, "", () => Global.Instance.StopPop3Service(), () => Global.Instance.IsPop3Run)
					}),
					new MenuBarItem ($"SMTP {RES.TAG_SERVICE}", new MenuItem [] {
						new MenuItem (RES.BTN_START, "", () => Global.Instance.StartSmtpService(), () => !Global.Instance.IsSmtpRun),
						new MenuItem (RES.BTN_STOP, "", () => Global.Instance.StopSmtpService(), () => Global.Instance.IsSmtpRun)
					}),
					null,
					new MenuItem (RES.MENU_SERVSET, "", () => {
						GuiApp.Get.LoadWindow(typeof(GuiServicesSettingsWindow));
					})
				}),
				tunnelMenu,
				new MenuBarItem (RES.MENU_SETTINGS, new MenuItem [] {
                    new MenuItem (RES.MENU_ACCBACKUP, "",
						async () => {
							if (MessageBox.Query (50, 7,
								RES.MENU_ACCSAVE,
								$"{RES.MENU_ACCBACKUP.ClearText()}?", RES.TAG_YES, RES.TAG_NO) == 0) {
									_ = await Global.Instance.AccountsBackup();
									RES.MENU_ACCSAVE.StatusBarText();
                                }
                        },
						() => BackupAllow),
                    new MenuItem (RES.MENU_ACCRESTORE, "",
                        async () => {
							if (MessageBox.Query (50, 7,
								RES.MENU_ACCLOAD,
								$"{RES.MENU_ACCRESTORE.ClearText()}?", RES.TAG_YES, RES.TAG_NO) == 0) {
									_ = await Global.Instance.AccountsRestore();
                                    try { Load(); } catch { }
                                    RES.MENU_ACCLOAD.StatusBarText();
                                }
                        },
                        () => BackupAllow),
                    RES.MENU_LIGHTTEXT.CreateCheckedMenuItem((b) => {
						if (b) GuiApp.IsLightText = !GuiApp.IsLightText;
						return GuiApp.IsLightText;
					}),
                    null,
                    new MenuItem (RES.MENU_REFRESH, "", () => {
                            try { Load(); } catch { }
                        }, null, null, Key.AltMask | Key.O),
                    new MenuItem (RES.MENU_SAVEALL, "", async () => {
						try {
							await new ConfigurationSave().Save();
							RES.TAG_SAVE.StatusBarText();
						} catch (Exception ex) { ex.StatusBarError(); }
					}, null, null, Key.AltMask | Key.S),
                    null,
                    new MenuItem (string.Format(RES.TAG_FMT_VERSION, appVersion), "", async () => {
                        try {
							if (appVersionBusy)
								return;
                            appVersionBusy = true;

                            GithubFeed feed = new();
							bool b = await feed.GetReleaseVersion().ConfigureAwait(false);
							if (!b || feed.IsEmpty) {
								RES.TAG_VERSIONERROR.StatusBarText();
								return;
                            }
							if (!(b = feed.CompareVersion(appVersion)))
                                feed.GetDescription.StatusBarText();
							Application.MainLoop.Invoke(() => {
								int x = MessageBox.Query (50, 7,
									RES.MENU_VERSIONCHECK,
									b ? string.Format(RES.MENU_FMT_VERSIONOLD, appVersion) :
									    string.Format(RES.MENU_FMT_VERSIONNEW, appVersion, feed.GetVersion),
									RES.TAG_YES, RES.TAG_NO);
								if (!b && (x == 0))
									new Uri(feed.GetReleasesUrl, UriKind.Absolute).BrowseUri();
                            });
                        } catch (Exception ex) { ex.StatusBarError(); }
						finally { appVersionBusy = false; }
					}, () => !appVersionBusy)
                }),
				new MenuBarItem ($"_{RES.TAG_ACCOUNTS}", new MenuItem [] {
					new MenuItem (RES.MENU_MAILACCOUNT, "", () => {
						GuiApp.Get.LoadWindow(typeof(GuiMailAccountWindow));
					}),
					new MenuItem ($"_VPN {RES.TAG_ACCOUNTS}", "", () => {
						GuiApp.Get.LoadWindow(typeof(GuiVpnAccountWindow));
					}),
					new MenuItem ($"_SSH {RES.TAG_ACCOUNTS}", "", () => {
						GuiApp.Get.LoadWindow(typeof(GuiSshAccountWindow));
					}),
					new MenuItem (RES.MENU_PROXYLIST, "", () => {
						GuiApp.Get.LoadWindow(typeof(GuiProxyListWindow));
					})
				})
			};
			Menus = rootItems;
		}
		#endregion

		#region Load
		public void Load(EventActionArgs a) {

			if ((a == null) ||
				(a.Id != MailEventId.EndInit) ||
				!"FindAutoInit".Equals(a.Src)) return;
			Load();
        }

		private void Load() {

            proxyItemsMenu = new MenuItem[MailProxy.AllProxyTypeList.Length];
			for (int i = 0; i < MailProxy.AllProxyTypeList.Length; i++) {

				ProxyType proxytype = MailProxy.AllProxyTypeList[i];
				string title = (i == 0) ? proxytype.ToString() :
					((i == (MailProxy.AllProxyTypeList.Length - 1)) ?
						RES.TAG_PROXYALL : $"{proxytype} {RES.TAG_PROXY}");

                proxyItemsMenu[i] = title.CreateCheckedMenuItem((b) => {
					int x = i;
					ProxyType pt = proxytype;
					if (b) UpdateProxyType(pt, x);
					return Global.Instance.Config.ProxyType == pt;
				});
				if (Global.Instance.Config.ProxyType == proxytype)
                    proxyItemsMenu[i].Checked = true;
			}
            Application.MainLoop.Invoke(() => proxyTypeMenu.Children = proxyItemsMenu);
			UpdateProxyTitle();

            if (Global.Instance.Accounts.Count > 0) {
                userItemsMenu = new MenuItem[Global.Instance.Accounts.Count];
                for (int i = 0; i < Global.Instance.Accounts.Count; i++) {

                    UserAccount acc = Global.Instance.Accounts[i];
                    userItemsMenu[i] = new MenuItem(
                        acc.Email, "", () => GuiApp.Get.LoadWindow(typeof(GuiMessagesListWindow), acc.Email), () => acc.Enable);
                }
            }
            Application.MainLoop.Invoke(() => mailboxMenu.Children = (userItemsMenu == null) ? defaultMenuItem() : userItemsMenu);

            if (Global.Instance.VpnAccounts.Count > 0) {
                vpnItemsMenu = new MenuItem[Global.Instance.VpnAccounts.Count];
				for (int i = 0; i < Global.Instance.VpnAccounts.Count; i++) {

					VpnAccount acc = Global.Instance.VpnAccounts[i];
                    vpnItemsMenu[i] = new MenuItem(acc.Name, "", null, () => !acc.IsEmpty && !acc.IsExpired && acc.Enable);
                    vpnItemsMenu[i].Action = async () => {
						_ = await Global.Instance.VpnAccounts.SelectAccount(acc.Name).ConfigureAwait(false);
						UpdateVpnTitle();
					};
				}
			}
            Application.MainLoop.Invoke(() => vpnAccountsMenu.Children = (vpnItemsMenu == null) ? defaultMenuItem() : vpnItemsMenu);
            UpdateVpnTitle();

            if (Global.Instance.SshProxy.Count > 0) {
                sshItemsMenu = new MenuItem[Global.Instance.SshProxy.Count];
                for (int i = 0; i < Global.Instance.SshProxy.Count; i++) {

                    SshAccount acc = Global.Instance.SshProxy[i];
                    sshItemsMenu[i] = new MenuItem(acc.Name, "", null, () => !acc.IsEmpty && !acc.IsExpired && acc.Enable);
                    sshItemsMenu[i].Action = async () => {
                        _ = await Global.Instance.SshProxy.SelectAccount(acc.Name).ConfigureAwait(false);
                        UpdateSshTitle();
                    };
                }
            }
            Application.MainLoop.Invoke(() => sshAccountsMenu.Children = (sshItemsMenu == null) ? defaultMenuItem() : sshItemsMenu);
            UpdateSshTitle();

            buildLogMenu = new MenuItem[] {
				RES.MENU_LOGVIEW.CreateCheckedMenuItem((b) => {
					if (b) { IsViewLog = !IsViewLog;
							 ToStatusBar(RES.MENU_LOGVIEW, IsViewLog); }
					return IsViewLog;
				}),
				RES.MENU_LOGEVENT.CreateCheckedMenuItem((b) => {
					if (b) { IsViewEvent = !IsViewEvent;
							 ToStatusBar(RES.MENU_LOGEVENT, IsViewEvent); }
					return IsViewEvent;
				}),
				RES.MENU_LOGVPN.CreateCheckedMenuItem((b) => {
					if (b) { Global.Instance.Config.IsEnableLogVpn = !Global.Instance.Config.IsEnableLogVpn;
							 ToStatusBar(RES.MENU_LOGVPN, Global.Instance.Config.IsEnableLogVpn); }
					return Global.Instance.Config.IsEnableLogVpn;
				}),
				RES.MENU_LOGCACHE.CreateCheckedMenuItem((b) => {
					if (b) { Global.Instance.Config.IsCacheMessagesLog = !Global.Instance.Config.IsCacheMessagesLog;
							 ToStatusBar(RES.MENU_LOGCACHE, Global.Instance.Config.IsCacheMessagesLog); }
					return Global.Instance.Config.IsCacheMessagesLog;
				}),
				RES.MENU_LOGWRITE.CreateCheckedMenuItem((b) => {
					if (b) { IsWriteLog = !IsWriteLog;
							 ToStatusBar(RES.MENU_LOGWRITE, IsWriteLog); }
					return IsWriteLog;
				}),
				null,
				new MenuItem (RES.MENU_LOGCLEAR, "", () => {
					actionClearLog.Invoke();
				})
			};
            Application.MainLoop.Invoke(() => logMenu.Children = buildLogMenu);

            buildTunnelMenu = new MenuItem[] {
                    new MenuBarItem ($"VPN {RES.TAG_SERVICE}", new MenuItem [] {
                        new MenuItem (RES.BTN_START, "",
							async () => await Global.Instance.Vpn.Start().ConfigureAwait(false),
							() => Global.Instance.Config.IsVpnEnable && !Global.Instance.Config.IsVpnTunnelRunning),
                        new MenuItem (RES.BTN_STOP, "",
							() => Global.Instance.Vpn.Stop(),
							() => Global.Instance.Config.IsVpnTunnelRunning)
                    }),
                    new MenuBarItem ($"SSH SOCKS5 {RES.TAG_SERVICE}", new MenuItem [] {
                        new MenuItem (RES.BTN_START, "",
							async () => await Global.Instance.SshProxy.Start().ConfigureAwait(false),
							() => !Global.Instance.SshProxy.IsEmpty && !Global.Instance.Config.IsSshRunning),
                        new MenuItem (RES.BTN_STOP, "",
							() => Global.Instance.SshProxy.Stop(),
							() => Global.Instance.Config.IsSshRunning)
                    }),
                    vpnAccountsMenu,
                    sshAccountsMenu,
                    proxyTypeMenu,
                    RES.MENU_VPNRANDOM.CreateCheckedMenuItem((b) => {
                        if (b) { Global.Instance.Config.IsVpnRandom = !Global.Instance.Config.IsVpnRandom;
                                 ToStatusBar(RES.MENU_VPNRANDOM, Global.Instance.Config.IsVpnRandom); }
                        return Global.Instance.Config.IsVpnRandom;
                    }),
                    RES.MENU_VPNALWAYS.CreateCheckedMenuItem((b) => {
                        if (b) { Global.Instance.Config.IsVpnAlways = !Global.Instance.Config.IsVpnAlways;
                                 ToStatusBar(RES.MENU_VPNALWAYS, Global.Instance.Config.IsVpnAlways); }
                        return Global.Instance.Config.IsVpnAlways;
                    }),
                    RES.MENU_PROXYREPACK.CreateCheckedMenuItem((b) => {
						if (b) { Global.Instance.Config.IsProxyListRepack = !Global.Instance.Config.IsProxyListRepack;
								 ToStatusBar(RES.MENU_PROXYREPACK, Global.Instance.Config.IsProxyListRepack); }
						return Global.Instance.Config.IsProxyListRepack;
					}),
					null,
					proxySelectedMenu,
					vpnSelectedMenu,
                    sshSelectedMenu,
                    RES.MENU_VPNENABLE.CreateCheckedMenuItem((b) => {
						return Global.Instance.Config.IsVpnEnable;
					}, false),
					RES.MENU_VPNTUNRUN.CreateCheckedMenuItem((b) => {
						return Global.Instance.Config.IsVpnTunnelRunning;
					}, false),
					RES.MENU_VPNREADY.CreateCheckedMenuItem((b) => {
						return Global.Instance.Config.IsVpnReady;
					}, false),
					RES.MENU_VPNBEGIN.CreateCheckedMenuItem((b) => {
						return Global.Instance.Config.IsVpnBegin;
					}, false),
					RES.MENU_PROXYCHECK.CreateCheckedMenuItem((b) => {
						return Global.Instance.Config.IsProxyCheckRun;
					}, false),
                    null,
                    new MenuItem (RES.MENU_REFRESHVPN, "", () => {
                            try { Load(); } catch { }
                        }, null, null, Key.AltMask | Key.R),
                };
            Application.MainLoop.Invoke(() => tunnelMenu.Children = buildTunnelMenu);
		}
        #endregion

		private void ToStatusBar<T1>(string tag, T1 t) where T1 : struct =>
			$"{tag} - {RES.TAG_OK}/{t}".ClearText().StatusBarText();

		private MenuItem[] defaultMenuItem() =>
                new MenuItem[] {
                    new MenuItem(
                        RES.TAG_NOTELEMENTS, "", () => {}, () => false)
                };

        private void UpdateProxyType(ProxyType pt, int i) {
			if (proxyTypeMenu.Children == null) return;
			for (int n = 0, c = i; n < proxyTypeMenu.Children.Length; n++)
				if ((proxyTypeMenu.Children[n] != null) && (n != c)) proxyTypeMenu.Children[n].Checked = false;
			Global.Instance.Config.ProxyType = pt;
			UpdateProxyTitle();
        }

        private void UpdateProxyTitle() =>
            UpdateNameTitle(proxySelectedMenu,
                RES.MENU_PROXYSELECT,
                (b) => b? Global.Instance.Config.ProxyType.ToString() : string.Empty,
                Global.Instance.Config.ProxyType != ProxyType.None);

        private void UpdateVpnTitle() =>
            UpdateNameTitle(vpnSelectedMenu,
                RES.MENU_VPNSELECT,
                (b) => (b && Global.Instance.VpnAccounts.IsAccountSelected) ?
					Global.Instance.VpnAccounts.AccountSelected.Name : string.Empty,
                Global.Instance.Config.IsVpnSelected);

        private void UpdateSshTitle() =>
            UpdateNameTitle(sshSelectedMenu,
                RES.MENU_SSHSELECT,
                (b) => (b && Global.Instance.SshProxy.IsAccountSelected) ?
					Global.Instance.SshProxy.AccountSelected.Name : string.Empty,
                Global.Instance.Config.IsSshSelected);

        private void UpdateNameTitle(MenuItem m, string s1, Func<bool, string> f, bool b) =>
            Application.MainLoop.Invoke(() => {
                if (b) {
					if (!string.IsNullOrWhiteSpace(s1))
                    m.Title = $"{s1}: {f.Invoke(b)}";
                    m.Checked = true;
                } else {
                    m.Checked = false;
                    m.Title = s1;
                }
            });
    }
}
