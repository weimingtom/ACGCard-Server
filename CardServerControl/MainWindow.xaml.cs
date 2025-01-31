﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Input;
using CardServerControl.Model;
using CardServerControl.Model.DTO;
using CardServerControl.Util;
using CardServerControl.Model.Cards;

namespace CardServerControl
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 版本信息
        public string internalVersion = "2015052601";//内部版本号
        public string officialVersion = "0.1Beta";//正式版本号
        #endregion

        public static MainWindow instance;
        private LogsSystem logsSystem;
        public MainWindow()
        {
            InitializeComponent();

            instance = this;
            logsSystem = LogsSystem.Instance;
            BindShortcut();//绑定快捷键
        }
        /// <summary>
        /// 处理命令
        /// </summary>
        /// <param name="args">参数数组</param>
        private void ProcessCommand(string[] args)
        {
            if (args[0] == "server")
            {
                if (args.Length == 1) { ArgNotEnough(); return; }
                else
                {
                    if (args[1] == "start")
                    {
                        UdpServer.Instance.Connect();
                        TcpServer.Instance.Init();
                    }
                    else if (args[1] == "stop")
                    {
                        UdpServer.Instance.StopListen();
                        TcpServer.Instance.StopListen();
                    }
                    else
                    {
                        UnkownCommand();
                    }
                }

            }
            else if (args[0] == "log")
            {
                if (args.Length == 1) { ArgNotEnough(); return; }
                else
                {
                    if (args[1] == "open")
                    {
                        OpenLogFile(null, new RoutedEventArgs());
                    }
                    else if (args[1] == "clear")
                    {
                        ClearScreen(null, new RoutedEventArgs());
                    }
                    else if (args[1] == "openfolder")
                    {
                        OpenLogFileFolder(null, new RoutedEventArgs());
                    }
                    else
                    {
                        UnkownCommand();
                    }
                }

            }
            else if (args[0] == "list")
            {
                ShowPlayerList(null, null);
            }
            else if (args[0] == "room")
            {
                if (args[1] == "detail")
                {
                    try
                    {
                        int roomID = Convert.ToInt32(args[2]);

                        GameRoomManager grm = TcpServer.Instance.GetGameRoomManager();
                        GameRoom room = grm.GetRoom(roomID);
                        if (room != null)
                        {
                            string showTXT = "";
                            showTXT += "\n\t房间ID:" + room.roomID + "\n";
                            showTXT += string.Format("\t对战双方: {0}({1}) - {2}({3})\n", room.playerSocketA.playerInfo.playerName, room.playerSocketA.playerInfo.playerUid, room.playerSocketB.playerInfo.playerName, room.playerSocketB.playerInfo.playerUid);
                            showTXT += "\tA方卡片背包列表:\n";
                            foreach (PlayerCard playerCard in room.playerDataA.characterCardInv)
                            {
                                showTXT += string.Format("\t\t{0}({1}-{2}-{3}-{4})\n", playerCard.cardName, playerCard.cardId, playerCard.cardLevel, playerCard.GetHealth(), playerCard.GetEnergy());
                            }
                            showTXT += "\tB方卡片背包列表:\n";
                            foreach (PlayerCard playerCard in room.playerDataB.characterCardInv)
                            {
                                showTXT += string.Format("\t\t{0}({1}-{2}-{3}-{4})", playerCard.cardName, playerCard.cardId, playerCard.cardLevel, playerCard.GetHealth(), playerCard.GetEnergy());
                            }
                            showTXT += "...";
                            logsSystem.Print(showTXT);
                        }
                        else
                        {
                            logsSystem.Print("房间号不存在", LogLevel.WARN);
                        }
                    }
                    catch (Exception ex)
                    {
                        logsSystem.Print("出现异常，可是能输入的命令不合法:" + ex.ToString(), LogLevel.WARN);
                    }
                }
            }
            else if (args[0] == "rooms")
            {
                ShowRoomsInfo(null, null);
            }
            else if (args[0] == "say")
            {
                if (args.Length == 1) { ArgNotEnough(); return; }
                else
                {
                    string message = args[1];
                    if (args.Length > 2)
                    {
                        for (int i = 2; i < args.Length; i++)
                        {
                            message += " " + args[i];
                        }
                    }

                    SocketModel model = new SocketModel();
                    model.areaCode = AreaCode.Server;
                    model.protocol = SocketProtocol.CHAT;
                    model.message = JsonCoding<ChatDTO>.encode(new ChatDTO(message, "Server", ""));
                    string sendMessage = JsonCoding<SocketModel>.encode(model);
                    UdpServer.Instance.SendToAllPlayer(Encoding.UTF8.GetBytes(sendMessage));

                    logsSystem.Print("[系统公告]" + message);
                }
            }
            else if (args[0] == "kick")
            {
                if (args.Length == 1) { ArgNotEnough(); return; }
                else
                {
                    try
                    {
                        int playerUid = Convert.ToInt32(args[1]);

                        Player player = PlayerManager.Instance.GetLobbyPlayerByUid(playerUid);
                        if (player != null)
                        {
                            //玩家在线
                            logsSystem.Print(string.Format("玩家[{0}({1})]已经被踢出了服务器", player.playerName, player.uid));
                            PlayerManager.Instance.LobbyPlayerLogout(playerUid);
                        }
                        else
                        {
                            //玩家不存在或不在线
                            logsSystem.Print("该玩家不存在", LogLevel.WARN);
                        }
                    }
                    catch (Exception ex)
                    {
                        logsSystem.Print("请输入玩家的Uid" + ex.ToString());
                    }
                }
            }
            else if (args[0] == "version")
            {
                ShowVersionInfo(null, null);
            }
            else if (args[0] == "help")
            {
                //打开帮助页
                OpenHelpURL(null, new RoutedEventArgs());
            }
            else
            {
                UnkownCommand();
            }
        }

        /// <summary>
        /// 日志打印：参数不足
        /// </summary>
        private void ArgNotEnough()
        {
            logsSystem.Print("参数不足", LogLevel.WARN);
        }

        /// <summary>
        /// 日志打印：未知的命令
        /// </summary>
        private void UnkownCommand()
        {
            logsSystem.Print("未知的命令", LogLevel.WARN);
        }

        #region 绑定快捷键
        public class CustomCommands
        {
            private static RoutedUICommand sendCommand;
            public static RoutedUICommand SendCommand
            {
                get
                {
                    if (sendCommand == null)
                    {
                        sendCommand = new RoutedUICommand("SendCommand", "SendCommand", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.Enter) });
                    }
                    return sendCommand;
                }
            }
        }
        private void BindShortcut()
        {
            this.CommandBindings.Add
                (new CommandBinding
                    (CustomCommands.SendCommand, (sender, e) =>
                    {
                        OnSubmit(sender, e);
                    },
                    (sender, e) =>
                    { e.CanExecute = true; }
                    )
                    );
        }
        #endregion

        #region UI交互
        /// <summary>
        /// 确认命令输入
        /// </summary>
        private void OnSubmit(object sender, RoutedEventArgs e)
        {
            string command = InputField.Text;
            if (command != "")
            {
                logsSystem.Print("[控制中心]" + command);
                InputField.Text = "";

                //处理命令
                command = command.ToLower();
                string[] args = command.Split(new char[] { ' ' });
                ProcessCommand(args);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            UdpServer.Instance.StopListen();
        }

        private void MenuOpenServer(object sender, RoutedEventArgs e)
        {
            ProcessCommand(new string[] { "server", "start" });
        }

        private void MenuCloseServer(object sender, RoutedEventArgs e)
        {
            ProcessCommand(new string[] { "server", "stop" });
        }

        private void ClearScreen(object sender, RoutedEventArgs e)
        {
            this.LogList.Items.Clear();
        }

        private void OpenLogFile(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", logsSystem.GetLogFileDir());
        }

        private void OpenLogFileFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", string.Format(@"/select,{0}", logsSystem.GetLogFileDir()));
        }

        private void OpenURL(object sender, RoutedEventArgs e)
        {
            string URL = @"http://www.moonrailgun.com/";
            Process.Start("explorer.exe", URL);
        }

        private void OpenHelpURL(object sender, RoutedEventArgs e)
        {
            string URL = @"http://www.moonrailgun.com/help";
            Process.Start("explorer.exe", URL);
        }

        /// <summary>
        /// 显示在线所有玩家
        /// </summary>
        private void ShowPlayerList(object sender, RoutedEventArgs e)
        {
            List<Player> playerList = PlayerManager.Instance.GetLobbyPlayerList();
            int i = 0;
            string listTXT = "";
            foreach (Player player in playerList)
            {
                if (listTXT == "")
                {
                    listTXT = string.Format("{0}({1})", player.playerName, player.uid);
                }
                else
                {
                    listTXT += "," + string.Format("{0}({1})", player.playerName, player.uid); ;
                }
                i++;
            }
            logsSystem.Print(string.Format("\r\n\t当前在线玩家为{0}(游戏中{3})/{1}\r\n\t玩家列表:{2}", i, PlayerManager.Instance.maxPlayerNumber, listTXT, PlayerManager.Instance.GetGamePlayerNumber()));
        }

        /// <summary>
        /// 显示房间信息
        /// </summary>
        private void ShowRoomsInfo(object sender, RoutedEventArgs e)
        {
            try
            {
                string showText = "";
                GameRoomManager grm = TcpServer.Instance.GetGameRoomManager();
                if (grm != null)
                {
                    showText = string.Format("\n\t正在进行游戏的房间有 {0} 个,\n\t已绑定的用户连接有 {1} 个,\n\t未知的连接有 {2} 个", grm.rooms.Count, grm.freedomPlayer.Count, grm.unknownSocket.Count);
                }
                else
                {
                    showText = "无法取得房间信息。请确认游戏服务器已经开启";
                }

                logsSystem.Print(showText);
            }
            catch (Exception ex)
            {
                logsSystem.Print("无法获取房间信息异常：" + ex.ToString(), LogLevel.WARN);
            }
        }
        #endregion

        /// <summary>
        /// 显示版本信息
        /// </summary>
        private void ShowVersionInfo(object sender, RoutedEventArgs e)
        {
            logsSystem.Print(string.Format("\n\t当前版本信息：\n\t\t内部版本号：{0}\n\t\t正式版本号：{1}", internalVersion, officialVersion));
        }
    }
}
